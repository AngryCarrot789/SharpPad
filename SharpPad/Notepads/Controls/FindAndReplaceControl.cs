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
using System.Windows;
using System.Windows.Controls;
using SharpPad.Controls.Bindings;
using SharpPad.Utils;

namespace SharpPad.Notepads.Controls {
    /// <summary>
    /// A control that gets placed in a notepad text editor
    /// </summary>
    public class FindAndReplaceControl : Control {
        public static readonly DependencyProperty FindModelProperty = DependencyProperty.Register("FindModel", typeof(FindAndReplaceModel), typeof(FindAndReplaceControl), new PropertyMetadata(default(FindAndReplaceModel), (d, e) => ((FindAndReplaceControl) d).OnFindModelChanged((FindAndReplaceModel) e.OldValue, (FindAndReplaceModel) e.NewValue)));
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(FindAndReplaceControl), new PropertyMetadata(BoolBox.False));

        public FindAndReplaceModel FindModel {
            get => (FindAndReplaceModel) this.GetValue(FindModelProperty);
            set => this.SetValue(FindModelProperty, value);
        }

        public bool IsActive {
            get => (bool) this.GetValue(IsActiveProperty);
            set => this.SetValue(IsActiveProperty, value.Box());
        }

        private readonly IBinder<FindAndReplaceModel> searchTextBinder = Binders.AccessorAEDP<FindAndReplaceModel, string>(TextBox.TextProperty, nameof(FindAndReplaceModel.SearchTextChanged), nameof(FindAndReplaceModel.SearchText));
        private TextBox PART_SearchTextBox;

        public FindAndReplaceControl() {
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.PART_SearchTextBox = this.GetTemplateChild(nameof(this.PART_SearchTextBox)) as TextBox ?? throw new Exception("Missing " + nameof(this.PART_SearchTextBox));

            this.searchTextBinder.AttachControl(this.PART_SearchTextBox);
        }

        private void OnFindModelChanged(FindAndReplaceModel oldValue, FindAndReplaceModel newValue) {
            if (oldValue != null) {
                this.searchTextBinder.DetachModel();
                oldValue.SearchResultsChanged -= this.OnSearchResultsChanged;
            }

            if (newValue != null) {
                this.searchTextBinder.AttachModel(newValue);
                newValue.SearchResultsChanged += this.OnSearchResultsChanged;
            }

            this.IsEnabled = newValue != null;
        }

        private void OnSearchResultsChanged(FindAndReplaceModel model) {
            // update text
        }
    }
}