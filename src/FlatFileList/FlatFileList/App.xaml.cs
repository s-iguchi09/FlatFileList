using System.Globalization;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Markup;

namespace FlatFileList;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// アプリケーションを初期化する。旧バージョンからのユーザー設定移行と、未初期化コレクション設定の既定値割り当てを行う。
    /// </summary>
    public App()
    {
        if(!FlatFileList.Properties.Settings.Default.IsUpgreated)
        {
            using(Disposable.Create(()=> { FlatFileList.Properties.Settings.Default.IsUpgreated = true; FlatFileList.Properties.Settings.Default.Save(); }))
            {
                FlatFileList.Properties.Settings.Default.Upgrade();
            }
        }

        if (FlatFileList.Properties.Settings.Default.CustomOpenProcess == null)
            FlatFileList.Properties.Settings.Default.CustomOpenProcess = [];

        if (FlatFileList.Properties.Settings.Default.ExcludeExtensions == null)
            FlatFileList.Properties.Settings.Default.ExcludeExtensions = [];
    }

    /// <summary>
    /// アプリケーション起動時の処理。UI要素の言語を現在のカルチャに合わせて設定する。
    /// </summary>
    /// <param name="e">起動イベントの引数。</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
          typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
              XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        base.OnStartup(e);
    }
}

