using System.Collections;
using System.ComponentModel;
using FlatFileList.Utilities.Shell32;

namespace FlatFileList.Datas
{
    /// <summary>
    /// ファイル一覧の列ソート用に、<see cref="FileProperty"/> を型付きで比較する比較器。
    /// </summary>
    /// <remarks>
    /// SortDescription によるソートは比較のたびにプロパティパスをリフレクションで解決するため、件数が多いと遅い。
    /// 列の SortMemberPath からキーの取り出し方を一度だけ決め、以降は直接プロパティを読んで比較する。
    /// </remarks>
    public sealed class FilePropertyComparer : IComparer
    {
        /// <summary>フォルダー列の SortMemberPath の接頭辞(例: Directory1.Name)。</summary>
        private const string DirectorySortMemberPrefix = "Directory";

        /// <summary>フォルダー列の SortMemberPath の接尾辞。</summary>
        private const string DirectorySortMemberSuffix = ".Name";

        /// <summary>昇順の比較処理。</summary>
        private readonly Comparison<FileProperty> _comparison;

        /// <summary>降順にするかどうか(1: 昇順, -1: 降順)。</summary>
        private readonly int _sign;

        /// <summary>
        /// 比較処理と並び順を指定してインスタンスを生成する。
        /// </summary>
        /// <param name="comparison">昇順の比較処理。</param>
        /// <param name="direction">並び順。</param>
        private FilePropertyComparer(Comparison<FileProperty> comparison, ListSortDirection direction)
        {
            _comparison = comparison;
            _sign = direction == ListSortDirection.Ascending ? 1 : -1;
        }

        /// <summary>
        /// 列の SortMemberPath に対応する比較器を生成する。対応しないパスの場合は <see langword="null"/> を返す。
        /// </summary>
        /// <param name="sortMemberPath">列の SortMemberPath(FileName / LastWriteTime / ModifiedTime / Directory1.Name〜)。</param>
        /// <param name="direction">並び順。</param>
        /// <returns>比較器。対応しない場合は <see langword="null"/>。</returns>
        public static FilePropertyComparer? Create(string? sortMemberPath, ListSortDirection direction)
        {
            //NOTE:フォルダー名は従来の SortDescription(既定の比較 = 現在のカルチャ)と同じ並びになるよう CurrentCulture で比較する。
            var stringComparer = StringComparer.CurrentCulture;

            Comparison<FileProperty>? comparison = sortMemberPath switch
            {
                //NOTE:ファイル名はエクスプローラーと同じ並び(数字を数値として比較。「file2」が「file10」より前)にする。
                nameof(FileProperty.FileName) => (x, y) => ExplorerStringComparer.Instance.Compare(x.FileName, y.FileName),
                nameof(FileProperty.LastWriteTime) => (x, y) => x.LastWriteTime.CompareTo(y.LastWriteTime),
                nameof(FileProperty.ModifiedTime) => (x, y) => Nullable.Compare(x.ModifiedTime, y.ModifiedTime),
                _ => TryGetDirectoryIndex(sortMemberPath, out var index)
                    ? (x, y) => stringComparer.Compare(x.Directories[index].Name, y.Directories[index].Name)
                    : null,
            };

            return comparison is null ? null : new FilePropertyComparer(comparison, direction);
        }

        /// <summary>
        /// 2つの行を比較する。
        /// </summary>
        /// <param name="x">比較対象1。</param>
        /// <param name="y">比較対象2。</param>
        /// <returns>x が前なら負、同じなら 0、後なら正。</returns>
        public int Compare(object? x, object? y)
        {
            if (x is not FileProperty fx || y is not FileProperty fy)
            {
                return 0;
            }

            return _sign * _comparison(fx, fy);
        }

        /// <summary>
        /// フォルダー列の SortMemberPath(Directory{n}.Name)から、<see cref="FileProperty.Directories"/> の添字を取り出す。
        /// </summary>
        /// <param name="sortMemberPath">列の SortMemberPath。</param>
        /// <param name="index">0 始まりの添字。</param>
        /// <returns>フォルダー列のパスだった場合は <see langword="true"/>。</returns>
        private static bool TryGetDirectoryIndex(string? sortMemberPath, out int index)
        {
            index = -1;

            if (sortMemberPath is null
                || !sortMemberPath.StartsWith(DirectorySortMemberPrefix, StringComparison.Ordinal)
                || !sortMemberPath.EndsWith(DirectorySortMemberSuffix, StringComparison.Ordinal))
            {
                return false;
            }

            var numberText = sortMemberPath.AsSpan(DirectorySortMemberPrefix.Length, sortMemberPath.Length - DirectorySortMemberPrefix.Length - DirectorySortMemberSuffix.Length);
            if (!int.TryParse(numberText, out var number) || number < 1 || number > ConstantObject.MaxDirectoryColumnCount)
            {
                return false;
            }

            index = number - 1;
            return true;
        }
    }
}
