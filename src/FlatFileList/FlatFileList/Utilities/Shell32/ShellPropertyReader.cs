using System.IO;
using System.Runtime.InteropServices;

namespace FlatFileList.Utilities.Shell32
{
    /// <summary>
    /// Shell32 経由でファイルの拡張プロパティを読み出します。
    /// </summary>
    /// <remarks>
    /// Shell.Application と Folder の生成は高コストなため、インスタンスの生存期間中はフォルダー単位でキャッシュして再利用します。
    /// COM オブジェクトを保持するため、生成から <see cref="Dispose"/> までを同一の STA スレッドで完結させてください。
    /// </remarks>
    public sealed class ShellPropertyReader : IDisposable
    {
        private readonly global::Shell32.Shell? _shell;
        private readonly Dictionary<string, global::Shell32.Folder?> _folders = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Shell.Application の COM インスタンスを生成する。生成できない環境では読み出しは常に空文字を返す。
        /// </summary>
        public ShellPropertyReader()
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");

            if (shellAppType is null)
            {
                return;
            }

            _shell = Activator.CreateInstance(shellAppType) as global::Shell32.Shell;
        }

        /// <summary>
        /// 指定ファイルの拡張プロパティ値を、プロパティのインデックス指定で取得する。
        /// フォルダーオブジェクトはインスタンス内でキャッシュして再利用する。
        /// </summary>
        /// <param name="file">対象ファイルの絶対パス。</param>
        /// <param name="propertyIndex">取得する拡張プロパティのインデックス(例: 154 は前回保存日時)。</param>
        /// <returns>プロパティ値の文字列。取得できない場合は空文字。</returns>
        public string GetValue(string file, int propertyIndex)
        {
            if (_shell is null)
            {
                return string.Empty;
            }

            var directory = Path.GetDirectoryName(file);

            if (string.IsNullOrEmpty(directory))
            {
                return string.Empty;
            }

            global::Shell32.FolderItem? folderItem = null;

            try
            {
                if (!_folders.TryGetValue(directory, out var folder))
                {
                    folder = _shell.NameSpace(directory);
                    _folders.Add(directory, folder);
                }

                if (folder is null)
                {
                    return string.Empty;
                }

                folderItem = folder.ParseName(Path.GetFileName(file));

                if (folderItem is null)
                {
                    return string.Empty;
                }

                return folder.GetDetailsOf(folderItem, propertyIndex);
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                //NOTE:FolderItem はファイルごとに生成されるため、都度解放しないと1回の走査で数万個溜まる。
                if (folderItem is not null)
                {
                    Marshal.FinalReleaseComObject(folderItem);
                }
            }
        }

        /// <summary>
        /// キャッシュしているフォルダーと Shell の COM オブジェクトをすべて解放する。
        /// </summary>
        public void Dispose()
        {
            foreach (var folder in _folders.Values.Where(f => f is not null))
            {
                Marshal.FinalReleaseComObject(folder!);
            }

            _folders.Clear();

            if (_shell is not null)
            {
                Marshal.FinalReleaseComObject(_shell);
            }
        }
    }
}
