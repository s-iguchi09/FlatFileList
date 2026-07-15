using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace FlatFileList.Behaviors
{
    public class FolderSelectBehavior : Behavior<ButtonBase>
    {
        // ViewModelのプロパティとバインドするための依存関係プロパティ
        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register(nameof(SelectedPath), typeof(string), typeof(FolderSelectBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string SelectedPath
        {
            get => (string)GetValue(SelectedPathProperty);
            set => SetValue(SelectedPathProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            // アタッチされた要素（ボタンなど）のクリックイベントを購読
            if (AssociatedObject is ButtonBase button)
            {
                button.Click += OnClick;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject is ButtonBase button)
            {
                button.Click -= OnClick;
            }
            base.OnDetaching();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
#if NETFRAMEWORK
            var dialog = new OpenFileDialog
            {
                Title = "フォルダを選択してください",
                Filter = "Directory|*.this.directory", // ユーザーに見せないダミー
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false,
                FileName = "Folder Selection" // フォルダ選択モードをシミュレート
            };

            if (dialog.ShowDialog() == true)
            {
                // ファイル名ではなく、その親ディレクトリのパスを取得
                SelectedPath = Path.GetDirectoryName(dialog.FileName)!;
            }
#else
            var dialog = new OpenFolderDialog
            {
                Title = "フォルダを選択してください",
                InitialDirectory = SelectedPath,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedPath = dialog.FolderName;
            }
#endif
        }
    }
}
