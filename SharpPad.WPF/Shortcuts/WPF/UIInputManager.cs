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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using SharpPad.WPF.AdvancedMenuService;
using SharpPad.WPF.Interactivity.Contexts;
using SharpPad.WPF.Utils;
using SharpPad.WPF.Utils.Visuals;

namespace SharpPad.WPF.Shortcuts.WPF
{
    /// <summary>
    /// A class which manages the WPF inputs and global focus scopes. This class also dispatches input events to the shortcut system
    /// </summary>
    public class UIInputManager : INotifyPropertyChanged
    {
        public delegate void FocusedPathChangedEventHandler(string oldPath, string newPath);

        /// <summary>
        /// A dependency property for a control's focus path. This is the full path, and
        /// should usually be set such that it is relative to a parent control's focus path
        /// </summary>
        public static readonly DependencyProperty FocusPathProperty = DependencyProperty.RegisterAttached("FocusPath", typeof(string), typeof(UIInputManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IsPathFocusedProperty = DependencyProperty.RegisterAttached("IsPathFocused", typeof(bool), typeof(UIInputManager), new PropertyMetadata(BoolBox.False));
        internal static readonly DependencyPropertyKey ShortcutProcessorPropertyKey = DependencyProperty.RegisterAttachedReadOnly("ShortcutProcessor", typeof(WPFShortcutInputManager), typeof(UIInputManager), new PropertyMetadata(default(WPFShortcutInputManager)));
        public static readonly DependencyProperty ShortcutProcessorProperty = ShortcutProcessorPropertyKey.DependencyProperty;
        public static readonly DependencyProperty UsePreviewEventsProperty = DependencyProperty.RegisterAttached("UsePreviewEvents", typeof(bool), typeof(UIInputManager), new PropertyMetadata(BoolBox.False));

        /// <summary>
        /// A dependency property which is used to tells the input system that shortcuts key strokes can be processed when the focused element is the base WPF text editor control
        /// </summary>
        public static readonly DependencyProperty IsInheritedFocusAllowedProperty = DependencyProperty.RegisterAttached("IsInheritedFocusAllowed", typeof(bool), typeof(UIInputManager), new PropertyMetadata(BoolBox.True));

        /// <summary>
        /// A dependency property used to check if a control blocks shortcut key processing. This is false by default for most
        /// standard controls, but is true for things like text boxes and rich text boxes (but can be set back to false explicitly)
        /// </summary>
        public static readonly DependencyProperty IsKeyShortcutProcessingBlockedProperty = DependencyProperty.RegisterAttached("IsKeyShortcutProcessingBlocked", typeof(bool), typeof(UIInputManager), new PropertyMetadata(BoolBox.False));

        /// <summary>
        /// A dependency property which is the same as <see cref="IsKeyShortcutProcessingBlockedProperty"/> except for mouse strokes
        /// </summary>
        public static readonly DependencyProperty IsMouseProcessingBlockedProperty = DependencyProperty.RegisterAttached("IsMouseProcessingBlocked", typeof(bool), typeof(UIInputManager), new PropertyMetadata(default(bool)));

        public static UIInputManager Instance { get; } = new UIInputManager();


        public static event FocusedPathChangedEventHandler OnFocusedPathChanged;

        public static WeakReference CurrentlyFocusedObject { get; } = new WeakReference(null);

        private string focusedPath;

        public string FocusedPath
        {
            get => this.focusedPath;
            private set
            {
                this.focusedPath = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.FocusedPath)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private UIInputManager()
        {
            if (Instance != null)
                throw new InvalidOperationException();
        }

        static UIInputManager()
        {
            // !!!
            Application.Current.Dispatcher.VerifyAccess();

            InputManager.Current.PreProcessInput += OnPreProcessInput;
            InputManager.Current.PostProcessInput += OnPostProcessInput;

            IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextBoxBase), new PropertyMetadata(BoolBox.True));
            IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextBox), new PropertyMetadata(BoolBox.True));
            IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(RichTextBox), new PropertyMetadata(BoolBox.True));
            IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextEditor), new PropertyMetadata(BoolBox.True));
        }

        /// <summary>
        /// Sets the element's focus path for the specific element, which is used to evaluate which shortcuts are visible to the element and its visual tree
        /// </summary>
        public static void SetFocusPath(DependencyObject element, string value) => element.SetValue(FocusPathProperty, value);

        /// <summary>
        /// Gets the element's focus path for the specific element, which is used to evaluate which shortcuts are visible to the element and its visual tree
        /// </summary>
        public static string GetFocusPath(DependencyObject element) => (string) element.GetValue(FocusPathProperty);

        /// <summary>
        /// Sets whether this element has group focus (will only be set)
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetIsPathFocused(DependencyObject element, bool value) => element.SetValue(IsPathFocusedProperty, value.Box());

        public static bool GetIsPathFocused(DependencyObject element) => (bool) element.GetValue(IsPathFocusedProperty);

        /// <summary>
        /// Sets whether the element should process inputs on the preview/tunnel event instead of the bubble event
        /// <para>
        /// This can be useful if a control handles the bubble event but not the preview event;
        /// setting this to true for that control will allow hotkeys to jump in and do their thing
        /// </para>
        /// </summary>
        /// <param name="element">The element to set the state of</param>
        /// <param name="value">True to process preview/tunnel events only, false to process bubble events only</param>
        public static void SetUsePreviewEvents(DependencyObject element, bool value) => element.SetValue(UsePreviewEventsProperty, value.Box());

        /// <summary>
        /// Gets whether the element should process inputs on the preview/tunnel event instead of the bubble event
        /// </summary>
        public static bool GetUsePreviewEvents(DependencyObject element) => (bool) element.GetValue(UsePreviewEventsProperty);

        public static bool GetIsInheritedFocusAllowed(DependencyObject element) => (bool) element.GetValue(IsInheritedFocusAllowedProperty);

        public static void SetIsKeyShortcutProcessingBlocked(DependencyObject element, bool value) => element.SetValue(IsKeyShortcutProcessingBlockedProperty, value.Box());

        public static bool GetIsKeyShortcutProcessingBlocked(DependencyObject element) => (bool) element.GetValue(IsKeyShortcutProcessingBlockedProperty);

        public static void SetIsMouseProcessingBlocked(DependencyObject element, bool value) => element.SetValue(IsMouseProcessingBlockedProperty, value);

        public static bool GetIsMouseProcessingBlocked(DependencyObject element) => (bool) element.GetValue(IsMouseProcessingBlockedProperty);

        public static void RaiseFocusGroupPathChanged(string oldGroup, string newGroup)
        {
            OnFocusedPathChanged?.Invoke(oldGroup, newGroup);
        }

        public static void ProcessFocusGroupChange(DependencyObject obj)
        {
            string oldPath = Instance.FocusedPath;
            string newPath = GetFocusPath(obj);
            if (oldPath != newPath)
            {
                Instance.FocusedPath = newPath;
                RaiseFocusGroupPathChanged(oldPath, newPath);
                UpdateFocusGroup(obj, newPath);
            }
        }

        /// <summary>
        /// Looks through the given dependency object's parent chain for an element that has the <see cref="FocusPathProperty"/> explicitly
        /// set, assuming that means it is a primary focus group, and then sets the <see cref="IsPathFocusedProperty"/> to true for
        /// that element, and false for the last element that was focused
        /// </summary>
        /// <param name="target">Target/focused element which now has focus</param>
        /// <param name="newPath"></param>
        public static void UpdateFocusGroup(DependencyObject target, string newPath)
        {
            object lastFocused = CurrentlyFocusedObject.Target;
            if (lastFocused != null)
            {
                CurrentlyFocusedObject.Target = null;
                SetIsPathFocused((DependencyObject) lastFocused, false);
            }

            if (string.IsNullOrEmpty(newPath))
                return;

            DependencyObject root = VisualTreeUtils.FindNearestInheritedPropertyDefinition(FocusPathProperty, target);
            // do {
            //     root = VisualTreeUtils.FindInheritedPropertyDefinition(FocusGroupPathProperty, root);
            // } while (root != null && !GetHasAdvancedFocusVisual(root) && (root = VisualTreeHelper.GetParent(root)) != null);

            if (root != null)
            {
                CurrentlyFocusedObject.Target = root;
                SetIsPathFocused(root, true);
                // if (root is UIElement element && element.Focusable && !element.IsFocused) {
                //     element.Focus();
                // }
            }
            else
            {
                Debug.WriteLine("Failed to find root control that owns the FocusPathProperty of '" + GetFocusPath(target) + "'");
            }
        }

        #region Input Event Handlers

        private static void OnPreProcessInput(object sender, PreProcessInputEventArgs args)
        {
            // Don't use args.Cancel(), because there is a problem with KeyEventArgs where WM_CHAR
            // gets sent to the window anyway. Handling the input event stops this happening.
            // Not sure if Cancel() is okay for mouse btn/wheel events
            switch (args.StagingItem.Input)
            {
                case KeyboardFocusChangedEventArgs e:
                    OnApplicationPreKeyboardPreFocusChanged(e, args);
                    break;
                case KeyEventArgs e:
                    if (OnApplicationKeyEvent(e, args))
                        e.Handled = true;
                    break;
                case MouseButtonEventArgs e:
                    if (OnApplicationMouseButtonEvent(e))
                        e.Handled = true;
                    break;
                case MouseWheelEventArgs e:
                    if (OnApplicationMouseWheelEvent(e))
                        e.Handled = true;
                    break;
            }
        }

        private static void OnPostProcessInput(object sender, ProcessInputEventArgs args)
        {
            if (args.StagingItem.Input is KeyboardFocusChangedEventArgs e)
            {
                ContextCapturingMenu.OnKeyboardFocusChanged(sender, e, args);
                CommandSystem.CommandManager.InternalOnApplicationFocusChanged(() =>
                {
                    if (Keyboard.FocusedElement is DependencyObject obj)
                        return DataManager.GetFullContextData(obj);
                    return EmptyContext.Instance;
                });
            }

            /*
             case TextCompositionEventArgs e:
                 if (!e.Handled && e.RoutedEvent == TextCompositionManager.TextInputEvent)
                     if (OnApplicationTextCompositionEvent(e, args))
                         e.Handled = true;
                 break;
             */
        }

        private static void OnApplicationPreKeyboardPreFocusChanged(KeyboardFocusChangedEventArgs e, PreProcessInputEventArgs args)
        {
            if (e.Device is KeyboardDevice keyboard && keyboard.Target is DependencyObject focused)
            {
                ProcessFocusGroupChange(focused);
            }
        }

        private static bool OnApplicationKeyEvent(KeyEventArgs e, PreProcessInputEventArgs inputArgs)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.DeadCharProcessed || key == Key.None)
            {
                return false;
            }

            if (ShortcutUtils.IsModifierKey(key) && e.IsRepeat)
            {
                return false;
            }

            if (!(e.InputSource.RootVisual is Window window))
            {
                return false;
            }

            WPFShortcutInputManager processor = (WPFShortcutInputManager) window.GetValue(ShortcutProcessorProperty);
            if (processor == null)
            {
                window.SetValue(ShortcutProcessorPropertyKey, processor = new WPFShortcutInputManager(WPFShortcutManager.WPFInstance));
            }
            else if (processor.isProcessingKey)
            {
                return false;
            }

            InputDevice recentInput = inputArgs.InputManager.MostRecentInputDevice;
            DependencyObject focusedObject = null;
            if (recentInput is KeyboardDevice keyboard)
            {
                if (keyboard.FocusedElement is DependencyObject obj && obj != window)
                {
                    focusedObject = obj;
                }
            }

            if (focusedObject == null)
            {
                if (recentInput is MouseDevice mouse)
                {
                    if (mouse.Target is DependencyObject obj && obj != window)
                    {
                        focusedObject = obj;
                    }
                }
                else
                {
                    mouse = inputArgs.InputManager.PrimaryMouseDevice;
                    if (mouse.Target is DependencyObject obj && obj != window)
                    {
                        focusedObject = obj;
                    }
                }
            }

            if (focusedObject != null)
            {
                bool isPreview = e.RoutedEvent == Keyboard.PreviewKeyDownEvent || e.RoutedEvent == Keyboard.PreviewKeyUpEvent;
                processor.OnInputSourceKeyEvent(processor, focusedObject, e, key, e.IsUp, isPreview);
                if (processor.isProcessingKey)
                    e.Handled = true;
                return e.Handled;
            }

            return false;
        }

        private static bool OnApplicationMouseButtonEvent(MouseButtonEventArgs e)
        {
            if (!(e.Device is MouseDevice mouse) || !(mouse.Target is DependencyObject target))
            {
                return false;
            }

            // not even sure why I added this line...
            if (!(Window.GetWindow(target) is Window window) || target == window)
            {
                return false;
            }

            bool isPreview, isDown;
            if (e.RoutedEvent == Mouse.PreviewMouseDownEvent)
            {
                isPreview = isDown = true;
            }
            else if (e.RoutedEvent == Mouse.PreviewMouseUpEvent)
            {
                isPreview = true;
                isDown = false;
            }
            else if (e.RoutedEvent == Mouse.MouseDownEvent)
            {
                isPreview = false;
                isDown = true;
            }
            else if (e.RoutedEvent == Mouse.MouseUpEvent)
            {
                isPreview = isDown = false;
            }
            else
            {
                return false;
            }

            if (isPreview)
            {
                ProcessFocusGroupChange(target);
            }

            if (!WPFShortcutInputManager.CanProcessEvent(target, isPreview) || GetIsMouseProcessingBlocked(target))
            {
                return false;
            }

            WPFShortcutInputManager processor = (WPFShortcutInputManager) window.GetValue(ShortcutProcessorProperty);
            if (processor == null)
            {
                window.SetValue(ShortcutProcessorPropertyKey, processor = new WPFShortcutInputManager(WPFShortcutManager.WPFInstance));
            }
            else if (processor.isProcessingMouse)
            {
                return false;
            }

            processor.OnInputSourceMouseButton(target, e, !isDown);
            if (processor.isProcessingMouse)
                e.Handled = true;

            return e.Handled;
        }

        private static bool OnApplicationMouseWheelEvent(MouseWheelEventArgs e)
        {
            if (e.Delta == 0 || !(e.Device is MouseDevice mouse) || !(mouse.Target is DependencyObject target))
            {
                return false;
            }

            // not even sure why I added this line...
            if (!(Window.GetWindow(target) is Window window) || target == window)
            {
                return false;
            }

            bool isPreview;
            if (e.RoutedEvent == Mouse.PreviewMouseWheelEvent)
            {
                isPreview = true;
                ProcessFocusGroupChange(target);
            }
            else if (e.RoutedEvent == Mouse.MouseWheelEvent)
            {
                isPreview = false;
            }
            else
            {
                return false;
            }

            if (!WPFShortcutInputManager.CanProcessEvent(target, isPreview) || GetIsMouseProcessingBlocked(target))
            {
                return false;
            }

            WPFShortcutInputManager processor = (WPFShortcutInputManager) window.GetValue(ShortcutProcessorProperty);
            if (processor == null)
            {
                window.SetValue(ShortcutProcessorPropertyKey, processor = new WPFShortcutInputManager(WPFShortcutManager.WPFInstance));
            }
            else if (processor.isProcessingMouse)
            {
                return false;
            }

            processor.OnInputSourceMouseWheel(target, e);
            if (processor.isProcessingMouse)
                e.Handled = true;
            return e.Handled;
        }

        #endregion
    }
}