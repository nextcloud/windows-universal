using System;
using System.Collections.Generic;
using System.Linq;
using NextcloudApp.Models;
using NextcloudClient.Types;

namespace NextcloudApp.Utils
{
    public class SizeSorter : Comparer<FileOrFolder>
    {
        private readonly SortMode _sortMode;

        public SizeSorter(SortMode sortMode)
        {
            _sortMode = sortMode;
        }

        public override int Compare(FileOrFolder x, FileOrFolder y)
        {
            return _sortMode == SortMode.Asc
                ? x.Size.CompareTo(y.Size)
                : y.Size.CompareTo(x.Size);
        }
    }
}
