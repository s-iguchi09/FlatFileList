using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FlatFileList.Behaviors
{
    internal class DataGridBehavior : Behavior<DataGrid>
    {
        public void Focus()
        {
            AssociatedObject.Focus();
        }

        public void ScrollIntoViewSelectedItem()
        {
            if(AssociatedObject.SelectedItem is null)
            {
                return;
            }

            AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);
        }
    }
}
