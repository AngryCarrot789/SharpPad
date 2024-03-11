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

using System.Collections.Generic;
using SharpPad.AdvancedMenuService.ContextService;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Contexts
{
    public class EditMenuContextGenerator : IContextGenerator
    {
        public static EditMenuContextGenerator Instance { get; } = new EditMenuContextGenerator();

        public void Generate(List<IContextEntry> list, IContextData context)
        {
            list.Add(new CommandContextEntry("UndoInEditor", "Undo"));
            list.Add(new CommandContextEntry("RedoInEditor", "Redo"));
            list.Add(SeparatorEntry.NewInstance);
            list.Add(new CommandContextEntry("CutInEditor", "Cut"));
            list.Add(new CommandContextEntry("CopyInEditor", "Copy"));
            list.Add(new CommandContextEntry("CopyFilePathInEditor", "Copy File Path"));
            list.Add(new CommandContextEntry("PasteInEditor", "Paste"));
            list.Add(new CommandContextEntry("DeleteTextOrLineInEditor", "Delete"));
            list.Add(new CommandContextEntry("DeleteLineInEditor", "Delete Line"));
            list.Add(SeparatorEntry.NewInstance);
            list.Add(new CommandContextEntry("SelectAllInEditor", "Select All"));
            list.Add(new CommandContextEntry("SelectLineInEditor", "Select Line"));
            list.Add(SeparatorEntry.NewInstance);
            list.Add(new CommandContextEntry("InvertCaseInEditor", "Invert Case"));
            list.Add(new CommandContextEntry("IndentSelectionInEditor", "Indent Selection"));
            list.Add(new CommandContextEntry("ConvertToUppercaseInEditor", "Convert To Upper case"));
            list.Add(new CommandContextEntry("ConvertToLowercaseInEditor", "Convert To Lower case"));
            list.Add(new CommandContextEntry("ConvertToTitleCaseInEditor", "Convert To Title Case"));
            list.Add(new CommandContextEntry("ConvertTabsToSpacesInEditor", "Convert Tabs To Spaces"));
            list.Add(new CommandContextEntry("ConvertSpacesToTabsInEditor", "Convert Spaces To Tabs"));
            list.Add(new CommandContextEntry("ConvertLeadingTabsToSpacesInEditor", "Convert Leading Tabs To Spaces"));
            list.Add(new CommandContextEntry("ConvertLeadingSpacesToTabsInEditor", "Convert Leading Spaces To Tabs"));
            list.Add(new CommandContextEntry("RemoveLeadingWhitespaceInEditor", "Remove Leading Whitespace"));
            list.Add(new CommandContextEntry("RemoveTrailingWhitespaceInEditor", "Remove Trailing Whitespace"));
        }
    }
}