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
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;
using SharpPad.Utils;

namespace SharpPad.Notepads.Commands {
    public abstract class NotepadCommand : Command {
        public override Executability CanExecute(CommandEventArgs e) {
            if (!DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                return Executability.Invalid;
            return this.CanExecute(notepad, e);
        }

        protected override Task Execute(CommandEventArgs e) {
            if (!DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                return Task.CompletedTask;
            return this.Execute(notepad, e);
        }

        public abstract Executability CanExecute(Notepad notepad, CommandEventArgs e);

        public abstract Task Execute(Notepad notepad, CommandEventArgs e);
    }

    public class NewFileCommand : NotepadCommand {
        public override Executability CanExecute(Notepad notepad, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override Task Execute(Notepad notepad, CommandEventArgs e) {
            notepad.AddDocument(new NotepadDocument() {
                DocumentName = "New Document " + (notepad.Documents.Count + 1)
            });
            return Task.CompletedTask;
        }
    }

    public class OpenFileCommand : NotepadCommand {
        public override Executability CanExecute(Notepad notepad, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override Task Execute(Notepad notepad, CommandEventArgs e) {
            string[] filePaths = IoC.FilePickService.OpenMultipleFiles("Select files to open", Filters.TextTypesAndAll);
            if (filePaths == null) {
                return Task.CompletedTask;
            }

            NotepadDocument lastDocument = null;
            foreach (string file in filePaths) {
                NotepadDocument document = new NotepadDocument() {
                    FilePath = file
                };

                TextDocument doc = document.Document;
                try {
                    doc.Text = File.ReadAllText(file);
                }
                catch (Exception ex) {
                    IoC.MessageService.ShowMessage("Error", "Error reading file", ex.GetToString());
                }

                notepad.AddDocument(lastDocument = document);
            }

            if (lastDocument != null) {
                notepad.ActiveDocument = lastDocument;
            }

            return Task.CompletedTask;
        }
    }
}