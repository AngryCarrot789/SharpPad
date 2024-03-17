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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using SharpPad.Tasks;
using SharpPad.Utils;
using SharpPad.Utils.RDA;

namespace SharpPad.Notepads {
    public delegate void FindAndReplaceEventHandler(FindAndReplaceModel model);

    public class FindAndReplaceModel {
        private readonly RateLimitedDispatchAction queryChangedRlda;
        private readonly List<TextRange> results;

        private string searchText;
        private string replaceText;
        private bool isMatchCases;
        private bool isWordSearch;
        private bool isRegexSearch;
        private int currentResultIndex;

        private CancellationTokenSource cancelSearch;
        private volatile bool hasDocumentChangedOnMainThread;
        private ActivityTask activeTask;
        private readonly object criticalLock; // used to guard critical sections

        public string SearchText {
            get => this.searchText;
            set {
                if (this.searchText == value)
                    return;
                this.searchText = value;
                this.SearchTextChanged?.Invoke(this);
                this.UpdateSearch();
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
                this.UpdateSearch();
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
                this.UpdateSearch();
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
                this.UpdateSearch();
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

        public NotepadDocument Document { get; }

        public event FindAndReplaceEventHandler SearchTextChanged;
        public event FindAndReplaceEventHandler ReplaceTextChanged;
        public event FindAndReplaceEventHandler IsMatchCasesChanged;
        public event FindAndReplaceEventHandler IsWordSearchChanged;
        public event FindAndReplaceEventHandler IsRegexSearchChanged;
        public event FindAndReplaceEventHandler SearchResultsChanged;
        public event FindAndReplaceEventHandler CurrentResultIndexChanged;

        public FindAndReplaceModel(NotepadDocument document) {
            this.currentResultIndex = -1;
            this.Document = document ?? throw new ArgumentNullException(nameof(document));
            this.queryChangedRlda = RateLimitedDispatchAction.ForDispatcherAsync(this.OnSearchQueryChanged_AMT, TimeSpan.FromSeconds(0.25));
            this.results = new List<TextRange>();
            this.criticalLock = new object();
            document.Document.Changed += this.OnDocumentModified;
        }

        public void MoveToNextResult() {
            int resultCount = this.results.Count;
            if (resultCount < 1) {
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
                this.hasDocumentChangedOnMainThread = true;
            }

            this.UpdateSearch();
        }

        public void Reset() {
            this.SearchText = null;
            this.ReplaceText = null;
            this.IsMatchCases = false;
            this.IsWordSearch = false;
            this.IsRegexSearch = false;

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
        /// Schedules the search data to be refreshes, meaning the document gets re-searched at some point in the near future
        /// </summary>
        public void UpdateSearch() {
            string find = this.searchText;
            if (string.IsNullOrEmpty(find)) {
                this.CurrentResultIndex = -1;
                if (this.results.Count > 0) {
                    this.results.Clear();
                    this.SearchResultsChanged?.Invoke(this);
                }

                return;
            }

            this.queryChangedRlda.InvokeAsync();
        }

        private async Task OnSearchQueryChanged_AMT() {
            string find = this.searchText;
            if (string.IsNullOrEmpty(find)) {
                return;
            }

            SearchOptions options = new SearchOptions(find, this.isMatchCases, this.isWordSearch, this.isRegexSearch);

            TextDocument document = this.Document.Document;

            // UNNECESSARY
            // if (this.cancelSearch != null) {
            //     this.cancelSearch.Cancel();
            //     this.cancelSearch.Dispose();
            // }

            this.cancelSearch = new CancellationTokenSource();

            this.results.Clear();
            this.activeTask = TaskManager.Instance.RunTask(async () => {
                ActivityTask task = TaskManager.Instance.CurrentTask;
                task.Progress.Text = "Searching...";

                // tiny delay to let the progress text update in the UI ;)
                await Task.Delay(5);

                List<TextRange> ranges = new List<TextRange>();

                // Keep searching in a loop. SearchImpl returns false when the document is modified externally,
                // meaning previous search results would be invalid, so just forget about them and search again.
                // This could be optimised a lot based on what changed in the document... but it works for now :D
                lock (this.criticalLock) {
                    this.hasDocumentChangedOnMainThread = false;
                    this.queryChangedRlda.ClearCriticalState();
                }

                try {
                    while (!await this.SearchImpl(options, task, document, ranges)) {
                        ranges.Clear();
                        await Task.Delay(100, task.CancellationToken);

                        lock (this.criticalLock) {
                            this.hasDocumentChangedOnMainThread = false;

                            // we clear the critical state, because this activity can recover.
                            // There is a tiny window where the activity user code is finished but
                            // the document or search query changes, meaning the activity will get
                            // re-created and the search happens again.
                            // But at a guess, this is maybe a sub-millisecond time window where the user
                            // would have to modify the document or search query riiight as the search is
                            // about to finish, and I'm fine with that being the case, it won't crash :)
                            this.queryChangedRlda.ClearCriticalState();
                        }
                    }
                }
                finally {
                    this.results.Clear();
                }

                this.results.AddRange(ranges);
            }, this.cancelSearch.Token);

            await this.activeTask;
            if (!this.activeTask.CancellationToken.IsCancellationRequested) {
                this.cancelSearch?.Dispose();
                this.cancelSearch = null;

                this.CurrentResultIndex = -1;
                this.SearchResultsChanged?.Invoke(this);
            }
            else {
                int a = 34;
            }
        }

        private async Task<bool> SearchImpl(SearchOptions options, ActivityTask task, TextDocument document, List<TextRange> results) {
            int textLen = options.textToFind.Length;
            task.CheckCancelled();
            if (this.hasDocumentChangedOnMainThread)
                return false;

            // Get text on main thread
            this.hasDocumentChangedOnMainThread = false;
            string text = await IoC.Dispatcher.InvokeAsync(() => document.Text);
            if (this.hasDocumentChangedOnMainThread)
                return false;

            int idx, nextStartIndex = 0, checkState = 0, progUpdateCounter = 0;
            while ((idx = DoNonRegexIndexOf(text, nextStartIndex, ref options)) != -1) {
                // avoid checking the cancellation/modified state each time, because it will ruin performance
                if (++checkState == 15) {
                    task.CheckCancelled();
                    if (this.hasDocumentChangedOnMainThread)
                        return false;
                    checkState = 0;
                }

                if (++progUpdateCounter == 10000) {
                    progUpdateCounter = 0;
                    task.Progress.TotalCompletion = (double) idx / text.Length;
                }

                results.Add(new TextRange(idx, textLen));
                nextStartIndex = idx + textLen;
            }

            return !this.hasDocumentChangedOnMainThread;
        }

        private static int DoNonRegexIndexOf(string src, int beginIndex, ref SearchOptions options) {
            if (options.isWordSearch) {
                return IndexOfWholeWord(src, options.textToFind, beginIndex);
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

        public static int IndexOfWholeWord(string str, string word, int beginIndex) {
            for (int j = beginIndex; j < str.Length && (j = str.IndexOf(word, j, StringComparison.Ordinal)) >= 0; j++)
                if ((j == 0 || !char.IsLetterOrDigit(str, j - 1)) && (j + word.Length == str.Length || !char.IsLetterOrDigit(str, j + word.Length)))
                    return j;
            return -1;
        }

        private readonly struct SearchOptions {
            public readonly string textToFind;
            public readonly bool isMatchCase;
            public readonly bool isWordSearch;
            public readonly bool isRegexSearch;

            public SearchOptions(string textToFind, bool isMatchCase, bool isWordSearch, bool isRegexSearch) {
                this.textToFind = textToFind;
                this.isMatchCase = isMatchCase;
                this.isWordSearch = isWordSearch;
                this.isRegexSearch = isRegexSearch;
            }
        }
    }
}