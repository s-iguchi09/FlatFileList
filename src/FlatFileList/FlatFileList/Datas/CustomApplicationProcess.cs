using FlatFileList.Extensions;
using Reactive.Bindings;

namespace FlatFileList.Datas
{
    /// <summary>
    /// 拡張子ごとにファイルを開くアプリケーションと引数を定義するユーザー設定項目。
    /// </summary>
    public class CustomApplicationProcess
    {
        public ReactivePropertySlim<string> Extension { get; } = new(string.Empty);
        public ReactivePropertySlim<string> Application { get; } = new(string.Empty);
        public ReactivePropertySlim<string> Args { get; } = new(string.Empty);
        public ReactivePropertySlim<bool> IsEnabled { get; } = new(true);

        /// <summary>
        /// 有効・各項目が空の状態で新しいインスタンスを生成する。
        /// </summary>
        public CustomApplicationProcess()
            : this(true, string.Empty, string.Empty, string.Empty)
        {

        }

        /// <summary>
        /// 指定した値で新しいインスタンスを生成する。
        /// </summary>
        /// <param name="isEnabled">この設定を有効にするかどうか。</param>
        /// <param name="extension">対象の拡張子。</param>
        /// <param name="application">起動するアプリケーションのパス。</param>
        /// <param name="args">起動時に渡す引数。</param>
        public CustomApplicationProcess(bool isEnabled, string extension, string application, string args)
        {
            Extension.Value = extension;
            Application.Value = application;
            Args.Value = args;
            IsEnabled.Value = isEnabled;
        }

        /// <summary>
        /// この設定をカンマ区切りの設定文字列に変換する。
        /// </summary>
        /// <returns>「有効,拡張子,アプリケーション,引数」形式の文字列。</returns>
        public string ToSettingParameter()
        {
            return $"{IsEnabled.Value},{Extension.Value},{Application.Value},{Args.Value}";
        }

        /// <summary>
        /// カンマ区切りの設定文字列からインスタンスを復元する。
        /// </summary>
        /// <param name="setting"><see cref="ToSettingParameter"/> で生成した設定文字列。</param>
        /// <returns>復元した <see cref="CustomApplicationProcess"/>。</returns>
        public static CustomApplicationProcess CreateFromSetting(string setting)
        {
            var items = setting?.Split(',').AsEnumerable() ?? Enumerable.Empty<string>();
            return new(items.FirstOrDefault(string.Empty).ToBoolean(false), items.Skip(1).FirstOrDefault(string.Empty), items.Skip(2).FirstOrDefault(string.Empty), items.Skip(3).FirstOrDefault(string.Empty));
        }
    }
}
