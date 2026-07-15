using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatFileList.Behaviors
{
    public class TextBoxBehavior : Behavior<TextBox>
    {
        /// <summary>
        /// クリア
        /// </summary>
        public void Clear()
        {
            this.AssociatedObject.Clear();
        }

        /// <summary>
        /// フォーカス設定
        /// </summary>
        public void Focus()
        {
            this.AssociatedObject.Focus();
        }

        /// <summary>
        /// 次のElementにフォーカスを移動
        /// </summary>
        public void MoveFocusToNext()
        {
            AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        /// <summary>
        /// 全選択
        /// </summary>
        public void SelectAll()
        {
            this.AssociatedObject.SelectAll();
        }
    }
}
