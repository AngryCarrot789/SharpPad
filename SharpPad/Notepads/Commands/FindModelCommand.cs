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

using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;
using SharpPad.Notepads.Controls;

namespace SharpPad.Notepads.Commands {
    public abstract class FindModelCommand : Command {
        public sealed override Executability CanExecute(CommandEventArgs e) {
            if (!DataKeys.FindModelKey.TryGetContext(e.ContextData, out FindAndReplaceModel model))
                return Executability.Invalid;
            return this.CanExecuteCore(model, e);
        }

        protected sealed override void Execute(CommandEventArgs e) {
            if (!DataKeys.FindModelKey.TryGetContext(e.ContextData, out FindAndReplaceModel model))
                return;
            this.ExecuteCore(model, e);
        }

        public virtual Executability CanExecuteCore(FindAndReplaceModel model, CommandEventArgs e) => Executability.Valid;

        protected abstract void ExecuteCore(FindAndReplaceModel model, CommandEventArgs e);
    }

    public class ShowFindCommand : Command {
        public override Executability CanExecute(CommandEventArgs e) {
            return e.ContextData.ContainsKey(DataKeys.NotepadEditorKey) ? Executability.Valid : Executability.Invalid;
        }

        protected override void Execute(CommandEventArgs e) {
            if (DataKeys.NotepadEditorKey.TryGetContext(e.ContextData, out NotepadEditor editor)) {
                editor.IsFindPanelOpen = true;
                if (DataKeys.UINotepadEditorKey.TryGetContext(e.ContextData, out INotepadEditorUI ui)) {
                    ui.FocusFindSearchBox();
                }
            }
        }
    }

    public class HideFindCommand : Command {
        public override Executability CanExecute(CommandEventArgs e) {
            return e.ContextData.ContainsKey(DataKeys.NotepadEditorKey) ? Executability.Valid : Executability.Invalid;
        }

        protected override void Execute(CommandEventArgs e) {
            if (DataKeys.NotepadEditorKey.TryGetContext(e.ContextData, out NotepadEditor editor)) {
                editor.IsFindPanelOpen = false;
            }
        }
    }

    public class ToggleMatchCaseCommand : FindModelCommand {
        protected override void ExecuteCore(FindAndReplaceModel model, CommandEventArgs e) {
            model.IsMatchCases = !model.IsMatchCases;
        }
    }

    public class ToggleWordSearchCommand : FindModelCommand {
        public override Executability CanExecuteCore(FindAndReplaceModel model, CommandEventArgs e) {
            return model.IsRegexSearch ? Executability.ValidButCannotExecute : Executability.Valid;
        }

        protected override void ExecuteCore(FindAndReplaceModel model, CommandEventArgs e) {
            if (!model.IsRegexSearch)
                model.IsWordSearch = !model.IsWordSearch;
        }
    }

    public class ToggleRegexSearchCommand : FindModelCommand {
        protected override void ExecuteCore(FindAndReplaceModel model, CommandEventArgs e) {
            model.IsRegexSearch = !model.IsRegexSearch;
        }
    }

    public class NextResultCommand : FindModelCommand {
        protected override void ExecuteCore(FindAndReplaceModel model, CommandEventArgs e) {
            model.MoveToNextResult();
        }
    }

    public class PrevResultCommand : FindModelCommand {
        protected override void ExecuteCore(FindAndReplaceModel model, CommandEventArgs e) {
            model.MoveToPrevResult();
        }
    }
}