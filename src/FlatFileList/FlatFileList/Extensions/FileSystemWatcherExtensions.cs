using System.IO;
using System.Reactive.Linq;

namespace FlatFileList.Extensions
{

    /// <summary>
    /// <see cref="FileSystemWatcher"/> の各種イベントを Rx のオブザーバブルとして扱うための拡張メソッドを提供する。
    /// </summary>
    public static class FileSystemWatcherExtensions
    {
        /// <summary>
        /// <see cref="FileSystemWatcher.Created"/> イベントをオブザーバブルシーケンスに変換する。
        /// </summary>
        /// <param name="watcher">対象の <see cref="FileSystemWatcher"/>。</param>
        /// <returns>ファイル作成イベントを通知するオブザーバブル。</returns>
        public static IObservable<FileSystemEventArgs> CreatedAsObservable(this FileSystemWatcher watcher)
        {
            return Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (sender, e) => h(e),
                h => watcher.Created += h,
                h => watcher.Created -= h);
        }

        /// <summary>
        /// <see cref="FileSystemWatcher.Deleted"/> イベントをオブザーバブルシーケンスに変換する。
        /// </summary>
        /// <param name="watcher">対象の <see cref="FileSystemWatcher"/>。</param>
        /// <returns>ファイル削除イベントを通知するオブザーバブル。</returns>
        public static IObservable<FileSystemEventArgs> DeletedAsObservable(this FileSystemWatcher watcher)
        {
            return Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (sender, e) => h(e),
                h => watcher.Deleted += h,
                h => watcher.Deleted -= h);
        }

        /// <summary>
        /// <see cref="FileSystemWatcher.Renamed"/> イベントをオブザーバブルシーケンスに変換する。
        /// </summary>
        /// <param name="watcher">対象の <see cref="FileSystemWatcher"/>。</param>
        /// <returns>ファイル名変更イベントを通知するオブザーバブル。</returns>
        public static IObservable<RenamedEventArgs> RenamedAsObservable(this FileSystemWatcher watcher)
        {
            return Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
                h => (sender, e) => h(e),
                h => watcher.Renamed += h,
                h => watcher.Renamed -= h);
        }

        /// <summary>
        /// <see cref="FileSystemWatcher.Changed"/> イベントをオブザーバブルシーケンスに変換する。
        /// </summary>
        /// <param name="watcher">対象の <see cref="FileSystemWatcher"/>。</param>
        /// <returns>ファイル変更イベントを通知するオブザーバブル。</returns>
        public static IObservable<FileSystemEventArgs> ChangedAsObservable(this FileSystemWatcher watcher)
        {
            return Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (sender, e) => h(e),
                h => watcher.Changed += h,
                h => watcher.Changed -= h);
        }
    }
}
