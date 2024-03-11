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
using System.IO;
using System.Windows.Threading;
using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;
using SharpPad.Tasks;
using SharpPad.Utils;

namespace SharpPad.Notepads.Commands
{
    public abstract class NotepadCommand : Command
    {
        public override Executability CanExecute(CommandEventArgs e)
        {
            if (!DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                return Executability.Invalid;
            return this.CanExecute(notepad, e);
        }

        protected override void Execute(CommandEventArgs e)
        {
            if (DataKeys.NotepadKey.TryGetContext(e.ContextData, out Notepad notepad))
                this.Execute(notepad, e);
        }

        public abstract Executability CanExecute(Notepad notepad, CommandEventArgs e);

        public abstract void Execute(Notepad notepad, CommandEventArgs e);
    }

    public class NewFileCommand : NotepadCommand
    {
        public override Executability CanExecute(Notepad notepad, CommandEventArgs e)
        {
            return Executability.Valid;
        }

        public override void Execute(Notepad notepad, CommandEventArgs e)
        {
            notepad.AddDocument(new NotepadDocument()
            {
                DocumentName = "New Document " + (notepad.Documents.Count + 1)
            });
        }
    }

    public class OpenFilesCommand : NotepadCommand
    {
        // lazy
        private readonly WeakReference<ActivityTask> currentTask = new WeakReference<ActivityTask>(null);

        public override Executability CanExecute(Notepad notepad, CommandEventArgs e)
        {
            if (this.currentTask.TryGetTarget(out var task) && task.IsRunning)
                return Executability.ValidButCannotExecute;
            return Executability.Valid;
        }

        public override void Execute(Notepad notepad, CommandEventArgs e)
        {
            if (this.currentTask.TryGetTarget(out ActivityTask activityTask) && activityTask.IsRunning)
            {
                IoC.MessageService.ShowMessage("Processing", "Already opening files. Please wait until the last operation has completed");
                return;
            }

            string[] filePaths = IoC.FilePickService.OpenMultipleFiles("Select files to open", Filters.AllAndTextType);
            if (filePaths != null)
                this.currentTask.SetTarget(OpenFiles(notepad, filePaths));
        }

        public static ActivityTask OpenFile(Notepad notepad, string path) => OpenFiles(notepad, new string[] {path});

        public static ActivityTask OpenFiles(Notepad notepad, string[] paths)
        {
            return TaskManager.Instance.RunTask(async () =>
            {
                IActivityProgress progress = TaskManager.Instance.CurrentTask.Progress;
                progress.Text = "Reading files";

                string[] textArray = new string[paths.Length];
                double percentPerFile = 1.0 / paths.Length;
                using (progress.PushCompletionRange(0.0, 0.5))
                {
                    for (int i = 0; i < paths.Length; i++)
                    {
                        textArray[i] = File.ReadAllText(paths[i]);
                        progress.OnProgress(percentPerFile);
                    }
                }

                progress.Text = "Creating tabs";
                NotepadDocument lastDocument = null;
                using (progress.PushCompletionRange(0.5, 1.0))
                {
                    for (int i = 0; i < paths.Length; i++)
                    {
                        string path = paths[i];
                        string text = textArray[i];
                        // Need dispatcher because TextDocument is not thread-safe and also tracks the owner thread
                        await IoC.Dispatcher.InvokeAsync(() =>
                        {
                            NotepadDocument document = new NotepadDocument()
                            {
                                FilePath = path
                            };

                            try
                            {
                                document.Document.Text = text;
                            }
                            catch (Exception ex)
                            {
                                IoC.MessageService.ShowMessage("Error", "Error reading file", ex.GetToString());
                            }

                            document.IsModified = false;
                            notepad.AddDocument(lastDocument = document);
                        }, DispatcherPriority.Loaded);

                        progress.OnProgress(percentPerFile);
                    }
                }

                if (lastDocument != null)
                {
                    await IoC.Dispatcher.InvokeAsync(() =>
                    {
                        notepad.ActiveDocument = lastDocument;
                    });
                }
            });
        }
    }

    public class SaveAllDocumentsCommand : NotepadCommand
    {
        public override Executability CanExecute(Notepad notepad, CommandEventArgs e)
        {
            return Executability.Valid;
        }

        public override void Execute(Notepad notepad, CommandEventArgs e)
        {
            foreach (NotepadDocument document in notepad.Documents)
                SaveDocumentCommand.SaveOrSaveAs(document);
        }
    }
}