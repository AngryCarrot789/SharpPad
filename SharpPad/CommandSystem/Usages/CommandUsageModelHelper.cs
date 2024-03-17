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
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using SharpPad.Interactivity.Contexts;

namespace SharpPad.CommandSystem.Usages {
    /// <summary>
    /// A class that is used to manage a model extracted from a <see cref="CommandUsage"/>'s available context data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CommandUsageModelHelper<T> where T : class {
        public delegate void ModelUsageEventHandler(CommandUsageModelHelper<T> helper);

        public T Model { get; private set; }

        /// <summary>
        /// An event fired when our <see cref="Model"/> is now valid (meaning it was located in the command usage's context data)
        /// <para>
        /// This should add event handlers to <see cref="Model"/> so that the owning command usage can re-query its executability
        /// state or any other data it needs when the model state changes
        /// </para>
        /// </summary>
        public event ModelUsageEventHandler Connected;

        /// <summary>
        /// An event fired when our <see cref="Model"/> is about to be set to null (meaning the command usage's
        /// context changed or was disconnected, and the model was no longer present in the context data).
        /// <para>
        /// This should remove any event handlers from <see cref="Model"/>
        /// </para>
        /// </summary>
        public event ModelUsageEventHandler Disconnected;

        public CommandUsageModelHelper() {
        }

        public void SetModel(CommandUsage usage, T newModel) {
            if (ReferenceEquals(this.Model, newModel)) {
                return;
            }

            if (this.Model != null) {
                this.Disconnected?.Invoke(this);
            }

            if ((this.Model = newModel) != null) {
                this.Connected?.Invoke(this);
            }

            usage.UpdateCanExecuteLater();
        }

        public void OnContextChanged(DataKey<T> dataKey, CommandUsage usage) {
            IContextData ctx = usage.GetContextData();
            if (ctx != null && dataKey.TryGetContext(ctx, out T newFindModel)) {
                this.SetModel(usage, newFindModel);
            }
            else {
                this.SetModel(usage, null);
            }
        }
    }
}