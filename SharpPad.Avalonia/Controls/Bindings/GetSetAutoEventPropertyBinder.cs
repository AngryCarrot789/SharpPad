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
using System.Reflection;
using Avalonia;
using SharpPad.Avalonia.Utils;

namespace SharpPad.Avalonia.Controls.Bindings;

/// <summary>
/// A standard basic binder that uses a getter and setter for the model value, adds and removes a dynamically generated event
/// handler for the model value changed event, and registers a dependency property changed event handler for the control.
/// </summary>
/// <typeparam name="TModel">The model type</typeparam>
public class GetSetAutoEventPropertyBinder<TModel> : BaseBinder<TModel> where TModel : class
{
    private readonly EventInfo eventInfo;
    private readonly Func<IBinder<TModel>, object> getter;
    private readonly Action<IBinder<TModel>, object> setter;
    private readonly Delegate handlerInternal;
    private IDisposable? propChangeHandler;

    /// <summary>
    /// Gets the property that is used to listen to property value changed notifications on the attached
    /// control. May be null if <see cref="BaseBinder{TModel}.OnControlValueChanged"/> is called manually
    /// </summary>
    public AvaloniaProperty? Property { get; }

    public GetSetAutoEventPropertyBinder(AvaloniaProperty property, string eventName, Func<IBinder<TModel>, object> getModelValue, Action<IBinder<TModel>, object> setModelValue)
    {
        this.eventInfo = typeof(TModel).GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
        if (this.eventInfo == null)
            throw new Exception("Could not find event by name: " + typeof(TModel).Name + "." + eventName);

        this.handlerInternal = EventUtils.CreateDelegateToInvokeActionFromEvent(this.eventInfo.EventHandlerType, this.OnEvent);
        this.getter = getModelValue;
        this.setter = setModelValue;
        this.Property = property;
    }

    private void OnEvent() => this.OnModelValueChanged();

    protected override void UpdateModelCore()
    {
        if (this.IsFullyAttached && this.Property != null && this.setter != null)
        {
            object newValue = this.Control.GetValue(this.Property);
            this.setter(this, newValue);
        }
    }

    protected override void UpdateControlCore()
    {
        if (this.IsFullyAttached && this.Property != null && this.getter != null)
        {
            object newValue = this.getter(this);
            this.Control.SetValue(this.Property, newValue);
        }
    }

    protected override void OnAttached()
    {
        this.eventInfo.AddEventHandler(this.Model, this.handlerInternal);
        if (this.Property != null)
            this.propChangeHandler = this.Property.Changed.AddClassHandler<AvaloniaObject>(this.OnPropertyValueChanged);
    }

    protected override void OnDetached()
    {
        this.eventInfo.RemoveEventHandler(this.Model, this.handlerInternal);
        this.propChangeHandler?.Dispose();
        this.propChangeHandler = null;
    }

    private void OnPropertyValueChanged(object sender, EventArgs e)
    {
        if (!this.IsUpdatingControl)
        {
            this.OnControlValueChanged();
        }
    }
}