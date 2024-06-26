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

using System.Windows.Input;

namespace SharpPad.WPF.Utils
{
    public static class KeyboardUtils
    {
        public static bool AreAnyModifiersPressed(ModifierKeys key1)
        {
            return (Keyboard.Modifiers & key1) != 0;
        }

        public static bool AreAnyModifiersPressed(ModifierKeys key1, ModifierKeys key2)
        {
            return AreAnyModifiersPressed(key1 | key2);
        }

        public static bool AreAnyModifiersPressed(ModifierKeys key1, ModifierKeys key2, ModifierKeys key3)
        {
            return AreAnyModifiersPressed(key1 | key2 | key3);
        }

        public static bool AreAnyModifiersPressed(ModifierKeys key1, ModifierKeys key2, ModifierKeys key3, ModifierKeys key4)
        {
            return AreAnyModifiersPressed(key1 | key2 | key3 | key4);
        }

        public static bool AreAnyModifiersPressed(params ModifierKeys[] keys)
        {
            ModifierKeys modifiers = ModifierKeys.None;
            foreach (ModifierKeys modifier in keys)
                modifiers |= modifier;
            return AreAnyModifiersPressed(modifiers);
        }

        public static bool AreModifiersPressed(ModifierKeys key1)
        {
            return (Keyboard.Modifiers & key1) == key1;
        }

        public static bool AreModifiersPressed(ModifierKeys key1, ModifierKeys key2)
        {
            return AreModifiersPressed(key1 | key2);
        }

        public static bool AreModifiersPressed(ModifierKeys key1, ModifierKeys key2, ModifierKeys key3)
        {
            return AreModifiersPressed(key1 | key2 | key3);
        }

        public static bool AreModifiersPressed(ModifierKeys key1, ModifierKeys key2, ModifierKeys key3, ModifierKeys key4)
        {
            return AreModifiersPressed(key1 | key2 | key3 | key4);
        }

        public static bool AreModifiersPressed(params ModifierKeys[] keys)
        {
            ModifierKeys modifiers = ModifierKeys.None;
            foreach (ModifierKeys modifier in keys)
                modifiers |= modifier;
            return AreModifiersPressed(modifiers);
        }
    }
}