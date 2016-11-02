using System;
using System.Collections.Generic;
using System.Linq;
using NextcloudApp.Models;

namespace NextcloudApp.Utils
{
    public class DateSorter : Comparer<FileOrFolder>
    {
        private readonly SortMode _sortMode;

        public DateSorter(SortMode sortMode)
        {
            _sortMode = sortMode;
        }

        public override int Compare(FileOrFolder x, FileOrFolder y)
        {
            return _sortMode == SortMode.Asc
                ? x.LastModified.CompareTo(y.LastModified)
                : y.LastModified.CompareTo(x.LastModified);
        }
    }
}
