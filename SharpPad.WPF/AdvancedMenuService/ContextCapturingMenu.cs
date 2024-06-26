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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SharpPad.WPF.Interactivity.Contexts;

namespace SharpPad.WPF.AdvancedMenuService
{
    /// <summary>
    /// A menu that captures the data context of the keyboard's focused element before this menu is focused
    /// (e.g. by showing a sub-menu item) allowing menu items to be treated like context menu items. Child menu
    /// items can access the <see cref="CapturedContextDataProperty"/> (which is inherited) to get the data
    /// </summary>
    public class ContextCapturingMenu : Menu
    {
        private const BindingFlags InternalInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly DependencyPropertyKey CapturedContextDataPropertyKey = DependencyProperty.RegisterAttachedReadOnly("CapturedContextData", typeof(IContextData), typeof(ContextCapturingMenu), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static readonly DependencyProperty CapturedContextDataProperty = CapturedContextDataPropertyKey.DependencyProperty;
        private static readonly EventInfo MenuModeChangedEventInfo = typeof(MenuBase).GetEvent("InternalMenuModeChanged", InternalInstanceFlags | BindingFlags.DeclaredOnly);
        private static readonly PropertyInfo IsMenuModePropertyInfo = typeof(MenuBase).GetProperty("IsMenuMode", InternalInstanceFlags | BindingFlags.DeclaredOnly);

        private bool IsMenuMode => (bool) IsMenuModePropertyInfo.GetValue(this, InternalInstanceFlags, null, null, CultureInfo.CurrentCulture);

        private bool canProcessFocusChange;

        public ContextCapturingMenu()
        {
            MenuModeChangedEventInfo.AddMethod.Invoke(this, InternalInstanceFlags, null, new object[] { new EventHandler(this.OnMenuModeChanged) }, CultureInfo.CurrentCulture);
        }

        // Called by UIInputManager
        internal static void OnKeyboardFocusChanged(object sender, KeyboardFocusChangedEventArgs e, ProcessInputEventArgs processInputArgs)
        {
            // This method is another workaround for the fact that all of the 'menu mode' based mechanisms are internal.
            // The KeyboardNavigation class is responsible for focusing a MenuItem (whose parent Menu's IsMainMenu property is true)
            // which means we need to capture the context, during that focus transition, in this method here
            if (e.RoutedEvent != Keyboard.GotKeyboardFocusEvent || !(e.NewFocus is MenuItem menuItem))
                return;

            if (!(e.OldFocus is DependencyObject oldFocus))
                return;

            DependencyObject parent = menuItem.Parent;
            if (parent is ContextCapturingMenu menu && menu.canProcessFocusChange)
            {
                menu.CaptureContextFromObject(oldFocus);
                menu.canProcessFocusChange = false;
            }
        }

        static ContextCapturingMenu() { }

        private void OnMenuModeChanged(object sender, EventArgs e)
        {
            // ContextMenu can capture the focused element in here since right clicking (well, opening the
            // context menu) doesn't seem to transfer focus from the originally focused element, however,
            // a Menu cannot be opened without focus (AFAIK)
            if (this.IsMenuMode)
            {
                this.canProcessFocusChange = true;
            }
            else
            {
                this.ClearValue(CapturedContextDataPropertyKey);
                DataManager.ClearContextData(this);
                Debug.WriteLine("Cleared captured data context");
                this.canProcessFocusChange = false;
            }
        }

        // Hopefully we can get away with not processing this and letting the menu mode logic work...
        // protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
        //     if (Keyboard.FocusedElement is DependencyObject focused && focused != this) {
        //         this.CaptureContextFromObject(focused);
        //         this.isMenuModeActivated = false;
        //     }
        //     else {
        //         this.ClearValue(CapturedContextDataProperty);
        //     }
        // }

        private void CaptureContextFromObject(DependencyObject focused)
        {
            IContextData ctx = DataManager.GetFullContextData(focused);
            Debug.WriteLine($"Captured context{(ctx is ContextData data ? $" ({data.Count} entries) " : " ")}before menu focus switch");
            this.SetValue(CapturedContextDataPropertyKey, ctx);
            DataManager.SetContextData(this, ctx);
        }

        public static IContextData GetCapturedContextData(DependencyObject element) => (IContextData) element.GetValue(CapturedContextDataProperty);
    }
}