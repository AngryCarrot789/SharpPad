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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using SharpPad.Avalonia.Controls.Bindings;
using SharpPad.Avalonia.Notepads;
using SharpPad.Avalonia.Utils;

namespace SharpPad.Avalonia.Controls;

public class FindAndReplaceControl : TemplatedControl
{
    public static readonly StyledProperty<FindAndReplaceModel?> FindModelProperty = AvaloniaProperty.Register<FindAndReplaceControl, FindAndReplaceModel?>("FindModel");
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<FindAndReplaceControl, bool>("IsActive");
    public static readonly StyledProperty<bool> IsRegexFaultedProperty = AvaloniaProperty.Register<FindAndReplaceControl, bool>("IsRegexFaulted", coerce:CoerceIsRegexFaulted);
    public static readonly StyledProperty<string?> RegexFaultMessageProperty = AvaloniaProperty.Register<FindAndReplaceControl, string?>("RegexFaultMessage");

    public FindAndReplaceModel? FindModel
    {
        get => this.GetValue(FindModelProperty);
        set => this.SetValue(FindModelProperty, value);
    }

    public bool IsActive
    {
        get => this.GetValue(IsActiveProperty);
        set => this.SetValue(IsActiveProperty, value);
    }

    public bool IsRegexFaulted
    {
        get => this.GetValue(IsRegexFaultedProperty);
        set => this.SetValue(IsRegexFaultedProperty, value);
    }

    public string? RegexFaultMessage
    {
        get => this.GetValue(RegexFaultMessageProperty);
        set => this.SetValue(RegexFaultMessageProperty, value);
    }
    
    public NotepadEditorControl? Owner { get; set; }

    private readonly IBinder<FindAndReplaceModel> searchTextBinder = Binders.AccessorAEDPFastStartup<FindAndReplaceModel, string?>(TextBox.TextProperty, nameof(FindAndReplaceModel.SearchTextChanged), nameof(FindAndReplaceModel.SearchText));
    private readonly IBinder<FindAndReplaceModel> regexFaultStateBinder = Binders.AccessorAEDPFastStartup<FindAndReplaceModel, bool>(IsRegexFaultedProperty, nameof(FindAndReplaceModel.IsRegexFaultedChanged), nameof(FindAndReplaceModel.IsRegexFaulted));
    private readonly IBinder<FindAndReplaceModel> regexFaultTextBinder = Binders.AccessorAEDPFastStartup<FindAndReplaceModel, string?>(RegexFaultMessageProperty, nameof(FindAndReplaceModel.IsRegexFaultedChanged), nameof(FindAndReplaceModel.SearchRegexFaultMessage));
    private TextBox? PART_SearchTextBox;
    private TextBlock? PART_ResultCountTextBlock;

    public FindAndReplaceControl()
    {
        this.regexFaultStateBinder.AttachControl(this);
        this.regexFaultTextBinder.AttachControl(this);
    }

    static FindAndReplaceControl()
    {
        FindModelProperty.Changed.AddClassHandler<FindAndReplaceControl, FindAndReplaceModel?>((sender, e) => sender.OnFindModelChanged(e.OldValue.GetValueOrDefault(), e.NewValue.GetValueOrDefault()));
    }

    private static bool CoerceIsRegexFaulted(AvaloniaObject d, bool value)
    {
        FindAndReplaceControl control = (FindAndReplaceControl) d;
        if (!value)
            return value;

        // If there's no active editor or its FAR model isn't equal to ours, then don't show any fault
        NotepadEditor? editor = control.Owner?.ActiveEditor;
        if (editor == null || editor.FindModel != control.FindModel)
            return false;

        return value;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        e.NameScope.GetTemplateChild(nameof(this.PART_SearchTextBox), out this.PART_SearchTextBox!);
        e.NameScope.GetTemplateChild(nameof(this.PART_ResultCountTextBlock), out this.PART_ResultCountTextBlock!);
        this.searchTextBinder.AttachControl(this.PART_SearchTextBox);

        this.PART_SearchTextBox.Tag = "Search document";

        this.UpdateSearchResultText();
    }

    private void OnFindModelChanged(FindAndReplaceModel? oldValue, FindAndReplaceModel? newValue)
    {
        if (oldValue != null)
        {
            this.searchTextBinder.DetachModel();
            this.regexFaultStateBinder.DetachModel();
            this.regexFaultTextBinder.DetachModel();
            oldValue.SearchResultsChanged -= this.UpdateForSearchThingsChanged;
            oldValue.CurrentResultIndexChanged -= this.UpdateForSearchThingsChanged;
            oldValue.IsFindInSelectionChanged -= this.OnIsFindInSelectionChanged;
        }

        if (newValue != null)
        {
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

    private void OnIsFindInSelectionChanged(FindAndReplaceModel model)
    {
        // Updates text box hint via the HintedTextBox style
        this.PART_SearchTextBox!.Tag = model.IsFindInSelection ? "Search in selection" : "Search document";
    }

    private void UpdateForSearchThingsChanged(FindAndReplaceModel model) => this.UpdateSearchResultText();

    private void UpdateSearchResultText()
    {
        // The template may have not been applied by the time the find model changes
        if (this.PART_ResultCountTextBlock == null)
            return;

        FindAndReplaceModel? model = this.FindModel;
        if (model == null)
            return;

        int count = model.Results.Count;

        int index = model.CurrentResultIndex;
        if (index == -1)
        {
            this.PART_ResultCountTextBlock.Text = $"{count} Results";
        }
        else
        {
            this.PART_ResultCountTextBlock.Text = $"{index + 1}/{count} Results";
        }
    }

    public void FocusSearchText()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            this.PART_SearchTextBox?.Focus();
            this.PART_SearchTextBox?.SelectAll();
        }, DispatcherPriority.Loaded);
    }
}