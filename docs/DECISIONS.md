# Decisions

## ENV-001 — Development environment
- Status: approved scope / initial implementation and technical verification complete
- Date: 2026-09-06; verified 2026-09-07 JST
- Owner: user; routine reversible engineering choices delegated to Codex
- Context: Codex-led Unity development with design discussion, actual execution, screenshots and reproducible tests. Windows PC target; free tools preferred.
- Decision: Reuse Unity 6000.3.14f1, URP 17.3.0, CoplayDev MCP, Computer Use, Git/LFS, Input System and Unity Test Framework. Add focused test/editor tools, project skills and decision records.
- Authorization: User asked to proceed automatically except for changes with extremely large consequences or expensive reversal. Routine setup does not need repeated approval; unconfirmed game design is not decided by this authorization.
- Effects: Project documentation, skills, tests, local development tools and Git review settings. No engine migration, paid commitment, public release or history rewrite.
- Evidence: docs/ENVIRONMENT_REPORT.md. Edit Mode 2/2, Play Mode 5/5, Windows Development Build succeeded with zero build errors/warnings. Game View and Windows Player images inspected; actual mouse look observed. Sustained OS keyboard/hold input, audio, performance and human experience remain unverified.

## ENV-002 — Preserve existing work
- Status: adopted under delegated scope / checked
- Date: 2026-09-06; checked 2026-09-07 JST
- Owner: Codex
- Context: Initial working tree modifies Sandbox.unity, manifest.json and packages-lock.json; Utility scripts are untracked. HEAD: 1921a37.
- Decision: Keep pre-change files and hashes. Do not reset or commit the user's work. Package edits extend the preserved manifest and lock.
- Evidence: artifacts/backups/environment-start-20260906 contains copies, a binary diff and SHA-256 hashes of 285 existing asset files; original also retained in the originating Codex task work/unity-baseline-20260906.
- Result: 283/285 existing Assets match the initial hashes. The two changes are the scoped fix in ENV-004. Main, Sandbox and pre-existing Utility files match. artifacts/verification/asset-integrity.json records exact differences.

## ENV-003 — Local MCP
- Status: adopted under delegated scope / connected and checked
- Date: 2026-09-06; checked 2026-09-07 JST
- Owner: Codex
- Decision: Retain the existing unityMCP endpoint http://127.0.0.1:8080/mcp; pin both server and Unity package to v10.2.0 and disable optional MCP telemetry. The MCP endpoint is loopback only.
- Reason: Existing MIT-licensed connection supports this project. v10.2.0 resolves relevant Console/screenshot issues; reuse and extend rather than fork. Selected tool results still go to Codex for processing.
- Effects: tools/UnityAgent.ps1, tools/unity-agent/pyproject.toml, uv.lock, Packages/manifest.json and packages-lock.json. Server 10.2.0; Unity package lock commit 30d22075093d1d35dfb0091c1c7550e9ad948577. Local SDK call commands require an instance and validate the actual projectRoot. No Windows autostart service added.
- Limits: Unity Development Player has its own standard PlayerConnection behavior, separate from this MCP endpoint. docs/KNOWN_ISSUES.md records update/foreground-control limitations.

## ENV-004 — Existing serialization error blocking Windows build
- Status: adopted under delegated routine-fix scope / tested
- Date: 2026-09-06
- Owner: Codex
- Context: LureAnomaly and its base class serialized the same field name revealColor. Unity rejected the Windows build.
- Options: Rename only the derived field and preserve its exact prefab value; or redesign the class/assembly structure. The latter is unnecessary for this build error.
- Decision: Rename the derived field to hallwayRevealColor and only its matching Standin_Lure prefab key. Preserve both saved red values, base field, scene contents and GUIDs. Do not introduce an ambiguous FormerlySerializedAs mapping that collides with the surviving base field.
- Effects: Assets/_Scripts/Anomalies/LureAnomaly.cs, Assets/Prefab/Anomalies/Standin_Lure.prefab, serialization regression tests. No new creative decision is represented by this fix.
- Evidence: artifacts/backups/lure-serialization contains originals; Edit Mode 2/2 and successful Windows build cover name uniqueness and retained values. Full usage search found no scene overrides requiring migration.

## ENV-005 — Verification evidence and known gaps
- Status: adopted under delegated tooling scope
- Date: 2026-09-07 JST
- Owner: Codex
- Decision: Store local build/test/capture evidence outside Assets, in ignored artifacts/. Preserve rather than automatically delete it. Use synthetic-input tests and observed Windows input as distinct evidence.
- Effects: Editor menu, docs/VALIDATION.md and docs/KNOWN_ISSUES.md. New generated performance-test machine information is archived; existing same-name files are preserved. Build callbacks back up known settings before/after restoring their bytes.
- Limits: ProBuilder persists its existing true default for script stripping; empty Resources.meta remains. Continuous Windows input, audio and human playtests are not certified. Missing plateText assignment is recorded without inventing its intended content.


# ゲーム基準版の決定記録

以下は2026-09-07に文書化。Owner: ユーザー。Status: 設計インタビューで選択済み、最終基準版への肯定と共有文書化依頼を受領。実装・検証・人の体験受入は別途。各要件の正確な例外は GAME_SPEC.md を参照する。

## GAME-001 — 体験と対象
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 常に怖さだけを強める方針は不採用。
- Decision: 未知の怖さから理解・習熟へ移る体験。PC操作に慣れたホラー初心者、静かな自宅、初回死亡込み10〜20分。
- Reason: 恐怖に慣れた人にも対処を上達する楽しさを残す。恐怖量の維持だけを目的にしない。
- Effects: docs/GAME_SPEC.md、全体体験・PLAYTEST_PLAN H-07/H-09。記載は変更予定であり実装完了ではない。

## GAME-002 — 基準版の規模
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 全5夜、3/4/5は拡張候補で必須にしない。
- Decision: 導入1＋怪異3夜、通常遭遇3/3/3、3系統各2表現の6パターン。
- Reason: 短時間と制作条件の中で基本学習・別表現・習熟をつなぐ。
- Effects: docs/GAME_SPEC.md、夜進行・遭遇データ・ROADMAP M2/M3。記載は変更予定であり実装完了ではない。

## GAME-003 — 固定配置と最終夜抽選
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 全夜完全ランダム、死亡時完全固定は基準版に採らない。
- Decision: 前半2怪異夜は基本／別表現をそれぞれ固定、最終夜は系統順と表現を抽選し死亡時も再抽選。
- Reason: 学習の足場と後半の変化を両立する。
- Effects: docs/GAME_SPEC.md、RunManager・配置・再挑戦。記載は変更予定であり実装完了ではない。

## GAME-004 — 安定した原則と掲示
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 説明HUD中心、隠れた原則反転、正しい操作の恣意的裏切りは不採用。
- Decision: 少数の原則を環境内掲示で教える。ルール自体は全編有効。
- Reason: 状況の見極めに迷わせ、理解した操作の信頼性を守る。
- Effects: docs/GAME_SPEC.md、掲示・情報提示・全怪異。記載は変更予定であり実装完了ではない。

## GAME-005 — 3系統と固有手がかり
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 複合怪異と決定的合図の完全な偽装は基準版外。
- Decision: 誘引は廊下差分／閉、挑発は非常停止への誘い／無操作、乗っ取りは8超え表示＋走行音上昇／停止。同時1系統。
- Reason: 観察と判断を明確な因果につなぐ。
- Effects: docs/GAME_SPEC.md、怪異・音・表示・ResponseEvaluator。記載は変更予定であり実装完了ではない。

## GAME-006 — 修正可能な失敗
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 全初誤答即死、提示中入力の持越し、走り必須の救済は不採用。
- Decision: 初誤答は変貌・手がかり後に修正受付、2度目誤答は死亡。後半は猶予を少し短縮するが速い判断なら歩きで救済。
- Reason: 初回の誤解を学習へ変え、習熟にも緊張を残す。
- Effects: docs/GAME_SPEC.md、FSM・タイマー・入力境界。記載は変更予定であり実装完了ではない。

## GAME-007 — 乗降と閉扉
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 戻るだけで救済、かごから室番号必読は不採用。
- Decision: かご内で真偽判別。敷居越えで誘引誤答。帰還＋閉で救済、外から閉は死。身体内で期限内に閉受理なら閉扉処理超過だけで死なない。挟まりは再開扉・残猶予再開。
- Reason: 判断と操作完了を公平に結び、扉アニメーションの遅延で罰しない。
- Effects: docs/GAME_SPEC.md、身体領域・扉・誘引。記載は変更予定であり実装完了ではない。

## GAME-008 — 通常ボタンと追加怪異
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 追加を通常ノルマに充当、追加を悪化済みで開始、段階解禁、33%の表示は不採用。
- Decision: 正常走行の非常停止受理1回につき約33%で全6から均等の初期怪異を追加。停止連打は再抽選なし、導入免除。危険は掲示、確率非表示。他階無効、通常閉で怪異なし、本物再提示は安全。
- Reason: 不要な停止には危険を持たせ、単純な連打抽選や安全行動への隠れた罰を避ける。
- Effects: docs/GAME_SPEC.md、通常走行・ボタン・抽選・掲示。記載は変更予定であり実装完了ではない。

## GAME-009 — 基本操作
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 長押しゲージ、スタミナ管理を基準版から除外。
- Decision: WASD・マウス・左クリック即時。走りあり、スタミナなし。中央点と対象反応。
- Reason: 観察と判断が中心で、長押し待ちを主な難しさにしない。
- Effects: docs/GAME_SPEC.md、PlayerLook・InteractionRaycaster・Interactable。記載は変更予定であり実装完了ではない。

## GAME-010 — 保存とリプレイ
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 常時計時HUD、厳密な競技用専用TA、オンラインランキングは必須外。
- Decision: 現在夜から再開、前夜保持。累積死亡・時間保持、怪異3夜を計時し死亡含む／ポーズロード除外。結果・自己ベスト、クリア後導入省略。
- Reason: 気軽な中断と上達の記録を両立する。
- Effects: docs/GAME_SPEC.md、保存・計時・結果・メニュー。記載は変更予定であり実装完了ではない。

## GAME-011 — 場所と経路
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 廃墟化、単なる施錠、遠い室番号のみで判別は不採用。
- Decision: 築約40年の居住中10階建て、自宅8階。正面に直線廊下、真正の特徴は毎夜固定。退出経路は同じホールへ空間ループ。
- Reason: 日常の記憶を真偽判定に使い、帰宅できない異常を空間で表す。
- Effects: docs/GAME_SPEC.md、レベル・ランドマーク・導入。記載は変更予定であり実装完了ではない。

## GAME-012 — 表現と物語
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: AAA怪物必須、無予告全面顔、長文解説、暗ければ怖いという前提は不採用。
- Decision: 写実寄り、白い蛍光灯、空間・機械、環境音中心。予兆ある急変可。短い任意断片、帰宅明確・原因と消滅は曖昧。
- Reason: 生活感を保ち、冗長な説明なしに違和感を生む。
- Effects: docs/GAME_SPEC.md、アート・音・物語。記載は変更予定であり実装完了ではない。

## GAME-013 — 設定と対応環境
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 字幕を基準版必須にする推奨は採らず、対応済みとも表示しない。
- Decision: 感度・反転・FOV・明るさ・音量・表示設定必須。揺れブラーは任意、軽減無効可。スピーカー攻略可。PAD条件付き、重要音字幕は最後。
- Reason: 操作快適性と必要な合図を守り、制作優先順位はユーザー選択に従う。
- Effects: docs/GAME_SPEC.md、設定・UI・品質検証。記載は変更予定であり実装完了ではない。

## GAME-014 — 期限・予算・販売
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 学内だけで終了、販売前の自動拡張、無断購入・公開は不採用。
- Decision: 9/21縦切り、10月学内完成目標、12月上旬最終。後に有料販売、ストア価格日は保留。無料優先、追加1万円優先・必要時2万円以内検討。
- Reason: 学内完成と商用展開を分け、必須を膨らませず品質を確認する。
- Effects: docs/GAME_SPEC.md、ROADMAP・素材・リリース。記載は変更予定であり実装完了ではない。

## GAME-015 — 試作詳細の委任
- Status: 承認済み設計（GAME-015の試作詳細は正式採用待ち）
- Date: 2026-09-07; Owner: user
- Context / options: 試作承認＝正式採用、ログ＝恐怖の証明は不採用。
- Decision: 機能と公平性を固定し、具体演出・掲示文・数値の初期案をCodexが範囲内で試作。変更理由・試作値・評価を記録。
- Reason: 全演出を会話で固定せず、遊べるものを人が評価する。
- Effects: docs/GAME_SPEC.md、PLAYTEST_PLAN・試作記録・体験受入。記載は変更予定であり実装完了ではない。

## WORK-001 — Codex主導の実装と共有記録
- Status: ユーザーの依頼に基づく運用構築・委任範囲の記録
- Date: 2026-09-07; Owner: user（運用の具体化はCodex）
- Context: 全Codexが仕様・開発順序・今の作業を理解し、人は最小限のUnity操作とフィードバックに集中したい。
- Decision: 同じローカルプロジェクトにAGENTSの読込指示、仕様、工程、現在地、決定、保留、試験計画を保存する。既存Unity MCP環境を再利用。進行・記録・通常の可逆的実装・技術確認はCodexが担当する。
- Reason / alternatives: 会話だけより再開時に根拠を読める。外部ボードや新サービスはこの制作では不要。別worktreeには未コミット記録が自動共有されない点に注意。
- Authorization: 今回は承認された基準版の文書化と開発運用構築を実行。以後の開発依頼では承認済み仕様内の実装・検証・可逆的技術判断を毎単位確認せず進める。具体的演出・数値はGAME-015の試作委任。未承認の体験・必須範囲変更や高影響の判断は含まない。
- Boundaries: エンジン／描画基盤の移行、大型システム置換、新たな依存・サービス、破壊的データ変更、支出、外部通信・公開等は既存の承認条件を維持。新しいセーブ形式等の重要実装判断は実装前に影響を提示し、既存承認・委任でカバーされない場合に確認する。
- Effects: AGENTS.md、docs/START_HERE.md、ROADMAP.md、PROJECT_STATUS.md、仕様関連文書、検証方針。ゲーム実装は今回未着手。
- Limits: 記録は自動排他・自動同期・利用制限中の自動継続を保証しない。単一Unity操作担当と単位ごとの記録更新で運用する。人の感想・発売判断は代理認定しない。

## IMPL-M1-02 — 通常帰宅試作の構成
- Status: WORK-001/GAME-015の委任に基づく可逆的実装選択。2026-09-08、Owner: Codex。
- Evidence: PlayerLook/InteractionRaycaster/ElevatorController/FSM/RunManagerとMainの配線を確認。既存MainはNormal1件、閉None、初期壁向き。M1-01入力は9テストと一時配置OSクリックで確認。
- Adopt / Extend / Build: 導入済みInput SystemとURPを採用、既存移動・扉を拡張、通常帰宅専用Controllerと独立M1NormalRoute機能試作を作る。怪異FSMは保持する。
- Alternatives: FSMに入口・帰宅・夜全体を集中させる案は既存怪異状態との混線が増えるため今回は採らない。全面FPSフォークも不要。通常制御と怪異制御でボタン受理・扉状態が二重管理になる段階で接続方法を再評価する。
- Sources（2026-09-08参照）: https://github.com/Unity-Technologies/InputSystem はUnity公式、Samples/Tests構成、Unity Companion License＋一部第三者条件。既存固定版を継続し外部ソースはコピーしない。https://github.com/Unity-Technologies/FPSSample は公式一人称比較、READMEに2018.3/HDRP/保守終了、約18GBと明記。LICENSEへのリンクあり、素材採用をしないため個別権利の適合認定はしない。https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html でMoveは重力を自動適用しないことを確認。
- Trade-off: 既存シーンを傷めず一巡を検証できる。仮空間は既存アートとの統合・廊下の正式評価を残す。FPS戦闘・ネットワーク構造は転用しない。
- Acceptance: 入力から呼ぶ/乗る/8/走行/降りる/自宅まで通り、扉閉塞で再開扉、無効ボタンで死亡しない。合成入力・Game View・Playerを分離して検証する。

## WORK-002 — 既存コードの品質改善
- Status: ユーザーが明示。2026-09-08、Owner: user。
- Decision: 既存コードは低品質なAI生成との説明があり、必要な変更・置換を許可。コードの形を維持すること自体を目的にしない。
- Effects: 承認済みゲーム仕様と検証結果を基準に整理・修正する。仕様・表現の意図の変更や支出・公開等の別ゲートは引き続き既存規則による。

## WORK-003 — モデル運用の簡素化
- Status: ユーザーの運用簡素化・必要文書編集依頼に基づくCodexの可逆的運用選択。モデル別配分をユーザーが個別承認したという記録ではない。
- Date: 2026-09-08; Owner: Codex（task 01a07f5f-d3ae-7920-a5c2-8b0069f98cda）。
- Context / options: Pro 5xの週次利用枠あたりの完成量を優先。Fast無効。常時Astra監督、細かなLuna振分けは人とモデル双方の管理負担が増えるため初期運用に採らない。
- Decision / reason: Sol mediumで通常開発を継続し、具体的な難所のみCodexがAstra mediumへの引継ぎを準備する。人は通常の再開文と通知時の切替だけを担当。詳細条件はSTART_HEREに一本化する。
- Effects / limits: AGENTS.md、START_HERE.md、PROJECT_STATUS.md。ゲーム仕様・検証基準・実装担当は維持。設定の自動変更・新規サービス・サブエージェント起動なし。節約率・最適性は未実測。

## WORK-004 — 単独開発のGit手順
- Superseded: 下記WORK-005が自動commit・通常pushの承認条件を更新。以下は初期整備時の履歴。
- Status / date / owner: ユーザーのGitルール整備依頼に基づくCodexの運用具体化、2026-09-08。
- Context / options: mainに既存未コミット変更が多数ある。工程ごとの分岐を強制する運用より、差分保護・依頼時の小単位コミットを採る。
- Decision / reason: AGENTSのGit workflowに集約。明示ステージ、検証・.meta保全、同期・競合・認証・破壊操作の境界を記載し、既存の自動コミット禁止と外部書込み承認を維持する。
- Effects / limits: AGENTS.md、START_HERE.md、PROJECT_STATUS.md。Git設定・ブランチ・履歴・リモートは変更しない。コミットやpushの包括許可ではない。

## WORK-005 — 自動コミット・区切りのpushを委任
- Status / date / owner: ユーザーが明示承認、2026-09-08、Owner: user。
- Decision: AIが意味のある変更単位を判断して自動commitし、キリのよい箇所で既存upstreamへ通常pushする。毎回の指示・確認は不要。WORK-004の依頼待ち運用を置き換える。
- Reason / effects: 保存忘れと人の管理負担を減らす。AGENTS、START_HERE、DEVELOPMENTに実行条件を反映。検証と差分の保護は維持し、既存混在変更を一括取り込みしない。
- Limits: force-push、履歴書換え、新規リモート・公開範囲変更、PR・リリース・デプロイへの委任ではない。失敗は成功扱いせず記録する。

## IMPL-M2-01 — 挑発の無操作成功を既存FSMに拡張
- Status / date / owner: WORK-001・GAME-015の委任内の可逆的実装選択、2026-09-08、Owner: Codex。
- Evidence: BeatStateMachineはTravel→Diagnosis→Reveal→Grace→Resolve/DeathとRunManagerの再試行を既に保持するが、Diagnosis/Graceの無入力を成功にできない。ResponseEvaluatorのNoneは誤答であり、GAME-005の挑発と不一致。
- Decision / reason: BeatDefinitionに挑発用の無操作成功時間を追加し、BeatStateMachineが入力なしの診断完了を明示的に成功扱いする。初回誤答後は既存Grace時間の無入力完了をGraceRecoveredとする。共通演出・死亡・進行を再利用し、系統ごとの並行FSMは増やさない。
- Alternative / trade-off: 各怪異専用Controllerは局所条件を隔離できるが、現時点でReveal/Grace/DeathとRunManager連携が重複する。誘引の扉・身体条件と乗っ取りの時限を加えた結果、2つ以上の入力受付経路で同じ条件分岐が重複する場合は境界を再検討する。
- Proof: 無入力DiagnosisのCorrect、誤答後無入力GraceのGraceRecovered、Grace中2度目誤答のDeath。合成Input Systemと全Play Mode回帰で検証する。具体時間と演出効果は人の評価待ち。

## IMPL-M2-02 — 誘引の身体境界と閉扉完了を既存FSMに拡張

- Status / date / owner: WORK-001・GAME-005〜007の承認内の可逆的実装選択、2026-09-08、Owner: Codex。
- Context: 誘引はボタン回答だけでなく、身体の敷居越え、閉ボタン受理時の身体位置、閉扉完了、閉塞による再開扉を一つの救済条件として扱う必要がある。従来FSMは入力直後に正解を確定し、扉アニメーションとGrace残時間を関連付けていなかった。
- Decision / reason: 通常帰宅と誘引でCharacterControllerの水平完全内包判定を `CabinOccupancy` に共有する。誘引は敷居越えを合成 `ExitCab` 誤答として既存Revealへ渡し、かご内の閉受理後はGrace消費を止め、実際の閉扉完了時だけ生還とする。閉塞で再開扉した場合は保存した残時間から入力受付を再開し、外からの閉は閉扉完了後に死亡させる。
- Alternative / trade-off: 誘引専用MonoBehaviourへ状態を分離する案は扉所有権と共通Reveal/Death進行を二重化するため今回は採らない。FSM内の系統分岐が今後さらに増え、同じ扉待機処理が複数系統へ広がる場合は、回答ポリシーを専用Strategyへ抽出する。
- Proof / limits: 誘引対象Play Mode 5/5、全Play Mode 23/23、Edit Mode serialization 2/2、Console Error 0。合成Input System→Raycastと身体座標で境界を固定した。実シーン配線、Windows Player実入力、演出品質と恐怖は未検証。

## IMPL-M2-03 — 乗っ取りの期限と停止後の走行復帰

- Status / date / owner: WORK-001・GAME-005・006・008・015の承認内の可逆的実装選択、2026-09-08、Owner: Codex。
- Context: FSMには未使用の乗っ取り期限分岐があったが、既存2アセットは正解が閉ボタン、開扉、期限0で仕様と逆だった。また正解後に走行状態を復帰する責務が次Beat開始へ暗黙依存していた。
- Decision / reason: 初期期限超過を内部 `None` 誤答として共通Revealへ渡し、Graceは既存の独立期限で非常停止だけを救済とする。乗っ取り正解時はResolve中だけ走行を止め、解決完了前に再走行させる。挑発は無操作成功時に不要な停止を入れない。2アセットは非常停止正解・閉扉走行・表示8から上昇・初期期限6秒へ修正した。
- Trial value / trade-off: 6秒はGAME-015に基づく初期試作値で、2表現とも同値にして表現差と難易度差を混ぜない。人の実プレイで識別時間と操作距離を測り、後半夜の短縮値を別途決める。各系統専用FSMは共通Reveal/Grace/Deathを重複させるため採らない。
- Proof / limits: 乗っ取り対象Play Mode 5/5、全Play Mode 28/28、Edit Mode serialization 2/2、Console Error 0。合成Input System→Raycastで期限前停止、一時停止からの再走行、期限超過、Grace救済、二度目誤答とGrace期限死を確認。実シーンの表示・音、Windows Player実入力、難易度・恐怖は未検証。

## IMPL-M2-04 — 3系統縦切りと自己完結する試作手掛かり

- Status / date / owner: WORK-001・GAME-004〜009・012〜015の承認と試作委任内の可逆的実装選択、2026-09-08、Owner: Codex。
- Context: 3系統の中核ロジックは分離テスト済みだが、一つの実シーンで通常入力を通す配線がなく、既存stand-in prefabのローカル座標と音・字幕参照も未設定だった。M1を直接変更すると通常帰宅の証拠を崩す。
- Decision / reason: M1から独立した `M2VerticalSlice` とrepair可能なBuilderを作り、Lure→Provocation→Hijackを短い一巡として配線する。BeatDefinitionにpresentation local transformを持たせ、既存prefabをデータ側で配置する。外部音素材なしでも区別を検証できるよう、挑発は車内の偽指示表示＋短い生成チャイム、乗っ取りは既存階数drift＋生成機械音、誘引は廊下中央の異常柱とする。
- Trial values / trade-off: Lure travel 2秒、Provocation無操作5秒、Hijack初期期限6秒、順番・寸法・ASCII文言・生成音色は人の評価前の試作値。説明HUDは追加せず環境内提示に限定した。procedural toneは依存と素材権利を増やさない一方、正式な音響品質ではない。日本語font asset追加は今回行わず、試作文言をASCIIに限定した。
- Proof / limits: 通常のWASD・マウス・短クリックで3系統一巡1/1、全Play Mode 31/31、Edit Mode 2/2、scene validate 0 issue、Console Error 0、Windows Development Build成功・Player応答とLure到達を確認。画像で柱、偽指示、9階表示を実見。Player画面の直接目視、OS入力一巡、聴感、初見理解、恐怖は未検証で、正式採用を意味しない。
- Effects: `Assets/Scenes/M2VerticalSlice.unity`、M2 Builder/テスト/ビルドメニュー、BeatDefinition/BeatStateMachine、Provocation/Hijack presentation、3 Beat asset、PROJECT_STATUS。

## IMPL-M2-05 — 夜経路の帰宅完了と死亡時の入口復帰

- Status / date / owner: WORK-001・GAME-003〜009の承認内の監査修正、2026-09-08、Owner: Codex。
- Context: M2-04は怪異列から直接開始し、3怪異終了だけでRunClearになっていた。死亡も怪異1へ戻すだけでPlayer位置・視点・空間が残り、承認済みの「入口から開始」「本物8階から帰宅」「死亡後は現在夜の入口」と不一致だった。
- Decision / reason: 既存NormalJourneyControllerを入口と本物8階の両端に再利用し、怪異中だけ無効化してRunManagerへ入力所有権を渡す。RunManagerは最終怪異後を帰宅待ちとして保持し、自宅ドア通知だけでRunClearする。死亡fadeではPlayerの開始pose、階表示、扉、空間、UIを一括して入口状態へ復元する。新規夜FSMや依存を増やさず、既存の通常経路・怪異経路の責務を接続する。
- Trade-off: 現在はシーン開始poseをAwakeで保持するため、将来の夜別spawnやsave再開を導入する際は明示的なcheckpointデータへ置き換える余地がある。M2範囲では単一シーン・現在夜入口の要件を満たす。
- Proof / limits: 対象2/2、全Play Mode 34 pass / 0 fail / 2 known skip、Edit Mode 2/2、scene validate 0、Console Error 0、Windows Development Build成功・Player起動ログ正常。入口、帰宅廊下、帰宅後clear、死亡後入口を画像で目視。Windows OS実入力と人の理解・恐怖は未評価。
