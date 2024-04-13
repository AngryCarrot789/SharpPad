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
using System.Windows.Input;
using SharpPad.WPF.Interactivity.Contexts;
using SharpPad.WPF.Utils;
using CommandManager = SharpPad.WPF.CommandSystem.CommandManager;

namespace SharpPad.WPF.Behaviours.Standard
{
    /// <summary>
    /// A behaviour that allows a command to be executed when a specific key is pressed
    /// </summary>
    public class KeyStrokeCommandBehaviour : Behaviour<UIElement>
    {
        public static readonly DependencyProperty CommandIdProperty = DependencyProperty.Register("CommandId", typeof(string), typeof(KeyStrokeCommandBehaviour), new PropertyMetadata(null));
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register("Key", typeof(Key), typeof(KeyStrokeCommandBehaviour), new PropertyMetadata(Key.None));
        public static readonly DependencyProperty ModifierKeysProperty = DependencyProperty.Register("ModifierKeys", typeof(ModifierKeys), typeof(KeyStrokeCommandBehaviour), new PropertyMetadata(ModifierKeys.None));

        public string CommandId
        {
            get => (string) this.GetValue(CommandIdProperty);
            set => this.SetValue(CommandIdProperty, value);
        }

        public Key Key
        {
            get => (Key) this.GetValue(KeyProperty);
            set => this.SetValue(KeyProperty, value);
        }

        public ModifierKeys ModifierKeys
        {
            get => (ModifierKeys) this.GetValue(ModifierKeysProperty);
            set => this.SetValue(ModifierKeysProperty, value);
        }

        public KeyStrokeCommandBehaviour()
        {
        }

        protected override void OnAttached()
        {
            this.AttachedElement.KeyDown += this.OnKeyDown;
        }

        protected override void OnDetatched()
        {
            this.AttachedElement.KeyDown -= this.OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string cmdId = this.CommandId;
            if (string.IsNullOrWhiteSpace(cmdId) || e.Key != this.Key || !KeyboardUtils.AreModifiersPressed(this.ModifierKeys))
            {
                return;
            }

            CommandSystem.CommandManager.Instance.TryExecute(cmdId, () => DataManager.GetFullContextData(this.AttachedElement));
        }
    }
}