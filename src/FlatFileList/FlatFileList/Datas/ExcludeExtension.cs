using Reactive.Bindings;

namespace FlatFileList.Datas
{
    /// <summary>
    /// 検索対象から除外する拡張子1件を表すデータ。有効・無効の切り替えと、一覧からの削除コマンドを持つ。
    /// </summary>
    /// <param name="extensionText">除外する拡張子の文字列。</param>
    /// <param name="enabled">この除外設定を有効にするかどうか。</param>
    public class ExcludeExtension(string extensionText,bool enabled = true)
    {
        public ReactivePropertySlim<bool> Enabled { get; } = new(enabled);
        public string ExtensionText { get; } = extensionText;
        public ReactiveCommand RemoveCommand { get; } = new();

        /// <summary>
        /// 拡張子が空・有効の状態で新しいインスタンスを生成する。
        /// </summary>
        public ExcludeExtension()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// この設定を保存用のデータに変換する。
        /// </summary>
        /// <returns>保存用の <see cref="ExcludeExtensionSetting"/>。</returns>
        public ExcludeExtensionSetting ToSetting()
        {
            return new() { Enabled = Enabled.Value, ExtensionText = ExtensionText };
        }

        /// <summary>
        /// 保存用のデータからインスタンスを復元する。
        /// </summary>
        /// <param name="setting"><see cref="ToSetting"/> で生成した保存用データ。</param>
        /// <returns>復元した <see cref="ExcludeExtension"/>。</returns>
        public static ExcludeExtension CreateFromSetting(ExcludeExtensionSetting setting)
        {
            return new(setting.ExtensionText ?? string.Empty, setting.Enabled);
        }
    }
}
