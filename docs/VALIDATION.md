# 検証方針

ゲームの設計承認と、実装が期待どおり動いた証拠は分ける。現段階のテストは「既存挙動の回帰検証」であり、新しいゲーム仕様の承認ではない。

## 最小の開発サイクル

1. docs/GAME_SPEC.md と関連決定を読み、今回変える挙動と受け入れ条件を明示する。
2. 未コミット差分、対象プロジェクト、未保存シーンを確認する。
3. 小さく実装する。外部ファイルを追加したらUnityへ全体Refreshを要求する。
4. 最新のEditor状態・Consoleを確認する。パッケージ更新は取得完了とコードの再コンパイル完了を別々に確認する。
5. 変更箇所に関係するテストを実行し、XMLのpassed/failed/skippedを読む。APIのsuccessだけで合格扱いにしない。
6. 描画・操作に関係する変更はPlay ModeとGame Viewを実見する。必要な場合はWindows Playerでも実施する。
7. 結果、画像、再現手順、未検証部分を記録する。新たな懸念がなければ同じテストをむやみに繰り返さない。

## 導入したテスト

以下は環境導入時の旧長押し仕様の回帰テスト記録。GAME-009では即時クリックを採用したため、対象実装を変更するときに意図のある回帰範囲を保ちながらテストを更新する。旧合格結果を新仕様の成功へ書き換えない。

**Tools > Unity Agent > Run Input Regression Tests** で実行するPlay Modeテスト:

| テスト | 実際に通す経路・確認 |
|---|---|
| Hold_CommitsThroughInputRaycastAndStateMachine | 合成マウス入力 → 中央Raycast → ゲージ進行 → 長押し確定 → BeatStateMachine → Correct/Depart |
| EarlyRelease_CancelsHold_AndNewPressCanCommit | 長押し途中の解放で取り消され、押し直しで確定できる |
| ContinuousHold_DoesNotCommitAgainAfterRepresentation | NormalのPressCloseで同じBeatに戻った際、押し続けたまま二重確定せず、離して押し直せば再確定できる |
| TargetLost_CancelsHold | 長押し中に照準対象がなくなると確定せずゲージを消す |
| WrongAction_ThenCorrectInput_RecoversDuringGrace | 誤答 → Grace → 押し直した正しい長押し → GraceRecovered |

InputTestFixtureで入力環境を分離し、テスト後にデバイス・設定・時間倍率・生成オブジェクト・イベント購読を戻す。実装のDebugSubmitActionは使わない。テストは分離された配置で実行し、既存Main/Sandboxを編集しない。これはOSの実マウス試験とは区別する。

**Run Serialization Checks** で実行するEdit Modeテスト:

- ゲームのMonoBehaviour階層に同じ保存フィールド名が重複していない。
- Standin_Lureの既存2色が、フィールド名修正後も両方とも元の保存値を維持している。

## 画面・操作の証拠

- シーン名、撮影時点の状態、解像度、Unity/URP/MCP版、入力方法を記録する。
- **Capture Game View** のPNGとJSONを基本にする。MCP撮影では `capture_source=game_view`、`include_image=true`、`output_folder=artifacts/captures` を指定し、最終合成画面の場合はcameraを指定しない。
- UI・ポストエフェクトを含む最終画面であるか、返却情報と実画像の両方を見る。Scene ViewやCamera単体の画像は補助資料とする。
- GUIの実入力を使う際はUnity/Playerの正しいウィンドウを前面にすることを知らせ、入力後の画面を確認する。1回のクリックや短いキー入力を、連続移動・長押し操作全体の確認と呼ばない。
- 明るさ・照準・HUDの不明点を見つけても、確認のために無断でゲームの照明やデザインを変えない。Play Mode中の一時的な検証配置は、保存せずに終了する。
- 音の出力と人がどう感じるかは別。音声を取得できない場合に「聴いて問題なし」と書かない。

## Windows Player

**Build Windows Development** で生成する。`build.json` の `status=Succeeded` と実行ファイルの存在を確認する。Development Buildは発売用ビルドの代用ではない。

Playerを実際に起動し、描画、短い操作、Playerログを確認する。テスト用アセンブリやEditor用ブリッジが通常のPlayerに含まれていないことも調べる。低スペックPC・長時間プレイ・フレームレート目標は別途承認された条件で検証する。

ビルド前後の設定は `artifacts/builds/<UTC時刻>/before-build` / `after-build` に記録する。URP/Input System/ProBuilderがビルド中に書き出す既知の設定ファイルをビルド直前の値へ戻したうえで、Git差分を確認する。キャッシュされた既定値が再保存される場合もあるため、自動復元だけで全差分が消えたと断定しない。残る差分はdocs/KNOWN_ISSUES.mdに記録する。テスト実行が新規生成したPerformanceTestRunInfo等は結果フォルダへ退避し、元からある同名ファイルは保持する。既存の検証メタデータがAssetsに残っている場合はビルドを拒否して確認する。

## 面白さ・怖さの検討

設計スキルで「狙う体験 → それを生む仕組み → 失敗する条件 → 小さな試作 → 初見の人による評価」を記録する。AI同士の賛成をプレイヤーの反応と見なさない。

例: 狙いが『慣れた場所への不信』と承認された場合、予告情報の出し方だけを変えた短い試作を比較する。気づいた場所、行動の理由、退屈・混乱・理不尽と感じた時点を振り返る。感想を誘導せず、プレイ前に答えを教えない。具体的な恐怖演出や勝利条件はここでは決めない。

## 結果の書き方

新仕様の受入項目と工程はROADMAP.md、体験仮説はPLAYTEST_PLAN.md、最新の実施状況と証拠はPROJECT_STATUS.mdに記録する。ゲーム実装開始後の各単位で更新し、変更だけで完了にしない。

新仕様で特に必要な境界の検証:

- 通常Input System → Raycast → 即時クリック → 確定。1押下1回、提示中入力の破棄、解決後の追加入力、無効ボタン。
- 挑発の無操作成功、乗っ取りの無操作悪化と救済、誘引のかご内無期限観察。
- 初誤答と再誤答、敷居越え、外から閉、身体帰還＋閉扉完了、期限内閉受理後の時間超過、挟まりと残猶予。
- 固定夜と最終夜再抽選、追加遭遇が通常枠を消費しない、停止中連打の抽選抑止、導入免除。乱数は制御した試験で分岐を確認し、単発試行で33%を証明しない。
- 夜保存と続き、累積時間・死亡、ポーズとロード除外、破損セーブ非破壊、導入スキップ、結果。
- 表示・音・設定無効時の合図、Windows Player、実測機器を明記した1080p60目標。人の怖さ・理解はPLAYTEST_PLANで別に評価。

各項目を **実装済み / コンパイル済み / 自動テスト済み / 画像確認済み / Player確認済み / 人による評価済み / 未検証** で区別する。エラー・失敗・スキップ・タイムアウトも残す。検証中に既存問題を見つけたら、原因、最小修正、保存値や既存作業への影響を記録する。

導入時の実測結果は docs/ENVIRONMENT_REPORT.md を参照。

## M1入力・通常帰宅の追加検証（2026-09-08）
旧長押し結果は上記の履歴。現在のInteractionInputTestsはクリック、対象外・壁の遮蔽、連続押下、Travel/Reveal/Resolveの入力破棄、Grace救済、無効ボタン、二度目誤答を確認する。
NormalJourneyInputTestsは通常帰宅の受付と扉閉塞を分離配置で確認。NormalRouteSceneTestsはM1NormalRouteを読み、合成Keyboard W・Mouse delta・短いクリックで移動から帰宅まで通す。座標へのテレポートやSubmitAction直接呼出しで代用しない。結果と画像はPROJECT_STATUSに記録する。
M1NormalRouteは既存Main/Sandboxと独立した機能試作。最終的な建物・照明・廊下の見分けや恐怖の受入ではない。Build M1 Normal Route Windows Developmentはビルド設定リストを変更せず当該シーンだけをビルドする。
