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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharpPad.WPF.Tasks
{
    /// <summary>
    /// Represents a task that can be run by a <see cref="TaskManager"/> on a background thread
    /// </summary>
    public class ActivityTask
    {
        private readonly TaskManager taskManager;
        private readonly Func<Task> action;
        private Exception exception;

        //  0 = waiting for activation
        //  1 = running
        //  2 = completed
        //  3 = cancelled
        private volatile int state;

        /// <summary>
        /// Returns true if the task is currently still running
        /// </summary>
        public bool IsRunning => this.state == 1;

        /// <summary>
        /// Returns true if the task is completed. <see cref="Exception"/> may be non-null when this is true
        /// </summary>
        public bool IsCompleted => this.state > 1;

        /// <summary>
        /// Gets the exception that was thrown during the execution of the user action
        /// </summary>
        public Exception Exception => this.exception;

        /// <summary>
        /// Gets the progress handler associated with this task. Will always be non-null
        /// </summary>
        public IActivityProgress Progress { get; }

        /// <summary>
        /// Gets the first token created from our <see cref="CancellationTokenSource"/>
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets this activity's task, which can be used to await completion. This task is a proxy of
        /// the user task function, and will not throw <see cref="OperationCanceledException"/> when
        /// awaited if our <see cref="CancellationToken"/>'s is cancelled
        /// </summary>
        public Task Task { get; }

        // internal int OwningThreadId;

        private ActivityTask(TaskManager taskManager, Func<Task> action, IActivityProgress activityProgress, CancellationToken cancellationToken)
        {
            this.taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.Progress = activityProgress ?? throw new ArgumentNullException(nameof(activityProgress));
            this.CancellationToken = cancellationToken;
            this.Task = Task.Run(this.TaskMain);
        }

        /// <summary>
        /// Gets this activity's awaiter that can be used to await the activity
        /// </summary>
        /// <returns>The awaiter</returns>
        public TaskAwaiter GetAwaiter() => this.Task.GetAwaiter();

        private async Task TaskMain()
        {
            // this.OwningThreadId = Thread.CurrentThread.ManagedThreadId;
            // Dispatcher.CurrentDispatcher.InvokeAsync(async () => {
            try
            {
                await TaskManager.InternalPreActivateTask(this.taskManager, this);
                this.CheckCancelled();
                await (this.action() ?? Task.CompletedTask);
                await this.OnCompleted(null);
            }
            catch (OperationCanceledException)
            {
                await this.OnCancelled();
            }
            catch (Exception e)
            {
                await this.OnCompleted(e);
            }
            // });
            // Dispatcher.Run();
        }

        public void CheckCancelled() => this.CancellationToken.ThrowIfCancellationRequested();

        private Task OnCancelled() => TaskManager.InternalOnActivityCompleted(this.taskManager, this, 3);

        private async Task OnCompleted(Exception e)
        {
            if ((this.exception = e) != null)
            {
                Debugger.Break();
            }

            await TaskManager.InternalOnActivityCompleted(this.taskManager, this, 2);
        }

        internal static ActivityTask InternalStartActivity(TaskManager taskManager, Func<Task> action, IActivityProgress progress, CancellationToken token)
        {
            return new ActivityTask(taskManager, action, progress ?? new DefaultProgressTracker(), token);
        }

        public static void InternalPostActivate(ActivityTask task)
        {
            task.state = 1;
        }

        public static void InternalComplete(ActivityTask task, int state)
        {
            task.state = state;
        }
    }
}