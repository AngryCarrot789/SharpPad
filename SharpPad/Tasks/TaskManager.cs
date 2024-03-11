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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SharpPad.Logger;
using SharpPad.Utils;

namespace SharpPad.Tasks
{
    public delegate void TaskManagerTaskEventHandler(TaskManager taskManager, ActivityTask task, int index);

    public class TaskManager : IDisposable
    {
        public static TaskManager Instance => IoC.TaskManager;

        private readonly ThreadLocal<ActivityTask> threadToTask;
        private readonly List<ActivityTask> tasks;
        private readonly object locker;

        public event TaskManagerTaskEventHandler TaskStarted;
        public event TaskManagerTaskEventHandler TaskCompleted;

        public IReadOnlyList<ActivityTask> ActiveTasks => this.tasks;

        public TaskManager()
        {
            this.threadToTask = new ThreadLocal<ActivityTask>();
            this.tasks = new List<ActivityTask>();
            this.locker = new object();
        }

        public ActivityTask RunTask(Func<Task> action)
        {
            return ActivityTask.InternalRun(this, action, null, CancellationToken.None);
        }

        public ActivityTask RunTask(Func<Task> action, IActivityProgress progress)
        {
            return ActivityTask.InternalRun(this, action, progress, CancellationToken.None);
        }

        /// <summary>
        /// Runs the given action in a background thread
        /// </summary>
        /// <param name="action">The action to invoke</param>
        /// <param name="progress">The task progress used to represent the task's completion</param>
        /// <param name="cancellationToken">A token used to signal task cancellation</param>
        /// <returns>The task</returns>
        public ActivityTask RunTask(Func<Task> action, IActivityProgress progress, CancellationToken cancellationToken)
        {
            return ActivityTask.InternalRun(this, action, progress, cancellationToken);
        }

        /// <summary>
        /// Tries to get the activity task associated with the current caller thread
        /// </summary>
        /// <param name="task">The task associated with the current thread</param>
        /// <returns>True if this thread is running a task</returns>
        public bool TryGetCurrentTask(out ActivityTask task)
        {
            if (this.threadToTask.IsValueCreated && (task = this.threadToTask.Value) != null)
            {
                return true;
            }

            task = null;
            return false;
        }

        public ActivityTask CurrentTask => this.threadToTask.Value;

        /// <summary>
        /// Gets either the current task's activity progress tracker, or the <see cref="EmptyActivityProgress"/> instance (for convenience over null-checks)
        /// </summary>
        /// <returns></returns>
        public IActivityProgress GetCurrentProgressOrEmpty()
        {
            if (this.TryGetCurrentTask(out ActivityTask task))
            {
                return task.Progress;
            }

            return EmptyActivityProgress.Instance;
        }

        public void Dispose()
        {
            this.threadToTask.Dispose();
        }

        internal static void InternalBeginActivateTask(TaskManager taskManager, ActivityTask task)
        {
            taskManager.threadToTask.Value = task;
            IoC.Dispatcher.InvokeAsync(() => OnTaskStarted_AMT(taskManager, task));
        }

        internal static void InternalOnTaskCompleted_BGTHREAD(TaskManager taskManager, ActivityTask task, int state)
        {
            taskManager.threadToTask.Value = null;
            IoC.Dispatcher.InvokeAsync(() => OnTaskCompleted_AMT(taskManager, task, state));
        }

        internal static void OnTaskStarted_AMT(TaskManager taskManager, ActivityTask task)
        {
            lock (taskManager.locker)
            {
                int index = taskManager.tasks.Count;
                taskManager.tasks.Insert(index, task);
                taskManager.TaskStarted?.Invoke(taskManager, task, index);
            }

            ActivityTask.InternalActivate(task);
        }

        internal static void OnTaskCompleted_AMT(TaskManager taskManager, ActivityTask task, int state)
        {
            ActivityTask.InternalComplete(task, state);
            lock (taskManager.locker)
            {
                int index = taskManager.tasks.IndexOf(task);
                if (index == -1)
                {
                    const string msg = "Completed activity task did not exist in this task manager's internal task list";
                    AppLogger.Instance.WriteLine("[FATAL] " + msg);
                    Debugger.Break();
                    return;
                }

                taskManager.tasks.RemoveAt(index);
                taskManager.TaskCompleted?.Invoke(taskManager, task, index);
            }

            if (task.Exception is Exception e)
            {
                IoC.MessageService.ShowMessage("Task Error", "An exception occurred while running a task", e.GetToString());
            }
        }
    }
}