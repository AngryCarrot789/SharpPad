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

using System;
using System.Collections.Generic;

namespace SharpPad.Interactivity.Contexts {
    public class ProviderContextData : IContextData {
        private Dictionary<string, Func<object>> map;

        public IEnumerable<KeyValuePair<string, object>> Entries {
            get {
                if (this.map == null)
                    yield break;

                foreach (KeyValuePair<string, Func<object>> entry in this.map) {
                    if (entry.Value() is object value) {
                        yield return new KeyValuePair<string, object>(entry.Key, value);
                    }
                }
            }
        }

        public void SetProvider<T>(DataKey<T> key, Func<T> provider) => this.SetProviderRaw(key.Id, () => provider());

        public void SetProviderRaw(DataKey key, Func<object> provider) => this.SetProviderRaw(key.Id, provider);

        public void SetProviderRaw(string key, Func<object> provider) => (this.map ?? (this.map = new Dictionary<string, Func<object>>()))[key] = provider;

        public bool TryGetContext(string key, out object value) {
            if (this.map != null && this.map.TryGetValue(key, out Func<object> func)) {
                return (value = func()) != null;
            }

            value = null;
            return false;
        }

        public bool ContainsKey(DataKey key) {
            return this.ContainsKey(key.Id);
        }

        public bool ContainsKey(string key) {
            return this.TryGetContext(key, out _);
        }

        public IContextData Clone() {
            return new ProviderContextData() {
                map = this.map != null ? new Dictionary<string, Func<object>>(this.map) : null
            };
        }

        public void Merge(IContextData ctx) {
            Dictionary<string, Func<object>> dictionary;
            if (ctx is ProviderContextData provider) {
                if (provider.map != null) {
                    if ((dictionary = this.map) == null) {
                        this.map = new Dictionary<string, Func<object>>(provider.map);
                    }
                    else {
                        foreach (KeyValuePair<string, Func<object>> entry in provider.map) {
                            dictionary[entry.Key] = entry.Value;
                        }
                    }
                }
            }
            else if (!(ctx is EmptyContext)) {
                using (IEnumerator<KeyValuePair<string, object>> enumerator = ctx.Entries.GetEnumerator()) {
                    if (!enumerator.MoveNext())
                        return;

                    dictionary = this.map ?? (this.map = new Dictionary<string, Func<object>>());
                    do {
                        KeyValuePair<string, object> entry = enumerator.Current;
                        object value = entry.Value;
                        dictionary[entry.Key] = () => value;
                    } while (enumerator.MoveNext());
                }
            }
        }
    }
}