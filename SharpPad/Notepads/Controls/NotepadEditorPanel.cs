//
// Copyright (c) 2023-2024 REghZy
//
// This file is part of SharpPad.
//
// SharpPad is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
//
// SharpPad is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using SharpPad.Interactivity;
using SharpPad.Interactivity.Contexts;
using SharpPad.Utils;
using SharpPad.Utils.RDA;

namespace SharpPad.Notepads.Controls
{
    /// <summary>
    /// A class that manages the currently viewed notepad document(s)
    /// </summary>
    public class NotepadEditorPanel : Control
    {
        public static readonly DependencyProperty NotepadProperty = DependencyProperty.Register("Notepad", typeof(Notepad), typeof(NotepadEditorPanel), new PropertyMetadata(null, (d, e) => ((NotepadEditorPanel) d).OnNotepadChanged((Notepad) e.OldValue, (Notepad) e.NewValue)));
        public static readonly DependencyProperty IsDroppableTargetOverProperty = DependencyProperty.Register("IsDroppableTargetOver", typeof(bool), typeof(NotepadEditorPanel), new PropertyMetadata(BoolBox.False));

        public Notepad Notepad
        {
            get => (Notepad) this.GetValue(NotepadProperty);
            set => this.SetValue(NotepadProperty, value);
        }

        public bool IsDroppableTargetOver
        {
            get => (bool) this.GetValue(IsDroppableTargetOverProperty);
            set => this.SetValue(IsDroppableTargetOverProperty, value.Box());
        }

        private NotepadTabControl PART_TabControl;
        private TextEditor PART_TextEditor;
        private NotepadDocument activeDocument;

        private readonly ContextData contextData;
        private bool isProcessingAsyncDrop;

        public TextEditor Editor => this.PART_TextEditor;

        // use RDA to prevent lag if the active document changes very quickly, e.g. opening files
        // but the active document is set during each file opened for some reason
        private readonly RapidDispatchAction<NotepadDocument> updateActiveDocumentRDA;

        public NotepadEditorPanel()
        {
            this.contextData = new ContextData();
            this.updateActiveDocumentRDA = new RapidDispatchAction<NotepadDocument>(this.SetActiveDocument, DispatcherPriority.Render);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.PART_TabControl = this.GetTemplateChild(nameof(this.PART_TabControl)) as NotepadTabControl ?? throw new Exception("Missing " + nameof(this.PART_TabControl));
            this.PART_TextEditor = this.GetTemplateChild(nameof(this.PART_TextEditor)) as TextEditor ?? throw new Exception("Missing " + nameof(this.PART_TextEditor));
            DataManager.SetContextData(this, this.contextData.Set(DataKeys.TextEditorKey, this.PART_TextEditor).Clone());
        }

        private void OnNotepadChanged(Notepad oldNotepad, Notepad newNotepad)
        {
            if (oldNotepad != null)
            {
                oldNotepad.ActiveDocumentChanged -= this.OnActiveDocumentChanged;
            }

            if (newNotepad != null)
            {
                newNotepad.ActiveDocumentChanged += this.OnActiveDocumentChanged;
                DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadKey, newNotepad).Set(DataKeys.DocumentKey, newNotepad.ActiveDocument).Clone());
            }
            else
            {
                // clear since this editor panel will be disabled when no notepad is available
                DataManager.ClearContextData(this);
            }

            this.PART_TabControl.Notepad = newNotepad;
            this.SetActiveDocument(newNotepad?.ActiveDocument);
        }

        private void OnActiveDocumentChanged(Notepad notepad, NotepadDocument oldDocument, NotepadDocument newDocument)
        {
            this.updateActiveDocumentRDA.InvokeAsync(newDocument);
        }

        public void SetActiveDocument(NotepadDocument document)
        {
            if (this.activeDocument != null)
            {
                this.activeDocument.RemoveEditor(this.PART_TextEditor);
                this.activeDocument = null;
            }

            if (document != null)
            {
                this.activeDocument = document;
                this.SetVisibility(Visibility.Visible);
                this.PART_TextEditor.IsEnabled = true;
                this.PART_TextEditor.Document = document.Document;
                this.activeDocument.AddEditor(this.PART_TextEditor);
            }
            else
            {
                this.SetVisibility(Visibility.Collapsed);
                this.PART_TextEditor.Document = null;
                this.PART_TextEditor.IsEnabled = false;
            }

            DataManager.SetContextData(this, this.contextData.Set(DataKeys.DocumentKey, document).Clone());
        }

        private void SetVisibility(Visibility visibility)
        {
            this.PART_TextEditor.Visibility = visibility;
            this.PART_TabControl.Visibility = visibility;
        }

        #region Drag dropping

        protected override void OnDragEnter(DragEventArgs e)
        {
            this.OnDragOver(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            e.Handled = true;
            if (this.isProcessingAsyncDrop)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            EnumDropType inputEffects = DropUtils.GetDropAction((int) e.KeyStates, (EnumDropType) e.Effects);
            if (inputEffects != EnumDropType.None && this.Notepad is Notepad notepad)
            {
                EnumDropType outputEffects = NotepadDropRegistry.DropRegistry.CanDropNative(notepad, new DataObjectWrapper(e.Data), inputEffects);
                if (outputEffects != EnumDropType.None)
                {
                    this.OnAcceptDrop();
                    e.Effects = (DragDropEffects) outputEffects;
                }
                else
                {
                    this.IsDroppableTargetOver = false;
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        private void OnAcceptDrop()
        {
            if (!this.IsDroppableTargetOver)
                this.IsDroppableTargetOver = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            this.Dispatcher.Invoke(() => this.ClearValue(IsDroppableTargetOverProperty), DispatcherPriority.Loaded);
        }

        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            e.Handled = true;
            if (this.isProcessingAsyncDrop || !(this.Notepad is Notepad clip))
                return;

            EnumDropType effects = DropUtils.GetDropAction((int) e.KeyStates, (EnumDropType) e.Effects);
            if (e.Effects == DragDropEffects.None)
            {
                return;
            }

            try
            {
                this.isProcessingAsyncDrop = true;
                await NotepadDropRegistry.DropRegistry.OnDroppedNative(clip, new DataObjectWrapper(e.Data), effects);
            }
            finally
            {
                this.isProcessingAsyncDrop = false;
                this.IsDroppableTargetOver = false;
            }
        }

        #endregion
    }
}