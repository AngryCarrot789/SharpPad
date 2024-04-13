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
using System.Windows.Controls;
using SharpPad.WPF.CommandSystem;
using SharpPad.WPF.Shortcuts.WPF.Converters;

namespace SharpPad.WPF.Shortcuts.WPF
{
    public static class ShortcutTooltipService
    {
        public static readonly DependencyProperty CommandIdProperty = DependencyProperty.RegisterAttached("CommandId", typeof(string), typeof(ShortcutTooltipService), new PropertyMetadata(null, OnCommandIdChanged));

        private static readonly DependencyPropertyKey ReadableShortcutStringPropertyKey = DependencyProperty.RegisterAttachedReadOnly("ReadableShortcutString", typeof(string), typeof(ShortcutTooltipService), new PropertyMetadata(null));
        public static readonly DependencyProperty ReadableShortcutStringProperty = ReadableShortcutStringPropertyKey.DependencyProperty;

        public static void SetCommandId(DependencyObject element, string value)
        {
            element.SetValue(CommandIdProperty, value);
        }

        public static string GetCommandId(DependencyObject element)
        {
            return (string) element.GetValue(CommandIdProperty);
        }

        public static string GetReadableShortcutString(DependencyObject element)
        {
            return (string) element.GetValue(ReadableShortcutStringProperty);
        }

        private static void OnCommandIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is string oldCmdId && !string.IsNullOrWhiteSpace(oldCmdId))
            {
                d.ClearValue(ReadableShortcutStringPropertyKey);
                ToolTipService.SetToolTip(d, null);
            }

            if (e.NewValue is string newCmdId && !string.IsNullOrWhiteSpace(newCmdId))
            {
                Command cmd = CommandManager.Instance.GetCommandById(newCmdId);
                if (cmd != null)
                {
                    if (CommandIdToGestureConverter.CommandIdToGesture(newCmdId, null, out string value))
                    {
                        d.SetValue(ReadableShortcutStringPropertyKey, value);
                    }
                }
            }
        }
    }
}