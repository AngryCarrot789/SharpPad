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
using System.Reflection;
using System.Windows;
using SharpPad.WPF.Utils;
using SharpPad.WPF.Utils.Accessing;

namespace SharpPad.WPF.Controls.Bindings
{
    /// <summary>
    /// A standard basic binder that uses a getter and setter for the model value, adds and removes a dynamically generated event
    /// handler for the model value changed event, and registers a dependency property changed event handler for the control.
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    public class AccessorAutoEventPropertyBinder<TModel, TValue> : BaseBinder<TModel> where TModel : class
    {
        private readonly EventInfo eventInfo;
        private readonly ValueAccessor<TValue> accessor;
        private readonly Delegate handlerInternal;
        private DependencyPropertyDescriptor descriptor;

        /// <summary>
        /// Gets the property that is used to listen to property value changed notifications on the attached
        /// control. May be null if <see cref="BaseBinder{TModel}.OnControlValueChanged"/> is called manually
        /// </summary>
        public DependencyProperty Property { get; }

        public AccessorAutoEventPropertyBinder(DependencyProperty property, string eventName, ValueAccessor<TValue> accessor)
        {
            this.accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            this.Property = property;
            this.eventInfo = typeof(TModel).GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
            if (this.eventInfo == null)
                throw new Exception("Could not find event by name: " + typeof(TModel).Name + "." + eventName);

            this.handlerInternal = EventUtils.CreateDelegateToInvokeActionFromEvent(this.eventInfo.EventHandlerType, this.OnEvent);
        }

        private void OnEvent() => this.OnModelValueChanged();

        protected override void UpdateModelCore()
        {
            if (this.IsFullyAttached && this.Property != null)
            {
                object newValue = this.Control.GetValue(this.Property);
                this.accessor.SetObjectValue(this.Model, newValue);
            }
        }

        protected override void UpdateControlCore()
        {
            if (this.IsFullyAttached && this.Property != null)
            {
                object newValue = this.accessor.GetObjectValue(this.Model);
                this.Control.SetValue(this.Property, newValue);
            }
        }

        protected override void OnAttached()
        {
            this.eventInfo.AddEventHandler(this.Model, this.handlerInternal);
            if (this.Property != null)
            {
                this.descriptor = DependencyPropertyDescriptor.FromProperty(this.Property, this.Control.GetType());
                this.descriptor.AddValueChanged(this.Control, this.OnPropertyValueChanged);
            }
        }

        protected override void OnDetached()
        {
            this.eventInfo.RemoveEventHandler(this.Model, this.handlerInternal);
            if (this.descriptor != null)
            {
                this.descriptor.RemoveValueChanged(this.Control, this.OnPropertyValueChanged);
                this.descriptor = null;
            }
        }

        private void OnPropertyValueChanged(object sender, EventArgs e)
        {
            if (!this.IsUpdatingControl)
            {
                this.OnControlValueChanged();
            }
        }
    }
}