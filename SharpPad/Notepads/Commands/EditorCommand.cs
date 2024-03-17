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
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Commands {
    public abstract class EditorCommand : Command {
        protected virtual bool CanHaveNullTextEditor => false;

        public sealed override Executability CanExecute(CommandEventArgs e) {
            if (!DataKeys.NotepadEditorKey.TryGetContext(e.ContextData, out NotepadEditor editor))
                return Executability.Invalid;

            if (editor.Document == null || (!this.CanHaveNullTextEditor && editor.TextEditor == null))
                return Executability.ValidButCannotExecute;

            return editor.Document == null ? Executability.Invalid : this.CanExecute(editor, editor.TextEditor, editor.Document.Document, e);
        }

        protected sealed override void Execute(CommandEventArgs e) {
            if (!DataKeys.NotepadEditorKey.TryGetContext(e.ContextData, out NotepadEditor editor))
                return;

            if (editor.Document == null || (!this.CanHaveNullTextEditor && editor.TextEditor == null))
                return;

            this.Execute(editor, editor.TextEditor, editor.Document.Document, e);
        }

        public abstract Executability CanExecute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e);

        public abstract void Execute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e);
    }

    public class RoutedEditorCommand : EditorCommand {
        public RoutedCommand Command { get; }

        public RoutedEditorCommand(RoutedCommand command) {
            this.Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public override Executability CanExecute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            return this.Command.CanExecute(null, textEditor.TextArea) ? Executability.Valid : Executability.ValidButCannotExecute;
        }

        public override void Execute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) => this.Command.Execute(null, textEditor.TextArea);
    }

    public class CutCommand : RoutedEditorCommand { public CutCommand() : base(ApplicationCommands.Cut) { } }

    public class CopyCommand : RoutedEditorCommand { public CopyCommand() : base(ApplicationCommands.Copy) { } }

    public class PasteCommand : RoutedEditorCommand { public PasteCommand() : base(ApplicationCommands.Paste) { } }

    public class UndoCommand : RoutedEditorCommand { public UndoCommand() : base(ApplicationCommands.Undo) { } }

    public class RedoCommand : RoutedEditorCommand { public RedoCommand() : base(ApplicationCommands.Redo) { } }

    public class SelectAllCommand : RoutedEditorCommand { public SelectAllCommand() : base(ApplicationCommands.SelectAll) { } }

    public class ToggleOverstrikeCommand : RoutedEditorCommand { public ToggleOverstrikeCommand() : base(AvalonEditCommands.ToggleOverstrike) { } }

    public class DeleteLineCommand : RoutedEditorCommand { public DeleteLineCommand() : base(AvalonEditCommands.DeleteLine) { } }

    public class RemoveLeadingWhitespaceCommand : RoutedEditorCommand { public RemoveLeadingWhitespaceCommand() : base(AvalonEditCommands.RemoveLeadingWhitespace) { } }

    public class RemoveTrailingWhitespaceCommand : RoutedEditorCommand { public RemoveTrailingWhitespaceCommand() : base(AvalonEditCommands.RemoveTrailingWhitespace) { } }

    public class ConvertToUppercaseCommand : RoutedEditorCommand { public ConvertToUppercaseCommand() : base(AvalonEditCommands.ConvertToUppercase) { } }

    public class ConvertToLowercaseCommand : RoutedEditorCommand { public ConvertToLowercaseCommand() : base(AvalonEditCommands.ConvertToLowercase) { } }

    public class ConvertToTitleCaseCommand : RoutedEditorCommand { public ConvertToTitleCaseCommand() : base(AvalonEditCommands.ConvertToTitleCase) { } }

    public class InvertCaseCommand : RoutedEditorCommand { public InvertCaseCommand() : base(AvalonEditCommands.InvertCase) { } }

    public class ConvertTabsToSpacesCommand : RoutedEditorCommand { public ConvertTabsToSpacesCommand() : base(AvalonEditCommands.ConvertTabsToSpaces) { } }

    public class ConvertSpacesToTabsCommand : RoutedEditorCommand { public ConvertSpacesToTabsCommand() : base(AvalonEditCommands.ConvertSpacesToTabs) { } }

    public class ConvertLeadingTabsToSpacesCommand : RoutedEditorCommand { public ConvertLeadingTabsToSpacesCommand() : base(AvalonEditCommands.ConvertLeadingTabsToSpaces) { } }

    public class ConvertLeadingSpacesToTabsCommand : RoutedEditorCommand { public ConvertLeadingSpacesToTabsCommand() : base(AvalonEditCommands.ConvertLeadingSpacesToTabs) { } }

    public class IndentSelectionCommand : RoutedEditorCommand { public IndentSelectionCommand() : base(AvalonEditCommands.IndentSelection) { } }

    public class SelectLineCommand : EditorCommand {
        public override Executability CanExecute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override void Execute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            int lineNumber = textEditor.TextArea.Caret.Line;
            int totalLines = document.LineCount;
            DocumentLine currentLine = document.GetLineByNumber(lineNumber);
            int nextLineOffset = (lineNumber + 1) > totalLines ? currentLine.EndOffset : document.GetLineByNumber(lineNumber + 1).Offset;

            int startOffset = currentLine.Offset;
            int endOffset = nextLineOffset;

            textEditor.Select(startOffset, endOffset - startOffset);
        }
    }

    public class DeleteTextOrLineCommand : EditorCommand {
        public override Executability CanExecute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override void Execute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            Selection selection = textEditor.TextArea.Selection;
            if (selection.Length > 0) {
                textEditor.Delete();
            }
            else {
                int totalLines = document.LineCount;
                int caretLine = textEditor.TextArea.Caret.Line;
                if (caretLine == totalLines) {
                    if (caretLine == 1) {
                        textEditor.SelectAll();
                        textEditor.Delete();
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
        }
    }

    public class DuplicateTextOrLineCommand : EditorCommand {
        public override Executability CanExecute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            return Executability.Valid;
        }

        public override void Execute(NotepadEditor editor, TextEditor textEditor, TextDocument document, CommandEventArgs e) {
            Selection selection = textEditor.TextArea.Selection;
            if (selection.Length > 0) {
                document.Insert(document.GetOffset(selection.EndPosition.Location), selection.GetText());
            }
            else {
                DocumentLine currentLine = document.GetLineByNumber(textEditor.TextArea.Caret.Line);
                int lineBegin = currentLine.Offset;
                string text = document.GetText(lineBegin, currentLine.Length);
                document.Insert(lineBegin + currentLine.Length, "\n" + text);
            }
        }
    }
}