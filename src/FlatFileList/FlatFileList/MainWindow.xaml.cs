using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatFileList;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// メインウィンドウを初期化し、<see cref="MainWindowViewModel"/> を DataContext に設定する。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    /// <summary>
    /// バージョン情報ボタンのクリックハンドラー。表示倍率をメインウィンドウに合わせてバージョン情報ダイアログを開く。
    /// </summary>
    /// <param name="sender">イベントの発生元。</param>
    /// <param name="e">ルーティングイベントの引数。</param>
    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        // バージョン情報ダイアログを表示(表示倍率をメインウィンドウに合わせる)
        var scale = (DataContext as MainWindowViewModel)?.WindowScale.Value ?? 1.0;
        var window = new AboutWindow(scale) { Owner = this };
        window.ShowDialog();
    }
}