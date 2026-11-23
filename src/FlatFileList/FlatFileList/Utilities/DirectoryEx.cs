using System.IO;
using System.IO.Enumeration;

namespace FlatFileList.Utilities
{
    /// <summary>
    /// 検索で見つかった1ファイルの情報。列挙時に得られる値だけを保持し、追加のファイルシステム呼び出しを不要にする。
    /// </summary>
    /// <param name="Path">ファイルの絶対パス。</param>
    /// <param name="LastWriteTime">ファイルの更新日時(ローカル時刻)。</param>
    public readonly record struct FoundFile(string Path, DateTime LastWriteTime);

    /// <summary>
    /// ディレクトリを再帰的に走査してファイルを列挙するユーティリティ。
    /// アクセス権限のないディレクトリは読み飛ばして走査を継続する。
    /// </summary>
    public static class DirectoryEx
    {
        /// <summary>
        /// 指定パス配下のファイルを再帰的に列挙する。
        /// </summary>
        /// <param name="path">走査の起点となるディレクトリのパス。</param>
        /// <param name="shouldRecurse">サブディレクトリを走査するかどうかを判定する述語。<see langword="false"/> のディレクトリは配下ごと除外する。</param>
        /// <param name="shouldInclude">ファイルを結果に含めるかどうかを判定する述語。</param>
        /// <returns>見つかったファイルの列。</returns>
        /// <remarks>
        /// <see cref="FileSystemEnumerable{TResult}"/> を用いるため、属性・更新日時は列挙時の検索データから取得され、
        /// ファイルごとの追加のシステム呼び出しは発生しない。リパースポイント(ジャンクション等)の配下は辿らない。
        /// </remarks>
        public static IEnumerable<FoundFile> EnumerateFiles(string path, FileSystemEnumerable<FoundFile>.FindPredicate shouldRecurse, FileSystemEnumerable<FoundFile>.FindPredicate shouldInclude)
        {
            //NOTE:既定の AttributesToSkip は Hidden|System を読み飛ばすため 0 にし、除外は呼び出し元の述語に任せる。
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
                ReturnSpecialDirectories = false,
            };

            return new FileSystemEnumerable<FoundFile>(
                path,
                static (ref FileSystemEntry entry) => new FoundFile(entry.ToFullPath(), entry.LastWriteTimeUtc.LocalDateTime),
                options)
            {
                ShouldRecursePredicate = shouldRecurse,
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && shouldInclude(ref entry),
            };
        }
    }
}
