using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using FlatFileList.Extensions;
using FlatFileList.Utilities.Shell32;

namespace FlatFileList.Datas
{
    /// <summary>
    /// ファイル一覧の1行に対応する、検索でヒットした1ファイルの情報。
    /// ファイル名・拡張子・アイコン・更新日時・前回保存日時、およびルートからの各階層ディレクトリを保持し、
    /// フィルター条件に応じたハイライト状態を管理する。
    /// </summary>
    /// <remarks>
    /// 数万件を生成するため、ReactiveProperty と Rx の購読は使わず、通常のプロパティと <see cref="PropertyChanged"/> で通知する。
    /// WPF のバインディングはスカラー値の変更通知を UI スレッドへ自動でマーシャリングするため、バックグラウンドからの更新も可能。
    /// </remarks>
    public class FileProperty : INotifyPropertyChanged
    {
        /// <summary>プロパティ値の変更を通知するイベント。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary><see cref="LastWriteTimeString"/> の変更通知用イベント引数(全行で共有)。</summary>
        private static readonly PropertyChangedEventArgs _lastWriteTimeStringChangedEventArgs = new(nameof(LastWriteTimeString));
        /// <summary><see cref="ModifiedTimeString"/> の変更通知用イベント引数(全行で共有)。</summary>
        private static readonly PropertyChangedEventArgs _modifiedTimeStringChangedEventArgs = new(nameof(ModifiedTimeString));

        /// <summary>更新日時の値。</summary>
        private DateTime _lastWriteTime;
        /// <summary>前回保存日時の値。</summary>
        private DateTime? _modifiedTime;
        /// <summary>フィルター条件に一致しているかどうかの値。</summary>
        private bool _isHighlighted = true;
        /// <summary>前回保存日時の取得を開始済みかどうか(0: 未取得, 1: 取得済み)。並列取得時の二重取得を防ぐ。</summary>
        private int _modifiedTimeLoadState;

        /// <summary>ファイルの更新日時(ミリ秒精度)。</summary>
        public DateTime LastWriteTime
        {
            get => _lastWriteTime;
            private set
            {
                if (SetProperty(ref _lastWriteTime, value))
                {
                    PropertyChanged?.Invoke(this, _lastWriteTimeStringChangedEventArgs);
                }
            }
        }

        /// <summary>一覧表示用に書式化した更新日時。</summary>
        public string LastWriteTimeString => LastWriteTime.ToString(ConstantObject.LastWriteTimeFormat);

        /// <summary>ドキュメントの前回保存日時。取得前・値なしの場合は <see langword="null"/>。</summary>
        public DateTime? ModifiedTime
        {
            get => _modifiedTime;
            private set
            {
                if (SetProperty(ref _modifiedTime, value))
                {
                    PropertyChanged?.Invoke(this, _modifiedTimeStringChangedEventArgs);
                }
            }
        }

        /// <summary>一覧表示用に書式化した前回保存日時。値がない場合は空文字。</summary>
        public string ModifiedTimeString => ModifiedTime?.ToString(ConstantObject.ModifiedTimeFormat) ?? string.Empty;

        /// <summary>フィルター条件にすべて一致しているかどうか。</summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            private set => SetProperty(ref _isHighlighted, value);
        }

        /// <summary>ファイルの絶対パス。</summary>
        public string FilePath { get; }

        /// <summary>ファイル名(拡張子を含む)。</summary>
        public string FileName { get; }
        /// <summary>拡張子(先頭のドットを含む)。</summary>
        public string ExtensionText { get; }

        /// <summary>拡張子に対応するアイコン(全行で共有する Freeze 済みインスタンス)。</summary>
        public BitmapSource? FileIcon { get; }

        /// <summary>ルートからの各階層のディレクトリ。常に <see cref="ConstantObject.MaxDirectoryColumnCount"/> 件以上にパディングされる。</summary>
        public List<DirectoryOpenner> Directories { get; }

        /// <summary>ルートからファイルまでのディレクトリ階層の深さ(フォルダー列の表示判定に使用)。</summary>
        public int DirectoryDepth { get; }

        //NOTE:XAML から Directories[i] とインデクサ束縛すると、コンテナ再利用(リサイクル)で DataContext が
        //     差し替わるたびに1セルにつきインデクサ解決が走る(20列分)。直接プロパティにして解決コストを下げる。
        //     Directories は常に MaxDirectoryColumnCount(20)件へパディングされるため、添字は常に有効。
        /// <summary>フォルダー列1のディレクトリ。</summary>
        public DirectoryOpenner Directory1 => Directories[0];
        /// <summary>フォルダー列2のディレクトリ。</summary>
        public DirectoryOpenner Directory2 => Directories[1];
        /// <summary>フォルダー列3のディレクトリ。</summary>
        public DirectoryOpenner Directory3 => Directories[2];
        /// <summary>フォルダー列4のディレクトリ。</summary>
        public DirectoryOpenner Directory4 => Directories[3];
        /// <summary>フォルダー列5のディレクトリ。</summary>
        public DirectoryOpenner Directory5 => Directories[4];
        /// <summary>フォルダー列6のディレクトリ。</summary>
        public DirectoryOpenner Directory6 => Directories[5];
        /// <summary>フォルダー列7のディレクトリ。</summary>
        public DirectoryOpenner Directory7 => Directories[6];
        /// <summary>フォルダー列8のディレクトリ。</summary>
        public DirectoryOpenner Directory8 => Directories[7];
        /// <summary>フォルダー列9のディレクトリ。</summary>
        public DirectoryOpenner Directory9 => Directories[8];
        /// <summary>フォルダー列10のディレクトリ。</summary>
        public DirectoryOpenner Directory10 => Directories[9];
        /// <summary>フォルダー列11のディレクトリ。</summary>
        public DirectoryOpenner Directory11 => Directories[10];
        /// <summary>フォルダー列12のディレクトリ。</summary>
        public DirectoryOpenner Directory12 => Directories[11];
        /// <summary>フォルダー列13のディレクトリ。</summary>
        public DirectoryOpenner Directory13 => Directories[12];
        /// <summary>フォルダー列14のディレクトリ。</summary>
        public DirectoryOpenner Directory14 => Directories[13];
        /// <summary>フォルダー列15のディレクトリ。</summary>
        public DirectoryOpenner Directory15 => Directories[14];
        /// <summary>フォルダー列16のディレクトリ。</summary>
        public DirectoryOpenner Directory16 => Directories[15];
        /// <summary>フォルダー列17のディレクトリ。</summary>
        public DirectoryOpenner Directory17 => Directories[16];
        /// <summary>フォルダー列18のディレクトリ。</summary>
        public DirectoryOpenner Directory18 => Directories[17];
        /// <summary>フォルダー列19のディレクトリ。</summary>
        public DirectoryOpenner Directory19 => Directories[18];
        /// <summary>フォルダー列20のディレクトリ。</summary>
        public DirectoryOpenner Directory20 => Directories[19];

        /// <summary>ルートからの相対ファイルパス(先頭に「\」を含む)。</summary>
        public string RelativeFilePath { get; }

        /// <summary>ルートからの相対ディレクトリパス。フォルダー名フィルターの判定対象。</summary>
        private readonly string _relativeDirectoryPath = string.Empty;

        /// <summary>
        /// ファイルパスと検索ルートから、一覧表示に必要な各種プロパティを構築する(更新日時はファイルシステムから取得する)。
        /// </summary>
        /// <param name="filePath">対象ファイルの絶対パス。</param>
        /// <param name="searchDirectoryPath">検索の起点となったルートディレクトリのパス。</param>
        public FileProperty(string filePath, string searchDirectoryPath)
            : this(filePath, File.GetLastWriteTime(filePath), new DirectoryOpennerCache(searchDirectoryPath))
        {
        }

        /// <summary>
        /// 列挙時に得た情報から、一覧表示に必要な各種プロパティを構築する。
        /// </summary>
        /// <param name="filePath">対象ファイルの絶対パス。</param>
        /// <param name="lastWriteTime">列挙時に取得した更新日時(ローカル時刻)。</param>
        /// <param name="directoryOpennerCache">検索1回分のディレクトリ情報の共有キャッシュ。検索ルートもここから取得する。</param>
        public FileProperty(string filePath, DateTime lastWriteTime, DirectoryOpennerCache directoryOpennerCache)
        {
            var searchDirectoryPath = directoryOpennerCache.RootPath;

            FilePath = filePath;

            FileName = Path.GetFileName(FilePath);
            ExtensionText = Path.GetExtension(FileName);
            FileIcon = FilePath.FileIconToBitmapSource();

            var relativeFilePath = FilePath.Replace(Path.GetDirectoryName(searchDirectoryPath) ?? string.Empty, string.Empty);
            RelativeFilePath = $"\\{relativeFilePath}";
            _relativeDirectoryPath = (Path.GetDirectoryName(relativeFilePath) ?? string.Empty).Replace(Path.GetDirectoryName(searchDirectoryPath) ?? string.Empty, string.Empty);

            var directories = GetDirectories(relativeFilePath);
            DirectoryDepth = directories.Count;
            Directories = CreateDirectoryOpenners(directories, directoryOpennerCache);

            _lastWriteTime = TruncateToMilliseconds(lastWriteTime);
        }

        /// <summary>
        /// ファイルシステムから更新日時(LastWriteTime)を取得し直して <see cref="LastWriteTime"/> を更新する。
        /// </summary>
        public void UpdateLastWriteTime()
        {
            LastWriteTime = TruncateToMilliseconds(File.GetLastWriteTime(FilePath));
        }

        /// <summary>
        /// 前回保存日時の取得をこれから行う権利を得る。まだ誰も取得していない場合のみ <see langword="true"/> を返す。
        /// </summary>
        /// <returns>取得を行うべき場合は <see langword="true"/>。</returns>
        public bool TryBeginLoadModifiedTime() => Interlocked.Exchange(ref _modifiedTimeLoadState, 1) == 0;

        /// <summary>
        /// プロパティシステムから前回保存日時を取得し、<see cref="ModifiedTime"/> を更新する。
        /// </summary>
        /// <param name="isIgnoreGettingLastSaveTime"><see langword="true"/> の場合は取得を行わず更新をスキップする。</param>
        public void UpdateModifiedTime(bool isIgnoreGettingLastSaveTime)
        {
            if (isIgnoreGettingLastSaveTime)
            {
                return;
            }

            ModifiedTime = DocumentDateSavedReader.Read(FilePath);
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
            IsHighlighted = isMatchFileName(FileName)
                            && isMatchExtensionText(ExtensionText)
                            && isMatchDirectoryName(_relativeDirectoryPath)
                            && isLastWriteTimeMatch(LastWriteTime)
                            && isModifiedTimeMatch(ModifiedTime);
        }

        /// <summary>
        /// 値が変わった場合のみフィールドを更新し、<see cref="PropertyChanged"/> を発生させる。
        /// </summary>
        /// <typeparam name="T">値の型。</typeparam>
        /// <param name="field">更新対象のフィールド。</param>
        /// <param name="value">新しい値。</param>
        /// <param name="propertyName">プロパティ名(呼び出し元から自動設定)。</param>
        /// <returns>値が変わった場合は <see langword="true"/>。</returns>
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        /// <summary>
        /// 日時をミリ秒精度に切り捨てる(表示・比較に不要なティック以下の端数を除く)。
        /// </summary>
        /// <param name="dt">対象の日時。</param>
        /// <returns>ミリ秒精度の日時。</returns>
        private static DateTime TruncateToMilliseconds(DateTime dt) => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);

        /// <summary>
        /// 各階層のディレクトリ名から列表示用の <see cref="DirectoryOpenner"/> の一覧を生成する。
        /// 中間階層はキャッシュの共有インスタンスを使い、末尾(ファイルの親フォルダー)はファイルを選択して開くため個別に生成する。
        /// 列数を <see cref="ConstantObject.MaxDirectoryColumnCount"/> に揃えるため、不足分は空のエントリでパディングする。
        /// </summary>
        /// <param name="directories">ルートから対象ファイルまでの各階層のフォルダー名。</param>
        /// <param name="cache">検索1回分のディレクトリ情報の共有キャッシュ。</param>
        /// <returns>列表示用の <see cref="DirectoryOpenner"/> の一覧。</returns>
        private List<DirectoryOpenner> CreateDirectoryOpenners(List<string> directories, DirectoryOpennerCache cache)
        {
            var openners = new List<DirectoryOpenner>(Math.Max(directories.Count, ConstantObject.MaxDirectoryColumnCount));

            if (directories.Count > 0)
            {
                //NOTE:以前は階層ごとに directories.Take(index) で先頭から連結し直していた(O(n²))。累積パスを伸ばして O(n) にする。
                var relativePath = string.Empty;
                for (var index = 0; index < directories.Count - 1; index++)
                {
                    relativePath = index == 0 ? directories[index] : string.Concat(relativePath, "\\", directories[index]);
                    openners.Add(cache.GetOrAdd(relativePath, directories[index]));
                }

                openners.Add(new DirectoryOpenner(cache.RootPath, FilePath));
            }

            for (var index = openners.Count; index < ConstantObject.MaxDirectoryColumnCount; index++)
            {
                openners.Add(DirectoryOpenner.CreateEmptyOpenner());
            }

            return openners;
        }

        /// <summary>
        /// 相対パスを分解し、ルート直下から末尾の親ディレクトリまでの各階層のフォルダー名を順に返す。
        /// </summary>
        /// <param name="relativePath">ルートからの相対ファイルパス。</param>
        /// <returns>各階層のフォルダー名。階層がない場合は空の一覧。</returns>
        private static List<string> GetDirectories(string relativePath)
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

            directories.Reverse();
            return directories;
        }
    }
}
