using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlakeGen.Test
{
    internal sealed class AssertUtil
    {
        internal static bool AreSorted<T>(IEnumerable<T> ids)
        {
            return Enumerable.SequenceEqual(ids, ids.OrderBy(id => id));
        }

        internal static bool AreUnique<T>(IEnumerable<T> ids)
        {
            return ids.Distinct().Count() == ids.Count();
        }
    }
}
