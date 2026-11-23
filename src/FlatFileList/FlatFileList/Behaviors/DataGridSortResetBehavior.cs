using FlatFileList.Datas;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

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
    /// </remarks>
    public class DataGridSortResetBehavior : Behavior<DataGrid>
    {

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

            var comparer = nextDirection is { } direction ? FilePropertyComparer.Create(e.Column.SortMemberPath, direction) : null;

            if (nextDirection is not null && comparer is null)
            {
                // 型付き比較器に対応しない列は、既定のソート処理に任せる
                //NOTE:CustomSort の setter は null の再代入でも SortDescriptions を消去するため、
                //     Shift+クリックによる複数列ソートの既存キーを残すよう、型付きソート中の場合だけ解除する。
                if (listColView.CustomSort is not null)
                {
                    listColView.CustomSort = null;
                }
                return;
            }

            //ソートを中断(既定のソート処理を行わせない)
            e.Handled = true;

            foreach (var column in AssociatedObject.Columns)
            {
                if (column != e.Column)
                {
                    column.SortDirection = null;
                }
            }

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
