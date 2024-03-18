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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SharpPad.Controls.Bindings;
using SharpPad.Utils;

namespace SharpPad.Notepads.Controls {
    /// <summary>
    /// A control that gets placed in a notepad text editor
    /// </summary>
    public class FindAndReplaceControl : Control {
        public static readonly DependencyProperty FindModelProperty = DependencyProperty.Register("FindModel", typeof(FindAndReplaceModel), typeof(FindAndReplaceControl), new PropertyMetadata(default(FindAndReplaceModel), (d, e) => ((FindAndReplaceControl) d).OnFindModelChanged((FindAndReplaceModel) e.OldValue, (FindAndReplaceModel) e.NewValue)));
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(FindAndReplaceControl), new PropertyMetadata(BoolBox.False));
        public static readonly DependencyProperty IsRegexFaultedProperty = DependencyProperty.Register("IsRegexFaulted", typeof(bool), typeof(FindAndReplaceControl), new PropertyMetadata(BoolBox.False, null, OnCoerceIsRegexFaulted));
        private static readonly DependencyProperty RegexFaultMessageProperty = DependencyProperty.Register("RegexFaultMessage", typeof(string), typeof(FindAndReplaceControl), new PropertyMetadata(default(string)));

        public FindAndReplaceModel FindModel {
            get => (FindAndReplaceModel) this.GetValue(FindModelProperty);
            set => this.SetValue(FindModelProperty, value);
        }

        public bool IsActive {
            get => (bool) this.GetValue(IsActiveProperty);
            set => this.SetValue(IsActiveProperty, value.Box());
        }

        public bool IsRegexFaulted {
            get => (bool) this.GetValue(IsRegexFaultedProperty);
            set => this.SetValue(IsRegexFaultedProperty, value.Box());
        }

        public string RegexFaultMessage {
            get => (string) this.GetValue(RegexFaultMessageProperty);
            set => this.SetValue(RegexFaultMessageProperty, value);
        }

        public NotepadEditorControl Owner { get; set; }

        private readonly IBinder<FindAndReplaceModel> searchTextBinder = Binders.AccessorAEDPFastStartup<FindAndReplaceModel, string>(TextBox.TextProperty, nameof(FindAndReplaceModel.SearchTextChanged), nameof(FindAndReplaceModel.SearchText));
        private readonly IBinder<FindAndReplaceModel> regexFaultStateBinder = Binders.AccessorAEDPFastStartup<FindAndReplaceModel, bool>(IsRegexFaultedProperty, nameof(FindAndReplaceModel.IsRegexFaultedChanged), nameof(FindAndReplaceModel.IsRegexFaulted));
        private readonly IBinder<FindAndReplaceModel> regexFaultTextBinder = Binders.AccessorAEDPFastStartup<FindAndReplaceModel, string>(RegexFaultMessageProperty, nameof(FindAndReplaceModel.IsRegexFaultedChanged), nameof(FindAndReplaceModel.SearchRegexFaultMessage));
        private TextBox PART_SearchTextBox;
        private TextBlock PART_ResultCountTextBlock;

        public FindAndReplaceControl() {
            this.regexFaultStateBinder.AttachControl(this);
            this.regexFaultTextBinder.AttachControl(this);
        }

        private static object OnCoerceIsRegexFaulted(DependencyObject d, object basevalue) {
            FindAndReplaceControl control = (FindAndReplaceControl) d;
            if (BoolBox.False.Equals(basevalue)) {
                return basevalue;
            }

            // If there's no active editor or its FAR model isn't equal to ours, then don't show any fault
            NotepadEditor editor = control.Owner?.ActiveEditor;
            if (editor == null || editor.FindModel != control.FindModel) {
                return BoolBox.False;
            }

            return basevalue;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            TemplateUtils.GetTemplateChild(this, nameof(this.PART_SearchTextBox), out this.PART_SearchTextBox);
            TemplateUtils.GetTemplateChild(this, nameof(this.PART_ResultCountTextBlock), out this.PART_ResultCountTextBlock);
            this.searchTextBinder.AttachControl(this.PART_SearchTextBox);

            this.PART_SearchTextBox.Tag = "Search document";

            this.UpdateSearchResultText();
        }

        private void OnFindModelChanged(FindAndReplaceModel oldValue, FindAndReplaceModel newValue) {
            if (oldValue != null) {
                this.searchTextBinder.DetachModel();
                this.regexFaultStateBinder.DetachModel();
                this.regexFaultTextBinder.DetachModel();
                oldValue.SearchResultsChanged -= this.UpdateForSearchThingsChanged;
                oldValue.CurrentResultIndexChanged -= this.UpdateForSearchThingsChanged;
                oldValue.IsFindInSelectionChanged -= this.OnIsFindInSelectionChanged;
            }

            if (newValue != null) {
                this.searchTextBinder.AttachModel(newValue);
                this.regexFaultStateBinder.AttachModel(newValue);
                this.regexFaultTextBinder.AttachModel(newValue);
                newValue.SearchResultsChanged += this.UpdateForSearchThingsChanged;
                newValue.CurrentResultIndexChanged += this.UpdateForSearchThingsChanged;
                newValue.IsFindInSelectionChanged += this.OnIsFindInSelectionChanged;
            }

            this.IsEnabled = newValue != null;
            this.UpdateSearchResultText();
        }

        private void OnIsFindInSelectionChanged(FindAndReplaceModel model) {
            // Updates text box hint via the HintedTextBox style
            this.PART_SearchTextBox.Tag = model.IsFindInSelection ? "Search in selection" : "Search document";
        }

        private void UpdateForSearchThingsChanged(FindAndReplaceModel model) => this.UpdateSearchResultText();

        private void UpdateSearchResultText() {
            // The template may have not been applied by the time the find model changes
            if (this.PART_ResultCountTextBlock == null) {
                return;
            }

            FindAndReplaceModel model = this.FindModel;
            if (model == null) {
                return;
            }

            int count = model.Results.Count;

            int index = model.CurrentResultIndex;
            if (index == -1) {
                this.PART_ResultCountTextBlock.Text = $"{count} Results";
            }
            else {
                this.PART_ResultCountTextBlock.Text = $"{index + 1}/{count} Results";
            }
        }

        public void FocusSearchText() {
            this.Dispatcher.InvokeAsync(() => {
                this.PART_SearchTextBox?.Focus();
                this.PART_SearchTextBox?.SelectAll();
            }, DispatcherPriority.Loaded);
        }
    }
}