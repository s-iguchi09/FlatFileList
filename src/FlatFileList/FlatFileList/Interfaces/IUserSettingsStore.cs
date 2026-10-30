using FlatFileList.Datas;

namespace FlatFileList.Interfaces
{
    /// <summary>
    /// ユーザー設定を永続化するためのインターフェイス。
    /// 読み込みは構成(<see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>)経由で行い、保存のみをここで担う。
    /// </summary>
    public interface IUserSettingsStore
    {
        /// <summary>
        /// 起動時に設定ファイルを読み込めず既定値で起動した場合の理由。読み込めた場合は <see langword="null"/>。
        /// </summary>
        string? LoadErrorMessage { get; }

        /// <summary>
        /// ユーザー設定を保存する。
        /// </summary>
        /// <param name="settings">保存するユーザー設定。</param>
        void Save(UserSettings settings);
    }
}
