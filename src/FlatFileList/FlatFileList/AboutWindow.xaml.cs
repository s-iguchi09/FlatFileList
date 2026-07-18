using System.Windows;

namespace FlatFileList;

/// <summary>
/// バージョン情報ダイアログ。アプリ情報と利用ライブラリのライセンス全文を表示する。
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(double windowScale)
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel(windowScale);
    }

    protected override void OnClosed(EventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }
}
