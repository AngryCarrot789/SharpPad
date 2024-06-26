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

namespace SharpPad.WPF.Logger
{
    public class LogEntry
    {
        /// <summary>
        /// Gets the time at which this log entry was created
        /// </summary>
        public DateTime LogTime { get; }

        public int Index { get; }

        /// <summary>
        /// Gets the stack trace of the log entry's creation, which contains the call stack up to something being logged
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Gets the string content containing the log message
        /// </summary>
        public string Content { get; }

        public LogEntry(DateTime logTime, int index, string stackTrace, string content)
        {
            this.LogTime = logTime;
            this.Index = index;
            this.StackTrace = stackTrace;
            this.Content = content;
        }
    }
}