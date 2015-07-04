using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public static class Extensions
    {
        public static void Replace<T>(this List<T> list, int index, List<T> input)
        {
            list.RemoveRange(index, input.Count);
            list.InsertRange(index, input);
        }
    }
}
