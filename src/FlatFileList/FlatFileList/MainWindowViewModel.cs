using FlatFileList.Datas;
using FlatFileList.Extensions;
using FlatFileList.Interfaces;
using FlatFileList.Utilities;
using FlatFileList.Utilities.Shell32;
using Microsoft.Extensions.Options;
using Microsoft.Xaml.Behaviors.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;

namespace FlatFileList
{
    /// <summary>
    /// メインウィンドウの ViewModel。フォルダー配下のファイル再帰検索、検索後の絞り込みフィルター、
    /// 進捗表示、ファイル/フォルダーのクリップボードコピー・オープン、ユーザー設定の読み書きなど、
    /// アプリケーションの中心的な状態と操作を提供する。
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged, IWindowClosing, IDisposable
    {
#pragma warning disable CS0067
        /// <summary>プロパティ値の変更を通知するイベント。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        private CompositeDisposable _disposable = new();
        /// <summary>保持しているリアクティブリソースをまとめて破棄する。</summary>
        public void Dispose() => _disposable.Dispose();

        private const string _TITLE = "FlatFileList";

        #region Window関連
        public ReadOnlyReactivePropertySlim<string> Title { get; }
        public ReadOnlyReactiveProperty<TaskbarItemProgressState> WindowProgressState { get; }
        #endregion

        #region 進捗ステータス
        public ReadOnlyReactiveProperty<double> ProgressPer { get; }
        public ReactiveProperty<bool> IsIndeterminate { get; } = new();
        public ReadOnlyReactiveProperty<bool> IsBackgroundTaskRunning { get; }
        public ReactiveProperty<bool> IsLastSaveTimeRefreshing { get; } = new(false);
        public ReactiveProperty<int> MaxCount { get; } = new(0);
        public ReactiveProperty<int> SuccessedCount { get; } = new(0);
        public ReadOnlyReactiveProperty<string> ProgressBarText { get; }
        #endregion

        #region 検索機能
        public ReactivePropertySlim<string> DirectoryPath { get; } = new(string.Empty);
        public AsyncReactiveCommand Search { get; }
        public ReadOnlyReactivePropertySlim<bool> CanExecuteOfSearch { get; }
        public ReactiveProperty<bool> IsSearching { get; } = new(false);

        /// <summary>前回保存日時の取得を行わないかどうか。</summary>
        public ReactivePropertySlim<bool> IsIgnoreGettingLastSaveTime { get; } = new();
        /// <summary>隠しフォルダーを検索対象外にするかどうか。</summary>
        public ReactivePropertySlim<bool> IsExcludeHiddenDirectory { get; } = new();
        /// <summary>ドットで始まるフォルダーを検索対象外にするかどうか。</summary>
        public ReactivePropertySlim<bool> IsExcludeDotStartDirectory { get; } = new();
        /// <summary>システムフォルダーを検索対象外にするかどうか。</summary>
        public ReactivePropertySlim<bool> IsExcludeSystemDirectory { get; } = new();
        /// <summary>ドットで始まるファイルを検索対象外にするかどうか。</summary>
        public ReactivePropertySlim<bool> IsExcludeDotStartFile { get; } = new();
        /// <summary>隠しファイルを検索対象外にするかどうか。</summary>
        public ReactivePropertySlim<bool> IsExcludeHiddenFile { get; } = new();
        /// <summary>システムファイルを検索対象外にするかどうか。</summary>
        public ReactivePropertySlim<bool> IsExcludeSystemFile { get; } = new();

        public ReactiveCommandSlim ClearSearchResult { get; } = new();

        #region 対象外拡張子Popup

        public ReactivePropertySlim<string> ExcludeExtensionText { get; } = new(string.Empty);
        public ReactiveCommandSlim AddExcludeExtension { get; }

        public ReactiveCollection<ExcludeExtension> ExcludeExtensions { get; } = [];

        #endregion

        #endregion

        #region 検索後フィルター機能
        public ReactivePropertySlim<string> FilteringFileNameText { get; } = new(string.Empty);
        public ReactivePropertySlim<string> FilteringExtensionText { get; } = new(string.Empty);
        public ReactivePropertySlim<string> FilteringDirectoryNameText { get; } = new(string.Empty);
        public ReactivePropertySlim<bool> IsRegexFileNameSearchEnabled { get; } = new(false);
        public ReactivePropertySlim<bool> IsRegexExtensionTextSearchEnabled { get; } = new(false);
        public ReactivePropertySlim<bool> IsRegexDirectoryNameSearchEnabled { get; } = new(false);
        public ReactiveProperty<DateTime?> FilteringLastWriteTime { get; } = new();
        public ReactivePropertySlim<ComparisonConditionType> FilteringLastWriteTimeComparisonConditionType { get; } = new(ComparisonConditionType.LessThan);
        public ReactiveProperty<DateTime?> FilteringModifiedTime { get; } = new();
        public ReactivePropertySlim<ComparisonConditionType> FilteringModifiedTimeComparisonConditionType { get; } = new(ComparisonConditionType.LessThan);
        public ReactivePropertySlim<bool> IsHighlightVisibled { get; } = new(true);
        #endregion

        #region ファイル一覧
        private readonly FileSystemWatcher _watcher = new();
        public ReactivePropertySlim<string> RootPath { get; } = new(string.Empty);
        public ReadOnlyReactivePropertySlim<bool> ExistsFileLists { get; }

        #region 一覧データ関連

        //NOTE:MaxCount/SuccessedCount/StatusText は ReactiveProperty のため、書き込みごとに Dispatcher へポストされ、
        //     派生する ProgressPer/ProgressBarText/WindowProgressState も再計算される。
        //     ファイル1件ごとに更新すると UI スレッドが飽和して操作が固まるため、この件数ごとに間引く。
        private const int ProgressReportInterval = 100;

        //NOTE:一覧への追加は Reset 通知1回でまとめて行うが、Reset のたびにビューの再構築が走るため、件数ではなく時間で間引く。
        /// <summary>検索中に一覧へ反映する間隔。</summary>
        private static readonly TimeSpan AddFlushInterval = TimeSpan.FromMilliseconds(250);

        /// <summary>FileProperty を並列に構築する単位(件数)。</summary>
        private const int BuildChunkSize = 2000;

        /// <summary>フィルターの文字入力を確定させるまでの待ち時間。</summary>
        private static readonly TimeSpan FilterTextThrottle = TimeSpan.FromMilliseconds(300);

        /// <summary>表示中の行の通知をまとめる待ち時間(スクロール中の連続通知を間引く)。</summary>
        private static readonly TimeSpan VisibleItemsThrottle = TimeSpan.FromMilliseconds(150);

        /// <summary>ファイル変更通知をまとめる間隔(1回の保存で複数回通知されるため)。</summary>
        private static readonly TimeSpan WatcherBufferInterval = TimeSpan.FromMilliseconds(500);

        public CollectionViewSource FileProperties { get; }
        protected RangeObservableCollection<FileProperty> _fileProperties { get; } = [];

        /// <summary>ファイルパスから一覧の行を引くための索引(ファイル監視で使用)。</summary>
        private readonly ConcurrentDictionary<string, FileProperty> _filePropertiesByPath = new(StringComparer.OrdinalIgnoreCase);

        public ReactivePropertySlim<bool> IsDirectory1Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory2Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory3Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory4Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory5Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory6Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory7Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory8Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory9Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory10Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory11Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory12Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory13Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory14Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory15Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory16Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory17Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory18Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory19Exists { get; } = new(false);
        public ReactivePropertySlim<bool> IsDirectory20Exists { get; } = new(false);

        protected readonly List<ReactivePropertySlim<bool>> IsDirectoriesExists;

        #endregion

        #region コマンド

        public ReactiveCommand<IList> CopyToColipboardFileNames { get; } = new();
        public ReactiveCommand<IList> CopyToColipboardRelativeFilePaths { get; } = new();
        public ReactiveCommand<IList> CopyToColipboardAbsoluteFilePaths { get; } = new();
        public ReactiveCommandSlim<string> CopyToColipboardSingleText { get; } = new();
        public ReactiveCommandSlim<string> OpenFile { get; } = new();
        public ReactiveCommandSlim<IList> OpenFiles { get; } = new();
        public ReactiveCommandSlim<string> OpenDirectory { get; } = new();
        public ReactiveCommandSlim<IList> ShowFileProperties { get; } = new();
        public ReactiveCommandSlim<string> ShowDirectoryProperties { get; } = new();
        /// <summary>画面に表示中の行(<see cref="FileProperty"/> のリスト)を受け取り、前回保存日時を優先取得させるコマンド。</summary>
        public ReactiveCommandSlim<IList> NotifyVisibleItems { get; } = new();

        #endregion

        #region subthread関連

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _refreshingLastSaveTimesTask;
        /// <summary>実行中の前回保存日時の取得処理。表示中の行を優先させるために参照する。</summary>
        private volatile LastSaveTimeLoader? _lastSaveTimeLoader;
        /// <summary>直近に通知された、画面に表示中の行。前回保存日時の取得開始時に最初に優先させる。</summary>
        private volatile IReadOnlyList<FileProperty> _lastVisibleItems = [];

        #endregion

        #endregion

        #region アプリケーション操作結果
        public ReactivePropertySlim<string> StatusText { get; } = new(string.Empty);
        #endregion

        #region アプリケーション起動プロセス
        public ReactiveCollection<CustomApplicationProcess> CustomApplicationProcesses { get; } = [];
        public ReactiveCommandSlim<IList> RemoveCustomProcessRow { get; } = new();
        #endregion

        #region 拡大率
        public ReadOnlyDictionary<int, string> WindowScaleTransformPers { get; } = new(new List<int>() { 50, 75, 100, 125, 150, 175, 200, 225, 250, 275, 300 }.ToDictionary(v => v, v => $"{v}%"));
        /// <summary>ウィンドウの表示倍率(%)。<see cref="WindowScaleTransformPers"/> のキーのいずれか。</summary>
        public ReactivePropertySlim<int> WindowScaleTransformPer { get; } = new();
        public ReadOnlyReactivePropertySlim<double> WindowScale { get; }
        #endregion

        /// <summary>ユーザー設定の保存先。</summary>
        private readonly IUserSettingsStore _userSettingsStore;

        /// <summary>
        /// 各リアクティブプロパティ・コマンドを構築し、検索やフィルター、進捗表示、ユーザー設定の
        /// 読み込み・保存に関する購読を配線する。
        /// </summary>
        /// <param name="userSettings">起動時に構成から読み込んだユーザー設定。</param>
        /// <param name="userSettingsStore">ユーザー設定の保存先。</param>
        public MainWindowViewModel(IOptions<UserSettings> userSettings, IUserSettingsStore userSettingsStore)
        {
            // 設定ファイルを読み込めず既定値で起動した場合は、その旨を初期のステータスとして表示する
            using var statusTextRefresh = Disposable.Create(() => StatusText.Value = userSettingsStore.LoadErrorMessage ?? string.Empty);

            _userSettingsStore = userSettingsStore;

            #region UserProfile初期値

            var settings = userSettings.Value;
            IsIgnoreGettingLastSaveTime.Value = settings.IsIgnoreGettingLastSaveTime;
            IsExcludeHiddenDirectory.Value = settings.IsExcludeHiddenDirectory;
            IsExcludeDotStartDirectory.Value = settings.IsExcludeDotStartDirectory;
            IsExcludeSystemDirectory.Value = settings.IsExcludeSystemDirectory;
            IsExcludeDotStartFile.Value = settings.IsExcludeDotStartFile;
            IsExcludeHiddenFile.Value = settings.IsExcludeHiddenFile;
            IsExcludeSystemFile.Value = settings.IsExcludeSystemFile;
            // 設定ファイルは手編集できるため、選択肢にない倍率(0 など)は既定の 100% に戻す
            WindowScaleTransformPer.Value = WindowScaleTransformPers.ContainsKey(settings.WindowScaleTransformPer)
                ? settings.WindowScaleTransformPer
                : 100;

            #endregion

            Title = RootPath.Select(p => string.IsNullOrEmpty(p) ? _TITLE : string.Join(" - ", _TITLE, p)).ToReadOnlyReactivePropertySlim<string>().AddTo(_disposable);

            #region ファイル一覧
            #region フォルダー列
            // NOTE:ディレクトリ列を追加する場合は対応するIsExistsプロパティを追加し、リストに追加すること。
            IsDirectoriesExists =
            [
                IsDirectory1Exists,
                IsDirectory2Exists,
                IsDirectory3Exists,
                IsDirectory4Exists,
                IsDirectory5Exists,
                IsDirectory6Exists,
                IsDirectory7Exists,
                IsDirectory8Exists,
                IsDirectory9Exists,
                IsDirectory10Exists,
                IsDirectory11Exists,
                IsDirectory12Exists,
                IsDirectory13Exists,
                IsDirectory14Exists,
                IsDirectory15Exists,
                IsDirectory16Exists,
                IsDirectory17Exists,
                IsDirectory18Exists,
                IsDirectory19Exists,
                IsDirectory20Exists,
            ];
            #endregion

            FileProperties = new() { Source = _fileProperties };
            FileProperties.FilterAsObservable().Subscribe(e => e.Accepted = !IsHighlightVisibled.Value || ((FileProperty)e.Item).IsHighlighted).AddTo(_disposable);

            ExistsFileLists = _fileProperties.CollectionChangedAsObservable().Select(_ => _fileProperties.Any()).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            _watcher.AddTo(_disposable);
            _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            //NOTE:1回の保存で Changed は複数回通知されるため、一定時間まとめてパスを重複排除し、
            //     以前のように通知ごとにスレッドを立てるのではなく、1バッチにつき1本の STA スレッドで処理する。
            //     行は線形探索ではなくパスの索引から引く。
            _watcher.ChangedAsObservable()
                    .Buffer(WatcherBufferInterval)
                    .Where(events => events.Count > 0)
                    .Subscribe(events =>
                    {
                        var items = events.Select(e => e.FullPath)
                                          .Distinct(StringComparer.OrdinalIgnoreCase)
                                          .Select(path => _filePropertiesByPath.TryGetValue(path, out var item) ? item : null)
                                          .OfType<FileProperty>()
                                          .ToList();

                        if (items.Count == 0)
                        {
                            return;
                        }

                        var isIgnoreGettingLastSaveTime = IsIgnoreGettingLastSaveTime.Value;
                        Thread t = new(() =>
                        {
                            foreach (var item in items)
                            {
                                item.UpdateLastWriteTime();
                                item.UpdateModifiedTime(isIgnoreGettingLastSaveTime);
                            }
                        })
                        { IsBackground = true };
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                    }).AddTo(_disposable);
            _watcher.IncludeSubdirectories = true;

            //NOTE:スクロール中は連続して通知されるため、止まってから表示中の行を前回保存日時の取得に割り込ませる。
            NotifyVisibleItems.Throttle(VisibleItemsThrottle)
                              .Subscribe(items =>
                              {
                                  var visibleItems = items.OfType<FileProperty>().ToList();
                                  _lastVisibleItems = visibleItems;
                                  _lastSaveTimeLoader?.Prioritize(visibleItems);
                              })
                              .AddTo(_disposable);

            #region コマンド

            CopyToColipboardFileNames.WithSubscribe(fs => CopyToClipboard(fs, fp => fp.Select(f => f.FileName))).AddTo(_disposable);
            CopyToColipboardRelativeFilePaths.WithSubscribe(fs => CopyToClipboard(fs, fp => fp.Select(f => f.RelativeFilePath))).AddTo(_disposable);
            CopyToColipboardAbsoluteFilePaths.WithSubscribe(fs => CopyToClipboard(fs, fp => fp.Select(f => f.FilePath))).AddTo(_disposable);

            CopyToColipboardSingleText.WithSubscribe(t =>
            {
                if (string.IsNullOrEmpty(t))
                {
                    return;
                }

                if (!SafeClip(t))
                {
                    MessageBox.Show("コピーに失敗しました。");
                }
            }).AddTo(_disposable);

            OpenFile.WithSubscribe(path => OpenFileInternal(path, true)).AddTo(_disposable);
            OpenFiles.WithSubscribe(fs =>
            {
                var fileProperties = IListSafeCastToFileProperties(fs);
                fileProperties.ToList().ForEach(f => OpenFileInternal(f.FilePath, false));
                StatusText.Value = $"{fileProperties.Count()} File{(fileProperties.Count() > 2 ? "s" : string.Empty)}Open.";
            }).AddTo(_disposable);

            OpenDirectory.WithSubscribe(path =>
            {
                var argument = CreateExploereArgument(path);
                if (string.IsNullOrEmpty(argument))
                {
                    StatusText.Value = $"DirectoryNotExist. file:{path}.";
                    MessageBox.Show("フォルダーが存在しません。", "DirectoryOpenError", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusText.Value = $"DirectoryOpen. file:{(File.Exists(path) ? Path.GetDirectoryName(path) : path)}.";
                Process.Start("EXPLORER.EXE", CreateExploereArgument(path));
            }).AddTo(_disposable);

            ShowFileProperties.WithSubscribe(fs => fs.SafeCast<FileProperty>().ToList().ForEach(f => ShellObjectProperties.Show(f.FilePath))).AddTo(_disposable);

            ShowDirectoryProperties.WithSubscribe(path => ShellObjectProperties.Show(path)).AddTo(_disposable);

            #endregion

            #endregion

            #region 進捗ステータス

            IsBackgroundTaskRunning = IsLastSaveTimeRefreshing.ToReadOnlyReactiveProperty().AddTo(_disposable);

            #endregion

            #region 検索後フィルター機能

            void RefreshIsMatchedOnUIDispatcher()
            {
                using (Application.Current.Dispatcher.DisableProcessing())
                {
                    Func<string, bool> funcIsFileNameMatch = CreateIsMatchSingleFunc(IsRegexFileNameSearchEnabled.Value, FilteringFileNameText.Value);
                    Func<string, bool> funcIsExtensionTextMatch = CreateIsMatchSingleFunc(IsRegexExtensionTextSearchEnabled.Value, FilteringExtensionText.Value);
                    Func<string, bool> funcIsDirectoryNameMatch = CreateIsMatchSingleFunc(IsRegexDirectoryNameSearchEnabled.Value, FilteringDirectoryNameText.Value);
                    Func<DateTime?, bool> funcIsLastWriteTimeMatch = CreateIsMatchDatetimeFunc(FilteringLastWriteTimeComparisonConditionType.Value, FilteringLastWriteTime.Value);
                    Func<DateTime?, bool> funcIsModifiedTimeMatch = CreateIsMatchDatetimeFunc(FilteringModifiedTimeComparisonConditionType.Value, FilteringModifiedTime.Value);
                    Func<string, bool> funcIsDirectoryNamesMatch = CreateIsMatchSingleFunc(IsRegexDirectoryNameSearchEnabled.Value, FilteringDirectoryNameText.Value);

                    Parallel.ForEach(_fileProperties, f => f.UpdateIsHighlighted(funcIsFileNameMatch, funcIsExtensionTextMatch, funcIsDirectoryNamesMatch, funcIsLastWriteTimeMatch, funcIsModifiedTimeMatch));

                    //NOTE:ハイライト表示を切っている(全件表示)場合はフィルターが常に通すため、ビューの再構築は不要。
                    //     行の見た目は IsHighlighted の変更通知で更新される。
                    if (IsHighlightVisibled.Value)
                    {
                        FileProperties.View.Refresh();
                    }
                }
            }

            //NOTE:文字入力は打鍵ごとに全件を再判定しないよう、入力が止まるまで待ってから反映する。
            //     (以前は View 側の Binding Delay で遅延させていたが、ViewModel に一本化した)
            //     チェック・日付・比較条件は1操作で1回しか変わらないため即時に反映する。
            Observable.Merge(FilteringFileNameText.ToUnit(),
                             FilteringExtensionText.ToUnit(),
                             FilteringDirectoryNameText.ToUnit())
                      .Throttle(FilterTextThrottle)
                      .ObserveOnUIDispatcher()
                      .Merge(Observable.Merge(
                             FilteringLastWriteTime.ToUnit(),
                             FilteringModifiedTime.ToUnit(),
                             IsRegexFileNameSearchEnabled.ToUnit(),
                             IsRegexExtensionTextSearchEnabled.ToUnit(),
                             IsRegexDirectoryNameSearchEnabled.ToUnit(),
                             FilteringLastWriteTimeComparisonConditionType.ToUnit(),
                             FilteringModifiedTimeComparisonConditionType.ToUnit()))
                      .Subscribe(_ => RefreshIsMatchedOnUIDispatcher())
                      .AddTo(_disposable);

            IsHighlightVisibled.Subscribe(_ => FileProperties.View.Refresh()).AddTo(_disposable);

            #endregion

            #region 進捗ステータス

            ProgressPer = Observable.CombineLatest(MaxCount, SuccessedCount, CalculateProgressPer).ToReadOnlyReactiveProperty().AddTo(_disposable);
            ProgressBarText = Observable.CombineLatest(ProgressPer, MaxCount, SuccessedCount, IsIndeterminate, IsSearching, IsLastSaveTimeRefreshing, (progressPer, maxCount, successedCount, isIndeterminate, isSearching, isLastSaveTimeRefreshing) => GetProgressText(progressPer, maxCount, successedCount, isIndeterminate, !isSearching, isLastSaveTimeRefreshing)).ToReadOnlyReactiveProperty<string>().AddTo(_disposable);
            WindowProgressState = Observable.CombineLatest(ProgressPer, IsIndeterminate, GetProgressState).ToReadOnlyReactiveProperty().AddTo(_disposable);

            #endregion

            #region 検索機能

            CanExecuteOfSearch = DirectoryPath.Select(Directory.Exists).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            Action clearSearchResults = () =>
            {
                CancelSearchSubThread();

                MaxCount.Value = 0;
                SuccessedCount.Value = 0;
                IsDirectoriesExists.ForEach(p => p.Value = false);
                _filePropertiesByPath.Clear();
                _lastVisibleItems = [];
                _fileProperties.Clear();
            };

            Search = CanExecuteOfSearch.ToAsyncReactiveCommand()
                .WithSubscribe(async () =>
            {
                using var finalize = Disposable.Create(() => { StatusText.Value = string.Empty; IsSearching.Value = false; IsIndeterminate.Value = false; _watcher.EnableRaisingEvents = true;});

                clearSearchResults();

                IsSearching.Value = true;
                _watcher.EnableRaisingEvents = false;
                _watcher.Path = DirectoryPath.Value;

                //NOTE:検索条件はバックグラウンドから UI のコレクション(ExcludeExtensions 等)を触らないよう、開始時にここで確定させる。
                var rootPath = DirectoryPath.Value.Last() == '\\' ? DirectoryPath.Value : $"{DirectoryPath.Value}\\";
                var shouldRecurseDirectory = CreateValidatorForDirectory(IsExcludeDotStartDirectory.Value, IsExcludeHiddenDirectory.Value, IsExcludeSystemDirectory.Value);
                var shouldIncludeFile = CreateValidatorForFile(IsExcludeDotStartFile.Value, IsExcludeHiddenFile.Value, IsExcludeSystemFile.Value, ExcludeExtensions.Where(ext => ext.Enabled.Value).Select(ext => ext.ExtensionText.StartsWith(".") ? ext.ExtensionText : $".{ext.ExtensionText}"));

                Func<string, bool> funcIsFileNameMatch = CreateIsMatchSingleFunc(IsRegexFileNameSearchEnabled.Value, FilteringFileNameText.Value);
                Func<string, bool> funcIsExtensionTextMatch = CreateIsMatchSingleFunc(IsRegexExtensionTextSearchEnabled.Value, FilteringExtensionText.Value);
                Func<DateTime?, bool> funcIsLastWriteTimeMatch = CreateIsMatchDatetimeFunc(FilteringLastWriteTimeComparisonConditionType.Value, FilteringLastWriteTime.Value);
                Func<DateTime?, bool> funcIsModifiedTimeMatch = CreateIsMatchDatetimeFunc(FilteringModifiedTimeComparisonConditionType.Value, FilteringModifiedTime.Value);
                Func<string, bool> funcIsDirectoryNamesMatch = CreateIsMatchSingleFunc(IsRegexDirectoryNameSearchEnabled.Value, FilteringDirectoryNameText.Value);

                await Task.Run(() =>
                {
                    Debug.WriteLine($"1.{DateTime.Now.ToString()}");
                    List<FoundFile> foundFiles;

                    IsIndeterminate.Value = true;
                    using (Disposable.Create(() => IsIndeterminate.Value = false))
                    {
                        RootPath.Value = rootPath;
                        var foundCount = 0;
                        foundFiles = DirectoryEx.EnumerateFiles(rootPath, shouldRecurseDirectory, shouldIncludeFile).Select(f =>
                        {
                            foundCount++;
                            if (foundCount % ProgressReportInterval == 0)
                            {
                                StatusText.Value = $"File found. [\"{f.Path}\"]";
                                MaxCount.Value = foundCount;
                            }
                            return f;
                        }).ToList();
                        MaxCount.Value = foundFiles.Count;
                    }

                    Debug.WriteLine($"2.{DateTime.Now.ToString()}");

                    //NOTE:FileProperty はチャンク単位で並列に構築し(FileIcon は Freeze 済みでスレッドを跨げる)、
                    //     UI スレッドへの追加は一定時間ごとに AddRange でまとめて Reset 通知1回にする。
                    //     中間階層のフォルダー情報は検索1回分のキャッシュで全行が共有する。
                    var directoryOpennerCache = new DirectoryOpennerCache(rootPath);
                    var pending = new List<FileProperty>();
                    var builtCount = 0;
                    var maxDirectoryDepth = 0;
                    var flushStopwatch = Stopwatch.StartNew();

                    void FlushPending()
                    {
                        if (pending.Count == 0)
                        {
                            return;
                        }

                        var adding = pending.ToArray();
                        pending.Clear();

                        Application.Current.Dispatcher.Invoke(() => _fileProperties.AddRange(adding));
                        SuccessedCount.Value = builtCount;
                        flushStopwatch.Restart();
                    }

                    foreach (var chunk in foundFiles.Chunk(BuildChunkSize))
                    {
                        var built = chunk.AsParallel().AsOrdered().Select(file =>
                        {
                            FileProperty f = new(file.Path, file.LastWriteTime, directoryOpennerCache);
                            f.UpdateIsHighlighted(funcIsFileNameMatch, funcIsExtensionTextMatch, funcIsDirectoryNamesMatch, funcIsLastWriteTimeMatch, funcIsModifiedTimeMatch);
                            return f;
                        }).ToArray();

                        foreach (var f in built)
                        {
                            _filePropertiesByPath[f.FilePath] = f;
                            maxDirectoryDepth = Math.Max(maxDirectoryDepth, f.DirectoryDepth);
                        }

                        pending.AddRange(built);
                        builtCount += built.Length;

                        if (flushStopwatch.Elapsed >= AddFlushInterval)
                        {
                            FlushPending();
                        }
                    }

                    FlushPending();
                    SuccessedCount.Value = builtCount;

                    Debug.WriteLine($"3.{DateTime.Now.ToString()}");

                    //NOTE:以前は列ごとに全行を走査していた(20回)。構築時に記録した最大の深さから一度に決める。
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (int index in Enumerable.Range(0, ConstantObject.MaxDirectoryColumnCount))
                        {
                            IsDirectoriesExists[index].Value = index < maxDirectoryDepth;
                        }
                    });

                    Debug.WriteLine($"4.{DateTime.Now.ToString()}");
                });

                IsIndeterminate.Value = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    using var disableProcessing = Application.Current.Dispatcher.DisableProcessing();
                    FileProperties.View.Refresh();
                });

            }).AddTo(_disposable);

            var searchCompleted = Observable.CombineLatest(CanExecuteOfSearch, IsSearching, RootPath, DirectoryPath, (canExecute, isSearching, rootPath, directoryPath) => canExecute && !isSearching && (rootPath.Trim('\\') == directoryPath)).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            _ = searchCompleted.Where(b => b && !IsIgnoreGettingLastSaveTime.Value && !IsLastSaveTimeRefreshing.Value).Subscribe(_ =>
            {
                _cancellationTokenSource = new();
                var token = _cancellationTokenSource.Token;

                //NOTE:一覧は UI スレッド上のコレクションのため、ここ(UI スレッド)でスナップショットを取ってから並列取得する。
                //     直近に通知された表示中の行を最初に優先させ、以降はスクロールのたびに優先させる。
                var loader = new LastSaveTimeLoader(_fileProperties);
                loader.Prioritize(_lastVisibleItems);
                _lastSaveTimeLoader = loader;

                _refreshingLastSaveTimesTask = new(()=>
                    {
                        IsLastSaveTimeRefreshing.Value = true;
                        using var backgroundTaskRaunning = Disposable.Create(() =>
                        {
                            if (ReferenceEquals(_lastSaveTimeLoader, loader))
                            {
                                _lastSaveTimeLoader = null;
                            }
                            IsLastSaveTimeRefreshing.Value = false;
                        });

                        SuccessedCount.Value = 0;

                        //NOTE:SuccessedCount は ReactiveProperty のため書き込みごとに Dispatcher へポストされ、
                        //     派生する ProgressPer/ProgressBarText/WindowProgressState も再計算される。
                        //     1件ごとに更新すると UI スレッドが飽和してスクロールが止まるため、loader 側で間引いて通知される。
                        loader.Run(() => IsIgnoreGettingLastSaveTime.Value, count => SuccessedCount.Value = count, token);
                    }, token);
                _refreshingLastSaveTimesTask.Start();
             }).AddTo(_disposable);

            ClearSearchResult.WithSubscribe(()=>
            {
                clearSearchResults();

                RootPath.Value = string.Empty;
            }).AddTo(_disposable);

            #region 対象外拡張子

            AddExcludeExtension = ExcludeExtensionText.CombineLatest(ExcludeExtensions.CollectionChangedAsObservable()).Select(item=>!string.IsNullOrEmpty(item.First) && !ExcludeExtensions.Any(e=>e.ExtensionText == item.First)).ToReactiveCommandSlim().WithSubscribe(() =>
            {
                ExcludeExtensions.Add(new(ExcludeExtensionText.Value));
                ExcludeExtensionText.Value = string.Empty;
            }).AddTo(_disposable);

            ExcludeExtensions.ObserveElementObservableProperty(e => e.RemoveCommand).Subscribe(p =>
            {
                ExcludeExtensions.Remove(p.Instance);
            }).AddTo(_disposable);

            #endregion

            #endregion

            #region UserProfile読み込み

            // 手編集された設定ファイルに備え、null 要素は除外する
            foreach (var item in settings.CustomOpenProcesses?.OfType<CustomApplicationProcessSetting>() ?? [])
            {
                CustomApplicationProcesses.Add(CustomApplicationProcess.CreateFromSetting(item));
            }

            // 画面からは空の拡張子を追加できないため、空の要素(JSON の null は空の要素として読み込まれる)も除外する
            foreach (var item in settings.ExcludeExtensions?.OfType<ExcludeExtensionSetting>().Where(e => !string.IsNullOrEmpty(e.ExtensionText)) ?? [])
            {
                ExcludeExtensions.Add(ExcludeExtension.CreateFromSetting(item));
            }

            #endregion

            #region UserProfile更新

            // 購読時に各プロパティ(一覧の要素を含む)が現在値を通知するため、購読が終わるまでの通知では保存しない
            // (ユーザー操作なしに設定ファイルを書き換えない)
            var isUserProfileSubscribed = false;
            Observable.Merge(CustomApplicationProcesses.ObserveElementObservableProperty(c => c.IsEnabled).ToUnit(),
                             CustomApplicationProcesses.ObserveElementObservableProperty(c => c.Extension).ToUnit(),
                             CustomApplicationProcesses.ObserveElementObservableProperty(c => c.Application).ToUnit(),
                             CustomApplicationProcesses.ObserveElementObservableProperty(c => c.Args).ToUnit(),
                             CustomApplicationProcesses.CollectionChangedAsObservable().ToUnit(),
                             ExcludeExtensions.ObserveElementObservableProperty(e=>e.Enabled).ToUnit(),
                             ExcludeExtensions.CollectionChangedAsObservable().ToUnit(),
                             IsExcludeDotStartDirectory.ToUnit(),
                             IsExcludeHiddenDirectory.ToUnit(),
                             IsExcludeSystemDirectory.ToUnit(),
                             IsExcludeDotStartFile.ToUnit(),
                             IsExcludeHiddenFile.ToUnit(),
                             IsExcludeSystemFile.ToUnit(),
                             IsIgnoreGettingLastSaveTime.ToUnit(),
                             WindowScaleTransformPer.ToUnit()
                             )
                      .Where(_ => isUserProfileSubscribed)
                      .Subscribe(_ =>
                      {
                          try
                          {
                              _userSettingsStore.Save(new()
                              {
                                  IsExcludeDotStartDirectory = IsExcludeDotStartDirectory.Value,
                                  IsExcludeHiddenDirectory = IsExcludeHiddenDirectory.Value,
                                  IsExcludeSystemDirectory = IsExcludeSystemDirectory.Value,
                                  IsExcludeDotStartFile = IsExcludeDotStartFile.Value,
                                  IsExcludeHiddenFile = IsExcludeHiddenFile.Value,
                                  IsExcludeSystemFile = IsExcludeSystemFile.Value,
                                  IsIgnoreGettingLastSaveTime = IsIgnoreGettingLastSaveTime.Value,
                                  CustomOpenProcesses = CustomApplicationProcesses.Select(c => c.ToSetting()).ToList(),
                                  ExcludeExtensions = ExcludeExtensions.Select(e => e.ToSetting()).ToList(),
                                  WindowScaleTransformPer = WindowScaleTransformPer.Value,
                              });
                              StatusText.Value = $"Save User Profile.";
                          }
                          catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                          {
                              StatusText.Value = $"Failed to save User Profile. {ex.Message}";
                          }
                      })
                      .AddTo(_disposable);
            isUserProfileSubscribed = true;

            #endregion

            #region アプリケーション起動プロセス

            RemoveCustomProcessRow.WithSubscribe(l => l.SafeCast<CustomApplicationProcess>().ToList().ForEach(c => CustomApplicationProcesses.Remove(c))).AddTo(_disposable);

            #endregion

            #region 拡大率

            WindowScale = WindowScaleTransformPer.Select(p => p / 100.0).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            #endregion
        }

        /// <summary>
        /// 非ジェネリックのリストを <see cref="FileProperty"/> の列へ安全に変換する。<see langword="null"/> の場合は空の列を返す。
        /// </summary>
        /// <param name="list">変換元のリスト。</param>
        /// <returns><see cref="FileProperty"/> の列。</returns>
        private static IEnumerable<FileProperty> IListSafeCastToFileProperties(IList list)
        {
            return list?.SafeCast<FileProperty>() ?? Enumerable.Empty<FileProperty>();
        }

        /// <summary>
        /// 指定ファイルを、拡張子に対応するカスタム起動設定(あれば)に従って開く。ファイルが存在しない場合はエラー扱いにする。
        /// </summary>
        /// <param name="path">開くファイルの絶対パス。</param>
        /// <param name="showMessage">結果やエラーをステータス・メッセージボックスに表示するかどうか。</param>
        private void OpenFileInternal(string path, bool showMessage)
        {
            if (!File.Exists(path))
            {
                if (showMessage)
                {
                    StatusText.Value = $"FileNotExist. file:{path}.";
                    MessageBox.Show("ファイルが存在しません。", "FileOpenError", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return;
            }

            var processStartInfo = CreateProcessStartInfo(path, CustomApplicationProcesses);
            processStartInfo.UseShellExecute = true;

            if (showMessage)
            {
                StatusText.Value = $"FileOpen. [{ConvertToCommandlineText(processStartInfo)}].";
            }

            Process.Start(processStartInfo);
        }

        /// <summary>
        /// 選択されたファイル群から取り出した文字列を改行区切りでクリップボードにコピーする。コピーに失敗した場合はメッセージを表示する。
        /// </summary>
        /// <param name="fs">対象のファイル(<see cref="FileProperty"/>)を含むリスト。</param>
        /// <param name="getStringFunc">各ファイルからコピーする文字列を取り出す関数(ファイル名・相対/絶対パスなど)。</param>
        private static void CopyToClipboard(IList fs, Func<IEnumerable<FileProperty>, IEnumerable<string>> getStringFunc)
        {
            var fileProperties = IListSafeCastToFileProperties(fs);

            if (!fileProperties.Any())
            {
                return;
            }

            if (!SafeClip(string.Join(Environment.NewLine, getStringFunc(fileProperties))))
            {
                MessageBox.Show("コピーに失敗しました。");
            }
        }

        /// <summary>
        /// エクスプローラー(EXPLORER.EXE)に渡す引数を生成する。ファイルの場合は選択状態で開く <c>/select</c>、フォルダーの場合はそのパスを返す。
        /// </summary>
        /// <param name="p">対象のファイルまたはフォルダーのパス。</param>
        /// <returns>エクスプローラー用の引数。対象が存在しない場合は空文字。</returns>
        private static string CreateExploereArgument(string p) => File.Exists(p) ? $"/select,{p}" : Directory.Exists(p) ? p : string.Empty;

        /// <summary>
        /// ファイルの拡張子に対応するカスタム起動設定を考慮して、<see cref="ProcessStartInfo"/> を生成する。
        /// 引数に <c>%0</c> が含まれる場合はファイルパスで置換し、含まれない場合は末尾に付与する。
        /// </summary>
        /// <param name="filePath">開くファイルの絶対パス。</param>
        /// <param name="customApplicationProcesses">拡張子ごとのカスタム起動設定の一覧。</param>
        /// <returns>プロセス起動情報。</returns>
        private static ProcessStartInfo CreateProcessStartInfo(string filePath, IEnumerable<CustomApplicationProcess> customApplicationProcesses)
        {
            var extensionText = Path.GetExtension(filePath);

            if (!customApplicationProcesses.Any(item => item.IsEnabled.Value && item.Extension.Value == extensionText))
            {
                return new(filePath.ToWrappedStringInDoubleQuotes());
            }

            var customParam = customApplicationProcesses.First(item => item.IsEnabled.Value && item.Extension.Value == extensionText);

            if (!customParam.Args.Value.Contains(@"%0"))
            {
                return new(customParam.Application.Value.ToWrappedStringInDoubleQuotes(), string.Join(" ", customParam.Args.Value, filePath.ToWrappedStringInDoubleQuotes()));
            }

            return new(customParam.Application.Value.ToWrappedStringInDoubleQuotes(), customParam.Args.Value.Replace(@"%0", filePath.ToWrappedStringInDoubleQuotes()));
        }

        /// <summary>
        /// <see cref="ProcessStartInfo"/> を「実行ファイル 引数」形式のコマンドライン文字列に変換する(ステータス表示用)。
        /// </summary>
        /// <param name="info">対象のプロセス起動情報。</param>
        /// <returns>コマンドライン文字列。</returns>
        private static string ConvertToCommandlineText(ProcessStartInfo info) => string.Join(" ", info.FileName, info.Arguments);

        /// <summary>
        /// ディレクトリを検索対象に含める(配下を走査する)かどうかを判定する述語を生成する。ドット始まり・隠し・システムの各条件で除外する。
        /// </summary>
        /// <param name="isExcludeDotStartDirectory">ドットで始まるディレクトリを除外するかどうか。</param>
        /// <param name="isExcludeHiddenDirectory">隠しディレクトリを除外するかどうか。</param>
        /// <param name="isExcludeSystemDirectory">システムディレクトリを除外するかどうか。</param>
        /// <returns>対象に含める場合に <see langword="true"/> を返す述語。</returns>
        /// <remarks>名前・属性は列挙時の検索データ(<see cref="FileSystemEntry"/>)から取るため、追加のシステム呼び出しは発生しない。</remarks>
        private static FileSystemEnumerable<FoundFile>.FindPredicate CreateValidatorForDirectory(bool isExcludeDotStartDirectory, bool isExcludeHiddenDirectory, bool isExcludeSystemDirectory)
        {
            var excludeAttributes = (isExcludeHiddenDirectory ? FileAttributes.Hidden : 0) | (isExcludeSystemDirectory ? FileAttributes.System : 0);

            return (ref FileSystemEntry entry) =>
                !(isExcludeDotStartDirectory && entry.FileName.StartsWith('.'))
                && (entry.Attributes & excludeAttributes) == 0;
        }

        /// <summary>
        /// ファイルを検索対象に含めるかどうかを判定する述語を生成する。ドット始まり・隠し・システム・除外拡張子の各条件で除外する。
        /// </summary>
        /// <param name="isExcludeDotStartFile">ドットで始まるファイルを除外するかどうか。</param>
        /// <param name="isExcludeHiddenFile">隠しファイルを除外するかどうか。</param>
        /// <param name="isExcludeSystemFile">システムファイルを除外するかどうか。</param>
        /// <param name="extensionTexts">除外する拡張子(先頭にドットを含む)の一覧。大文字小文字は区別しない。</param>
        /// <returns>対象に含める場合に <see langword="true"/> を返す述語。</returns>
        /// <remarks>
        /// 除外拡張子は生成時に <see cref="HashSet{T}"/> へ確定させ、ファイルごとに元の一覧を評価し直さない。
        /// 拡張子は代替ルックアップで文字列を生成せずに照合する。
        /// </remarks>
        private static FileSystemEnumerable<FoundFile>.FindPredicate CreateValidatorForFile(bool isExcludeDotStartFile, bool isExcludeHiddenFile, bool isExcludeSystemFile, IEnumerable<string> extensionTexts)
        {
            var excludeAttributes = (isExcludeHiddenFile ? FileAttributes.Hidden : 0) | (isExcludeSystemFile ? FileAttributes.System : 0);
            var excludeExtensions = new HashSet<string>(extensionTexts, StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();

            return (ref FileSystemEntry entry) =>
                !(isExcludeDotStartFile && entry.FileName.StartsWith('.'))
                && (entry.Attributes & excludeAttributes) == 0
                && !excludeExtensions.Contains(Path.GetExtension(entry.FileName));
        }

        /// <summary>
        /// 1つの文字列がフィルター条件に一致するかを判定する述語を生成する。検索文字列が空なら常に一致とみなす。
        /// </summary>
        /// <param name="useRegex">正規表現として扱うかどうか。<see langword="false"/> の場合は大文字小文字を無視した部分一致。</param>
        /// <param name="searchText">検索文字列。</param>
        /// <returns>一致する場合に <see langword="true"/> を返す述語。</returns>
        private static Func<string,bool> CreateIsMatchSingleFunc(bool useRegex, string searchText)
        {
            if(string.IsNullOrEmpty(searchText))
            {
                return _ => true;
            }

            if(useRegex)
            {
                //NOTE:全行に対して照合するため、コンパイル済みにして1件あたりの照合を速くする。
                Regex r = new(searchText, RegexOptions.Compiled);
                return r.IsMatch;
            }

            //NOTE:カルチャ依存の比較は1件あたりのコストが高いため、序数比較(大文字小文字無視)にする。
            return target => target.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 日時が基準値に対して指定した比較条件を満たすかを判定する述語を生成する。基準値が <see langword="null"/> なら常に一致とみなす。
        /// </summary>
        /// <param name="type">比較条件(等しい・より大きい・より小さいなど)。</param>
        /// <param name="dateTime">比較の基準となる日時。</param>
        /// <returns>条件を満たす場合に <see langword="true"/> を返す述語。</returns>
        /// <exception cref="NotImplementedException"><paramref name="type"/> が未対応の値の場合。</exception>
        private static Func<DateTime?, bool> CreateIsMatchDatetimeFunc(ComparisonConditionType type, DateTime? dateTime)
        {
            if(dateTime is null)
            {
                return _ => true;
            }

            return type switch
            {
                ComparisonConditionType.Equal => dt => dt.HasValue && dt.Value == dateTime,
                ComparisonConditionType.NotEqual => dt => dt.HasValue && dt != dateTime,
                ComparisonConditionType.GreaterThan => dt => dt.HasValue && dt > dateTime,
                ComparisonConditionType.GreaterThanOrEqual => dt => dt.HasValue && dt >= dateTime,
                ComparisonConditionType.LessThan => dt => dt.HasValue && dt < dateTime,
                ComparisonConditionType.LessThanOrEqual => dt => dt.HasValue && dt <= dateTime,
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// 文字列の列のいずれかがフィルター条件に一致するかを判定する述語を生成する。空の列かつ検索文字列が空の場合は一致とみなす。
        /// </summary>
        /// <param name="useRegex">正規表現として扱うかどうか。</param>
        /// <param name="searchText">検索文字列。</param>
        /// <returns>いずれかの要素が一致する場合に <see langword="true"/> を返す述語。</returns>
        private static Func<IEnumerable<string>, bool> CreateIsMatchListFunc(bool useRegex, string searchText)
        {
            var f = CreateIsMatchSingleFunc(useRegex,searchText);
            return list => !list.Any() && string.IsNullOrEmpty(searchText) ? true : list.Any(n => f(n));
        }

        /// <summary>
        /// 進捗率と不確定フラグから、タスクバーに表示する進捗状態を決定する。
        /// </summary>
        /// <param name="progressPer">進捗率(0.0〜1.0)。</param>
        /// <param name="isIndeterminate">進捗が不確定かどうか。</param>
        /// <returns>タスクバーの進捗状態。</returns>
        private static TaskbarItemProgressState GetProgressState(double progressPer, bool isIndeterminate)
        {
            if(isIndeterminate)
            {
                return TaskbarItemProgressState.Indeterminate;
            }

            if (progressPer == 0.0 || progressPer == 1.0)
            {
                return TaskbarItemProgressState.None;
            }

            return TaskbarItemProgressState.Normal;
        }

        /// <summary>
        /// 総数と処理済み数から進捗率(0.0〜1.0)を算出する。
        /// </summary>
        /// <param name="maxCount">処理対象の総数。</param>
        /// <param name="progressCount">処理済みの数。</param>
        /// <returns>進捗率。</returns>
        private static double CalculateProgressPer(int maxCount, int progressCount)
        {
            if (progressCount == 0)
            {
                return 0;
            }
            if (progressCount == maxCount)
            {
                return 1;
            }

            return (double)progressCount / maxCount;
        }

        /// <summary>
        /// 現在の進捗状況に応じてプログレスバーに表示する文字列を生成する。
        /// </summary>
        /// <param name="progressPer">進捗率(0.0〜1.0)。</param>
        /// <param name="maxCount">処理対象の総数。</param>
        /// <param name="successCount">処理済みの数。</param>
        /// <param name="isIndeterminate">進捗が不確定かどうか。</param>
        /// <param name="completed">検索が完了しているかどうか。</param>
        /// <param name="isLastSaveTimeRefreshing">前回保存日時の更新処理中かどうか。</param>
        /// <returns>表示用の進捗テキスト。</returns>
        private static string GetProgressText(double progressPer = 0.0, int maxCount = 0, int successCount = 0, bool isIndeterminate = false, bool completed = false, bool isLastSaveTimeRefreshing = false)
        {
            if(isLastSaveTimeRefreshing)
            {
                return $"Refreshing last save time...";
            }

            if(completed && maxCount > 0)
            {
                return $"{maxCount:#,0} Files Completed!";
            }

            if (progressPer == 1.0)
            {
                return "Refreshing...";
            }

            if(isIndeterminate || maxCount > 0)
            {
                return $"{successCount:#,0} / {maxCount:#,0}";
            }


            return string.Empty;
        }

        /// <summary>
        /// 例外を握りつぶして安全にクリップボードへ文字列を設定する。
        /// </summary>
        /// <param name="text">クリップボードに設定する文字列。</param>
        /// <returns>設定に成功した場合は <see langword="true"/>、失敗した場合は <see langword="false"/>。</returns>
        public static bool SafeClip(string text)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 前回保存日時の更新など、バックグラウンドで動作中のサブスレッド処理をキャンセルし、関連リソースを破棄する。
        /// </summary>
        private void CancelSearchSubThread()
        {
            _cancellationTokenSource?.Cancel();
            _refreshingLastSaveTimesTask?.Wait();
            _refreshingLastSaveTimesTask?.Dispose();
            _refreshingLastSaveTimesTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// ウィンドウが閉じられる際に、バックグラウンド処理をキャンセルしてからクローズを許可する。
        /// </summary>
        /// <returns>常に <see langword="false"/>(クローズをキャンセルしない)。</returns>
        bool IWindowClosing.OnClosing()
        {
            CancelSearchSubThread();

            return false;
        }
    }

    /// <summary>
    /// XAML デザイナー用の <see cref="MainWindowViewModel"/>。デザイン時プレビューのためのサンプルデータを投入する。
    /// </summary>
    public class MainWindowViewModelDesignMode : MainWindowViewModel
    {
        /// <summary>
        /// デザイン時に表示するサンプルのファイル項目を追加し、対応するフォルダー列の表示状態を設定する。
        /// </summary>
        public MainWindowViewModelDesignMode()
            : base(Options.Create(new UserSettings()), new DesignTimeUserSettingsStore())
        {
            _fileProperties.Add(new(@"C:\Windows\System32\notepad.exe", @"C:\Windows"));

            foreach (int index in Enumerable.Range(0, ConstantObject.MaxDirectoryColumnCount))
            {
                IsDirectoriesExists[index].Value = _fileProperties.Any(f => index < f.DirectoryDepth);
            }
        }

        /// <summary>
        /// デザイン時用のユーザー設定保存先。デザイナーから実際の設定ファイルを書き換えないよう、何も保存しない。
        /// </summary>
        private class DesignTimeUserSettingsStore : IUserSettingsStore
        {
            /// <summary>デザイン時は設定ファイルを読み込まないため、常に <see langword="null"/>。</summary>
            public string? LoadErrorMessage => null;

            /// <summary>
            /// 何もしない。
            /// </summary>
            /// <param name="settings">保存対象のユーザー設定(無視される)。</param>
            public void Save(UserSettings settings)
            {
            }
        }
    }
}
