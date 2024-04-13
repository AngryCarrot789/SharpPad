using System;
using Avalonia;
using Avalonia.Data;

namespace SharpPad.Avalonia.Utils;

public static class AvalonidaPropertyHelper
{
    public static T WithChangedHandler<T, TOwner, TValue>(this T property, Action<TOwner, AvaloniaPropertyChangedEventArgs<TValue>> handler)
        where TOwner : AvaloniaObject
        where T : AvaloniaProperty<TValue>
    {
        property.Changed.AddClassHandler(handler);
        return property;
    }

    public static bool TryGetOldValue<TValue>(this AvaloniaPropertyChangedEventArgs<TValue> args, out TValue value)
    {
        Optional<TValue> oldVal = (args).OldValue;
        if (oldVal.HasValue && (value = oldVal.Value) != null)
            return true;

        value = default!;
        return false;
    }

    public static bool TryGetNewValue<TValue>(this AvaloniaPropertyChangedEventArgs<TValue> args, out TValue value)
    {
        BindingValue<TValue> newVal = (args).NewValue;
        if (newVal.HasValue && (value = newVal.Value) != null)
            return true;

        value = default!;
        return false;
    }
}