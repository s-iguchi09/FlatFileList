namespace FlatFileList.Extensions
{
    public static class IEnumerableExtensions
    {
#if NETFRAMEWORK
        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            return !source.Any() ? defaultValue : source.Last();
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            return !source.Any() ? defaultValue : source.First();
        }
#endif
    }
}
