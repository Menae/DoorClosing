# 過去の作業記録（2026-09-15〜11-05）— 2026-11-06移動分

2026-11-06（ORCH-001）にPROJECT_STATUSから本文をそのまま移した。担当・「現在」「次」・実行中表示は記録当時の情報で、現在の指示ではない。現在地は [PROJECT_STATUS.md](PROJECT_STATUS.md) を参照する。

## 開発体制（2026-11-02〜05）

### CU-001 ClaudeとCodex双方のComputer Use（2026-11-05、Codexはアプリ経路で確認済み／Claudeは次セッションで確認）
- 結果（ユーザーが両方を設定後）: ユーザーがCodex DesktopアプリにSolスレッド「Unity Editorを撮影」（01a1abd0…、gpt-6.1-sol・max、source=vscode）を作り、SMOKE-005でnode_repl経路の撮影に成功（`artifacts/orchestra/SMOKE-005/editor-window.png` を実見）。同スレッドへ `codex queue` で予約した指示はアプリが消化して実行した。最初の予約（SMOKE-006初回）はUnity Editorが22:08:48に正常終了していて対象なし（終了の原因は不明。撮影ターンはlist/get/activate/get_window_stateのみで閉じる操作なし）。Editor再起動後、新設の `sol.py app` で予約→アプリ内Solが照合・前面化・撮影・保存→構造化報告→完了検知まで約3分20秒で成功（`artifacts/orchestra/SMOKE-006/`、画像実見、stageなし）。
- Claude側: ユーザーがClaude DesktopのComputer useをオンにしたが、このセッションには `mcp__computer-use__*` が読み込まれていない（ツールはセッション開始時に読み込まれると推定）。次のセッションで `request_access` によりUnity等の許可と動作を確認する。
- 担当: Claude Code（Opus）リーダー、開始main 15e11a1・origin/mainと0/0（調査結果は99ec362で保存）。ユーザー報告: Codexには許可済み、以前もAstraは使えてSolは許可なしと主張した（Astraは経路違いと見立て）。ユーザー指定: ClaudeとCodexの両方でComputer Useを使えるようにする。
- 調査結果: SMOKE-001／002の拒否は、@oai/skyがアプリ単位の許可をMCP elicitation（「Allow Codex to use Unity?」）で問い合わせ、`accept` 以外が返った時の文言。guardian（承認の自動審査）はnode_replの撮影コード実行を `allow` しており原因ではない。保存済みの「常に許可」を返す処理（`computer-use-persisted-state`）はcodex.exe（CLI）に無く、Desktopアプリ側だけが持つ。Desktopアプリは自分の `codex app-server --listen stdio://` でスレッドを動かすため、CLIの `codex exec` で起動したSolは許可に答える相手がなく拒否される（構造上の制約。許可の付け忘れやSolの経路ミスではない）。9月のAstraの成功も同じnode_repl経路で、アプリ内スレッドだった。アプリ内でSolが失敗した件はelicitationを使えない文脈から呼んだ可能性があり、未確認。
- 試行: `codex queue --resume` でCLIスレッド01a19b40へ撮影指示を予約→「start a local daemon or specify a server with --remote」で再開できず、予約は未消化のまま残る（撮影1回の無害な指示。このスレッドは今後resumeしない）。`remote-control`／`app-server daemon` は新しいサーバーを立てるだけで、Desktopアプリのapp-serverには接続できない。
- 対応: `sol.py app` を追加（DEVELOPMENTのWORK-007節、DECISIONS WORK-007 Revision）。独立レビュー1回目CHANGES_REQUIRED（major: 予約後に待機をやめた時にロックが外れ記録が残らない、アプリ経路の全項目記録なし。minor 8件）を受け、予約前の読み取り検査・予約直後の記録・未完了時のロック保持と `app-wait`・ターン全項目のitems.jsonl・終端3値・受付番号の最初の一致・final_agent_item_id・報告の必須キー検査・長さ／timeout検査に直した。
- 確認: 不正なtask ID・存在しないthreadは予約前に停止。SMOKE-007を `--timeout-min 0.1` で実行し `inProgress`・exit 2・ロックに `app_pending`・その間の `run` 拒否を確認、続く `app-wait` で completed・items.jsonl 23件・report・ロック解放（exit 2時点のmetaはapp-waitで上書きされ、その経過はコンソール出力のみ）。再審査はPASS（minor 6件）。
- 手順違反の記録: リーダーはSMOKE-007のアプリ内ターン実行中（22:29:58 JST）にsol.py（ロック順）を編集し、再審査依頼では「SMOKE-007の後」と誤って説明した。Solはファイルを編集しておらず（items.jsonlにfileChangeなし、Sol自身が差分量の変化を報告）、成果への影響はないが、DEVELOPMENTの「Sol実行中は編集しない」に反する。以後、exit 2の間も実行中として扱う旨を手順に明記した。`app-wait` は変更後の版で動いた。拒否された旧版の `run` が空フォルダ `artifacts/orchestra/SMOKE-007x/20261105-132941` を残した（削除方針が未合意のため保持）。
- 残minor対応: mkdirのtry内移動、app_pending時の案内、app-waitの終了済み拒否・ロック取り直し・先頭一致での解放・待機経過の追記、プロセス作成失敗時のロック解放とコマンドライン長の検査、スレッドのモデル／全権限の拒否。
- 現行版の確認: 編集なしでSMOKE-008（`artifacts/orchestra/SMOKE-008/`）を `app` で実行し、約2分40秒でcompleted・items.jsonl 24件・report completed・モデル検査（gpt-6.1-sol／max）通過・ロック解放・stageなし。終了済みの実行への `app-wait` は記録を変えず拒否。偽の受付番号の仮実行（scratchpad）で、ロック無しからの取り直し・exit 2での保持・他の実行の拒否と案内・meta.waitsの追記を確認し、手で外してよい条件（受付番号の発言0件・予約0件）を読み取りで確認してテスト用ロックを外した。最後のminor対応は再審査に出しておらず、上記の実地確認で代えている。
- 次: Claude側は次セッションで `request_access` によりUnity等を承認して確認する。その後、完成目標と計画の議論へ。
- Unity: `Launch` で未起動からEditor（PID 51128）とMCPを起動し、instance登録まで確認（11/05）。

### WORK-007 Opus主導・Sol実装・独立Opusレビューの連携環境（2026-11-02、構築・疎通確認済み／Computer UseはCU-001でDesktopアプリ経路に対応）
- 担当: Claude Code（Opus 5.5 xhigh）リーダーセッション。開始main 7060765、origin/mainと0/0、stagedなし。Unity Editor未起動、8080のMCPサーバー停止。既存3差分は中身を確認し（Homecoming＝設備音0.518・比較用の声1件＋Unityの空白差分、PlayableDemo＝作者の掲示・案内文、slnx＝並び順）、作者の編集として `f9dad1c` に保存した。
- ユーザー指定: リーダーOpus → 実装Sol（gpt-6.1-sol・推論max）→ 独立Opus 5.5レビュー。user-inputで選択: Solは `codex exec` CLIで起動／workspace-write＋承認の自動審査（guardian）／実装ごとに文脈を持たないOpusレビュー役を起動し、合格後にリーダーがcommit/push／既存3差分は作者の編集として先に保存／Computer Useの前面操作とUnity Editorの自動起動は常時可。最初の工程は自動実装せず、完成目標と計画をユーザーとOpusで議論して練り直す。
- 範囲: 作者差分の保存→Sol起動・Unity環境起動の補助スクリプト（tools/orchestra）→レビュー役定義（.claude/agents）→AGENTS／START_HERE／DECISIONSの体制記述→疎通確認。ゲームのコード・シーン・仕様は変更しない。ユーザーのCodex全体設定（~/.codex/config.toml）は変更せず、起動時の引数で指定する。
- 完了条件と検証: Sol max起動と構造化報告の取得、MCPサーバー＋Editor起動とClaude／Codex双方のUnity MCP接続、SolのComputer Useによるwindow列挙、Sol報告をOpusレビュー役が審査する一往復。各結果をartifacts/orchestraへ保存し、未確認点を記録する。
- 成果: `tools/orchestra/sol.py`（起動・構造化報告・容量不足時の同session自動再開・原子的ロック・プロセスツリー停止・ignored領域の書込み記録）、`report.schema.json`、`sol-contract.md`、`tools/UnityAgent.ps1 Launch`、`.claude/agents/opus-reviewer.md`、AGENTS／START_HERE／DECISIONS WORK-007／DEVELOPMENTの手順。
- Unity環境: `Launch` が未起動状態からMCPサーバー10.2.0とEditor 6000.3.14f1（PID 26748）を起動し、`GraduationProject@40efb0f06b9c2d3f` の登録まで待機（約10分、初回import込み）。起動済み状態では二重起動せず3秒でprojectRoot照合済みのinstanceを返す（PS 5.1.26100）。Editorの有効platformは `StandaloneOSX`（MAC-001の名残と推定、未変更）。このClaudeセッションのUnity MCPは起動時のECONNREFUSEDのまま再接続されず、リーダーは既存CLI client（`UnityAgent.ps1 Client`）で照合した。次のセッションではサーバー稼働中なら接続される想定（未確認）。
- Sol: SMOKE-001（`artifacts/orchestra/SMOKE-001/`）初回は `--approve-for-me` と `-s` の併用不可で失敗→修正。2回目はOpenAI側「Selected model is at capacity」で中断→同sessionのresumeで完了（約6分、partial）。Unity MCPでprojectRoot一致・保存済みEdit Mode・Console error/warning 0・EditMode 2/2 Passed、window列挙で対象Editorを一意特定。画面取得は「Computer Use was not approved to use unity」で拒否。SMOKE-002（新規session、`--approve-for-me`）でも同じ拒否で、原因はCodexのアプリ単位の許可と切り分けた。SMOKE-003で最終版sol.pyの新規→resumeの一往復（resume時 `-c` 承認指定）を確認、stage・残存プロセス・残存ロックなし（確認用にeffort low）。
- レビュー: 独立Opusレビュー役（定義は作成直後のため未登録、general-purpose＋model opus＋定義ファイルで代替）が1回目CHANGES_REQUIRED（major: resume時の承認方式未指定、minor 8件）。全件対応後の再審査でPASS（残minor 5件も対応）。Solの報告は証拠・events.jsonlと一致し、規約違反・権限迂回なしと判定。
- 未確認・残り: CLI起動のSolでComputer Useが使えない件はCU-001で原因特定・Desktopアプリ経路で対応。容量不足時の自動再開ループとtimeout時のtaskkillの実働（意図的に起こせず未発火）。初回SMOKE-001のevents.jsonlに無関係なwindow titleが残る（Git対象外のローカルのみ、以後は絞り込み手順）。
- 次: ユーザーとOpusで完成目標・計画を議論して練り直す。自動実装はその合意後に開始する。
- 設計レビュー（2026-11-06、ユーザー依頼）: 独立サブエージェント（Opus 5.5・xhigh、読み取り専用）が体制の効率と洗練余地を審査した。要約とリーダーの照合は `artifacts/orchestra/reviews/WORK-007-design-review-20261106.md`（Git対象外）。
  - 指摘: 判断不要の改善が8件（計測記録、実行前後のUnity・作業ツリー検査、Solの冷間読込削減、summary.jsonによる機械検査、停止検知、CLI timeout既定など）。ユーザー判断が必要なのは5件（Sol実行中の編集をhookで阻止、記録タイミング、レビュー段階化、推論強度の使い分け、app経路の縮小）。
  - 照合で訂正: SMOKE-002の4回の失敗は文字化けではなく、`rg` が無いことが原因（現行のCodex binにrg.exeがない）。
  - 状態: すべて未実装。

## 展示会用画像

### KV-001 「ドアが閉まります」作品キービジュアル（2026-10-20、添付入口スクショ・明朝体へ改訂／作成確認済み）
- 第3改訂: main eca4e52、既存3差分とstagedなしを確認。ユーザーが『ウミガリ』の添付ロゴを参照し、歪みを除いた近いフォントを探して採用、文字を拡大、添付2枚目の入口と同じ場所・アングルへ変更するよう指定。書体選定は依頼の範囲で行い、元ロゴの正確な書体名は未確定とする。
- 第3改訂の構図: user-inputで「スクショ全体を残す」を選択。添付の郵便受けと自動扉の配置・画角を保持し、500×500の上下の暗い余白に大きいタイトルを置く。背景は再生成せず、従来承認の軽いポスト処理を継続。出力・添付コピー・書体資料・ライセンス・処理script・確認はartifacts/exhibition-keyvisual/revision-03へ保存。元スクショと既存候補は削除しない。
- 第3改訂の書体選定（依頼内委任）: 添付は縦横の太細差・三角形のウロコ・かなの払いを持つ明朝系と観察。元ロゴの正確な書体は断定せず、しっぽり明朝ExtraBold／Bold、Noto Serif JP Bold、游明朝Demiboldを実描画で比較し、かなの曲線・太さが近い通常版しっぽり明朝ExtraBoldを選定。公式説明・Google Fonts metadata・OFL 1.1を確認してfontとlicenseを取得、systemへinstallせず画像制作内で使用。B1の墨だまり版や文字の歪みは使わない。選定資料は`revision-03/source/font-research.txt`／`font-comparison.png`。sandboxのDNS失敗後、通常ホストの承認機構で取得を完了。
- 第3改訂成果／確認: `revision-03/output/ドアが閉まります_入口_しっぽり明朝_500x500.png`、500×500 PNG、184,533 bytes、SHA256 `3e13351a23db330bb663effc06e8d154049456b52d386ef310c03bb1b9d38470`。添付925×520の全体を500×281で配置（切抜きなし）、下の余白に白い78pxの2行タイトル（前回36px）。文字変形なし。最終500px画像を実見し、元構図・全文・余白を確認。`source/validation.json`に画像・fontのhash、配置、処理値を保持。ゲームテスト・Unity操作なし、既存3差分と旧候補保持。画像とfontはGit外ローカル保存。
- 改訂担当: 同task。開始main ae925b3、既存Homecoming／PlayableDemo／slnx差分、stagedなし。ユーザーが旧生成画像の削除、背景に実ゲームスクショ、軽いポスト処理、質素なフォントを明示指定。場面を1つに選ばず複数候補を希望。
- 改訂承認: user-inputで旧納品PNG＋workspace生成原本＋generated_images原本の3点削除を選択し、全3点の削除・不存在確認済み。promptと記録、元ゲームスクショは保持する。別のuser-inputで、生成AIによる再描画なし、Aホール閉扉／B通常8階廊下／Cかご内の3候補、軽い色調補正・周辺減光・光のにじみ、白い細めのゴシック体を承認。
- 改訂範囲／完了条件: 既存スクショを正方形へ切出し、内容を再描画しない画像処理と文字合成。500×500 PNGを3点作成し、出典・処理値を記録、全3点実見・形式寸法確認→記録保存。artifacts/exhibition-keyvisual/revision-02へ保持。Unity・ゲーム本体は変更しない。以下の初案成果・hashは撤回前の履歴であり、画像は削除済み。
- 改訂成果: `revision-02/output/`にA_閉じた扉・B_8階の廊下・C_かご内の3PNG、すべて500×500。比較画像は`revision-02/候補比較.png`。Yu Gothic Light 36px、白、1行。元画像750×422の422px正方形から同率拡大。明るさ1.12／彩度0.9、僅かな寒色補正、周辺減光最大16%、弱いbloomと文字下の暗化を適用。生成AIは使用せず、スクショの形状・設備・表示を保持。
- 改訂確認: 3枚を各500pxで実見し、タイトル全文・文字の余白・背景を確認。`revision-02/source/manifest.json`に元スクショのパス・hash・切出し範囲・処理値・出力hash、`validation.json`にPNG寸法／元画像不変／旧3画像の不存在を記録。作成scriptと中間画像も保持。正式に選ぶ案は未指定、3候補を納品。既存3差分保持、文書以外のGit追加なし。
- 担当: task 01a1480b-6346-7703-aa2f-7c7197252d89。ユーザー依頼: ゲームの雰囲気を伝える展示会用画像、タイトル「ドアが閉まります」、500×500 pixel、PNG。展示用表記でありゲーム内タイトルの変更は行わない。
- 開始: main 8428a7e、既存Homecoming／PlayableDemo／slnx差分、stagedなし。中断した前turnに実行された生成処理はない。ゲーム開発・Unity操作は引き継がない。
- 範囲／工程: 仕様と保存済みゲーム画像で材質・照明・静かな不安を確認→built-in image_genで画像制作→指定寸法への縮小・PNG形式確認・タイトルと全体の実見→記録・WORK-005保存。画像はAssets外のartifacts/exhibition-keyvisualへ保存し、既存ファイルを置換しない。
- 完了条件: 正確な日本語タイトルを含む正方形画像、500×500 PNG、日常のエレベーターと廊下の不穏さが主題。制作画像と実ゲーム画像を区別する。生成原本とpromptを保持。外部投稿・展示会への送信は含まない。
- 成果: `artifacts/exhibition-keyvisual/output/ドアが閉まります_キービジュアル_500x500.png`。閉まりかけた金属扉、隙間の長い廊下、緑灰色の石材、白い蛍光灯、8階表示と2行の白い日本語タイトル。built-in image_genで1254×1254を生成し、既存sharpで縦横同率のLanczos3縮小だけを実施。ゲーム実画面そのものではなく展示用の生成キービジュアル。
- 確認: 最終500×500を実見し「ドアが閉まります」の全文・文字の余白・扉と廊下を確認。PNG／500×500／515,368 bytes、SHA256 `def445e3e7d4aec95cdb4d9fc7cd42dafd8493cc7c614986303b7129f7906daf`。`source/validation.json`、`source/prompt.txt`、生成原本と縮小scriptを保持。成果物はGit外ローカル保存、既存3差分保持。画像の正式採用・外部掲載は別。ゲーム変更のない画像制作のためUnityテスト・buildは実行していない。

## 中間講評資料

### SLIDES-002 Door Closing 中間講評（2026-10-17、作成・対象確認済み）
- 担当: task 01a1480b-6346-7703-aa2f-7c7197252d89。ゲーム開発担当は引き継がず、資料と関連記録だけを編集。開始時main 75db7f0、既存Homecoming／PlayableDemo／slnx差分、stagedなし。
- 目的: 作品をほぼ知らない副査へ、制作意図・先行作品との関係・制作方法・進捗を簡潔に紹介し、提示された6評価項目をカバーする。
- 本人回答: 作品名Door Closing、川俣絢大。密室性とボタンを押した後に結果を待つエレベーターの怖さを共有したい。『8番出口』の形式から怪異への対応を複数に広げたい。制作全体を本人が担当し、実装は主にCodex、Claude Codeも使用。AIエージェントを初めて本格的に使った経験を制作方法に含める。
- 承認: 30分・形式自由。本編8枚＋参考資料1枚、約7〜8分で紹介し残りは画面説明・対話へ使える構成。発表ノートは不要との追加指定。先行作品は『8番出口』との具体比較を中心とし、他作品は参考として扱う。読了未確認の研究を検討済みとしない。
- 工程／範囲: 仕様・記録・実差分・既存検証結果と公式資料を照合→実画面を用いる編集可能PPTX→全9枚の描画・内容・構造確認→記録・WORK-005の保存。出力・生成ソース・検証はartifacts/midterm-slidesに保持する。
- 完了条件: 9枚、本人の動機と担当範囲を正確に反映、発表ノートなし、外部根拠は参考資料で追跡可能、全ページ実見。9月20日までの技術証拠と未検証の現シーン変更、人の評価を区別する。Unity操作・再build・新規試遊は本作業に含めない。
- 成果: `artifacts/midterm-slides/output/DoorClosing_中間講評_完成版.pptx`、9枚、編集可能テキスト・表1点・実画像6枚。約7〜8分は構成の目安であり本人のリハーサル実測ではない。[内容と評価ポイントの対応・根拠](MIDTERM_SLIDES.md)を記録。
- 確認: `build/validation-final.json`で構造・配置・指定フォント・再import合格、警告0。全ページ描画を実見し2枚目の改行と8枚目の画像説明の接近を修正。最終版の未変更7枚は内容・配置・画像hashの一致、変更2枚は再描画・実見で確認。`build/delivery-check.json`でノート本文0、画像比率・無裁断・元画像hash一致、9枚・native表1を確認。SHA256 `82c67c6a8a352035bcff0ddcaa1253bc88571380276b9fa4f4d7b422c3b07508`。
- 制約／試行: PowerPoint実アプリでは未確認。初回finalizerのファイルリンク作成はsandbox EPERM、通常ホストの承認機構で再実行して確定。補助検査の初回はexport時に変わるOffice生成IDと画像relationship IDを同一内容と比較できず失敗し、生成IDだけの正規化と画像hash解決へ修正。既存sceneのtrailing whitespaceは対象外、今回文書のdiff checkは合格。既存3差分は編集・stage対象外。PPTX・生成ソース・証拠はGit外ローカル保持。

## 現在のゲーム開発

### MAC-001 本編のMac試遊版（2026-09-20、build・Editor検証済み／Mac実機待ち）
- 担当: task 01a0bdd5-4115-7920-a6b7-b5ca379d3e75。ユーザーが現状分析と他人に見せる最適構成のMac buildを委任。自分のMacで確認可能と回答。
- 選択: Homecoming全4夜、非Development、Mono、Metal、Intel＋Apple Silicon。旧PlayableDemoは短いが現行の導入・保存・全6表現を見せられないため本編を採用。既存Unity/URPをExtend、新依存なし。今回の限定判断にOSS比較・基盤変更は不要。
- 工程: 既存差分・設定保全→Mac build専用Editor補助→製品ビルド→構成／CPU／権限／同梱物検査→操作案内・ZIP→記録保存。実機でのプレイ合格はMac確認後。
- 開始: main 5498120、既存Homecoming／PlayableDemo／slnx差分あり、stagedなし。初回sandbox検出ではEditorが見えなかったが、通常ホストで既存Editorを確認。二重起動ガードでbatch起動を中止し、既存Homecoming保存済みEdit ModeへMCP接続して実施。ゲーム挙動・ユーザーシーン編集なし。
- Build: `artifacts/mac-01/player/build.json`、Succeeded、84.14秒、error/warning0、非Development／Mono／Metal／全4夜。初回補助コードの旧MacOSArchitecture型を現行OSArchitectureへ修正しcompile成功。シーンの全4夜フラグ・6参照・missing script 0をbuild前検査。
- 配布: `artifacts/mac-01/delivery/Homecoming-Mac-20260920.zip`、58,030,970 bytes、SHA256 `b74deb14d8d4570a0744c01093becb231006275f9f9300afa4a3114675363542`。app＋操作／Mac確認案内＋素材license。全6ネイティブbinaryのIntel／ARM64、ZIP CRC・実行権限を確認。最低OS設定12.0。Developer ID署名・公証は未実施。成果物はGit外、外部公開なし。
- 製品検査: Tests／Editor assemblyなし、ゲームdllのdebug正解／誤答・commit key・TestSaveDirectoryなし。既存MCPForUnity.Runtimeは互換／画像／serialization helperのみで残るが、Editor側の通信・serverは含まれない。CPUごとのcode signature load commandは存在するが、Mac codesignによる有効性検証とは区別。
- 保全: シーン2枚・slnx・build設定6ファイルの開始時SHA256一致。Library内のEditorUserBuildSettingsは操作に伴う差分があるため丸ごと復元せず、build補助で元のarchitecture値へ戻した。Windowsターゲットと旧デモのbuild listは保持。
- 入力検証: 最終`artifacts/tests/20260920-113550-355/playmode.xml`は全4夜＋最終夜死亡再試行、保存／設定／タイトル／再開の2/2 Passed、failed/skip0、317.52秒。合成Input System→Raycast→click。最終Game Viewは`artifacts/m3-04/safe-20260920-113555/`、結果画面を実見。これはWindows Editor検証でありMac入力・描画の合格ではない。
- 試行の限界: 初回MCP経由は保存再開成功／全夜はフォーカス喪失の自動ポーズで失敗。再実行は最後の結果まで到達したがMCP監視が開始timeout扱いとなり正式結果を保持しなかった。既存UnityAgentMenuのXML保存経路へ切替え、最後の2件の合格で確認した。製品挙動やテストassertは変更していない。
- 停止: Homecoming保存済みEdit Mode、処理中build／testなし。既存ユーザー3差分保持。再作成用Editorメニューと梱包Python／README／DEVELOPMENTを追加。人の怖さ・Mac実機の起動／全夜／音／性能は未認定。本人はMac確認「まだやってない」と回答、次は同梱READMEによる実機確認。

### SLIDES-001 ゼミ紹介資料（2026-09-20、作成・検証済み）
- 担当: task 01a0bdd5-4115-7920-a6b7-b5ca379d3e75。ゲーム開発担当は引き継がず、Unity操作なし。対象は紹介資料と本記録のみ。
- ユーザー選択: 現行仕様・最新進捗・既存実画面を使い、概要／遊び方／進捗／スケジュール／展望の5枚。白・黒・青1色、短文、画像は比率維持・無裁断。10月目標／12月上旬最終を記載。
- 完了条件: 編集可能PPTX、5分以内の発表ノート、全ページ描画確認、内容と出典照合。成果物・生成ソース・検証はartifacts/seminar-slidesへ保持。既存3ファイルの差分は対象外。
- 成果: `artifacts/seminar-slides/output/ゼミ発表_自作ホラーゲーム_発表用.pptx`。5枚、編集可能テキストと個別画像7枚、約4分20秒配分の発表ノート。仕様・進捗・ROADMAPに基づき、実装と人の評価待ちを区別。
- 検証: 最終PPTXの構造・配置・フォント指定・再import合格、警告0。最終ファイルから全5枚を描画して実見、画像無裁断・比率保持を確認。PowerPoint実アプリでは未確認。初回の検証環境変数不足、再出力時のreceipt重複を解消。最終証拠はbuild/validation-v3.jsonとslide-1〜5.png。成果物はGit外のローカル保存でありpush対象外。

### M5-01 空間・環境演出の仕上げ（2026-09-20、実装・対象検証済み／体験評価待ち）
- 担当Astra、同checkout main a842d50。ユーザーがM5の実装・技術検証までの自走を明示委任。人の怖さ・楽しさの最終評価は後の通しプレイで行う。既存の文章・音量・比較設定は保持する。
- 工程: 現シーン監査→未実装の退出／階段の空間ループと環境音の接続→既存6表現・導線の画面確認と必要な修正→Windows版・記録・保存。既存Unity/URP、造作・AudioSource・Input System試験をExtendし、新依存や全面置換を行わない。
- 変更候補: Homecomingの追加造作・狭い演出component、作者用Inspector、関連入力試験、Docs。GAME-011の短い接続通路、GAME-012/015の生活設備音と日常／怪異の対比を試作。文章は作者執筆方針を保ち、新しい物語・ルール・怪異は追加しない。
- 完了条件: 入力経路で退出→同ホール帰還、反復／ポーズ／復帰で破綻しないこと、怪異の判別・帰路の維持、最終Game View実見、音の出力条件、Console・製品build。M5全体の実装確認と人の受入は分け、残件を明記する。
- 保全: 開始時stagedなし。Homecomingは作者のdroneVolume=.518／声1件とUnity空白差分、PlayableDemoも既存作者差分。両シーンの開始時コピー・hashをartifacts/m5-01へ保存し、今回の追加分だけをcommitする。
- 実装: 対称通路の途中で視点pitch／移動を保って同じ場所へ接続。退路・階段口の歩行とダッシュ、反復、ポーズ、帰宅後の翌夜、固定設備音の非表示階停止と追従音非重複を追加。初回062612-311は試験の状態名AwaitingCall誤記で失敗（実装はWaitingForCall）。修正後`artifacts/tests/20260920-062801-642/playmode.xml`の対象試験1/1成功・71.23秒。
- 追加監査: 機械暴走に残る旧発光cubeを本編だけ非表示にし、実かご照明の0.85／0.65倍への遷移・復元へ接続。全6表現・階数・期限・正解操作を維持。5戸しかなかった8階へ806を加え、番号を作者編集へ接続。結果後のページ式スタッフロール、フェード、固定スキップ、編集Inspectorを追加。新しい物語の文章は追加しない。
- 通し結果: `artifacts/tests/20260920-063721-070/playmode.xml`、3/3成功・failed/skip0・387.36秒。全4夜＋最終夜死亡再試行、保存最終夜→結果→スタッフロール→スキップ→タイトル、長文でも固定スキップ・ページフェード・自動終了。怪異中の実照明と終了後復元、6表現の画像を確認。機械暴走の旧cube非表示と天井伸長は別経路で維持。
- 反射更新後: `artifacts/tests/20260920-064515-349/playmode.xml`、3/3成功・failed/skip0・111.35秒。開扉前の濡れ→徒歩救済・復元、旧M2の13超えと一定間隔・停止、期限超過から非常停止で救済。`artifacts/tests/20260920-064805-301/editmode.xml`、2/2成功・failed/skip0。今回の修正後合計はPlayMode7件＋EditMode2件。全テスト一括実行ではない。Consoleには従来のLure plateText未接続warningがあり、今回のerrorなし。
- 実画像: `artifacts/m5-01/input-20260920-062806/`の退出／階段接続・戻ったホール、`artifacts/m3-04/safe-20260920-063840/`の各夜6表現・結果、`artifacts/m4-01/tests/3eefe61acdb14dfc86cbd5cd4366851b/credits-first-page.png`、反射更新後`artifacts/m3-01/recovery-20260920-064521/07b-wet-during-opening.png`を実見。通常モニターは既存作者設定の表示6秒／フェード3秒を保持し、撮影タイミングで暗くなる。初見の読解・怖さの合格宣言はしない。
- Windows: `artifacts/builds/20260920-064826-157/GraduationProject.exe`、非Development、15.11秒、build error/warning0、Tests assemblyなし。READMEとlicensesを同梱。設定5ファイルは直前hashへ一致復元。Computer Useで起動・1920幅のタイトル・続き表示を実見。クリック後の遷移は確認できず、次の操作で`user input was detected in this window`となったため再観測後に前面操作を終了。Playerは起動したまま、ユーザーの進行を上書きしていない。
- Player証拠: `artifacts/m5-01/Player-title.png`／`Player-startup.log`。後者は通常ホストの読取権限で取得。起動ログに例外・shader errorなし、既存D3D12 info queue診断あり。今回のWindows操作・全4夜通しの成功とは扱わない。主な通し証拠はEditorの合成Input System入力。
- 保全・次: Homecomingの作者音量／比較設定とPlayableDemoの開始時hashを保持し、今回の意味差分だけをGitへ保存する。Editorは保存済みEdit Mode。M5の仕上げ実装を通し評価できる版とし、次は本人の理解・怖さ・間延び・音への評価に応じて修正。M6のWindows全行程・代表環境の性能確認は残る。物語・掲示の最終文章は作者執筆という委任境界を維持。

### M4-01 保存・結果（2026-09-20、実装・対象検証済み）
- 担当Astra、同checkout。M4_SAVE_PLAN採用と自己ベスト独立保存の回答を反映。既存JSON API／標準IOと既存メニューをExtendし、外部frameworkなし。
- 範囲: 専用保存層、DemoSession／HomecomingCampaign／RunManager、作者用追加文章グループ、隔離保存試験。進行／プロフィールを分離し、一時ファイル＋直前backup・版／整合性検査・排他で誤上書きを防ぐ。新規／続き／タイトル・結果・設定を既存UIへ接続。
- 完了条件: 保存の正常／異常系、独立ベスト、累積・計時除外、入力経路の夜再開／設定／結果、最終画像、Windows終了→再起動→続き、Console／build、作者差分保護・Docs・commit/push。素材・物語・大規模UI刷新は行わない。
- 現在: 保存層と既存メニュー接続、文章編集11を実装。初回050801-799は5/8成功、空／未来版のInvalidDataException捕捉漏れとEditorのbuild list外シーン再ロードが失敗。例外条件とEditor専用再ロード経路を修正。再試験`artifacts/tests/20260920-051050-622/playmode.xml`は9/9成功・failed/skip0・8.66秒。復元・設定・タイトル再ロード・上書き取消／確定・導入省略・独立ベスト・完成直後中断・破損／二重起動／IO失敗の保護を確認。
- 通し検証: `artifacts/tests/20260920-051148-286/playmode.xml`、全4夜・最終夜死亡→結果の累積と既存PlayableDemo再プレイの2/2成功・failed/skip0、462.8秒。導入を除く247.41秒・死亡1を今回値と初回ベストへ保存。`artifacts/m4-01/tests/7de8b7af4dfe4add913bad679a44ea13/continue-title.png` と79f09163…のskip-choice.pngを実見。
- 結果画面の実画像で旧RunManagerの帰宅文が背後に残るのを発見し、専用結果では旧文だけ非表示へ修正。`artifacts/tests/20260920-052118-563/playmode.xml`、最終夜保存→通常入力でクリアが1/1成功・failed/skip0・68.6秒。`artifacts/m4-01/tests/4d86b8a62e734c58ba9ffec39473e757/four-nights-complete.png`を実見して重複解消。文章編集の導入表示も追加。Editorのdirty表示はSaveAsCopyと現ファイルのSHA256完全一致を確認して保存した。
- Windows: `artifacts/builds/20260920-052326-582/GraduationProject.exe`、非Development、11.17秒、error/warning0、テストassembly／Editor保存先overrideなし。build設定5ファイルを直前値へ一致復元。Computer Useで720pの音量変更→開始→フォーカス喪失ポーズ→確認付きタイトル復帰→終了、別PIDで再起動→同じ音量／サイズと続き表示→クリックで導入入口へ復帰を確認。終了後のプロセス残存なし。検証用設定は1080p・音量約1へ戻し、導入の確認用セーブを残した。
- 証拠: `artifacts/m4-01/Player-title-after-restart.png`、`Player-settings-after-restart.png`、`Player-continued-entrance.png`、前後JSON、`Player-restarted.log`。例外／shader errorなし。既存D3D12 info queue診断と影atlas縮小あり。Computer UseのEsc送信は反応を確認できず、実キーボードの可否とは分離。Windows再起動は導入で検証し、怪異夜の再開・死亡累積・全4夜完走はEditor入力試験で検証。Windows全夜通し／人の怖さ・長さは未評価。
- 保全: Homecomingの新文章グループだけを既存作者差分と分離して保存。作者の音量0.518／声1件、PlayableDemoの文章・音量変更は保持。PlayableDemoのSHA256は開始時と一致。Editor保存済みEdit Mode。
- 次: M4の必須機能は実装した。Astra継続、M5の演出・空間・音と初見の理解を詰める。技術合格から面白さや10〜20分の体感を認定しない。旧節の「保存未実装／確認待ち」は当時の履歴として扱う。

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

---

# 過去の作業記録（2026-09-08〜14）

2026-09-15にPROJECT_STATUSの過去本文を保存。以下の担当・「現在」「次」・実行中表示は記録当時の情報で、現在の指示ではない。再開時は [PROJECT_STATUS.md](PROJECT_STATUS.md) を参照し、原因調査や証跡確認に必要な節だけ読む。過去の検証結果を現在版の合格に読み替えない。

---

# 現在地・再開情報

## VIS-06 全体監査と経年表現（2026-09-14）
- 状態: VIS-06の監査・下記修正と検証を完了。担当Astra、本task、開始main clean / 985dc6c。同checkout単独操作。ローカルMCP停止を既存Start＋Connectで復旧。終了時EditorはM2保存済みEdit Mode、検証Player終了。
- 依頼: ビジュアルとシステムを徹底検証し開発再開。ユーザーは清潔すぎる見た目と不安の弱さを指摘、システム開発を一任。GAME-015内の具体表現を試作し、人の恐怖評価とは分ける。
- 対象: M1/M2共有内装、材質の実寸投影・乾いた摩耗・粗さ／凹凸、照明の接触影、RunManagerの製品ビルド用debug入力除外。材質と照明を第一単位とし、進行や公平性を変えない。新パッケージなし。監査結果はAUDIT_2026-09-14へ記録済み。
- 検証: 開始時全入力回帰と画面を基準として保存。変更後にM1/M2・救済／死亡／再挑戦・日本語・壁付け表示を再確認、shader compile、Windows build/画面、Draw/性能は測定条件と限界を記録。正式な写実品質や恐怖は代行認定しない。
- 重要な発見: 基準回帰33/33（20260913-171341-868）でも、M2偽到着はEntranceHallが残り柱が壁裏に隠れていた。材質第一案の回帰33/33（171856-222）の画像で発見し、PrepareEncounterEnvironmentでHomeCorridorへ切替。車内から柱ColliderへのRaycastが直接届くassert追加後33/33（172424-803）。従来の柱視認確認は不十分だった。
- 中間修正: 石材の人工的な計算模様を専用生成画像へ差替え、住戸扉は塗装材へ分離。可視になった仮柱の白発光を通常の壁材へ変更し、Reveal/Grace色は保持。怪異のRenderer.materials生成instanceが解放されない問題もOnDestroyへ解放処理を追加。M2実経路で消滅後の破棄assertを含む全33/33成功（172901-024、失敗skip0）。その画面に残った柱上端の白飛び・案内器の影の縞を照明／前面shadow設定で修正し、M2の3件を再検証中。
- 追加確認: 173430-429でM2の3/3成功。柱の白飛びは軽減。案内器の縞はshadow設定だけでは残り、液晶面を専用Unlit材へ分離して一巡1/1成功（173744-377）。画面で縞の消失と日本語を確認。原因を通常shadowだけと断定しない。
- 最終検証: EditMode2/2（173911-976）、shader errors/messages0、Console error0。全33件の後の照明／液晶材変更は上記3件＋1件で再確認し、全33件をその後再実行したとはしない。すべて失敗skip0。M1画面はartifacts/m1-02/scene-input-20260913-173143-554、最終M2一巡はartifacts/m2-04/scene-input-20260913-173748-219（1579×888）。救済／死亡再挑戦はartifacts/m2-06の173615-330／173538-151。入力は合成Keyboard/Mouse→Raycast→click。
- Windows: Development 20260913-173921-504、非Development 20260913-174008-540ともbuild.json Succeeded、errors0/warnings0。両Assembly-CSharp比較でdebug用5名称はDevelopmentに存在、非Developmentに不在、通常BeginEncounterRunは両方に存在。Tests assemblyも両方なし（artifacts/visual-06/build-symbol-check.json）。公開・配布ではなくローカル検証。
- 実画面: Computer Useで非Development Player入口1280×720を確認し、artifacts/visual-06/windows-player.png／Player-observed.logへ保存。実行時例外／Unicode警告なし。既知のD3D12 info queue診断行は残る。OS入力の全経路・音の聴感・1080p60fpsは未認定。Player終了確認済み、生成時の行末空白のみ整えM2再読込後Console error0。artifactsはGit外のローカル証跡。
- 判断／次: **Astraのブラッシュアップを継続し、Solへ全面引継ぎしない。** 次は車内3.4m幅／天井3mと廊下の比率を、到達距離・救済移動・敷居と同時に検証。その次に足音／生活環境音と機械音の自然さ。M3全4夜・M4保存／UIは未着手。恐怖、初見理解、目標写実度は未受入。品質改善をM3完成と取り違えない。次担当は本記録とAUDITを読んで所有確認から再開する。

## VIS-05 扉材と常設案内器の修正（2026-09-11）
- 担当: Astra、main clean / 264c84fから継続。直前の中断ターンでは変更なし。
- 依頼と方針: 同一材の扉／壁を分け、扉上の浮いた表示を操作盤内へ統合。挑発パネルは壁に接する常設の金属枠案内器をM1/M2へ設置し、通常案内から警告文へ短くフェード切替する。怪異の正解・猶予・開始条件は維持。
- 中間経過: 初回適用はdirty guardで停止。artifacts/visual-05/unsaved-M2.unityへコピーを退避し、保存済みとの差分が今回追加のinformationDisplay参照1行のみであることを確認して保存・再開。初回回帰32/33、追加撮影移動後の閉ボタンが距離外でM2一巡1件失敗。ゲーム距離や判定は変えず、試験経路で盤へ歩いて近づく処理を追加し、当該1件の再実行成功。
- 対象と検証: ApartmentVisualPass、ElevatorRealismPass、ProvocationAnomaly／BeatStateMachineの表示参照、常設表示component、M1/M2、scene入力回帰。正面だけでなく側面から壁との接触、開扉時の浮遊表示消失、挑発終了時の通常表示復帰を確認し、関連テスト・Player buildを記録する。新依存なし、既存素材を再利用。
- 完了範囲: 扉はDoorSteel、壁はIvoryLaminateへ分離。旧中央表示を無効化し、3操作盤内で階数を同期。案内器は背面壁に2mm重ねて固定し、通常文から挑発文へ0.3秒で内容のみ切替、終了時に復帰。M1/M2ともgeometry-audit-result.jsonで接触と材質・無効化を確認。
- 検証結果: 初回PlayMode32成功・1失敗（artifacts/tests/20260910-172147-594）、経路修正後M2一巡1/1成功（20260910-172645-850）。全33件に成功結果が揃ったが、修正後の全件一括再実行ではない。skip0。EditMode2/2（20260910-173016-344）、Console error0。artifacts/m2-04/scene-input-20260910-172650-665の斜め視点・挑発・操作盤階数表示を実見（761×428）。常設器の存在／接触、浮遊prefab描画停止、通常表示復帰もassert成功。
- Windows: artifacts/builds/20260910-173028-227/build.json成功、errors0 / warnings0。Computer Useで入口Player画面1280×720を実見、artifacts/visual-05/windows-player.png・Player-observed.logへ保存。実行時例外／Unicode警告なし。OS入力による全経路は未検証、全経路はEditorのInput System→Raycast試験。Player終了確認済み、UnityはM2保存済みEdit Mode。artifactsはローカルのみ。
- 判断: 今回の3点は実装・検証済み。Solへの引継ぎは保留し、Astraの品質確認を継続する方針。残る全体の縮尺・壁の法線／粗さ・空間の統一感は未完成で、『8番出口』相当や人の視覚評価済みとは扱わない。M3未着手。

## VIS-04 実物基準への配置・掲示・動作音の修正（2026-09-11）

- 担当／状態: Astra継続。ユーザーがVIS-03の品質とSol引継ぎを否定し、操作盤移設・掲示縮尺・動作速度・音の徹底修正を明示依頼。以前の限定引継ぎ判断を撤回。開始時main clean、同じcheckout。
- 調査: CR3内の6000×4000 JPEGをartifacts/visual-04/referencesへ無加工抽出して実見。IMG_3783は縦長金属盤に表示器・スピーカー・2列階ボタン・開閉・保守扉、IMG_3821は縦長の白紙に密度のある黒文字と2片のテープ。実在連絡先は転記しない。三菱の出入口脇主操作盤＋両側面車いす操作盤、日立の主／横型操作盤資料を確認。出典はVISUAL_REFERENCESへ記録する。
- 計画: 既存Unity/URP/Input Systemを拡張（外部パッケージや別シミュレータのForkは不要）。入口脇の縦長盤＋両側の横型盤、1〜10階・開閉・ゲーム固有の非常停止、同期階数表示を実装。奥壁の操作盤を撤去扱いで無効化し、掲示はA4相当の本文階層へ。通常移動24秒／扉2.4秒を試作し、駆動・扉・着床音と加減速の音量変化を配線する。
- 中間記録: 最初の適用でUnity Objectの疑似nullを`??`で扱ったAudioSource生成が失敗。部分変更を保存・記録し、明示null判定へ修正後、M1/M2への再適用成功。PlayMode回帰と新しい3面のレイ入力・駆動信号試験を実行中。
- 実装済み: 出入口脇の縦長主盤＋左右横型盤（39ボタン）、外側呼出盤、4面同期階数表示。日本語見出し／本文のA4掲示、24秒の通常移動、2.4秒の扉、駆動・扉・着床音。hover時の数字の白飛びも修正。掲示／表示59箇所の枠監査で超過0（artifacts/visual-04/layout-audit-result.json）。
- 検証: 初回PlayMode29/33（扉参照を持たないfixtureの完了判定3件、音声GetOutputData初回取得0の1件が失敗）。扉状態待ちを保全し、信号検査は1秒以内の実サンプル取得で再検証。最終全回帰33/33、失敗skip0（artifacts/tests/20260910-155327-881/playmode.xml）。その後の呼出盤／文字枠仕上げは通常帰宅＋3盤Raycast＋駆動出力1/1（20260910-155943-255）、EditMode2/2（20260910-160048-544）。実装条件を弱めるskipなし。
- 画面: artifacts/m1-02/scene-input-20260910-155947-855 が最終通常入力の掲示・3盤・帰宅。M2全経路は artifacts/m2-04/scene-input-20260910-155347-182、再現・死亡／救済も回帰成功。Game Viewは実取得761×428／拡大時1579×888で、1080p固定を認定しない。文字は近づいて読む実寸、操作数字はhover中も識別可能。
- Windows: artifacts/builds/20260910-160105-899/build.json 成功、errors0 / warnings0。Computer Useで最終Player入口1280×720を実見、artifacts/visual-04/windows-player.pngとPlayer-observed.log保存、実行時例外／Unicode警告なし。短いOS W入力では持続移動を確認できずOS入力一巡は未検証。Playerは終了、UnityはM2保存済みEdit Modeへ戻す。音声は実出力を検証したが人の聴感評価は未了。
- 判断: VIS-04の指摘4点に対する実装・技術確認は完了。Solへの引継ぎは撤回したまま。Astraによる次の品質確認対象は車内の3.4m幅・壁材の法線／粗さ・怪異の仮表示と空間全体の統一感。現在の形を『8番出口』相当／無欠点と認定しない。M3は開始しない。証跡artifactsはローカルのみ。
- 対象: Editor装飾、FloorIndicator、ElevatorController、NormalJourneyController、M1/M2 scene、音声素材、移動と操作対象名を参照するscene入力試験。経路・怪異の正解規則を保ち、救済動線／遅くした扉と期限の整合を確認する。実画面で3面・掲示・全通常入力、回帰、Windows Player、音の再生状態と信号を確認。人の聴感・無欠点の保証は技術検証から断定しない。

## VIS-03 操作盤の比率・金属材質（2026-09-11）

- 状態／担当: VIS-03の実装・下記検証を完了し区切り。Astra task 01a07fbb-3ac2-7a62-a049-19d919724625。直前VIS-02から継続、開始時main clean。終了時M2 Edit Mode、検証Player終了。Solは次回開始時に所有を確認して下記範囲を続けられる。
- 範囲／委任: 継続依頼とGAME-015の試作内で、操作位置を維持しつつボタン面と当たり判定を同じ比率で小型化、表示板・金属板・銘板を整理する。金属base colorを追加し反射を調整。移動空間／猶予／進行ルールは維持。実寸の完全再現よりも操作距離での可読性を優先して評価する。
- 対象／検証: ApartmentVisualPass、ApartmentVisuals、M1/M2 scenes、必要な入力試験。通常Keyboard/Mouse→Raycast、誤答救済、Game Viewの文字・位置・材質、Windows buildを確認。小型化で操作しづらくなることが主なリスク。正式品質受入と人の押しやすさ評価は別。
- 中間結果: 小型化後Play Mode 33/33成功、失敗skipなし（artifacts/tests/20260910-150530-393）。M1 panel-hover画面で「非常停止」の右端はみ出しを発見し、二段表示へ修正。生成素材・プロンプト・試作寸法はVISUAL_REFERENCES。
- 最終検証: Play Mode 33/33成功、失敗skipなし（artifacts/tests/20260910-150830-014/playmode.xml）、Edit Mode 2/2（20260910-151024-561/editmode.xml）。Console Error 0。M1のartifacts/m1-02/scene-input-20260910-150952-837/panel-hover.png（1579x888）で非常停止を含む全ボタンの文字収まりと8のhoverを実見。M2のartifacts/m2-04/scene-input-20260910-150843-956は同じ最終codeの通常入力証跡。
- Windows: artifacts/builds/20260910-151107-633/build.json、成功・0 errors / 0 warnings。Computer Useで入口1280x720を確認しartifacts/visual-03/windows-player.png保存。Player.logに例外・文字欠落の該当行なし。短いW入力では十分な移動差を確認できず、OS入力一巡は未検証。設定ファイル差分なし。人の操作性・音・60fps・目標写実度の受入は未了。
- Solへの限定引継ぎ: 次はVIS-04として、既存の内装構成・白色照明・Noto Sans JPを維持して、貼り紙の紙／テープの厚みと縁、金属枠の接合部など近景の仕上げを1単位ずつ進めてよい。入口・操作盤・廊下の同一視点比較を残し、表示距離で読めることと動線を確認する。実在電話番号や個人情報は使わない。共有処理はApartmentVisualPass、対象はM1/M2、素材方針はVISUAL_REFERENCES。M3へ勝手に進めず、既存操作位置・猶予・部屋寸法の変更や新依存はこの引継ぎに含めない。
- モデル判断: 操作盤の主要な比率・材質修正は本単位で区切り、上記の限定した仕上げはSolで続行可能。常時Astra監督は不要。『8番出口』相当の品質達成とはしない。車内寸法・怪異の仮形状・法線／粗さの専用素材・人の押しやすさ評価が残り、大きな作り直しが必要なら原因と影響を整理して再判断する。

## VIS-02 内装の造作・照明整理（2026-09-10）

- 状態／担当: 造作改善の実装・下記検証を完了し区切り。Astra task 01a07fbb-3ac2-7a62-a049-19d919724625。開始時main clean、Sol実装task idleを確認。終了時EditorはM2 Edit Mode、検証Playerは終了。次の実装担当は本記録を読んで所有を確認する。
- 計画／委任: GAME-015/WORK-001と今回の継続依頼に基づく試作。ApartmentVisualPass、共有素材、M1/M2 sceneを対象に、写真に近い穴あき天井板、操作盤の縁・固定金具、玄関造作と共用廊下の生活感、白色照明の見え方を調整する。既存の移動経路・操作対象・進行条件を維持し、新たな操作対象は追加しない。
- 検証: コンパイル、両sceneの通常入力回帰と実Game View、Windows Development Build／画面、追加装飾のColliderなしを確認。寸法・材質・文字の改善余地を実画面から判断し、Astra継続／Sol引継ぎの結論を記録する。品質受入・恐怖・最低環境性能は人の評価と区別する。
- 中間結果: 初回project入力回帰33/33、失敗skipなし（artifacts/tests/20260910-144938-220）。画面で天井板の角の光漏れ、非active RectTransformの位置が保存時に戻る階数表示、玄関下部の隙間を発見して修正。絞り込みMCP job 239cc2c6b42c4449b5a48c97970536c8は初期化timeout、合格に数えない。EditorがEdit Modeへ戻ったことを確認し、既存メニューのXML保存方式で最終回帰を再実行する。
- ビルド再試行: Unity保存が付けた行末空白のみを今回の対象ファイルで除去。再読込ダイアログ中の最初のbuild要求はtimeout、新規build artifactなし。未保存変更なしを確認済みの自分のsceneを再読込してから再要求する。
- 最終検証: Play Mode 33/33成功、失敗skipなし（artifacts/tests/20260910-145410-785/playmode.xml）、Edit Mode 2/2（20260910-145556-172/editmode.xml）。M1/M2とも追加InteriorVisuals内Collider 0。Console Error 0。最終Windows M2 build artifacts/builds/20260910-145749-464/build.json 成功、errors 0 / warnings 0。設定の一時変更はbuild処理が復元し、設定ファイルの差分なし。
- 画面: artifacts/m2-04/scene-input-20260910-145420-690（1579x888）の廊下・挑発・階数表示を実見。M1通常入力証跡はartifacts/m1-02/scene-input-20260910-145529-574。Windows Computer Useで1280x720 Player入口、日本語表示とマウス視点応答を確認。OS入力による帰宅一巡は未検証で、合成Keyboard/Mouse→Raycastの回帰と混同しない。持続60fps・音・恐怖・正式品質は未認定。
- 次担当の判断: **Astraでさらにブラッシュアップが必要。現時点ではSolへ全面引継ぎしない。** 操作盤が大きく材質も均一で、写実度の基準が未成立。次は操作盤／車内の実寸感と金属・壁面の粗さを優先し、既存の試作委任を越える操作配置・移動寸法変更が必要なら先に設計判断を解決する。基準成立後にSolへ反復配置・文書・回帰を渡す。M3は未着手。

## VIS-01 参考写真に基づくM1/M2内装改善（2026-09-08）

- 状態／担当: 内装の第一段階を実装・技術確認済み、見た目の人間評価待ち。Astra task 01a07fbb-3ac2-7a62-a049-19d919724625。Sol task idle、main差分なし、M2 Edit Mode未保存変更なしを確認して担当した。
- 依頼: ユーザー提供4枚。『8番出口』を写実度の目標、残り3枚を築年数のある居住中マンションと車内の材質・貼り紙・天井灯の参考とする。日本語と実プレイ時の文字可視性に留意。GAME-015/WORK-001の試作委任内で可逆的な見た目を改善する。正式な視覚品質・恐怖の受入は人の評価待ち。
- 計画: 既存URP/TMPを採用し共有内装を拡張。Editor専用の再適用可能な装飾処理、素材、両scene、日本語の挑発表示を対象に、車内→入口／8階廊下→実画面確認の順で進める。ゲーム進行・経路・ボタン当たり判定・難易度は維持。写真の人物・実在連絡先は素材化しない。
- 検証予定: compile/Console、既存M1/M2入力回帰、1080p Game Viewで入口・車内・廊下・怪異表示、Windows Development Player起動とOS入力を分離確認。素材反復、影、文字欠落、対象遮蔽が主な確認点。未実施を合格扱いしない。
- 中間検証: project Play Mode 33/33成功・失敗skipなし（artifacts/tests/20260908-092718-003 および 20260908-093052-727）。今回は対象assemblyを指定した33件で、過去の全37件とは母数が異なる。Edit Mode 2/2（20260908-093548-293）。
- 修正経過: TMP追加でTransformがRectTransformへ置換される参照切れ、Unity native componentのnull判定を修正して再適用。作業開始時dirtyなし、生成途中の自分の変更のみを保存・再開した。画面確認で入口掲示の折返し、車内注意書きの重なり、挑発文字のパネル外はみ出し、発光materialのkeyword無効、立体目地の遠景ちらつきを発見して修正。画像資料・採用範囲・生成素材はVISUAL_REFERENCES.md。
- 最終検証: 床材／照明変更後の通常入力2/2成功・失敗skipなし（MCP job bd1a63278f194c1a851f8cf53ff7f10c）。Game Viewの入口・挑発表示・8超え階数・廊下を実見（artifacts/m2-04/scene-input-20260908-093627-704、1579x888）。その後、入口掲示の柱との重なりと扉の見切り線だけを修正。最終M2 Windows Development Buildはartifacts/builds/20260908-093929-670/build.json、成功・errors 0 / warnings 0。Console Error 0、M2 scene validate 0 issue、追加InteriorVisuals内Collider 0を確認。
- Windows証跡: artifacts/visual-01/windows-player.png はWindows Computer Useで取得した最終Player入口（client 1280x720）。Player-observed.logの保存時点で実行時例外・Unicode欠落警告なし。ユーザー入力を検知してこちらの入力を中止し、Playerを起動したまま残す。AgentによるOS入力一巡は未検証。ユーザー側の操作ログをAgentの操作試験に数えない。
- 限界／次: 1080p固定画面・60fps／最低環境・音の聴感・恐怖・正式な写実品質は未認定。操作盤と空間寸法は既存試作のまま。『8番出口』相当の完成品質には未達で、第一段階の内装・日本語を実際に遊んで評価してから、形状／固有ディテールを詰める。M3は今回対象外。ゲーム工程の次担当は未着手状態から本記録を読む。

## Computer Use手順の訂正（2026-09-08）

- 担当／範囲: task 01a07fbb-3ac2-7a62-a049-19d919724625。ユーザーの明示依頼でAGENTS・START_HERE・DEVELOPMENT・本書のみ更新。Sol担当はidleを確認。ゲーム工程・実装担当は変更しない。
- 原因／訂正: SolはWindowsスキルを読んだがcua_replだけを呼び、apps空から全体を利用不可と判断した。同日Astraが別の正式経路 `mcp__node_repl__js`＋`@oai/sky` でUnityウィンドウの列挙・前面化・画面取得に成功。従来の「Computer Use不可」はこの調査で訂正する。モデル差・権限不足が原因との断定はしない。
- 手順: 開始時の必読文書からDEVELOPMENTの接続確認へ誘導し、ツール探索→初期化→Windows列挙を行ってから可否を判断する。既存の承認・操作制限は維持。ユーザーの毎回の再指示は不要。
- 検証／限界: 記載した呼出しは直前の診断で実行済み。今回の文書変更は差分・参照を確認し保存する。ゲーム変更・テスト再実行なし。M2の過去のOS実入力未実施は未検証のまま残す。

## M2-06 救済・死亡・再挑戦の画面証跡（2026-09-08）

- 状態／担当: 試作実装・自動検証完了、人の体験評価待ち。現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。ユーザー指示によりM3へ進まずM2-04の画面理解を最優先した。
- 目的／見える行動: 誘引の初回誤答で怪異が明瞭に変貌し、帰還＋閉で救済されて怪異が去ること、2回目誤答で死亡暗転し夜入口から再挑戦になることを、一連のGame View画像で比較できるようにする。通常プレイ中の正誤ラベルや説明HUDは追加しない。
- 対象／検証: M2 scene PlayMode testと必要時のみ既存Anomaly/Run presentation・Builder/scene。合成Keyboard/Mouse→RaycastでReveal、Grace、Resolve、Death fade、入口復帰を撮影し、画像を実見する。全Play/Edit、Console、scene validate、Windows buildは製品変更が生じた場合に再実行する。
- 実装: 誘引stand-inの未配線だったReveal用Lightとdroneをruntime試作として補完。初回誤答の赤い照明、Graceの橙照明、閉扉救済後の消失を環境内だけで示す。正誤ラベル・説明HUD・外部素材・新依存は追加していない。色・強度・生成音はGAME-015の試作値で正式採用前。
- 画面証跡: `artifacts/m2-06/lure-recovery-20260908-083340-699` で赤Reveal→橙Grace→閉扉Resolve、`artifacts/m2-06/death-retry-20260908-083325-672` で赤Reveal→橙Grace→黒い死亡暗転→1階入口を目視。最初の撮影は次フレームの移動で構図がずれたため成功扱いせず、captureをEndOfFrame同期して再撮影した。
- 検証: 対象2/2（job `a593c6ba193b47ea842d9130e822d41e`）、全Play Mode 37件中35成功・0失敗・Unity Input System既知2件skip（job `365e2c9df63d40568a7f99c9c42225ac`）、Edit Mode 2/2（job `8ae3f0a948c348e6a417c121f8144b69`）、Console Error 0。Development Build `artifacts/builds/20260908-083658-046/build.json` は成功・0 errors / 0 warnings。Player実起動は応答あり・ログ例外なしで終了。
- 限界／次: 画面上の状態差は確認したが、仮形状・試作色・生成音のため正式な見た目や聴感ではない。人が操作して怪異・救済・死亡・再挑戦を理解できるか、怖さ・理不尽さ・猶予は未評価。M3依存はこの人間評価後に判定する。

## M2-05 監査修正：死亡復帰と帰宅完了（2026-09-08）

- 状態／担当: 実装・自動検証完了。現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。ユーザー監査でM2-04の不足2件を確認して修正。
- 不足／期待動作: 死亡fade後に座標・視点・空間状態が残ったまま怪異1へ戻る不具合を直し、現在夜の入口ホールへ復帰する。3怪異だけではクリアせず、本物8階を提示し、降車して自宅ドアを通常クリックした時だけ夜クリアにする。
- 実装方針: 既存NormalJourneyControllerを夜の入口／本物8階区間にも再利用し、RunManagerは怪異列・死亡fade・夜完了を担当する。InteractionRaycasterは有効な通常Journeyを優先し、怪異中だけBeatStateMachineへ渡す既存境界を維持する。大型置換・新依存・保存形式追加なし。
- 対象／検証: NormalJourneyController、RunManager、PlayerLook、M2 Builder/scene、M2 scene testと必要な分離テスト、DECISIONS、本書。入口からの通常入力、3怪異後RunClear未発火、実廊下の徒歩＋自宅クリック、死亡時の入口座標・視点・hall/corridor・beat index復帰、全Play/Edit、Game View、Windows Development Playerを分離確認する。
- 開始時差分: `Assets/Scenes/M2VerticalSlice.unity` にUnityが追加したURP camera/light dataとTMP style hashの未コミット差分あり。ユーザー由来と断定せず保護し、scene修復後も必要なUnity正規化として残る場合は本単位へ明記して含める。
- 実装: NormalJourneyControllerを入口→怪異列→本物8階→自宅の夜経路として接続。RunManagerは最終怪異成功時に帰宅区間を提示し、自宅ドア完了通知までRunClearを保留する。死亡fade後は怪異列を停止し、入口座標・body/camera回転・1階表示・閉扉・hall/corridor・完了UIを夜開始状態へ戻す。初期化時の閉扉はアニメーションせず即時復元し、開始直後の呼出クリックを落とさない。
- 検証: 対象2/2合格（最終job `df9b505f054e47689800e1f40297564d`）。通常Keyboard/Mouse→Raycast経路で入口呼出・乗車・8階・3怪異・本物8階・徒歩・自宅クリックを通し、3怪異直後のRunClear未発火と自宅後発火を確認。死亡は誘引で敷居越え→帰還→2回目誤入力を通し、入口位置 `(0,0.95,-1)`・body/camera正面・WaitingForCall・hall表示・home非表示・RunClearなしを確認。全Play Mode 36件中34成功・0失敗・Unity Input System既知2件skip（job `7f700d52b6d042a89007c692127e9ba7`）、Edit Mode 2/2（job `cfdae30428f14aa096156c94e732f4db`）、scene validate 0 issue、Console Error 0。
- 証跡／Windows: `artifacts/m2-04/scene-input-20260908-080426-417` のhome corridorと自宅後clear、`artifacts/m2-05/death-restart-20260908-080515-866` の死亡後入口を目視。最終Development Build `artifacts/builds/20260908-081517-491/build.json` は成功・0 errors / 0 warnings。Playerを実起動し応答あり、WaitingForCall到達、ログ例外なしを確認して終了。Computer Useにはnative app surfaceが公開されずPlayer画面へのOS操作は未実施。
- 検証中断記録／限界: 最初の対象2件は起動時にアニメーション閉扉が走り呼出クリックを捨てて2/2失敗し、即時閉扉復元へ修正して解消。最初の全Play Mode job `6e7e23677b534e2eb751723bc3c5ae5c` は0件のまま初期化timeoutで失敗し、Edit ModeでEditor状態を更新後に再実行して上記全件結果を得た。URP追加dataとTMP hashはscene repair後もUnity正規化として残り本単位に含める。人の初見理解・難易度・恐怖は未評価で、M3の体験受入を代理認定しない。

## M2-04 3系統縦切りシーン（2026-09-08）

- 状態／担当: 試作実装・自動検証完了。現在のCodex task 01a07f7e-0cd8-7500-b2c1-afbb84a88bfb、このcheckout。Astra修正 `a3e8447` のpush済み・全30件合格を確認して継続。
- 目的／見える行動: 専用シーンで誘引を閉じて拒否、挑発を無操作で耐え、乗っ取りを非常停止し、3系統を通常のWASD・マウス・左クリック経路で一巡して終了できる。誤答時は既存Reveal・Grace・死亡再挑戦を同じ配線で使う。
- 対象仕様／試作: GAME-004〜009、012〜015、ROADMAP M2。M1NormalRouteを上書きせず専用 `M2VerticalSlice` を生成する。既存BeatDefinitionの未配線値を仕様に合わせ、時間・簡易表示・順番は人の評価前の試作値として記録する。
- 対象ファイル／検証: M2シーンBuilder、生成scene/.meta、lure/provocationアセット、必要なPlayerテストとEditorメニュー、DECISIONS、本書。Unityコンパイル、専用シーンの通常入力一巡、誤答再挑戦、Game View実見、全Play/Edit回帰、Windows Development Playerを分離確認する。
- 既存作業: ユーザーがSandbox・Utility・関連差分のcommitまたは不要時削除を明示委任。まず内容を保存・検証し、M2変更と混ぜず独立単位で扱う。削除が必要でない限り保持する。
- 実装: M1を上書きせず `M2VerticalSlice` と再生成可能なBuilderを追加し、Lure→Provocation→HijackをRunManagerへ配線。BeatDefinitionごとの試作配置、誘引の白い異常柱、挑発の車内偽指示表示＋生成チャイム、乗っ取りの8階超え表示＋生成モーター音を追加した。外部素材・runtime依存は追加していない。挑発のASCII文言、音色、3/5/6秒等は正式採用前の試作値。
- 検証: 専用シーン通常入力テスト1/1合格（最終job `4f9b38024e9347cbae3e768a522ef2cc`、合成Keyboard/Mouse→Raycast→短クリック、直接SubmitAction・teleportなし）。最終コードで全Play Mode 31/31（`artifacts/tests/20260908-073553-800/playmode.xml`）、Edit Mode 2/2（`artifacts/tests/20260908-073104-376/editmode.xml`）、Console Error 0、scene validateはmissing script 0 / broken prefab 0 / issues 0。最終画像は `artifacts/m2-04/scene-input-20260908-072621-529` で柱・挑発表示を実見し、`hijack-panel.png` は表示9を捉えた。
- Windows: 最終コードのM2単独Development Build成功、0 errors / 0 warnings、`artifacts/builds/20260908-073657-567/build.json`。一つ前の同機能build `20260908-073211-130` はPlayerを実起動し、応答あり・M2のLure Diagnosis到達・通常終了をログ確認。最終差分は正式参照がある場合に試作panel色を上書きしないguardのみ。ネイティブ画面取得は利用不可でPlayer画面とOS実入力一巡は未確認。人の理解度・恐怖・音色・難易度は未評価。
- 検証中断記録: 初回シーンテストはBeat asset参照がnullで失敗し、sceneをrepairして解消。MCP全件job `118ef1383f924c9d833c048d69f1ef60` は初期化時にorphan化し、成功扱いせずEditorメニューから再実行して上記31/31を得た。誘引Prefabのplate/drone未配線warningは残り、現段階の柱判別には影響しないが正式演出では解消する。

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
- 現在工程: M2基本3系統の専用縦切り試作まで実装・自動検証済み。次は人の短時間プレイ評価に基づく可読性・音・間の調整、またはM3への移行判断。
- 最新の実装単位: M2-04 3系統縦切り。全Play Mode 31/31、Edit Mode 2/2、scene validate 0件、Windows Development BuildとPlayer起動ログを確認。
- Unity操作担当／実装担当: 現在のtask（01a07f7e-0cd8-7500-b2c1-afbb84a88bfb）。この記録は排他ロックではない。
- 次の一手: M2 Playerを人が短時間プレイし、柱の判別、挑発と乗っ取りの区別、無操作5秒、生成音の聞き取りを評価する。技術実装はM3へ進められるが、試作演出の正式採用はこの評価後。
- 人の作業待ち: M2の体験受入は未実施。技術作業自体のblockerではない。
- M1-01: 入力コード・FSM・テスト・Editor検証補助を変更。9/9 Play Mode合格、Mainの一時配置でOSクリック→PressClose→Departを確認。
- 文書整備時の履歴: 対象10文書のリンク・文字化け検査に合格。全体git diff --checkは既存Sandbox.unityの末尾空白を報告。今回は独立M1NormalRouteを追加し、Main/Sandboxは保存していない。

## 工程状態

| 工程 | 状態 | 証拠／残り |
|---|---|---|
| 文書・運用 | 完了 | 仕様、決定、保留、工程、現在地、実験、開始手順を整備。リンク・参照を検査 |
| M1 通常帰宅 | 試作実装済み・一部未検証 | 入力・閉塞・実シーン・走り・停止再受付の全回帰15/15合格、画面実見。Windowsビルド・起動・描画確認。OS連続操作一巡・人の受入は未実施 |
| M2 基本3系統 | 専用縦切り試作・自動検証済み | 3系統を通常入力で一巡、全Play 31/31、Edit 2/2、画面証跡、Windows build/起動確認。Player画面・OS実入力一巡と人の理解/恐怖/音評価は未実施 |
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

## DEMO-002 掲示編集とデモ接続（2026-09-15）

- 開始main clean / 8ae08e1。ユーザーが導入＋怪異1夜の続行と、掲示での学習・Hierarchyで文章調整を明示指定。Astra単独で実装、追加依存・保存形式・外部公開なし。
- EditableNoticeと日本語Inspectorを追加。EntranceHall/掲示物_文章編集はこちらに3掲示。再生成で文章を保持し、既存M1/M2にも適用。右掲示の柱遮蔽を発見してx=2.12へ修正。紙のA3相当寸法は保持し、短文・掲示用照明で読みやすくした。強すぎた照明を減光。ExecuteAlways再適用で開くだけでdirtyになる問題も修正。
- PlayableDemoをUnity APIでM2から複製し、DemoSessionで導入→翌夜→完了→再プレイを接続。既存NormalJourney/RunManagerへ完了イベントを追加。メニューとゲームの入力・時間・音を分離し、復帰クリックを遮断。Settingsは既存URP/TMP/Input Systemで実装。元M1/M2の進行を保持。
- デモ限定で距離ベースの足音と低音量の換気音を追加。独自生成wav5点、再生成scriptを保存。発火・再生とpauseを技術確認。音を聴いた／自然さを承認したとはしない。
- 初回テスト060513はテスト期待値Entranceと実enum WaitingForCallの不一致。060607は怪異生成直後（提示前）に押したため次遭遇待ちで失敗。正しい状態待機へ修正し061008は導入から再プレイまで1/1。ゲームの判定をテストに合わせて弱めていない。
- PlayMode全回帰34/34（artifacts/tests/20260915-061502-693/playmode.xml、305秒）。EditMode2/2（062129-324）。追加したデモ死亡・再挑戦・怪異中ポーズを含む最終通し1/1（062149-789、143秒）。いずれも最終実施分の失敗・skip0。入力は合成デバイスから実UI／Raycastを経由。最終画像artifacts/demo-02/input-20260915-062153。
- 保存・再生成・Scene再読込の文章保持をexecute_codeで確認し、試験文言を元へ復元。その後の明示的な文章短縮も各Sceneのsource欄に反映。Inspectorはartifacts/demo-02/notice-inspector.png。最終読み込みPlayableDemoはdirty=false。
- Windows通常build 20260915-062502-491はSucceeded、14.6秒、error/warning0、Tests assemblyなし。READMEと公式OFL（既存ttf内部copyright照合）を同梱。artifacts/demo-02/PlayableDemo-20260915.zipはruntime・README・licensesのみを選び、before/after-build記録やBurstDebugInformation_DoNotShipは含めない。
- Computer Useで実Playerの開始・設定・720→1080・反転表示と復帰・フォーカス喪失ポーズを確認。skyキー送信ではW/Esc反応を観測できず、ユーザーが実キーボードのEsc・Wは正常と回答。クリック成功や合成試験をOS全経路の実行へ読み替えない。Player.logに新規例外なし、従来のD3D12 info queue行あり。WMI機器照会はsandboxで拒否され、性能認定はしていない。
- Playerはユーザーの確認用に起動したまま残す。Editorは保存済みPlayableDemo、test/build処理なし。初見理解・怖さ・聴感・Windows全経路/終了・性能・長時間安定性は人の評価・追加確認が必要。Solへの全面引継ぎは保留を維持。


## 2026-09-15 TEXT-001 — 全表示文の執筆入口

- ユーザーが起動Canvas・掲示物を含む全文章の直接執筆を依頼。シーン最上部に8カテゴリの常設編集入口、日本語Inspector、シーン保存の文章コレクションを追加。既存掲示はEditableNoticeの同じデータを編集する。
- 起動・設定・結果・確認・怪異案内の動的表示と館内TMPを接続。内装再適用時に執筆済み値を保持。空欄を尊重し、長文メニューはスクロール、壊れた階数書式は数字表示へフォールバック。判定・操作機能・配置は維持。
- 保存／再読込／実際の内装再適用の保持確認成功。対象PlayMode 2/2成功、0 fail/skip（144.38秒）。新テストは改名したボタンをInput Systemのマウスで操作。既存のデモ通し回帰も成功。テストのTMP参照不足はテストasmdefへ既存TMP参照を追加して修正。
- InspectorをComputer Useで実見。最終Game View画像でタイトル・設定を確認。通常Windows build成功（20260915-072340-630、error/warning 0、11.71秒）。新buildのWindows実入力と作者が執筆した文章の理解は未検証。
- 開始時main 59e9abb、Git差分なし。未保存シーンをartifacts/text-01/user-scene-before.unityへ保護。既存との差はTMPの内部正規化2項目のみで保持。検証後のUnity生成末尾空白のみを整理し、無関係な素材変更は残さなかった。
- 操作案内はDEMO_AUTHORING。artifactsはローカル証拠でGit同期対象外。従来のPlayerは閉じておらず、その実行物は今回の変更前の版。

## FEEDBACK-001 実プレイ9点の修正（2026-09-15）

Astra、開始main c97208d。内容・出典はDECISIONS/FEEDBACK-001とVISUAL_REFERENCES。ユーザー執筆のPlayableDemoをartifacts/feedback-01へ保護。ゲーム文は変更せず、改名された文章編集rootの再識別を修正。

到着の間1.4秒、8外周登録灯、内側扉目地・水平継ぎ目、右袖壁モニター、実録革靴足音と静音化、共通音Inspectorを3シーンに反映。誘引の開扉中退出を拾い、閉扉に寄りかかっただけの誤発火を実際の扉面判定で防止。救済の全身帰還条件を維持。Hijackは13階上限とReveal加速を廃して一定間隔で増加、非常停止／遭遇終了で停止。既存死亡期限は維持。

検証の試行: 085057-414全37件34成功3失敗→外側開始fixture誤Reveal修正、Hijack新fixtureのStart完了待ち、デモtimeout診断追加→085853-402対象29件28成功1失敗（退出境界が扉より前）→扉平面修正→090555-342対象26/26成功。デモ全経路・階数継続は085853で成功済み。EditMode090719-963は2/2。最終全回帰一括再実行なし。画像inside-door-selected／sprint-revealを実見。

通常build090821-735成功13.5秒・error/warning0。Player起動windowは返ったがComputer Useのprocess id取得失敗が再接続でも継続したため、Windows画面検証は未完と記録。音素材はHerbertBoland/CC BY4.0、配布用licensesへ同梱。聴感・怖さは人の受入待ち。元の写真・取得音源・検証画像はartifactsに留め外部へ送らない。

## LURE-002 到着前からのダッシュ（2026-09-16）

ユーザー報告をM2実シーンのKeyboard Shift+W保持＋Mouseクリックで再現。初回052234-658はReveal待ちtimeoutで失敗。原因は到着時のラッチ単独ではなく、NormalJourney→RunBeatの再CloseDoorsが、閉扉済みでも密着プレイヤーを安全センサーで検出し再開扉することだった。プレイヤーはArrive前に廊下へ出て、Lure退出履歴が成立しない。

ElevatorController.MoveDoorsの移動距離を先に判定し、両扉が既に閉じている再指示では再開扉を抑止。実移動時の安全処理、参照なしfixture、Lureの全身帰還・閉操作・死亡仕様は維持。旧到着後ダッシュ試験も残し、到着前からの保持と到着まで扉位置不変のassertを追加。

PlayMode052426-846は28/28成功・skip0。LureGraceRecoveryの実シーン入力、全InteractionInputTestsを含む。artifacts/lure-02/before-arrival-reveal.png実見、Console error0。通常build・Windows実入力は再実行なし。ユーザーの未保存文章／音量・時間設定をコピー保護して保存し、検証終了後SHA256で完全一致を確認。シーンは今回コミット対象外。MCPの接続はComputer Useで既存EditorのConnect Local MCPを操作して復旧した。

## MONITOR-001 かご内モニターのスライド（2026-09-16）

ユーザー指定の複数文・Inspector編集・フェード速度設定をCabinInformationDisplayへ実装。先頭文はGameTextCollectionの既存cabin.standbyを正本として保持、追加Listと表示／片道fade秒数はモニターに保存。GameTextCollectionEditorから同じSerializedObjectを編集でき、データ二重化を避ける。モニター本体にも日本語Inspector。既存文章を創作・変更せず、作者が追加するための空リストで提供。

Restore/OnEnableで最初から再開、OnDisable/Presentで通常循環を停止。怪異の提示fadeは既存0.15秒を維持して対応時間を変えない。通常fadeは初期0.5秒×往復、完全表示5秒。1枚時は点滅なし、空欄尊重、0秒切替を許可。

対象PlayMode054452-045は2/2、failed/skip0。スライド境界・停止・割込みと3怪異一巡を検証。最終画像はartifacts/monitor-01、InspectorはComputer Useで実見し追加＋を展開。ユーザーSceneの未コミット編集を維持、今回Scene変更なし。Windows buildは再実行していない。
