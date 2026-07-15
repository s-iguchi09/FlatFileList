using System.Collections;

namespace FlatFileList.Extensions
{
    public static class IListExtensions
    {
        /// <summary>
        /// 指定した型に変換します。変換できないデータは無視されます。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<T> SafeCast<T>(this IList list)
        {
            foreach (var index in Enumerable.Range(0, list.Count))
            {
                if (list[index] is T casted)
                {
                    yield return casted;
                }
            }
        }
    }
}
