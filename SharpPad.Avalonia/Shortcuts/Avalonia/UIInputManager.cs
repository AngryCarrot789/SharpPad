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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using SharpPad.Avalonia.Shortcuts.Managing;
using SharpPad.Avalonia.Utils.Visuals;

namespace SharpPad.Avalonia.Shortcuts.Avalonia;

/// <summary>
/// A class which manages the WPF inputs and global focus scopes. This class also dispatches input events to the shortcut system
/// </summary>
public class UIInputManager
{
    public delegate void FocusedPathChangedEventHandler(string? oldPath, string? newPath);

    private static readonly AttachedProperty<AvaloniaShortcutInputProcessor?> ShortcutProcessorProperty = AvaloniaProperty.RegisterAttached<UIInputManager, TopLevel, AvaloniaShortcutInputProcessor?>("ShortcutProcessor");

    /// <summary>
    /// A dependency property for a control's focus path. This is the full path, and
    /// should usually be set such that it is relative to a parent control's focus path
    /// </summary>
    public static readonly AttachedProperty<string?> FocusPathProperty = AvaloniaProperty.RegisterAttached<UIInputManager, AvaloniaObject, string?>("FocusPath", inherits: true);

    /// <summary>
    /// A property used to determine if shortcut processing is disabled on an object.
    /// Default value is false, but you can override the metadata for specific controls
    /// and set the default value to true.
    /// <see cref="IsKeyShortcutProcessingUnblockedWithKeyModifiers"/> is checked when this
    /// property's value is true and the input keystroke has modifier keys 
    /// </summary>
    public static readonly AttachedProperty<bool> IsKeyShortcutProcessingBlockedProperty = AvaloniaProperty.RegisterAttached<UIInputManager, AvaloniaObject, bool>("IsKeyShortcutProcessingBlocked");

    public static readonly AttachedProperty<bool> IsKeyShortcutProcessingUnblockedWithKeyModifiersProperty = AvaloniaProperty.RegisterAttached<UIInputManager, AvaloniaObject, bool>("IsKeyShortcutProcessingUnBlockedWithKeyModifiers");

    // /// <summary>
    // /// A dependency property which is used to tells the input system that shortcuts key strokes can be processed when the focused element is the base WPF text editor control
    // /// </summary>
    // public static readonly AvaloniaProperty IsInheritedFocusAllowedProperty = DependencyProperty.RegisterAttached("IsInheritedFocusAllowed", typeof(bool), typeof(UIInputManager), new PropertyMetadata(BoolBox.True));
    //
    // /// <summary>
    // /// A dependency property used to check if a control blocks shortcut key processing. This is false by default for most
    // /// standard controls, but is true for things like text boxes and rich text boxes (but can be set back to false explicitly)
    // /// </summary>
    // public static readonly DependencyProperty IsKeyShortcutProcessingBlockedProperty = DependencyProperty.RegisterAttached("IsKeyShortcutProcessingBlocked", typeof(bool), typeof(UIInputManager), new PropertyMetadata(BoolBox.False));
    //
    // /// <summary>
    // /// A dependency property which is the same as <see cref="IsKeyShortcutProcessingBlockedProperty"/> except for mouse strokes
    // /// </summary>
    // public static readonly DependencyProperty IsMouseProcessingBlockedProperty = DependencyProperty.RegisterAttached("IsMouseProcessingBlocked", typeof(bool), typeof(UIInputManager), new PropertyMetadata(default(bool)));

    public static UIInputManager Instance { get; } = new UIInputManager();


    public static event FocusedPathChangedEventHandler OnFocusedPathChanged;

    public static WeakReference CurrentlyFocusedObject { get; } = new WeakReference(null);

    public string? FocusedPath { get; private set; }

    private UIInputManager()
    {
        if (Instance != null)
            throw new InvalidOperationException();
    }

    static UIInputManager()
    {
        ApplicationCore.Instance.CheckThreadAccess();
        InputElement.GotFocusEvent.AddClassHandler<TopLevel>((s, e) => OnFocusChanged(e.Source as InputElement, false), handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<TopLevel>((s, e) => OnFocusChanged(e.Source as InputElement, true), handledEventsToo: true);

        InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnTopLevelPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnTopLevelKeyDown, handledEventsToo: true);
        InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnTopLevelPreviewKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnTopLevelKeyUp, handledEventsToo: true);

        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnTopLevelPreviewPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnTopLevelPointerPressed, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(OnTopLevelPreviewPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(OnTopLevelPointerReleased, handledEventsToo: true);

        IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextBox), new StyledPropertyMetadata<bool>(true));
        IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextPresenter), new StyledPropertyMetadata<bool>(true));
        IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextEditor), new StyledPropertyMetadata<bool>(true));
        IsKeyShortcutProcessingBlockedProperty.OverrideMetadata(typeof(TextArea), new StyledPropertyMetadata<bool>(true));
    }

    public static void Init()
    {
        ShortcutManager.Instance = new AvaloniaShortcutManager();
    }

    private static void OnTopLevelPreviewPointerPressed(TopLevel sender, PointerPressedEventArgs e)
    {
        if (!(e.Source is InputElement element))
        {
            return;
        }
    }

    // Mouse Events

    private static void OnTopLevelPointerPressed(TopLevel sender, PointerPressedEventArgs e)
    {
    }

    private static void OnTopLevelPreviewPointerReleased(TopLevel sender, PointerReleasedEventArgs e)
    {
        // Example: A button click will be handled after this method returns
    }

    private static void OnTopLevelPointerReleased(TopLevel sender, PointerReleasedEventArgs e)
    {
        // Example: A button click will have been processed before this method
    }

    // Key Events

    private static void OnTopLevelPreviewKeyDown(TopLevel sender, KeyEventArgs e) => OnTopLevelKeyEvent(sender, e, false, true);

    private static void OnTopLevelKeyDown(TopLevel sender, KeyEventArgs e) => OnTopLevelKeyEvent(sender, e, false, false);

    private static void OnTopLevelPreviewKeyUp(TopLevel sender, KeyEventArgs e) => OnTopLevelKeyEvent(sender, e, true, true);

    private static void OnTopLevelKeyUp(TopLevel sender, KeyEventArgs e) => OnTopLevelKeyEvent(sender, e, true, false);

    private static void OnTopLevelKeyEvent(TopLevel sender, KeyEventArgs e, bool isRelease, bool isPreview)
    {
        // we only want to handle shortcuts in preview events, since we will
        // have the target element but its non-preview typical handler will
        // not have been delivered yet
        if (!isPreview)
            return;

        Key key = e.Key;
        if (key == Key.DeadCharProcessed || key == Key.System || key == Key.None)
            return;

        AvaloniaShortcutInputProcessor processor = GetShortcutProcessor(sender);
        if (processor.isProcessingKey)
            return;

        if (!(e.Source is InputElement element))
            return;

        processor.OnInputSourceKeyEvent(processor, element, e, key, isRelease);
        // If OnInputSourceKeyEvent becomes async for some reason,
        // e.Handled will be set to true in assumption that work is being done
        // if (processor.isProcessingKey)
        //     e.Handled = true;
    }

    private static AvaloniaShortcutInputProcessor GetShortcutProcessor(TopLevel topLevel)
    {
        AvaloniaShortcutInputProcessor? processor = topLevel.GetValue(ShortcutProcessorProperty);
        if (processor == null)
            topLevel.SetValue(ShortcutProcessorProperty, processor = new AvaloniaShortcutInputProcessor(AvaloniaShortcutManager.AvaloniaInstance));
        return processor;
    }

    public static void SetIsKeyShortcutProcessingBlocked(AvaloniaObject obj, bool value) => obj.SetValue(IsKeyShortcutProcessingBlockedProperty, value);
    public static bool GetIsKeyShortcutProcessingBlocked(AvaloniaObject obj) => obj.GetValue(IsKeyShortcutProcessingBlockedProperty);

    public static void SetIsKeyShortcutProcessingUnblockedWithKeyModifiers(AvaloniaObject obj, bool value) => obj.SetValue(IsKeyShortcutProcessingUnblockedWithKeyModifiersProperty, value);
    public static bool GetIsKeyShortcutProcessingUnblockedWithKeyModifiers(AvaloniaObject obj) => obj.GetValue(IsKeyShortcutProcessingUnblockedWithKeyModifiersProperty);

    #region Focus managing

    private static void OnFocusChanged(InputElement? element, bool lost)
    {
        if (element == null)
        {
            return;
        }

        string? oldPath = Instance.FocusedPath;
        string? newPath = lost ? null : GetFocusPath(element);
        if (oldPath != newPath)
        {
            Instance.FocusedPath = newPath;
            OnFocusedPathChanged?.Invoke(oldPath, newPath);
            UpdateFocusGroup(element, newPath);
        }
    }

    /// <summary>
    /// Sets the element's focus path for the specific element, which is used to evaluate which shortcuts are visible to the element and its visual tree
    /// </summary>
    public static void SetFocusPath(AvaloniaObject element, string value) => element.SetValue(FocusPathProperty, value);

    /// <summary>
    /// Gets the element's focus path for the specific element, which is used to evaluate which shortcuts are visible to the element and its visual tree
    /// </summary>
    public static string? GetFocusPath(AvaloniaObject element) => element.GetValue(FocusPathProperty);

    /// <summary>
    /// Looks through the given dependency object's parent chain for an element that has the <see cref="FocusPathProperty"/> explicitly
    /// set, assuming that means it is a primary focus group, and then sets the <see cref="IsPathFocusedProperty"/> to true for
    /// that element, and false for the last element that was focused
    /// </summary>
    /// <param name="target">Target/focused element which now has focus</param>
    /// <param name="newPath"></param>
    public static void UpdateFocusGroup(AvaloniaObject target, string? newPath)
    {
        object? lastFocused = CurrentlyFocusedObject.Target;
        if (lastFocused != null)
        {
            CurrentlyFocusedObject.Target = null;
        }

        if (string.IsNullOrEmpty(newPath))
            return;

        AvaloniaObject? root = VisualTreeUtils.FindNearestInheritedPropertyDefinition(FocusPathProperty, target);
        if (root != null)
        {
            CurrentlyFocusedObject.Target = root;
        }
        else
        {
            Debug.WriteLine("Failed to find root control that owns the FocusPathProperty of '" + GetFocusPath(target) + "'");
        }
    }

    #endregion
}