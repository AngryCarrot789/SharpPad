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

namespace SharpPad.Avalonia.Notepads;

/// <summary>
/// An interface for a UI component that represents a tab in a notepad panel
/// </summary>
public interface INotepadTabUI
{
    /// <summary>
    /// Gets or sets if the "find-in-file" panel is visible
    /// </summary>
    bool IsFindPanelOpen { get; set; }

    /// <summary>
    /// Brings this tab into the UI view so that the user can see it
    /// </summary>
    void BringIntoView();
}