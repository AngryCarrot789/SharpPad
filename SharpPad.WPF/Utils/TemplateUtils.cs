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
using System.Windows.Controls;

namespace SharpPad.WPF.Utils
{
    /// <summary>
    /// A helper class for conveniently accessing control template children by name
    /// </summary>
    public static class TemplateUtils
    {
        /// <summary>
        /// Gets the template child of the given control. Throws an exception if no such child exists with that name or the child is an invalid type
        /// </summary>
        /// <param name="control"></param>
        /// <param name="childName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T GetTemplateChild<T>(Control control, string childName) where T : DependencyObject
        {
            object child = FindTemplateChild(control, childName);
            if (child == null)
            {
                throw new Exception($"Missing template part '{childName}'");
            }

            if (!(child is T value))
            {
                throw new Exception($"Incompatible template part type '{childName}'. Expected {typeof(T).FullName}, got {child.GetType().FullName}");
            }

            return value;
        }

        /// <summary>
        /// A void version of <see cref="GetTemplateChild{T}(System.Windows.Controls.Control,string)"/> which sets the return value as the given out variable
        /// </summary>
        /// <param name="control">The control which has a templated applied</param>
        /// <param name="childName">The name of the templated child</param>
        /// <param name="value">The output child</param>
        /// <typeparam name="T">The type of templated child</typeparam>
        public static void GetTemplateChild<T>(Control control, string childName, out T value) where T : DependencyObject
        {
            value = GetTemplateChild<T>(control, childName);
        }

        /// <summary>
        /// Tries to find a templated child with the given name and of the given generic type
        /// </summary>
        /// <param name="control">The control which has a templated applied</param>
        /// <param name="childName">The name of the templated child</param>
        /// <param name="value">The found template child</param>
        /// <typeparam name="T">The type of child</typeparam>
        /// <returns>True if the child was found, or false if not</returns>
        public static bool TryGetTemplateChild<T>(Control control, string childName, out T value) where T : DependencyObject
        {
            return (value = FindTemplateChild(control, childName) as T) != null;
        }

        /// <summary>
        /// Tries to find a templated child with the given name of the given control's template, if it has one.
        /// </summary>
        /// <param name="control">The control which has a templated applied</param>
        /// <param name="childName">The name of the templated child</param>
        /// <returns>
        /// The templated child, or null if the control has no template or the child does not exist
        /// </returns>
        public static object FindTemplateChild(Control control, string childName)
        {
            return control.Template?.FindName(childName, control);
        }
    }
}