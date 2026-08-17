using System.Collections.ObjectModel;
using Alfred.App.Interop;
using Alfred.Core.Items;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class PlansViewModel : PageViewModel
{
    private readonly Vault _vault;

    public PlansViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavPlans), "PlansIcon")
    {
        _vault = vault;
        Actions =
        [
            new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionCopy), "CopyGlyph", CopyToClipboard),
        ];

        Refresh();
    }

    public override IReadOnlyList<ActionBarItem> Actions { get; }

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

    private void CopyToClipboard() =>
        Clipboards.Set(string.Join(Environment.NewLine, Rows.Select(row => $"- {row.Title}  ({row.Meta})")));

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
                string progress = Plan.Steps.Count > 0
                    ? LocalizationService.Text(LocalizationKeys.PlansProgress, done, Plan.Steps.Count)
                    : LocalizationService.Text(LocalizationKeys.PlansNoSteps);

                return Plan.Target is { } target
                    ? progress + " · " + target.ToString("d MMM", LocalizationService.Current.Culture)
                    : progress;
            }
        }

        public double Progress => Plan.Steps.Count == 0
            ? 0
            : (double)Plan.Steps.Count(step => step.Done) / Plan.Steps.Count;

        public bool IsExpanded
        {
            get;
            set => Set(ref field, value);
        }

        public void AddStep(string title)
        {
            PlanStep step = new() { Title = title };
            Plan.Steps.Add(step);
            Steps.Add(new StepRow(step, this));
            _owner.Persist();
            Raise(nameof(Meta));
            Raise(nameof(Progress));
        }

        public void Remove() => _owner.Remove(Plan);

        internal void StepChanged()
        {
            _owner.Persist();
            Raise(nameof(Meta));
            Raise(nameof(Progress));
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
