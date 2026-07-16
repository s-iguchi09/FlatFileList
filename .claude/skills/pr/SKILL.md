---
name: pr
description: >-
  現在の変更でPull Requestを作成し、レビューのwebhook通知を待って指摘を修正し、
  再レビューを依頼する、というサイクルを指摘がなくなるまで繰り返して、最後にマージするワークフロー。
  「PRを出してレビュー対応からマージまでやって」「/pr」のように、PR作成〜レビュー対応〜マージを
  一連で任せたいときに使う。
---

# pr — PR作成からレビュー対応・マージまで

現在のブランチの変更を Pull Request にし、レビューの指摘対応を繰り返して、
指摘がなくなったらマージするまでを一連で行うワークフロー。

## 全体の流れ

1. PR を作成する
2. レビューの webhook 通知が来るまで待つ
3. 指摘を修正する
4. 再レビューを依頼する
5. 指摘がなくなるまで 2〜4 を繰り返す
6. 指摘がなくなったらマージする

## 前提

- GitHub 操作は `mcp__github__*` ツールを使う（`gh` CLI は使えない）。
- レビュー通知は `mcp__claude-code-remote__subscribe_pr_activity`（または
  `mcp__github__subscribe_pr_activity`）で購読し、`<github-webhook-activity>` として届く。
- リポジトリ・オーナー・PR番号は各ツール呼び出しで明示的に渡す。

---

## Step 1. PR を作成する

1. まず作業ブランチの状態を確認する。
   - `git status` / `git log` で未コミットの変更やプッシュ漏れがないか確認し、
     必要ならコミットして `git push -u origin <branch>` する。
2. `mcp__github__get_me` で現在のユーザー・権限を確認する。
3. リポジトリに PR テンプレートがあるか確認する
   （`.github/pull_request_template.md`、`.github/PULL_REQUEST_TEMPLATE/`、
   ルートや `docs/` の `PULL_REQUEST_TEMPLATE.md`）。あればその見出し構成に沿って本文を作る。
4. `mcp__github__create_pull_request` で PR を作成する。
   - タイトルと本文は変更内容から簡潔にまとめる。
   - base はデフォルトブランチ、head は作業ブランチ。
5. 作成した PR 番号・URL を控える。

> ユーザーが明示的に依頼していない限り、勝手に別ブランチへ push したり
> 想定外のリポジトリへ PR を出したりしない。

## Step 2. レビューの通知を待つ

1. 作成した PR を購読する:
   `mcp__claude-code-remote__subscribe_pr_activity(owner, repo, pullNumber)`。
   - すでに別エージェント（PR Steward）が監視している場合はイベントが届かない旨が返るので、
     その場合はユーザーに知らせて指示を仰ぐ。
2. 購読したら**ターンを終える**。レビューコメントや CI 結果は
   `<github-webhook-activity>` メッセージとして届き、このセッションが起こされる。
   - **`sleep` などで待たない。ポーリングもしない。** イベントで起きる。
3. webhook が来ない種類の状態変化（CI 成功、conflict 化など）に備え、
   `mcp__claude-code-remote__send_later` が使えるなら 1 時間程度先に self check-in を仕込んでおく。

## Step 3. 指摘を修正する

webhook イベントが届いたら:

1. `mcp__github__pull_request_read` でレビュー・レビューコメント・スレッドを取得し、
   未解決の指摘を洗い出す。CI 失敗イベントなら `mcp__github__get_job_logs` 等で原因を特定する。
2. 各指摘について:
   - 対応方針が明確で、会話のこれまでの方針に反せず、大規模なリファクタを要さないなら、修正する。
   - **解釈が分かれる指摘・アーキテクチャに関わる変更**は、`AskUserQuestion` でユーザーに確認してから動く。
   - 重複や対応不要と判断したものは、静かにスキップするか、必要なら理由を一言返す。
3. 修正をコミットして作業ブランチに push する。
   - 指摘元スレッドには、必要に応じて `mcp__github__add_reply_to_pull_request_comment` で
     対応内容を簡潔に返す。対応済みスレッドは `mcp__github__resolve_review_thread` で解決にする。
   - 毎ラウンド逐一報告はしない。差分が記録になる。タスク完了・質問が要るときだけ返信する。

## Step 4. 再レビューを依頼する

1. 指摘を修正・push したら、レビュアーに再レビューを依頼する。
   - 人間のレビュアーには `mcp__github__add_issue_comment` で「対応しました。再レビューをお願いします」と
     コメントし、可能なら該当レビュアーへ re-request する。
   - Copilot レビューを使っている場合は `mcp__github__request_copilot_review` を使う。
2. 依頼したら Step 2 と同様に**ターンを終えて次の通知を待つ**。

## Step 5. 繰り返し

- 新しい指摘イベントが届くたびに Step 3 → Step 4 を繰り返す。
- 「指摘がなくなった」= 未解決のレビュースレッドがなく、レビューが approve 済み、
  かつ CI がグリーンな状態。イベントは全状態をカバーしないので、
  節目では `mcp__github__pull_request_read` で未解決スレッドと mergeable 状態を実際に確認する。

## Step 6. マージする

指摘がなくなったら:

1. `mcp__github__pull_request_read` で最終確認する（未解決スレッドなし・approve 済み・CI グリーン・mergeable）。
2. `mcp__github__merge_pull_request` でマージする。
   - マージ方式はリポジトリの慣習に合わせる（不明なら squash を基本とし、迷えばユーザーに確認）。
3. マージできたら `mcp__claude-code-remote__unsubscribe_pr_activity` で購読を解除し、
   self check-in を仕込んでいれば止める。
4. マージ完了をユーザーに報告する。

> マージは巻き戻しにくい外向きの操作。CI が赤い・approve が揃っていない・conflict がある場合は
> 勝手にマージせず、状況を報告してユーザーの判断を仰ぐ。ユーザーが「approve 不要」等を
> 明示している場合はそれに従う。
