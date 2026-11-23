using System.IO;
using System.Runtime.InteropServices;

namespace FlatFileList.Utilities.Shell32
{
    /// <summary>
    /// Windows プロパティシステム(IPropertyStore)から、ドキュメントの「前回保存日時」(System.Document.DateSaved)を読み出す。
    /// </summary>
    /// <remarks>
    /// 以前は Shell.Application の GetDetailsOf で表示用文字列を取得し、書式文字の除去と日時パースを行っていた。
    /// プロパティストアから FILETIME のまま読むことで、文字列化・正規表現・パースが不要になり、表示言語にも依存しない。
    /// 状態を持たないため、COM が初期化されていれば任意のスレッドから並行して呼び出せる。
    /// </remarks>
    public static class DocumentDateSavedReader
    {
        /// <summary>
        /// 前回保存日時を持ちうるファイル(Office 系)の拡張子。これ以外のファイルは問い合わせない。
        /// </summary>
        private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".doc", ".docx", ".docm", ".dot", ".dotx", ".dotm",
            ".xls", ".xlsx", ".xlsm", ".xlsb", ".xlt", ".xltx", ".xltm",
            ".ppt", ".pptx", ".pptm", ".pot", ".potx", ".potm", ".pps", ".ppsx", ".ppsm",
            ".vsd", ".vsdx", ".vsdm", ".vst", ".vstx", ".vstm",
            ".pub", ".mpp",
        };

        /// <summary>
        /// 拡張子による文字列化なしの照合に使う、<see cref="_supportedExtensions"/> の代替ルックアップ。
        /// </summary>
        private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _supportedExtensionsLookup = _supportedExtensions.GetAlternateLookup<ReadOnlySpan<char>>();

        /// <summary>PKEY_Document_DateSaved の書式 ID(FMTID_SummaryInformation)。</summary>
        private static readonly Guid _summaryInformationFormatId = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

        /// <summary>PKEY_Document_DateSaved のプロパティ ID(PIDSI_LASTSAVE_DTM)。</summary>
        private const uint DateSavedPropertyId = 13;

        /// <summary>IPropertyStore のインターフェイス ID。</summary>
        private static readonly Guid _propertyStoreInterfaceId = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

        /// <summary>PROPVARIANT の型: FILETIME。</summary>
        private const ushort VT_FILETIME = 64;

        /// <summary>GETPROPERTYSTOREFLAGS: 既定(読み取り専用・ハンドラーを使用)。</summary>
        private const int GPS_DEFAULT = 0;

        /// <summary>
        /// 指定ファイルが前回保存日時の取得対象(Office 系の拡張子)かどうかを返す。
        /// </summary>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <returns>取得対象の場合は <see langword="true"/>。</returns>
        public static bool IsSupported(string filePath) => _supportedExtensionsLookup.Contains(Path.GetExtension(filePath.AsSpan()));

        /// <summary>
        /// 指定ファイルの前回保存日時を取得する。取得対象外の拡張子や、値を持たない・読み出せない場合は <see langword="null"/>。
        /// </summary>
        /// <param name="filePath">対象ファイルの絶対パス。</param>
        /// <returns>前回保存日時(ローカル時刻・分単位に切り捨て)。取得できない場合は <see langword="null"/>。</returns>
        public static DateTime? Read(string filePath)
        {
            if (!IsSupported(filePath))
            {
                return null;
            }

            IPropertyStore? store = null;

            try
            {
                var interfaceId = _propertyStoreInterfaceId;
                if (SHGetPropertyStoreFromParsingName(filePath, IntPtr.Zero, GPS_DEFAULT, ref interfaceId, out store) < 0 || store is null)
                {
                    return null;
                }

                var key = new PropertyKey { FormatId = _summaryInformationFormatId, PropertyId = DateSavedPropertyId };
                var value = new PropVariant();

                try
                {
                    if (store.GetValue(ref key, out value) < 0 || value.VarType != VT_FILETIME)
                    {
                        return null;
                    }

                    var local = DateTime.FromFileTimeUtc(value.FileTime).ToLocalTime();
                    //NOTE:従来の GetDetailsOf の表示文字列は分単位だったため、比較フィルターの挙動を揃えるために切り捨てる。
                    return new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, DateTimeKind.Local);
                }
                finally
                {
                    PropVariantClear(ref value);
                }
            }
            catch (Exception ex) when (ex is COMException or ArgumentException or ArgumentOutOfRangeException)
            {
                return null;
            }
            finally
            {
                //NOTE:プロパティストアはファイルを開いたまま保持するため、GC を待たずに即時解放する。
                if (store is not null)
                {
                    Marshal.FinalReleaseComObject(store);
                }
            }
        }

        #region ネイティブ定義

        /// <summary>
        /// パスからプロパティストアを取得する Win32 API。
        /// </summary>
        /// <param name="pszPath">対象のパス。</param>
        /// <param name="pbc">バインドコンテキスト(未使用)。</param>
        /// <param name="flags">GETPROPERTYSTOREFLAGS。</param>
        /// <param name="riid">要求するインターフェイス ID。</param>
        /// <param name="ppv">取得したプロパティストア。</param>
        /// <returns>HRESULT。</returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int SHGetPropertyStoreFromParsingName(string pszPath, IntPtr pbc, int flags, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore? ppv);

        /// <summary>
        /// PROPVARIANT が保持するリソースを解放する Win32 API。
        /// </summary>
        /// <param name="pvar">解放対象の PROPVARIANT。</param>
        /// <returns>HRESULT。</returns>
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern int PropVariantClear(ref PropVariant pvar);

        /// <summary>
        /// プロパティを識別するキー(PROPERTYKEY)。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            /// <summary>書式 ID。</summary>
            public Guid FormatId;
            /// <summary>プロパティ ID。</summary>
            public uint PropertyId;
        }

        /// <summary>
        /// プロパティ値(PROPVARIANT)。本クラスで扱う FILETIME 型の読み出しに必要な部分のみを定義する。
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            /// <summary>値の型(VARTYPE)。</summary>
            [FieldOffset(0)]
            public ushort VarType;

            /// <summary>FILETIME 型の値。</summary>
            [FieldOffset(8)]
            public long FileTime;

            /// <summary>共用体の領域確保用(ポインター2つ分のサイズを満たす)。</summary>
            [FieldOffset(8)]
            private IntPtr _pointer1;

            /// <summary>共用体の領域確保用(ポインター2つ分のサイズを満たす)。</summary>
            [FieldOffset(16)]
            private IntPtr _pointer2;
        }

        /// <summary>
        /// プロパティストア(IPropertyStore)の COM インターフェイス定義。
        /// </summary>
        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            /// <summary>保持しているプロパティ数を取得する。</summary>
            /// <param name="count">プロパティ数。</param>
            /// <returns>HRESULT。</returns>
            [PreserveSig]
            int GetCount(out uint count);

            /// <summary>指定位置のプロパティキーを取得する。</summary>
            /// <param name="index">位置。</param>
            /// <param name="key">プロパティキー。</param>
            /// <returns>HRESULT。</returns>
            [PreserveSig]
            int GetAt(uint index, out PropertyKey key);

            /// <summary>指定キーのプロパティ値を取得する。</summary>
            /// <param name="key">プロパティキー。</param>
            /// <param name="value">プロパティ値。</param>
            /// <returns>HRESULT。</returns>
            [PreserveSig]
            int GetValue(ref PropertyKey key, out PropVariant value);

            /// <summary>指定キーのプロパティ値を設定する(本クラスでは使用しない)。</summary>
            /// <param name="key">プロパティキー。</param>
            /// <param name="value">プロパティ値。</param>
            /// <returns>HRESULT。</returns>
            [PreserveSig]
            int SetValue(ref PropertyKey key, ref PropVariant value);

            /// <summary>変更を確定する(本クラスでは使用しない)。</summary>
            /// <returns>HRESULT。</returns>
            [PreserveSig]
            int Commit();
        }

        #endregion
    }
}
