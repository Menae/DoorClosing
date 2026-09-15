# 現在地・再開情報

更新: 2026-09-15。ここには現在の担当・進捗・次の作業を置く。過去の詳細は [PROJECT_HISTORY.md](PROJECT_HISTORY.md)、仕様の正本は [GAME_SPEC.md](GAME_SPEC.md)。履歴中の「現在」や古い担当を再開指示に使わない。

## 現在のゲーム開発

### TEXT-001 文章編集の入口を整備（2026-09-15）

- 担当Astra。ユーザーの全表示文の直接執筆依頼に対応。現行3シーンのHierarchy最上部 `★文章編集_ここから` に、起動／ポーズ／翌夜・結果／設定／動的案内／館内表示／掲示の8カテゴリを配置。日本語Inspectorから編集・Ctrl+S保存。詳しくは [DEMO_AUTHORING](DEMO_AUTHORING.md)。ルール・物理配置は変更なし。
- メニューの固定文をシーン保存値へ接続。空欄保持・長文スクロール・ボタン名変更に対応。内装再適用でも執筆内容を保持。掲示は既存EditableNoticeへの編集入口を追加し、元の場所からも編集可能。
- 検証: 保存・再読込・ApartmentVisualPass再適用で独自タイトル／空欄の保持を確認。PlayMode対象2/2合格（長文・空欄・改名ボタン・不正階数書式＋既存の通常帰宅→3怪異→死亡再挑戦→帰宅・再プレイ）。`artifacts/tests/20260915-071925-578/playmode.xml`。初回テストコードのTMP参照不足は修正後に合格。全回帰34件の再実行はしていない。
- 実見: `artifacts/text-01/title-inspector.png` と `artifacts/demo-02/input-20260915-071930/` の最終Game Viewで編集欄・タイトル・設定を確認。
- 通常Windows build: `artifacts/builds/20260915-072340-630/GraduationProject.exe`、11.7秒、error/warning 0。今回の新buildのWindows実入力は未確認。以前起動したPlayerには文章編集は反映されない。作者の執筆後は保存・再buildする。
- 開始時の未保存シーンは `artifacts/text-01/user-scene-before.unity` にコピー保護。意味差分はTMPスタイルhash／Canvas追加shader channelのみと確認し、保持した。停止時はPlayableDemo保存済みEdit Mode。人の理解・怖さは未受入のまま。
- 次: ユーザーが上記8カテゴリで文章を執筆し、実プレイでルール説明の分かりやすさを評価する。文章変更とゲームルール変更は別。Solへの全面引継ぎは今回実施していない。

### DEMO-002 完成候補を実装・検証（2026-09-15）

- 担当: Astra task 01a07fbb-3ac2-7a62-a049-19d919724625。開始main clean / 8ae08e1。DEMO-001の続行と「掲示でのルール学習／Hierarchyで文章編集箇所を明示」を実装。Solへの全面引継ぎは保留のまま。
- 起動: `Assets/Scenes/PlayableDemo.unity`。開始→通常帰宅→翌夜の基本3怪異→帰宅→再プレイ／終了。死亡は怪異夜の入口へ戻る。M1/M2は個別確認用として保持。通常buildの入口もPlayableDemoに変更。
- 掲示: 右壁に三原則、左掲示板に帰宅手順と修正方法。遮蔽を避け、掲示用照明と短い日本語で提示。Hierarchyで `文章編集` を検索し、各EditableNoticeの見出し／本文を編集。保存・内装再適用・再読込で文章を保持。詳細は [DEMO_AUTHORING](DEMO_AUTHORING.md)。
- UI／音: 開始・ポーズ・設定・完了画面。メニュー中は入力／時間／音を停止し、復帰直後のクリックも遮断。フォーカス喪失でポーズ。感度・反転・視野角・明るさ・音量・表示サイズを調整。足音4種と低音量の換気音を独自生成。ゲーム側の永続保存・新依存なし。
- 成果物: `artifacts/builds/20260915-062502-491/GraduationProject.exe`、必要runtimeファイル＋README／licenses入り `artifacts/demo-02/PlayableDemo-20260915.zip`。ローカル評価用。外部公開はしていない。artifactsはGit外で自動同期されない。
- 停止時: Unity EditorはPlayableDemoの保存済みEdit Mode、compile／Console errorなし。テスト・buildの実行中処理なし。Windows Playerはユーザーの確認用に起動したまま残す。キー操作の確認後に強制終了しない。
- 次: ユーザーが解説なしで入口の掲示に気づくか、普通の廊下を覚え、三原則で判断できるかを通しプレイで評価。迷い／不公平／文字・音の判別を優先修正する。大型美術再制作やM3全4夜へ広げない。初見理解・怖さ・音色の受入を技術合格から認定しない。

## 最新の技術証拠と限界

| 項目 | DEMO-002の結果 |
|---|---|
| 回帰 | PlayMode 34/34、失敗・skip 0（20260915-061502-693）。続いてデモの死亡／再挑戦・怪異中ポーズを加えた通し1/1（062149-789、143秒）。EditMode 2/2（062129-324） |
| 入力・画像 | 合成Keyboard/Mouse→UI raycast／ゲームRaycast→clickで導入・三系統・死亡再開・帰宅・再プレイ。最終画像はartifacts/demo-02/input-20260915-062153。掲示Inspectorはnotice-inspector.png。掲示の保存／再適用保持、紙面超過なし、再読込で不要なdirtyなしを確認 |
| 音 | 実移動による足音発火と換気source再生、怪異中ポーズのAudioListener停止／復帰を確認。生成波形のpeak／RMS確認。聴感・自然さは未認定 |
| Windows | 通常build成功、error/warning 0、Tests assemblyなし、README／font license同梱（062502-491）。Computer Useのクリックで開始・設定・720p→1080p切替・反転表示を確認。フォーカス喪失後のポーズを実見。ユーザーが実キーボードのEsc・W正常を確認 |
| 限界 | Computer Useキー送信ではW/Escの反応を確認できず、ユーザーの実キー確認と区別。Windows全経路・終了操作、初見理解、怖さ、スピーカー聴感、1080p60fps測定・最低環境・長時間安定性は未認定。既知D3D12診断行は残る |

初回テストの状態名誤記と提示前クリックをテスト側で修正した。途中の掲示遮蔽・強い照明・開くだけでdirtyになる副作用は実装側で解消。詳細な試行は [PROJECT_HISTORY](PROJECT_HISTORY.md#demo-002-掲示編集とデモ接続2026-09-15)。本編4夜・別演出・保存は [DEMO_PLAN](DEMO_PLAN.md) に従い保留する。

## DOCS-06 Astra運用文書の整理（2026-09-15）

- 状態: 文書編集・検証完了。担当: task 01a0a383-4c35-73c0-b769-4876bb5eb297、このcheckout。開始main clean / 9210c30。ゲーム実装担当を引き継がず、Unity操作・テスト・buildは実行していない。
- 変更: AGENTS、START_HERE、PROJECT_STATUS／HISTORY、VALIDATION、DEVELOPMENT、OPEN_QUESTIONS、DECISIONS、2スキル、WORKFLOW_AUDIT_2026-09-15。目的別参照、現在地の一本化、旧hold訂正、完了・継続と検証範囲を整理。ゲーム仕様・コード・Assets・設定は無変更。
- 検証: 18文書の相対リンク48件・見出し7件は不備0。旧進捗本文のbyte一致、変更範囲、git diff --checkを確認。2スキルのquick_validateはUTF-8指定で合格。証拠はartifacts/docs-06/validation.json。
- 失敗／回復と限界: quick_validateの初回はWindows既定cp932でUnicodeDecodeError、`-X utf8`で解消。代表的な作業の指示読み合わせを実施したが、モデル動作・時間・利用枠削減の実測ではない。個人用Global AGENTSの所見は監査へ残し、このプロジェクト外の設定は編集していない。
- Git事前確認: WORK-005に従う保存単位は今回の11文書。既存公開origin/mainは9210c30、開始ahead/behind 0/0、ローカルActions定義なし、commit／push hookはGit LFS。`gh`は通常ホストのPATHにも見つからず、認証変更は行わずgit ls-remoteとGitHub接続のrepo metadataで確認した。最終commitと送信状態はGit履歴・statusを参照。
- 次: 本依頼の文書作業は完了。ゲーム開発を再開するときは上の現在地から所有・ライブUnity状態を確認する。

## 記録の更新

### DEMO-001 方針相談（2026-09-15）
- 担当: Astra task 01a07fbb-3ac2-7a62-a049-19d919724625。開始main clean / 4dca30f。今回の目的は読取監査・方針相談・決定記録で、ゲーム実装やUnity操作はしない。
- 対象: PROJECT_STATUS、DECISIONS、DEMO_PLAN。現在の仕様／記録、進行・入力・掲示・音・build設定と保存済み証跡を照合。完了条件は不足・優先順・合格条件・ユーザー選択の記録と文書差分の検査。
- 結果: ユーザーは導入＋怪異1夜を選択。掲示の非常停止一律推奨が正解規則と矛盾、M1/M2未接続、終了UIなし、Esc非ポーズ、標準buildリストが旧Mainを指す点を優先課題へ。過去のゲーム検証を今回再実行したとは扱わない。実装未着手、次回はDEMO_PLANの順序で再開。

作業開始時に目的／工程・担当task・対象ファイル・期待結果・必要な検証を現在の作業欄へ記す。単位完了・停止時に実施結果、残る不確実性、実行中処理、次の具体的な行動へ更新する。現在地を上書きする前に必要な詳細を履歴へ残し、古い「次の作業」を現行欄へ積み重ねない。
