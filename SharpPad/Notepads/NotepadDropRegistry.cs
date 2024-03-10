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
using System.Windows;
using SharpPad.Interactivity;
using SharpPad.Interactivity.Contexts;
using SharpPad.Notepads.Commands;

namespace SharpPad.Notepads
{
    public static class NotepadDropRegistry
    {
        public static DragDropRegistry<Notepad> DropRegistry { get; }

        static NotepadDropRegistry()
        {
            DropRegistry = new DragDropRegistry<Notepad>();
            DropRegistry.RegisterNative<Notepad>(NativeDropTypes.FileDrop, CanDropFile, OnDropFile);
        }

        private static EnumDropType CanDropFile(Notepad notepad, IDataObjekt data, EnumDropType dropType, IContextData ctx)
        {
            return data.GetDataPresent(DataFormats.FileDrop) ? EnumDropType.Copy : EnumDropType.None;
        }

        private static Task OnDropFile(Notepad notepad, IDataObjekt data, EnumDropType dropType, IContextData ctx)
        {
            if (!(data.GetData(NativeDropTypes.FileDrop) is string[] files))
            {
                return Task.CompletedTask;
            }

            OpenFilesCommand.OpenFiles(notepad, files);
            return Task.CompletedTask;
        }
    }
}