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
using System.Windows;

namespace SharpPad.WPF.Controls.Bindings
{
    /// <summary>
    /// The base class for general binders, which are used to create a "bind" between model and event.
    /// <para>
    /// The typical behaviour is to add an event handler in user code and call <see cref="OnModelValueChanged"/>
    /// which will cause <see cref="UpdateControlCore"/> to be called, allowing you to update the control's value. An internal bool
    /// will stop a stack overflow when the control's value ends up calling <see cref="OnControlValueChanged"/> which ignores
    /// the call if that bool is true
    /// </para>
    /// <para>
    /// Then, an event handler should be added for the control and it should call <see cref="OnControlValueChanged"/>, which causes
    /// <see cref="UpdateModelCore"/>. As before, an internal bool stops a stack overflow when the value changes ends up
    /// calling <see cref="OnModelValueChanged"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TModel">The type of model</typeparam>
    public abstract class BaseBinder<TModel> : IBinder<TModel> where TModel : class
    {
        public FrameworkElement Control { get; private set; }

        public TModel Model { get; private set; }

        public bool IsFullyAttached { get; private set; }

        /// <summary>
        /// Gets whether the binder is currently processing the model change signal, and is now updating
        /// the control's value. This is used to prevent a stack overflow exception
        /// </summary>
        public bool IsUpdatingControl { get; protected set; }

        protected BaseBinder() { }

        public void OnModelValueChanged()
        {
            if (!this.IsFullyAttached)
            {
                return;
            }

            // We don't check if we are updating the control, just in case the model
            // decided to coerce its own value which is different from the UI control

            try
            {
                this.IsUpdatingControl = true;
                this.UpdateControlCore();
            }
            finally
            {
                this.IsUpdatingControl = false;
            }
        }

        public void OnControlValueChanged()
        {
            if (!this.IsUpdatingControl && this.IsFullyAttached)
            {
                this.UpdateModelCore();
            }
        }

        /// <summary>
        /// This method should be overridden to update the model's value using the element's value
        /// </summary>
        protected abstract void UpdateModelCore();

        /// <summary>
        /// This method should be overridden to update the control's value using the model's value
        /// </summary>
        protected abstract void UpdateControlCore();

        public void Attach(FrameworkElement control, TModel model)
        {
            if (this.IsFullyAttached)
                throw new Exception("Already fully attached");
            if (this.Control != null)
                throw new InvalidOperationException("A control is already attached");
            if (this.Model != null)
                throw new InvalidOperationException("A model is already attached");
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            this.Model = model;
            this.Control = control;
            this.IsFullyAttached = true;
            this.OnAttached();
            this.OnModelValueChanged();
        }

        public void AttachControl(FrameworkElement control)
        {
            if (this.IsFullyAttached)
                throw new Exception("Already fully attached");
            if (this.Control != null)
                throw new InvalidOperationException("A control is already attached");
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            this.Control = control;
            if (this.Model != null)
            {
                this.IsFullyAttached = true;
                this.OnAttached();
                this.OnModelValueChanged();
            }
        }

        public void AttachModel(TModel model)
        {
            if (this.IsFullyAttached)
                throw new Exception("Already fully attached");
            if (this.Model != null)
                throw new InvalidOperationException("A model is already attached");
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            this.Model = model;
            if (this.Control != null)
            {
                this.IsFullyAttached = true;
                this.OnAttached();
                this.OnModelValueChanged();
            }
        }

        public void Detach()
        {
            if (!this.IsFullyAttached)
                throw new Exception("Not attached");
            this.TryDetatchCore();
            this.Model = null;
            this.Control = null;
        }

        public void DetachControl()
        {
            if (this.Control == null)
                throw new InvalidOperationException("No control is attached");
            this.TryDetatchCore();
            this.Control = null;
        }

        public void DetachModel()
        {
            if (this.Model == null)
                throw new InvalidOperationException("No model is attached");
            this.TryDetatchCore();
            this.Model = null;
        }

        protected abstract void OnAttached();

        protected abstract void OnDetached();

        private void TryDetatchCore()
        {
            if (this.IsFullyAttached)
            {
                this.OnDetached();
                this.IsFullyAttached = false;
            }
        }
    }
}