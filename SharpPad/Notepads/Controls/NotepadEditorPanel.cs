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
using ICSharpCode.AvalonEdit;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Controls {
    /// <summary>
    /// A class that manages the currently viewed notepad document(s)
    /// </summary>
    public class NotepadEditorPanel : Control {
        public static readonly DependencyProperty NotepadProperty = DependencyProperty.Register("Notepad", typeof(Notepad), typeof(NotepadEditorPanel), new PropertyMetadata(null, (d, e) => ((NotepadEditorPanel) d).OnNotepadChanged((Notepad) e.OldValue, (Notepad) e.NewValue)));

        public Notepad Notepad {
            get => (Notepad) this.GetValue(NotepadProperty);
            set => this.SetValue(NotepadProperty, value);
        }

        private NotepadTabControl PART_TabControl;
        private TextEditor PART_TextEditor;
        private NotepadDocument activeDocument;

        private readonly ContextData contextData;

        public NotepadEditorPanel() {
            this.contextData = new ContextData();
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.PART_TabControl = this.GetTemplateChild(nameof(this.PART_TabControl)) as NotepadTabControl ?? throw new Exception("Missing " + nameof(this.PART_TabControl));
            this.PART_TextEditor = this.GetTemplateChild(nameof(this.PART_TextEditor)) as TextEditor ?? throw new Exception("Missing " + nameof(this.PART_TextEditor));
            DataManager.SetContextData(this, this.contextData.Set(DataKeys.TextEditorKey, this.PART_TextEditor).Clone());
        }

        private void OnNotepadChanged(Notepad oldNotepad, Notepad newNotepad) {
            if (oldNotepad != null) {
                oldNotepad.ActiveDocumentChanged -= this.OnActiveDocumentChanged;
            }

            if (newNotepad != null) {
                newNotepad.ActiveDocumentChanged += this.OnActiveDocumentChanged;
                DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadKey, newNotepad).Set(DataKeys.DocumentKey, newNotepad.ActiveDocument).Clone());
            }
            else {
                // clear since this editor panel will be disabled when no notepad is available
                DataManager.ClearContextData(this);
            }

            this.PART_TabControl.Notepad = newNotepad;
            this.SetActiveDocument(newNotepad?.ActiveDocument);
        }

        private void OnActiveDocumentChanged(Notepad notepad, NotepadDocument oldDocument, NotepadDocument newDocument) {
            this.SetActiveDocument(newDocument);
        }

        private void SetActiveDocument(NotepadDocument document) {
            if (this.activeDocument != null) {
                this.activeDocument.RemoveEditor(this.PART_TextEditor);
                this.activeDocument = null;
            }

            if (document != null) {
                this.activeDocument = document;
                this.Visibility = Visibility.Visible;
                this.PART_TextEditor.IsEnabled = true;
                this.PART_TextEditor.Document = document.Document;
                this.activeDocument.AddEditor(this.PART_TextEditor);
            }
            else {
                this.Visibility = Visibility.Collapsed;
                this.PART_TextEditor.Document = null;
                this.PART_TextEditor.IsEnabled = false;
            }

            DataManager.SetContextData(this, this.contextData.Set(DataKeys.DocumentKey, document).Clone());
        }
    }
}