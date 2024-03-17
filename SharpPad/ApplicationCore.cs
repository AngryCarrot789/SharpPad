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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using SharpPad.CommandSystem;
using SharpPad.Logger;
using SharpPad.Notepads;
using SharpPad.Notepads.Commands;
using SharpPad.Services.Files;
using SharpPad.Services.Messages;
using SharpPad.Services.WPF.Files;
using SharpPad.Services.WPF.Messages;
using SharpPad.Tasks;

namespace SharpPad {
    /// <summary>
    /// An application instance for SharpPad
    /// </summary>
    public class ApplicationCore {
        private readonly ServiceManager serviceManager;

        public IServiceManager Services => this.serviceManager;

        public static ApplicationCore Instance { get; } = new ApplicationCore();

        /// <summary>
        /// Gets the current version of the application. This value does not change during runtime.
        /// <para>The <see cref="Version.Major"/> property is used to represent a rewrite of the application (for next update)</para>
        /// <para>The <see cref="Version.Minor"/> property is used to represent a large change (for next update)</para>
        /// <para>The <see cref="Version.Build"/> property is used to represent any change to the code (for next update)</para>
        /// <para>
        /// 'for next update' meaning the number is incremented when there's a push to the github, as this is
        /// easiest to track. Many different changes can count as one update
        /// </para>
        /// </summary>
        public Version CurrentVersion { get; } = new Version(1, 0, 0, 0);

        /// <summary>
        /// Gets the current build version of this application. This accesses <see cref="CurrentVersion"/>, and changes whenever a new change is made to the application (regardless of how small)
        /// </summary>
        public int CurrentBuild => this.CurrentVersion.Build;

        public Notepad Nodepad { get; private set; }

        public IDispatcher Dispatcher { get; }

        private ApplicationCore() {
            this.Dispatcher = new DispatcherDelegate(Application.Current.Dispatcher);
            this.serviceManager = new ServiceManager();
            this.serviceManager.Register<IMessageDialogService>(new WPFMessageDialogService());
            this.serviceManager.Register<IUserInputDialogService>(new WPFUserInputDialogService());
            this.serviceManager.Register<IFilePickDialogService>(new WPFFilePickDialogService());
            this.serviceManager.Register<TaskManager>(new TaskManager());
        }

        public void OnApplicationLoaded(Notepad notepad, string[] args) {
            this.Nodepad = notepad;
            if (args.Length > 0 && File.Exists(args[0])) {
                OpenFilesCommand.OpenFile(notepad, args[0]);
            }
            else {
                this.LoadDefaultNotepad();
            }

            TaskManager.Instance.RunTask(async () => {
                IActivityProgress prog = TaskManager.Instance.CurrentTask.Progress;
                prog.Text = "Dummy task";
                const int duration = 1;
                const double interval = 0.1;
                const int updates = (int) (duration / interval);
                const double progressPerUpdate = 1.0 / updates;
                TimeSpan delayTime = TimeSpan.FromSeconds(interval);

                for (int i = 0; i < updates; i++) {
                    await Task.Delay(delayTime);
                    prog.OnProgress(progressPerUpdate);
                }
            });
        }

        public void OnApplicationExiting() { }

        private void LoadDefaultNotepad() {
            this.Nodepad.AddNewEditor(new NotepadDocument() {DocumentName = "New Document 1", Document = {Text = ""}, IsModified = false});
            this.Nodepad.AddNewEditor(new NotepadDocument() {DocumentName = "New Document 2", Document = {Text = "some text here"}, IsModified = false});
            this.Nodepad.AddNewEditor(new NotepadDocument() {DocumentName = "New Document 3", Document = {Text = "some more text 111"}, IsModified = false});
        }

        public void RegisterActions(CommandManager manager) {
            // general commands
            manager.Register("NewFile", new NewFileCommand());
            manager.Register("OpenFile", new OpenFilesCommand());
            manager.Register("SaveDocumentFile", new SaveDocumentCommand());
            manager.Register("SaveDocumentFileAs", new SaveDocumentAsCommand());
            manager.Register("SaveAllDocumentFilesAs", new SaveAllDocumentsCommand());
            manager.Register("CloseDocument", new CloseDocumentCommand());

            // document/editor commands
            manager.Register("CutInEditor", new CutCommand());
            manager.Register("CopyInEditor", new CopyCommand());
            manager.Register("CopyFilePathInEditor", new CopyFilePathCommand());
            manager.Register("PasteInEditor", new PasteCommand());
            manager.Register("DeleteTextOrLineInEditor", new DeleteTextOrLineCommand());
            manager.Register("DuplicateTextOrLineInEditor", new DuplicateTextOrLineCommand());
            manager.Register("UndoInEditor", new UndoCommand());
            manager.Register("RedoInEditor", new RedoCommand());
            manager.Register("SelectAllInEditor", new SelectAllCommand());
            manager.Register("SelectLineInEditor", new SelectLineCommand());

            manager.Register("ToggleOverstrikeInEditor", new ToggleOverstrikeCommand());
            manager.Register("RemoveLeadingWhitespaceInEditor", new RemoveLeadingWhitespaceCommand());
            manager.Register("RemoveTrailingWhitespaceInEditor", new RemoveTrailingWhitespaceCommand());
            manager.Register("ConvertToUppercaseInEditor", new ConvertToUppercaseCommand());
            manager.Register("ConvertToLowercaseInEditor", new ConvertToLowercaseCommand());
            manager.Register("ConvertToTitleCaseInEditor", new ConvertToTitleCaseCommand());
            manager.Register("InvertCaseInEditor", new InvertCaseCommand());
            manager.Register("ConvertTabsToSpacesInEditor", new ConvertTabsToSpacesCommand());
            manager.Register("ConvertSpacesToTabsInEditor", new ConvertSpacesToTabsCommand());
            manager.Register("ConvertLeadingTabsToSpacesInEditor", new ConvertLeadingTabsToSpacesCommand());
            manager.Register("ConvertLeadingSpacesToTabsInEditor", new ConvertLeadingSpacesToTabsCommand());
            manager.Register("IndentSelectionInEditor", new IndentSelectionCommand());

            // find
            manager.Register("ShowFindPanel", new ShowFindCommand());
            manager.Register("HideFindPanel", new HideFindCommand());
            manager.Register("ToggleFindMatchCase", new ToggleMatchCaseCommand());
            manager.Register("ToggleFindWordSearch", new ToggleWordSearchCommand());
            manager.Register("ToggleFindRegexSearch", new ToggleRegexSearchCommand());
            manager.Register("NextFindResult", new NextResultCommand());
            manager.Register("PrevFindResult", new PrevResultCommand());

            AppLogger.Instance.PushHeader($"Registered {CommandManager.Instance.Count} commands", false);
            foreach (KeyValuePair<string, Command> pair in CommandManager.Instance.Commands) {
                AppLogger.Instance.WriteLine($"{pair.Key}: {pair.Value.GetType()}");
            }

            AppLogger.Instance.PopHeader();
        }
    }
}