using System.Runtime.InteropServices;

namespace FlatFileList.Utilities.Shell32
{

    /// <summary>
    /// Shell32 の <c>SHObjectProperties</c> を利用して、ファイルやフォルダーのプロパティダイアログを表示するユーティリティ。
    /// </summary>
    public static class ShellObjectProperties
    {
        /// <summary>
        /// エクスプローラーのプロパティダイアログを表示する Win32 API。
        /// </summary>
        /// <param name="hwnd">親ウィンドウのハンドル。</param>
        /// <param name="shopObjectType">対象の種類(ファイルパス・プリンター名・ボリューム GUID のいずれか)。</param>
        /// <param name="pszObjectName">対象オブジェクトの名前(パスなど)。</param>
        /// <param name="pszPropertyPage">最初に表示するタブ名。<see langword="null"/> で既定タブ。</param>
        /// <returns>成功した場合は <see langword="true"/>。</returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHObjectProperties(
            IntPtr hwnd,
            uint shopObjectType,
            string pszObjectName,
            string pszPropertyPage // null で既定タブ
        );

        private const uint SHOP_PRINTERNAME = 0x00000001;
        private const uint SHOP_FILEPATH = 0x00000002;
        private const uint SHOP_VOLUMEGUID = 0x00000004;

        /// <summary>
        /// 指定したファイルまたはフォルダーのプロパティダイアログを表示する。
        /// </summary>
        /// <param name="path">対象のファイルまたはフォルダーのパス。</param>
        /// <param name="parentHwnd">親ウィンドウのハンドル。既定はデスクトップ。</param>
        /// <param name="initialPage">最初に表示するタブ名。<see langword="null"/> で既定タブ。</param>
        /// <returns>ダイアログの表示に成功した場合は <see langword="true"/>。</returns>
        public static bool Show(string path, IntPtr parentHwnd = default, string initialPage = null)
            => SHObjectProperties(parentHwnd, SHOP_FILEPATH, path, initialPage);
    }

}
