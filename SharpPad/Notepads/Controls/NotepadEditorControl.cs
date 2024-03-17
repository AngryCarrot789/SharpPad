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

namespace SharpPad.Notepads.Controls {
    /// <summary>
    /// A control that manages a notepad object. This tracks the active editor, handles drag-drop, etc.
    /// <para>
    /// Each notepad window will have one of these, unless I implement a split-screen feature, in which case
    /// where will be multiple instances of this control per window
    /// </para>
    /// </summary>
    public class NotepadEditorControl : Control, INotepadEditorUI {
        public static readonly DependencyProperty NotepadProperty = DependencyProperty.Register("Notepad", typeof(Notepad), typeof(NotepadEditorControl), new PropertyMetadata(null, (d, e) => ((NotepadEditorControl) d).OnNotepadChanged((Notepad) e.OldValue, (Notepad) e.NewValue)));
        public static readonly DependencyProperty IsDroppableTargetOverProperty = DependencyProperty.Register("IsDroppableTargetOver", typeof(bool), typeof(NotepadEditorControl), new PropertyMetadata(BoolBox.False));

        public Notepad Notepad {
            get => (Notepad) this.GetValue(NotepadProperty);
            set => this.SetValue(NotepadProperty, value);
        }

        public bool IsDroppableTargetOver {
            get => (bool) this.GetValue(IsDroppableTargetOverProperty);
            set => this.SetValue(IsDroppableTargetOverProperty, value.Box());
        }

        /// <summary>
        /// Gets the raw text editor control
        /// </summary>
        public TextEditor Editor => this.PART_TextEditor;

        // controls
        private NotepadTabControl PART_TabControl;
        private TextEditor PART_TextEditor;
        private FindAndReplaceControl PART_FindAndReplacePanel;

        // shit
        private NotepadEditor activeEditor;
        private FindAndReplaceModel activeFindModel; // a ref to activeEditor.FindModel
        private NotepadDocument activeDocument;
        private readonly ContextData contextData;
        private bool isProcessingAsyncDrop;

        // use RDA to prevent lag if the active document changes very quickly, e.g. opening files
        // but the active document is set during each file opened for some reason
        private readonly RapidDispatchAction<NotepadEditor> updateActiveEditorRda;

        public NotepadEditorControl() {
            this.contextData = new ContextData().Set(DataKeys.UINotepadEditorKey, this);
            this.updateActiveEditorRda = new RapidDispatchAction<NotepadEditor>(this.SetActiveEditor, DispatcherPriority.Render);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            this.PART_TabControl = this.GetTemplateChild(nameof(this.PART_TabControl)) as NotepadTabControl ?? throw new Exception("Missing " + nameof(this.PART_TabControl));
            this.PART_TextEditor = this.GetTemplateChild(nameof(this.PART_TextEditor)) as TextEditor ?? throw new Exception("Missing " + nameof(this.PART_TextEditor));
            this.PART_FindAndReplacePanel = this.GetTemplateChild(nameof(this.PART_FindAndReplacePanel)) as FindAndReplaceControl ?? throw new Exception("Missing " + nameof(this.PART_FindAndReplacePanel));
            this.PART_FindAndReplacePanel.Visibility = Visibility.Collapsed;
            if (this.activeEditor != null)
                this.activeEditor.TextEditor = this.PART_TextEditor;
        }

        private void OnNotepadChanged(Notepad oldNotepad, Notepad newNotepad) {
            if (oldNotepad != null) {
                oldNotepad.ActiveEditorChanged -= this.OnActiveEditorChanged;
            }

            if (newNotepad != null) {
                newNotepad.ActiveEditorChanged += this.OnActiveEditorChanged;
            }

            DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadKey, newNotepad).Clone());

            this.PART_TabControl.Notepad = newNotepad;
            this.SetActiveEditor(newNotepad?.ActiveEditor);
        }

        private void OnActiveEditorChanged(object sender, NotepadEditor oldEditor, NotepadEditor newEditor) {
            this.updateActiveEditorRda.InvokeAsync(newEditor);
        }

        public void SetActiveEditor(NotepadEditor editor) {
            if (this.activeEditor != null) {
                this.activeEditor.DocumentChanged -= this.OnActiveEditorDocumentChanged;
                this.SetFindModel(null);
                this.activeEditor.TextEditor = null;
                this.activeEditor = null;
            }

            if (editor != null) {
                if (editor.TextEditor != null)
                    throw new InvalidOperationException("Editor is already associated with another control");

                this.activeEditor = editor;
                if (this.PART_TextEditor != null)
                    editor.TextEditor = this.PART_TextEditor;
                editor.DocumentChanged += this.OnActiveEditorDocumentChanged;
                this.SetVisibility(Visibility.Visible);
                this.SetActiveDocument(editor.Document);
                this.SetFindModel(editor.FindModel);
            }
            else {
                this.SetVisibility(Visibility.Collapsed);
                this.SetActiveDocument(null);
                this.PART_TextEditor.Document = null;
                this.PART_TextEditor.IsEnabled = false;
            }

            DataManager.SetContextData(this, this.contextData.Set(DataKeys.EditorKey, editor).Clone());
        }

        private void OnActiveEditorDocumentChanged(NotepadEditor editor, NotepadDocument olddoc, NotepadDocument newDoc) {
            this.SetActiveDocument(newDoc);
            this.SetFindModel(editor.FindModel);
        }

        private void SetActiveDocument(NotepadDocument document) {
            if (this.activeDocument != null) {
                // code...
                this.activeDocument = null;
            }

            if (document != null) {
                this.PART_TextEditor.IsEnabled = true;
                this.PART_TextEditor.Document = document.Document;
                this.activeDocument = document;
            }
            else {
                this.PART_TextEditor.IsEnabled = false;
                this.PART_TextEditor.Document = null;
            }

            DataManager.SetContextData(this, this.contextData.Set(DataKeys.DocumentKey, document).Clone());
        }

        private void SetFindModel(FindAndReplaceModel model) {
            if (this.activeFindModel != null) {
                this.activeFindModel.SearchResultsChanged -= this.OnSearchResultsChanged;
            }

            if ((this.activeFindModel = model) != null) {
                this.activeFindModel.SearchResultsChanged += this.OnSearchResultsChanged;
                this.PART_FindAndReplacePanel.Visibility = Visibility.Visible;
                this.PART_FindAndReplacePanel.FindModel = model;
            }
            else {
                this.PART_FindAndReplacePanel.FindModel = null;
                this.PART_FindAndReplacePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnSearchResultsChanged(FindAndReplaceModel model) {
            int currentOffset = this.PART_TextEditor.CaretOffset;
            int index = BinarySearch.IndexOf(model.Results, currentOffset, (e) => e.Index);
            if (index < 0)
                index = ~index;

            TextRange range = model.Results[index];
            this.PART_TextEditor.Select(range.Index, range.Length);

            TextViewPosition caret = this.PART_TextEditor.TextArea.Caret.Position;
            this.PART_TextEditor.ScrollTo(caret.Line, caret.Column);
        }

        private void SetVisibility(Visibility visibility) {
            this.PART_TextEditor.Visibility = visibility;
            this.PART_TabControl.Visibility = visibility;
        }

        #region Drag dropping

        protected override void OnDragEnter(DragEventArgs e) {
            this.OnDragOver(e);
        }

        protected override void OnDragOver(DragEventArgs e) {
            e.Handled = true;
            if (this.isProcessingAsyncDrop) {
                e.Effects = DragDropEffects.None;
                return;
            }

            EnumDropType inputEffects = DropUtils.GetDropAction((int) e.KeyStates, (EnumDropType) e.Effects);
            if (inputEffects != EnumDropType.None && this.Notepad is Notepad notepad) {
                EnumDropType outputEffects = NotepadDropRegistry.DropRegistry.CanDropNative(notepad, new DataObjectWrapper(e.Data), inputEffects);
                if (outputEffects != EnumDropType.None) {
                    this.OnAcceptDrop();
                    e.Effects = (DragDropEffects) outputEffects;
                }
                else {
                    this.IsDroppableTargetOver = false;
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        private void OnAcceptDrop() {
            if (!this.IsDroppableTargetOver)
                this.IsDroppableTargetOver = true;
        }

        protected override void OnDragLeave(DragEventArgs e) {
            base.OnDragLeave(e);
            this.Dispatcher.Invoke(() => this.ClearValue(IsDroppableTargetOverProperty), DispatcherPriority.Loaded);
        }

        protected override async void OnDrop(DragEventArgs e) {
            base.OnDrop(e);
            e.Handled = true;
            if (this.isProcessingAsyncDrop || !(this.Notepad is Notepad clip))
                return;

            EnumDropType effects = DropUtils.GetDropAction((int) e.KeyStates, (EnumDropType) e.Effects);
            if (e.Effects == DragDropEffects.None)
                return;

            try {
                this.isProcessingAsyncDrop = true;
                await NotepadDropRegistry.DropRegistry.OnDroppedNative(clip, new DataObjectWrapper(e.Data), effects);
            }
            finally {
                this.isProcessingAsyncDrop = false;
                this.IsDroppableTargetOver = false;
            }
        }

        #endregion
    }
}