using FlatFileList.Datas;
using FlatFileList.Extensions;
using Reactive.Bindings;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FlatFileList
{
    /// <summary>
    /// アプリケーション全体で共有する定数を提供する静的クラス。
    /// </summary>
    public static class ConstantObject
    {
        /// <summary>
        /// ディレクトリ列の最大数。XAMLのColumn定義数と一致させること。
        /// </summary>
        /// <remarks>値を変更する場合は、合わせてXAMLのDierctory列、対応するIsDirectory*Exists、IsDirectoriesExistsへの追加を行うこと。</remarks>
        public static int MaxDirectoryColumnCount => 20;

        /// <summary>更新日時(ファイルシステムの LastWriteTime)の表示フォーマット。</summary>
        public static string LastWriteTimeFormat => "yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>前回保存日時(Shell 拡張プロパティ)の表示フォーマット。</summary>
        public static string ModifiedTimeFormat => "yyyy/MM/dd HH:mm";
    }
}
