# 開発環境の使い方

対象は GraduationProject、Unity 6000.3.14f1、URP 17.3.0、Windows PC。ゲームの内容・面白さの判断は docs/GAME_SPEC.md と意思決定記録を基準にする。

開始・再開の入口はdocs/START_HERE.md。全体仕様はGAME_SPEC、開発順はROADMAP、現在担当・進捗・次の一手はPROJECT_STATUSを読む。以下の環境導入時のテスト説明にある長押しは旧実装の記録で、現行承認仕様は即時クリック。新仕様へ更新するときに関連テストも移行する。

## 普段の開始

1. Codexで既存の GraduationProject を開く。Unityを操作する作業は、この保存済みプロジェクトの作業ディレクトリで行う。別worktreeを使う場合は、そちらを明示的にUnityで開き、接続先も確認する。
2. Unity Hubから同じプロジェクトを6000.3.14f1で開く。既に開いていれば再起動不要。
3. プロジェクトのPowerShellで `./tools/UnityAgent.ps1 Start` を実行する。既存の同版サーバーは再利用する。
4. Unityで **Tools > Unity Agent > Connect Local MCP**。または **Window > MCP for Unity > Toggle MCP Window** の Connect。
5. Codexは `mcpforunity://instances` と `mcpforunity://project/info` を読み、プロジェクトの絶対パスを照合して対象を選択する。複数Editorがあるときは名前だけで推測しない。

MCPのアドレスは `http://127.0.0.1:8080/mcp`。サーバーはこのPC内のみ。サーバー起動に管理者権限・有料アカウントは不要。自動起動するWindowsサービスや定期実行タスクは作っていない。

## 固定した構成

| 用途 | 構成 |
|---|---|
| Editor / URP | 6000.3.14f1 / 17.3.0を維持 |
| Unity側MCP | CoplayDev unity-mcp v10.2.0、packages-lock.jsonのGitコミットで固定 |
| サーバー | mcpforunityserver==10.2.0、tools/unity-agent/uv.lockで間接依存も固定 |
| 入力 / テスト | 既存Input System 1.19.0 / Test Framework 1.6.0 |
| 視覚確認 | 最終Game ViewのPNG、MCP、Windows Computer Use |
| ソース管理 | 既存Git/LFS、Unity YAMLのテキスト差分 |

`./tools/UnityAgent.ps1 Setup` は別PCや環境再構築時のコマンド。uvがロックファイルどおりに `.venv` を作る。通常のStartはダウンロードしない。サーバー/Unityパッケージの片方だけを更新しない。更新は小さく行い、後述のテスト・接続・描画を確認する。

## Codexへの依頼例

設計: 「$horror-design-review を使って、プレイヤーに感じてほしい怖さから整理したい。現在の実装と未決定の仕様を分けて、面白くなる根拠と失敗しそうな条件を一緒に検討して。」

開発: 「$unity-verified-development を使って、承認済みの仕様を実装して。Console、関連テスト、実画面で確認し、実施した検証と未検証を分けて報告して。」

AGENTS.mdは重大・復元困難な変更を確認し、通常の可逆的な環境整備は委任済みとして進める。ゲームの目的・雰囲気・遊び方の未決定部分を環境整備の都合で決めない。2つのスキルはこのリポジトリの `.agents/skills/` にある。

2026-09-07のWORK-001により、承認済みゲーム仕様の通常の可逆的実装・検証・記録もCodexが主導する。GAME-015の範囲内の演出・数値は試作できる。未承認の重大設計を発明する委任ではない。

## 検証メニュー

Unityの **Tools > Unity Agent** に次の操作を追加している。

- **Run Input Regression Tests**: 独立したPlay Modeの5テストを実行し、`artifacts/tests/<UTC時刻>/playmode.xml` へ保存する。未保存のシーンがあれば実行を拒否する。
- **Run Serialization Checks**: 保存フィールド名の重複と既存Prefabの保存値を検証し、`editmode.xml` へ保存する。
- **Capture Game View**: Play Mode中のGame ViewをPNGと撮影条件JSONとして `artifacts/captures/` に保存する。実行要求とファイル保存完了は別なので、ファイルが生成されたことを確認して画像を開く。Game Viewを表示しておく。
- **Build Windows Development**: 現在有効なビルドシーンでWindows64のDevelopment Buildを作り、`artifacts/builds/<UTC時刻>/build.json` と実行ファイルを保存する。シーンは自動保存せず、未保存状態なら拒否する。プラットフォームも自動変更しない。

これらの補助コードはAssets/Editor内にあり、通常のPlayerへ入らない。PlayModeTestsはTestAssembliesとして通常ビルドから除外される。テストは既存Assembly-CSharpを維持するため、テスト用アダプター内だけで型・シリアライズフィールドを参照する。ゲーム側の型やフィールドを変更したら、テストも意図に合わせて更新する。ビルドでUnityが自動変更する既知の設定ファイルは、直前のバックアップから復元して差分を確認する。ProBuilderによる既定値の再保存など、残る差分はdocs/KNOWN_ISSUES.mdに記録している。テストが新規生成したPC情報ファイルは検証結果側へ退避する。

## MCPツールが表示されない場合

Codexを再接続するか、導入済みの公式MCP Python SDKを使ったCLIから同じサーバーを操作する。別のEditorブリッジは増やさない。

```powershell
./tools/UnityAgent.ps1 Status
./tools/UnityAgent.ps1 Client resource mcpforunity://instances
./tools/UnityAgent.ps1 Client --instance 'GraduationProject@40efb0f06b9c2d3f' resource mcpforunity://project/info
./tools/UnityAgent.ps1 Client tools --name manage_editor
./tools/UnityAgent.ps1 Client --group testing tools --name run_tests
```

Instance IDは例。毎回instancesの実値を使う。`call` は `--instance` を必須とし、実際のprojectRootがこのクライアントのあるリポジトリと一致するかを自動確認する。JSONは複雑なシェルエスケープを避けるため `--args-file` を使う。結果を `--output artifacts/...json` で保存できる。追加グループは `--group testing` などで指定する。MCPの成功応答でもテスト不合格・ビルド失敗という結果はあり得るので、中身を確認する。

## 異常時の手順

### Windows Computer Useの接続確認

Unityの編集はMCP優先。前面化・最終画面・Windows実入力が必要なときは、インストール済み `computer-use:computer-use` スキルの現在のSKILL.mdと参照手順を読み、Windows用プラグインを使う。以下は2026-09-08にこのPCで確認した接続経路で、モデル共通の手順。

1. ツール一覧／検索から `mcp__node_repl__js` を探す。functions経由なら `ALL_TOOLS` を名前で絞り、返された宣言を確認して `tools.mcp__node_repl__js(...)` を呼ぶ。最初から見えていないだけで不存在と判断しない。
2. **node_replのJavaScriptセッション**で次を初期化する（PowerShellやブラウザ用cua_replでは実行しない）。

   ```js
   if (!globalThis.sky) {
     const { sky } = await import("@oai/sky");
     globalThis.sky = sky;
   }
   ```

3. 別セルで `globalThis.windows = await sky.list_windows(); nodeRepl.write(JSON.stringify(windows));` を実行。返されたapp・id・titleから目的のUnity／Playerウィンドウを一意に選び、スキルどおり `sky.get_window` → 必要な前面化 → `sky.get_window_state` へ進む。IDは毎回取得し、過去の値を固定しない。前面操作は事前に知らせる。
4. `cua_repl` の `apps: []` や同ツールの `Native computer APIs are disabled` は、その経路の制限として扱う。別途提供されたWindowsプラグイン全体の利用不可や、Solの非対応とは推定しない。実際にWindows側で拒否された場合は迂回せず、その制限に従う。
5. Windows用ツールの探索結果、import、列挙、対象選択、画面取得のどこで失敗したかと具体エラーを記録する。復旧はスキルの範囲で行い、公開された権限要求が必要なら実際の承認機構を使う。シェルの昇格承認をComputer Use権限と混同せず、存在しない権限APIを要求しない。独自SendInput／PowerShell UI操作やhelper直起動で代替しない。

確認実績: task `01a07fbb-3ac2-7a62-a049-19d919724625` で、cua_replはapps空だった一方、上記Windows経路でUnityの列挙・前面化・画面取得に成功した。旧記録の「native app surfaceなし」はWindows経路未確認の判断であり、現在の不可判定に流用しない。過去に省略したPlayer実入力試験は、これで実施済みになるわけではない。利用可能なら必要な実画面／実入力検証を行い、起動ログや合成入力だけで代用しない。

### Unity MCP・ビルドの復旧

- **接続が切れた**: Statusでサーバーを確認し、UnityのConnectを実行する。起動直後・コンパイル・Play Mode切り替え中は待つ。同じプロジェクトに2つ目のEditorを起動しない。
- **Startがバージョン違いを報告**: 8080の既存プロセスを勝手に終了せず、所有者と起動元を調べる。
- **追加したスクリプトが認識されない**: `refresh_unity` は `scope=all, mode=force` を使って新規ファイルをインポートし、その後コンパイル完了を確認する。scriptsだけでは新規ファイルのインポートが済まないことがある。
- **要求したビルドが始まらない**: Editorを前面にして更新を進め、新しいbuild.jsonを確認する。開始確認前に同じ要求を重ねない。
- **MCPの状態がstale**: 最後のスナップショットの時刻も見る。Console・読み取り応答・GUIと照合する。staleだけからコンパイル完了を断定しない。
- **例外・クラッシュ**: 元のEditor.log/Player.log、Console、テストXMLを保存して照合する。ログ種別だけを根拠に成功/失敗を決めない。未保存シーンを保護してから再起動などを判断する。
- **元に戻す必要がある**: 変更差分とバックアップから対象だけを復元する。`git reset --hard` / `clean` は使わない。既存Sandbox・Utility・パッケージ変更はユーザーの作業として保存してある。

## 保存先・コスト・限界

`artifacts/` は証拠、ビルド、キャッシュ、復元用コピー用。Assets外・Git対象外で、Unityの不要なインポートを避ける。自動削除しないので蓄積量は定期的に確認する。`tools/unity-agent/.venv` もGit対象外。ゲームの画像・音声等の既存LFS設定は維持する。WORK-005に従い作業単位の自動commit・区切りの通常pushを行う。履歴書換え・公開範囲変更・リリースは別承認。詳細はAGENTS.mdのGit workflowを参照。

追加の有料サービスは導入していない。Codex利用枠、ローカルの容量と計算資源は使用する。MCPの任意テレメトリは無効化するが、Codexに渡したコード・画像・ツール応答はCodexによる処理の対象になる。Unity自体のアカウント・分析設定は変更していない。

テスト成功は面白さ・怖さ・完成度の保証ではない。機械的な操作確認と初見プレイヤーの評価は docs/VALIDATION.md のように分ける。音の聞こえ方、長時間の安定性、最低動作環境、発売判定は個別の検証が必要。
