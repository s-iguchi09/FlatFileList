using System.Runtime.InteropServices;

namespace FlatFileList.Utilities.Shell32
{
    /// <summary>
    /// Windows のエクスプローラーと同じ規則(StrCmpLogicalW)で文字列を比較する比較器。
    /// </summary>
    /// <remarks>
    /// 文字列中の数字を数値として比較するため、「file2」は「file10」より前になる。大文字小文字は区別しない。
    /// エクスプローラーの「名前」列の並び順と一致させるため、自前で実装せず Windows の API を使う。
    /// </remarks>
    public sealed class ExplorerStringComparer : IComparer<string?>
    {
        /// <summary>全体で共有するインスタンス(状態を持たないため、任意のスレッドから使える)。</summary>
        public static ExplorerStringComparer Instance { get; } = new();

        /// <summary>
        /// 共有インスタンス以外から生成させないためのコンストラクター。
        /// </summary>
        private ExplorerStringComparer()
        {
        }

        /// <summary>
        /// 2つの文字列をエクスプローラーと同じ規則で比較する。<see langword="null"/> は空文字より前とする。
        /// </summary>
        /// <param name="x">比較対象1。</param>
        /// <param name="y">比較対象2。</param>
        /// <returns>x が前なら負、同じなら 0、後なら正。</returns>
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            return StrCmpLogicalW(x, y);
        }

        /// <summary>
        /// 数字を数値として扱い、大文字小文字を区別せずに2つの文字列を比較する Win32 API(エクスプローラーの並び順)。
        /// </summary>
        /// <param name="psz1">比較対象1。</param>
        /// <param name="psz2">比較対象2。</param>
        /// <returns>psz1 が前なら -1、同じなら 0、後なら 1。</returns>
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);
    }
}
