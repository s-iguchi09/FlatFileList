using System.IO;

namespace FlatFileList.Utilities
{
    /// <summary>
    /// ディレクトリを再帰的に走査してファイルを列挙するユーティリティ。
    /// アクセス権限のないディレクトリで例外が発生しても走査を継続する。
    /// </summary>
    public static class DirectoryEx
    {
        /// <summary>
        /// 指定パス配下のすべてのファイルを再帰的に列挙する。
        /// </summary>
        /// <param name="path">走査の起点となるパス。</param>
        /// <returns>見つかったファイルの絶対パスの列。</returns>
        public static IEnumerable<string> GetAllFiles(string path)
        {
            return GetAllFiles(path, _ => true);
        }

        /// <summary>
        /// 指定パス配下のすべてのファイルを再帰的に列挙する。<paramref name="predicate"/> が <see langword="false"/> を返すディレクトリは走査対象から除外する。
        /// </summary>
        /// <param name="path">走査の起点となるパス。</param>
        /// <param name="predicate">サブディレクトリを走査するかどうかを判定する述語。</param>
        /// <returns>見つかったファイルの絶対パスの列。</returns>
        public static IEnumerable<string> GetAllFiles(string path, Func<string, bool> predicate)
        {
            if (File.Exists(path))
            {
                yield return path;
            }

            foreach (var fileName in SafeGetFiles(path))
            {
                yield return fileName;
            }


            foreach (var directoryName in SafeGetDirectories(path))
            {
                if (!predicate(directoryName))
                {
                    continue;
                }

                foreach (var fileName in GetAllFiles(directoryName))
                {
                    yield return fileName;
                }
            }
        }

        /// <summary>
        /// 指定パス直下のサブディレクトリを取得する。例外が発生した場合は空の列を返す。
        /// </summary>
        /// <param name="path">対象のディレクトリパス。</param>
        /// <returns>サブディレクトリのパスの列。取得に失敗した場合は空。</returns>
        private static IEnumerable<string> SafeGetDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// 指定パス直下のファイルを取得する。例外が発生した場合は空の列を返す。
        /// </summary>
        /// <param name="path">対象のディレクトリパス。</param>
        /// <returns>ファイルのパスの列。取得に失敗した場合は空。</returns>
        private static IEnumerable<string> SafeGetFiles(string path)
        {
            try
            {
                return Directory.GetFiles(path);
            }
            catch
            {
                return [];
            }
        }
    }
}
