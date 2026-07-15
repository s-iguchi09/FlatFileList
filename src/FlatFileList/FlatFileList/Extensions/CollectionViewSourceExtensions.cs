using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FlatFileList.Extensions
{
    public static class CollectionViewSourceExtensions
    {
        public static IObservable<FilterEventArgs> FilterAsObservable(this CollectionViewSource collectionViewSource)
        {
            return Observable.FromEvent<FilterEventHandler, FilterEventArgs>(
                h => (sender, e) => h(e),
                h => collectionViewSource.Filter += h,
                h => collectionViewSource.Filter -= h);
        }
    }
}
