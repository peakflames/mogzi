using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class AsyncEnumerable
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
