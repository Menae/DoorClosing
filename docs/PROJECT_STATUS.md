# 現在地・再開情報

更新: 2026-09-19。ここには現在の担当・進捗・次の作業を置く。過去の詳細は [PROJECT_HISTORY.md](PROJECT_HISTORY.md)、仕様の正本は [GAME_SPEC.md](GAME_SPEC.md)。履歴中の「現在」や古い担当を再開指示に使わない。

## 現在のゲーム開発

### M4-01 保存・結果（2026-09-20、作業中）
- 担当Astra、同checkout。M4_SAVE_PLAN採用と自己ベスト独立保存の回答を反映。既存JSON API／標準IOと既存メニューをExtendし、外部frameworkなし。
- 範囲: 専用保存層、DemoSession／HomecomingCampaign／RunManager、作者用追加文章グループ、隔離保存試験。進行／プロフィールを分離し、一時ファイル＋直前backup・版／整合性検査・排他で誤上書きを防ぐ。新規／続き／タイトル・結果・設定を既存UIへ接続。
- 完了条件: 保存の正常／異常系、独立ベスト、累積・計時除外、入力経路の夜再開／設定／結果、最終画像、Windows終了→再起動→続き、Console／build、作者差分保護・Docs・commit/push。素材・物語・大規模UI刷新は行わない。

### DOOR-001 行先未選択の閉扉待機（2026-09-20、実装・対象検証済み）
- 担当Astra、main 65f124a。ユーザー指定: 8階未選択で「閉」を押したら、その場で閉じたまま待機する。既存の到着階再提示は維持。正常乗車時のCloseAndReopenを分離し、「開」・閉扉後8選択・安全再開扉を検証する。
- 範囲: NormalJourneyController、対象Input System試験と実シーン入力、GAME-008／Docs。新状態の追加やserialized enum値変更は不要で、乗車待機状態のまま扉を閉じる。既存の作者シーン2枚は変更・commit対象外。
- 検証: 修正前045218-082で閉扉保持の失敗を再現。修正後`artifacts/tests/20260920-045442-809/playmode.xml`、5/5成功・failed/skip0、64秒。閉扉保持・移動なし・無効ボタン・開／8選択・閉塞再開扉・正常帰宅を入力経路で確認。`artifacts/door-01/safe-20260920-045454/`で閉扉待機と到着のGame Viewを保存、閉扉画像を実見。
- Windows build: `artifacts/builds/20260920-045843-622`、非Development、13.9秒、error/warning0。この版のWindows内操作は未実施（M4完成版と併せて確認）。作者シーン2枚のSHA256は開始時と一致。自己ベストは独立保存と回答済み。

### M3-05 通常停止の追加遭遇（2026-09-19、実装・対象検証済み）
- 担当Astra、main 594930d（M3-04 commit/push済み）。GAME-008に従い、本編の通常走行で受理した非常停止1回につき約1/3、全6表現から均等に追加する。導入と正解の暴走停止は免除、停止中連打は再抽選しない。
- 方針: HomecomingCampaignに実行中だけの追加待ち列、RunManagerで通常3件の手前へ挿入、NormalJourney／FSMの通常走行で短い停止と再開。通常ノルマを消費せず、追加も通常のDiagnosisから開始する。既存デモは本編componentなしで維持。新依存・保存形式なし。
- 範囲／完了条件: 停止受付・抽選・追加順序・終了／死亡時の破棄、Inspectorの実行状況、対象入力試験（導入免除、停止中連打、再開後再受理、6候補、正解停止免除、追加を経て通常3件と帰宅）、既存通常走行の回帰、Windows build／画像・Docs・保存。両シーンの作者差分は保持する。
- 試験履歴: 092204-172は抽選・既存正常走行2件成功、通しは既存▲の2時点測定が揺れの同位相を拾い失敗。0.6秒窓内の最大変位へ修正（動作と閾値は維持）。092337-016は受理成功・固定seedの当選期待が共有乱数の消費で外れた。怪異乱数をSystem.Randomへ分離し、試験fixtureのみでseed固定。強制正解／遭遇開始呼出しは使用していない。
- 結果: `artifacts/tests/20260919-092707-461/playmode.xml`、3/3成功・failed/skip0・152.8秒。6000回の抽選分布・全6候補・元asset保全と定義破棄、固定夜／最終夜の抽選回帰、Homecoming通常入力で導入停止免除→翌夜の当選／外れ→追加の濡れ→遭遇間停止→追加の声→通常3件→帰宅→次夜。停止中連打／保持とポーズ、暴走正解の抽選免除、追加の非重複を確認。既存M1正常停止試験は092204-172で成功。全回帰一括は未実施。
- 証拠: `artifacts/m3-05/safe-20260919-092717/`。追加遭遇時の最終Game Viewは側面操作盤を向いたままの記録で、濡れの見た目の評価には使わない。濡れの描画自体の証拠はM3-02／01参照。通常入力の通し証拠はEditorであり、Windowsの全行程とは区別する。
- Windows: `artifacts/builds/20260919-093133-664/GraduationProject.exe`、非Development・8.87秒・build error/warning0・Tests assemblyなし。Computer Useで開始画面→実クリック→入口を確認。`artifacts/m3-05/Player-startup.log` に例外・shader errorなし、既存D3D12 info queue診断／影atlas縮小の記録あり。Windows全4夜・OS移動入力は未検証。
- 保全: build直前設定5ファイルのhash一致復元。PlayableDemoは開始時SHA256一致、Homecomingの残差分は作者の音量0.518／声1件とUnity空白のみ。Editorは保存済みEdit Mode。通常停止の1秒は既存Inspector値を共用。
- 次: M3主要実装は揃った。技術確認を怖さ・面白さの受入とは扱わない。M4保存／続き／累積記録へ進むため、未決の保存方式を `M4_SAVE_PLAN.md` に具体化しM4-SAVE-001で確認する。文章の細かな調整はユーザー指定どおり後回し。

### M3-04 全4夜・最終夜抽選（2026-09-19、実装・対象検証済み）
- 担当Astra、main 797acc6。M3-03をcommit/push済み。6表現を既定の導入＋3/3/3へ接続する。Homecomingに任意の本編進行componentを追加し、既存デモ／単独怪異比較を維持する。
- 範囲: 夜ごとの定義・最終夜の系統別抽選と順序shuffle、現在夜の死亡再試行、後半猶予90%の試作、暗転で次夜へ。UIは既存開始／ポーズ／帰宅終了を再利用し、途中の説明画面は増やさない。保存形式・新依存なし。M4保存／結果集計、通常停止の追加遭遇は別単位。
- 方針: RunManagerは1夜内、HomecomingCampaignは夜の構成、DemoSessionは既存UI／暗転。定義assetを直接変更せず、短縮値は実行用コピーで破棄する。全4夜と作者の単独比較はInspectorで明示切替。
- 検証: 固定夜各3件・各系統1回、最終夜の同条件再現・死亡時再抽選、元asset非変更／コピー破棄、通常入力で全4夜帰宅、後半の救済、ポーズと既存デモ回帰。今回のシーン追加hunkと作者の声1件／音量調整を分離して保存。
- 結果: 085942-265の抽選／定義保全・既存デモ2件は成功。全4夜はUnity既定の180秒制限で中断したため当該試験のみ600秒へ延長（ゲーム内の所要時間は変更なし）。`artifacts/tests/20260919-090730-599/playmode.xml` は1/1成功・failed/skip0・312.7秒。合成Input System→Raycast→clickで全4夜帰宅、最終夜の死亡→再乗車・再抽選、後半の徒歩帰還／暴走期限超過救済を確認。
- 画像: `artifacts/m3-04/safe-20260919-090735/` の各夜帰宅とfour-nights-complete。最終Game Viewの終了画面を実見し、途中のデモ終了表示がないことを検証。全回帰一括・怖さの受入は未実施。
- Windows: `artifacts/builds/20260919-091347-408/GraduationProject.exe`、非Development・11.85秒・error/warning0・Tests assemblyなし・README／licenses同梱。設定5ファイルをbuild直前値へ一致復元。PlayableDemoの開始時SHA256一致。Homecomingの作者の声1件／音量0.518は保持し、追加componentだけをcommit対象とする。
- Windows実見: Computer Useの実クリックではじめる→入口表示を確認。Windows全4夜・OS移動入力は未検証で、全4夜の通し証拠はEditor。次はGAME-008の通常停止による追加遭遇。

### M3-03 かご内の空間異常（2026-09-19、実装・対象検証済み）
- 担当Astra、main 41b15c7。ユーザーは概ね満足と評価し、文章の微調整を後回しにして継続を指示。次の既定表現をGAME-015内で試作する。
- 方針: 既存Hijackの判定・階上昇・走行音をExtendし、天井と照明が上へ遠ざかる専用PrefabをBuild。上部壁を伸ばし、床・操作盤・移動範囲・カメラは固定。全面的な空間変形shader／外部frameworkは採らず、新依存なし。既存UnityのTransform／Rendererを使用する狭い実装比較で、OSS移植は不要。
- 対象: HijackAnomalyの任意演出接続、空間表現・Prefab／定義・比較Inspector、対象入力試験とDocs。診断時4m・悪化時さらに2m程度の伸長を試し、成功・死亡・停止で元へ復元する。具体値や怖さは正式採用と分離する。
- 完了条件: 閉扉中の天井伸長と階数／音の合図、非常停止成功、期限超過→救済、二度目誤操作／猶予超過死亡、ポーズと復元、既存Hijack回帰。最終Game View／Windows実見、試すシーン・手順の明示、記録・commit/push。
- 保全: 作者の両シーン差分は除外。Homecomingの未保存内容を `artifacts/m3-03/user-unsaved-before.unity` に保全してから検証する。現在の遭遇リスト変更も作者の比較設定として保持。
- 結果: `artifacts/tests/20260919-084459-992/playmode.xml`、PlayMode3/3・failed/skip0・157秒。通常帰宅→濡れ→声→空間異常4回で直接停止／期限超過から救済／閉から救済／二度目閉で死亡、元天井の復元・固定操作盤・ポーズを合成Input Systemで確認。既存Hijackの13超え／一定ペース／音停止と猶予超過死亡も成功。全回帰一括は未実施。
- 実画像: `artifacts/m3-03/safe-20260919-084507/` の11-normal-ceiling／12-spatial-diagnosis／13-spatial-grace-1（最終Game View 750×422）を実見。元天井→4m伸長→6m伸長を比較。正確な間接照明・怖さは未受入。
- Windows: `artifacts/builds/20260919-084859-219/GraduationProject.exe` は空間異常1件の比較版。非Development・15.2秒・error/warning0・Tests assemblyなし・README／licenses同梱。Computer Useで実クリック開始とフェード後の入口を1920幅で実見。Windowsの怪異到達・OS移動入力は未検証。通し入力はEditorの証拠。
- 復元: 作者の単独「声」リスト・音量0.518を保持し、build前シーンとの意味差分0。PlayableDemoは開始時SHA256と一致。build設定5ファイル復元一致。テストConsoleの既知warningはplateText未使用・Hijack検証fixtureのRenderer未指定、errorなし。
- 次: 6表現の実装が揃ったため、全4夜の進行／最終夜抽選へ接続する。M3全体はまだ未完。確認メニューと試す手順はDEMO_AUTHORING。

### M3-02 開扉前の濡れ表示／外からの声（2026-09-19、実装・対象検証済み）
- 担当Astra、main 5ba0864。濡れが開扉後に出現するという本人報告を再現・修正し、既定の次表現「外から助けを求める声」を制作する。
- 範囲: Lureの見た目を開扉前に準備、入力受付・退出検出・猶予は維持。声は独立した音声Prefabと既存Provocation判定を組み合わせ、音源・音量・間隔をInspectorで編集。新規のゲーム規則・人物表示は追加しない。
- 検証: 開扉途中の表示を先に失敗再現→修正→車内観察／到着前ダッシュ／帰還。声は閉扉走行・無操作成功・誤操作後の無操作救済・二度目誤操作・ポーズ／終了時停止を確認。音色の本人評価は技術確認と分離。関連Docsとcommit／pushまで行う。
- 保全: 両シーンの既存作者差分を除外。開始時Homecoming未保存は最初のLureを濡れへ切替した差分のみで、`artifacts/m3-02/user-unsaved-before.unity`へ保全。作者の比較設定として保持する。
- 結果: 開扉前表示の試験で失敗再現（`artifacts/tests/20260919-080334-972/playmode.xml`、0/1）→Lureの準備を開扉前へ分離。濡れ帰還＋到着前ダッシュ2/2成功（080704-653、146秒）。声は仮WAV2本・独立Prefab・日本語Inspectorを追加し、通常帰宅から濡れ→声3回（無操作／救済／二度目誤操作死亡）まで1/1成功（081242-032、133秒）。修正後failed/skip0。合成Input System→Raycast→clickで、ポーズ中の声停止・終了後の音源破棄も確認。全回帰／EditMode一括は未実施。
- 実画像: 最終Game View 750×422の `artifacts/m3-01/recovery-20260919-080749/07b-wet-during-opening.png` で開き始めの隙間から既に濡れている床を確認。`artifacts/m3-02/safe-20260919-081250/10-outside-voice-closed-door.png` で声の際の閉扉・通常モニター・不要なパネルなしを確認。音声は再生位置の進行を検証したが聴感は未確認、自然さ・怖さの本人評価も未了。
- Windows比較版: `artifacts/builds/20260919-082009-538/GraduationProject.exe`。最初は作者設定の濡れ、2番目を外からの声とした比較用。非Development、13.3秒、build error/warning0、Tests assemblyなし。Homecoming専用README・音声creditを含むlicenses同梱。既存build要求のdelayCallが未実行だったため、その待機callbackだけを一度dispatchして完了。重複buildなし。
- 確認の限界: 今回のtool一覧にはWindows pluginのnode_repl入口がなく、ネイティブComputer Use／Windows版の操作検証は実施していない。cua_replだけを見た権限不可判定ではない。通し入力・画像の証拠はEditor。仮音声制作のローカルVOICEVOX helperは停止済み。
- 保全／停止: build前コピー `artifacts/m3-02/homecoming-before-voice-build.unity` と復元後の意味差分0。2番目だけ設備放送へ復元し、作者の最初の濡れ選択・設備音量0.518を保持。PlayableDemo SHA256は開始時4112DABE…F6F8DB56と一致。両シーンはコミット対象外。build設定5ファイルは直前値へ一致復元。Editor保存済みEdit Mode、声Prefabを選択。Console error/warning0。
- 次: Astra継続。6表現中の残り「かご内の空間異常」を制作し、その後に全4夜・最終夜抽選・追加遭遇へ接続する。現段階は5表現の技術実装で、M3完了や怖さの正式受入ではない。

### M3-01 誘引「濡れた廊下」（2026-09-19、実装・対象検証済み）
- 担当Astra、main e0799cd。音はユーザーが一旦及第点とし次工程を指示。仮台詞・仮音声も今回だけuser-inputで委任、正式文章の執筆方針は保持。
- 最初の単位: 誘引の別表現「濡れた廊下」。既存BeatDefinition／Lure判定／減光・帰還を再利用し、柱と別のPrefab・Materialへ分ける。通常床や歩行ルールは変更しない。作者がInspectorから比較・調整できる入口を整える。
- 検証: 車内から濡れを識別できる最終Game View、車内待機の安全、退出→徒歩帰還＋閉、終了後の表面／照明／音の復元、正常廊下との比較。新依存・保存形式なし。音声・空間異常と全4夜進行は後続の実装単位。
- 保全: 開始時はPlayableDemoに既存作者差分。Homecomingに未保存の設備音量0.12→0.518のみを確認し、`artifacts/m3-01/user-unsaved-before.unity`へコピー保全。作者調整として保持し、自動コミットへ混ぜない。
- 進捗: 濡れPrefab・専用shader・室内反射・比較Inspectorを実装。PlayMode対象2/2成功（073659-784）、反射修正後の帰還ケース1/1成功（074249-508）。反射ベイク時にURPが追加するlight metadataを後処理で取り除き、再生成前後のシーン意味差分は作者音量のみ、Cubemap SHA256も一致。
- 成果: 不規則な濡れ・細かい水面変化・室内照明の反射を既存廊下へ追加。車内から判別でき、Collider・歩行速度・正解・猶予を変更しない。専用Materialの日本語項目、怪異比較Inspector、配置変更時の反射再生成メニューを追加。通常初回は柱、濡れは後続夜用の別asset。
- 試験履歴: 初回2件とも失敗（`artifacts/tests/20260919-072838-373/playmode.xml`）。旧試験の音量0.12固定を作者設定値との一致へ変更し、到着待ちをゲーム時間35秒＋実時間60秒watchdogへ修正（旧実時間32秒は通常所要約30秒に対し描画停止の余裕不足）。ゲームの成功条件・作者音量は変更しない。再実行2/2成功・failed/skip0（073659-784、220秒）。最終反射修正後の救済ケース1/1成功・failed/skip0（074249-508、110秒）。全件回帰・EditMode一括は今回未実施。
- 確認範囲: 通常帰宅→翌夜、6秒車内観察の安全、退出後の減光、ポーズ停止、徒歩帰還＋閉、次の遭遇、照明／音／水面／反射probeの復元。合成Input System→Raycast→clickで実施。`artifacts/m3-01/recovery-20260919-074258/`の最終Game View（750×422）で水面と通常床・減光後を実見。初回shader仮表示と空の青い反射は修正済み。
- Windows: `artifacts/builds/20260919-074650-058/GraduationProject.exe` は最初の誘引だけを濡れにした比較用。非Development・10.65秒・build error/warning0・Tests assemblyなし・README／licenses同梱。Computer Useで実クリック開始→入口を実見（1920幅）、`artifacts/m3-01/windows-entrance.png`。Editorでも比較ボタンの実クリックと定義切替を確認し `inspector.png` 保存。Windows全経路・OS移動入力・濡れ到達は未検証。通しの証拠はEditor。
- 保全／停止: 比較build後、Homecomingの最初の定義を柱へ復元。直前コピーとの意味差分0。現在のHomecoming差分は作者の設備音量0.518のみ（Unity保存時の空白差分を除く）、PlayableDemoは開始時SHA256 4112DABE…F6F8DB56のまま。両シーンはコミット対象外。Editorは保存済みEdit Mode、Windows比較版を起動したまま。
- 既知／限界: 室内反射は128pxの事前生成で、減光後の鏡像は正確には更新しない。反射再生成後のCubemap SHA256一致とシーン保全を確認。Player起動ログは既存D3D12 info queue診断／URP影atlas縮小warningのみ、例外・shader errorなし。怖さ・初見判別・音の正式受入は本人評価待ち。仮音声は委任を記録したが素材未作成。
- 次: Astra継続。既定のProvocation「外から助けを求める声」、Hijack「かごの空間異常」、その後に全4夜・最終夜抽選・通常停止の追加遭遇を実装。6表現・本編完走の完成扱いにはしない。

### OPENING-003 操作盤縮小と代表Lureの演出（2026-09-19、実装・関連検証済み）
- 担当Astra、main ef3bb33。PlayableDemoの作者差分は除外。ユーザーは入口の他の改善を好評価、操作盤だけ大きすぎると指定し開発継続を指示。
- 範囲／工程: 操作盤を幅205×高さ440mmへ縮小し壁との接触を保つ→本編HomecomingだけにLure演出componentを接続→入力／救済／状態復元と最終画像→Docs・保存。
- 演出試作: 日常の廊下との差を柱で判別し、誤降車後に奥から照明が弱まり機械的な低音が立ち上がる。手前の帰還経路と操作盤は保持。正解操作・期限・移動距離・新怪異・物語文は変更しない（GAME-015／OPENING-001）。既存デモの色演出は互換経路として維持。
- 保守性: 既存のProvocation表示接続と同じく、状態機械はタイミング、シーンcomponentは参照・演出値・復元を担当。新framework・新依存を導入せず、人がInspectorから調整できるようにする。
- 完了条件: 小型盤の実Raycastクリックで805解錠。Lureの車内観察が安全、誤降車の変化と徒歩帰還＋閉で救済、演出後に元の照明・音へ戻る。通常シーン・作者編集保持。技術合格と恐怖の受入は別。
- 成果: 幅205×高さ440mm・厚さ32.5mmの操作盤を壁に密着。入口操作Inspectorに配置選択ボタン。本編のLureは奥→中央2灯の減光（遅れ0.35秒／0.65秒で6%）と設備音（0.12／1.2秒fade）に置換し、手前1灯・柱の材質・判定は保持。Hierarchy最上部 `演出調整_誘引の廊下` に日本語Inspector。参照・演出・復元を専用component、受付と期限を既存FSMに分離。編集方法はDEMO_AUTHORING。
- 検証: import／compile成功。最初の対象PlayModeは3成功・1失敗・skip0（`artifacts/tests/20260919-065748-797/playmode.xml`、310秒）。新しい救済試験がReveal中に早押ししていたため、既存仕様のGrace受付開始を待つよう試験を修正。対象だけ再実行1/1成功・failed/skip0（`artifacts/tests/20260919-070346-893/playmode.xml`、110秒）。ゲームの期待結果は緩和していない。
- 成功範囲: 小型キーの誤入力・取消・805解錠、通常帰宅と翌夜、6秒車内観察で演出なし、誤降車後の奥→手前の減光、ポーズ中の停止、帰路の光を保持した徒歩帰還＋閉、次の遭遇へ進行・光と音の復元。既存M2の到着前ダッシュと救済も成功。合成Input System→Raycast→clickで実施。全件回帰・EditModeは今回未実施。
- 実画像: `artifacts/opening-03/recovery-20260919-070355/` の最終Game View（763×429）で操作盤・診断時／変貌時を比較。Windows版は `artifacts/builds/20260919-070620-792/GraduationProject.exe`、非Development・11.7秒・build error/warning0・Tests assemblyなし、README／licenses同梱。Computer Useで実クリック開始とフェード後の入口（1920幅）を確認し `artifacts/opening-03/windows-entrance.png` 保存。日本語Inspectorも実見。
- 限界／既知: Windows全経路・音の聴感・怖さは未評価。Player.logには既存D3D12 info queue取得診断とURP shadow atlas縮小warning。Editorの既存Lure plateText未接続warningは表札を使わない構成による。再試験後Console error0。CUのEditor前面化は一度timeoutしたが再観察でbuild完了を確認、重複要求なし。
- 保全／次: Homecomingの既存object削除0、意味変更は操作盤Transform・FSM参照・SceneRootsのみ、演出object追加6。既存PlayableDemoのSHA256は開始前と同じ4112DABE…F6F8DB56で未ステージ保持。Editorは保存済みEdit Mode、演出調整を選択、Windows版は起動したまま。Astra継続。代表Lureの怖さ・音と早押しの体感評価を受け、その後に既定の残り表現／M3へ。技術合格だけで本編完成とはしない。

### OPENING-002 近距離マーカー・集合ポストの改善（2026-09-19、実装・関連検証済み）
- 担当: Astra / task 01a07fbb-3ac2-7a62-a049-19d919724625。main b14b85bから。既存PlayableDemo.unityの作者差分は対象外。
- 明示決定: user-inputで1階共用入口、2〜10階各6戸、計54戸。805を調べる必須導線は維持する。
- 工程／範囲: ゲームUIとメーカー実例調査→Homecomingの郵便受けを実寸54戸へ更新→▲の微小浮遊・フェード・注視強調・密集抑制と調整Inspector→入口通し入力試験・最終画像→記録・commit/push。
- 期待結果: 部屋数に整合する薄型集合ポスト、読み取れる部屋番号、操作を邪魔しない滑らかな目印。既存入力距離／遮蔽／クリックと翌夜移行を維持。表現の具体値は委任内試作で、本人の見た目・理解の受入とは分ける。
- 実装: 6列×9段の54戸、薄型金属扉・投函口・個別番号・丸い錠・端部の見切りと壁付け照明。▲は0.18秒フェード、2.4秒周期・2.5px浮遊、注視1.25倍。ポストでは面内に収め、テンキーは1個に集約。Playerの日本語Inspectorで調整可能。旧9個は非表示で保存。
- 検証: 入口通しPlayMode1/1（062756-487、102秒）成功後、実画面でポスト▲の段違いと金属の暗潰れを修正。最終版1/1（`artifacts/tests/20260919-063240-397/playmode.xml`、102秒、failed/skip0）。54戸の一意番号・805前の入力不可・誤番号／取消／805解錠・扉通行・▲の浮遊／注視／ポーズ非表示／完了後非表示・テンキー集約・翌夜Lureまで合成Input Systemで確認。全44件の一括回帰ではない。
- 画像: `artifacts/opening-02/input-20260919-063244/` の最終Game Viewを実見（750×422）。最後に見切り裏の固定レール・照明ブラケットを追加し、壁との接続を補った。衝突なしの支持金具追加後はbuildとWindows画像を確認し、通し入力の再実行はしていない。
- Windows: `artifacts/builds/20260919-063703-280/GraduationProject.exe`、非Development・6.5秒・build error/warning0・Tests assemblyなし、README／licenses同梱。Computer Useで実クリック開始、フェード後の入口を1920幅のPlayerで実見、`artifacts/opening-02/windows-entrance.png`。Windowsでの全経路・OS移動入力は今回未検証。起動ログにはD3D12 info queue取得診断と既存URPのshadow atlas縮小warningあり（描画継続）。Editorでも後者を観測。性能目標の合格とはしない。
- 保全: PlayableDemoのSHA256は開始前と同じ4112DABE…F6F8DB56。既存Homecomingのserialized object削除0、意味変更は入口の参照／テキスト登録／旧配置非表示／Player表示設定／親の子リストに限定。EditorはHomecoming保存済みEdit Mode、Windows版は評価用に起動したまま。本人の好み・初見理解は未受入。次は入口評価と既定の代表Lure演出、Astra継続。

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
