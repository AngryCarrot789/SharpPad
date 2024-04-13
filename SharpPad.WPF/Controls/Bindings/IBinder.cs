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

using System.Windows;

namespace SharpPad.WPF.Controls.Bindings
{
    /// <summary>
    /// A generic interface for a binder
    /// </summary>
    /// <typeparam name="TModel">The type of model this binder attaches to</typeparam>
    public interface IBinder<TModel> : IBinder where TModel : class
    {
        /// <summary>
        /// The currently attached element that owns this binder
        /// </summary>
        FrameworkElement Control { get; }

        /// <summary>
        /// The current attached model that this binder uses to update the model value from the view, and vice versa
        /// </summary>
        TModel Model { get; }

        /// <summary>
        /// Attaches both the control and the model to this binder. This is equivalent to
        /// calling <see cref="AttachControl"/> and <see cref="AttachModel"/>.
        /// </summary>
        /// <param name="control">The control to be associated with</param>
        /// <param name="model">The model to be associated with</param>
        void Attach(FrameworkElement control, TModel model);

        /// <summary>
        /// Attaches the control to this binder. If a model is attached (via <see cref="AttachModel"/>) then the binder becomes fully attached
        /// </summary>
        /// <param name="control">The control to be associated with</param>
        void AttachControl(FrameworkElement control);

        /// <summary>
        /// Attaches the model to this binder. If a control is attached (via <see cref="AttachControl"/>) then the binder becomes fully attached
        /// </summary>
        /// <param name="model">The model to be associated with</param>
        void AttachModel(TModel model);

        /// <summary>
        /// Detaches both the control and model from this binder. This is equivalent to
        /// calling <see cref="DetachControl"/> and <see cref="DetachModel"/>.
        /// </summary>
        void Detach();

        /// <summary>
        /// Detaches the control from this binder
        /// </summary>
        void DetachControl();

        /// <summary>
        /// Detaches the model from this binder
        /// </summary>
        void DetachModel();
    }

    /// <summary>
    /// A non-generic interface for a binder
    /// </summary>
    public interface IBinder
    {
        /// <summary>
        /// Returns true when this binder is fully attached to a control and model,
        /// meaning <see cref="Control"/> and <see cref="Model"/> are non-null
        /// </summary>
        bool IsFullyAttached { get; }

        /// <summary>
        /// Notifies the binder that the model value has changed, and to therefore the control value will be updated
        /// </summary>
        void OnModelValueChanged();

        /// <summary>
        /// Notifies the binder that the control value has changed, and to therefore the model value will be updated
        /// </summary>
        void OnControlValueChanged();
    }
}