using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace FlatFileList.Behaviors
{
    /// <summary>
    /// ボタンのクリックでフォルダー選択ダイアログを開き、選択されたパスを <see cref="SelectedPath"/> に反映するビヘイビア。
    /// .NET Framework では <see cref="OpenFileDialog"/> を、それ以外では <see cref="OpenFolderDialog"/> を使用する。
    /// </summary>
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

        /// <summary>
        /// ビヘイビアがアタッチされた際に、対象ボタンのクリックイベントを購読する。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            // アタッチされた要素（ボタンなど）のクリックイベントを購読
            if (AssociatedObject is ButtonBase button)
            {
                button.Click += OnClick;
            }
        }

        /// <summary>
        /// ビヘイビアが取り外された際に、購読していたクリックイベントを解除する。
        /// </summary>
        protected override void OnDetaching()
        {
            if (AssociatedObject is ButtonBase button)
            {
                button.Click -= OnClick;
            }
            base.OnDetaching();
        }

        /// <summary>
        /// ボタンクリック時のハンドラー。フォルダー選択ダイアログを表示し、選択結果を <see cref="SelectedPath"/> に設定する。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">ルーティングイベントの引数。</param>
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
