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
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        // バージョン情報ダイアログを表示(表示倍率をメインウィンドウに合わせる)
        var scale = (DataContext as MainWindowViewModel)?.WindowScale.Value ?? 1.0;
        var window = new AboutWindow(scale) { Owner = this };
        window.ShowDialog();
    }
}