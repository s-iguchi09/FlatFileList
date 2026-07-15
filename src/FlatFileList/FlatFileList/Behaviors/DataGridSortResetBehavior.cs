using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace FlatFileList.Behaviors
{
    public class DataGridSortResetBehavior : Behavior<DataGrid>
    {

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Sorting += AssociatedObject_Sorting; ;
        }

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

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Sorting -= AssociatedObject_Sorting;
        }

    }
}
