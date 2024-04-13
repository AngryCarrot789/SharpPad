using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using SharpPad.Avalonia.Notepads;
using SharpPad.Avalonia.Utils;
using SharpPad.Avalonia.Utils.RDA;

namespace SharpPad.Avalonia.Controls;

public class NotepadEditorControl : TemplatedControl
{
    public static readonly StyledProperty<Notepad?> NotepadProperty = AvaloniaProperty.Register<NotepadEditorControl, Notepad?>("Notepad");

    public Notepad? Notepad
    {
        get => this.GetValue(NotepadProperty);
        set => this.SetValue(NotepadProperty, value);
    }

    /// <summary>
    /// Gets the raw text editor control
    /// </summary>
    public TextEditor Editor => this.PART_TextEditor;

    // controls
    private NotepadTabControl PART_TabControl;

    private TextEditor PART_TextEditor;
    // private Border PART_FindAndReplacePanel;
    // private FindAndReplaceControl PART_FindAndReplaceControl;

    // shit
    private NotepadEditor activeEditor;
    private FindAndReplaceModel activeFindModel; // a ref to activeEditor.FindModel
    private NotepadDocument activeDocument;

    // use RDA to prevent lag if the active document changes very quickly, e.g. opening files
    // but the active document is set during each file opened for some reason
    private readonly RapidDispatchAction<NotepadEditor> updateActiveEditorRda;
    private readonly SearchResultBackgroundRenderer searchColorizor;
    // private SearchResultColorizingTransformer findResultOutliner;

    public NotepadEditorControl()
    {
        this.updateActiveEditorRda = new RapidDispatchAction<NotepadEditor>(this.SetActiveEditor, DispatcherPriority.Render);
        this.searchColorizor = new SearchResultBackgroundRenderer();
    }

    static NotepadEditorControl() => NotepadProperty.Changed.AddClassHandler<NotepadEditorControl, Notepad>((s, args) => s.OnNotepadChanged(args));

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        e.NameScope.GetTemplateChild(nameof(this.PART_TabControl), out this.PART_TabControl);
        e.NameScope.GetTemplateChild(nameof(this.PART_TextEditor), out this.PART_TextEditor);
        // e.NameScope.GetTemplateChild(nameof(this.PART_FindAndReplacePanel), out this.PART_FindAndReplacePanel);


        this.PART_TextEditor.TextArea.SelectionCornerRadius = 0;
        this.PART_TextEditor.TextArea.SelectionBorder = null;
        if (this.activeEditor != null)
            this.activeEditor.TextEditor = this.PART_TextEditor;
    }

    private void OnNotepadChanged(AvaloniaPropertyChangedEventArgs<Notepad> e)
    {
        if (e.TryGetOldValue(out Notepad oldNotepad))
        {
            oldNotepad.ActiveEditorChanged -= this.OnActiveEditorChanged;
        }

        if (e.TryGetNewValue(out Notepad newNotepad))
        {
            newNotepad.ActiveEditorChanged += this.OnActiveEditorChanged;
        }

        this.PART_TabControl.Notepad = newNotepad;
        this.SetActiveEditor(newNotepad?.ActiveEditor);
    }

    private void OnActiveEditorChanged(object sender, NotepadEditor oldEditor, NotepadEditor newEditor)
    {
        this.updateActiveEditorRda.InvokeAsync(newEditor);
    }

    public void SetActiveEditor(NotepadEditor? editor)
    {
        if (this.activeEditor != null)
        {
            this.activeEditor.DocumentChanged -= this.OnActiveEditorDocumentChanged;
            this.activeEditor.IsFindPanelOpenChanged -= this.OnIsFindPanelOpenChanged;
            this.SetVisibleFindModel(null, false);
            this.activeEditor.TextEditor = null;
            this.activeEditor = null;
        }

        if (editor != null)
        {
            if (editor.TextEditor != null)
                throw new InvalidOperationException("Editor is already associated with another control");

            this.activeEditor = editor;
            if (this.PART_TextEditor != null)
                editor.TextEditor = this.PART_TextEditor;
            editor.DocumentChanged += this.OnActiveEditorDocumentChanged;
            editor.IsFindPanelOpenChanged += this.OnIsFindPanelOpenChanged;
            this.SetVisibility(true);
            this.SetActiveDocument(editor.Document);
            if (editor.IsFindPanelOpen)
                this.SetVisibleFindModel(editor.FindModel, false);
        }
        else
        {
            this.SetVisibility(false);
            this.SetActiveDocument(null);
            this.PART_TextEditor.Document = null;
            this.PART_TextEditor.IsEnabled = false;
        }
    }

    private void OnIsFindPanelOpenChanged(NotepadEditor editor) => this.SetVisibleFindModel(editor.IsFindPanelOpen ? editor.FindModel : null, true);

    private void OnActiveEditorDocumentChanged(NotepadEditor editor, NotepadDocument olddoc, NotepadDocument newDoc)
    {
        this.SetActiveDocument(newDoc);
        this.SetVisibleFindModel(editor.FindModel, false);
    }

    private void SetActiveDocument(NotepadDocument document)
    {
        if (this.activeDocument != null)
        {
            // code...
            this.activeDocument = null;
        }

        if (document != null)
        {
            this.PART_TextEditor.IsEnabled = true;
            this.PART_TextEditor.Document = document.Document;
            this.activeDocument = document;
        }
        else
        {
            this.PART_TextEditor.IsEnabled = false;
            this.PART_TextEditor.Document = null;
        }
    }

    // Sets the find model that is being present. Null hides the find panel, non-null shows it
    private void SetVisibleFindModel(FindAndReplaceModel model, bool focusTextBox)
    {
        if (this.activeFindModel != null)
        {
            this.activeFindModel.SearchResultsChanged -= this.OnSearchResultsChanged;
            this.activeFindModel.CurrentResultIndexChanged -= this.OnCurrentResultIndexChanged;
        }

        if ((this.activeFindModel = model) != null)
        {
            model.SearchResultsChanged += this.OnSearchResultsChanged;
            model.CurrentResultIndexChanged += this.OnCurrentResultIndexChanged;
            if (focusTextBox)
            {
                this.FocusFindSearchBox();
            }
        }
        else
        {
            this.PART_TextEditor.Focus();
        }

        this.UpdateSearchResultRender();
    }

    private void OnCurrentResultIndexChanged(FindAndReplaceModel model)
    {
        if (this.PART_TextEditor.TextArea.IsFocused || model.IsFindInSelection)
        {
            return;
        }

        int index = model.CurrentResultIndex;
        if (index == -1)
        {
            // Instead of defaulting to the first result, we just do nothing. This is to present the GUI from spasming
            // when CurrentResultIndex changes between dispatcher calls, which it does, since:
            // - Find/Search begins: results get cleared, but CurrentResultIndex becomes -1 first,
            //   so GUI scrolls to top if there is at least one result.
            // - Search happens, is uses an activity so it uses the dispatcher to jump between threads
            // - Search completes, some code somewhere (I forgot) sets CurrentResultIndex to the nearest result
            //   nearest to the editor caret, and scrolls to it.
            // When the search operation takes less than maybe half a second, you can see it
            // flash as it scrolls to the top then to the old caret extremely quickly. By doing nothing
            // here, we prevent that happening. Or... could just use an RDAEx on Background to update the caret ;)
            return;
        }

        if (index < model.Results.Count)
        {
            TextRange range = model.Results[index];
            this.MoveToSearchResult(range);
        }
        else
        {
            this.PART_TextEditor.SelectionLength = 0;
        }
    }

    private void OnSearchResultsChanged(FindAndReplaceModel model)
    {
        if (!this.PART_TextEditor.TextArea.IsFocused && !model.IsFindInSelection)
        {
            int caret = this.PART_TextEditor.CaretOffset;
            int selection = this.PART_TextEditor.SelectionLength;
            int currentOffset = caret - selection;
            int index = BinarySearch.IndexOf(model.Results, currentOffset, (e) => e.Index);
            if (index < 0)
                index = ~index;

            if (index < model.Results.Count)
            {
                model.CurrentResultIndex = index;
            }
            else
            {
                this.PART_TextEditor.SelectionLength = 0;
            }
        }

        this.UpdateSearchResultRender();

        // this.PART_TextEditor.TextArea.TextView.LineTransformers.Remove(this.findResultOutliner);
        // this.PART_TextEditor.TextArea.TextView.LineTransformers.Add(this.findResultOutliner);
    }

    private void UpdateSearchResultRender()
    {
        this.searchColorizor.OnSearchUpdated(this.activeFindModel?.Results);
        this.PART_TextEditor?.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    private void MoveToSearchResult(TextRange range)
    {
        this.PART_TextEditor.Select(range.Index, range.Length);

        TextViewPosition caret = this.PART_TextEditor.TextArea.Caret.Position;
        this.PART_TextEditor.ScrollTo(caret.Line, caret.Column);
    }

    private void SetVisibility(bool visibility)
    {
        this.PART_TextEditor.IsVisible = visibility;
        this.PART_TabControl.IsVisible = visibility;
    }

    public void FocusFindSearchBox()
    {
    }
}