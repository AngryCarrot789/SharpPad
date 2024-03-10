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

using System.Windows;
using ICSharpCode.AvalonEdit;
using SharpPad.Notepads;
using SharpPad.Notepads.Controls;

namespace SharpPad.Interactivity.Contexts
{
    public static class DataKeys
    {
        public static readonly DataKey<Window> HostWindowKey = DataKey<Window>.Create("HostWindow");
        public static readonly DataKey<Notepad> NotepadKey = DataKey<Notepad>.Create("Notepad");
        public static readonly DataKey<NotepadDocument> DocumentKey = DataKey<NotepadDocument>.Create("Document");
        public static readonly DataKey<TextEditor> TextEditorKey = DataKey<TextEditor>.Create("TextEditor");

        public static readonly DataKey<INotepadTabUI> UINotepadTabKey = DataKey<INotepadTabUI>.Create("UINotepadTab");
    }
}