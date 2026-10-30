namespace FlatFileList.Datas
{
    /// <summary>
    /// ユーザー設定。設定ファイル(JSON)の <see cref="SectionName"/> セクションに対応し、
    /// 起動時に構成から読み込まれ、変更時に <see cref="Interfaces.IUserSettingsStore"/> で保存される。
    /// </summary>
    public class UserSettings
    {
        /// <summary>設定ファイル内でユーザー設定を格納するセクション名。</summary>
        public const string SectionName = "UserSettings";

        /// <summary>隠しフォルダーを検索対象外にするかどうか。</summary>
        public bool IsExcludeHiddenDirectory { get; set; } = true;

        /// <summary>システムフォルダーを検索対象外にするかどうか。</summary>
        public bool IsExcludeSystemDirectory { get; set; } = true;

        /// <summary>ドットで始まるフォルダーを検索対象外にするかどうか。</summary>
        public bool IsExcludeDotStartDirectory { get; set; } = true;

        /// <summary>隠しファイルを検索対象外にするかどうか。</summary>
        public bool IsExcludeHiddenFile { get; set; } = true;

        /// <summary>システムファイルを検索対象外にするかどうか。</summary>
        public bool IsExcludeSystemFile { get; set; } = true;

        /// <summary>ドットで始まるファイルを検索対象外にするかどうか。</summary>
        public bool IsExcludeDotStartFile { get; set; } = true;

        /// <summary>前回保存日時の取得を行わないかどうか。</summary>
        public bool IsIgnoreGettingLastSaveTime { get; set; } = false;

        /// <summary>ウィンドウの表示倍率(%)。</summary>
        public int WindowScaleTransformPer { get; set; } = 100;

        /// <summary>拡張子ごとにファイルを開くアプリケーションの設定一覧。</summary>
        public List<CustomApplicationProcessSetting> CustomOpenProcesses { get; set; } = [];

        /// <summary>検索対象から除外する拡張子の設定一覧。</summary>
        public List<ExcludeExtensionSetting> ExcludeExtensions { get; set; } = [];
    }

    /// <summary>
    /// <see cref="CustomApplicationProcess"/> を設定ファイルへ保存するためのデータ。
    /// </summary>
    public class CustomApplicationProcessSetting
    {
        /// <summary>この設定を有効にするかどうか。</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>対象の拡張子。</summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>起動するアプリケーションのパス。</summary>
        public string Application { get; set; } = string.Empty;

        /// <summary>起動時に渡す引数。</summary>
        public string Args { get; set; } = string.Empty;
    }

    /// <summary>
    /// <see cref="ExcludeExtension"/> を設定ファイルへ保存するためのデータ。
    /// </summary>
    public class ExcludeExtensionSetting
    {
        /// <summary>この除外設定を有効にするかどうか。</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>除外する拡張子の文字列。</summary>
        public string ExtensionText { get; set; } = string.Empty;
    }
}
