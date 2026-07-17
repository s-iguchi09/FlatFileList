using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using FlatFileList.Extensions;
using FlatFileList.Utilities.Shell32;
using System.Text.RegularExpressions;

namespace FlatFileList.Datas
{
    public partial class FileProperty : INotifyPropertyChanged, IDisposable
    {
        //NOTE:毎回 new すると走査1回あたり数万回コンパイルが走るため、ソース生成で静的に持つ。
        [GeneratedRegex(@"[^\x20-\x7F]")]
        private static partial Regex NonAsciiRegex();


#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        private readonly CompositeDisposable _disposables = new();
        public void Dispose() => _disposables.Dispose();

        public ReactivePropertySlim<DateTime> LastWriteTime { get; } = new();
        public ReadOnlyReactivePropertySlim<string> LastWriteTimeString { get; }
        public ReactivePropertySlim<DateTime?> ModifiedTime { get; } = new();
        public ReadOnlyReactivePropertySlim<string> ModifiedTimeString { get; }
        public ReactivePropertySlim<bool> IsHighlighted { get; } = new(true);

        public string FilePath { get; }

        public string FileName { get; }
        public string ExtensionText { get; }

        public BitmapSource? FileIcon { get; }

        public List<DirectoryOpenner> Directories { get; }

        public string RelativeFilePath { get; }

        private string _relativeDirectoryPath = string.Empty;

        private readonly string _searchDirectoryPath;

        public FileProperty(string filePath, string searchDirectoryPath)
        {
            _searchDirectoryPath = searchDirectoryPath;

            FilePath = filePath;

            FileName = Path.GetFileName(FilePath);
            ExtensionText = Path.GetExtension(FileName);
            FileIcon = FilePath.IconToBitmapSource();

            var relativeFilePath = FilePath.Replace(Path.GetDirectoryName(_searchDirectoryPath) ?? string.Empty, string.Empty);
            RelativeFilePath = $"\\{relativeFilePath}";
            _relativeDirectoryPath = Path.GetDirectoryName(relativeFilePath).Replace(Path.GetDirectoryName(_searchDirectoryPath) ?? string.Empty, string.Empty);
            var directories = GetDirectories(relativeFilePath);

            Directories = GetDirectoryOpenners(directories).ToList();

            UpdateLastWriteTime();

            //NOTE:ObserveOnUIDispatcher を挟むとファイル1件ごとに Dispatcher へポストされ、走査中のスクロールを阻害する。
            //     WPF のバインディングは PropertyChanged を UI スレッドへ自動でマーシャリングするため、ここでの切り替えは不要。
            LastWriteTimeString = LastWriteTime.Select(dt => dt.ToString(ConstantObject.LastWriteTimeFormat)).ToReadOnlyReactivePropertySlim<string>(string.Empty).AddTo(_disposables);
            ModifiedTimeString = ModifiedTime.Select(dt => dt?.ToString(ConstantObject.ModifiedTimeFormat) ?? string.Empty).ToReadOnlyReactivePropertySlim<string>(string.Empty).AddTo(_disposables);
        }

        public void UpdateLastWriteTime()
        {
            var dt = File.GetLastWriteTime(FilePath);
            LastWriteTime.Value = new(dt.Year,dt.Month,dt.Day,dt.Hour,dt.Minute,dt.Second,dt.Millisecond);
        }

        [STAThread]
        public void UpdateModifiedTime(bool isIgnoreGettingLastSaveTime, ShellPropertyReader reader)
        {
            var modifiredTimeString = GetModifiredTimeString(reader);
            if (!isIgnoreGettingLastSaveTime)
            {
                ModifiedTime.Value = DateTime.TryParse(modifiredTimeString, out DateTime modifiedTime) ? modifiedTime : null;
            }
        }

        [STAThread]
        private string GetModifiredTimeString(ShellPropertyReader reader)
        {
            //NOTE:154=前回保存日時。正規表現でフォーマット文字を除外。
            return NonAsciiRegex().Replace(reader.GetValue(FilePath, 154), "");
        }

        private IEnumerable<DirectoryOpenner> GetDirectoryOpenners(IEnumerable<string> directories)
        {
            if (directories.Any())
            {
                foreach (var index in Enumerable.Range(1, directories.Count() - 1))
                {
                    yield return new(_searchDirectoryPath, directories.Take(index));
                }
                yield return new(_searchDirectoryPath, FilePath);
            }
            var needsEmptyDataCount = ConstantObject.MaxDirectoryColumnCount - directories.Count();

            foreach (var openner in Enumerable.Repeat(DirectoryOpenner.CreateEmptyOpenner(), needsEmptyDataCount))
            {
                yield return openner;
            }
        }

        public void UpdateIsHighlighted(Func<string,bool> isMatchFileName, Func<string, bool> isMatchExtensionText, Func<string, bool> isMatchDirectoryName, Func<DateTime?, bool> isLastWriteTimeMatch, Func<DateTime?, bool> isModifiedTimeMatch)
        {
            IsHighlighted.Value = isMatchFileName(FileName)
                                  && isMatchExtensionText(ExtensionText)
                                  && isMatchDirectoryName(_relativeDirectoryPath)
                                  && isLastWriteTimeMatch(LastWriteTime.Value)
                                  && isModifiedTimeMatch(ModifiedTime.Value);
        }

        private static IEnumerable<string> GetDirectories(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return [];
            }

            var root = Path.GetPathRoot(relativePath);

            var prevPath = Path.GetDirectoryName(relativePath) ?? string.Empty;

            if (root == prevPath)
                return [];

            List<string> directories = [];
            do
            {
                var directory = Path.GetDirectoryName(prevPath) ?? string.Empty;
                directories.Add(Path.GetFileName(prevPath));

                if (directory == root)
                    break;

                prevPath = directory;
            } while (true);

            return directories.Reverse<string>();
        }
    }
}
