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

using System;
using System.Collections.Generic;
using SharpPad.WPF.Shortcuts.Inputs;

namespace SharpPad.WPF.Shortcuts.Managing
{
    public readonly struct ShortcutCollectorArgs
    {
        public readonly IInputStroke stroke;
        public readonly List<GroupedShortcut> list;
        public readonly Predicate<GroupedShortcut> filter;

        public ShortcutCollectorArgs(IInputStroke stroke, List<GroupedShortcut> list, Predicate<GroupedShortcut> filter)
        {
            this.stroke = stroke;
            this.list = list;
            this.filter = filter;
        }
    }
}