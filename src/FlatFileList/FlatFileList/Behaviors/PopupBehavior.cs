using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace FlatFileList.Behaviors
{
    /// <summary>
    /// アタッチした <see cref="Popup"/> の開閉を外部から操作できるようにするビヘイビア。
    /// </summary>
    public class PopupBehavior : Behavior<Popup>
    {
        /// <summary>
        /// ポップアップを開く。
        /// </summary>
        public void Open()
        {
            this.AssociatedObject.IsOpen = true;
        }

        /// <summary>
        /// ポップアップを閉じる。
        /// </summary>
        public void Close()
        {
            this.AssociatedObject.IsOpen = false;
        }
    }
}
