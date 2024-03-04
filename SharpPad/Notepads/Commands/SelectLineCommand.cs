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
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Commands {
    public class SelectLineCommand : Command {
        public override Executability CanExecute(CommandEventArgs e) {
            return e.ContextData.ContainsKey(DataKeys.TextEditorKey) ? Executability.Valid : Executability.Invalid;
        }

        protected override Task Execute(CommandEventArgs e) {
            if (!DataKeys.TextEditorKey.TryGetContext(e.ContextData, out TextEditor editor))
                return Task.CompletedTask;

            TextDocument document = editor.Document;
            if (document != null) {
                int lineNumber = editor.TextArea.Caret.Line;
                DocumentLine currentLine = document.GetLineByNumber(lineNumber);
                int startOffset = currentLine.Offset;
                int endOffset = currentLine.EndOffset;

                editor.Select(startOffset, endOffset - startOffset);
            }

            return Task.CompletedTask;
        }
    }
}