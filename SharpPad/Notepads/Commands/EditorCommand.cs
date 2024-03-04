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
    public abstract class EditorCommand : Command {
        public sealed override Executability CanExecute(CommandEventArgs e) {
            if (!DataKeys.TextEditorKey.TryGetContext(e.ContextData, out TextEditor editor))
                return Executability.Invalid;
            TextDocument document = editor.Document;
            return document == null ? Executability.Invalid : this.CanExecute(editor, document, e);
        }

        protected sealed override Task Execute(CommandEventArgs e) {
            if (!DataKeys.TextEditorKey.TryGetContext(e.ContextData, out TextEditor editor))
                return Task.CompletedTask;
            TextDocument document = editor.Document;
            if (document == null)
                return Task.CompletedTask;
            return this.Execute(editor, document, e);
        }

        public abstract Executability CanExecute(TextEditor editor, TextDocument document, CommandEventArgs e);

        public abstract Task Execute(TextEditor editor, TextDocument document, CommandEventArgs e);
    }

    public class SelectLineCommand : EditorCommand {
        public override Executability CanExecute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override Task Execute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            int lineNumber = editor.TextArea.Caret.Line;
            int totalLines = document.LineCount;
            DocumentLine currentLine = document.GetLineByNumber(lineNumber);
            DocumentLine nextLine = (lineNumber + 1) >= totalLines ? currentLine : document.GetLineByNumber(lineNumber + 1);

            int startOffset = currentLine.Offset;
            int endOffset = nextLine.Offset;

            editor.Select(startOffset, endOffset - startOffset);
            return Task.CompletedTask;
        }
    }
}