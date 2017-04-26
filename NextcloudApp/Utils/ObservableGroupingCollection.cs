using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace NextcloudApp.Utils
{
    public class ObservableGroupingCollection<TK, T> where TK : IComparable
    {
        private readonly ObservableCollection<T> _rootCollection;
        private Func<T, TK> _groupFunction;

        private IComparer<T> _sortOrder;

        public ObservableGroupingCollection(ObservableCollection<T> collection)
        {
            _rootCollection = collection;
            _rootCollection.CollectionChanged += _rootCollection_CollectionChanged;
        }

        public ObservableCollection<Grouping<TK, T>> Items { get; private set; }

        private void _rootCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HandleCollectionChanged(e);
        }

        public void ArrangeItems(IComparer<T> sortorder, Func<T, TK> group)
        {
            _sortOrder = sortorder;
            _groupFunction = group;

         var temp = _rootCollection
                .OrderBy(i => i, _sortOrder)
                .GroupBy(_groupFunction)
                .ToList()
                .Select(g => new Grouping<TK, T>(g.Key, g));

            Items = new ObservableCollection<Grouping<TK, T>>(temp);
        }

        private void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var item = (T) e.NewItems[0];
                    var value = _groupFunction.Invoke(item);

                    // find matching group if exists
                    var existingGroup = Items.FirstOrDefault(g => g.Key.Equals(value));

                    if (existingGroup == null)
                    {
                        var newlist = new List<T> {item};

                        // find first group where Key is greater than this key
                        var insertBefore = Items.FirstOrDefault(g => g.Key.CompareTo(value) > 0);
                        if (insertBefore == null)
                        {
                            // not found - add new group to end of list
                            Items.Add(new Grouping<TK, T>(value, newlist));
                        }
                        else
                        {
                            // insert new group at this index
                            Items.Insert(Items.IndexOf(insertBefore), new Grouping<TK, T>(value, newlist));
                        }
                    }
                    else
                    {
                        // find index to insert new item in existing group
                        var index = existingGroup.ToList().BinarySearch(item, _sortOrder);
                        if (index < 0)
                        {
                            existingGroup.Insert(~index, item);
                        }
                    }
                }
                    break;
                case NotifyCollectionChangedAction.Remove:
                {
                    var item = (T) e.OldItems[0];
                    var value = _groupFunction.Invoke(item);

                    var existingGroup = Items.FirstOrDefault(g => g.Key.Equals(value));

                    if (existingGroup != null)
                    {
                        // find existing item and remove
                        var targetIndex = existingGroup.IndexOf(item);
                        existingGroup.RemoveAt(targetIndex);

                        // remove group if zero items
                        if (existingGroup.Count == 0)
                        {
                            Items.Remove(existingGroup);
                        }
                    }
                }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}