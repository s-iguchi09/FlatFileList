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
    public class DataGridSortResetBehavior : Behavior<DataGrid>
    {

        /// <summary>
        /// ビヘイビアがアタッチされた際に、<see cref="DataGrid"/> のソートイベントを購読する。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Sorting += AssociatedObject_Sorting; ;
        }

        /// <summary>
        /// ソート実行時のハンドラー。降順からさらにソートしようとした場合はソートを中断し、並び順をクリアする。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">ソート対象の列などを含むイベント引数。</param>
        private void AssociatedObject_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (AssociatedObject.ItemsSource == null) return;

            var listColView = (ListCollectionView)CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
            if (listColView == null) return;

            if (e.Column.SortDirection == ListSortDirection.Descending)
            {
                //ソートを中断
                e.Handled = true;
                //ソートの方向をクリア
                e.Column.SortDirection = null;
                AssociatedObject.Items.SortDescriptions.Clear();
            }
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
