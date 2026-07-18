using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FlatFileList.Extensions
{
    /// <summary>
    /// パス文字列からアイコンを取得したり、文字列を <see cref="bool"/> に変換したりする拡張メソッドを提供する。
    /// アイコンは拡張子単位でキャッシュして再取得のコストを抑える。
    /// </summary>
    public static class StringExtensions
    {

        private static object _syncObj = new object();
        private static Dictionary<string, BitmapSource?> _extIcons = new Dictionary<string, BitmapSource?>();
        private static BitmapSource? _directoryIcon = null;

        /// <summary>
        /// <see cref="Icon.ExtractAssociatedIcon"/> を介して取得した <see cref="Icon"/> の <see cref="Bitmap"/> を返します。
        /// </summary>
        /// <param name="path">アイコンを取得する対象のファイルまたはフォルダーのパス。</param>
        /// <returns><see cref="Icon"/></returns>
        /// <remarks><see cref="Icon.ExtractAssociatedIcon"/> は速度が遅くなるため2回目以降はメモリ上に拡張子別に保持したIconを返します。 </remarks>
        public static BitmapSource? IconToBitmapSource(this string path)
        {
            if (Directory.Exists(path))
            {
                if (_directoryIcon is null)
                {
                    using var icon = SafeGetExtractAssociatedIcon(path);
                    _directoryIcon = ConvertToBitmapSource(icon?.ToBitmap());

                }
                return _directoryIcon;
            }

            var extension = Path.GetExtension(path).ToLower();
            lock (_syncObj)
            {
                if (_extIcons.TryGetValue(extension, out BitmapSource? val))
                {
                    return val;
                }

                using var icon = SafeGetExtractAssociatedIcon(path);
                _extIcons.Add(extension, ConvertToBitmapSource(icon?.ToBitmap()));
                return _extIcons.Last().Value;
            }
        }

        /// <summary>
        /// <see cref="Icon.ExtractAssociatedIcon"/> を例外を握りつぶして安全に呼び出す。
        /// </summary>
        /// <param name="path">アイコンを取得する対象のパス。</param>
        /// <returns>取得した <see cref="Icon"/>。失敗した場合は <see langword="null"/>。</returns>
        private static Icon? SafeGetExtractAssociatedIcon(string path)
        {
            try
            {
                return Icon.ExtractAssociatedIcon(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// GDI オブジェクトのハンドルを解放する Win32 API。
        /// </summary>
        /// <param name="hObject">解放する GDI オブジェクトのハンドル。</param>
        /// <returns>成功した場合は <see langword="true"/>。</returns>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// <see cref="Bitmap"/> を WPF で扱える <see cref="BitmapSource"/> に変換する。
        /// スレッドを跨いで共有できるよう <see cref="System.Windows.Freezable.Freeze"/> 済みにし、GDI ハンドルは確実に解放する。
        /// </summary>
        /// <param name="bitmap">変換元のビットマップ。</param>
        /// <returns>変換した <see cref="BitmapSource"/>。<paramref name="bitmap"/> が <see langword="null"/> の場合は <see langword="null"/>。</returns>
        private static BitmapSource? ConvertToBitmapSource(Bitmap? bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            var hBitmap = bitmap.GetHbitmap();

            try
            {
                // CreateBitmapSourceFromHBitmap()で System.Windows.Media.Imaging.BitmapSource に変換する
                BitmapSource bitmapsource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                        hBitmap,
                                        IntPtr.Zero,
                                        Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions());

                //NOTE:Freeze することで全行が共有するアイコンの変更追跡が不要になり、スレッドを跨いでの生成も可能になる。
                bitmapsource.Freeze();

                return bitmapsource;
            }
            finally
            {
                //NOTE:GetHbitmap() が返す GDI ハンドルは明示的に解放しないとリークする。
                DeleteObject(hBitmap);
            }
        }

        /// <summary>
        /// 文字列を <see cref="bool"/> に変換する。変換できない場合は指定した既定値を返す。
        /// </summary>
        /// <param name="val">変換元の文字列。</param>
        /// <param name="defaultValue">変換できない場合に返す既定値。</param>
        /// <returns>変換結果、または既定値。</returns>
        public static bool ToBoolean(this string val, bool defaultValue)
        {
            try
            {
                if (val == null || val == bool.TrueString || val == bool.FalseString)
                    return Convert.ToBoolean(val);
                else
                    return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 文字列を <see cref="Nullable{Boolean}"/> に変換する。変換できない場合は <see langword="null"/> を返す。
        /// </summary>
        /// <param name="val">変換元の文字列。</param>
        /// <returns>変換結果。変換できない場合は <see langword="null"/>。</returns>
        public static bool? ToNullableBoolean(this string val)
        {
            try
            {
                if (val == null || val == bool.TrueString || val == bool.FalseString)
                    return Convert.ToBoolean(val);
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

#if NETFRAMEWORK
        /// <summary>
        /// 文字列に指定した文字が含まれるかどうかを返す(.NET Framework 用の補完)。
        /// </summary>
        /// <param name="str">対象の文字列。</param>
        /// <param name="substring">検索する文字。</param>
        /// <returns>含まれる場合は <see langword="true"/>。</returns>
        public static bool Contains(this string str, char substring) => str.Contains(substring.ToString());

        /// <summary>
        /// 指定した比較方法で、文字列に指定した文字が含まれるかどうかを返す(.NET Framework 用の補完)。
        /// </summary>
        /// <param name="str">対象の文字列。</param>
        /// <param name="substring">検索する文字。</param>
        /// <param name="comp">文字列の比較方法。</param>
        /// <returns>含まれる場合は <see langword="true"/>。</returns>
        public static bool Contains(this string str, char substring, StringComparison comp) => str.Contains(substring.ToString(), comp);

        /// <summary>
        /// 指定した比較方法で、文字列に指定した部分文字列が含まれるかどうかを返す(.NET Framework 用の補完)。
        /// </summary>
        /// <param name="str">対象の文字列。</param>
        /// <param name="substring">検索する部分文字列。</param>
        /// <param name="comp">文字列の比較方法。</param>
        /// <returns>含まれる場合は <see langword="true"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="substring"/> が <see langword="null"/> の場合。</exception>
        /// <exception cref="ArgumentException"><paramref name="comp"/> が <see cref="StringComparison"/> の定義値でない場合。</exception>
        public static bool Contains(this string str, string substring, StringComparison comp)
        {
            if (substring == null)
                throw new ArgumentNullException("substring",
                                             "substring cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                         "comp");

            return str.IndexOf(substring, comp) >= 0;
        }
#endif
    }
}
