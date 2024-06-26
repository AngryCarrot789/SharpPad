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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using SharpPad.WPF.Utils.Visuals;

namespace SharpPad.WPF.Behaviours
{
    public class BehaviourCollection : FreezableCollection<BehaviourBase>
    {
        // The property is private because it forces the XAML reader stuff to call the GetBehaviours method, which
        // dynamically creates an instance of the collection, which is preferable over having to do it manually in XAML each time
        private static readonly DependencyProperty BehavioursProperty = DependencyProperty.RegisterAttached("InternalBehaviours", typeof(BehaviourCollection), typeof(BehaviourCollection), new PropertyMetadata(null, OnBehavioursChanged));
        private static readonly Action<Visual> RemoveHandler;
        private static readonly Action<Visual> AddHandler;

        private bool IsOwnerVisual => this.Owner is Visual;

        /// <summary>
        /// Gets the element that owns this collection
        /// </summary>
        public DependencyObject Owner { get; private set; }

        // lazily add/remove event handler for VAC, as handlers existing do have some tiny overhead in the VT operations
        private int vacCount;

        public BehaviourCollection()
        {
            ((INotifyCollectionChanged) this).CollectionChanged += this.OnCollectionChanged;
        }

        static BehaviourCollection()
        {
            VisualAncestorChangedEventInterface.CreateInterface(OnVisualAncestorChanged, out AddHandler, out RemoveHandler);
        }

        internal void RegisterVAC(BehaviourBase behaviour)
        {
            if (this.Owner == null)
                throw new InvalidOperationException("No owner");

            if (!this.IsOwnerVisual)
            {
                Debug.WriteLine(behaviour.GetType() + " tried to register the VisualAncestorChanged event, but our owner is not a visual: " + this.Owner);
                return;
            }

            if (this.vacCount == 0)
            {
                AddHandler((Visual) this.Owner);
            }

            this.vacCount++;
        }

        internal void UnregisterVAC()
        {
            if (this.Owner == null)
                throw new InvalidOperationException("No owner");
            if (!this.IsOwnerVisual)
                return;

            if (--this.vacCount == 0)
            {
                RemoveHandler((Visual) this.Owner);
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.DetatchAndTryAttachAll(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    DetatchAll(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    DetatchAll(e.OldItems);
                    this.DetatchAndTryAttachAll(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Move: break;
                case NotifyCollectionChangedAction.Reset:
                    DetatchAll(this);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void DetatchAndTryAttachAll(IEnumerable enumerable)
        {
            foreach (BehaviourBase behaviour in enumerable)
            {
                if (behaviour.AttachedElement != null)
                    behaviour.Detatch();
                if (this.Owner != null && ((IBehaviour) behaviour).CanAttachTo(this.Owner))
                    behaviour.Attach(this);
            }
        }

        private static void DetatchAll(IEnumerable enumerable)
        {
            foreach (BehaviourBase behaviour in enumerable)
            {
                if (behaviour.AttachedElement != null)
                    behaviour.Detatch();
            }
        }

        public void Connect(DependencyObject element)
        {
            if (this.Owner != null)
                throw new InvalidOperationException("Already attached");
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            this.Owner = element;
            this.DetatchAndTryAttachAll(this);
        }

        public void Disconnect()
        {
            if (this.Owner == null)
                throw new InvalidOperationException("Not attached: no owner");

            DetatchAll(this);
            if (this.vacCount > 0)
            {
                Debug.WriteLine("Expected VACCount to be zero when all items are detached");
                Debugger.Break();
                this.vacCount = 0;
                RemoveHandler((Visual) this.Owner);
            }

            this.Owner = null;
        }

        public static BehaviourCollection GetBehaviours(UIElement element)
        {
            BehaviourCollection value = (BehaviourCollection) element.GetValue(BehavioursProperty);
            if (value == null)
            {
                element.SetValue(BehavioursProperty, value = new BehaviourCollection());
            }

            return value;
        }

        private static void OnBehavioursChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is BehaviourCollection oldCollection)
            {
                oldCollection.Disconnect();
            }

            if (e.NewValue is BehaviourCollection newCollection)
            {
                if (newCollection.Owner != null)
                    throw new InvalidOperationException("New behaviour collection is already connected");

                newCollection.Connect(d);
            }
        }

        private static void OnVisualAncestorChanged(object sender, DependencyObject element, DependencyObject oldParent)
        {
            if (ReferenceEquals(sender, element) && element.GetValue(BehavioursProperty) is BehaviourCollection collection)
            {
                // collection should be non-null realistically, based on the VAC event registration logic
                if (collection.Owner != element)
                {
                    Debug.WriteLine("Fatal error: received VAC event for unrelated visual");
                    Debugger.Break();
                    return;
                }

                foreach (BehaviourBase behaviour in collection)
                {
                    BehaviourBase.InternalProcessVisualParentChanged(behaviour, oldParent);
                }
            }
        }
    }
}