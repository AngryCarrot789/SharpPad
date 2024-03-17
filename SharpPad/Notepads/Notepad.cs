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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpPad.Notepads {
    public delegate void NotepadActiveEditorChangedEventHandler(Notepad sender, NotepadEditor oldEditor, NotepadEditor newEditor);
    public delegate void NotepadEditorIndexMovedEventHandler(Notepad notepad, NotepadEditor editor, int oldIndex, int newIndex);

    /// <summary>
    /// The class for a single notepad window
    /// </summary>
    public class Notepad {
        private readonly List<NotepadEditor> editors;
        private NotepadEditor activeEditor;

        /// <summary>
        /// Gets the read-only collection that backs the list of editors currently open in this notepad object
        /// </summary>
        public ReadOnlyCollection<NotepadEditor> Editors { get; }

        public NotepadEditor ActiveEditor {
            get => this.activeEditor;
            set {
                NotepadEditor doc = this.activeEditor;
                if (doc == value)
                    return;
                this.activeEditor = value;
                this.ActiveEditorChanged?.Invoke(this, doc, value);
            }
        }

        /// <summary>
        /// An event called when an editor is added to, removed from or moved within a notepad.
        /// <para>
        /// When adding, oldIndex will be -1 and newIndex will be valid. When removing, oldIndex
        /// will be valid and newIndex will be -1. When moving, oldIndex and newIndex will be valid
        /// </para>
        /// </summary>
        public event NotepadEditorIndexMovedEventHandler EditorIndexChanged;

        /// <summary>
        /// An event fired when the active editor (aka selected notepad tab) changes
        /// </summary>
        public event NotepadActiveEditorChangedEventHandler ActiveEditorChanged;

        public Notepad() {
            this.editors = new List<NotepadEditor>();
            this.Editors = this.editors.AsReadOnly();
        }

        public NotepadEditor AddNewEditor(NotepadDocument document) {
            NotepadEditor editor = new NotepadEditor(document);
            this.InsertEditor(this.editors.Count, editor);
            return editor;
        }

        public void AddEditor(NotepadEditor editor) => this.InsertEditor(this.editors.Count, editor);

        public void InsertEditor(int index, NotepadEditor editor) {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            if (editor.IsOwnedBy(this))
                throw new InvalidOperationException("editor already added");

            this.editors.Insert(index, editor);
            NotepadEditor.InternalAddViewerUnsafe(editor, this);
            this.EditorIndexChanged?.Invoke(this, editor, -1, index);

            if (this.editors.Count == 1)
                this.ActiveEditor = editor;
        }

        public bool RemoveEditor(NotepadEditor editor) {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            int index = this.editors.IndexOf(editor);
            if (index == -1)
                return false;

            this.RemoveEditorAt(index);
            return true;
        }

        public void RemoveEditorAt(int index) {
            NotepadEditor editor = this.editors[index];
            if (!editor.IsOwnedBy(this))
                throw new Exception("Fatal error: editor in this notepad was not owned by this notepad instance");

            NotepadEditor.InternalRemoveViewerUnsafe(editor, this);
            this.editors.RemoveAt(index);
            this.EditorIndexChanged?.Invoke(this, editor, index, -1);
        }
    }
}