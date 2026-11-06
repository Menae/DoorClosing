# 開発環境の使い方

対象は GraduationProject、Unity 6000.3.14f1、URP 17.3.0、Windows PC。ゲームの内容・面白さの判断は docs/GAME_SPEC.md と意思決定記録を基準にする。

開始・再開の入口は [START_HERE.md](START_HERE.md)。本書はUnity接続・検証ツール・復旧が必要なときに該当節を読む。現在担当と進捗はPROJECT_STATUS、変更ごとの検証選択は [VALIDATION.md](VALIDATION.md#変更に応じた検証)。現行入力は即時クリックで、長押し試験は導入時の履歴。

## 普段の開始

1. Codexで既存の GraduationProject を開く。Unityを操作する作業は、この保存済みプロジェクトの作業ディレクトリで行う。別worktreeを使う場合は、そちらを明示的にUnityで開き、接続先も確認する。
2. Unity操作が必要でEditorが閉じている場合はUnity Hubから同じプロジェクトを6000.3.14f1で開く。既に開いていれば再起動不要。
3. MCPが未接続ならStatusで確認し、必要時にプロジェクトのPowerShellで `./tools/UnityAgent.ps1 Start` を実行する。既存の同版サーバーは再利用する。
4. 接続が必要ならUnityで **Tools > Unity Agent > Connect Local MCP**。または **Window > MCP for Unity > Toggle MCP Window** の Connect。
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

- **Run Input Regression Tests**: `GraduationProject.PlayModeTests`を実行し、`artifacts/tests/<UTC時刻>/playmode.xml` へ保存する。件数は現行コードと結果XMLで確認する。対象を絞る場合は既存の`RunInputTests(string[] testNames)`やMCPのtest filterを使う。未保存のシーンがあれば実行を拒否する。
- **Run Serialization Checks**: 保存フィールド名の重複と既存Prefabの保存値を検証し、`editmode.xml` へ保存する。
- **Capture Game View**: Play Mode中のGame ViewをPNGと撮影条件JSONとして `artifacts/captures/` に保存する。実行要求とファイル保存完了は別なので、ファイルが生成されたことを確認して画像を開く。Game Viewを表示しておく。
- **Build Windows Development**: 現在有効なビルドシーンでWindows64のDevelopment Buildを作り、`artifacts/builds/<UTC時刻>/build.json` と実行ファイルを保存する。シーンは自動保存せず、未保存状態なら拒否する。プラットフォームも自動変更しない。

これらの補助コードはAssets/Editor内にあり、通常のPlayerへ入らない。PlayModeTestsはTestAssembliesとして通常ビルドから除外される。テストは既存Assembly-CSharpを維持するため、テスト用アダプター内だけで型・シリアライズフィールドを参照する。ゲーム側の型やフィールドを変更したら、テストも意図に合わせて更新する。ビルドでUnityが自動変更する既知の設定ファイルは、直前のバックアップから復元して差分を確認する。ProBuilderによる既定値の再保存など、残る差分はdocs/KNOWN_ISSUES.mdに記録している。テストが新規生成したPC情報ファイルは検証結果側へ退避する。

## Mac試遊版の再作成（MAC-001）

Mac Build Support導入済みの6000.3.14f1を使用する。既存Editorで保存済みHomecomingを開き、`Tools > Unity Agent > Build Homecoming Mac Playtest`を実行する。別Editorを同じprojectへ起動しない。全4夜設定・各夜3参照・missing script・テスト用metadataを検査し、非Development／Mono／Metal／Intel＋Apple Siliconの本編を明示指定して作る。元のbuild scene listと描画・backend・architecture設定は保持する。

出力は`artifacts/builds/mac-<UTC時刻>`、結果はその中の`build.json`。`GRADUATION_MAC_OUTPUT`をEditor起動時に指定すれば出力先を変えられるが、既存Homecoming.appがある出力先は拒否する。`Succeeded`を確認後、`docs/MAC_PLAYTEST_README.txt`を出力先の`README.txt`へ、`docs/licenses/*.txt`を`licenses/`へコピーする。

`python tools/package-mac-playtest.py <出力先> <新しいZIPのパス>`で配布ZIPを作る。Python標準ライブラリのみ。appのCPU・製品assembly・ZIP CRC・実行権限を検査する。ZIPは既存ファイルへ上書きしない。app単体をWindowsで再圧縮すると実行権限を失う可能性があるため、生成したZIPをそのままMacへ渡す。

`Tools > Unity Agent > Verify Homecoming Mac Source`はEditorで全4夜＋死亡再試行／保存再開の既存Input System試験を実行し、`artifacts/tests/<UTC時刻>/playmode.xml`へ保存する。フォーカス喪失でゲームが自動ポーズするため、実行中はUnityを前面に保つ。これはMac Player検証の代用ではない。Mac実機ではREADMEの起動・入力・音・全夜・保存再開・性能を別途確認する。

Developer ID署名・公証・ストア公開はこの手順に含めない。Macの警告は[Appleの案内](https://support.apple.com/ja-jp/102445)に従い、OS全体の保護を無効化しない。対応構成は[Unity 6000.3のMacビルド説明](https://docs.unity3d.com/6000.3/Documentation/Manual/macos-building.html)を参照。

## Opus主導の連携（WORK-007）

役割は次のとおり。決定理由はDECISIONSのWORK-007（2026-11-06改訂＝ORCH-001）。
- **リーダー**（Claude Code / Opus）: ブリーフ作成・統合・記録・commit/push。
- **Sol**: 実装。
- **独立Opusレビュー役**: 審査。

1. **環境起動（リーダー）**: `./tools/UnityAgent.ps1 Launch`。
   - 固定版MCPサーバー（8080）を起動し、このprojectのEditorが開いていなければProjectVersion.txtの版で起動する。
   - MCPにGraduationProjectのinstanceが登録されるまで最大15分待つ。
   - 起動済みEditorはコマンドラインと `Temp/UnityLockfile` で検出し、2つ目を開かない。Editorのパスは `UNITY_EDITOR_PATH` で上書きできる。
2. **ブリーフ**: `artifacts/orchestra/briefs/<ID>.md` に次を書く。
   - 目的
   - 完了条件
   - 変更してよい範囲
   - 読むべき仕様のIDと見出し
   - 必要な検証
   - 現在地（PROJECT_STATUSから、必要な事実だけ）

   Solは契約によりPROJECT_STATUS／HISTORY／START_HEREを読まないため、ブリーフが現在地の正本になる。ブリーフは実行前の記録も兼ねる。PROJECT_STATUSは保存時と中断時に更新する（AGENTS.md）。
3. **実装依頼**: `python tools/orchestra/sol.py run --task <ID> --brief <ブリーフ> --scope '<glob>' --unity`。先頭に `sol-contract.md` の共通規約が付く。
   - **既定**:
     - gpt-6.1-sol・推論max。
     - workspace-write＋`--approve-for-me`（承認要求をCodexの自動審査へ回す）。
     - 1試行90分で停止（`--timeout-min`）。
     - `features.default_mode_request_user_input` を無効化する（無人実行で質問ツールを使わせない）。
     - 調査だけなら `--read-only`。
     - resumeは `--approve-for-me`／`--sandbox` を受け付けないため、同等の `-c sandbox_mode／approval_policy="on-request"／approvals_reviewer="guardian_subagent"` を明示する。
   - **推論強度**: 設計判断を含む実装はmax。撮影・テスト実行など証拠取得だけの依頼や、定型の修正は `--effort high`。
   - **事前検査**:
     - 新規の実行は、作業ツリーがclean（追跡・未追跡とも）でなければ拒否する。変更の出どころをSolに限るためで、作者の未コミット編集が混ざらない。
     - 同じタスクの差し戻しは `--resume`（dirtyを許可）か `--allow-dirty` で行う。
     - `--unity` は `Launch` の後に、project/info（build target）・editor state・アクティブシーン・Consoleのerror／warning件数を読む。Play中・未保存シーン・compile中なら拒否する。未保存シーンの保存・破棄はユーザーに確認する。
     - editor stateが `stale_status` だけの場合（Editorが背面で待機中で、状態の更新が古い）は拒否しない。シーンとConsoleの読み取りが応答していることで代え、状態の古さ（`state_age_ms`）をmetaに残す。このとき、古い状態からコンパイル完了を断定しない。
     - Unityを触る作業では `--unity` を必ず付ける。
   - **実行中**:
     - リーダーはバックグラウンドで実行し、終了通知を待つ。
     - `artifacts/orchestra/.sol.lock` がある間は、`.claude/settings.json` のPreToolUse hook（`tools/orchestra/guard_hook.py`）が、リーダーの次の操作を拒否する: リポジトリ内の編集（`artifacts/orchestra/briefs`／`reviews` を除く）、状態を変えるgit、Unity操作。
     - hookは、`.claude/settings.json` が開始時にあるセッションで有効になる。
     - ロックはUnityの事前検査より前に取る。そのため `Launch` で待つ間もhookが効き、検査の後にもう一度cleanを確かめる。
     - **hookの既知の穴**: シェル経由のファイル書き込み（リダイレクト、`Set-Content`、`sed -i` など）は止めないので、規則として行わない。gitも、パターンが想定しない書き方（変数に入れたコマンド、エイリアスなど）では止まらない。
     - **安全側の誤拒否**: ロック中は、`git commit` を含む文字列の検索や `UnityAgent.ps1` の閲覧のような読み取りも拒否されることがある。
     - **失敗時の扱い**:
       - スクリプト内の例外は、ロック中なら拒否、ロックが無ければ許可になる。
       - pythonやスクリプトが見つからない場合は、許可になる（コマンドの `|| true`）。終了コード2は全ツールの拒否と扱われるため、それを避けている。
       - パスは `\\?\` 付きの形と `/c/…` の形も、ドライブ表記に直して判定する。
   - **中止と停止理由**:
     - 中止は `python tools/orchestra/sol.py stop`。codexはプロセスツリーごと止め、sol.pyは本体だけを止めて、ロックを外す。TaskStopだけではcodexが残ることがある。
     - `--unity` の事前検査中に止めた場合、`Launch` が起動したUnity EditorとMCPサーバーは止めずに残す。
     - 止める相手は、ロックを書いたプロセスと照合する（コマンドラインと起動時刻）。PIDが再利用された別のプロセス（Codexアプリ、MCPサーバー等）は止めない。
     - プロセス表を読めないときは、ロックを残す（自動の除去も `stop` もしない）。
     - 止めた後も生きていればロックを残す。止めた実行は、次の方法で記録が残る: 試行は開始時と途中でmeta.jsonに書くため残り、session idもevents.jsonlから補う。
     - その実行のsol.pyもcodexも生きていないロックは、次の実行が自動で外す。
     - モデル容量不足（`at capacity` 等）は、同じsessionを間隔を伸ばして自動再開する（既定4回・180秒刻み）。
     - 利用上限（usage limit 等）とtimeoutは再試行せず、`meta.stop_reason` に残す。
4. **出力**: `artifacts/orchestra/<ID>/<UTC>/` に次を置く。
   - prompt.md、events.jsonl、stderr.log。
   - report.json（`report.schema.json` 形式）。
   - meta.json: codex版、session id、各試行のcommandとPID、所要秒、usage、前後のgit／Unity状態、`ignored_writes`、停止理由。
   - summary.json（機械検査）:
     - この実行で変わったファイル、`--scope` 外のファイル。
     - 報告との差。
     - Assets配下の.meta対応とGUIDの変化。移動で消えたGUIDも含む。
     - `git diff --check`（この実行のファイルだけ）と、新規テキストファイルの空白・競合記号。
     - 失敗したコマンドとMCP呼び出し。
     - Unityの残留状態（Play・未保存・error増加）。
     - 推奨レビュー段階。
5. **レビュー（段階化）**: summary.jsonの `suggested_review` に従う。
   - **full**: 次のいずれかに当てはまる場合。
     - Assets／ProjectSettings／Packages、C#、シーン、Prefabに触れる。
     - 契約で禁じたPROJECT_STATUS／DECISIONSを編集した。
     - `.claude/`・`tools/orchestra/`・AGENTS.mdに触れる。
     - 変更があるのに `--scope` が未指定。
     - 範囲外のファイルがある。
     - 報告と実際の変更が食い違う。
     - .meta／GUIDの問題、Unity flag、headの変化、stageのどれかがある。
     - Unityに触れたのに `--unity` を付けていない。
     - 報告が無い、または未完了。

     リーダーが `opus-reviewer` を起動し、ブリーフ・実行フォルダ・基準commitを渡す。
   - **light**: 文書だけ、証拠取得だけ、Assets外の小修正。リーダーがsummary.json・差分・証拠を確認して判断し、疑義があればfullに上げる。
   - **記録**: 審査結果は `<実行フォルダ>/review-<n>.md` に保存する（`sol.py status` が回数を数える）。
   - **差し戻し**: CHANGES_REQUIREDなら `sol.py run --task <ID> --brief <指摘.md> --resume <session_id>` で同じSolへ戻し、合格まで繰り返す。
   - **リーダーの直接実装**: Unity操作の要らない tools／docs／.claude の変更は、リーダーが直接実装してよい。tools／.claudeのコードはopus-reviewerが審査し、文書だけならリーダーが確認する。Assets・シーンの変更はSolに依頼する。
6. **保存**:
   - 合格後、リーダーがPROJECT_STATUSを3〜5行で更新し、対象を明示stageしてcommit、既存upstreamへpushする（WORK-005）。
   - 詳細はcommit本文とartifactsに残し、PROJECT_STATUSへ実行記録を書き写さない。
7. **再開時**: `python tools/orchestra/sol.py status` で次を確認する。
   - ロックの持ち主（実行中／app待ち／STALE）。
   - 最近の実行（所要秒、トークン、結果、推奨レビュー、レビュー回数）。

### 補足
- **パス**:
  - codex.exeとrg.exeは、Desktopアプリの更新で別々のハッシュフォルダに入る。sol.pyは `%LOCALAPPDATA%\OpenAI\Codex\bin\*\` の最新のcodex.exeを使う（`CODEX_CLI_PATH` で上書き可）。
  - PATHにrgが無ければ、同じ場所のrg.exeをSolのPATHへ足す。
  - ユーザーのCodex全体設定は変更しない。
- **無人運転**:
  - 長い自動実行の前に、リーダーはClaude desktopアプリの `request_keep_awake` でスリープを防ぐ。
  - app経路を使う日はCodexアプリを起動したままにする。
  - Unity Editorはフォーカスを失うと自動でポーズするため、画面操作中はほかのアプリの通知や前面化と競合しうる。
- **Computer Useの制約**:
  - Codexの画面取得・操作は、アプリ単位の許可をMCP elicitationで問い合わせる。保存済みの「常に許可」を返すのはDesktopアプリだけ。
  - そのため `sol.py run`（`codex exec`）で起動したSolは、window列挙はできても、撮影・操作は「Computer Use was not approved to use …」で拒否される（CU-001、SMOKE-001／002）。
  - 設定で問い合わせを自動承認させる等の迂回はしない。
  - Game Viewの撮影はCLIのSolからUnity MCPで行い、app経路はOSレベルの入力が必要なときだけに使う（ORCH-001の判断D）。
- **Claude側のComputer Use**（2026-11-06に確認）:
  - Claude DesktopのComputer useがオンなら、`mcp__computer-use__*` が使える。許可はセッションごとに `request_access` で求める。
  - スタートメニューの「Unity」はUnity 6000.3.18f1を指す。プロジェクトのEditorには、`C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe` をパスで指定して許可を求める（tierはfull）。
  - 前面にあるのが許可外のアプリ（Chrome等）だと、クリックは拒否される。Editorは、PowerShellの `(New-Object -ComObject WScript.Shell).AppActivate(<EditorのPID>)` で前面に出す。`open_application("Unity")` は別の版を起動するおそれがあるので使わない。
  - Windows Playerを操作するときは、そのexeにも別途許可を求める。
- **画面操作が必要な作業**: `python tools/orchestra/sol.py app --task <ID> --brief <ブリーフ.md> [--unity]`。IDは英数字と `-` のみで、予約コマンドは引用後に32,000字以内。
  - **予約**: ユーザーがDesktopアプリに作ったSol専用スレッド（gpt-6.1-sol・max、IDは `artifacts/orchestra/app-thread.json`、`--thread` で上書き可）へ `codex queue` で予約し、アプリが実行する。
  - **予約前の検査**（読み取り）: Codexの履歴DBの列、スレッドの存在、作業フォルダの一致、モデル（gpt-6.1-sol・max以外は拒否）、全権限（danger）でないこと、cleanな作業ツリー。
  - **予約直後**: meta.jsonに受付番号・スレッド設定・queue出力・スレッドのトークン数を書く。
  - **完了の判定**: `~/.codex/thread_history_1.sqlite` を読み取り専用で見て、受付番号を含む最初のユーザー発言のターンが completed／failed／interrupted になるまで待つ。
  - **出力**:
    - prompt.md。
    - meta.json（ターンの開始・終了・error、トークンの差）。
    - items.jsonl（ターンの全項目＝コマンド・ファイル変更・ツール呼び出し）。
    - report.json（スキーマの必須キーを検査）。
    - summary.json。
    - final.md（報告がスキーマに合わなかったときだけ）。
  - **未完了（exit 2）**: 次の場合は exit 2 で返す。
    - 5分たっても始まらない（`--start-min`。アプリが閉じている・スレッドが使用中）。
    - 20分間新しい項目がない（`--stall-min`。アプリ内の承認・質問・新しいアプリの許可待ちの可能性）。
    - 既定120分で終わらない。

    予約やターンはアプリ内に残るため、ロック（`app_pending`）を残して `run`／`app` を拒否する。exit 2の間もSol実行中として扱い、hookも有効のまま。
  - **待ち直し**: `python tools/orchestra/sol.py app-wait --run-dir <実行フォルダ>`。終われば記録してロックを外す。終了済みの実行は記録を上書きせずに拒否し、待機の経過は `meta.waits` に追記する。
  - **予約の扱い**:
    - 予約の一覧・取消の手段は、CLIでは未確認。
    - ロックを手で外してよいのは、次のどちらかのときに限る（どちらも読み取りで確認する）。
      - Codexの `thread_history_1.sqlite` に受付番号を含む発言が無く、`queue_1.sqlite` の `queued_items` にもそのスレッド宛ての行が無い。
      - 該当ターンが終了状態になっている。
  - **差し戻し**: `--resume` ではなく、同じスレッドへ指摘のブリーフを `app --allow-dirty` で送る（スレッドが文脈を持つ）。
  - **スレッドの運用**:
    - ユーザーが同じスレッドで会話すると指示が混ざるため、Sol専用にする。
    - スレッドの文脈は使うほど増えるため、マイルストーンごとにユーザーが新しいSolスレッド（gpt-6.1-sol・max、作業フォルダはこのリポジトリ）を作り、リーダーが `app-thread.json` のIDを差し替える。
- **レビュー役の登録**: `opus-reviewer` はセッション開始時に読み込まれる。定義を作成・変更したセッションでは名前で起動できないため、general-purpose（model: opus）に定義ファイルを読ませて同じ手順で審査する。
- **Sol実行中**: リーダーとレビュー役は同じcheckoutを編集せず、Unityも操作しない（読み取りのみ）。

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

3. 別セルで `globalThis.windows = (await sky.list_windows()).filter(w => /Unity\.exe|GraduationProject/i.test(w.app + " " + w.title)); nodeRepl.write(JSON.stringify(windows.map(w => ({ app: w.app, id: w.id, title: w.title }))));` を実行する。関係のないアプリのtitleを出力・保存しないよう、node_repl内で対象に絞ってから書き出す。返されたapp・id・titleから目的のUnity／Playerウィンドウを一意に選び、スキルどおり `sky.get_window` → 必要な前面化 → `sky.get_window_state` へ進む。IDは毎回取得し、過去の値を固定しない。前面操作は事前に知らせる。
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
