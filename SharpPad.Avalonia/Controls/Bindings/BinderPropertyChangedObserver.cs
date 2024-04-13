using System;
using Avalonia;

namespace SharpPad.Avalonia.Controls.Bindings;

public class BinderPropertyChangedObserver<TValue> : IObserver<AvaloniaPropertyChangedEventArgs<TValue>>
{
    private readonly Action<AvaloniaPropertyChangedEventArgs<TValue>> _action;

    public BinderPropertyChangedObserver(Action<AvaloniaPropertyChangedEventArgs<TValue>> action)
    {
        this._action = action;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(AvaloniaPropertyChangedEventArgs<TValue> value)
    {
        this._action(value);
    }
}