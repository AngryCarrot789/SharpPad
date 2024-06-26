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

namespace SharpPad.WPF.Utils
{
    /// <summary>
    /// A class the provides simple argument validation
    /// </summary>
    public static class Validate
    {
        /// <summary>
        /// Ensures the value is not null
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="argName">The argument name</param>
        /// <exception cref="ArgumentNullException">The value is null</exception>
        public static void NotNull(object value, string argName)
        {
            if (value == null)
                throw new ArgumentNullException($"'{argName}' cannot be null", nameof(argName));
        }

        /// <summary>
        /// Ensures the string is not null and is not empty
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="argName">The argument name</param>
        /// <exception cref="ArgumentException">The string is null or empty</exception>
        public static void NotNullOrEmpty(string value, string argName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"'{argName}' cannot be null or empty", nameof(argName));
        }

        /// <summary>
        /// Ensures the string is not null, not empty and does not consist of entirely whitespaces
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="argName">The argument name</param>
        /// <exception cref="ArgumentException">The string is null, empty or consists of only whitespaces</exception>
        public static void NotNullOrWhiteSpaces(string value, string argName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"'{argName}' cannot be null, empty or consist of only whitespaces", nameof(argName));
        }
    }
}