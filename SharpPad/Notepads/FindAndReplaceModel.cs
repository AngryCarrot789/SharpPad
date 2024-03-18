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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using SharpPad.Tasks;
using SharpPad.Utils;
using SharpPad.Utils.RDA;

namespace SharpPad.Notepads {
    public delegate void FindAndReplaceEventHandler(FindAndReplaceModel model);

    public class FindAndReplaceModel {
        private readonly RateLimitedDispatchAction invalidateSearchStateRlda;
        private readonly List<TextRange> results;

        private volatile Regex searchRegex;
        private volatile string searchText;
        private volatile string replaceText;
        private volatile bool isMatchCases;
        private volatile bool isWordSearch;
        private volatile bool isRegexSearch;
        private volatile bool isFindInSelection;
        private int currentResultIndex;
        private bool isRegexFaulted;

        private volatile bool isActiveSearchInvalid;
        private ActivityTask activeTask;
        private readonly object criticalLock; // used to guard critical sections

        public string SearchText {
            get => this.searchText;
            set {
                if (this.searchText == value)
                    return;

                this.searchText = value;
                this.SearchTextChanged?.Invoke(this);
                this.InvalidateSearchStateForPossibleRegexChange();
            }
        }

        public string ReplaceText {
            get => this.replaceText;
            set {
                if (this.replaceText == value)
                    return;
                this.replaceText = value;
                this.ReplaceTextChanged?.Invoke(this);
            }
        }

        public bool IsMatchCases {
            get => this.isMatchCases;
            set {
                if (this.isMatchCases == value)
                    return;
                this.isMatchCases = value;
                this.IsMatchCasesChanged?.Invoke(this);
                this.InvalidateSearchState();
            }
        }

        public bool IsWordSearch {
            get => this.isWordSearch;
            set {
                if (this.isWordSearch == value)
                    return;

                if (value && this.isRegexSearch)
                    throw new InvalidOperationException("Cannot set word search while using regex search");

                this.isWordSearch = value;
                this.IsWordSearchChanged?.Invoke(this);
                this.InvalidateSearchState();
            }
        }

        public bool IsRegexSearch {
            get => this.isRegexSearch;
            set {
                if (this.isRegexSearch == value)
                    return;

                if (value && this.isWordSearch) {
                    // cannot use word search while searching with regex, so disable it.
                    // inlined the IsWordSearch setter, so that we don't call
                    // UpdateSearch again, not that it would matter but still
                    this.isWordSearch = false;
                    this.IsWordSearchChanged?.Invoke(this);
                }

                this.isRegexSearch = value;
                this.IsRegexSearchChanged?.Invoke(this);
                this.InvalidateSearchStateForPossibleRegexChange();
            }
        }

        public bool IsFindInSelection {
            get => this.isFindInSelection;
            set {
                if (this.isFindInSelection == value)
                    return;
                this.isFindInSelection = value;
                this.IsFindInSelectionChanged?.Invoke(this);
                this.InvalidateSearchState();
            }
        }

        /// <summary>
        /// Gets the last regex fault message associated with <see cref="SearchText"/>. This is updated before <see cref="IsRegexFaultedChanged"/> is fired
        /// </summary>
        public string SearchRegexFaultMessage { get; private set; }

        /// <summary>
        /// Gets if the regex query is invalid
        /// </summary>
        public bool IsRegexFaulted {
            get => this.isRegexFaulted;
            private set {
                if (this.isRegexFaulted == value)
                    return;
                if (!value)
                    this.SearchRegexFaultMessage = null;

                this.isRegexFaulted = value;
                this.IsRegexFaultedChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Gets or sets the current search result index, which can be used by the UI to select the text of a specific result
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// The index is not -1 and the value is not within the range of <see cref="Results"/>
        /// </exception>
        public int CurrentResultIndex {
            get => this.currentResultIndex;
            set {
                if (value != -1 && value >= this.results.Count) {
                    throw new IndexOutOfRangeException("Index exceeds the result count");
                }

                if (this.currentResultIndex == value) {
                    return;
                }

                this.currentResultIndex = value;
                this.CurrentResultIndexChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Returns a read-only list of text range results, ordered based on the <see cref="TextRange.Index"/> property
        /// </summary>
        public IReadOnlyList<TextRange> Results => this.results;

        /// <summary>
        /// Gets the editor associated with this model
        /// </summary>
        public NotepadEditor Editor { get; }

        /// <summary>
        /// An event fired when the <see cref="SearchText"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler SearchTextChanged;

        /// <summary>
        /// An event fired when the <see cref="ReplaceText"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler ReplaceTextChanged;

        /// <summary>
        /// An event fired when the <see cref="IsMatchCases"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler IsMatchCasesChanged;

        /// <summary>
        /// An event fired when the <see cref="IsWordSearch"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler IsWordSearchChanged;

        /// <summary>
        /// An event fired when the <see cref="IsRegexSearch"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler IsRegexSearchChanged;

        /// <summary>
        /// An event fired when the <see cref="IsFindInSelection"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler IsFindInSelectionChanged;

        /// <summary>
        /// An event fired when <see cref="IsRegexSearch"/> is true and our <see cref="SearchText"/> changed but was an invalid regex expression
        /// </summary>
        public event FindAndReplaceEventHandler IsRegexFaultedChanged;

        /// <summary>
        /// An event fired when the <see cref="CurrentResultIndex"/> property changes
        /// </summary>
        public event FindAndReplaceEventHandler CurrentResultIndexChanged;

        /// <summary>
        /// An event fired when our <see cref="Results"/> has likely changed
        /// </summary>
        public event FindAndReplaceEventHandler SearchResultsChanged;

        private NotepadDocument myDocument; // updated automatically

        public FindAndReplaceModel(NotepadEditor editor) {
            this.currentResultIndex = -1;
            this.Editor = editor ?? throw new ArgumentNullException(nameof(editor));
            this.invalidateSearchStateRlda = RateLimitedDispatchAction.ForDispatcherAsync(this.OnSearchStateInvalidated, TimeSpan.FromSeconds(0.1));
            this.results = new List<TextRange>();
            this.criticalLock = new object();

            if (editor.Document != null) {
                this.SetDocument(editor.Document);
            }

            // TODO: add a way to unregister this event handler.
            // It is assumed that FindAndReplaceModel is not created outside of the NotepadEditor class
            editor.DocumentChanged += this.OnEditorDocumentChanged;
        }

        private void OnEditorDocumentChanged(NotepadEditor editor, NotepadDocument olddoc, NotepadDocument newdoc) {
            this.SetDocument(newdoc);
            this.InvalidateSearchStateForPossibleRegexChange();
        }

        private void SetDocument(NotepadDocument document) {
            if (this.myDocument != null) {
                this.myDocument.Document.Changed += this.OnDocumentModified;
            }

            if ((this.myDocument = document) != null) {
                document.Document.Changed += this.OnDocumentModified;
            }
        }

        private void InvalidateSearchState() {
            lock (this.criticalLock) {
                this.isActiveSearchInvalid = true;
                this.UpdateSearch();
            }
        }

        private void InvalidateSearchStateForPossibleRegexChange() {
            lock (this.criticalLock) {
                if (this.isRegexSearch && !string.IsNullOrWhiteSpace(this.searchText)) {
                    // Check that the regex expression is valid
                    try {
                        this.searchRegex = new Regex(this.searchText, this.isMatchCases ? RegexOptions.Multiline : (RegexOptions.Multiline | RegexOptions.IgnoreCase));
                        this.IsRegexFaulted = false;
                    }
                    catch (ArgumentException e) {
                        this.searchRegex = null;
                        this.SearchRegexFaultMessage = e.Message;
                        this.IsRegexFaulted = true;
                    }
                }
                else {
                    // If search is empty (or not using regex search) then clear any fault state and regex object
                    this.IsRegexFaulted = false;
                    this.searchRegex = null;
                }

                this.UpdateSearch();
            }
        }

        public void MoveToNextResult() {
            int resultCount = this.results.Count;
            if (resultCount < 1) {
                // should be -1 anyway...
                this.CurrentResultIndex = -1;
                return;
            }

            int index = this.CurrentResultIndex + 1;
            if (index >= this.results.Count)
                index = 0;

            this.CurrentResultIndex = index;
        }

        public void MoveToPrevResult() {
            int resultCount = this.results.Count;
            if (resultCount < 1) {
                this.CurrentResultIndex = -1;
                return;
            }

            int index = this.CurrentResultIndex - 1;
            if (index == -1)
                index = this.results.Count - 1;

            this.CurrentResultIndex = index;
        }

        private void OnDocumentModified(object sender, DocumentChangeEventArgs e) {
            lock (this.criticalLock) {
                this.isActiveSearchInvalid = true;
                this.UpdateSearch();
            }
        }

        public void Reset() {
            this.SearchText = null;
            this.ReplaceText = null;
            this.IsMatchCases = false;
            this.IsWordSearch = false;
            this.IsRegexSearch = false;
            this.IsFindInSelection = false;

            this.ClearResults();
        }

        public void ClearResults() {
            this.CurrentResultIndex = -1;
            if (this.results.Count > 0) {
                this.results.Clear();
                this.SearchResultsChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Schedules the search data to be refreshes, meaning the document gets re-searched at some
        /// point in the near future once control is returned back to the dispatcher
        /// </summary>
        public void UpdateSearch() {
            string find = this.searchText;
            if (this.myDocument == null || string.IsNullOrEmpty(find)) {
                this.ClearResults();
                return;
            }

            this.invalidateSearchStateRlda.InvokeAsync();
        }

        private async Task OnSearchStateInvalidated() {
            this.ClearResults();
            if (string.IsNullOrEmpty(this.searchText) || this.myDocument == null) {
                return;
            }

            bool isFaultedRegex = false;
            List<TextRange> ranges = new List<TextRange>();
            this.activeTask = TaskManager.Instance.RunTask(async () => {
                ActivityTask task = TaskManager.Instance.CurrentTask;
                task.Progress.Text = "Searching...";

                // tiny delay to let the progress text update in the UI ;)
                await Task.Delay(5);

                lock (this.criticalLock) {
                    this.isActiveSearchInvalid = false;
                    this.invalidateSearchStateRlda.ClearCriticalState();
                }

                // Keep searching in a loop. SearchImpl returns false when the document is modified externally,
                // meaning previous search results would be invalid, so just forget about them and search again.
                // This could be optimised a lot based on what changed in the document... but it works for now :D
                while (!await (this.isRegexSearch ? this.SearchImpl_Regex(task, ranges) : this.SearchImpl_NonRegex(task, ranges))) {
                    ranges.Clear();

                    task.Progress.Text = "Restarting search...";
                    task.Progress.TotalCompletion = 0;

                    // Add a delay between each search attempt when the document or search query changes
                    await Task.Delay(200, task.CancellationToken);

                    task.Progress.Text = "Searching...";
                    lock (this.criticalLock) {
                        this.isActiveSearchInvalid = false;
                        if (this.isRegexFaulted) {
                            isFaultedRegex = true;
                            return;
                        }

                        // we clear the critical state, because this activity can recover.
                        // There is a tiny window where the activity user code is finished but
                        // the document or search query changes, meaning the activity will get
                        // re-created and the search happens again.
                        // But at a guess, this is maybe a sub-millisecond time window where the user
                        // would have to modify the document or search query riiight as the search is
                        // about to finish, and I'm fine with that being the case, it won't crash :)
                        this.invalidateSearchStateRlda.ClearCriticalState();
                    }
                }
            }, new DefaultProgressTracker(System.Windows.Threading.DispatcherPriority.Background));

            await this.activeTask;

            if (isFaultedRegex) {
                return;
            }

            // This handles that edge case, where the search is invalidated just after
            // search code completes but before the above await statement finishes

            if (!this.isActiveSearchInvalid) {
                this.results.AddRange(ranges);
                this.SearchResultsChanged?.Invoke(this);
            }
            else {
                this.UpdateSearch();
            }
        }

        private readonly struct SearchTextRange {
            public readonly string text;
            public readonly int offsetFromDocumentStart;

            public SearchTextRange(string text, int offsetFromDocumentStart) {
                this.text = text;
                this.offsetFromDocumentStart = offsetFromDocumentStart;
            }
        }

        // Returns:
        //   FALSE: document or search query/settings changed during operation
        //   TRUE: Operation was successful
        private async Task<bool> SearchImpl_NonRegex(ActivityTask task, List<TextRange> results) {
            SearchOptions options = new SearchOptions(this.searchText, this.isMatchCases, this.isWordSearch);
            int textLen = options.textToFind.Length;
            if (textLen < 1) {
                return true;
            }

            lock (this.criticalLock)
                if (this.isActiveSearchInvalid)
                    return false;

            // Get text on main thread
            SearchTextRange segment = await this.GetTextRange();
            if (segment.text == null) {
                return true;
            }

            lock (this.criticalLock)
                if (this.isActiveSearchInvalid)
                    return false;

            // Just in case the document is huge and a match is at the bottom
            // of the document. The index-of scan would take some time
            task.Progress.IsIndeterminate = true;
            int idx, nextIndex = 0, checkCancel = 0, progUpdateCounter = 0;
            while ((idx = DoNonRegexIndexOf(segment.text, nextIndex, ref options)) != -1) {
                if (++checkCancel == 5) {
                    if (this.isActiveSearchInvalid)
                        return false;
                    checkCancel = 0;
                }

                // Updating total completion takes a bit of time to get working,
                // so only start doing it for big operations (with 10000+ results)
                if (++progUpdateCounter == 1000) {
                    progUpdateCounter = 0;
                    task.Progress.IsIndeterminate = false;
                    task.Progress.TotalCompletion = (double) idx / segment.text.Length;
                }

                results.Add(new TextRange(idx + segment.offsetFromDocumentStart, textLen));
                nextIndex = idx + textLen;
            }

            return !this.isActiveSearchInvalid;
        }

        private async Task<bool> SearchImpl_Regex(ActivityTask task, List<TextRange> results) {
            if (string.IsNullOrWhiteSpace(this.searchText) || this.searchRegex == null) {
                return true;
            }

            lock (this.criticalLock)
                if (this.isActiveSearchInvalid)
                    return false;

            // Get text on main thread
            SearchTextRange segment = await this.GetTextRange();
            if (segment.text == null) {
                return true;
            }

            lock (this.criticalLock)
                if (this.isActiveSearchInvalid)
                    return false;

            MatchCollection matches = this.searchRegex.Matches(segment.text);
            task.Progress.Text = "Searching regex...";
            task.Progress.IsIndeterminate = true;

            int checkCancel = 0;
            foreach (Match match in matches) {
                if (++checkCancel == 5) {
                    if (this.isActiveSearchInvalid)
                        return false;
                    checkCancel = 0;
                }

                results.Add(new TextRange(match.Index + segment.offsetFromDocumentStart, match.Length));
            }

            return !this.isActiveSearchInvalid;
        }

        private Task<SearchTextRange> GetTextRange() {
            return IoC.Dispatcher.InvokeAsync(() => {
                NotepadDocument document = this.myDocument;
                if (document == null) {
                    return default;
                }

                // In the off chance that the editor is null, just use the document text
                TextEditor editor = this.Editor.TextEditor;
                if (this.isFindInSelection && editor != null) {
                    ISegment segment = editor.TextArea.Selection.SurroundingSegment;
                    string selection = document.Document.GetText(segment);
                    if (string.IsNullOrEmpty(selection)) {
                        return new SearchTextRange(document.Document.Text, 0);
                    }

                    return new SearchTextRange(selection, segment.Offset);
                }

                return new SearchTextRange(document.Document.Text, 0);
            });
        }

        private static int DoNonRegexIndexOf(string src, int beginIndex, ref SearchOptions options) {
            if (options.isWordSearch) {
                return IndexOfWholeWord(src, ref options, beginIndex);
            }
            else {
                return CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                    src,
                    options.textToFind,
                    beginIndex,
                    src.Length - beginIndex,
                    options.isMatchCase ? CompareOptions.None : CompareOptions.IgnoreCase);
            }
        }

        private static int IndexOfWholeWord(string src, ref SearchOptions options, int i) {
            int srcLen = src.Length, findLen = options.textToFind.Length;
            for (; i < srcLen && (i = src.IndexOf(options.textToFind, i, StringComparison.Ordinal)) >= 0; i++) {
                if ((i == 0 || !char.IsLetterOrDigit(src, i - 1)) && ((i + findLen) == srcLen || !char.IsLetterOrDigit(src, i + findLen))) {
                    return i;
                }
            }

            return -1;
        }

        private readonly struct SearchOptions {
            public readonly string textToFind;
            public readonly bool isMatchCase;
            public readonly bool isWordSearch;

            public SearchOptions(string textToFind, bool isMatchCase, bool isWordSearch) {
                this.textToFind = textToFind;
                this.isMatchCase = isMatchCase;
                this.isWordSearch = isWordSearch;
            }
        }
    }
}