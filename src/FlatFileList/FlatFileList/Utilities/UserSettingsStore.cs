using FlatFileList.Datas;
using FlatFileList.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace FlatFileList.Utilities
{
    /// <summary>
    /// ユーザー設定を %LocalAppData%\FlatFileList\usersettings.json に JSON 形式で保存する。
    /// 書き込み途中の異常終了でファイルが壊れないよう、一時ファイルに書いてから置き換える。
    /// </summary>
    public class UserSettingsStore : IUserSettingsStore
    {
        /// <summary>ユーザー設定ファイルの絶対パス。</summary>
        public static string FilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlatFileList",
            "usersettings.json");

        /// <summary>JSON 出力時のシリアライズオプション(インデント付き・日本語等をエスケープしない)。</summary>
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// 起動時に設定ファイルを読み込めず既定値で起動した場合の理由。読み込めた場合は <see langword="null"/>。
        /// </summary>
        public string? LoadErrorMessage { get; private set; }

        /// <summary>
        /// 設定ファイルを上書きしてよいかどうか。読み込めない設定ファイルを .bak へ退避できなかった場合は
        /// 元のファイルを失わないよう <see langword="false"/> にし、保存を拒否する。
        /// </summary>
        private bool _canOverwriteFile = true;

        /// <summary>
        /// 設定ファイルを読み込み、ユーザー設定の構成とするキーと値の一覧を返す。
        /// JSON の null は型の既定値(bool なら false)として束縛されてしまうため除外し、設定モデルの既定値を使わせる。
        /// JSON が壊れている・<see cref="UserSettings.SectionName"/> セクションが無い・値を設定の型へ変換できない場合は、
        /// 既定値で起動できるようファイルを .bak へ退避して <see cref="LoadErrorMessage"/> に理由を記録する。
        /// </summary>
        /// <returns>ユーザー設定の構成とするキーと値の一覧。ファイルが無い・読み込めない場合は空。</returns>
        public IReadOnlyList<KeyValuePair<string, string?>> LoadFile()
        {
            if (!File.Exists(FilePath))
                return [];

            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(FilePath, optional: false, reloadOnChange: false)
                    .Build();

                if (!configuration.GetSection(UserSettings.SectionName).Exists())
                    throw new InvalidDataException($"{UserSettings.SectionName} section not found.");

                var values = configuration.AsEnumerable().Where(p => p.Value is not null).ToList();

                // 値を設定の型へ変換できるか(例: 数値の項目に文字列)を、構成とする前に検証する
                var section = new ConfigurationBuilder()
                    .AddInMemoryCollection(values)
                    .Build()
                    .GetSection(UserSettings.SectionName);
                var settings = section.Get<UserSettings>() ?? new();

                // 一覧は変換できない要素や配列でない値(文字列など)をバインダーが例外を出さずに無視するため、
                // 配列でない値と要素数の不一致で検出する
                // (ErrorOnUnknownConfiguration は不要になった古いキーまで失敗扱いにするため使わない)
                if (!IsListBound(section, nameof(UserSettings.CustomOpenProcesses), settings.CustomOpenProcesses.Count)
                    || !IsListBound(section, nameof(UserSettings.ExcludeExtensions), settings.ExcludeExtensions.Count))
                    throw new InvalidDataException("Some list settings could not be converted.");

                return values;
            }
            catch (Exception ex) when (ex is InvalidDataException or FormatException or InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                var backupPath = FilePath + ".bak";
                try
                {
                    File.Move(FilePath, backupPath, overwrite: true);
                    LoadErrorMessage = $"Failed to load User Profile. Started with default settings. (Backup: {backupPath})";
                }
                catch (Exception moveEx) when (moveEx is IOException or UnauthorizedAccessException)
                {
                    _canOverwriteFile = false;
                    LoadErrorMessage = "Failed to load User Profile. Started with default settings. (Settings will not be saved.)";
                }
                return [];
            }
        }

        /// <summary>
        /// 一覧の設定が配列として、すべての要素を欠落なく束縛できたかを判定する。
        /// </summary>
        /// <param name="section">ユーザー設定のセクション。</param>
        /// <param name="key">一覧の設定のキー。</param>
        /// <param name="boundCount">束縛できた要素数。</param>
        /// <returns>配列でない値(文字列など)が無く、構成の要素数と束縛できた要素数が一致する場合は <see langword="true"/>。</returns>
        private static bool IsListBound(IConfigurationSection section, string key, int boundCount)
        {
            var list = section.GetSection(key);
            // 空配列([])は空文字列の値として読み込まれるため、空文字列は配列でない値として扱わない
            return string.IsNullOrEmpty(list.Value) && list.GetChildren().Count() == boundCount;
        }

        /// <summary>
        /// ユーザー設定を <see cref="UserSettings.SectionName"/> セクション配下に書き出して保存する。
        /// </summary>
        /// <param name="settings">保存するユーザー設定。</param>
        /// <exception cref="IOException">読み込めなかった設定ファイルを退避できておらず、上書きすると元の内容が失われる場合。</exception>
        public void Save(UserSettings settings)
        {
            if (!_canOverwriteFile)
                throw new IOException("The unreadable settings file could not be backed up, so it will not be overwritten.");

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            // 複数インスタンスが同時に保存しても互いの一時ファイルを上書きしないよう、保存ごとに一意な名前にする
            var tempPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
            var root = new Dictionary<string, UserSettings> { [UserSettings.SectionName] = settings };
            try
            {
                File.WriteAllText(tempPath, JsonSerializer.Serialize(root, _jsonOptions));
                File.Move(tempPath, FilePath, overwrite: true);
            }
            finally
            {
                // 置き換えに失敗した場合に一時ファイルを残さない(置き換え済みなら既に存在しない)
                File.Delete(tempPath);
            }
        }
    }
}
