using System.Collections.Concurrent;

namespace FlatFileList.Datas
{
    /// <summary>
    /// 検索結果の各ファイルの前回保存日時を、複数のワーカースレッドで並列に取得する。
    /// 画面に表示中の行など、優先して取得したい行を途中から割り込ませることができる。
    /// </summary>
    /// <remarks>
    /// ワーカーは「優先スタック → 一覧の先頭から順」の順に行を取り出す。
    /// 同じ行を二重に取得しないよう、取得の権利は <see cref="FileProperty.TryBeginLoadModifiedTime"/> で排他する。
    /// </remarks>
    public sealed class LastSaveTimeLoader
    {
        //NOTE:取得はファイル I/O が主体のため、CPU 数より多くしても効果が薄くディスクの競合が増える。上限を設ける。
        /// <summary>ワーカースレッド数の上限。</summary>
        private const int MaxWorkerCount = 4;

        /// <summary>進捗を通知する間隔(件数)。通知ごとに UI スレッドへのポストが発生するため間引く。</summary>
        private const int ProgressReportInterval = 100;

        /// <summary>取得対象の行(開始時点のスナップショット)。</summary>
        private readonly FileProperty[] _items;

        /// <summary>優先して取得する行。後から積んだもの(最新の表示範囲)から取り出す。</summary>
        private readonly ConcurrentStack<FileProperty> _priorityItems = new();

        /// <summary>通常順で次に取り出す位置の1つ前。</summary>
        private int _nextIndex = -1;

        /// <summary>取得を終えた件数。</summary>
        private int _processedCount;

        /// <summary>
        /// 取得対象の行を指定してインスタンスを生成する。
        /// </summary>
        /// <param name="items">取得対象の行。呼び出し時点の内容をスナップショットとして保持する。</param>
        public LastSaveTimeLoader(IEnumerable<FileProperty> items)
        {
            _items = items.ToArray();
        }

        /// <summary>取得対象の件数。</summary>
        public int Count => _items.Length;

        /// <summary>
        /// 指定した行を優先して取得するよう割り込ませる。先頭の行ほど先に取得される。取得済みの行は読み飛ばされる。
        /// </summary>
        /// <param name="items">優先する行(画面の上から順)。</param>
        public void Prioritize(IReadOnlyList<FileProperty> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            // スタックは後に積んだものから取り出されるため、逆順に積んで先頭の行を最初に取り出させる
            _priorityItems.PushRange(items.Reverse().ToArray());
        }

        /// <summary>
        /// すべての行の前回保存日時を取得し終えるか、キャンセルされるまでブロックする。
        /// </summary>
        /// <param name="isIgnoreGettingLastSaveTime">取得を行わない設定かどうかを返す関数。途中で設定が変わった場合も反映する。</param>
        /// <param name="reportProgress">取得済み件数を通知するコールバック。間引いて呼ばれる。</param>
        /// <param name="token">キャンセル トークン。</param>
        public void Run(Func<bool> isIgnoreGettingLastSaveTime, Action<int> reportProgress, CancellationToken token)
        {
            var workerCount = Math.Max(1, Math.Min(MaxWorkerCount, Environment.ProcessorCount));

            //NOTE:プロパティハンドラーにはアパートメントモデルのものがあるため、従来どおり STA スレッドで呼び出す。
            var workers = Enumerable.Range(0, workerCount).Select(_ =>
            {
                Thread t = new(() => Work(isIgnoreGettingLastSaveTime, reportProgress, token)) { IsBackground = true };
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                return t;
            }).ToList();

            foreach (var t in workers)
            {
                t.Join();
            }

            reportProgress(Volatile.Read(ref _processedCount));
        }

        /// <summary>
        /// 1つのワーカースレッドの処理。取り出せる行がなくなるか、キャンセルされるまで取得を続ける。
        /// </summary>
        /// <param name="isIgnoreGettingLastSaveTime">取得を行わない設定かどうかを返す関数。</param>
        /// <param name="reportProgress">取得済み件数を通知するコールバック。</param>
        /// <param name="token">キャンセル トークン。</param>
        private void Work(Func<bool> isIgnoreGettingLastSaveTime, Action<int> reportProgress, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_priorityItems.TryPop(out var item))
                {
                    var index = Interlocked.Increment(ref _nextIndex);
                    if (index >= _items.Length)
                    {
                        break;
                    }

                    item = _items[index];
                }

                if (!item.TryBeginLoadModifiedTime())
                {
                    continue;
                }

                item.UpdateModifiedTime(isIgnoreGettingLastSaveTime());

                var count = Interlocked.Increment(ref _processedCount);
                if (count % ProgressReportInterval == 0)
                {
                    reportProgress(count);
                }
            }
        }
    }
}
