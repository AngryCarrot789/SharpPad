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
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SharpPad.Utils;

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
                NotepadEditor oldEditor = this.activeEditor;
                if (oldEditor == value) {
                    return;
                }

                if (value != null && !value.IsOwnedBy(this)) {
                    throw new InvalidOperationException("The new editor is not owned by this notepad. It must be added first via" + nameof(this.AddEditor));
                }

                this.activeEditor = value;
                this.ActiveEditorChanged?.Invoke(this, oldEditor, value);
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

        /// <summary>
        /// Creates and adds a new notepad editor using the given initial document
        /// </summary>
        /// <param name="document">The document the editor will use</param>
        /// <returns>The created editor</returns>
        public NotepadEditor AddNewEditorForDocument(NotepadDocument document) {
            NotepadEditor editor = new NotepadEditor(document);
            this.AddEditor(editor);
            return editor;
        }

        public void AddEditor(NotepadEditor editor) => this.InsertEditor(this.editors.Count, editor);

        public void InsertEditor(int index, NotepadEditor editor) {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            if (editor.Owner != null)
                throw new InvalidOperationException("The editor is already associated with another notepad instance");

            this.editors.Insert(index, editor);
            NotepadEditor.SetOwner(editor, this);
            this.EditorIndexChanged?.Invoke(this, editor, -1, index);

            // Check it is owned by us, just in case an event handler removes it immediately for some reason...
            if (this.editors.Count == 1 && editor.IsOwnedBy(this)) {
                this.ActiveEditor = editor;
            }
        }

        public bool RemoveEditor(NotepadEditor editorToAdd) {
            if (editorToAdd == null)
                throw new ArgumentNullException(nameof(editorToAdd));
            if (!editorToAdd.IsOwnedBy(this))
                return false;

            int index = this.editors.IndexOf(editorToAdd);
            if (index == -1) {
                Debug.Assert(false, "Fatal error: we owned the editor but it did not exist in our list");
                return false;
            }

            this.RemoveEditorAt(index);
            return true;
        }

        public void RemoveEditorAt(int index) {
            NotepadEditor editorToRemove = this.editors[index];
            if (editorToRemove == this.activeEditor) {
                // Clear or change active editor, to allow the old one to possibly be GC'd
                int newActiveIndex = CollectionUtils.GetNeighbourIndex(this.editors, index);
                this.ActiveEditor = newActiveIndex == -1 ? null : this.editors[newActiveIndex];
            }

            this.editors.RemoveAt(index);
            NotepadEditor.SetOwner(editorToRemove, null);
            this.EditorIndexChanged?.Invoke(this, editorToRemove, index, -1);
        }
    }
}