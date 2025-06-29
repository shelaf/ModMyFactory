using System;
using System.Collections;
using System.Collections.Generic;
using ModMyFactory.Web;

namespace ModMyFactory.MVVM.Sorters
{
    sealed class FactorioOnlineVersionSorter : IComparer<FactorioOnlineVersion>, IComparer
    {
        public int Compare(FactorioOnlineVersion x, FactorioOnlineVersion y)
        {
            int result = y.Version.CompareTo(x.Version);
            if (result != 0) return result;
            return x.IsExpansion.CompareTo(y.IsExpansion);
        }

        public int Compare(object x, object y)
        {
            if (!(x is FactorioOnlineVersion) || !(y is FactorioOnlineVersion))
                throw new ArgumentException("Parameters need to be of type FactorioVersion.");

            return Compare((FactorioOnlineVersion)x, (FactorioOnlineVersion)y);
        }
    }
}
