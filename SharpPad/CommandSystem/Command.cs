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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpPad.Logger;
using SharpPad.Utils;

namespace SharpPad.CommandSystem {
    /// <summary>
    /// Represents some sort of action that can be executed. Commands use provided contextual
    /// information (see <see cref="CommandEventArgs.ContextData"/>) to do work. Commands do
    /// their work in the <see cref="Execute"/> method, and can optionally specify their
    /// executability via the <see cref="CanExecute"/> method
    /// <para>
    /// Commands are the primary things used by the shortcut system to do some work. They
    /// can also be used by things like context menus
    /// </para>
    /// <para>
    /// These commands can be executed through the <see cref="CommandManager.Execute(string,SharpPad.CommandSystem.Command,SharpPad.Interactivity.Contexts.IContextData,bool)"/> function
    /// </para>
    /// </summary>
    public abstract class Command {
        private bool isExecuting;

        /// <summary>
        /// Returns true when this command is allowed to be executed multiple times even if
        /// it was executed previously and the task has not completed, e.g. downloading a file.
        /// This is true by default. Overriding this and setting it to false means an is-executing
        /// flag becomes true when executing the command, and false once it finishes, and this command
        /// cannot be executed normally until it has finished
        /// </summary>
        public virtual bool AllowMultipleExecutors => true;

        protected Command() {
        }

        // When focus changes, raise notification to update commands
        // Then fire ContextDataChanged for those command hooks or whatever, they can then disconnect
        // old event handlers and attach new ones

        /// <summary>
        /// Get the command context Checks if this command can actually be executed. This typically isn't checked before
        /// <see cref="Execute"/> is invoked; this is mainly used by the UI to determine if
        /// something like a button or menu item is actually clickable
        /// <para>
        /// This method should be quick to execute, as it may be called quite often
        /// </para>
        /// </summary>
        /// <param name="e">The command event args, containing info about the current context</param>
        /// <returns>
        /// True if executing this command would most likely result in success, otherwise false
        /// </returns>
        public virtual Executability CanExecute(CommandEventArgs e) {
            return Executability.Valid;
        }

        /// <summary>
        /// Executes this specific command with the given command event args. This is called by <see cref="ExecuteAsync"/>
        /// </summary>
        /// <param name="e">The command event args, containing info about the current context</param>
        protected abstract Task Execute(CommandEventArgs e);

        internal static bool InternalBeginExecution(Command command) {
            if (command.AllowMultipleExecutors)
                return true;

            if (command.isExecuting)
                return false;

            command.isExecuting = true;
            return true;
        }

        internal static void InternalEndExecution(Command command) {
            if (command.AllowMultipleExecutors)
                return;

            command.isExecuting = false;
        }

        public static bool IsExecuting(Command command) => !command.AllowMultipleExecutors && command.isExecuting;

        internal static async Task InternalExecuteUser(string cmdId, Command command, CommandEventArgs e) {
            if (!InternalBeginExecution(command)) {
                if (e.IsUserInitiated)
                    IoC.MessageService.ShowMessage("Already running", "This command is already running");
                return;
            }

            try {
                if (e.IsUserInitiated) {
                    try {
                        await command.Execute(e);
                    }
                    catch (TaskCanceledException) {
                        AppLogger.Instance.WriteLine($"Command execution was cancelled: {CmdToString(cmdId, command)}");
                    }
                    catch (OperationCanceledException) {
                        AppLogger.Instance.WriteLine($"Command (operation) execution was cancelled: {CmdToString(cmdId, command)}");
                    }
                    catch (Exception ex) when (!Debugger.IsAttached) {
                        IoC.MessageService.ShowMessage("Command execution exception", $"An exception occurred while executing '{CmdToString(cmdId, command)}'", ex.GetToString());
                    }
                }
                else {
                    await command.Execute(e);
                }
            }
            finally {
                InternalEndExecution(command);
            }
        }

        private static string CmdToString(string cmdId, Command cmd) {
            if (cmdId != null && !string.IsNullOrWhiteSpace(cmdId)) {
                return $"{cmdId} ({cmd.GetType()})";
            }
            else {
                return cmd.GetType().ToString();
            }
        }
    }
}