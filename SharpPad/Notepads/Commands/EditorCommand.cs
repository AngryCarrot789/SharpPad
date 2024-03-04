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
using System.Threading.Tasks;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
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

    public class EditorApplicationCommand : EditorCommand {
        public RoutedCommand Command { get; }

        public EditorApplicationCommand(RoutedCommand command) {
            this.Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public override Executability CanExecute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            return this.Command.CanExecute(null, editor.TextArea) ? Executability.Valid : Executability.ValidButCannotExecute;
        }

        public override Task Execute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            this.Command.Execute(null, editor.TextArea);
            return Task.CompletedTask;
        }
    }

    public class CutCommand : EditorApplicationCommand {public CutCommand() : base(ApplicationCommands.Cut) {}}
    public class CopyCommand : EditorApplicationCommand {public CopyCommand() : base(ApplicationCommands.Copy) {}}
    public class PasteCommand : EditorApplicationCommand {public PasteCommand() : base(ApplicationCommands.Paste) {}}
    public class UndoCommand : EditorApplicationCommand {public UndoCommand() : base(ApplicationCommands.Undo) {}}
    public class RedoCommand : EditorApplicationCommand {public RedoCommand() : base(ApplicationCommands.Redo) {}}
    public class SelectAllCommand : EditorApplicationCommand { public SelectAllCommand() : base(ApplicationCommands.SelectAll) { } }
    public class ToggleOverstrikeCommand : EditorApplicationCommand { public ToggleOverstrikeCommand() : base(AvalonEditCommands.ToggleOverstrike) { } }
    public class DeleteLineCommand : EditorApplicationCommand { public DeleteLineCommand() : base(AvalonEditCommands.DeleteLine) { } }
    public class RemoveLeadingWhitespaceCommand : EditorApplicationCommand { public RemoveLeadingWhitespaceCommand() : base(AvalonEditCommands.RemoveLeadingWhitespace) { } }
    public class RemoveTrailingWhitespaceCommand : EditorApplicationCommand { public RemoveTrailingWhitespaceCommand() : base(AvalonEditCommands.RemoveTrailingWhitespace) { } }
    public class ConvertToUppercaseCommand : EditorApplicationCommand { public ConvertToUppercaseCommand() : base(AvalonEditCommands.ConvertToUppercase) { } }
    public class ConvertToLowercaseCommand : EditorApplicationCommand { public ConvertToLowercaseCommand() : base(AvalonEditCommands.ConvertToLowercase) { } }
    public class ConvertToTitleCaseCommand : EditorApplicationCommand { public ConvertToTitleCaseCommand() : base(AvalonEditCommands.ConvertToTitleCase) { } }
    public class InvertCaseCommand : EditorApplicationCommand { public InvertCaseCommand() : base(AvalonEditCommands.InvertCase) { } }
    public class ConvertTabsToSpacesCommand : EditorApplicationCommand { public ConvertTabsToSpacesCommand() : base(AvalonEditCommands.ConvertTabsToSpaces) { } }
    public class ConvertSpacesToTabsCommand : EditorApplicationCommand { public ConvertSpacesToTabsCommand() : base(AvalonEditCommands.ConvertSpacesToTabs) { } }
    public class ConvertLeadingTabsToSpacesCommand : EditorApplicationCommand { public ConvertLeadingTabsToSpacesCommand() : base(AvalonEditCommands.ConvertLeadingTabsToSpaces) { } }
    public class ConvertLeadingSpacesToTabsCommand : EditorApplicationCommand { public ConvertLeadingSpacesToTabsCommand() : base(AvalonEditCommands.ConvertLeadingSpacesToTabs) { } }
    public class IndentSelectionCommand : EditorApplicationCommand { public IndentSelectionCommand() : base(AvalonEditCommands.IndentSelection) { } }

    public class SelectLineCommand : EditorCommand {
        public override Executability CanExecute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override Task Execute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            int lineNumber = editor.TextArea.Caret.Line;
            int totalLines = document.LineCount;
            DocumentLine currentLine = document.GetLineByNumber(lineNumber);
            int nextLineOffset = (lineNumber + 1) > totalLines ? currentLine.EndOffset : document.GetLineByNumber(lineNumber + 1).Offset;

            int startOffset = currentLine.Offset;
            int endOffset = nextLineOffset;

            editor.Select(startOffset, endOffset - startOffset);
            return Task.CompletedTask;
        }
    }

    public class DeleteTextOrLineCommand : EditorCommand {
        public override Executability CanExecute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override Task Execute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            Selection selection = editor.TextArea.Selection;
            if (selection.Length > 0) {
                editor.Delete();
            }
            else {
                int totalLines = document.LineCount;
                int caretLine = editor.TextArea.Caret.Line;
                if (caretLine == totalLines) {
                    if (caretLine == 1) {
                        editor.SelectAll();
                        editor.Delete();
                    }
                    else {
                        DocumentLine prevLine = document.GetLineByNumber(caretLine - 1);
                        DocumentLine currLine = document.GetLineByNumber(caretLine);
                        int prevLineOffset = prevLine.EndOffset;
                        document.Remove(prevLineOffset, currLine.EndOffset - prevLineOffset);
                    }
                }
                else {
                    DocumentLine currLine = document.GetLineByNumber(caretLine);
                    DocumentLine nextLine = document.GetLineByNumber(caretLine + 1);
                    int currLineOffset = currLine.Offset;
                    document.Remove(currLineOffset, nextLine.Offset - currLineOffset);
                }
            }

            return Task.CompletedTask;
        }
    }

    public class DuplicateTextOrLineCommand : EditorCommand {
        public override Executability CanExecute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override Task Execute(TextEditor editor, TextDocument document, CommandEventArgs e) {
            Selection selection = editor.TextArea.Selection;
            if (selection.Length > 0) {
                editor.Document.Insert(document.GetOffset(selection.EndPosition.Location), selection.GetText());
            }
            else {
                DocumentLine currentLine = document.GetLineByNumber(editor.TextArea.Caret.Line);
                int lineBegin = currentLine.Offset;
                string text = document.GetText(lineBegin, currentLine.Length);
                document.Insert(lineBegin + currentLine.Length, "\n" + text);
            }

            return Task.CompletedTask;
        }
    }
}