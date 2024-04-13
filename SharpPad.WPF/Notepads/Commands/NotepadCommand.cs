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
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using SharpPad.WPF.CommandSystem;
using SharpPad.WPF.Interactivity.Contexts;

namespace SharpPad.WPF.Notepads.Commands
{
    public abstract class NotepadCommand : Command
    {
        public override Executability CanExecute(CommandEventArgs e)
        {
            if (!DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                return Executability.Invalid;
            return this.CanExecute(notepad, e);
        }

        protected override void Execute(CommandEventArgs e)
        {
            if (DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                this.Execute(notepad, e);
        }

        public abstract Executability CanExecute(Notepad notepad, CommandEventArgs e);

        public abstract void Execute(Notepad notepad, CommandEventArgs e);
    }
}