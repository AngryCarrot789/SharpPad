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
using System.IO;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace SharpPad.Notepads {
    public delegate void DocumentEventHandler(NotepadDocument document);

    /// <summary>
    /// A document that exists in one or more notepad views
    /// </summary>
    public class NotepadDocument {
        private readonly List<Notepad> owners;
        private string filePath;
        private bool isModified;
        private readonly List<TextEditor> editors;

        /// <summary>
        /// Gets the text editors that are using this document
        /// </summary>
        public IReadOnlyList<TextEditor> Editors => this.editors;

        /// <summary>
        /// Gets or sets the document display name. There is no event fired when this changes,
        /// as it is assumed to only change either during initialisation or when <see cref="FilePath"/>
        /// changes in which case you can listen to <see cref="FilePathChanged"/>
        /// </summary>
        public string DocumentName { get; set; }

        /// <summary>
        /// Gets or sets the file path of this document on the system. Can only
        /// be set as a valid file path, otherwise exceptions will be thrown
        /// </summary>
        public string FilePath {
            get => this.filePath;
            set {
                if (this.filePath == value)
                    return;
                this.filePath = Path.GetFullPath(value);
                this.DocumentName = Path.GetFileName(this.filePath);
                this.FilePathChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Gets or sets the modified state of this document. When modified, it shows
        /// an indicator to the user that they should save their changes to the disk
        /// </summary>
        public bool IsModified {
            get => this.isModified;
            set {
                if (this.isModified == value)
                    return;
                this.isModified = value;
                this.IsModifiedChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Gets the AvalonEdit document that represents the contents of the text document.
        /// This property does not change, it is initialised during the constructor
        /// </summary>
        public TextDocument Document { get; }

        public event DocumentEventHandler FilePathChanged;
        public event DocumentEventHandler IsModifiedChanged;

        public NotepadDocument() {
            this.owners = new List<Notepad>();
            this.DocumentName = "New Document";
            this.Document = new TextDocument();
            this.editors = new List<TextEditor>();

            this.Document.Changed += this.OnDocumentContentChanged;
        }

        private void OnDocumentContentChanged(object sender, DocumentChangeEventArgs e) {
            this.IsModified = true;
        }

        public void AddEditor(TextEditor editor) {
            if (this.editors.Contains(editor))
                throw new InvalidOperationException("Editor already added");
            this.editors.Add(editor);
        }

        public void RemoveEditor(TextEditor editor) {
            if (!this.editors.Remove(editor))
                throw new InvalidOperationException("Editor already removed");
        }

        internal static void AddOwner(NotepadDocument document, Notepad notepad) {
            if (document.owners.Contains(notepad))
                throw new InvalidOperationException("Document already owned by the notepad");
            document.owners.Add(notepad);
        }

        internal static void RemoveOwner(NotepadDocument document, Notepad notepad) {
            if (!document.owners.Remove(notepad))
                throw new InvalidOperationException("Document was not owned by the notepad");
        }

        public static bool IsOwnedBy(NotepadDocument document, Notepad notepad) {
            return document.owners.Contains(notepad);
        }
    }
}