# 現在地・再開情報

更新: 2026-09-19。ここには現在の担当・進捗・次の作業を置く。過去の詳細は [PROJECT_HISTORY.md](PROJECT_HISTORY.md)、仕様の正本は [GAME_SPEC.md](GAME_SPEC.md)。履歴中の「現在」や古い担当を再開指示に使わない。

## 現在のゲーム開発

### OPENING-001 本編冒頭と最初の怪異の制作（2026-09-19、入口単位を実装・検証）
- 担当: Astra / task 01a07fbb-3ac2-7a62-a049-19d919724625。開始 main 95753d1。PlayableDemo.unityの既存ユーザー編集は保持し、対象から除外する。
- ユーザーは「導入から初回怪異を一本の体験として仕上げ、その後M3全4夜へ」を承認。導入内容はuser-inputで「帰宅の日常を中心に描く」を選択。文章の執筆は引き続きユーザー担当。
- 工程: 接続と未保存状態確認→本編用シーンへ既存体験を複製→入口・掲示・通常帰宅の連続した導入→代表Lureの演出→関連入力・画像検証→記録と保存。新しい人物／会話／屋外全域、未承認ルールは追加しない。
- 対象: 本編冒頭シーンとEditor組立、導入・夜間遷移、Lure演出、必要な試験、ROADMAP／作者向け文書。既存デモの起動・作者文・検証シーンは保つ。具体配置・音・間はGAME-015内の試作で、人の理解・怖さの受入と分ける。
- 完了条件: 解説画面に依存せず開始・掲示確認・通常帰宅・翌夜の初回怪異へ進める。メニュー／死亡再開を壊さず、Lure判別と帰還経路を保持。import／compile／対象試験／最終Game Viewを確認する。
- 追加の明示指定: 入口の集合郵便受け805を調べるとオートロックを操作可能にし、805入力で自動ドアを開ける必須導線を制作。▲は近づいた操作対象に表示。短い独白案は採らない。入口一式を先に完成・検証し、その後に代表Lureへ進む。新しい入口には物理キーパッド・郵便受け・自動扉と作者向けInspectorを用意する。
- 成果: `Assets/Scenes/Homecoming.unity`に独立した本編冒頭。集合ポスト805を調べると扉が開き、操作盤が有効化。805入力でガラス自動扉が開く。誤番号・取消・再入力、近距離／遮蔽なしの▲、開始フェード中のポーズ、説明メニューなしの翌夜移行。翌夜の再開位置は既存ホールへ切替。詳細と作者の編集入口はOPENING_PLAN／DEMO_AUTHORING。
- 検証: PlayMode 27/27（failed/skip 0、`artifacts/tests/20260919-055658-986/playmode.xml`）。入口から翌夜Lureまでの合成Input System入力、既存デモ3怪異・死亡再開・再プレイ、既存クリック／救済回帰。操作音と日本語Inspector追加後、入口通し1/1を再検証（060401-722、100秒）。最終Game Viewは`artifacts/opening-01/input-20260919-060406/`。今回はEditMode試験は実行していない。
- Windows: 非Development build成功18.2秒、error/warning 0。`artifacts/builds/20260919-060557-702/GraduationProject.exe`、Homecomingのみ・Tests assemblyなし、READMEと素材license同梱。Computer UseでWindows版の実クリック開始・フェード後の入口を実見。OSキー送信の移動反応は確証を得られず、Windows全経路・音の聴感は未確認。経路の成功はEditor合成入力の証拠と区別する。
- 保全: PlayableDemoのSHA256は作業前後一致（4112DABE…F6F8DB56）。作者差分は未ステージ保持。EditorはHomecoming保存済みEdit Mode、Playerは評価用に起動。buildによる設定差分は補助処理で直前値へ復元済み。
- 次: 入口の本人評価（805に気づくか・▲・文字と音）を受けつつ、代表Lureの空間・光・音の変貌を制作する。今回Lureの演出自体は変更していない。出口／階段ループ、全4夜・6表現・保存は後続。Astra継続、Solへ全面引継ぎはしていない。

### MONITOR-001 通常案内のスライドショー実装（2026-09-16）
- 担当Astra / task 01a07fbb-3ac2-7a62-a049-19d919724625。対象CabinInformationDisplay・文章Inspector・関連試験。既存文を1枚目として保持、追加文リスト・表示秒数・フェード秒数を日本語Inspectorへ。ユーザー執筆のため新しい文章は創作せず、追加欄で本人が入力する。
- 通常案内を順に循環し、フェードアウト→次の文→フェードイン。怪異案内が優先し終了時に通常案内を再開。怪異の提示時間／ルールと既存シーン編集は変更しない。コンパイル、複数文／1枚／空欄／停止復帰／怪異割込みと関連実シーン画像を確認し記録保存する。
- 編集入口: テキスト編集→05_案内・怪異・階数。「最初の文言」＋「追加の文言（＋で追加）」、1枚の表示秒数（初期5）、片道フェード秒数（初期0.5）。モニター本体Inspectorでも同じ設定。初期追加0件、1枚なら常時表示。空欄は空白スライド。既存文／設定を上書きするscene移行なし。
- 検証: PlayMode2/2成功・failed/skip0（artifacts/tests/20260916-054452-045/playmode.xml）。複数文／空欄／一周／中間alpha／時間停止／怪異割込み・表示保持／Restore／0秒／disable-enable／1枚と、既存3怪異通しを確認。最終Game View 01-first・02-fade・03-secondを実見。Computer Useで編集欄と＋の存在を確認し、入口を選択したまま停止。Console error0を確認。
- 限界: Windowsビルド／OS実入力は今回は再実行せず、既存exeへは未反映。追加の文言は作者の入力待ちで、こちらで新しい掲示文を創作していない。PlayableDemoの既存ユーザー差分は未ステージ保持。



### LURE-002 到着前からの連続ダッシュ取りこぼしを修正（2026-09-16）
- 担当Astra / task 01a07fbb-3ac2-7a62-a049-19d919724625。開始main 38f0f70。対象はElevatorControllerの再閉指示と関連テスト。閉扉への押し付けは安全、開扉後の退出でReveal、既存救済・外側閉操作は維持する。
- 工程: 未保存ユーザーシーン保護→到着前からShift+Wを保持する再現テスト→最小修正→通常／連続ダッシュと救済回帰・最終画像→記録と保存。PlayableDemoの文章／音・時間調整は変更対象外。未保存内容をartifacts/lure-02/user-unsaved-before.unityに複製し保存してからテストする。
- 再現: artifacts/tests/20260916-052234-658/playmode.xmlで到着前からShift+W保持→Reveal待ちtimeout（0/1）。原因はRunBeatの再CloseDoorsが、閉扉済みでも安全センサーを評価して開き直すこと。到着前に退出でき、到着時ラッチがfalseになる。単にラッチを常時trueにする修正はせず、扉の不要な再開扉を抑える。
- 結果: 移動距離のない再閉指示では安全センサーによる再開扉を起こさず、実際の閉扉中の障害物検知は維持。Lureの既存退出／救済条件自体は変更なし。
- 検証: PlayMode 28/28、failed/skip 0（artifacts/tests/20260916-052426-846/playmode.xml）。到着前からのShift+W、到着後ダッシュ、車内観察、外側閉操作、帰還／閉扉救済・障害物再開扉を含む。before-arrival-reveal.pngを最終Game Viewとして実見。Console error 0。
- 停止: PlayableDemo保存済みEdit Mode。未保存だったユーザーシーンと現在のファイルはSHA256一致（97D4AE83…86CA10）。今回シーン変更なし、既存ユーザー差分を未ステージ保持。通常build／Windows入力は今回は再実行していないため、以前のexeにはこの修正は未反映。確認はEditor Playで行う。




### FEEDBACK-001 指摘9点を実装・検証（2026-09-15）
- 担当Astra。ユーザー指定9点：革靴足音、静かな走行／扉音と調整Inspector、扉の内側目地／水平線、到着後の間、8ボタン登録灯、右袖壁上部モニター、早期ダッシュ誘引判定、上限なし一定階数上昇。新しい怪異・依存は追加せず、既存判定と救済を保つ。階数表示は遭遇中継続し、非常停止または死亡等の遭遇終了で止める。
- 工程：実例調査→音／表示／到着→境界判定→関連入力・画像・音出力・通常build→記録保存。音色の聴感は技術検証と区別。
- 開始main c97208d。PlayableDemoにユーザーの文章／root名変更あり、artifacts/feedback-01へ元シーンとpatch保護。既存ユーザー差分は維持し、今回の変更だけをstageする。
- 操作入口: 3シーンのHierarchy「音・動作調整」。各音量、チャイム後1.4秒、怪異の階数間隔1秒をInspectorで編集可能。Play後の保存方法はDEMO_AUTHORING。足音は革靴／コンクリートの実録4種、ライセンス同梱。
- 検証: 初回PlayMode37件中34合格／3失敗。退出判定の既存外側開始ケースと新テストのStart競合を修正。デモ通しtimeoutは再実行で再現せず、失敗診断画像を追加。対象再検証29件中28合格／1失敗の後、実際の扉面へ退出境界を合わせ、最終対象26/26合格（090555-342）。デモ通しと13超え／一定間隔／停止は085853-402で合格。EditMode2/2（090719-963）、skipなし。最終状態で全37件を一括再実行はしていない。
- 実見: artifacts/feedback-01/inside-door-selected.pngで内側目地・水平線除去・8登録灯、sprint-reveal.pngでダッシュ退出後の赤い廊下。モニターは袖壁への2mm接触をテスト・画像で確認。音の波形・発火検証と人の聴感受入は別、自然さの評価は未完。
- 通常Windows build: artifacts/builds/20260915-090821-735/GraduationProject.exe。13.5秒、error/warning 0、足音／フォントlicense同梱。起動してwindow生成を確認したが、Computer Useが「foreground window did not report a process id」で取得・再接続とも失敗。新Playerの画面／実入力は未確認。前面操作を迂回せず停止。
- 停止状態: PlayableDemo保存済みEdit Mode、「音・動作調整」を選択。ユーザーの文章変更は未コミット差分として保持。今回の実装のみ保存対象。次は新しい足音・静音バランス・到着の間の実プレイ評価。Solへの全面引継ぎはしていない。



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
