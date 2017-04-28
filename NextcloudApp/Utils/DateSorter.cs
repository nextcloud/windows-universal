using System;
using System.Collections.Generic;
using System.Linq;
using NextcloudApp.Models;

namespace NextcloudApp.Utils
{
    public class DateSorter : Comparer<FileOrFolder>
    {
        private readonly SortSequence _sortMode;

        public DateSorter(SortSequence sortMode)
        {
            _sortMode = sortMode;
        }

        public override int Compare(FileOrFolder x, FileOrFolder y)
        {
            return _sortMode == SortSequence.Asc
                ? x.LastModified.CompareTo(y.LastModified)
                : y.LastModified.CompareTo(x.LastModified);
        }
    }
}
