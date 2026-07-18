using System.Windows;

namespace FlatFileList;

/// <summary>
/// バージョン情報ダイアログ。アプリ情報と利用ライブラリのライセンス全文を表示する。
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>
    /// バージョン情報ダイアログを初期化する。
    /// </summary>
    /// <param name="windowScale">ダイアログ全体の表示倍率(呼び出し元のメインウィンドウに合わせる)。</param>
    public AboutWindow(double windowScale)
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel(windowScale);
    }

    /// <summary>
    /// ウィンドウが閉じられた際に、DataContext(ViewModel)が <see cref="IDisposable"/> を実装していれば破棄する。
    /// </summary>
    /// <param name="e">イベントの引数。</param>
    protected override void OnClosed(EventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }
}
