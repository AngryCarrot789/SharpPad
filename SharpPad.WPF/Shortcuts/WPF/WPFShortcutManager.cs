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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SharpPad.WPF.Notepads.Views;
using SharpPad.WPF.Shortcuts.Inputs;
using SharpPad.WPF.Shortcuts.Keymapping;
using SharpPad.WPF.Shortcuts.Managing;
using SharpPad.WPF.Shortcuts.Usage;
using SharpPad.WPF.Utils;

namespace SharpPad.WPF.Shortcuts.WPF
{
    public class WPFShortcutManager : ShortcutManager
    {
        public const int BUTTON_WHEEL_UP = 143; // Away from the user
        public const int BUTTON_WHEEL_DOWN = 142; // Towards the user
        public const string DEFAULT_USAGE_ID = "DEF";

        public static WPFShortcutManager WPFInstance => (WPFShortcutManager) Instance ?? throw new Exception("No WPF shortcut manager available");

        static WPFShortcutManager()
        {
            KeyStroke.KeyCodeToStringProvider = (x) => ((Key) x).ToString();
            KeyStroke.ModifierToStringProvider = (x, s) =>
            {
                StringJoiner joiner = new StringJoiner(s ? " + " : "+");
                ModifierKeys keys = (ModifierKeys) x;
                if ((keys & ModifierKeys.Control) != 0)
                    joiner.Append("Ctrl");
                if ((keys & ModifierKeys.Alt) != 0)
                    joiner.Append("Alt");
                if ((keys & ModifierKeys.Shift) != 0)
                    joiner.Append("Shift");
                if ((keys & ModifierKeys.Windows) != 0)
                    joiner.Append("Win");
                return joiner.ToString();
            };

            MouseStroke.MouseButtonToStringProvider = (x) =>
            {
                switch (x)
                {
                    case 0: return "LMB";
                    case 1: return "MMB";
                    case 2: return "RMB";
                    case 3: return "X1";
                    case 4: return "X2";
                    case BUTTON_WHEEL_UP: return "WHEEL_UP";
                    case BUTTON_WHEEL_DOWN: return "WHEEL_DOWN";
                    default: return $"Unknown Button ({x})";
                }
            };
        }

        public WPFShortcutManager() { }

        public override ShortcutInputManager NewProcessor() => new WPFShortcutInputManager(this);

        public void DeserialiseRoot(Stream stream)
        {
            this.InvalidateShortcutCache();
            Keymap map = WPFKeyMapSerialiser.Instance.Deserialise(this, stream);
            this.Root = map.Root; // invalidates cache automatically
            try
            {
                this.EnsureCacheBuilt(); // do keymap check; crash on errors (e.g. duplicate shortcut path)
            }
            catch (Exception e)
            {
                this.InvalidateShortcutCache();
                this.Root = ShortcutGroup.CreateRoot(this);
                throw new Exception("Failed to process keymap and built caches", e);
            }
        }

        protected internal override void OnSecondShortcutUsagesProgressed(ShortcutInputManager inputManager)
        {
            base.OnSecondShortcutUsagesProgressed(inputManager);
            StringJoiner joiner = new StringJoiner(", ");
            foreach (KeyValuePair<IShortcutUsage, GroupedShortcut> pair in inputManager.ActiveUsages)
            {
                joiner.Append(pair.Key.CurrentStroke.ToString());
            }

            BroadcastShortcutActivity("Waiting for next input: " + joiner);
        }

        protected internal override void OnShortcutUsagesCreated(ShortcutInputManager inputManager)
        {
            base.OnShortcutUsagesCreated(inputManager);
            StringJoiner joiner = new StringJoiner(", ");
            foreach (KeyValuePair<IShortcutUsage, GroupedShortcut> pair in inputManager.ActiveUsages)
            {
                joiner.Append(pair.Key.CurrentStroke.ToString());
            }

            BroadcastShortcutActivity("Waiting for next input: " + joiner);
        }

        protected internal override void OnCancelUsageForNoSuchNextMouseStroke(ShortcutInputManager inputManager, IShortcutUsage usage, GroupedShortcut shortcut, MouseStroke stroke)
        {
            base.OnCancelUsageForNoSuchNextMouseStroke(inputManager, usage, shortcut, stroke);
            BroadcastShortcutActivity("No such shortcut for next mouse stroke: " + stroke);
        }

        protected internal override void OnCancelUsageForNoSuchNextKeyStroke(ShortcutInputManager inputManager, IShortcutUsage usage, GroupedShortcut shortcut, KeyStroke stroke)
        {
            base.OnCancelUsageForNoSuchNextKeyStroke(inputManager, usage, shortcut, stroke);
            BroadcastShortcutActivity("No such shortcut for next key stroke: " + stroke);
        }

        protected internal override void OnNoSuchShortcutForMouseStroke(ShortcutInputManager inputManager, string group, MouseStroke stroke)
        {
            base.OnNoSuchShortcutForMouseStroke(inputManager, group, stroke);
            if (Debugger.IsAttached)
            {
                BroadcastShortcutActivity("No such shortcut for mouse stroke: " + stroke + " in group: " + group);
            }
        }

        protected internal override void OnNoSuchShortcutForKeyStroke(ShortcutInputManager inputManager, string group, KeyStroke stroke)
        {
            base.OnNoSuchShortcutForKeyStroke(inputManager, group, stroke);
            if (stroke.IsKeyDown && Debugger.IsAttached)
            {
                BroadcastShortcutActivity("No such shortcut for key stroke: " + stroke + " in group: " + group);
            }
        }

        protected override bool OnShortcutActivatedOverride(ShortcutInputManager inputManager, GroupedShortcut shortcut)
        {
            string tostr;

            if (Debugger.IsAttached)
            {
                tostr = $"shortcut command: {shortcut} -> {(string.IsNullOrWhiteSpace(shortcut.CommandId) ? "<none>" : shortcut.CommandId)}";
            }
            else
            {
                tostr = $"shortcut command: {(string.IsNullOrWhiteSpace(shortcut.CommandId) ? "<none>" : shortcut.CommandId)}";
            }

            BroadcastShortcutActivity($"Activating {tostr}...");
            bool result = Application.Current.Dispatcher.Invoke(() => base.OnShortcutActivatedOverride(inputManager, shortcut), DispatcherPriority.Render);
            BroadcastShortcutActivity($"Activated {tostr}!");
            return result;
        }

        private static void BroadcastShortcutActivity(string msg)
        {
            // lazy. This should be done via event handling
            if (Application.Current.MainWindow is NotepadWindow window && window.ActivityTextBlock != null)
            {
                window.ActivityTextBlock.Text = msg;
            }
        }
    }
}