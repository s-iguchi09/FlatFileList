using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatFileList
{
    /// <summary>
    /// DataContext を経由せずにデータをバインディングへ橋渡しするためのプロキシ。
    /// <see cref="Freezable"/> を継承することで、視覚ツリー外の要素からでも <see cref="Data"/> をバインディングできる。
    /// </summary>
    public class BindingProxy : Freezable
    {
        #region Overrides of Freezable

        /// <summary>
        /// <see cref="Freezable"/> の新しいインスタンスを生成する。
        /// </summary>
        /// <returns>新しい <see cref="BindingProxy"/> インスタンス。</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}
