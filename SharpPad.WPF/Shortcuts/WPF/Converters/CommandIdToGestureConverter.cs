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
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SharpPad.WPF.CommandSystem;
using SharpPad.WPF.Shortcuts.Managing;

namespace SharpPad.WPF.Shortcuts.WPF.Converters
{
    public class CommandIdToGestureConverter : IValueConverter
    {
        public static CommandIdToGestureConverter Instance { get; } = new CommandIdToGestureConverter();

        public string NoSuchActionText { get; set; } = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string id)
            {
                return CommandIdToGesture(id, this.NoSuchActionText, out string gesture) ? gesture : DependencyProperty.UnsetValue;
            }

            throw new Exception("Value is not a string");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static bool CommandIdToGesture(string id, string fallback, out string gesture, CmdToShortcutFlags flags = CmdToShortcutFlags.Both)
        {
            if (CommandManager.Instance.GetCommandById(id) == null)
            {
                return (gesture = fallback) != null;
            }

            IEnumerable<GroupedShortcut> shortcuts = ShortcutManager.Instance.GetShortcutsByCommandId(id, flags);
            if (shortcuts == null)
            {
                return (gesture = fallback) != null;
            }

            return (gesture = ShortcutIdToGestureConverter.ShortcutsToGesture(shortcuts, fallback)) != null;
        }

        public static bool CommandIdToGesture(string id, string fallback, out string gesture, string focusPath, bool canInherit = true)
        {
            if (CommandManager.Instance.GetCommandById(id) == null)
            {
                return (gesture = fallback) != null;
            }

            IEnumerable<GroupedShortcut> shortcuts = ShortcutManager.Instance.GetShortcutsByCommandId(id, focusPath, canInherit);
            if (shortcuts == null)
            {
                return (gesture = fallback) != null;
            }

            return (gesture = ShortcutIdToGestureConverter.ShortcutsToGesture(shortcuts, fallback)) != null;
        }
    }
}