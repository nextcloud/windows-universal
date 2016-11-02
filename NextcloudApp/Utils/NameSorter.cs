using System;
using System.Collections.Generic;
using System.Linq;
using NextcloudApp.Models;

namespace NextcloudApp.Utils
{
    public class NameSorter : Comparer<FileOrFolder>
    {
        private readonly SortMode _sortMode;

        public NameSorter(SortMode sortMode)
        {
            _sortMode = sortMode;
        }

        public override int Compare(FileOrFolder x, FileOrFolder y)
        {
            int result;
            if (_sortMode == SortMode.Asc)
            {
                result = x.Name.First().CompareTo(y.Name.First());

                if (result != 0)
                {
                    return result;
                }
                result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);

                return result != 0 ? result : string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
            result = y.Name.First().CompareTo(x.Name.First());

            if (result != 0)
            {
                return result;
            }
            result = string.Compare(y.Name, x.Name, StringComparison.Ordinal);

            return result != 0 ? result : string.Compare(y.Name, x.Name, StringComparison.Ordinal);
        }
    }
}
