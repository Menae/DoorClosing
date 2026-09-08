# 現在地・再開情報

## M2 Astraレビュー（2026-09-08）

- 担当: task 01a07fbb-3ac2-7a62-a049-19d919724625。Sol担当taskはidle/完了を確認し、ユーザー依頼のレビュー・不具合修正のみ引き継ぐ。
- 対象: 9263d9e / 85fb0bf / 566e5e2のM1境界とM2中核。M2-04の新規シーン制作は今回の範囲外。
- 発見／修正予定: 誘引でかご内から閉を押した後、閉扉中に外へ出て再開扉するとDiagnosisへ戻り、敷居越えが失われる。GAME-005/007に従いRevealと救済へ移す。
- 対象ファイル／検証: BeatStateMachine、InteractionInputTests、本書。合成Mouse→Raycastの再現テストを先に実行し、修正後に全Play Mode・Consoleを確認する。既存未コミット素材・Sandbox等は保護。EditorはM1NormalRoute/Edit Mode、テスト実行なし、対象projectRoot一致。
- 再現と追加発見: 誘引の再現は修正前に失敗（artifacts/astra-review/before-fix.json）、修正後の全回帰では当該テスト通過。全29件中、Sol追加の走行テストがwalk=0.117m/sprint=0.153mで失敗（artifacts/tests/20260908-063939-071/playmode.xml）。同フレーム数でも経過時間が異なるため、NormalRouteSceneTestsを実経過時間あたりの速度比較へ修正し、1.35倍の判定基準は維持した。
- 乗っ取り追加修正: ElevatorControllerとは別のHijackAnomaly motorが初期正解後の停止中も再生されることを、合成クリックとAudioSource.isPlayingで再現（artifacts/astra-review/motor-before-fix.json）。Resolve開始時に既存Cleanupを呼び、motor・drift・jitterを停止する。音量・音色の聴感評価ではない。
- 最終結果: レビュー・修正完了。Unityコンパイル後の全Play Mode 30/30（artifacts/tests/20260908-064251-862/playmode.xml）、Edit Mode 2/2（artifacts/tests/20260908-064332-273/editmode.xml）、Console Error 0（artifacts/astra-review/console-final.json）。追加2テストは修正前失敗→修正後合格を確認。対象4ファイルのdiff check合格。全体diff checkには既存Sandboxの末尾空白があり、今回変更せず保護した。
- 画像／限界: 同回帰によるM1NormalRouteの最終Game View corridor.png（artifacts/m1-02/scene-input-20260908-064306-769、759x427）を実見。これはM1の合成Keyboard/Mouse一巡であり、M2の画面・実音・Windows Player・恐怖の合格ではない。コードは566e5e2＋今回4ファイル差分。M2-04配線と人の評価は未完のまま。
- 停止時: EditorはM1NormalRoute/Edit Mode、実行中テストなしとの応答。状態resourceにstale表示があるため、それ単独での最新性は保証せず、完了XMLとConsole応答を併記する。今回4ファイルだけをWORK-005に従い保存し、既存未コミット5ファイルとResources.meta/Utilityは混ぜない。次工程は従来どおりM2-04。追加実装の自動継続は開始しない。

## M2-03 乗っ取りの期限・非常停止救済（2026-09-08）

- 状態／担当: 中核実装・自動検証完了。現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。
- 目的／見える行動: 8階超え表示と上昇音の乗っ取り中、初期期限内の非常停止で短く停止して通常走行へ戻る。閉または初期期限超過はReveal。Grace内の非常停止は救済、閉または期限超過は死亡。無効な開・階は入力として持ち越さない。
- 対象仕様／決定: GAME-005・006・008、ROADMAP M2。既存BeatStateMachineの内部期限と共通Reveal/Graceを使い、停止後の走行再開を検証可能な状態として固定する。試作時間はBeatDefinitionへ限定し、人の難易度評価前は正式値としない。
- 対象ファイル／設定: BeatStateMachine、ElevatorController、InteractionInputTests、hijack BeatDefinition 2件、DECISIONS、本書。2アセットを非常停止正解・閉扉走行・初期期限6秒へ修正。6秒はGAME-015に基づく試作値で、人の難易度評価前は正式値ではない。Main/Sandboxと既存未コミット素材は変更していない。
- 検証結果: 乗っ取り対象Play Mode 5/5、全Play Mode 28/28、Edit Mode serialization 2/2、Console Error 0。期限前停止の一時停止→再走行、期限超過Reveal、Grace救済、二度目の閉による死亡、Grace期限死を合成Mouse→Raycastで確認。証跡は `artifacts/tests/20260908-062447-008/playmode.xml` と `artifacts/tests/20260908-062532-627/editmode.xml`（git対象外）。
- 未検証／次: Mainへの3系統配線、8超え表示と走行音の実画面・実音、Windows Player実入力、人の理解度・難易度・恐怖は未検証。次はM2-04として既存Mainを直接汚さず、3系統を一巡できる専用縦切りシーンと実入力証跡を作る。

## M2-02 誘引の身体・閉扉・救済境界（2026-09-08）

- 状態／担当: 実装・自動検証完了。現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。
- 目的／見える行動: 誘引のかご内待機は無期限。敷居越えで初回誤答。外から閉は閉扉後に死亡。Grace中は身体帰還＋閉受理＋閉扉完了で生還し、受理後の閉扉時間は旧期限を超えても死亡させない。閉塞で再開扉した場合は残猶予から再開。
- 対象仕様／決定: GAME-005〜007、ROADMAP M2、WORK-002。かごとCharacterControllerの水平方向完全内包判定を通常帰宅と共有し、既存BeatStateMachineの系統別完了条件として実装。
- 対象ファイル: BeatStateMachine、NormalJourneyController、新規CabinOccupancy、InteractionInputTests、DECISIONS、本書。既存未コミットのLureAnomaly・Standin_Lure・Sandboxは変更しない。
- 検証結果: 誘引対象Play Mode 5/5、全Play Mode 23/23、Edit Mode serialization 2/2、Console Error 0。合成Mouse→Raycastと身体座標変化で、無期限待機、敷居越えReveal、外閉死亡、帰還＋閉扉完了、閉扉中期限超過、閉塞後の残猶予再開を固定。証跡は `artifacts/tests/20260908-061505-964/playmode.xml` と `artifacts/tests/20260908-061557-856/editmode.xml`（git対象外）。
- 未検証／次: 実シーン配線、Windows Player実入力、見た目・恐怖は未検証。次はM2-03の乗っ取り期限と停止救済を同じ入力経路で固定する。

## M2-01 挑発の無操作成功（2026-09-08）

- 担当／場所: 現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。M1-03検証後に継続。
- 目的／見える行動: 挑発中は非常停も閉も押さず待てば正解。初回に押してRevealしても、Grace中に何も押さなければ回復。Grace中の2回目誤入力は死亡。
- 対象仕様／決定: GAME-004〜006、ROADMAP M2、IMPL-M2-01。無操作成功時間はBeatDefinitionの試作値とし、既存BeatStateMachineの共通遷移を拡張する。
- 対象ファイル: BeatDefinition、BeatStateMachine、InteractionInputTests、DECISIONS、本書。新依存・新セーブ形式・シーン保存なし。
- 検証: 無入力診断成功、初回誤入力後の無入力回復、Grace中再誤入力の死亡を合成Input Systemで確認。コンパイル、対象Play Mode、全Play Mode回帰、Consoleを分けて記録する。
- 結果: BeatDefinitionに挑発の無操作成功時間を追加。BeatStateMachineでDiagnosisの無入力成功、誤答後Graceの無入力回復、Grace中の再誤答死亡を実装。対象3/3合格（artifacts/m2-01/test-result.json）、全Play Mode 18/18合格（artifacts/tests/20260908-055152-192/playmode.xml）、Edit Mode serialization 2/2合格（artifacts/tests/20260908-055408-809/editmode.xml）、Console error 0。
- 制限／次: まだM2専用シーンとBeat assetには配線しておらず、画像・Windows Player・人の聴感は未検証。次は誘引の敷居越え、身体帰還＋閉、閉扉完了と残り猶予の境界を実装する。

## M1-03 残る入力境界の検証（2026-09-08）

- 担当／作業場所: 現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。前担当の明示停止後、ユーザーの再開指示により引き継ぐ。
- 対象仕様: GAME-008／009、ROADMAP M1。走りが歩きより速い、通常停止の自動再開後に新たな1回を受け付ける、本物8階で閉じても安全に再提示されることを確認する。
- 対象ファイル: PlayerLook、NormalJourneyController、NormalJourneyInputTests、NormalRouteSceneTests、UnityAgentMenu、本書。不足がテストだけならゲーム挙動は変更しない。
- 検証: 合成KeyboardのWとLeft Shiftによる移動量比較、合成Mouse→Raycastで停止・再開・再受付、本物8階の閉扉後のArrived復帰、Unityコンパイル、対象Play Mode、Console。
- 開始時状態: main、staged差分なし。多数の既存変更を保護。ローカルMCP server 10.2.0は起動したが、Unityのinstance登録は0件で接続待ち。テスト／ビルド実行中なし。対象Editorはproject path一致、M1NormalRouteを読込済み。復旧のため固定local endpointのみdomain reload後自動再接続するEditor補助を追加。
- 初回検証: 対象2件中、停止後再受付は通過。走りは同一フレームにキューしたShiftとWの合成入力が上書きされ、walk=0.251m / sprint=0mで失敗。製品修正はせず、Shift反映後にWを押す再現可能なテストへ修正して再検証する。失敗記録: artifacts/m1-03/test-result.json。
- 最終検証: 再実行2/2合格（artifacts/m1-03/test-result-rerun.json）。通常停止は停止中連打を1回に抑え、再開後の新たな非常停を2回目として受理。本物8階の閉扉後はArrivedへ復帰。左Shift走行は実シーン上で歩行の1.35倍超を確認。全Play Modeは15/15合格（artifacts/tests/20260908-054450-189/playmode.xml）、Console error 0。MCPの最初の全件要求はdomain reload後にorphan化したため明示クリアし、Editorメニュ経由の結果XMLで合格を確認。
- 残り: Computer Useにネイティブappが公開されていないため、Windows実入力の連続一巡は未検証。ゲーム挙動の変更はなく、この制限を残してM2へ進む。

## 就寝前の引継ぎ — 2026-09-08

- 状態: ユーザーの「一旦寝る」に従い開発を停止。この依頼では本書のみ更新し、Unity操作・追加実装・テスト・ビルドを開始していない。以下の過去の「作業中」「再実行予定」は履歴であり、現在の実行指示ではない。
- 変更内容: M1-01でInteractionRaycasterの即時クリック化、入力持越し・重複受付・壁越し操作の防止、BeatStateMachineの無効入力保護。M1-02でNormalJourneyController、PlayerLookの走り/重力、ElevatorControllerの閉塞再開扉を追加。独立M1NormalRouteシーン、生成・ビルド用Editorメニュー、関連PlayModeテストを整備。自宅ラベルとホバー時の可読性も修正。
- 検証済み: 最終PlayMode 14/14合格（artifacts/tests/20260907-181004-025/playmode.xml）。合成Keyboard/Mouse→移動/視点→Raycast→クリックで通常帰宅を一巡。Game View実見、Console警告/エラー0。Windows Development Build成功、Playerの起動・描画・短い入力を確認。証拠の詳細は末尾のM1-02最終検証記録。
- 未検証・未完成: Windows実入力による連続移動一巡、走りの専用回帰、音の配線/聴感、性能・長時間安定性、人の理解/怖さ。怪異FSM・夜進行との統合、基本3系統の新仕様対応、全4夜、保存/メニュー、空間ループは未完成。既存MainのplateText未設定/703表示、既知D3D12診断行も未解決。M1全受入・完成ゲームとは扱わない。
- 最後のEditor確認: M1NormalRoute/Edit Mode、isDirty=false、Player終了。今回ライブ状態は再確認していない。再開時は接続先・未保存・実行中処理を照合し、テスト/ビルドを重複起動しない。
- 次の作業（順番）: ①必読文書・担当・git差分とUnity状態を確認。②M1の走り、通常停止からの再開と再受付、本物8階で閉→安全な再提示など、既存テストが覆っていない境界を確認し不足分を検証。③Windowsの連続実入力一巡は合成入力と別枠で記録。④M2基本3系統の接続計画（無操作成功、誘引の身体帰還＋閉扉完了、救済期限、死亡再挑戦）を記録して着手。既に合格した試験は関連変更や新たな懸念がない限り繰り返さない。
- Git保存状態: main、upstreamはorigin/main、今回開始時staged差分なし。ゲーム・環境・運用文書の既存変更が多数残る。本書も未追跡で、別taskの運用追記を含むため、今回の引継ぎだけを既存内容から独立した追加ファイルとしてコミットできない。既存一式の確認なしに混在コミットせず、今回はcommit/pushを保留。再開時に由来と依存を確認してWORK-005に沿う単位へ整理する。artifactsを含めローカル保存であり、Git/リモートへバックアップ済みではない。
- この引継ぎの担当: task 01a07cef-5e12-7de1-a923-91d73de50bd0。ユーザーの再開指示まで実装は停止。

## Git委任更新（2026-09-08、WORK-005）
- 本taskによる文書更新: ユーザー明示依頼で自動commit・区切りの通常pushを運用化。AGENTS/START_HERE/DEVELOPMENT/DECISIONSの旧禁止・依頼待ち記述を置換する。ゲーム担当・実装は変更しない。
- 導入時注意: 今回触れる運用文書自体が以前から未追跡で、ゲーム実装記録などの既存内容を含む。今回の追記だけを通常のファイル追加として分離できないため、既存文書一式の初期コミットは依存・内容確認後に行う。既存ゲーム変更との一括commit/pushは行わない。

## Git運用整備（2026-09-08）
- 担当: task 01a07f5f-d3ae-7920-a5c2-8b0069f98cda。文書のみの依頼。既存実装担当はidleを確認、担当を引き継がない。
- 計画: AGENTSのGit手順、START_HEREの利用案内、DECISIONSの根拠を追記。現状mainと多数の既存未コミット変更を保護する。
- 確認: 4文書の追記を読戻し、Git workflowへの参照整合を確認。Gitの書込み操作・Unity操作・ゲーム変更なし。
- 状態: 文書整備完了。コミット・push・設定変更は未実施。

## 運用文書の整備（2026-09-08）
- 担当: task 01a07f5f-d3ae-7920-a5c2-8b0069f98cda。ユーザー依頼によるモデル運用の簡素化のみ。既存実装担当taskは一覧でidleを確認し、その担当・ゲーム進捗は変更しない。
- 計画: START_HEREにSol medium主体・難所のみAstraへの引継ぎを記載し、AGENTSから参照、DECISIONSへ委任内の運用選択を記録する。ゲーム挙動への影響なし。
- 確認: 追記の読戻しと参照先の存在確認。ゲームコード・Unity操作・テスト実行は対象外。
- 状態: 上記4文書を更新、追記の読戻しと参照先存在確認を完了。初回の複数ファイルpatchはDECISIONSの文脈不一致で未適用となり、対象を確認して修正・再適用済み。ゲームコード・Unityは未変更、節約効果は未測定。

最終更新: 2026-09-08。更新者: task 01a07cef-5e12-7de1-a923-91d73de50bd0。

## 一目で分かる状態

- 仕様: [GAME_SPEC.md](GAME_SPEC.md) v1.0を文書化済み。ユーザーの意図確認と今回の文書化依頼に基づく。
- 現在工程: M2実装中。M1の残る合成入力境界を検証し、挑発の無操作成功・誤答後無操作回復の中核ロジックを実装。
- 最新の実装単位: M2-01 挑発の無操作成功。コンパイル、対象3/3、全Play Mode 18/18、Edit Mode 2/2、Consoleを検証済み。
- Unity操作担当／実装担当: 現在のtask（01a07f7e-0cd8-7500-b2c1-afbb84a88bfb）。この記録は排他ロックではない。
- 次の一手: M2-02誘引の身体・敷居・閉扉・救済期限の境界を既存FSMに接続し、分離配置の合成入力で固定する。
- 人の作業待ち: 現時点なし。最初の体験評価はM2の短いビルドが遊べてから。
- M1-01: 入力コード・FSM・テスト・Editor検証補助を変更。9/9 Play Mode合格、Mainの一時配置でOSクリック→PressClose→Departを確認。
- 文書整備時の履歴: 対象10文書のリンク・文字化け検査に合格。全体git diff --checkは既存Sandbox.unityの末尾空白を報告。今回は独立M1NormalRouteを追加し、Main/Sandboxは保存していない。

## 工程状態

| 工程 | 状態 | 証拠／残り |
|---|---|---|
| 文書・運用 | 完了 | 仕様、決定、保留、工程、現在地、実験、開始手順を整備。リンク・参照を検査 |
| M1 通常帰宅 | 試作実装済み・一部未検証 | 入力・閉塞・実シーン・走り・停止再受付の全回帰15/15合格、画面実見。Windowsビルド・起動・描画確認。OS連続操作一巡・人の受入は未実施 |
| M2 基本3系統 | 中核ロジック実装済み・縦切り配線待ち | 挑発・誘引・乗っ取りの入力／期限／身体／扉境界は全Play Mode 28/28合格。専用シーン配線、表示・音の実見、Windows Player、人の受入は未実施 |
| M3 全4夜 | 未着手 | 6表現・順序・追加遭遇 |
| M4 保存・メニュー | 未着手 | 累積時間・死亡の継続含む |
| M5 仕上げ | 未着手 | 素材、音、視認性、設定等 |
| M6 学内完成 | 未着手 | 技術検証とユーザー受入が必要 |
| M7 販売準備 | 意図的保留 | プラットフォーム・価格・発売日未決 |

## 引き継ぐ実装事実と既存作業

- 導入時のEdit Mode2/2、Play Mode5/5、Windows Development Build成功は旧実装の履歴。[ENVIRONMENT_REPORT.md](ENVIRONMENT_REPORT.md) 参照。クリック仕様や新しい怪異ルールの成功ではない。
- 旧長押し・ゲージとNone誤答はM1-01で修正。旧Beat/RunManager進行、既存Main配線は未統合。新しい通常帰宅はM1NormalRouteで検証する。
- 文書整備前からGitに多数の変更・未追跡あり: .gitattributes/.gitignore、Sandbox、Lure関連、Packages、slnx、ProBuilder設定、Editor・Tests・Utility、docs/tools/.agents等。ユーザー作業を含む。まとめてreset/clean/stash/commitしない。
- 既知問題は [KNOWN_ISSUES.md](KNOWN_ISSUES.md)。以前の警告を解決済みと推測しない。
- この文書は同じファイルを共有するローカルチャット向け。別worktree/別PCでは未コミット文書は自動同期されない。

## 作業中はこの欄を更新する

- タスクID・目的: M1-02、呼ぶ→乗る→8→降車→自宅の独立機能試作。
- 担当／作業場所: task 01a07cef-5e12-7de1-a923-91d73de50bd0、このcheckout。2026-09-08 02:38 JSTからM1着手。
- 対象仕様・決定ID: GAME-009、GAME-006、GAME-015、WORK-001/002、IMPL-M1-02。
- 対象ファイル: NormalJourneyController、PlayerLook、InteractionRaycaster、ElevatorController、M1NormalRouteBuilder、M1NormalRouteシーン、PlayMode tests、UnityAgentMenu。
- 見える結果・検証: 即時クリック、中央点、走り、重力、扉閉塞再開扉、通常停止、8階帰宅。合成入力と実画面、Windows確認を分離。
- 実施済み変更: 上記実装、14/14合格。自宅ラベルの拡大崩れとボタンの白飛びを試作シーン設定で修正。
- 実行中の操作: なし。Player終了、M1NormalRoute/Edit Mode、isDirty=false。結果はartifacts/tests、artifacts/m1-02、artifacts/builds。
- 最後に確認した区切り: 20260907-181004-025 Play Mode14/14。Windowsビルド20260907-181110-326成功、Player起動・描画・短い入力確認。
- 中断時注意: 最新の結果ファイルとEditor状態を確認してから再実行。ビルド要求の二重送信・MainへのPlay変更保存をしない。
- 次の具体的な操作: M1の残る入力境界を仕様と照合。M2はまだ未着手。

各単位終了時、実装／コンパイル／自動テスト／画像／Player／人の評価を別々に記入。証拠のない項目は未検証。中断・利用制限後は実行中処理の完了を確認してから再実行する。ログが古いという理由だけで他の担当を解除しない。

## M1-01 作業記録（2026-09-08 02:38 JST）
- 担当: task 01a07cef-5e12-7de1-a923-91d73de50bd0、このcheckout。他の稼働担当なしをタスク一覧で確認。
- 対象: GAME-009即時クリック、GAME-006入力破棄、無効ボタンの誤答抑止。
- 計画: InteractionRaycasterを押下エッジ確定へ変更。旧ゲージ参照は保存互換のため残して非表示。BeatStateMachineの受付と無効ボタンを保護。InteractionInputTestsをクリック要件へ更新。
- 期待行動: 中央対象への短い左クリックで1回確定。押し続け・対象外・提示中の入力は次の受付に持ち越さない。
- 検証: 短いクリック、連続押下、対象外、Reveal/Travel入力破棄、Grace救済、無効ボタンの合成入力テスト、Console、Main Game View、実入力。証拠はartifacts/m1-01とartifacts/tests。
- 接続確認: MCP 10.2.0復旧、projectRoot一致、Main/Edit Mode、全シーンisDirty=false。既存シーン・Prefab差分は保持。
- 状態: 実装開始。未検証。通常帰宅配線は次単位。
- 検証補助追加: UnityAgentMenuにMain専用Play Mode一時配置メニュー。MCPのset_propertyはPlay Mode時にエラーを返したため停止して状態を戻した。実入力検証のみの固定視点と閉ボタン配線をUnity APIで設定し、通常Playerには含めない。9/9入力テスト合格済み。
- 2026-09-08 02:45 JST再開: クレジット不足による中断後、コード差分とMCPを照合。現在Main/Edit Mode、テスト実行なし。前回の実クリックはCommit証拠なし・画像は壁のため未合格。固定視点設定の上書きを調査して再試験する。

## M1-01 検証結果
- Play Mode: artifacts/tests/20260907-174006-805/playmode.xml、9 passed / 0 failed / 0 skipped。Input System合成マウス→Raycast→FSM。
- 実入力: MainをPlay Modeで一時配線。固定視点からComputer UseのOS左クリックでPressCloseが1回確定、Committed→Depart。artifacts/m1-01/smoke-console-final.json。
- Game View: artifacts/captures/game-view-20260907-174915-328.png + JSON、750x422、Linear、最終画面を実見。ゲージなし、操作盤あり。旧撮影174336/174720は壁で不適切だったため合格画像に使わない。
- 未検証: 自然な移動を含む一巡、通常シーンのボタン配線、Windows Player、人の体験評価。既存NormalArrivalのplateText未設定と703表示は次工程の課題。
- Mainの一時変更はPlay終了で復元。保存していない。

## M1-02 計画（委任範囲内、2026-09-08）
- 目的: 呼びボタン→乗車→8階選択→通常走行→降車→自宅クリックで導入夜終了。無効階、通常停止、挟まり再開扉、中央点、走りを独立試作で確認。
- 方針: Input System/PlayerLook/InteractionRaycaster/ElevatorControllerを再利用。通常帰宅用の小さなControllerを追加し、怪異FSMを置換しない。新依存・保存形式・公開なし。既存Main/Sandboxは保存しない。
- 対象: NormalJourneyController、入力ルーティング、PlayerLookの任意走り、ElevatorControllerの扉状態・閉塞判定、EditorのM1NormalRouteシーン生成、専用PlayMode tests。新シーンは灰色箱の機能試作で、最終アート・基準廊下の正式採用ではない。
- 試作値: 歩き2.5m/s、走り4m/s（Left Shift）、扉1秒、通常走行4秒、停止1秒。身体判定と戻れる距離を検証。掲示・配置はGAME-015委任内の初期案。
- 検証: 合成Input→Raycastで一巡、無効操作、停止連打抑止、閉扉挟まり、初期Hallと8階Corridorの切替。Game ViewとWindows Development Player。失敗・未検証を分離する。
- M1-02コンパイル成功、入力テスト12/12合格（artifacts/tests/20260907-175810-798/playmode.xml）。NormalJourney一巡、停止連打、外から8無効、閉塞と閉途中侵入を確認。シーン生成・実画面・Playerは作業中。
- 実シーン一巡テスト初回は13/14。シーン読込完了前にSetActiveSceneした検証コードの例外で失敗。ゲームの一巡成功とは扱わず、Async読込完了待ちへ修正して再実行予定。壁越し入力の回帰は合格。

## M1-02 最終検証記録（2026-09-08 03:15 JST）
- 実装: 通常帰宅Controller、クリック振分け、任意の走り・重力、扉の身体閉塞再開扉、独立ProBuilderシーン。初期Hall→8階Corridor→自宅で暗転。怪異FSM・夜進行との統合は未実施。
- 最終Play Mode: artifacts/tests/20260907-181004-025/playmode.xml、14 passed / 0 failed / 0 skipped。短いクリック・無効ボタン・壁遮蔽・状態遷移・救済/死亡・通常停止連打・身体帰還/扉閉塞・実シーンW移動とマウス視点による帰宅一巡。
- 初回のシーン読込テスト失敗はAsync完了待ちで修正。13/14の失敗記録を保持し、その後の合格と区別。
- 画像: artifacts/m1-02/scene-input-20260907-181015-550/（Game View、750x422、Unity 6000.3.14f1、URP17.3.0、Linear）。panel-hover.pngで8の文字と選択色を実見。自宅の拡大文字は180842-593/corridor.pngで修正を実見。初期・廊下・終了画像も保存。最終アートの採用ではない。
- Console: artifacts/m1-02/console-final.json、warning/error 0。
- Windows: artifacts/builds/20260907-181110-326/build.json、Succeeded、0 errors / 0 warnings、5.72秒。M1NormalRouteのみのDevelopment Build。MainのBuild Settingsは変更なし。既知の設定5ファイルはビルド前と現在のSHA256一致。
- Player: 上記exeを実際に起動。Computer Useで1920x1080の初期Hall・掲示・呼びボタンを実見。マウス座標入力で視点変化、短いW入力も送信したが移動量は判定できず、OS一巡の成功とはしない。画面はこのtaskのComputer Use返却画像、ログはartifacts/m1-02/Player-181110.log。正常終了を確認。
- Player診断: C#例外なし。既知のD3D12 info queue診断0x80004002が残る（KNOWN_ISSUES PLAYER-001）。APIの起動応答はウィンドウなしだったが列挙で既存起動を確認、重複起動なし。
- Player含有物: ScriptingAssemblies.jsonにTests/Editorアセンブリなし。既存MCPForUnity.Runtime.dllは含まれる（互換/シリアライズ/撮影ヘルパー）；Editor接続ブリッジは含まれない。MCP全体を除去済みとはしない。
- 残り: OS連続移動・走り専用回帰・音・性能・初見の理解/怖さ、既存Main統合、怪異3系統/全4夜/保存。音素材はこの試作へ未配線。M1全受入または完成ゲームとは認定しない。
- Git: 変更した追跡済みゲームコードのdiff --check合格。既存Sandbox等の差分を保持、コミットなし。
