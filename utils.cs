using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class Utils
{
    public static async Task<int> IAsyncEnumeratorCount<T>(IAsyncEnumerator<T> asyncEnumerator)
    {
        int count = 0;
        while (await asyncEnumerator.MoveNextAsync())
        {
            count++;
        }
        return count;
    }
    public static async Task<T[]> IAsyncEnumeratorToArray<T>(IAsyncEnumerator<T> asyncEnumerator)
    {
        List<T> tmp = new List<T>();
        while (await asyncEnumerator.MoveNextAsync())
        {
            tmp.Add(asyncEnumerator.Current);
        }
        return tmp.ToArray();
    }
}