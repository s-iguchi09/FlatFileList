using FlatFileList.Extensions;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;

namespace FlatFileList.Datas
{
    /// <summary>
    /// ファイル一覧の各行が持つ、ルートからの相対パス上のディレクトリ1階層分を表すデータ。
    /// 列に表示するフォルダー名と、エクスプローラーで開く対象パスを保持する。
    /// </summary>
    public class DirectoryOpenner : INotifyPropertyChanged, IDisposable
    {
#pragma warning disable CS0067
        /// <summary>プロパティ値の変更を通知するイベント。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        /// <summary>保持しているリソースを破棄する。</summary>
        public void Dispose()
        {
            //_disposables.Dispose();
        }

        //private readonly CompositeDisposable _disposables = new();

        private readonly string _rootPath = string.Empty;

        public string Name { get; } = string.Empty;
        public string RelativePath { get; } = string.Empty;
        public string AbsolutePath { get; } = string.Empty;
        public string TargetPath { get; } = string.Empty;

        public bool IsEnabled { get; } = false;

        /// <summary>
        /// ルートパスのみを保持する空のインスタンスを生成する。
        /// </summary>
        /// <param name="rootPath">検索ルートのパス。</param>
        private DirectoryOpenner(string rootPath)
        {
            _rootPath = rootPath;

        }

        /// <summary>
        /// ルートからのディレクトリ階層リストからインスタンスを生成する。
        /// </summary>
        /// <param name="rootPath">検索ルートのパス。</param>
        /// <param name="directories">ルートから対象ディレクトリまでの各階層のフォルダー名。</param>
        public DirectoryOpenner(string rootPath, IEnumerable<string> directories)
            : this(rootPath)
        {
            Name = directories.LastOrDefault(string.Empty);
            IsEnabled = !string.IsNullOrEmpty(Name);

            var relativePath = string.Join("\\", directories);
            RelativePath = $"\\{relativePath}";
            TargetPath = Path.GetFullPath(string.Join("\\", _rootPath, relativePath));
            AbsolutePath = TargetPath;
        }

        /// <summary>
        /// ファイルパスから、その親ディレクトリを表すインスタンスを生成する。
        /// </summary>
        /// <param name="rootPath">検索ルートのパス。</param>
        /// <param name="filePath">対象ファイルの絶対パス。</param>
        public DirectoryOpenner(string rootPath, string filePath)
            : this(rootPath)
        {
            Name = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;
            IsEnabled = !string.IsNullOrEmpty(Name);

            var relativePath = Path.GetDirectoryName(filePath)?.Replace(rootPath, "") ?? string.Empty;
            RelativePath = string.IsNullOrEmpty(relativePath) ? "" : $"\\{relativePath}";
            TargetPath = filePath;
            AbsolutePath = Path.GetDirectoryName(filePath) ?? string.Empty;
        }

        /// <summary>
        /// 列のパディング用に、値を持たない空のインスタンスを生成する。
        /// </summary>
        /// <returns>空の <see cref="DirectoryOpenner"/>。</returns>
        public static DirectoryOpenner CreateEmptyOpenner() => new(string.Empty);
    }
}
