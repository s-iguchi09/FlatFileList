using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FlatFileList.Behaviors
{
    /// <summary>
    /// <see cref="DataGrid"/> に対するフォーカス操作や選択項目へのスクロールを提供するビヘイビア。
    /// </summary>
    internal class DataGridBehavior : Behavior<DataGrid>
    {
        /// <summary>
        /// アタッチされている <see cref="DataGrid"/> にフォーカスを設定する。
        /// </summary>
        public void Focus()
        {
            AssociatedObject.Focus();
        }

        /// <summary>
        /// 選択中の項目が表示範囲に入るようにスクロールする。選択項目がない場合は何もしない。
        /// </summary>
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
