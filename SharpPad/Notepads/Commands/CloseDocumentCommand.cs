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

using System.Threading.Tasks;
using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Commands {
    public class CloseDocumentCommand : Command {
        public override Executability CanExecute(CommandEventArgs e) {
            return e.ContextData.ContainsAll(DataKeys.NotepadKey, DataKeys.DocumentKey) ? Executability.Valid : Executability.Invalid;
        }

        protected override Task Execute(CommandEventArgs e) {
            if (!DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                return Task.CompletedTask;
            if (!DataKeys.DocumentKey.TryGetContext(e.ContextData, out NotepadDocument document))
                return Task.CompletedTask;

            int index = notepad.Documents.IndexOf(document);
            if (index == -1) {
                return Task.CompletedTask;
            }

            bool isActiveDocument = notepad.ActiveDocument == document;
            notepad.RemoveDocumentAt(index);
            if (!isActiveDocument) {
                if (index > 0) {
                    index--;
                }

                if (index < notepad.Documents.Count) {
                    notepad.ActiveDocument = notepad.Documents[index];
                }
            }

            return Task.CompletedTask;
        }
    }
}