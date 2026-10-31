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
        /// この設定を保存用のデータに変換する。
        /// </summary>
        /// <returns>保存用の <see cref="CustomApplicationProcessSetting"/>。</returns>
        public CustomApplicationProcessSetting ToSetting()
        {
            return new() { IsEnabled = IsEnabled.Value, Extension = Extension.Value, Application = Application.Value, Args = Args.Value };
        }

        /// <summary>
        /// 保存用のデータからインスタンスを復元する。
        /// </summary>
        /// <param name="setting"><see cref="ToSetting"/> で生成した保存用データ。</param>
        /// <returns>復元した <see cref="CustomApplicationProcess"/>。</returns>
        public static CustomApplicationProcess CreateFromSetting(CustomApplicationProcessSetting setting)
        {
            return new(setting.IsEnabled, setting.Extension ?? string.Empty, setting.Application ?? string.Empty, setting.Args ?? string.Empty);
        }
    }
}
