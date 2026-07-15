using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using FlatFileList.Extensions;
using System.Text.RegularExpressions;

namespace FlatFileList.Datas
{
    public class FileProperty : INotifyPropertyChanged, IDisposable
    {
#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        private readonly CompositeDisposable _disposables = new();
        public void Dispose() => _disposables.Dispose();

        public ReactivePropertySlim<DateTime> LastWriteTime { get; } = new();
        public ReadOnlyReactivePropertySlim<string> LastWriteTimeString { get; }
        public ReactivePropertySlim<DateTime?> ModifiedTime { get; } = new();
        public ReadOnlyReactiveProperty<string> ModifiedTimeString { get; }
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

            LastWriteTimeString = LastWriteTime.ObserveOnUIDispatcher().Select(dt => dt.ToString(ConstantObject.LastWriteTimeFormat)).ToReadOnlyReactivePropertySlim<string>().AddTo(_disposables);
            ModifiedTimeString = ModifiedTime.ObserveOnUIDispatcher().Select(dt => dt?.ToString(ConstantObject.ModifiedTimeFormat) ?? string.Empty).ToReadOnlyReactiveProperty<string>().AddTo(_disposables);
        }

        public void UpdateLastWriteTime()
        {
            var dt = File.GetLastWriteTime(FilePath);
            LastWriteTime.Value = new(dt.Year,dt.Month,dt.Day,dt.Hour,dt.Minute,dt.Second,dt.Millisecond);
        }

        [STAThread]
        public void UpdateModifiedTime(bool isIgnoreGettingLastSaveTime)
        {
            var modifiredTimeString = GetModifiredTimeString();
            if (!isIgnoreGettingLastSaveTime)
            {
                ModifiedTime.Value = DateTime.TryParse(modifiredTimeString, out DateTime modifiedTime) ? modifiedTime : null;
            }
        }

        [STAThread]
        private string GetModifiredTimeString()
        {
            //NOTE:154=前回保存日時。正規表現でフォーマット文字を除外。
            return Regex.Replace(FilePath.GetFilePropertyValue(154), @"[^\u0020-\u007F]", "");
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
