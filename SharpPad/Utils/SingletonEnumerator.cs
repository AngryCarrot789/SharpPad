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

using System.Collections;
using System.Collections.Generic;

namespace SharpPad.Utils
{
    public class SingletonEnumerator<T> : IEnumerator<T>
    {
        private bool hasMovedNext;

        // should this throw if hasMovedNext is false?
        public T Current { get; }

        object IEnumerator.Current => this.Current;

        public SingletonEnumerator(T value)
        {
            this.Current = value;
        }

        public bool MoveNext()
        {
            if (this.hasMovedNext)
            {
                return false;
            }
            else
            {
                this.hasMovedNext = true;
                return true;
            }
        }

        public void Reset()
        {
            this.hasMovedNext = false;
        }

        public void Dispose() { }
    }
}