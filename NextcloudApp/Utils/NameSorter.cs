using System;
using System.Collections.Generic;
using System.Linq;
using NextcloudApp.Models;
using System.Globalization;

namespace NextcloudApp.Utils
{
    public class NameSorter : Comparer<FileOrFolder>
    {
        private readonly SortSequence sortMode;
        private readonly StringComparer comparer;

        public NameSorter(SortSequence sortMode)
        {
            this.sortMode = sortMode;
            this.comparer = StringComparer.CurrentCulture;
        }

        public override int Compare(FileOrFolder x, FileOrFolder y)
        {
            int result;

            if (sortMode == SortSequence.Asc)
            {
                result = x.Name.First().ToString().CompareTo(y.Name.First().ToString());

                if (result != 0)
                {
                    return result;
                }

                result = comparer.Compare(x.Name, y.Name);
                return result != 0 ? result : comparer.Compare(x.Name, y.Name);
            }

            result = y.Name.First().ToString().CompareTo(x.Name.First().ToString());

            if (result != 0)
            {
                return result;
            }

            result = comparer.Compare(y.Name, x.Name);
            return result != 0 ? result : comparer.Compare(y.Name, x.Name);
        }
    }
}
