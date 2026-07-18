namespace FlatFileList.Extensions
{
    /// <summary>
    /// <see cref="IEnumerable{T}"/> に対する拡張メソッドを提供する。
    /// .NET Framework には既定値指定版の <c>LastOrDefault</c>/<c>FirstOrDefault</c> が存在しないため補完する。
    /// </summary>
    public static class IEnumerableExtensions
    {
#if NETFRAMEWORK
        /// <summary>
        /// シーケンスの最後の要素を返す。要素が存在しない場合は指定した既定値を返す。
        /// </summary>
        /// <typeparam name="TSource">要素の型。</typeparam>
        /// <param name="source">対象のシーケンス。</param>
        /// <param name="defaultValue">要素が存在しない場合に返す既定値。</param>
        /// <returns>最後の要素、または既定値。</returns>
        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            return !source.Any() ? defaultValue : source.Last();
        }

        /// <summary>
        /// シーケンスの最初の要素を返す。要素が存在しない場合は指定した既定値を返す。
        /// </summary>
        /// <typeparam name="TSource">要素の型。</typeparam>
        /// <param name="source">対象のシーケンス。</param>
        /// <param name="defaultValue">要素が存在しない場合に返す既定値。</param>
        /// <returns>最初の要素、または既定値。</returns>
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            return !source.Any() ? defaultValue : source.First();
        }
#endif
    }
}
