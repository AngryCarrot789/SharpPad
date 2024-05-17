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

using System.Collections.Generic;

namespace SharpPad.Avalonia.Interactivity.Contexts;

/// <summary>
/// Data context that scans a visual tree for an entry
/// </summary>
public class ScanningContextData : IContextData
{
    public IEnumerable<KeyValuePair<string, object>> Entries { get; }

    public bool TryGetContext(string key, out object value)
    {
        throw new System.NotImplementedException();
    }

    public bool ContainsKey(string key) => this.TryGetContext(key, out _);
}