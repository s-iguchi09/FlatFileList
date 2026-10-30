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
        /// 設定ファイルが構成として読み込めるか(JSON として正しく、各値が設定の型へ変換できるか)を検証する。
        /// 読み込めない場合は既定値で起動できるよう、ファイルを .bak へ退避して <see cref="LoadErrorMessage"/> に理由を記録する。
        /// </summary>
        /// <returns>設定ファイルを構成へ追加してよい場合は <see langword="true"/>。ファイルが無い・読み込めない場合は <see langword="false"/>。</returns>
        public bool ValidateFile()
        {
            if (!File.Exists(FilePath))
                return false;

            try
            {
                new ConfigurationBuilder()
                    .AddJsonFile(FilePath, optional: false, reloadOnChange: false)
                    .Build()
                    .GetSection(UserSettings.SectionName)
                    .Get<UserSettings>();
                return true;
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
                    LoadErrorMessage = "Failed to load User Profile. Started with default settings.";
                }
                return false;
            }
        }

        /// <summary>
        /// ユーザー設定を <see cref="UserSettings.SectionName"/> セクション配下に書き出して保存する。
        /// </summary>
        /// <param name="settings">保存するユーザー設定。</param>
        public void Save(UserSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var tempPath = FilePath + ".tmp";
            var root = new Dictionary<string, UserSettings> { [UserSettings.SectionName] = settings };
            File.WriteAllText(tempPath, JsonSerializer.Serialize(root, _jsonOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }
    }
}
