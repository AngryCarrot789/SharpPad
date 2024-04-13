using System;
using System.Text;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using SharpPad.Avalonia.Notepads;
using SharpPad.Avalonia.Tasks;
using SharpPad.Avalonia.Themes.Controls;
using SharpPad.Avalonia.Utils.RDA;

namespace SharpPad.Avalonia;

public partial class MainWindow : WindowEx
{
    public static readonly StyledProperty<Notepad> NotepadProperty = AvaloniaProperty.Register<MainWindow, Notepad>("Notepad");

    public Notepad Notepad
    {
        get => this.GetValue(NotepadProperty);
        set => this.SetValue(NotepadProperty, value);
    }

    static MainWindow()
    {
        NotepadProperty.Changed.AddClassHandler<MainWindow, Notepad>((s, e) => s.OnNotepadChanged(e.OldValue.GetValueOrDefault(), e.NewValue.GetValueOrDefault()));
    }

    // private readonly ContextData contextData;
    private ActivityTask primaryActivity;

    // used to delay the caret text update, just in case an intensive document update happens;
    // we don't wanna update the caret text when it's just gonna be overwritten a few milliseconds later
    private readonly RateLimitedDispatchAction updateCaretTextRDA;
    private NotepadDocument activeDocument;

    public MainWindow()
    {
        this.InitializeComponent();
        this.updateCaretTextRDA = RateLimitedDispatchAction.ForDispatcherSync(() =>
        {
            TextArea area = this.PART_NotepadPanel.Editor?.TextArea;
            if (area == null)
            {
                this.PART_CaretText.Text = $"-:-";
            }
            else
            {
                Caret caret = area.Caret;
                Selection sel = area.Selection;
                if (sel.IsEmpty)
                {
                    this.PART_CaretText.Text = $"{caret.Line}:{caret.Column} ({caret.Offset} offset)";
                }
                else if (sel is RectangleSelection rect)
                {
                    TextViewPosition end = rect.EndPosition;
                    int offsetA = area.Document.GetOffset(rect.StartPosition.Location);
                    int offsetB = area.Document.GetOffset(end.Location);
                    this.PART_CaretText.Text = $"{caret.Line}:{caret.Column} ({offsetA} -> {offsetB - end.Column + end.VisualColumn + 1})";
                }
                else
                {
                    int offsetA = area.Document.GetOffset(sel.StartPosition.Location);
                    int offsetB = area.Document.GetOffset(sel.EndPosition.Location);
                    if (offsetA > offsetB)
                        Utils.Maths.Swap(ref offsetA, ref offsetB);
                    this.PART_CaretText.Text = $"{caret.Line}:{caret.Column} ({offsetA} offset, {(offsetB - offsetA)} chars)";
                }
            }
        }, TimeSpan.FromSeconds(0.2));
        this.Loaded += this.EditorWindow_Loaded;

        TaskManager.Instance.TaskStarted += this.OnTaskStarted;
        TaskManager.Instance.TaskCompleted += this.OnTaskCompleted;
    }

    private void OnTaskStarted(TaskManager taskmanager, ActivityTask task, int index)
    {
        if (this.primaryActivity == null || this.primaryActivity.IsCompleted)
        {
            this.SetActivityTask(task);
        }
    }

    private void OnTaskCompleted(TaskManager taskmanager, ActivityTask task, int index)
    {
        if (task == this.primaryActivity)
        {
            // try to access next task
            task = taskmanager.ActiveTasks.Count > 0 ? taskmanager.ActiveTasks[0] : null;
            this.SetActivityTask(task);
        }
    }

    private void SetActivityTask(ActivityTask task)
    {
        IActivityProgress prog = null;
        if (this.primaryActivity != null)
        {
            prog = this.primaryActivity.Progress;
            prog.TextChanged -= this.OnPrimaryActivityTextChanged;
            prog.CompletionValueChanged -= this.OnPrimaryActionCompletionValueChanged;
            prog.IsIndeterminateChanged -= this.OnPrimaryActivityIndeterminateChanged;
            prog = null;
        }

        this.primaryActivity = task;
        if (task != null)
        {
            prog = task.Progress;
            prog.TextChanged += this.OnPrimaryActivityTextChanged;
            prog.CompletionValueChanged += this.OnPrimaryActionCompletionValueChanged;
            prog.IsIndeterminateChanged += this.OnPrimaryActivityIndeterminateChanged;
            this.PART_ActiveBackgroundTaskGrid.IsVisible = true;
        }
        else
        {
            this.PART_ActiveBackgroundTaskGrid.IsVisible = false;
        }

        this.OnPrimaryActivityTextChanged(prog);
        this.OnPrimaryActionCompletionValueChanged(prog);
        this.OnPrimaryActivityIndeterminateChanged(prog);
    }

    private void OnPrimaryActivityTextChanged(IActivityProgress tracker)
    {
        this.PART_TaskCaption.Text = tracker?.Text ?? "";
    }

    private void OnPrimaryActionCompletionValueChanged(IActivityProgress tracker)
    {
        this.PART_ActiveBgProgress.Value = tracker?.TotalCompletion ?? 0.0;
    }

    private void OnPrimaryActivityIndeterminateChanged(IActivityProgress tracker)
    {
        this.PART_ActiveBgProgress.IsIndeterminate = tracker?.IsIndeterminate ?? false;
    }

    private void EditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        this.PART_ActiveBackgroundTaskGrid.IsVisible = false;
        this.PART_NotepadPanel.Editor.TextArea.Caret.PositionChanged += this.OnCaretChanged;
        this.updateCaretTextRDA.InvokeAsync();
    }

    private void OnCaretChanged(object sender, EventArgs e) => this.updateCaretTextRDA.InvokeAsync();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.LeftAlt)
        {
            e.Handled = true;
        }
    }

    private void OnNotepadChanged(Notepad? oldNotepad, Notepad? newNotepad)
    {
        this.PART_NotepadPanel.Notepad = newNotepad;
        this.UpdateTitle();
        if (oldNotepad != null)
            oldNotepad.ActiveEditorChanged -= this.OnActiveEditorChanged;
        if (newNotepad != null)
            newNotepad.ActiveEditorChanged += this.OnActiveEditorChanged;
    }

    private void OnActiveEditorChanged(Notepad notepad, NotepadEditor oldEditor, NotepadEditor newEditor)
    {
        this.OnActiveDocumentChanged(newEditor?.Document);
        if (oldEditor != null)
            oldEditor.DocumentChanged -= this.OnActiveEditorDocumentChanged;
        if (newEditor != null)
            newEditor.DocumentChanged += this.OnActiveEditorDocumentChanged;
    }

    private void OnActiveEditorDocumentChanged(NotepadEditor editor, NotepadDocument olddoc, NotepadDocument newdoc)
    {
    }

    private void OnActiveDocumentChanged(NotepadDocument newDocument)
    {
        if (this.activeDocument != null)
        {
            this.activeDocument.IsModifiedChanged -= this.OnActiveDocumentIsModifiedChanged;
            this.activeDocument.FilePathChanged -= this.OnActiveDocumentFilePathChanged;
            this.activeDocument = null;
        }

        if ((this.activeDocument = newDocument) != null)
        {
            newDocument.IsModifiedChanged += this.OnActiveDocumentIsModifiedChanged;
            newDocument.FilePathChanged += this.OnActiveDocumentFilePathChanged;
        }

        Dispatcher.UIThread.InvokeAsync(this.UpdateTitle, DispatcherPriority.Loaded);
    }

    private void OnActiveDocumentFilePathChanged(NotepadDocument document) => this.UpdateTitle();

    private void OnActiveDocumentIsModifiedChanged(NotepadDocument document)
    {
        Dispatcher.UIThread.InvokeAsync(this.UpdateTitle, DispatcherPriority.Loaded);
    }

    private void UpdateTitle()
    {
        const string title = "SharpPad v1.0";

        if (this.Notepad?.ActiveEditor?.Document is NotepadDocument document)
        {
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
        else
        {
            this.Title = title;
        }
    }
}