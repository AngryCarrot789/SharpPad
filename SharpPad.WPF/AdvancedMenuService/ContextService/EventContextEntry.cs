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
using SharpPad.WPF.Interactivity.Contexts;

namespace SharpPad.WPF.AdvancedMenuService.ContextService
{
    /// <summary>
    /// The class for action-based context entries. The header, tooltip, etc, are automatically fetched
    /// </summary>
    public class EventContextEntry : BaseContextEntry
    {
        public Action<IContextData> Action { get; set; }

        public EventContextEntry(string header, string description = null) : base(header, description) { }

        public EventContextEntry(Action<IContextData> action, string header, string description = null) : base(header, description)
        {
            this.Action = action;
        }
    }
}