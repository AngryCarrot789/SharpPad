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

using System.Collections.Generic;
using SharpPad.AdvancedMenuService.ContextService;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Contexts {
    public class NotepadTabContextGenerator : IContextGenerator {
        public static NotepadTabContextGenerator Instance { get; } = new NotepadTabContextGenerator();

        public void Generate(List<IContextEntry> list, IContextData context) {
            if (!context.ContainsAll(DataKeys.DocumentKey, DataKeys.NotepadKey))
                return;

            list.Add(new CommandContextEntry("SaveDocumentFile", "Save"));
            list.Add(new CommandContextEntry("SaveDocumentFileAs", "Save As..."));
            list.Add(new SeparatorEntry());
            list.Add(new CommandContextEntry("CloseDocument", "Close"));
        }
    }
}