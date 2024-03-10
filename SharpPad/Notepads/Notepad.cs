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

namespace SharpPad.Notepads
{
    public delegate void NotepadEventHandler(Notepad notepad);

    public delegate void NotepadActiveDocumentChangedEventHandler(Notepad notepad, NotepadDocument oldDocument, NotepadDocument newDocument);

    public delegate void NotepadDocumentIndexEventHandler(Notepad notepad, NotepadDocument document, int oldIndex, int newIndex);

    /// <summary>
    /// The class for a single notepad window
    /// </summary>
    public class Notepad
    {
        private readonly List<NotepadDocument> documents;
        private NotepadDocument activeDocument;

        public ReadOnlyCollection<NotepadDocument> Documents { get; }

        public NotepadDocument ActiveDocument
        {
            get => this.activeDocument;
            set
            {
                NotepadDocument doc = this.activeDocument;
                if (doc == value)
                    return;
                this.activeDocument = value;
                this.ActiveDocumentChanged?.Invoke(this, doc, value);
            }
        }

        /// <summary>
        /// An event called when a document is added to, removed from or moved within a notepad.
        /// <para>
        /// When adding, oldIndex will be -1 and newIndex will be valid. When removing, oldIndex
        /// will be valid and newIndex will be -1. When moving, oldIndex and newIndex will be valid
        /// </para>
        /// </summary>
        public event NotepadDocumentIndexEventHandler DocumentIndexChanged;

        public event NotepadActiveDocumentChangedEventHandler ActiveDocumentChanged;

        public Notepad()
        {
            this.documents = new List<NotepadDocument>();
            this.Documents = this.documents.AsReadOnly();
        }

        public void AddDocument(NotepadDocument document) => this.InsertDocument(this.documents.Count, document);

        public void InsertDocument(int index, NotepadDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (NotepadDocument.IsOwnedBy(document, this))
                throw new InvalidOperationException("Document already added");
            this.documents.Insert(index, document);
            NotepadDocument.AddOwner(document, this);
            this.DocumentIndexChanged?.Invoke(this, document, -1, index);

            if (this.documents.Count == 1)
                this.ActiveDocument = document;
        }

        public bool RemoveDocument(NotepadDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            int index = this.documents.IndexOf(document);
            if (index == -1)
                return false;
            this.RemoveDocumentAt(index);
            return true;
        }

        public void RemoveDocumentAt(int index)
        {
            NotepadDocument document = this.documents[index];
            NotepadDocument.RemoveOwner(document, this);
            this.documents.RemoveAt(index);
            this.DocumentIndexChanged?.Invoke(this, document, index, -1);
        }
    }
}