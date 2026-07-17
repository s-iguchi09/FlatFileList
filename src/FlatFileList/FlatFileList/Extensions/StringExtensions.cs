using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FlatFileList.Extensions
{
    public static class StringExtensions
    {

        private static object _syncObj = new object();
        private static Dictionary<string, BitmapSource?> _extIcons = new Dictionary<string, BitmapSource?>();
        private static BitmapSource? _directoryIcon = null;

        /// <summary>
        /// <see cref="Icon.ExtractAssociatedIcon"/> を介して取得した <see cref="Icon"/> の <see cref="Bitmap"/> を返します。
        /// </summary>
        /// <param name="path"></param>
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

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

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
        public static bool Contains(this string str, char substring) => str.Contains(substring.ToString());
        public static bool Contains(this string str, char substring, StringComparison comp) => str.Contains(substring.ToString(), comp);

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
