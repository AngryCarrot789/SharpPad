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
using Avalonia.Threading;
using SharpPad.Avalonia.CommandSystem;
using SharpPad.Avalonia.Notepads.Commands;
using SharpPad.Avalonia.Services.Files;
using SharpPad.Avalonia.Services.Messages;
using SharpPad.Avalonia.Tasks;

namespace SharpPad.Avalonia;

/// <summary>
/// An application instance for SharpPad
/// </summary>
public class ApplicationCore
{
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

    public Dispatcher Dispatcher { get; }

    public Thread DispatcherThread { get; }

    private ApplicationCore()
    {
        this.Dispatcher = Dispatcher.UIThread;
        this.DispatcherThread = Thread.CurrentThread;
        this.serviceManager = new ServiceManager();
        this.RegisterActions(CommandManager.Instance);
        
        this.serviceManager.Register<IMessageDialogService>(new DummyMessageDialogService());
        this.serviceManager.Register<IUserInputDialogService>(new DummyUserInputDialogService());
        this.serviceManager.Register<IFilePickDialogService>(new DummyFilePickDialogService());
        this.serviceManager.Register<TaskManager>(new TaskManager());
    }

    private class DummyMessageDialogService : IMessageDialogService
    {
        public MessageBoxResult ShowMessage(string caption, string message, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            return MessageBoxResult.None;
        }

        public MessageBoxResult ShowMessage(string caption, string header, string message, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            return MessageBoxResult.None;
        }
    }

    private class DummyUserInputDialogService : IUserInputDialogService
    {
        public string ShowSingleInputDialog(string caption, string message, string defaultInput = null, Predicate<string> validate = null, bool allowEmptyString = false)
        {
            return null;
        }
    }

    private class DummyFilePickDialogService : IFilePickDialogService
    {
        public string OpenFile(string message, string? filter = null, string? initialDirectory = null)
        {
            return null;
        }

        public string[] OpenMultipleFiles(string message, string? filter = null, string? initialDirectory = null)
        {
            return null;
        }

        public string SaveFile(string message, string? filter = null, string? initialFilePath = null)
        {
            return null;
        }
    }

    public void RegisterActions(CommandManager manager)
    {
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
        manager.Register("ToggleFindInSelection", new ToggleFindInSelectionCommand());
        manager.Register("NextFindResult", new NextResultCommand());
        manager.Register("PrevFindResult", new PrevResultCommand());
    }

    public void CheckThreadAccess()
    {
        if (this.DispatcherThread != Thread.CurrentThread)
            throw new InvalidOperationException("Wrong thread: not on application main thread");
    }
}