using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Items;
using Alfred.Core.Storage;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class PlansViewModel : Observable, IToolbarHost
{
    private readonly Vault _vault;

    public PlansViewModel(Vault vault)
    {
        _vault = vault;
        Actions =
        [
            new ToolbarAction("Copy list", "CopyGlyph", CopyToClipboard),
        ];

        Refresh();
    }

    public IReadOnlyList<ToolbarAction> Actions { get; }

    public string? PrimaryActionName => "New plan";

    public event EventHandler? PrimaryRequested;

    public void InvokePrimary() => PrimaryRequested?.Invoke(this, EventArgs.Empty);

    private void CopyToClipboard() =>
        Interop.Clipboards.Set(string.Join(Environment.NewLine, Rows.Select(row => $"- {row.Title}  ({row.Meta})")));

    public ObservableCollection<PlanRow> Rows { get; } = [];

    public bool IsEmpty => Rows.Count == 0;

    public void Add(string title, DateOnly? target)
    {
        _vault.Data.Plans.Add(new Plan { Title = title, Target = target });
        _vault.Save();
        Refresh();
    }

    internal void Remove(Plan plan)
    {
        Recycler.Delete(_vault.Data, plan);
        _vault.Save();
        Refresh();
    }

    internal void Persist() => _vault.Save();

    private void Refresh()
    {
        Rows.Clear();

        foreach (Plan plan in _vault.Data.Plans
            .Where(plan => !plan.Done)
            .OrderBy(plan => plan.Target ?? DateOnly.MaxValue))
        {
            Rows.Add(new PlanRow(plan, this));
        }

        Raise(nameof(IsEmpty));
    }

    public sealed class PlanRow : Observable
    {
        private readonly PlansViewModel _owner;

        public PlanRow(Plan plan, PlansViewModel owner)
        {
            Plan = plan;
            _owner = owner;
            Steps = [.. plan.Steps.Select(step => new StepRow(step, this))];
        }

        public Plan Plan { get; }

        public ObservableCollection<StepRow> Steps { get; }

        public string Title => Plan.Title;

        public string Meta
        {
            get
            {
                int done = Plan.Steps.Count(step => step.Done);
                string progress = Plan.Steps.Count > 0 ? $"{done} of {Plan.Steps.Count}" : "No steps";
                return Plan.Target is { } target
                    ? progress + " · " + target.ToString("d MMM", CultureInfo.InvariantCulture)
                    : progress;
            }
        }

        public bool IsExpanded
        {
            get;
            set => Set(ref field, value);
        }

        public string Notes
        {
            get => Plan.Notes ?? string.Empty;
            set
            {
                Plan.Notes = string.IsNullOrWhiteSpace(value) ? null : value;
                _owner.Persist();
            }
        }

        public void AddStep(string title)
        {
            PlanStep step = new() { Title = title };
            Plan.Steps.Add(step);
            Steps.Add(new StepRow(step, this));
            _owner.Persist();
            Raise(nameof(Meta));
        }

        public void Remove() => _owner.Remove(Plan);

        internal void StepChanged()
        {
            _owner.Persist();
            Raise(nameof(Meta));
        }
    }

    public sealed class StepRow : Observable
    {
        private readonly PlanRow _owner;

        public StepRow(PlanStep step, PlanRow owner)
        {
            Step = step;
            _owner = owner;
        }

        public PlanStep Step { get; }

        public string Title => Step.Title;

        public bool IsDone
        {
            get => Step.Done;
            set
            {
                Step.Done = value;
                _owner.StepChanged();
            }
        }
    }
}
