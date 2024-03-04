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
        private string documentName;
        private string filePath;
        private bool isModified;
        private readonly List<TextEditor> editors;

        public IReadOnlyList<TextEditor> Editors => this.editors;

        public string DocumentName {
            get => this.documentName;
            set {
                if (this.documentName == value)
                    return;
                this.documentName = value;
                this.DocumentNameChanged?.Invoke(this);
            }
        }

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

        public bool IsModified {
            get => this.isModified;
            set {
                if (this.isModified == value)
                    return;
                this.isModified = value;
                this.IsModifiedChanged?.Invoke(this);
            }
        }


        public TextDocument Document { get; }

        public event DocumentEventHandler DocumentNameChanged;
        public event DocumentEventHandler FilePathChanged;
        public event DocumentEventHandler IsModifiedChanged;

        public NotepadDocument() {
            this.owners = new List<Notepad>();
            this.documentName = "New Document";
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