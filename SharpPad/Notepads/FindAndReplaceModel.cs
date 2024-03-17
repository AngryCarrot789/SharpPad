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
        private readonly object locker;
        private readonly List<TextRange> results;

        private volatile string searchText;
        private volatile string replaceText;
        private volatile bool matchCases;
        private int currentResultIndex;

        public string SearchText {
            get => this.searchText;
            set {
                if (this.searchText == value)
                    return;
                this.UpdateSearch();
                this.searchText = value;
                this.SearchTextChanged?.Invoke(this);
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

        public bool MatchCases {
            get => this.matchCases;
            set {
                if (this.matchCases == value)
                    return;
                this.UpdateSearch();
                this.matchCases = value;
                this.MatchCasesChanged?.Invoke(this);
            }
        }

        public int CurrentResultIndex {
            get => this.currentResultIndex;
            private set {
                if (this.currentResultIndex == value)
                    return;
                this.currentResultIndex = value;
                this.CurrentResultIndexChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Returns a read-only list of text range results, ordered based on the <see cref="TextRange.Index"/> property
        /// </summary>
        public IReadOnlyList<TextRange> Results => this.results;

        public NotepadDocument Document { get; }

        private CancellationTokenSource cancelSearch;
        private volatile bool hasDocumentChangedOnMainThread;

        public event FindAndReplaceEventHandler SearchTextChanged;
        public event FindAndReplaceEventHandler ReplaceTextChanged;
        public event FindAndReplaceEventHandler MatchCasesChanged;
        public event FindAndReplaceEventHandler SearchResultsChanged;
        public event FindAndReplaceEventHandler CurrentResultIndexChanged;

        public FindAndReplaceModel(NotepadDocument document) {
            this.currentResultIndex = -1;
            this.Document = document ?? throw new ArgumentNullException(nameof(document));
            this.queryChangedRlda = RateLimitedDispatchAction.ForDispatcherAsync(this.OnSearchQueryChanged, TimeSpan.FromSeconds(0.15));
            this.locker = new object();
            this.results = new List<TextRange>();
            document.Document.Changed += this.OnDocumentModified;
        }

        public void MoveToNextResult() {
            int resultCount = this.results.Count;
            if (resultCount < 1) {
                this.CurrentResultIndex = -1;
                return;
            }

            int index = this.CurrentResultIndex;
            if (index == -1)
                index = 0;
            else if (index >= (this.results.Count - 1))
                index = 0;

            this.CurrentResultIndex = index;
        }

        private void OnDocumentModified(object sender, DocumentChangeEventArgs e) {
            this.hasDocumentChangedOnMainThread = true;
        }

        public void Reset() {
            this.SearchText = null;
            this.ReplaceText = null;
            this.MatchCases = false;
        }

        /// <summary>
        /// Schedules the search data to be refreshes, meaning the document gets re-searched at some point in the near future
        /// </summary>
        public void UpdateSearch() => this.queryChangedRlda.InvokeAsync();

        private async Task OnSearchQueryChanged() {
            string find = this.searchText;
            if (string.IsNullOrEmpty(find))
                return;

            // TextEditor editor = this.Document.Editors.FirstOrDefault();
            // if (editor == null)
            //     return;

            TextDocument document = this.Document.Document;

            this.cancelSearch?.Dispose();
            this.cancelSearch = new CancellationTokenSource();
            this.results.Clear();
            await TaskManager.Instance.RunTask(async () => {
                ActivityTask task = TaskManager.Instance.CurrentTask;
                List<TextRange> ranges = new List<TextRange>();
                while (!await this.SearchImpl(new SearchOptions(find, this.matchCases), task, document, ranges)) {
                    ranges.Clear();
                    await Task.Delay(100);
                    this.hasDocumentChangedOnMainThread = false;
                }

                this.results.Clear();
                this.results.AddRange(ranges);
            }, this.cancelSearch.Token);
            this.cancelSearch?.Dispose();
            this.CurrentResultIndex = -1;
            this.SearchResultsChanged?.Invoke(this);
        }

        private async Task<bool> SearchImpl(SearchOptions options, ActivityTask task, TextDocument document, List<TextRange> results) {
            int textLen = options.text.Length;
            task.CheckCancelled();
            if (this.hasDocumentChangedOnMainThread)
                return false;

            string text = await IoC.Dispatcher.InvokeAsync(() => document.Text);

            int idx, nextStartIndex = 0, checkState = 0;
            while ((idx = text.IndexOf(options.text, nextStartIndex)) != -1) {
                if (++checkState == 10) {
                    task.CheckCancelled();
                    if (this.hasDocumentChangedOnMainThread)
                        return false;
                }

                results.Add(new TextRange(idx, textLen));
                nextStartIndex = idx + textLen;
            }

            return !this.hasDocumentChangedOnMainThread;
        }

        private readonly struct SearchOptions {
            public readonly string text;
            public readonly bool matchCase;

            public SearchOptions(string text, bool matchCase) {
                this.text = text;
                this.matchCase = matchCase;
            }
        }
    }
}