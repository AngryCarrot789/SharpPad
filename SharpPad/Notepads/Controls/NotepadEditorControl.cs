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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using SharpPad.Interactivity;
using SharpPad.Interactivity.Contexts;
using SharpPad.Utils;
using SharpPad.Utils.RDA;

namespace SharpPad.Notepads.Controls {
    /// <summary>
    /// A control that manages a notepad object. This tracks the active editor, handles drag-drop, etc.
    /// <para>
    /// Each notepad window will have one of these, unless I implement a split-screen feature, in which case
    /// where will be multiple instances of this control per window
    /// </para>
    /// </summary>
    public class NotepadEditorControl : Control, INotepadEditorUI {
        public static readonly DependencyProperty NotepadProperty = DependencyProperty.Register("Notepad", typeof(Notepad), typeof(NotepadEditorControl), new PropertyMetadata(null, (d, e) => ((NotepadEditorControl) d).OnNotepadChanged((Notepad) e.OldValue, (Notepad) e.NewValue)));
        public static readonly DependencyProperty IsDroppableTargetOverProperty = DependencyProperty.Register("IsDroppableTargetOver", typeof(bool), typeof(NotepadEditorControl), new PropertyMetadata(BoolBox.False));

        public Notepad Notepad {
            get => (Notepad) this.GetValue(NotepadProperty);
            set => this.SetValue(NotepadProperty, value);
        }

        public bool IsDroppableTargetOver {
            get => (bool) this.GetValue(IsDroppableTargetOverProperty);
            set => this.SetValue(IsDroppableTargetOverProperty, value.Box());
        }

        /// <summary>
        /// Gets the raw text editor control
        /// </summary>
        public TextEditor Editor => this.PART_TextEditor;

        // controls
        private NotepadTabControl PART_TabControl;
        private TextEditor PART_TextEditor;
        private Border PART_FindAndReplacePanel;
        private FindAndReplaceControl PART_FindAndReplaceControl;

        // shit
        private NotepadEditor activeEditor;
        private FindAndReplaceModel activeFindModel; // a ref to activeEditor.FindModel
        private NotepadDocument activeDocument;
        private readonly ContextData contextData;
        private bool isProcessingAsyncDrop;

        // use RDA to prevent lag if the active document changes very quickly, e.g. opening files
        // but the active document is set during each file opened for some reason
        private readonly RapidDispatchAction<NotepadEditor> updateActiveEditorRda;
        private readonly SearchResultBackgroundRenderer searchColorizor;
        // private SearchResultColorizingTransformer findResultOutliner;

        public bool IsVisibleInUI => this.activeEditor != null && this.Notepad is Notepad notepad && this.activeEditor.IsViewedBy(notepad);

        public NotepadEditor ActiveEditor => this.activeEditor;

        public NotepadEditorControl() {
            DataManager.SetContextData(this, this.contextData = new ContextData().Set(DataKeys.UINotepadEditorKey, this).Clone());
            this.updateActiveEditorRda = new RapidDispatchAction<NotepadEditor>(this.SetActiveEditor, DispatcherPriority.Render);
            this.searchColorizor = new SearchResultBackgroundRenderer();
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            TemplateUtils.GetTemplateChild(this, nameof(this.PART_TabControl), out this.PART_TabControl);
            TemplateUtils.GetTemplateChild(this, nameof(this.PART_TextEditor), out this.PART_TextEditor);
            TemplateUtils.GetTemplateChild(this, nameof(this.PART_FindAndReplacePanel), out this.PART_FindAndReplacePanel);
            TemplateUtils.GetTemplateChild(this, nameof(this.PART_FindAndReplaceControl), out this.PART_FindAndReplaceControl);

            this.PART_FindAndReplaceControl.Owner = this;
            TextEditor editor = this.PART_TextEditor;

            editor.Options = new TextEditorOptions() {
            };

            editor.TextArea.TextView.BackgroundRenderers.Add(this.searchColorizor);
            editor.TextArea.SelectionCornerRadius = 0;
            editor.TextArea.SelectionBorder = null;

            // editor.TextArea.TextView.DefaultLineHeight

            // this.PART_TextEditor.TextArea.TextView.LineTransformers.Add(this.findResultOutliner = new SearchResultColorizingTransformer(this));
            this.PART_FindAndReplacePanel.Visibility = Visibility.Collapsed;
            if (this.activeEditor != null)
                this.activeEditor.TextEditor = editor;
        }

        private void OnNotepadChanged(Notepad oldNotepad, Notepad newNotepad) {
            if (oldNotepad != null) {
                oldNotepad.ActiveEditorChanged -= this.OnActiveEditorChanged;
            }

            if (newNotepad != null) {
                newNotepad.ActiveEditorChanged += this.OnActiveEditorChanged;
            }

            using (DataManager.SuspendMergedContextInvalidation(this)) {
                DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadKey, newNotepad).Clone());

                this.PART_TabControl.Notepad = newNotepad;
                this.SetActiveEditor(newNotepad?.ActiveEditor);
            }
        }

        private void OnActiveEditorChanged(object sender, NotepadEditor oldEditor, NotepadEditor newEditor) {
            this.updateActiveEditorRda.InvokeAsync(newEditor);
        }

        public void SetActiveEditor(NotepadEditor editor) {
            using (DataManager.SuspendMergedContextInvalidation(this)) {
                if (this.activeEditor != null) {
                    this.activeEditor.DocumentChanged -= this.OnActiveEditorDocumentChanged;
                    this.activeEditor.IsFindPanelOpenChanged -= this.OnIsFindPanelOpenChanged;
                    this.SetVisibleFindModel(null, false);
                    this.activeEditor.TextEditor = null;
                    this.activeEditor = null;
                }

                if (editor != null) {
                    if (editor.TextEditor != null)
                        throw new InvalidOperationException("Editor is already associated with another control");

                    this.activeEditor = editor;
                    if (this.PART_TextEditor != null)
                        editor.TextEditor = this.PART_TextEditor;
                    editor.DocumentChanged += this.OnActiveEditorDocumentChanged;
                    editor.IsFindPanelOpenChanged += this.OnIsFindPanelOpenChanged;
                    this.SetVisibility(Visibility.Visible);
                    this.SetActiveDocument(editor.Document);
                    if (editor.IsFindPanelOpen)
                        this.SetVisibleFindModel(editor.FindModel, false);
                }
                else {
                    this.SetVisibility(Visibility.Collapsed);
                    this.SetActiveDocument(null);
                    this.PART_TextEditor.Document = null;
                    this.PART_TextEditor.IsEnabled = false;
                }

                DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadEditorKey, editor).Clone());
            }

            this.PART_FindAndReplaceControl.CoerceValue(FindAndReplaceControl.IsRegexFaultedProperty);
        }

        private void OnIsFindPanelOpenChanged(NotepadEditor editor) => this.SetVisibleFindModel(editor.IsFindPanelOpen ? editor.FindModel : null, true);

        private void OnActiveEditorDocumentChanged(NotepadEditor editor, NotepadDocument olddoc, NotepadDocument newDoc) {
            this.SetActiveDocument(newDoc);
            this.SetVisibleFindModel(editor.FindModel, false);
        }

        private void SetActiveDocument(NotepadDocument document) {
            if (this.activeDocument != null) {
                // code...
                this.activeDocument = null;
            }

            if (document != null) {
                this.PART_TextEditor.IsEnabled = true;
                this.PART_TextEditor.Document = document.Document;
                this.activeDocument = document;
            }
            else {
                this.PART_TextEditor.IsEnabled = false;
                this.PART_TextEditor.Document = null;
            }

            DataManager.SetContextData(this, this.contextData.Set(DataKeys.DocumentKey, document).Clone());
        }

        // Sets the find model that is being present. Null hides the find panel, non-null shows it
        private void SetVisibleFindModel(FindAndReplaceModel model, bool focusTextBox) {
            if (this.activeFindModel != null) {
                this.activeFindModel.SearchResultsChanged -= this.OnSearchResultsChanged;
                this.activeFindModel.CurrentResultIndexChanged -= this.OnCurrentResultIndexChanged;
            }

            if ((this.activeFindModel = model) != null) {
                model.SearchResultsChanged += this.OnSearchResultsChanged;
                model.CurrentResultIndexChanged += this.OnCurrentResultIndexChanged;
                this.PART_FindAndReplacePanel.Visibility = Visibility.Visible;
                this.PART_FindAndReplaceControl.FindModel = model;
                if (focusTextBox) {
                    this.FocusFindSearchBox();
                }
            }
            else {
                this.PART_FindAndReplaceControl.FindModel = null;
                this.PART_FindAndReplacePanel.Visibility = Visibility.Collapsed;
                this.PART_TextEditor.Focus();
            }

            if (this.contextData.TryReplace(DataKeys.FindModelKey, model))
                DataManager.SetContextData(this, this.contextData.Clone());

            this.UpdateSearchResultRender();
        }

        private void OnCurrentResultIndexChanged(FindAndReplaceModel model) {
            if (this.PART_TextEditor.TextArea.IsFocused || model.IsFindInSelection) {
                return;
            }

            int index = model.CurrentResultIndex;
            if (index == -1) {
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

            if (index < model.Results.Count) {
                TextRange range = model.Results[index];
                this.MoveToSearchResult(range);
            }
            else {
                this.PART_TextEditor.SelectionLength = 0;
            }
        }

        private void OnSearchResultsChanged(FindAndReplaceModel model) {
            if (!this.PART_TextEditor.TextArea.IsFocused && !model.IsFindInSelection) {
                int caret = this.PART_TextEditor.CaretOffset;
                int selection = this.PART_TextEditor.SelectionLength;
                int currentOffset = caret - selection;
                int index = BinarySearch.IndexOf(model.Results, currentOffset, (e) => e.Index);
                if (index < 0)
                    index = ~index;

                if (index < model.Results.Count) {
                    model.CurrentResultIndex = index;
                }
                else {
                    this.PART_TextEditor.SelectionLength = 0;
                }
            }

            this.UpdateSearchResultRender();

            // this.PART_TextEditor.TextArea.TextView.LineTransformers.Remove(this.findResultOutliner);
            // this.PART_TextEditor.TextArea.TextView.LineTransformers.Add(this.findResultOutliner);
        }

        private void UpdateSearchResultRender() {
            this.searchColorizor.OnSearchUpdated(this.activeFindModel?.Results);
            this.PART_TextEditor?.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        private void MoveToSearchResult(TextRange range) {
            this.PART_TextEditor.Select(range.Index, range.Length);

            TextViewPosition caret = this.PART_TextEditor.TextArea.Caret.Position;
            this.PART_TextEditor.ScrollTo(caret.Line, caret.Column);
        }

        private void SetVisibility(Visibility visibility) {
            this.PART_TextEditor.Visibility = visibility;
            this.PART_TabControl.Visibility = visibility;
        }

        #region Drag dropping

        protected override void OnDragEnter(DragEventArgs e) {
            this.OnDragOver(e);
        }

        protected override void OnDragOver(DragEventArgs e) {
            e.Handled = true;
            if (this.isProcessingAsyncDrop) {
                e.Effects = DragDropEffects.None;
                return;
            }

            EnumDropType inputEffects = DropUtils.GetDropAction((int) e.KeyStates, (EnumDropType) e.Effects);
            if (inputEffects != EnumDropType.None && this.Notepad is Notepad notepad) {
                EnumDropType outputEffects = NotepadDropRegistry.DropRegistry.CanDropNative(notepad, new DataObjectWrapper(e.Data), inputEffects);
                if (outputEffects != EnumDropType.None) {
                    this.OnAcceptDrop();
                    e.Effects = (DragDropEffects) outputEffects;
                }
                else {
                    this.IsDroppableTargetOver = false;
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        private void OnAcceptDrop() {
            if (!this.IsDroppableTargetOver)
                this.IsDroppableTargetOver = true;
        }

        protected override void OnDragLeave(DragEventArgs e) {
            base.OnDragLeave(e);
            this.Dispatcher.Invoke(() => this.ClearValue(IsDroppableTargetOverProperty), DispatcherPriority.Loaded);
        }

        protected override async void OnDrop(DragEventArgs e) {
            base.OnDrop(e);
            e.Handled = true;
            if (this.isProcessingAsyncDrop || !(this.Notepad is Notepad clip))
                return;

            EnumDropType effects = DropUtils.GetDropAction((int) e.KeyStates, (EnumDropType) e.Effects);
            if (e.Effects == DragDropEffects.None)
                return;

            try {
                this.isProcessingAsyncDrop = true;
                await NotepadDropRegistry.DropRegistry.OnDroppedNative(clip, new DataObjectWrapper(e.Data), effects);
            }
            finally {
                this.isProcessingAsyncDrop = false;
                this.IsDroppableTargetOver = false;
            }
        }

        #endregion

        public void FocusFindSearchBox() => this.PART_FindAndReplaceControl?.FocusSearchText();
    }
}