# Unity / Codex 開発環境の導入結果

作業日: 2026-09-06〜07（日本時間）。対象: `C:\Users\menae\Desktop\Unity_Projects\GraduationProject`。

状態: **開発環境の初期導入と技術検証を完了**。Windows Playerの描画とマウスによる視点変化まで確認した。連続移動、OS入力での長押し完遂、音、性能、面白さは別途検証が必要。

設計相談、Unity操作、操作回帰テスト、画面撮影、Windows検証ビルドを実行できる環境を導入した。通常の可逆的な整備は委任済みとして進め、ゲームの体験設計・照明・レベル構成は新しく決めていない。大きな影響や復元困難な変更を事前確認する方針をAGENTS.mdに記録した。

## 導入したもの

| 機能 | 内容 |
|---|---|
| Unity操作 | 既存CoplayDev MCPを10.2.0へ更新。Unityパッケージとサーバーを同版に固定 |
| 再現できる起動 | `tools/UnityAgent.ps1` のSetup / Start / Status / Client、Python環境とuv.lock |
| 誤操作防止 | SDKの変更・操作コマンドはInstance指定を必須化し、実際のprojectRootを照合 |
| 設計相談 | `.agents/skills/horror-design-review`。意図、面白くなる根拠、失敗条件、人による試作評価を検討 |
| 検証しながら開発 | `.agents/skills/unity-verified-development`。Console、入力経路、最終画面、Playerの証拠を区別 |
| 仕様・意思決定 | AGENTS.md、GAME_SPEC、DECISIONS、OPEN_QUESTIONS、DEVELOPMENT、VALIDATION |
| 操作テスト | 独立したPlay Modeテスト5件。Input System → Raycast → 長押し → 状態遷移を実行 |
| 保存データの検証 | Edit Modeテスト2件。保存名の重複とPrefabの値を検証 |
| Editorメニュー | ローカル接続、テスト、PNG撮影、Windows Development Build |
| Git | シーン・マテリアル・Prefab等のテキスト差分。既存の画像・音声等のLFSは維持 |
| 証拠保存 | Assets外のartifactsへ保存。テストの生成メタデータを退避し、ビルド中の設定自動変更を保護 |

Unity 6000.3.14f1 / URP 17.3.0を維持。追加の有料契約、クラウドサービス、Windows常駐サービス、OS全体の権限変更、公開、Git履歴の書き換えは行っていない。

## 最終状態での実測

| 検証 | 結果・証拠 |
|---|---|
| Unityコンパイル | 成功。追加メニューと新パッケージが認識されたことを確認 |
| Edit Mode | **2/2成功、失敗0、スキップ0**。`artifacts/tests/20260906-144451-453/editmode.xml` |
| Play Mode | **5/5成功、失敗0、スキップ0**。`artifacts/tests/20260906-144609-342/playmode.xml` |
| Console | 最終テスト後、およびMainの画面確認中、エラー・警告0件 |
| Windows64ビルド | **成功、エラー0・警告0、25.35秒**。`artifacts/builds/20260906-144824-543/build.json` |
| 生成Player | `artifacts/builds/20260906-144824-543/GraduationProject.exe`。約195 MB（BuildReportの総サイズ） |
| Game View | PNG保存・画像確認済み。エレベーター内の壁面、表示、ボタン等を確認。`artifacts/captures/gameview-mcp-10.2.png` |
| 実入力 | Editorでマウス入力による視点変化を確認。短いW入力は座標変化を確認できず、連続移動の成功とは扱わない |
| Windows Player実行 | 実起動し、1920×1080のウィンドウ画像で壁・床・緑の操作盤を確認。マウスのクリック・ドラッグで上下左右の視点変化を確認。終了後のShutdownログとプロセス終了も確認 |
| Playerのキー入力 | 短いSキーを送ったが、明確な移動は確認できなかった。連続移動・実機長押しの検証とは扱わない |
| Playerログ | ゲーム開始・Diagnosis到達を確認。既存の`Standin_Normal(Clone)`で`plateText`未設定の警告、D3D12のinfo queue取得失敗という診断行あり。検査したログにExceptionは見つからなかった |
| SDK | スキーマ取得、localhost接続、UTF-8出力、別projectRootの拒否を確認 |
| スキル | 両方ともskill-creatorの形式検証に成功 |
| テレメトリ | MCPの任意テレメトリ無効を確認。Codexに渡すコード・画像等の処理は別 |
| 既存Assets保全 | **285ファイル中283ファイルが開始時とSHA-256一致**。残る2ファイルは下記ビルド阻害要因の最小修正。Main/Sandbox・Utilityの元の内容は一致 |

PlayerのManagedフォルダにEditor用MCPアセンブリ・今回のテストアセンブリ・TestRunnerは含まれない。既存MCPパッケージの共通ユーティリティ `MCPForUnity.Runtime.dll` は含まれる。ソース確認では自動起動するHTTP/WebSocketサーバーではなく、互換処理・撮影等の共通ヘルパーである。「MCPに関係するファイルが一切ない」とは評価していない。

## 検証で見つけて修正した問題

**既存のWindowsビルドエラー**: AnomalyBehaviourとLureAnomalyの双方に、同名の保存項目 `revealColor` があり、Playerのビルドが停止した。Lure側だけ `hallwayRevealColor` に変更し、Standin_Lure.prefabの該当キー1箇所を同名に変更した。色の値・シーン・GUIDは維持した。旧名を維持する親側とぶつかるため、曖昧な旧名変換属性は使っていない。全使用箇所を調べ、明示的なPrefab上書きがないことを確認した。新規のシリアライズ検証と実ビルドで確認した。

初回の失敗記録は `artifacts/builds/20260906-143111-950/build.json` に残している。修正前の2ファイルは `artifacts/backups/lure-serialization/` に保存した。

**MCP更新の取り込み**: 10.0.0では通常ログがExceptionとして返る現象を確認した。10.2.0の取得後も旧コンパイル済みコードが一時的に残ったため、全体Refreshと再コンパイル・再接続を実施した。最終テストでは通常ログがLogとして返ることを確認した。パッケージ取得中にAssetImportWorker 0/1が旧パッケージのasmdefを読めず停止した記録がある。その後の再コンパイル・テスト・ビルドは成功し、継続エラーはない。原因を完全に保証するものではないため、更新時の再発は監視対象とする。

**Windows文字コード**: SDKの英語説明に含まれる記号をCP932で出力できず失敗したため、クライアント出力をUTF-8に固定した。

**生成物の混入**: Unityのテストが生成したPC情報のJSONや、ビルドが書き出したURP/Input System/ProBuilderの設定を確認した。対象設定のビルド前後を保存して元へ戻す処理を追加した。ProBuilderが既定値`editor.stripProBuilderScriptsOnBuild=true`を再保存する差分は残る。導入済みProBuilderのソースで、この値は元からtrueが既定値であることを確認した。空の`Assets/Resources`フォルダのmetaも残るが、生成されたPC情報JSONはAssetsから退避済み。テスト前から存在する同名データは退避対象にしない。

**Playerの画面取得**: 最初の起動APIは「targetable windowがない」と返したが、その後のウィンドウ列挙でPlayerを特定できた。起動APIの返答だけで起動失敗と断定しない。Computer Useの画像と条件は`artifacts/verification/player-20260907/`に保存した。画像内の青いカーソル強調はComputer Useによる表示を含む。

`plateText`の割り当て先や表示内容はゲームの仕様に関係するため、今回の環境整備では推測して埋めていない。追跡は`docs/KNOWN_ISSUES.md`に記録した。

## 作業保全と戻し方

開始時のユーザー変更はSandbox.unity、Packagesの2ファイル、未追跡Utility。これらをコミット・reset・cleanしていない。開始前のコピーとバイナリ差分、Assets全285ファイルのハッシュは、このCodexタスクの`work/unity-baseline-20260906/`と、プロジェクトの`artifacts/backups/environment-start-20260906/`に保存した。

今回追加・変更したファイルだけを差分レビューし、必要なら対象を絞って復元する。MCP10.0.0の直前状態は `artifacts/backups/mcp-10.0.0/` にある。バージョンを戻す場合もUnityパッケージとサーバーの両方を揃える。

Gitの全体diff --checkは、変更前からのSandbox.unity差分に含まれるUnity生成の末尾空白を検出した。Sandboxの内容は開始時ハッシュと一致しており、見栄えを整えるための再保存・空白削除はしていない。今回の対象差分を限定した確認では問題はなかった。

## まだ検証していないこと

Windows版の連続移動・OS入力による長押し操作の完遂、長時間プレイ、低スペック環境、目標FPS、音の聴感、初見プレイヤーによる面白さ・怖さの評価。これらはテスト成功や画面1枚から推測しない。完成仕様、発売可否、品質基準は未承認のまま保持している。

現行Computer Useの公開APIにはキーやマウスボタンを指定時間だけ押し続ける機能がない。今回の長押し回帰テストはUnity Input Systemの合成入力で通している。Playerの実機長押しまで自動化する手段は、必要な操作と対象を定めて別途検討する。

## 利用手順

1. Codexの保存済みプロジェクト「GraduationProject」と、同じ場所のUnityプロジェクトを開く。
2. PowerShellで `./tools/UnityAgent.ps1 Start`。
3. Unityの **Tools > Unity Agent > Connect Local MCP**。
4. 設計相談は `$horror-design-review`、実装・検証は `$unity-verified-development` を使って依頼する。

Unityのメニューから要求したビルドがまだ始まらない場合は、Editorを前面にして更新が進む状態にする。新しいbuild.jsonが生成されるまでは、ビルド開始済みと扱わず、重複要求もしない。

簡単な開始方法はdocs/START_HERE.md、詳しい手順はdocs/DEVELOPMENT.md、品質の判断方法はdocs/VALIDATION.md。

## 採用判断の根拠

- [CoplayDev Unity MCP](https://github.com/CoplayDev/unity-mcp): 既存MITライセンスの連携を拡張して採用。別ブリッジへのFork/移行は不要と判断。
- [v10.2.0リリース](https://github.com/CoplayDev/unity-mcp/releases/tag/v10.2.0): Console種別・画像色空間等の修正を確認して更新。
- [Unity Input System 1.19のTesting](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Testing.html): 隔離された入力テストと復元手順を採用。
- [Unity Test Framework 1.6](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/manual/index.html): 既存TestRunnerとXML形式を再利用。
- [Unity ScreenCapture](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScreenCapture.CaptureScreenshot.html): 最終画面保存を採用。

既存プロジェクトを検証できるようにするため、追加は作品専用の手順・記録・検証コードに絞った。
