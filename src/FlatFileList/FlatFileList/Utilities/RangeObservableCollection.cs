using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlatFileList.Utilities
{
    /// <summary>
    /// 複数要素の一括追加を、変更通知1回(Reset)で行える <see cref="ObservableCollection{T}"/>。
    /// </summary>
    /// <typeparam name="T">要素の型。</typeparam>
    /// <remarks>
    /// 1件ずつ <see cref="Collection{T}.Add"/> すると要素ごとに CollectionChanged が発生し、
    /// CollectionView のフィルター評価や DataGrid の更新が要素数ぶん走る。まとめて追加することで通知を1回に抑える。
    /// </remarks>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// 指定した要素をまとめて末尾に追加し、Reset の変更通知を1回だけ発生させる。
        /// </summary>
        /// <param name="items">追加する要素の列。</param>
        public void AddRange(IEnumerable<T> items)
        {
            CheckReentrancy();

            //NOTE:遅延評価の列が途中で例外を送出すると、追加済みの要素だけが残り通知と不整合になる。
            //     先に列挙し終えてから追加する(配列などのコレクションはコピーせずそのまま使う)。
            var adding = items as ICollection<T> ?? items.ToList();
            if (adding.Count == 0)
            {
                return;
            }

            foreach (var item in adding)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
