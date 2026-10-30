using FlatFileList.Datas;
using FlatFileList.Interfaces;
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
