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

namespace SharpPad.WPF.Themes.Attached
{
    public static class MenuHelper
    {
        public static readonly DependencyProperty UseStretchedContentProperty = DependencyProperty.RegisterAttached("UseStretchedContent", typeof(bool), typeof(MenuHelper), new PropertyMetadata(false));

        public static void SetUseStretchedContent(DependencyObject element, bool value)
        {
            element.SetValue(UseStretchedContentProperty, value);
        }

        public static bool GetUseStretchedContent(DependencyObject element)
        {
            return (bool) element.GetValue(UseStretchedContentProperty);
        }
    }
}