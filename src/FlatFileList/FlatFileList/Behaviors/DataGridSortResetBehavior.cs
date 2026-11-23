using FlatFileList.Datas;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FlatFileList.Behaviors
{
    /// <summary>
    /// <see cref="DataGrid"/> のソートを「昇順 → 降順 → 未ソート」の3状態で循環させるビヘイビア。
    /// 降順の次のクリックでソート方向をクリアし、元の並び順に戻す。
    /// </summary>
    /// <remarks>
    /// 既定のソート(SortDescription)は比較のたびにプロパティパスをリフレクションで解決するため、
    /// 列の SortMemberPath に対応する型付き比較器(<see cref="FilePropertyComparer"/>)を
    /// <see cref="ListCollectionView.CustomSort"/> に設定して高速にソートする。対応しない列は既定のソートに任せる。
    /// Shift+クリックでは既定のソートと同様に、既存のソートキーを保ったまま列をキーに追加・変更・解除する。
    /// </remarks>
    public class DataGridSortResetBehavior : Behavior<DataGrid>
    {
        /// <summary>型付き比較器でソートしている列と並び順(優先順)。</summary>
        private readonly List<(DataGridColumn Column, ListSortDirection Direction)> _sortKeys = [];

        /// <summary>
        /// ビヘイビアがアタッチされた際に、<see cref="DataGrid"/> のソートイベントを購読する。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Sorting += AssociatedObject_Sorting;
        }

        /// <summary>
        /// ソート実行時のハンドラー。次のソート方向を決め、型付き比較器でソートする。降順の次はソートを解除する。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">ソート対象の列などを含むイベント引数。</param>
        private void AssociatedObject_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (AssociatedObject.ItemsSource == null) return;

            if (CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource) is not ListCollectionView listColView) return;

            ListSortDirection? nextDirection = e.Column.SortDirection switch
            {
                null => ListSortDirection.Ascending,
                ListSortDirection.Ascending => ListSortDirection.Descending,
                _ => null,
            };

            var isAddingKey = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (nextDirection is null && isAddingKey && listColView.CustomSort is null && _sortKeys.Count == 0)
            {
                // 既定のソート(SortDescriptions)中の Shift+クリックによる解除は、この列のキーだけを取り除き、他のキーは残す
                //NOTE:既定のソート処理は昇順・降順の切り替えしか行わないため、解除はここで行う。
                e.Handled = true;
                var sortMemberPath = e.Column.SortMemberPath;
                for (var index = AssociatedObject.Items.SortDescriptions.Count - 1; index >= 0; index--)
                {
                    if (AssociatedObject.Items.SortDescriptions[index].PropertyName == sortMemberPath)
                    {
                        AssociatedObject.Items.SortDescriptions.RemoveAt(index);
                    }
                }
                e.Column.SortDirection = null;
                return;
            }

            // Shift+クリックは既存のキーを保ち、この列のキーだけを追加・変更・解除する。通常のクリックはこの列だけにする
            var nextKeys = isAddingKey ? new List<(DataGridColumn Column, ListSortDirection Direction)>(_sortKeys) : [];
            var keyIndex = nextKeys.FindIndex(key => key.Column == e.Column);
            if (nextDirection is { } direction)
            {
                if (keyIndex >= 0)
                {
                    nextKeys[keyIndex] = (e.Column, direction);
                }
                else
                {
                    nextKeys.Add((e.Column, direction));
                }
            }
            else if (keyIndex >= 0)
            {
                nextKeys.RemoveAt(keyIndex);
            }

            var comparer = FilePropertyComparer.Create(nextKeys.Select(key => (key.Column.SortMemberPath, key.Direction)).ToList());

            if (nextKeys.Count > 0 && comparer is null)
            {
                // 型付き比較器に対応しない列は、既定のソート処理に任せる
                //NOTE:CustomSort の setter は null の再代入でも SortDescriptions を消去するため、
                //     Shift+クリックによる複数列ソートの既存キーを残すよう、型付きソート中の場合だけ解除する。
                if (listColView.CustomSort is not null)
                {
                    listColView.CustomSort = null;
                    _sortKeys.Clear();
                }
                return;
            }

            //ソートを中断(既定のソート処理を行わせない)
            e.Handled = true;

            foreach (var column in AssociatedObject.Columns)
            {
                if (column != e.Column && !nextKeys.Exists(key => key.Column == column))
                {
                    column.SortDirection = null;
                }
            }

            _sortKeys.Clear();
            _sortKeys.AddRange(nextKeys);

            AssociatedObject.Items.SortDescriptions.Clear();
            //NOTE:CustomSort を設定するとビューが再ソートされる。null で元の並び順に戻る。
            listColView.CustomSort = comparer;
            e.Column.SortDirection = nextDirection;
        }

        /// <summary>
        /// ビヘイビアが取り外された際に、購読していたソートイベントを解除する。
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Sorting -= AssociatedObject_Sorting;
        }

    }
}
