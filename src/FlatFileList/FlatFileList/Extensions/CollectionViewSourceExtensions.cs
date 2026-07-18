using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FlatFileList.Extensions
{
    /// <summary>
    /// <see cref="CollectionViewSource"/> のイベントを Rx のオブザーバブルとして扱うための拡張メソッドを提供する。
    /// </summary>
    public static class CollectionViewSourceExtensions
    {
        /// <summary>
        /// <see cref="CollectionViewSource.Filter"/> イベントをオブザーバブルシーケンスに変換する。
        /// </summary>
        /// <param name="collectionViewSource">対象の <see cref="CollectionViewSource"/>。</param>
        /// <returns>フィルターイベントを通知するオブザーバブル。</returns>
        public static IObservable<FilterEventArgs> FilterAsObservable(this CollectionViewSource collectionViewSource)
        {
            return Observable.FromEvent<FilterEventHandler, FilterEventArgs>(
                h => (sender, e) => h(e),
                h => collectionViewSource.Filter += h,
                h => collectionViewSource.Filter -= h);
        }
    }
}
