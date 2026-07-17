using FlatFileList.Datas;
using FlatFileList.Extensions;
using FlatFileList.Interfaces;
using FlatFileList.Utilities;
using FlatFileList.Utilities.Shell32;
using Microsoft.Xaml.Behaviors.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;

namespace FlatFileList
{
    public class MainWindowViewModel : INotifyPropertyChanged, IWindowClosing, IDisposable
    {
#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        private CompositeDisposable _disposable = new();
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

        public ReactivePropertySlim<bool> IsIgnoreGettingLastSaveTime { get; } = new(Properties.Settings.Default.IsIgnoreGettingLastSaveTime);
        public ReactivePropertySlim<bool> IsExcludeHiddenDirectory { get; } = new(Properties.Settings.Default.IsExcludeHiddenDirectory);
        public ReactivePropertySlim<bool> IsExcludeDotStartDirectory { get; } = new(Properties.Settings.Default.IsExcludeDotStartDirectory);
        public ReactivePropertySlim<bool> IsExcludeSystemDirectory { get; } = new(Properties.Settings.Default.IsExcludeSystemDirectory);
        public ReactivePropertySlim<bool> IsExcludeDotStartFile { get; } = new(Properties.Settings.Default.IsExcludeDotStartFile);
        public ReactivePropertySlim<bool> IsExcludeHiddenFile { get; } = new(Properties.Settings.Default.IsExcludeHiddenFile);
        public ReactivePropertySlim<bool> IsExcludeSystemFile { get; } = new(Properties.Settings.Default.IsExcludeSystemFile);

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

        //NOTE:UI スレッドへの追加をまとめる単位。
        private const int AddBatchSize = 500;

        public CollectionViewSource FileProperties { get; }
        protected ReactiveCollection<FileProperty> _fileProperties { get; } = [];

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

        #endregion

        #region subthread関連

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _refreshingLastSaveTimesTask;

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
        public ReactivePropertySlim<int> WindowScaleTransformPer { get; } = new(Properties.Settings.Default.WindowScaleTransformPer);
        public ReadOnlyReactivePropertySlim<double> WindowScale { get; }
        #endregion

        public MainWindowViewModel()
        {
            using var statusTextRefresh = Disposable.Create(() => StatusText.Value = string.Empty);

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
            FileProperties.FilterAsObservable().Subscribe(e => e.Accepted = !IsHighlightVisibled.Value || ((FileProperty)e.Item).IsHighlighted.Value).AddTo(_disposable);

            ExistsFileLists = _fileProperties.CollectionChangedAsObservable().Select(_ => _fileProperties.Any()).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            _watcher.AddTo(_disposable);
            _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            _watcher.ChangedAsObservable().SubscribeOnUIDispatcher().Subscribe(e =>
            {
                var item = _fileProperties.SingleOrDefault(p => p.FilePath == e.FullPath);
                item?.UpdateLastWriteTime();
                Thread t = new(() =>
                {
                    if (item is null)
                    {
                        return;
                    }

                    using ShellPropertyReader reader = new();
                    item.UpdateModifiedTime(IsIgnoreGettingLastSaveTime.Value, reader);
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }).AddTo(_disposable);
            _watcher.IncludeSubdirectories = true;

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
                    FileProperties.View.Refresh();
                }
            }

            Observable.Merge(FilteringFileNameText.ToUnit(),
                             FilteringExtensionText.ToUnit(),
                             FilteringDirectoryNameText.ToUnit(),
                             FilteringLastWriteTime.ToUnit(),
                             FilteringModifiedTime.ToUnit(),
                             IsRegexFileNameSearchEnabled.ToUnit(),
                             IsRegexExtensionTextSearchEnabled.ToUnit(),
                             IsRegexDirectoryNameSearchEnabled.ToUnit(),
                             FilteringLastWriteTimeComparisonConditionType.ToUnit(),
                             FilteringModifiedTimeComparisonConditionType.ToUnit())
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
                //NOTE:FileProperty は Rx の購読を抱えるため、Clear の前に破棄しないと検索のたびに蓄積して徐々に重くなる。
                foreach (var f in _fileProperties)
                {
                    f.Dispose();
                }
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

                await Task.Run(() =>
                {
                    Debug.WriteLine($"1.{DateTime.Now.ToString()}");
                    var allFilesString = Enumerable.Empty<string>();

                    IsIndeterminate.Value = true;
                    using (Disposable.Create(() => IsIndeterminate.Value = false))
                    {
                        RootPath.Value = DirectoryPath.Value.Last() == '\\' ? DirectoryPath.Value : $"{DirectoryPath.Value}\\";
                        Func<string, bool> validateDirectoryFunc = CreateValidatorForDirectory(IsExcludeDotStartDirectory.Value, IsExcludeHiddenDirectory.Value, IsExcludeSystemDirectory.Value);
                        Func<string, bool> validateFileFunc = CreateValidatorForFile(IsExcludeDotStartFile.Value, IsExcludeHiddenFile.Value, IsExcludeSystemFile.Value, ExcludeExtensions.Where(ext => ext.Enabled.Value).Select(ext =>ext.ExtensionText.StartsWith(".") ? ext.ExtensionText : $".{ext.ExtensionText}"));
                        var foundCount = 0;
                        allFilesString = DirectoryEx.GetAllFiles(RootPath.Value, validateDirectoryFunc).Where(p => validateFileFunc(p)).Select(f =>
                        {
                            foundCount++;
                            if (foundCount % ProgressReportInterval == 0)
                            {
                                StatusText.Value = $"File found. [\"{f}\"]";
                                MaxCount.Value = foundCount;
                            }
                            return f;
                        }).ToList();
                        MaxCount.Value = allFilesString.Count();
                    }

                    Debug.WriteLine($"2.{DateTime.Now.ToString()}");

                    Func<string, bool> funcIsFileNameMatch = CreateIsMatchSingleFunc(IsRegexFileNameSearchEnabled.Value, FilteringFileNameText.Value);
                    Func<string, bool> funcIsExtensionTextMatch = CreateIsMatchSingleFunc(IsRegexExtensionTextSearchEnabled.Value, FilteringExtensionText.Value);
                    Func<DateTime?, bool> funcIsLastWriteTimeMatch = CreateIsMatchDatetimeFunc(FilteringLastWriteTimeComparisonConditionType.Value, FilteringLastWriteTime.Value);
                    Func<DateTime?, bool> funcIsModifiedTimeMatch = CreateIsMatchDatetimeFunc(FilteringModifiedTimeComparisonConditionType.Value, FilteringModifiedTime.Value);
                    Func<string, bool> funcIsDirectoryNamesMatch = CreateIsMatchSingleFunc(IsRegexDirectoryNameSearchEnabled.Value, FilteringDirectoryNameText.Value);

                    //NOTE:以前はファイル1件ごとに Dispatcher.Invoke して UI スレッド上で FileProperty を構築していたため、
                    //     1件につき同期マーシャリング1回 + 進捗更新の Dispatcher ポストが発生していた。
                    //     FileIcon は Freeze 済みでスレッドを跨げるため、構築はこのバックグラウンドスレッドで行い、
                    //     UI スレッドへの追加のみをバッチにまとめる。
                    var allFiles = new List<FileProperty>(allFilesString.Count());
                    var batch = new List<FileProperty>(AddBatchSize);

                    void FlushBatch()
                    {
                        if (batch.Count == 0)
                        {
                            return;
                        }

                        var adding = batch.ToArray();
                        batch.Clear();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var f in adding)
                            {
                                _fileProperties.Add(f);
                            }
                        });
                    }

                    foreach (var p in allFilesString)
                    {
                        FileProperty f = new(p, RootPath.Value);
                        f.UpdateIsHighlighted(funcIsFileNameMatch, funcIsExtensionTextMatch, funcIsDirectoryNamesMatch, funcIsLastWriteTimeMatch, funcIsModifiedTimeMatch);

                        allFiles.Add(f);
                        batch.Add(f);

                        if (batch.Count >= AddBatchSize)
                        {
                            FlushBatch();
                            SuccessedCount.Value = allFiles.Count;
                        }
                    }

                    FlushBatch();
                    SuccessedCount.Value = allFiles.Count;

                    Debug.WriteLine($"3.{DateTime.Now.ToString()}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (int index in Enumerable.Range(0, ConstantObject.MaxDirectoryColumnCount))
                        {
                            IsDirectoriesExists[index].Value = allFiles.Any(f => !string.IsNullOrEmpty(f.Directories.Skip(index)?.FirstOrDefault()?.Name ?? string.Empty));
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
                _refreshingLastSaveTimesTask = new(()=> 
                    {
                        IsLastSaveTimeRefreshing.Value = true;
                        using var backgroundTaskRaunning = Disposable.Create(() => IsLastSaveTimeRefreshing.Value = false);

                        SuccessedCount.Value = 0;

                        Thread t = new(() =>
                        {
                            //NOTE:Shell.Application と Folder の生成は高コストなため、走査全体で1つの reader を使い回す。
                            using ShellPropertyReader reader = new();

                            var count = 0;
                            foreach (var f in _fileProperties)
                            {
                                if (token.IsCancellationRequested)
                                    break;

                                count++;
                                //NOTE:SuccessedCount は ReactiveProperty のため書き込みごとに Dispatcher へポストされ、
                                //     派生する ProgressPer/ProgressBarText/WindowProgressState も再計算される。
                                //     1件ごとに更新すると UI スレッドが飽和してスクロールが止まるため間引く。
                                if (count % ProgressReportInterval == 0)
                                {
                                    SuccessedCount.Value = count;
                                }

                                f.UpdateModifiedTime(IsIgnoreGettingLastSaveTime.Value, reader);
                            }

                            SuccessedCount.Value = count;
                        });
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();
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

            foreach (var item in Properties.Settings.Default?.CustomOpenProcess?.SafeCast<string>().Where(s => !string.IsNullOrEmpty(s)) ?? [])
            {
                CustomApplicationProcesses.Add(CustomApplicationProcess.CreateFromSetting(item ?? string.Empty));
            }

            foreach(var item in Properties.Settings.Default?.ExcludeExtensions?.SafeCast<string>().Where(s => !string.IsNullOrEmpty(s)) ?? [])
            {
                ExcludeExtensions.Add(ExcludeExtension.CreateFromSetting(item ?? string.Empty));
            }

            #endregion

            #region UserProfile更新

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
                      .Subscribe(_ =>
                      {
                          using var save = Disposable.Create(() => Properties.Settings.Default.Save());
                          Properties.Settings.Default.IsExcludeDotStartDirectory = IsExcludeDotStartDirectory.Value;
                          Properties.Settings.Default.IsExcludeHiddenDirectory = IsExcludeHiddenDirectory.Value;
                          Properties.Settings.Default.IsExcludeSystemDirectory = IsExcludeSystemDirectory.Value;
                          Properties.Settings.Default.IsExcludeDotStartFile = IsExcludeDotStartFile.Value;
                          Properties.Settings.Default.IsExcludeHiddenFile = IsExcludeHiddenFile.Value;
                          Properties.Settings.Default.IsExcludeSystemFile = IsExcludeSystemFile.Value;
                          Properties.Settings.Default.IsIgnoreGettingLastSaveTime = IsIgnoreGettingLastSaveTime.Value;
                          Properties.Settings.Default.CustomOpenProcess.Clear();
                          Properties.Settings.Default.CustomOpenProcess.AddRange(CustomApplicationProcesses.Select(c => c.ToSettingParameter()).ToArray());
                          Properties.Settings.Default.ExcludeExtensions.Clear();
                          Properties.Settings.Default.ExcludeExtensions.AddRange(ExcludeExtensions.Select(e => e.ToSettingParameter()).ToArray());
                          Properties.Settings.Default.WindowScaleTransformPer = WindowScaleTransformPer.Value;
                          StatusText.Value = $"Save User Profile.";
                      })
                      .AddTo(_disposable);

            #endregion

            #region アプリケーション起動プロセス

            RemoveCustomProcessRow.WithSubscribe(l => l.SafeCast<CustomApplicationProcess>().ToList().ForEach(c => CustomApplicationProcesses.Remove(c))).AddTo(_disposable);

            #endregion

            #region 拡大率

            WindowScale = WindowScaleTransformPer.Select(p => p / 100.0).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            #endregion
        }

        private static IEnumerable<FileProperty> IListSafeCastToFileProperties(IList list)
        {
            return list?.SafeCast<FileProperty>() ?? Enumerable.Empty<FileProperty>();
        }

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

        private static string CreateExploereArgument(string p) => File.Exists(p) ? $"/select,{p}" : Directory.Exists(p) ? p : string.Empty;

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

        private static string ConvertToCommandlineText(ProcessStartInfo info) => string.Join(" ", info.FileName, info.Arguments);

        private static Func<string, bool> CreateValidatorForDirectory(bool isExcludeDotStartDirectory, bool isExcludeHiddenDirectory, bool isExcludeSystemDirectory)
        {
            var isDotStartFunc = CreateValidateDotStartFunc(isExcludeDotStartDirectory);
            var fileAttributes = (isExcludeHiddenDirectory ? FileAttributes.Hidden : 0) | (isExcludeSystemDirectory ? FileAttributes.System : 0);
            var isFileAttributesFunc = CreateValidateFileAttributesFunc(isExcludeHiddenDirectory || isExcludeSystemDirectory, fileAttributes);

            return directoryPath =>
            {
                var directoryName = Path.GetFileName(directoryPath);
                var attributes = new DirectoryInfo(directoryPath).Attributes;

                return !isDotStartFunc(directoryName) && !isFileAttributesFunc(attributes);
            };
        }

        private static Func<string, bool> CreateValidatorForFile(bool isExcludeDotStartFile, bool isExcludeHiddenFile, bool isExcludeSystemFile, IEnumerable<string> extensionTexts)
        {
            var isDotStartFunc = CreateValidateDotStartFunc(isExcludeDotStartFile);
            var fileAttributes = (isExcludeHiddenFile ? FileAttributes.Hidden : 0) | (isExcludeSystemFile ? FileAttributes.System : 0);
            var isFileAttributesFunc = CreateValidateFileAttributesFunc(isExcludeHiddenFile || isExcludeSystemFile, fileAttributes);

            return filePath =>
            {
                var fileName = Path.GetFileName(filePath);
                var attributes = File.GetAttributes(filePath);
                var extension = Path.GetExtension(filePath);

                return !isDotStartFunc(fileName) && !isFileAttributesFunc(attributes) && !extensionTexts.Contains(extension);
            };
        }

        public static Func<string, bool> CreateValidateDotStartFunc(bool needsValidate)
        {
            return needsValidate ? fileName => fileName.StartsWith(".") : _ => false;
        }

        public static Func<FileAttributes, bool> CreateValidateFileAttributesFunc(bool needsValidate, FileAttributes fileAttributes)
        {
            return needsValidate ? attributes => (attributes & fileAttributes) > 0 : _ => false;
        }

        private static Func<string,bool> CreateIsMatchSingleFunc(bool useRegex, string searchText)
        {
            if(string.IsNullOrEmpty(searchText))
            {
                return _ => true;
            }

            if(useRegex)
            {
                Regex r = new(searchText);
                return r.IsMatch;
            }

            return target => target.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
        }

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

        private static Func<IEnumerable<string>, bool> CreateIsMatchListFunc(bool useRegex, string searchText)
        {
            var f = CreateIsMatchSingleFunc(useRegex,searchText);
            return list => !list.Any() && string.IsNullOrEmpty(searchText) ? true : list.Any(n => f(n));
        }

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

        private void CancelSearchSubThread()
        {
            _cancellationTokenSource?.Cancel();
            _refreshingLastSaveTimesTask?.Wait();
            _refreshingLastSaveTimesTask?.Dispose();
            _refreshingLastSaveTimesTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        bool IWindowClosing.OnClosing()
        {
            CancelSearchSubThread();

            return false;
        }
    }

    public class MainWindowViewModelDesignMode : MainWindowViewModel
    {
        public MainWindowViewModelDesignMode()
            : base()
        {
            _fileProperties.Add(new(@"C:\Windows\System32\notepad.exe", @"C:\Windows"));

            foreach (int index in Enumerable.Range(0, ConstantObject.MaxDirectoryColumnCount))
            {
                IsDirectoriesExists[index].Value = _fileProperties.Any(f => !string.IsNullOrEmpty(f.Directories.Skip(index)?.FirstOrDefault()?.Name ?? string.Empty));
            }
        }
    }
}
