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
    public class PopupBehavior : Behavior<Popup>
    {
        public void Open()
        {
            this.AssociatedObject.IsOpen = true;
        }

        public void Close()
        {
            this.AssociatedObject.IsOpen = false;
        }
    }
}
