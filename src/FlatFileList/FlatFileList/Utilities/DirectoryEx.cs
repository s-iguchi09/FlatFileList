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
    /// アクセス権限のないディレクトリや、読み取りに失敗したディレクトリは読み飛ばして走査を継続する。
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
        /// <see cref="FileSystemEnumerator{TResult}"/> を用いるため、属性・更新日時は列挙時の検索データから取得され、
        /// ファイルごとの追加のシステム呼び出しは発生しない。リパースポイント(ジャンクション等)の配下は辿らない。
        /// 起点ディレクトリへのアクセスが拒否された場合は例外にせず、空の結果を返す(<see cref="EnumerationOptions.IgnoreInaccessible"/>)。
        /// 起点ディレクトリがそれ以外の理由で開けない場合は例外を送出する。
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

            using var enumerator = new FoundFileEnumerator(path, options, shouldRecurse, shouldInclude);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        /// 除外条件を適用しながらファイルを列挙し、子ディレクトリの読み取りエラーではその枝だけを読み飛ばす列挙子。
        /// </summary>
        /// <remarks>
        /// <see cref="EnumerationOptions.IgnoreInaccessible"/> が無視するのはアクセス拒否だけで、
        /// ネットワーク切断(ERROR_NETNAME_DELETED)などの I/O エラーでは例外となり、検索全体が中断される。
        /// <see cref="FileSystemEnumerable{TResult}"/> ではエラー時の継続を指定できないため、列挙子を継承して制御する。
        /// </remarks>
        private sealed class FoundFileEnumerator : FileSystemEnumerator<FoundFile>
        {
            /// <summary>サブディレクトリを走査するかどうかを判定する述語。</summary>
            private readonly FileSystemEnumerable<FoundFile>.FindPredicate _shouldRecurse;

            /// <summary>ファイルを結果に含めるかどうかを判定する述語。</summary>
            private readonly FileSystemEnumerable<FoundFile>.FindPredicate _shouldInclude;

            /// <summary>
            /// 起点ディレクトリを開き終えたかどうか。
            /// 起点ディレクトリは基底クラスのコンストラクター内で開かれるため、その間は <see langword="false"/> のままとなる。
            /// </summary>
            private readonly bool _isRootOpened;

            /// <summary>
            /// 起点ディレクトリと除外条件を指定して列挙子を生成する。
            /// </summary>
            /// <param name="path">走査の起点となるディレクトリのパス。</param>
            /// <param name="options">列挙のオプション。</param>
            /// <param name="shouldRecurse">サブディレクトリを走査するかどうかを判定する述語。</param>
            /// <param name="shouldInclude">ファイルを結果に含めるかどうかを判定する述語。</param>
            public FoundFileEnumerator(string path, EnumerationOptions options, FileSystemEnumerable<FoundFile>.FindPredicate shouldRecurse, FileSystemEnumerable<FoundFile>.FindPredicate shouldInclude)
                : base(path, options)
            {
                _shouldRecurse = shouldRecurse;
                _shouldInclude = shouldInclude;
                _isRootOpened = true;
            }

            /// <summary>
            /// 列挙中のエラーで走査を継続するかどうかを返す。起点ディレクトリを開けない場合のみ例外とする。
            /// </summary>
            /// <remarks>
            /// アクセス拒否は <see cref="EnumerationOptions.IgnoreInaccessible"/> により本メソッドより先に読み飛ばされるため、
            /// 起点ディレクトリへのアクセス拒否は例外にならず、空の結果となる。
            /// </remarks>
            /// <param name="error">Win32 エラーコード。</param>
            /// <returns>継続する(そのディレクトリを読み飛ばす)場合は <see langword="true"/>。</returns>
            protected override bool ContinueOnError(int error) => _isRootOpened;

            /// <summary>
            /// サブディレクトリを走査するかどうかを返す。リパースポイントは辿らず、それ以外は除外条件に従う。
            /// </summary>
            /// <param name="entry">対象のディレクトリ。</param>
            /// <returns>走査する場合は <see langword="true"/>。</returns>
            protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
                => (entry.Attributes & FileAttributes.ReparsePoint) == 0 && _shouldRecurse(ref entry);

            /// <summary>
            /// エントリを結果に含めるかどうかを返す。ディレクトリは含めず、ファイルは除外条件に従う。
            /// </summary>
            /// <param name="entry">対象のエントリ。</param>
            /// <returns>結果に含める場合は <see langword="true"/>。</returns>
            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
                => !entry.IsDirectory && _shouldInclude(ref entry);

            /// <summary>
            /// エントリを結果の <see cref="FoundFile"/> に変換する。
            /// </summary>
            /// <param name="entry">対象のエントリ。</param>
            /// <returns>ファイルの絶対パスと更新日時。</returns>
            protected override FoundFile TransformEntry(ref FileSystemEntry entry)
                => new(entry.ToFullPath(), entry.LastWriteTimeUtc.LocalDateTime);
        }
    }
}
