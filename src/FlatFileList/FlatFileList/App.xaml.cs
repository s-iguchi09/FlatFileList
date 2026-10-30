using FlatFileList.Datas;
using FlatFileList.Interfaces;
using FlatFileList.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace FlatFileList;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// アプリケーション全体のサービス(ユーザー設定・ViewModel・ウィンドウ)を保持する汎用ホスト。
    /// </summary>
    private readonly IHost _host;

    /// <summary>
    /// アプリケーションを初期化する。ユーザー設定ファイルを構成に読み込み、各サービスを登録した汎用ホストを構築する。
    /// 設定ファイルが壊れている・値を変換できない場合は読み込まず、既定値で起動する。
    /// </summary>
    public App()
    {
        var builder = Host.CreateApplicationBuilder();

        var userSettingsStore = new UserSettingsStore();
        builder.Configuration.AddInMemoryCollection(userSettingsStore.LoadFile());

        builder.Services.Configure<UserSettings>(builder.Configuration.GetSection(UserSettings.SectionName));
        builder.Services.AddSingleton<IUserSettingsStore>(userSettingsStore);
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
    }

    /// <summary>
    /// アプリケーション起動時の処理。UI要素の言語を現在のカルチャに合わせて設定し、ホストを開始してメインウィンドウを表示する。
    /// </summary>
    /// <param name="e">起動イベントの引数。</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(
          typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
              XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        base.OnStartup(e);

        _host.Start();
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    /// <summary>
    /// アプリケーション終了時の処理。ホストを停止し、登録済みサービス(ViewModel など)を破棄する。
    /// </summary>
    /// <param name="e">終了イベントの引数。</param>
    protected override void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }

        base.OnExit(e);
    }
}
