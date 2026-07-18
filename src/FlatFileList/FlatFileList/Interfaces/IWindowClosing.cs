namespace FlatFileList.Interfaces
{
    /// <summary>
    /// ウィンドウを閉じる際の処理を ViewModel 側で扱えるようにするためのインターフェイス。
    /// </summary>
    public interface IWindowClosing
    {
        /// <summary>
        /// ウィンドウが閉じられようとしたときに呼び出される。
        /// </summary>
        /// <returns>クローズをキャンセルする場合は <see langword="true"/>、そのまま閉じる場合は <see langword="false"/>。</returns>
        bool OnClosing();
    }
}
