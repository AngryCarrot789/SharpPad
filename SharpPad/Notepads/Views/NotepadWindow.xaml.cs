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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Editing;
using SharpPad.Interactivity.Contexts;
using SharpPad.Tasks;
using SharpPad.Themes;
using SharpPad.Utils.RDA;
using SharpPad.Views;

namespace SharpPad.Notepads.Views {
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class NotepadWindow : WindowEx {
        public static readonly DependencyProperty NotepadProperty = DependencyProperty.Register("Notepad", typeof(Notepad), typeof(NotepadWindow), new PropertyMetadata(null, (o, e) => ((NotepadWindow) o).OnNotepadChanged((Notepad) e.OldValue, (Notepad) e.NewValue)));

        public Notepad Notepad {
            get => (Notepad) this.GetValue(NotepadProperty);
            set => this.SetValue(NotepadProperty, value);
        }

        private readonly ContextData contextData;
        private ActivityTask primaryActivity;

        // used to delay the caret text update, just in case an intensive document update happens;
        // we don't wanna update the caret text when it's just gonna be overwritten a few milliseconds later
        private readonly RateLimitedDispatchAction updateCaretTextRDA;
        private NotepadDocument activeDocument;

        public NotepadWindow() {
            DataManager.SetContextData(this, this.contextData = new ContextData().Set(DataKeys.HostWindowKey, this).Clone());
            this.InitializeComponent();
            this.updateCaretTextRDA = RateLimitedDispatchAction.ForDispatcherSync(() => {
                TextArea area = this.PART_NotepadPanel.Editor?.TextArea;
                if (area == null) {
                    this.PART_CaretText.Text = $"-:-";
                }
                else {
                    Caret caret = area.Caret;
                    Selection sel = area.Selection;
                    if (sel.IsEmpty) {
                        this.PART_CaretText.Text = $"{caret.Line}:{caret.Column} ({caret.Offset} offset)";
                    }
                    else {
                        int len = sel.Length;
                        this.PART_CaretText.Text = $"{caret.Line}:{caret.Column} ({caret.Offset - len} offset, {len} chars)";
                    }
                }
            }, TimeSpan.FromSeconds(0.2));
            this.Loaded += this.EditorWindow_Loaded;

            TaskManager taskManager = IoC.TaskManager;
            taskManager.TaskStarted += this.OnTaskStarted;
            taskManager.TaskCompleted += this.OnTaskCompleted;
        }

        private void OnTaskStarted(TaskManager taskmanager, ActivityTask task, int index) {
            if (this.primaryActivity == null || this.primaryActivity.IsCompleted) {
                this.SetActivityTask(task);
            }
        }

        private void OnTaskCompleted(TaskManager taskmanager, ActivityTask task, int index) {
            if (task == this.primaryActivity) {
                // try to access next task
                task = taskmanager.ActiveTasks.Count > 0 ? taskmanager.ActiveTasks[0] : null;
                this.SetActivityTask(task);
            }
        }

        private void SetActivityTask(ActivityTask task) {
            IActivityProgress prog = null;
            if (this.primaryActivity != null) {
                prog = this.primaryActivity.Progress;
                prog.TextChanged -= this.OnPrimaryActivityTextChanged;
                prog.CompletionValueChanged -= this.OnPrimaryActionCompletionValueChanged;
                prog.IsIndeterminateChanged -= this.OnPrimaryActivityIndeterminateChanged;
                prog = null;
            }

            this.primaryActivity = task;
            if (task != null) {
                prog = task.Progress;
                prog.TextChanged += this.OnPrimaryActivityTextChanged;
                prog.CompletionValueChanged += this.OnPrimaryActionCompletionValueChanged;
                prog.IsIndeterminateChanged += this.OnPrimaryActivityIndeterminateChanged;
                this.PART_ActiveBackgroundTaskGrid.Visibility = Visibility.Visible;
            }
            else {
                this.PART_ActiveBackgroundTaskGrid.Visibility = Visibility.Collapsed;
            }

            this.OnPrimaryActivityTextChanged(prog);
            this.OnPrimaryActionCompletionValueChanged(prog);
            this.OnPrimaryActivityIndeterminateChanged(prog);
        }

        private void OnPrimaryActivityTextChanged(IActivityProgress tracker) {
            this.PART_TaskCaption.Text = tracker?.Text ?? "";
        }

        private void OnPrimaryActionCompletionValueChanged(IActivityProgress tracker) {
            this.PART_ActiveBgProgress.Value = tracker?.TotalCompletion ?? 0.0;
        }

        private void OnPrimaryActivityIndeterminateChanged(IActivityProgress tracker) {
            this.PART_ActiveBgProgress.IsIndeterminate = tracker?.IsIndeterminate ?? false;
        }

        protected override Task<bool> OnClosingAsync() {
            this.Notepad = null;
            return Task.FromResult(true);
        }

        private void EditorWindow_Loaded(object sender, RoutedEventArgs e) {
            this.PART_ActiveBackgroundTaskGrid.Visibility = Visibility.Collapsed;
            this.PART_NotepadPanel.Editor.TextArea.Caret.PositionChanged += this.OnCaretChanged;
            this.updateCaretTextRDA.InvokeAsync();
        }

        private void OnCaretChanged(object sender, EventArgs e) => this.updateCaretTextRDA.InvokeAsync();

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.Key == Key.LeftAlt) {
                e.Handled = true;
            }
        }

        private void OnNotepadChanged(Notepad oldNotepad, Notepad newNotepad) {
            this.PART_NotepadPanel.Notepad = newNotepad;
            DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadKey, newNotepad).Clone());
            this.UpdateTitle();
            if (oldNotepad != null)
                oldNotepad.ActiveEditorChanged -= this.OnActiveEditorChanged;
            if (newNotepad != null)
                newNotepad.ActiveEditorChanged += this.OnActiveEditorChanged;
        }

        private void OnActiveEditorChanged(Notepad notepad, NotepadEditor oldEditor, NotepadEditor newEditor) {
            this.OnActiveDocumentChanged(newEditor?.Document);
        }

        private void OnActiveDocumentChanged(NotepadDocument newDocument) {
            if (this.activeDocument != null) {
                this.activeDocument.IsModifiedChanged -= this.OnActiveDocumentIsModifiedChanged;
                this.activeDocument.FilePathChanged -= this.OnActiveDocumentFilePathChanged;
                this.activeDocument = null;
            }

            if ((this.activeDocument = newDocument) != null) {
                newDocument.IsModifiedChanged += this.OnActiveDocumentIsModifiedChanged;
                newDocument.FilePathChanged += this.OnActiveDocumentFilePathChanged;
            }

            this.Dispatcher.InvokeAsync(this.UpdateTitle, DispatcherPriority.Loaded);
        }

        private void OnActiveDocumentFilePathChanged(NotepadDocument document) => this.UpdateTitle();

        private void OnActiveDocumentIsModifiedChanged(NotepadDocument document) {
            this.Dispatcher.InvokeAsync(this.UpdateTitle, DispatcherPriority.Loaded);
        }

        private void UpdateTitle() {
            const string title = "SharpPad v1.0";

            if (this.Notepad?.ActiveEditor?.Document is NotepadDocument document) {
                StringBuilder sb = new StringBuilder(title);
                bool hasName = !string.IsNullOrWhiteSpace(document.DocumentName);
                bool hasPath = !string.IsNullOrWhiteSpace(document.FilePath);
                if (hasName)
                    sb.Append(" - ").Append(document.DocumentName);
                if (hasPath)
                    sb.Append(" (").Append(document.FilePath).Append(')');
                if (document.IsModified)
                    sb.Append('*');
                this.Title = sb.ToString();
            }
            else {
                this.Title = title;
            }
        }

        private void SetThemeClick(object sender, RoutedEventArgs e) {
            ThemeType type;
            switch (((MenuItem) sender).Uid) {
                case "0":
                    type = ThemeType.DeepDark;
                    break;
                case "1":
                    type = ThemeType.SoftDark;
                    break;
                case "2":
                    type = ThemeType.DarkGreyTheme;
                    break;
                case "3":
                    type = ThemeType.GreyTheme;
                    break;
                case "4":
                    type = ThemeType.RedBlackTheme;
                    break;
                case "5":
                    type = ThemeType.LightTheme;
                    break;
                default: return;
            }

            ThemeController.SetTheme(type);
        }
    }
}