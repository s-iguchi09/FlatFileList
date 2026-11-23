using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;

namespace FlatFileList.Datas
{
    /// <summary>
    /// ファイル一覧の各行が持つ、ルートからの相対パス上のディレクトリ1階層分を表すデータ。
    /// 列に表示するフォルダー名と、エクスプローラーで開く対象パスを保持する。
    /// </summary>
    /// <remarks>値は生成後に変化しないため、同じフォルダーを指すインスタンスは <see cref="DirectoryOpennerCache"/> で全行が共有する。</remarks>
    public class DirectoryOpenner : INotifyPropertyChanged
    {
#pragma warning disable CS0067
        /// <summary>プロパティ値の変更を通知するイベント。</summary>
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        /// <summary>列のパディング用の、値を持たない共有インスタンス。</summary>
        private static readonly DirectoryOpenner _empty = new();

        /// <summary>列に表示するフォルダー名。</summary>
        public string Name { get; } = string.Empty;
        /// <summary>検索ルートからの相対パス(先頭に「\」を含む)。</summary>
        public string RelativePath { get; } = string.Empty;
        /// <summary>フォルダーの絶対パス。</summary>
        public string AbsolutePath { get; } = string.Empty;
        /// <summary>エクスプローラーで開く対象のパス(フォルダー、または選択状態にするファイル)。</summary>
        public string TargetPath { get; } = string.Empty;

        /// <summary>有効な(フォルダー名を持つ)エントリかどうか。</summary>
        public bool IsEnabled { get; } = false;

        /// <summary>
        /// 値を持たない空のインスタンスを生成する。
        /// </summary>
        private DirectoryOpenner()
        {
        }

        /// <summary>
        /// ルートからの相対フォルダーパスとフォルダー名からインスタンスを生成する。
        /// </summary>
        /// <param name="rootPath">検索ルートのパス。</param>
        /// <param name="relativePath">ルートから対象フォルダーまでの相対パス(先頭の「\」なし)。</param>
        /// <param name="name">対象フォルダーの名前。</param>
        public DirectoryOpenner(string rootPath, string relativePath, string name)
        {
            Name = name;
            IsEnabled = !string.IsNullOrEmpty(Name);

            RelativePath = $"\\{relativePath}";
            TargetPath = Path.GetFullPath(string.Join("\\", rootPath, relativePath));
            AbsolutePath = TargetPath;
        }

        /// <summary>
        /// ファイルパスから、その親ディレクトリを表すインスタンスを生成する。開く際はファイルを選択状態にする。
        /// </summary>
        /// <param name="rootPath">検索ルートのパス。</param>
        /// <param name="filePath">対象ファイルの絶対パス。</param>
        public DirectoryOpenner(string rootPath, string filePath)
        {
            Name = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;
            IsEnabled = !string.IsNullOrEmpty(Name);

            var relativePath = Path.GetDirectoryName(filePath)?.Replace(rootPath, "") ?? string.Empty;
            RelativePath = string.IsNullOrEmpty(relativePath) ? "" : $"\\{relativePath}";
            TargetPath = filePath;
            AbsolutePath = Path.GetDirectoryName(filePath) ?? string.Empty;
        }

        /// <summary>
        /// 列のパディング用の、値を持たない空のインスタンスを返す。全行で同じインスタンスを共有する。
        /// </summary>
        /// <returns>空の <see cref="DirectoryOpenner"/>。</returns>
        public static DirectoryOpenner CreateEmptyOpenner() => _empty;
    }

    /// <summary>
    /// 検索1回分の <see cref="DirectoryOpenner"/> を相対フォルダーパス単位で共有するキャッシュ。
    /// </summary>
    /// <remarks>
    /// 同じフォルダー配下のファイルは中間階層の <see cref="DirectoryOpenner"/> がすべて同じになるため、
    /// ファイルごとに生成すると生成コスト(Path.GetFullPath 等)とメモリがファイル数×階層数ぶんかかる。並列構築に対応する。
    /// </remarks>
    public class DirectoryOpennerCache
    {
        /// <summary>相対フォルダーパスをキーにした共有インスタンス。</summary>
        private readonly ConcurrentDictionary<string, DirectoryOpenner> _openners = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 検索ルートを指定してキャッシュを生成する。
        /// </summary>
        /// <param name="rootPath">検索ルートのパス。</param>
        public DirectoryOpennerCache(string rootPath)
        {
            RootPath = rootPath;
        }

        /// <summary>検索ルートのパス。</summary>
        public string RootPath { get; }

        /// <summary>
        /// 相対フォルダーパスに対応する <see cref="DirectoryOpenner"/> を取得する。未生成の場合は生成して登録する。
        /// </summary>
        /// <param name="relativePath">ルートから対象フォルダーまでの相対パス(先頭の「\」なし)。</param>
        /// <param name="name">対象フォルダーの名前。</param>
        /// <returns>共有の <see cref="DirectoryOpenner"/>。</returns>
        public DirectoryOpenner GetOrAdd(string relativePath, string name)
        {
            if (_openners.TryGetValue(relativePath, out var openner))
            {
                return openner;
            }

            return _openners.GetOrAdd(relativePath, new DirectoryOpenner(RootPath, relativePath, name));
        }
    }
}
