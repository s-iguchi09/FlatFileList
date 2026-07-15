using System.Runtime.InteropServices;

namespace FlatFileList.Utilities.Shell32
{

    public static class ShellObjectProperties
    {
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

        public static bool Show(string path, IntPtr parentHwnd = default, string initialPage = null)
            => SHObjectProperties(parentHwnd, SHOP_FILEPATH, path, initialPage);
    }

}
