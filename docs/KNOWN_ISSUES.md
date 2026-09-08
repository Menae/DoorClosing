# 既知の問題と検証の限界

2026-09-07 JST時点。環境の稼働確認と、ゲームの完成判定は別。

| ID | 状態 | 観測内容 / 影響 / 次の確認 |
|---|---|---|
| GAME-001 | 未修正・既存 | MainのWindows Playerで`Standin_Normal(Clone)`の`plateText`が未設定と警告され、文字演出が省略された。どの表示に何を出すかを確認してから割り当てる。該当実装はNormalArrival / AnomalyBehaviour。 |
| VERIFY-001 | 未検証 | マウスでPlayerの視点が変わることは確認済み。短いW/Sキーでは明確な移動を確認できなかった。現行Computer Use APIに押下時間指定がなく、連続移動とOS入力の長押し完遂は未検証。Input Systemの合成入力による長押し回帰テストは5/5成功。 |
| VERIFY-002 | 未検証 | 音の聴感、長時間安定性、最低動作環境、目標FPS、初見の怖さ・面白さ。承認した条件と人のプレイで評価する。 |
| TOOL-001 | 回避手順あり | メニューのWindowsビルド要求はEditorApplication.delayCallで開始する。Editorが前面にないと開始が遅れる場合がある。前面へ切り替え、build.jsonを確認する。開始前に重複要求しない。 |
| TOOL-002 | 回復済み | MCP 10.2.0取得直後、旧コンパイル済みコードとAssetImportWorkerの旧asmdef参照が残った。全体Refresh・再コンパイル・再接続後のテストとビルドは成功。再発時は元ログを保存して調査する。 |
| TOOL-003 | 記録済み | ビルド後、ProBuilderが`editor.stripProBuilderScriptsOnBuild=true`を設定ファイルへ再保存する。既存パッケージの既定値と同じ。自動生成された空のResourcesフォルダmetaも残る。PC情報JSONはAssetsに残していない。 |
| PLAYER-001 | 診断行あり | D3D12ログに`failed to query info queue interface (0x80004002)`がある。その実行で描画は確認できたが、診断行の原因を確定してはいない。今回これを根拠にDX11への切り替えやドライバー変更はしていない。 |
| TOOL-004 | 回避手順あり | Player起動APIがtargetable windowなしを返しても、後のウィンドウ列挙で実際のPlayerが見つかった。プロセス・ログ・列挙結果を照合してから再試行し、重複起動しない。 |

証拠: `artifacts/verification/player-20260907/`の画像・capture.json・Player.log、およびdocs/ENVIRONMENT_REPORT.md。
