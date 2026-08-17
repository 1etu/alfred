using System.Collections.ObjectModel;
using Alfred.Core.Items;
using Alfred.Core.Storage;
using Alfred.UIKit;

namespace Alfred.App.ViewModels;

public sealed class BoardViewModel : Observable
{
    private readonly Vault _vault;
    private int _nextTint;

    public BoardViewModel(Vault vault)
    {
        _vault = vault;
        Columns =
        [
            new ColumnModel(BoardColumn.Backlog, "Backlog", this),
            new ColumnModel(BoardColumn.Doing, "Doing", this),
            new ColumnModel(BoardColumn.Done, "Done", this),
        ];
        Refresh();
    }

    public IReadOnlyList<ColumnModel> Columns { get; }

    public void Add(BoardColumn column, string title)
    {
        _vault.Data.Cards.Add(new BoardCard
        {
            Title = title,
            Column = column,
            Tint = _nextTint++ % 5,
            Order = _vault.Data.Cards.Count(card => card.Column == column),
        });

        _vault.Save();
        Refresh();
    }

    internal void Move(BoardCard card, BoardColumn target)
    {
        if (card.Column == target)
        {
            return;
        }

        card.Column = target;
        card.Order = _vault.Data.Cards.Count(other => other.Column == target);
        _vault.Save();
        Refresh();
    }

    internal void Remove(BoardCard card)
    {
        Recycler.Delete(_vault.Data, card);
        _vault.Save();
        Refresh();
    }

    private void Refresh()
    {
        foreach (ColumnModel column in Columns)
        {
            column.Cards.Clear();

            foreach (BoardCard card in _vault.Data.Cards
                .Where(card => card.Column == column.Column)
                .OrderBy(card => card.Order))
            {
                column.Cards.Add(new CardRow(card, this));
            }
        }
    }

    public sealed class ColumnModel
    {
        private readonly BoardViewModel _owner;

        public ColumnModel(BoardColumn column, string title, BoardViewModel owner)
        {
            Column = column;
            Title = title;
            _owner = owner;
        }

        public BoardColumn Column { get; }

        public string Title { get; }

        public ObservableCollection<CardRow> Cards { get; } = [];

        public void Add(string title) => _owner.Add(Column, title);

        public void Drop(CardRow row) => _owner.Move(row.Card, Column);
    }

    public sealed class CardRow
    {
        private readonly BoardViewModel _owner;

        public CardRow(BoardCard card, BoardViewModel owner)
        {
            Card = card;
            _owner = owner;
        }

        public BoardCard Card { get; }

        public string Title => Card.Title;

        public int Tint => Card.Tint;

        public void Remove() => _owner.Remove(Card);
    }
}
