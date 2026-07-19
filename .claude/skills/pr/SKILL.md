---
name: pr
description: 現在の変更をPR化し、CodeRabbitのレビュー指摘がゼロになるまで自動で修正ループを回す。PRを出す・CodeRabbitレビュー対応・レビュー指摘の修正を頼まれたときに使う。
---

# PR レビュー自動化スキル（FlatFileList）

現在の変更を Pull Request にし、CodeRabbit のレビュー指摘が **ゼロになるまで** 自動で
「レビュー → 一括修正 → 再レビュー」のループを回す。

- リポジトリ: `s-iguchi09/FlatFileList` / ベースブランチ: `main`
- 実行モード: **完全自動**（指摘ゼロまで commit / push / 再レビューを繰り返す）
- **承認プロンプトを出さない**: 各ステップでユーザーに確認を求めず自律的に進める。
  PR URL や各ラウンドの結果は都度「報告」するが、続行のための承認待ちはしない。
  人に問い合わせるのは異常時のみ（gh 未認証・ビルド恒久失敗・最大ラウンド未収束など）。

## 環境自動切替（Webhook / ポーリング）

実行環境に応じて **GitHub の操作手段とレビュー待ち方式を自動で切り替える**。

- **モード A（Webhook）**: **GitHub MCP ツール（`mcp__github__*`）が利用可能な場合**
  （claude.ai Web 環境などで GitHub 連携済み）。`subscribe_pr_activity` で PR を購読し、
  レビュー/CI イベントを `<github-webhook-activity>` として **プッシュで受け取る**。`sleep`
  ポーリングは不要。**こちらを優先する。**
- **モード B（ポーリング）**: **GitHub MCP ツールが無い場合**（ローカル PC の Claude Code CLI など）。
  PR 操作は `gh` CLI で行う（PowerShell からでも可）。**レビュー待ちの監視ループは Git Bash
  （Bash ツール）で実行する** — ループは `mktemp` / `awk` / `trap` / `${var:+...}` 等の
  POSIX シェル機能に依存するため、PowerShell では動かない点に注意。

> 各手順は「A / B」で分岐して記述する。まずステップ 1 でどちらのモードかを判定すること。

---

## 絶対に守る不変条件（リポジトリ規約）

**正典はリポジトリ直下の [`AGENTS.md`](../../../AGENTS.md)。修正・レビュー対応の前に必ず読むこと。**
本スキルの修正（CodeRabbit 指摘対応・新規コード）はすべて AGENTS.md の規約に従う。要点のみ再掲:

- **XML ドキュメントコメント必須（日本語）** — 全クラス/メソッド/プロパティ/イベント（private 含む）
- **MVVM 遵守** — コードビハインド（`*.xaml.cs`）にロジックを書かない（ViewModel / ReactiveProperty へ）
- **ビルド検証は VS MSBuild**（`dotnet build` 不可。手順・vswhere スニペットは AGENTS.md 参照）
- **文言はすべて日本語**（コメント・コミット・PR・報告）

---

## 手順

### 1. 前提確認 ＆ モード判定

- **モード判定**: GitHub MCP ツール（`mcp__github__*`、特に `subscribe_pr_activity` /
  `create_pull_request`）が利用可能かを確認する。
  - 使える → **モード A（Webhook）**
  - 使えない → **モード B（ポーリング）**。この場合 `gh auth status` で認証を確認
    （未認証なら停止して案内）。
- リポジトリと現在ブランチ・変更状況を確認: `git status` / `git branch --show-current`。
- 以降、決定したモードで各ステップの「A / B」いずれかを実行する。

### 2. ブランチ準備

- **`main` 上にいる場合**: 変更内容から命名した feature ブランチを作成する
  （例: `feature/<topic>` / `fix/<topic>` / `docs/<topic>`）。

  ```powershell
  git switch -c feature/<topic>
  ```

- 未コミットの変更があれば、日本語メッセージで論理単位ごとに commit。
  - コミットメッセージ末尾に付与:
    `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- リモートへ push: `git push -u origin <branch>`
- **PR に出せる変更が無い場合**（`main` と差分ゼロ）はその旨を報告して停止。

### 3. PR 作成 または 既存 PR の再開

まず既存のオープンな `main` 宛て PR が無いか確認し、あれば再利用（重複作成防止）。
タイトル・本文は日本語で、変更概要 / 目的 / 主な変更点を記載する。

- **A（Webhook）**:
  - 既存確認: `mcp__github__list_pull_requests`（`head` にブランチ、`base` に `main`）。
    返却の `base.ref == "main"` を必ず確認する。
  - 無ければ作成: `mcp__github__create_pull_request`（`base: main`）。
  - **購読**: `subscribe_pr_activity` で PR を購読し、以降のイベントを
    `<github-webhook-activity>` として受け取れるようにする。
  - **購読直後に状態を再取得**して取りこぼしを補う: `mcp__github__pull_request_read`
    （`get_status` / `get_check_runs` / `get_review_comments`）。
- **B（ポーリング）**:
  - 既存確認: `gh pr list --head <branch> --base main --state open`
  - 無ければ作成:

    ```powershell
    gh pr create --base main --head <branch> --title "<日本語タイトル>" --body "<日本語本文>"
    ```

- PR 番号（`<n>`）と URL を保持し、**必ずユーザーに PR URL を提示**する。

### 4. CodeRabbit レビューを依頼

CodeRabbit App は導入済み。`.coderabbit.yaml` は `auto_incremental_review: false` のため
push だけでは再レビューされない。必ず PR に明示コメントで依頼する。

**依頼コマンドはラウンドで使い分ける**（収束性・使用量・ノイズ低減のため）:

- **初回ラウンド** → `@coderabbitai full review`（PR 全体を網羅レビュー）。
  ※ PR 作成直後の自動初回レビューが既に走っている場合はそれを利用し、明示依頼は省略してよい。
- **2 ラウンド目以降** → `@coderabbitai review`（**インクリメンタル**。直前の修正差分だけを検証）。
  解決済みの蒸し返しが少なく、指摘ゼロへ収束しやすい。使用量も少ない。
- 網羅性を再確認したい時（例: マージ直前）だけ、任意で `full review` を使う。

投稿方法:

- **A（Webhook）**: `mcp__github__add_issue_comment`（または同等の PR コメントツール）で上記コマンドを投稿。
- **B（ポーリング）**: レビュー依頼の **直前に基準時刻を記録**（`since=$(date -u +%Y-%m-%dT%H:%M:%SZ)`）
  してから投稿する。

  ```powershell
  # 初回ラウンド
  gh pr comment <n> --body "@coderabbitai full review"
  # 2 ラウンド目以降
  gh pr comment <n> --body "@coderabbitai review"
  ```

> 「1回で指摘を修正」= 明示依頼した1回のレビューで出た **全 actionable 指摘をまとめて1パスで修正** する運用
> （push 毎の自動再レビューは `.coderabbit.yaml` の `auto_incremental_review: false` で抑止済み）。

### 5. レビュー完了待ち ＆ 指摘収集（承認不要）

CodeRabbit のレビューは数分かかる。完了を待って自動で次へ進む。ユーザーへの承認要求や
「確認しますか?」の問い合わせはしない。「review in progress」等の**処理中表示がある間は次へ進まない**。

- **A（Webhook）**: `sleep` ポーリングはしない。レビュー/CI 結果は `<github-webhook-activity>`
  イベントとしてセッションに届くので **到着を待つ**。長時間反応が無い場合のみ
  `mcp__github__pull_request_read`（`get_reviews` / `get_review_comments` / `get_status`）で
  状況を確認して続行する（ユーザーには聞かない）。
- **B（ポーリング）**: **Git Bash（Bash ツール）でバックグラウンドの監視ループ**を回す
  （前景 `sleep` は使わず、`run_in_background`＝デタッチ実行）。到着判定は **構造化フィールドを主**に
  行う（`.user.type=="Bot"` かつ `login` が `coderabbitai` で始まり、`submitted_at` が `$since` より
  後の**レビュー提出**があるか）。bot 名や「Actionable comments posted:」本文への依存は補助に留める。

  レート消費を抑えるため **ETag 条件付きリクエスト**で「変化あり(200)/無し(304)」を判定し、
  **304 はレート非課金**。認証トークンは **argv に出さず** 権限を絞った curl 設定ファイル経由で渡す。
  JSON 解析は **`gh api --jq`（gh 内蔵 jq）** を使う（環境に外部 `jq` が無くても動く）。

  ```bash
  n="<PR番号>"; since="<ステップ4Bで記録した時刻>"
  repo="s-iguchi09/FlatFileList"
  umask 077; tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT   # 終了時に必ず後始末
  # 認証ヘッダは設定ファイル経由（プロセス一覧にトークンを出さない）
  printf 'header = "Authorization: Bearer %s"\nheader = "Accept: application/vnd.github+json"\n' \
    "$(gh auth token)" > "$tmp/curl.cfg"
  url="https://api.github.com/repos/$repo/pulls/$n/reviews"
  etag=""; deadline=$(( $(date +%s) + 900 ))              # 15分で打ち切り
  while [ "$(date +%s)" -lt "$deadline" ]; do
    code=$(curl -sS -K "$tmp/curl.cfg" ${etag:+-H "If-None-Match: $etag"} \
      -o "$tmp/rev.json" -D "$tmp/hdr.txt" -w '%{http_code}' "$url" || echo 000)
    if [ "$code" = "200" ]; then                          # 変化あり（304/000は非課金）
      etag=$(awk 'tolower($1)=="etag:"{print $2}' "$tmp/hdr.txt" | tr -d "\r")
      cnt=$(gh api "repos/$repo/pulls/$n/reviews" \
        --jq "[.[]|select(.user.type==\"Bot\" and (.user.login|startswith(\"coderabbitai\")) and .submitted_at>\"$since\")]|length" \
        2>/dev/null || echo 0)
      [ "${cnt:-0}" -gt 0 ] && { echo "review-ready:$cnt"; break; }
    fi
    sleep 60
  done
  ```

**指摘の収集**（モード共通。A は MCP、B は `gh api --jq`）:

- レビュー本文: `.../pulls/<n>/reviews`（A: `mcp__github__pull_request_read` `get_reviews`）
- インラインコメント: `.../pulls/<n>/comments`（A: 同 `get_review_comments`）

**完了判定 & 件数**: CodeRabbit サマリ本文の **`Actionable comments posted: N`** を読む。

- `N = 0` かつ新規 actionable インラインコメント無し → **クリーン（ループ終了）**
- `N > 0` → 各インラインコメント（ファイル・行・指摘内容・提案）を収集して次へ
- CI グリーンや walkthrough / 「Pre-merge checks passed」だけを根拠にクリーンと判断しない
  （中間シグナル。後から actionable な指摘が追加され得る）。

**タイムアウト / フォールバック**: 15 分待っても新レビューが付かない場合は、最新レビュー本文を
直接読んで状況を判断する。それでも判断できない時のみユーザーへ状況を報告する。

### 6. 一括修正（1ラウンド）

- 収集した **全 actionable 指摘を1パスでまとめて修正**。上記「不変条件」を厳守。
- **MSBuild でビルド検証**（AGENTS.md の vswhere 手順）。失敗したら直してから進む。
- 日本語メッセージで commit → `git push`。
- **A（Webhook）のみ**: 対応を終えたレビュースレッドは解決済みにする
  （`mcp__github__resolve_review_thread`）。見送り/反論の場合は理由を 1 回だけ返信してから解決。
- 短く報告: `ラウンドN: X件を修正 / ビルドOK / push済`。

### 7. ループ

- ステップ 4〜6 を **`Actionable comments posted: 0`** になるまで繰り返す。
- 2 ラウンド目以降の再レビューは **`@coderabbitai review`（インクリメンタル）** を使う
  （モードに応じてステップ 4：A: MCPコメント / B: `gh pr comment`＋`since`更新）。

### 8. 暴走防止

- **最大ラウンド数 = 5**。到達しても収束しない場合は、残指摘を要約して報告し停止
  （無限ループを避ける）。
- 同じ指摘が繰り返し出て収束しない場合も、状況を説明してユーザーに判断を仰ぐ。

### 9. 完了報告

- 使用したモード（A: Webhook / B: ポーリング）・PR URL / 実施ラウンド数 /
  最終状態（`Actionable comments posted: 0`）をまとめて報告。
- **A（Webhook）のみ**: 監視が不要になったら `unsubscribe_pr_activity` で購読を解除する。

---

## 補足

- 本リポジトリには `.coderabbit.yaml`（レビュー設定）がある。CodeRabbit のレビュー方針
  （日本語・徹底レビュー・XMLコメント/MVVMチェック）はこの設定で制御される。設定変更を
  `main` に反映するには PR マージが必要（初回はこの PR に含めて反映される）。
- **レート消費の最小化（モード B）**: 待機ループは ①条件付きリクエスト（ETag→304 はノーカウント）
  ②60 秒間隔 ③エンドポイント1本（reviews のみ）で、GitHub API 消費を最小化する。
  モード A（Webhook）はプッシュ受信なので待機中のポーリング消費は元々ゼロ。
