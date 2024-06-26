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
using SharpPad.WPF.Utils;

namespace SharpPad.WPF.AdvancedMenuService.ContextService
{
    /// <summary>
    /// The class for command-based context entries. The header, tooltip, etc, are automatically fetched
    /// </summary>
    public class CommandContextEntry : BaseContextEntry
    {
        public string CommandId { get; }

        public CommandContextEntry(string commandId, string header, string description, IEnumerable<IContextEntry> children = null) : base(header, description, children)
        {
            this.CommandId = commandId;
        }

        public CommandContextEntry(string commandId, string header, IEnumerable<IContextEntry> children = null) : this(commandId, header, null, children) { }

        public CommandContextEntry(string commandId, IEnumerable<IContextEntry> children = null) : this(commandId, StringUtils.SplitLast(commandId, '.'), null, children) { }

        public CommandContextEntry(IEnumerable<IContextEntry> children = null) : this(null, null, null, children) { }
    }
}