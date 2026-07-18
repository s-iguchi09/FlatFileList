using FlatFileList.Datas;
using Reactive.Bindings;
using System.IO;
using System.Reflection;

namespace FlatFileList
{
    /// <summary>バージョン情報ダイアログのViewModel。</summary>
    public class AboutWindowViewModel : IDisposable
    {
        /// <summary>保持しているリアクティブリソースを破棄する。</summary>
        public void Dispose() => SelectedLicense.Dispose();

        /// <summary>ダイアログ全体の表示倍率(メインウィンドウに合わせる)。</summary>
        public double WindowScale { get; }

        /// <summary>ヘッダーに表示するバージョン文字列。</summary>
        public string VersionText { get; }

        /// <summary>ライセンス一覧(先頭はアプリ自身)。ライブラリ追加時は Resources\Licenses\&lt;表示名&gt;.txt にライセンス全文を置き、ここに1行追加する。</summary>
        public IReadOnlyList<LicenseInfo> Licenses { get; }

        /// <summary>一覧で選択中のエントリ。</summary>
        public ReactivePropertySlim<LicenseInfo?> SelectedLicense { get; }

        /// <summary>
        /// バージョン文字列とライセンス一覧を構築する。
        /// </summary>
        /// <param name="windowScale">ダイアログ全体の表示倍率(メインウィンドウに合わせる)。</param>
        public AboutWindowViewModel(double windowScale)
        {
            WindowScale = windowScale;
            VersionText = $"Version {GetVersion(typeof(AboutWindowViewModel).Assembly)}";

            // バージョンは各ライブラリのアセンブリから実行時に取得するため、パッケージ更新時の修正は不要。
            Licenses =
            [
                CreateEntry("FlatFileList", typeof(AboutWindowViewModel).Assembly),
                CreateEntry("MaterialDesignThemes", typeof(MaterialDesignThemes.Wpf.PackIcon).Assembly),
                CreateEntry("MaterialDesignColors", typeof(MaterialDesignColors.SwatchesProvider).Assembly),
                CreateEntry("Microsoft.Xaml.Behaviors.Wpf", typeof(Microsoft.Xaml.Behaviors.Behavior).Assembly),
                CreateEntry("ReactiveProperty.WPF", typeof(Reactive.Bindings.Interactivity.EventToReactiveCommand).Assembly),
                CreateEntry("ReactiveProperty", typeof(ReactiveProperty<object>).Assembly),
                CreateEntry("ReactiveProperty.Core", typeof(ReactivePropertySlim<object>).Assembly),
                CreateEntry("System.Reactive", typeof(System.Reactive.Unit).Assembly),
            ];

            SelectedLicense = new(Licenses[0]);
        }

        /// <summary>表示名と同名の埋め込みリソースからライセンス全文を読み込んでエントリを作る。</summary>
        private static LicenseInfo CreateEntry(string name, Assembly assembly)
            => new(name, GetVersion(assembly), LoadLicenseText(name));

        /// <summary>アセンブリのバージョン文字列を取得する(ビルドメタデータ「+」以降は除く)。</summary>
        private static string GetVersion(Assembly assembly)
        {
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrEmpty(informational))
            {
                return assembly.GetName().Version?.ToString(3) ?? string.Empty;
            }
            var plusIndex = informational.IndexOf('+');
            return plusIndex < 0 ? informational : informational[..plusIndex];
        }

        /// <summary>埋め込みリソース Resources\Licenses\&lt;<paramref name="name"/>&gt;.txt からライセンス全文を読み込む。</summary>
        private static string LoadLicenseText(string name)
        {
            var resourceName = $"FlatFileList.Resources.Licenses.{name}.txt";
            using var stream = typeof(AboutWindowViewModel).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return $"(ライセンスファイルが見つかりません: {resourceName})";
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
