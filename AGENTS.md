# FlatFileList — リポジトリ規約（開発ガイド）

WPF アプリ。AI エージェント（Claude / その他モデル）が開発・修正するときは以下の規約を
**常に守ること**。PR ワークフロー（`.claude/skills/pr`）でも同じ規約を適用する。

## プロジェクト概要
- **UI/FW**: WPF / .NET 10（`net10.0-windows`）/ C# 13 / Nullable enable
- **アーキテクチャ**: MVVM（**ReactiveProperty** ベース。CommunityToolkit.Mvvm / Prism ではない）
- **主要ライブラリ**: MaterialDesignThemes, ReactiveProperty.WPF, Microsoft.Xaml.Behaviors, COM: Shell32
- **プロジェクト**: `src\FlatFileList\FlatFileList\FlatFileList.csproj`（**`.sln` は無い**）
- 例: `MainWindow.xaml(.cs)` + `MainWindowViewModel.cs`。Model 層は `Datas/`、その他
  `Behaviors/` `Converters/` `Extensions/` `Helpers/` `Utilities/` に整理。

## 絶対に守る不変条件

1. **XML ドキュメントコメント必須（日本語）**
   すべての **クラス・メソッド・プロパティ・イベント** に `/// <summary>` を付ける。
   private メンバーも対象。既存コードに合わせ **日本語** で書く。欠落を見つけたら補う。

2. **MVVM 遵守 / コードビハインドにロジックを書かない**
   `*.xaml.cs`（コードビハインド）に **ビジネスロジック・UI ロジックを書かない**。
   ロジックは ViewModel（ReactiveProperty）へ置く。イベントハンドラ内に処理を書かず、
   `ReactiveCommand` / データバインディング / `Behaviors/` の Behavior で表現する。

3. **ビルドは VS MSBuild を使う（`dotnet build` は使わない）**
   `dotnet build` は COMReference（Shell32）で **失敗する**。必ず Visual Studio の MSBuild で
   ビルド・検証する。
   ```powershell
   $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
   $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
   & $msbuild "src\FlatFileList\FlatFileList\FlatFileList.csproj" -t:Build -p:Configuration=Debug -v:minimal
   ```

4. **文言はすべて日本語**
   コード内コメント・コミットメッセージ・PR タイトル/本文・ユーザーへの報告は日本語で書く。

## その他
- コードは周囲の既存コードのスタイル（命名・`#region` の日本語ラベル・コメント密度）に合わせる。
- CodeRabbit のレビュー方針は `.coderabbit.yaml`（日本語・徹底レビュー・上記1/2をチェック）で定義。
- PR 作成〜レビュー対応〜指摘ゼロまでの自動ループは `/pr` スキル（`.claude/skills/pr/SKILL.md`）を使う。
