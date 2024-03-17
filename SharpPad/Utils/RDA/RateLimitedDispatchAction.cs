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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SharpPad.Utils.RDA {
    /// <summary>
    /// A class that is similar to <see cref="RapidDispatchActionEx"/>, but has a set amount of time
    /// that has to pass before the callback is scheduled, ensuring the callback is not executed too quickly
    /// </summary>
    public class RateLimitedDispatchAction : IDispatchAction {
        private const int F_CONTINUE = 1;          // Keeps the task running
        private const int F_RUNNING = 2;           // Whether or not the task is running
        private const int F_EXECUTING = 4;         // Whether or not we're executing the callback
        private const int F_CONTINUE_CRITICAL = 8; // InvokeAsync was called while executing

        private readonly Func<Task> callback; // The non-null user callback method to run code
        private readonly object stateLock;    // Used to guard state modifications
        private volatile int state;           // Stores the current state of this object
        private long lastExecutionTime;       // The time at which the callback execution completed
        private long minIntervalTicks;        // The minimum interval per callbacks

        /// <summary>
        /// Gets or sets the minimum callback interval, that is, the smallest amount of time
        /// that must pass before the callback function can be invoked and awaited
        /// </summary>
        public TimeSpan MinimumInterval {
            get => new TimeSpan(Interlocked.Read(ref this.minIntervalTicks));
            set => Interlocked.Exchange(ref this.minIntervalTicks, value.Ticks);
        }

        public RateLimitedDispatchAction(Func<Task> callback) : this(callback, TimeSpan.FromMilliseconds(250)) { }

        public RateLimitedDispatchAction(Func<Task> callback, TimeSpan minimumInterval) {
            Validate.NotNull(callback, nameof(callback));
            if (minimumInterval.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumInterval), "Minimum interval must represent zero or more time");

            this.callback = callback;
            this.MinimumInterval = minimumInterval;
            this.stateLock = new object();
        }

        public static RateLimitedDispatchAction ForDispatcherAsync(Func<Task> callback, TimeSpan minInterval, DispatcherPriority priority = DispatcherPriority.Send) => ForDispatcherAsync(callback, minInterval, Application.Current.Dispatcher, priority);

        public static RateLimitedDispatchAction ForDispatcherAsync(Func<Task> callback, TimeSpan minInterval, Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Send) {
            Validate.NotNull(callback, nameof(callback));
            Validate.NotNull(dispatcher, nameof(dispatcher));
            return new RateLimitedDispatchAction(async () => {
                try {
                    DispatcherOperation<Task> operation = dispatcher.InvokeAsync(callback, priority);
                    Task task = await operation;
                    await (task ?? Task.CompletedTask);
                }
                catch (Exception e) {
                    dispatcher.BeginInvoke(new Action(() => {
                        throw new Exception("Exception while awaiting operation", e);
                    }));
                }

            }, minInterval);
        }

        public static RateLimitedDispatchAction ForDispatcherSync(Action callback, TimeSpan minInterval, DispatcherPriority priority = DispatcherPriority.Send) => ForDispatcherSync(callback, minInterval, Application.Current.Dispatcher, priority);

        public static RateLimitedDispatchAction ForDispatcherSync(Action callback, TimeSpan minInterval, Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Send) {
            Validate.NotNull(callback, nameof(callback));
            Validate.NotNull(dispatcher, nameof(dispatcher));
            // No need to use async, since we can just directly access the DispatcherOperation's task,
            // which will become completed after the callback returns
            return new RateLimitedDispatchAction(() => dispatcher.InvokeAsync(callback, priority).Task, minInterval);
        }

        /// <summary>
        /// Triggers this executor, possibly starting a new <see cref="Task"/>, or notifying the existing internal task that there's new input
        /// </summary>
        public void InvokeAsync() {
            lock (this.stateLock) {
                int myState = this.state;
                if ((myState & F_EXECUTING) == 0) {
                    // We are not executing, so append CONTINUE to let the task continue running
                    myState |= F_CONTINUE;
                }
                else {
                    // The callback is currently being processed, so the critical condition is set
                    myState |= F_CONTINUE_CRITICAL;
                }

                if ((myState & F_RUNNING) == 0) {
                    // We are not running, so start a new task and append RUNNING
                    this.state = myState | F_RUNNING;
                    Task.Run(this.TaskMain);
                }
                else {
                    // Task is already running, so just volatile write the state
                    this.state = myState;
                }
            }
        }

        private async Task TaskMain() {
            long lastExecTime = Interlocked.Read(ref this.lastExecutionTime);
            long interval = Time.GetSystemTicks() - lastExecTime;

            do {
                // We will sleep at least twice, even if InvokeAsync is only called.
                // This is so that we don't need to keep creating lots of tasks when
                // InvokeAsync is called very often
                long minInterval = Interlocked.Read(ref this.minIntervalTicks);
                if (interval < minInterval)
                    await Task.Delay(new TimeSpan(minInterval - interval));

                int myState;
                lock (this.stateLock) {
                    if (((myState = this.state) & F_CONTINUE) == 0) {
                        this.state = myState & ~F_RUNNING;
                        Interlocked.Exchange(ref this.lastExecutionTime, lastExecTime);
                        return;
                    }
                    else {
                        this.state = (myState & ~F_CONTINUE) | F_EXECUTING;
                    }
                }

                try {
                    // Use CompletedTask just in case execute returns a null task for some reason
                    await (this.callback.Invoke() ?? Task.CompletedTask);
                }
                catch (Exception e) {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => throw new Exception("Exception while awaiting operation", e)));
                }
                finally {
                    // This sets CONTINUE to false, indicating that there is no more work required.
                    // However there is a window between when the task finishes and condition being set to false
                    // where another thread can set condition to true:
                    //     Task just completes, another thread sets condition from false to true,
                    //     but then that change is overwritten and condition is set to false here
                    //
                    // That might mean that whatever work the task does will lose out on the absolute latest
                    // update (that occurred a few microseconds~ after the task completed)
                    // So hopefully, the usage of EXECUTING and CONTINUE_CRITICAL will help against that
                    lock (this.stateLock) {
                        if (((myState = this.state) & F_CONTINUE_CRITICAL) != 0) {
                            // Critical condition is active, so: Remove CRITICAL and append CONTINUE
                            myState = (myState & ~F_CONTINUE_CRITICAL) | F_CONTINUE;
                        }
                        else {
                            // Critical condition not met, so just remove continue,
                            // allowing the task to possibly exit normally
                            myState &= ~F_CONTINUE;
                        }

                        // Remove EXECUTING flag, meaning the critical condition cannot be met,
                        // which is good since we just processed it.
                        // RUNNING will still be present
                        this.state = myState & ~F_EXECUTING;
                    }
                }

                lastExecTime = Time.GetSystemTicks();
                interval = 0;
            } while (true);
        }

        /// <summary>
        /// Clears the critical continuation state.
        /// <para>
        /// This state is used to re-schedule the callback when <see cref="InvokeAsync"/> is invoked during
        /// execution of the callback.
        /// </para>
        /// <para>
        /// By clearing the state, it means the callback won't be rescheduled if <see cref="InvokeAsync"/> was invoked during
        /// the callback execution. This is useful if you have code to handle similar 're-scheduling' behaviour manually
        /// </para>
        /// </summary>
        public void ClearCriticalState() {
            lock (this.stateLock) {
                int myState = this.state;
                if ((myState & F_CONTINUE_CRITICAL) != 0) {
                    // Critical condition is active, so remove it, as if it was never activated
                    myState &= ~F_CONTINUE_CRITICAL;
                }

                this.state = myState;
            }
        }
    }
}