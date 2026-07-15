using FlatFileList.Extensions;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;

namespace FlatFileList.Datas
{
    public class DirectoryOpenner : INotifyPropertyChanged, IDisposable
    {
#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
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

        private DirectoryOpenner(string rootPath)
        {
            _rootPath = rootPath;

        }
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

        public static DirectoryOpenner CreateEmptyOpenner() => new(string.Empty);
    }
}
