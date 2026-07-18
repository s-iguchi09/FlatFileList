using FlatFileList.Extensions;
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
        /// この設定をカンマ区切りの設定文字列に変換する。
        /// </summary>
        /// <returns>「有効,拡張子」形式の文字列。</returns>
        public string ToSettingParameter()
        {
            return $"{Enabled.Value},{ExtensionText}";
        }

        /// <summary>
        /// カンマ区切りの設定文字列からインスタンスを復元する。
        /// </summary>
        /// <param name="setting"><see cref="ToSettingParameter"/> で生成した設定文字列。</param>
        /// <returns>復元した <see cref="ExcludeExtension"/>。</returns>
        public static ExcludeExtension CreateFromSetting(string setting)
        {
            var items = setting?.Split(',').AsEnumerable() ?? Enumerable.Empty<string>();
            return new(items.Skip(1).FirstOrDefault(string.Empty), items.FirstOrDefault(string.Empty).ToBoolean(false));
        }
    }
}
