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

using SharpPad.Avalonia.CommandSystem.Usages;
using SharpPad.Avalonia.Interactivity.Contexts;

namespace SharpPad.Avalonia.Notepads.Commands;

public class ToggleMatchCaseCommandUsage : BaseToggleButtonCommandUsage
{
    public CommandUsageModelHelper<FindAndReplaceModel> Helper { get; }

    public ToggleMatchCaseCommandUsage() : base("ToggleFindMatchCase")
    {
        this.Helper = new CommandUsageModelHelper<FindAndReplaceModel>();
        this.Helper.Connected += h =>
        {
            h.Model.IsMatchCasesChanged += this.OnIsMatchCasesChanged;
        };

        this.Helper.Disconnected += h =>
        {
            h.Model.IsMatchCasesChanged -= this.OnIsMatchCasesChanged;
        };
    }

    protected override void OnContextChanged()
    {
        base.OnContextChanged();
        this.Helper.OnContextChanged(DataKeys.FindModelKey, this);
    }

    private void OnIsMatchCasesChanged(FindAndReplaceModel findAndReplaceModel) => this.UpdateIsCheckedAndCanExecute();

    public override bool GetRealIsChecked() => this.Helper.Model?.IsMatchCases ?? false;
}

public class ToggleWordSearchCommandUsage : BaseToggleButtonCommandUsage
{
    public CommandUsageModelHelper<FindAndReplaceModel> Helper { get; }

    public ToggleWordSearchCommandUsage() : base("ToggleFindWordSearch")
    {
        this.Helper = new CommandUsageModelHelper<FindAndReplaceModel>();
        this.Helper.Connected += h =>
        {
            h.Model.IsWordSearchChanged += this.UpdateThing;
            h.Model.IsRegexSearchChanged += this.UpdateThing;
        };

        this.Helper.Disconnected += h =>
        {
            h.Model.IsWordSearchChanged -= this.UpdateThing;
            h.Model.IsRegexSearchChanged -= this.UpdateThing;
        };
    }

    protected override void OnContextChanged()
    {
        base.OnContextChanged();
        this.Helper.OnContextChanged(DataKeys.FindModelKey, this);
    }

    private void UpdateThing(FindAndReplaceModel findAndReplaceModel) => this.UpdateIsCheckedAndCanExecute();

    public override bool GetRealIsChecked() => this.Helper.Model?.IsWordSearch ?? false;
}

public class ToggleRegexSearchCommandUsage : BaseToggleButtonCommandUsage
{
    public CommandUsageModelHelper<FindAndReplaceModel> Helper { get; }

    public ToggleRegexSearchCommandUsage() : base("ToggleFindRegexSearch")
    {
        this.Helper = new CommandUsageModelHelper<FindAndReplaceModel>();
        this.Helper.Connected += h =>
        {
            h.Model.IsRegexSearchChanged += this.OnIsRegexSearchChanged;
        };

        this.Helper.Disconnected += h =>
        {
            h.Model.IsRegexSearchChanged -= this.OnIsRegexSearchChanged;
        };
    }

    protected override void OnContextChanged()
    {
        base.OnContextChanged();
        this.Helper.OnContextChanged(DataKeys.FindModelKey, this);
    }

    private void OnIsRegexSearchChanged(FindAndReplaceModel findAndReplaceModel) => this.UpdateIsCheckedAndCanExecute();

    public override bool GetRealIsChecked() => this.Helper.Model?.IsRegexSearch ?? false;
}

public class BaseSearchResultCommandUsage : BasicButtonCommandUsage
{
    public CommandUsageModelHelper<FindAndReplaceModel> Helper { get; }

    public BaseSearchResultCommandUsage(string commandId) : base(commandId)
    {
        this.Helper = new CommandUsageModelHelper<FindAndReplaceModel>();
        this.Helper.Connected += h =>
        {
            h.Model.SearchResultsChanged += this.OnSearchResultsUpdated;
        };

        this.Helper.Disconnected += h =>
        {
            h.Model.SearchResultsChanged -= this.OnSearchResultsUpdated;
        };
    }

    protected override void OnContextChanged()
    {
        base.OnContextChanged();
        this.Helper.OnContextChanged(DataKeys.FindModelKey, this);
    }

    private void OnSearchResultsUpdated(FindAndReplaceModel findAndReplaceModel) => this.UpdateCanExecuteLater();
}

public class ToggleFindInSelectionCommandUsage : BaseToggleButtonCommandUsage
{
    public CommandUsageModelHelper<FindAndReplaceModel> Helper { get; }

    public ToggleFindInSelectionCommandUsage() : base("ToggleFindInSelection")
    {
        this.Helper = new CommandUsageModelHelper<FindAndReplaceModel>();
        this.Helper.Connected += h =>
        {
            h.Model.IsFindInSelectionChanged += this.OsInFindInSelectionChanged;
        };

        this.Helper.Disconnected += h =>
        {
            h.Model.IsFindInSelectionChanged -= this.OsInFindInSelectionChanged;
        };
    }

    protected override void OnContextChanged()
    {
        base.OnContextChanged();
        this.Helper.OnContextChanged(DataKeys.FindModelKey, this);
    }

    private void OsInFindInSelectionChanged(FindAndReplaceModel findAndReplaceModel) => this.UpdateIsCheckedAndCanExecute();

    public override bool GetRealIsChecked() => this.Helper.Model?.IsFindInSelection ?? false;
}

public class PrevFindResultCommandUsage : BaseSearchResultCommandUsage
{
    public PrevFindResultCommandUsage() : base("PrevFindResult")
    {
    }
}

public class NextFindResultCommandUsage : BaseSearchResultCommandUsage
{
    public NextFindResultCommandUsage() : base("NextFindResult")
    {
    }
}