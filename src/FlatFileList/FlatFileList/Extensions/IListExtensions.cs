using System.Collections;

namespace FlatFileList.Extensions
{
    /// <summary>
    /// 非ジェネリックの <see cref="IList"/> に対する拡張メソッドを提供する。
    /// </summary>
    public static class IListExtensions
    {
        /// <summary>
        /// 指定した型に変換します。変換できないデータは無視されます。
        /// </summary>
        /// <typeparam name="T">変換先の型。</typeparam>
        /// <param name="list">対象のリスト。</param>
        /// <returns><typeparamref name="T"/> に変換できた要素だけの列。</returns>
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
