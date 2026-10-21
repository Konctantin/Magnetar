using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Magnetar.UI.Common;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        CheckReentrancy();

        bool hasItemsAdded = false;
        foreach (var item in collection)
        {
            Items.Add(item);
            hasItemsAdded = true;
        }

        if (hasItemsAdded)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(collection)));
        }
    }

    public void Reload(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        CheckReentrancy();

        Items.Clear();
        foreach (var item in collection)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool CanMoveUp(T? item)
    {
        if (item == null)
            return false;
        return IndexOf(item) > 0;
    }

    public bool CanMoveDown(T? item)
    {
        if (item == null)
            return false;
        int index = IndexOf(item);
        return index >= 0 && index < Count - 1;
    }

    public void MoveUp(T? item)
    {
        if (item == null)
            return;
        int oldIndex = IndexOf(item);
        if (oldIndex > 0)
        {
            Move(oldIndex, oldIndex - 1);
        }
    }

    public void MoveDown(T? item)
    {
        if (item == null)
            return;
        int oldIndex = IndexOf(item);
        if (oldIndex >= 0 && oldIndex < Count - 1)
        {
            Move(oldIndex, oldIndex + 1);
        }
    }
}
