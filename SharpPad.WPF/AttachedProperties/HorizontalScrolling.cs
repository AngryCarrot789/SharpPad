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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SharpPad.WPF.Utils;
using SharpPad.WPF.Utils.Visuals;

namespace SharpPad.WPF.AttachedProperties
{
    public static class HorizontalScrolling
    {
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        private static extern bool SystemParametersInfo(int nAction, int nParam, ref int value, int ignore);

        private static bool hasCachedScrollChars;
        private static int scrollChars;

        public static int ScrollChars
        {
            [SecurityCritical]
            get
            {
                if (hasCachedScrollChars)
                    return scrollChars;

                if (!SystemParametersInfo(108, 0, ref scrollChars, 0))
                    throw new Win32Exception();

                hasCachedScrollChars = true;
                return scrollChars;
            }
        }


        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached("UseHorizontalScrolling", typeof(bool), typeof(HorizontalScrolling), new PropertyMetadata(BoolBox.False, OnUseHorizontalScrollWheelPropertyChanged));
        public static readonly DependencyProperty IsRequireShiftForHorizontalScrollProperty = DependencyProperty.RegisterAttached("IsRequireShiftForHorizontalScroll", typeof(bool), typeof(HorizontalScrolling), new PropertyMetadata(BoolBox.True));
        public static readonly DependencyProperty ForceHorizontalScrollingProperty = DependencyProperty.RegisterAttached("ForceHorizontalScrolling", typeof(bool), typeof(HorizontalScrolling), new PropertyMetadata(BoolBox.False));
        public static readonly DependencyProperty HorizontalScrollingAmountProperty = DependencyProperty.RegisterAttached("HorizontalScrollingAmount", typeof(int), typeof(HorizontalScrolling), new PropertyMetadata(ScrollChars));

        public static void SetUseHorizontalScrolling(DependencyObject element, bool value) => element.SetValue(UseHorizontalScrollingProperty, value);
        public static bool GetUseHorizontalScrolling(DependencyObject element) => (bool) element.GetValue(UseHorizontalScrollingProperty);

        public static void SetIsRequireShiftForHorizontalScroll(DependencyObject element, bool value) => element.SetValue(IsRequireShiftForHorizontalScrollProperty, value);
        public static bool GetIsRequireShiftForHorizontalScroll(DependencyObject element) => (bool) element.GetValue(IsRequireShiftForHorizontalScrollProperty);

        public static bool GetForceHorizontalScrolling(DependencyObject d) => (bool) d.GetValue(ForceHorizontalScrollingProperty);
        public static void SetForceHorizontalScrolling(DependencyObject d, bool value) => d.SetValue(ForceHorizontalScrollingProperty, value);

        public static int GetHorizontalScrollingAmount(DependencyObject d) => (int) d.GetValue(HorizontalScrollingAmountProperty);
        public static void SetHorizontalScrollingAmount(DependencyObject d, int value) => d.SetValue(HorizontalScrollingAmountProperty, value);

        private static void OnUseHorizontalScrollWheelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
                if ((bool) e.NewValue)
                {
                    element.PreviewMouseWheel += OnPreviewMouseWheel;
                }
            }
            else
            {
                throw new Exception("Attached property must be used with UIElement");
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is UIElement element && e.Delta != 0)
            {
                ScrollViewer scroller = VisualTreeUtils.FindVisualChild<ScrollViewer>(element);
                if (scroller == null)
                {
                    return;
                }

                if (GetIsRequireShiftForHorizontalScroll(element) && scroller.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
                {
                    return;
                }

                int amount = GetHorizontalScrollingAmount(element);
                if (amount < 1)
                {
                    amount = 3;
                }

                if (Keyboard.Modifiers == ModifierKeys.Shift || Mouse.MiddleButton == MouseButtonState.Pressed || GetForceHorizontalScrolling(element))
                {
                    int count = (e.Delta / 120) * amount;
                    if (e.Delta < 0)
                    {
                        for (int i = -count; i > 0; i--)
                        {
                            scroller.LineRight();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            scroller.LineLeft();
                        }
                    }

                    e.Handled = true;
                }
            }
        }
    }
}