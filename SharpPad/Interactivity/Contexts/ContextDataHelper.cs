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

using System.Collections;
using System.Collections.Generic;

namespace SharpPad.Interactivity.Contexts {
    public static class ContextDataHelper {
        public static bool ContainsAll(this IContextData data, params DataKey[] keys) {
            foreach (DataKey key in keys)
                if (!data.ContainsKey(key))
                    return false;
            return true;
        }

        public static bool ContainsAll(this IContextData data, IEnumerable<DataKey> keys) {
            foreach (DataKey key in keys)
                if (!data.ContainsKey(key))
                    return false;
            return true;
        }

        public static bool ContainsAll(this IContextData data, DataKey keyA, DataKey keyB) {
            return data.ContainsKey(keyA) && data.ContainsKey(keyB);
        }

        public static bool ContainsAll(this IContextData data, DataKey keyA, DataKey keyB, DataKey keyC) {
            return data.ContainsKey(keyA) && data.ContainsKey(keyB) && data.ContainsKey(keyC);
        }

        public static bool ContainsAll(this IContextData data, params string[] keys) {
            foreach (string key in keys)
                if (!data.ContainsKey(key))
                    return false;
            return true;
        }

        public static bool ContainsAll(this IContextData data, IEnumerable<string> keys) {
            foreach (string key in keys)
                if (!data.ContainsKey(key))
                    return false;
            return true;
        }

        public static bool ContainsAll(this IContextData data, string keyA, string keyB) {
            return data.ContainsKey(keyA) && data.ContainsKey(keyB);
        }

        public static bool ContainsAll(this IContextData data, string keyA, string keyB, string keyC) {
            return data.ContainsKey(keyA) && data.ContainsKey(keyB) && data.ContainsKey(keyC);
        }
    }
}