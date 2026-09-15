# GraduationProject

Communicate in Japanese. Existing first-person atmospheric horror for Windows; Unity 6000.3.14f1 / URP 17.3.0.

## Working agreement

- 開始・再開時は [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) の現在地・担当とGit差分を確認し、[docs/START_HERE.md](docs/START_HERE.md) の目的別案内から必要な資料を読む。仕様全文や過去ログの一律読込は不要。ゲーム挙動を変えるときはGAME_SPECの関連規則・例外とDECISIONS／OPEN_QUESTIONSを照合する。
- 2026-09-07の仕様基準版と最新の明示決定を守る。WORK-001/002により承認済み範囲の可逆的実装・環境整備・検証・記録は委任済み。GAME-015の具体演出・数値は試作できるが、正式採用・怖さの評価とは分ける。
- 依頼範囲の実装、関連検証、発見した回帰の修正、記録、WORK-005の保存まで継続する。初回実装や単位ごとの報告を継続確認のゲートにしない。新しい懸念がなければ合格済みの検証を広げず次へ進む。ユーザーの停止指示は直ちに優先する。
- 質問は既存の決定・委任で解けない重大な判断に限る。未承認のゲーム体験・必須範囲、エンジン／描画基盤移行、大型置換、新依存・サービス・保存形式、破壊的変更、費用・公開・アクセス変更は影響を具体化して確認する。判断には利用可能なuser-inputツール、アクセスには実際の承認機構を使う。人の評価待ちは依存する範囲だけ止める。
- 編集前にPROJECT_STATUSへ目的／工程、担当task、変更範囲、期待結果、完了条件と検証を簡潔に記録し、意味のある単位と停止時に更新する。中断した処理は結果を確認してから再試行する。過去の詳細はPROJECT_HISTORYへ残す。
- 同checkoutの実装・Unity操作は1担当。他の担当が稼働中なら所有を確認してから編集し、経過時間だけで引き継がない。PROJECT_STATUSは排他ロックではない。別taskは読み取りレビュー可。
- モデル運用はSTART_HEREのWORK-003を参照し、最新の明示的な担当指定を優先する。モデル設定変更・サブエージェント起動の承認をこの規約から推測しない。スキルの一般手順で既存のユーザー委任を狭めない。

## Unity workflow

- Unity実装・不具合調査にはunity-verified-developmentを使い、[docs/VALIDATION.md](docs/VALIDATION.md) から変更に必要な検証を選ぶ。MCP接続・復旧は [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)。文書のみの編集にEditor起動・ビルドは不要。
- Unity MCPを優先し、操作前に接続先project・scene・未保存状態を確認する。同じprojectに別Editor／batchを起動しない。別worktreeには独立Libraryと明示ルーティングが必要。
- .meta GUID、ユーザーの未保存シーン、Play変更を保護する。シーン／Prefabは実用的な範囲でUnity APIを使い、serialized fileの広範囲書換えを避ける。変更後のimport／compile／domain reloadとConsoleを確認する。
- 通常入力はInput System → Raycast → click → commit。Debug直接呼出しは代用にならない。見た目はUI・post-processingを含む最終Game View／Playerを実見する。テスト／debugコードは通常製品ビルドから除外する。
- 前面Computer Useは事前に知らせ、対象windowを確認する。[Windows接続手順](docs/DEVELOPMENT.md#windows-computer-useの接続確認)に従い、`cua_repl`の制限だけでWindows plugin全体の不可と判断しない。実際の権限拒否は迂回しない。

## Git workflow
- 作業開始・再開時は `git status --short`、現在ブランチ、対象のunstaged/staged差分を確認する。既存変更と今回の変更を区別し、由来不明の差分を混ぜない。
- 単独開発では工程ごとのブランチ・worktree作成を必須にしない。既存ブランチを無断で切り替えない。分離が必要で作成が依頼・委任された場合は `codex/<短い作業名>` を使い、Unityを停止し未保存・未コミット作業の保全を確認してからcheckoutを変更する。
- WORK-005の明示委任により、機能・修正・文書整備など意味のある単位の関連検証が完了したら、Codexの判断で確認を挟まず自動コミットする。行数ではなく再現可能な作業単位を基準にし、変更コード・関連テスト・必要な.meta・進捗文書を揃える。長い未完作業を中断する場合は自分の変更をWIPコミットしてよいが、失敗・未検証範囲を記録し完成扱いしない。
- ステージは対象ファイルまたはhunkを明示する。`git add .` / `git add -A` による一括追加は使わない。既存ステージを勝手に解除・コミットせず、同じファイルに他の変更が混在し分離できない場合は対象を確認する。
- コミット前はstaged差分、`git diff --cached --check`、関連検証結果を確認する。短いメッセージに工程IDと目的を含める（例: `fix(M1): prevent duplicate button commits`）。後でcommit IDと残る未コミット変更を報告する。
- Assetsの追加・移動は対応する.metaと一組にしGUIDを保つ。Library/Temp/ビルド/認証情報は追加しない。artifactsは現行.gitignoreどおりローカル保持し、Gitに保存・バックアップ済みとは扱わない。LFS・改行規則は維持し、無断の移行や全体正規化をしない。
- 検証済みの工程の区切り、または作業終了・引継ぎ時に未送信の完成コミットがあれば、既存の設定済みupstreamへ確認を挟まず通常pushする（WORK-005）。送信対象の全コミット、接続先・ブランチ、ahead/behind、機密情報・意図しない配布物・自動デプロイの有無を確認する。upstream不明、未確認の既存コミット混在、分岐・拒否・認証失敗時はローカルコミットを保持して理由を報告し、宛先の推測・強制送信をしない。WIPは完成コミットと区別し、自動pushの対象に混ざる場合は先に検証・解消する。
- 未保存作業がある状態のpull/mergeを避け、自動stashや暗黙のrebaseを使わない。競合は一括ours/theirsで消さず、意図と再検証範囲を確認する。
- 通常のcommit/pushはWORK-005で継続承認済み。新規リモート・公開範囲変更、PR送信・リリース・デプロイは別途明示承認を得る。reset --hard、clean、変更を捨てるrestore/checkout、ブランチ削除、amend/rebase等の履歴書換え、force-pushも対象と影響を示して明示承認を得る。アクセス権限は実際の承認機構に従う。
- GitHub認証はWindows Credential Managerを使い、トークンを環境変数や平文へコピーしない。sandbox内の認証失敗だけで再ログインせず、必要な権限で通常ホストの `gh auth status --hostname github.com` を確認する。

## Completion report

成果・変更ファイル、実際の検証と証拠、失敗／skip／未検証、commit／pushと残る差分を短く報告する。実装・コンパイル・自動テスト・実画像・Player・人の評価を区別し、技術検証から面白さ・怖さを認定しない。証拠はAssets外のartifactsへ再現条件付きで保存し、合意した方針なしに自動削除しない。
