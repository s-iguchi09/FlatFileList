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
    /// <summary>
    /// ファイル一覧の1行に対応する、検索でヒットした1ファイルの情報。
    /// ファイル名・拡張子・アイコン・更新日時・前回保存日時、およびルートからの各階層ディレクトリを保持し、
    /// フィルター条件に応じたハイライト状態を管理する。
    /// </summary>
    public partial class FileProperty : INotifyPropertyChanged, IDisposable
    {
        //NOTE:毎回 new すると走査1回あたり数万回コンパイルが走るため、ソース生成で静的に持つ。
        /// <summary>ASCII 印字可能文字(0x20〜0x7F)以外にマッチする、ソース生成された正規表現。</summary>
        [GeneratedRegex(@"[^\x20-\x7F]")]
        private static partial Regex NonAsciiRegex();


#pragma warning disable CS0067
        /// <summary>プロパティ値の変更を通知するイベント。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        private readonly CompositeDisposable _disposables = new();
        /// <summary>保持しているリアクティブリソースを破棄する。</summary>
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

        //NOTE:XAML から Directories[i] とインデクサ束縛すると、コンテナ再利用(リサイクル)で DataContext が
        //     差し替わるたびに1セルにつきインデクサ解決が走る(20列分)。直接プロパティにして解決コストを下げる。
        //     Directories は常に MaxDirectoryColumnCount(20)件へパディングされるため、添字は常に有効。
        public DirectoryOpenner Directory1 => Directories[0];
        public DirectoryOpenner Directory2 => Directories[1];
        public DirectoryOpenner Directory3 => Directories[2];
        public DirectoryOpenner Directory4 => Directories[3];
        public DirectoryOpenner Directory5 => Directories[4];
        public DirectoryOpenner Directory6 => Directories[5];
        public DirectoryOpenner Directory7 => Directories[6];
        public DirectoryOpenner Directory8 => Directories[7];
        public DirectoryOpenner Directory9 => Directories[8];
        public DirectoryOpenner Directory10 => Directories[9];
        public DirectoryOpenner Directory11 => Directories[10];
        public DirectoryOpenner Directory12 => Directories[11];
        public DirectoryOpenner Directory13 => Directories[12];
        public DirectoryOpenner Directory14 => Directories[13];
        public DirectoryOpenner Directory15 => Directories[14];
        public DirectoryOpenner Directory16 => Directories[15];
        public DirectoryOpenner Directory17 => Directories[16];
        public DirectoryOpenner Directory18 => Directories[17];
        public DirectoryOpenner Directory19 => Directories[18];
        public DirectoryOpenner Directory20 => Directories[19];

        public string RelativeFilePath { get; }

        private string _relativeDirectoryPath = string.Empty;

        private readonly string _searchDirectoryPath;

        /// <summary>
        /// ファイルパスと検索ルートから、一覧表示に必要な各種プロパティを構築する。
        /// </summary>
        /// <param name="filePath">対象ファイルの絶対パス。</param>
        /// <param name="searchDirectoryPath">検索の起点となったルートディレクトリのパス。相対パスや階層列の算出に使用する。</param>
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

        /// <summary>
        /// ファイルシステムから更新日時(LastWriteTime)を取得し直して <see cref="LastWriteTime"/> を更新する。
        /// </summary>
        public void UpdateLastWriteTime()
        {
            var dt = File.GetLastWriteTime(FilePath);
            LastWriteTime.Value = new(dt.Year,dt.Month,dt.Day,dt.Hour,dt.Minute,dt.Second,dt.Millisecond);
        }

        /// <summary>
        /// Shell 拡張プロパティから前回保存日時を取得し、<see cref="ModifiedTime"/> を更新する。
        /// </summary>
        /// <param name="isIgnoreGettingLastSaveTime"><see langword="true"/> の場合は取得を行わず更新をスキップする。</param>
        /// <param name="reader">Shell プロパティの読み出しに使用するリーダー。</param>
        [STAThread]
        public void UpdateModifiedTime(bool isIgnoreGettingLastSaveTime, ShellPropertyReader reader)
        {
            var modifiredTimeString = GetModifiredTimeString(reader);
            if (!isIgnoreGettingLastSaveTime)
            {
                ModifiedTime.Value = DateTime.TryParse(modifiredTimeString, out DateTime modifiedTime) ? modifiedTime : null;
            }
        }

        /// <summary>
        /// Shell 拡張プロパティ「前回保存日時」を取得し、書式文字などの非 ASCII 文字を除去した文字列を返す。
        /// </summary>
        /// <param name="reader">Shell プロパティの読み出しに使用するリーダー。</param>
        /// <returns>前回保存日時を表す文字列。</returns>
        [STAThread]
        private string GetModifiredTimeString(ShellPropertyReader reader)
        {
            //NOTE:154=前回保存日時。正規表現でフォーマット文字を除外。
            return NonAsciiRegex().Replace(reader.GetValue(FilePath, 154), "");
        }

        /// <summary>
        /// 各階層のディレクトリ名から <see cref="DirectoryOpenner"/> の列を生成する。
        /// 列数を <see cref="ConstantObject.MaxDirectoryColumnCount"/> に揃えるため、不足分は空のエントリでパディングする。
        /// </summary>
        /// <param name="directories">ルートから対象ファイルまでの各階層のフォルダー名。</param>
        /// <returns>列表示用の <see cref="DirectoryOpenner"/> の列。</returns>
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

        /// <summary>
        /// 各フィルター条件をすべて満たすかどうかを判定し、<see cref="IsHighlighted"/> を更新する。
        /// </summary>
        /// <param name="isMatchFileName">ファイル名に対する一致判定。</param>
        /// <param name="isMatchExtensionText">拡張子に対する一致判定。</param>
        /// <param name="isMatchDirectoryName">ディレクトリ名(相対パス)に対する一致判定。</param>
        /// <param name="isLastWriteTimeMatch">更新日時に対する一致判定。</param>
        /// <param name="isModifiedTimeMatch">前回保存日時に対する一致判定。</param>
        public void UpdateIsHighlighted(Func<string,bool> isMatchFileName, Func<string, bool> isMatchExtensionText, Func<string, bool> isMatchDirectoryName, Func<DateTime?, bool> isLastWriteTimeMatch, Func<DateTime?, bool> isModifiedTimeMatch)
        {
            IsHighlighted.Value = isMatchFileName(FileName)
                                  && isMatchExtensionText(ExtensionText)
                                  && isMatchDirectoryName(_relativeDirectoryPath)
                                  && isLastWriteTimeMatch(LastWriteTime.Value)
                                  && isModifiedTimeMatch(ModifiedTime.Value);
        }

        /// <summary>
        /// 相対パスを分解し、ルート直下から末尾の親ディレクトリまでの各階層のフォルダー名を順に返す。
        /// </summary>
        /// <param name="relativePath">ルートからの相対ファイルパス。</param>
        /// <returns>各階層のフォルダー名。階層がない場合は空の列。</returns>
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
