# GraduationProject：開発の入口

Unity 6000.3.14f1 / URP 17.3.0。日本語でやり取りする。Codexでこの保存済みローカルプロジェクトを開いて作業する。

## 人が普段すること

デモの起動・掲示の文章調整は [DEMO_AUTHORING.md](DEMO_AUTHORING.md)。`PlayableDemo` を開き、Hierarchyで `文章編集` を検索すると編集箇所へ進める。

開発を開始・再開するときは、次の一文でよい。

> AGENTS.mdとdocs/START_HERE.mdに従い、PROJECT_STATUSの現在地と最新の担当指定から、依頼済み・承認済みの開発を進めて。必要な実装、関連検証、不具合修正、記録、commit／pushまで任せます。人の判断が必要な部分は具体化して示し、独立して進められる作業は続けて。

「一旦停止」で中断、「続けて」で再開する。利用制限や強制終了の後はCodexが差分と実行中処理を照合する。自動継続・自動リセットは設定していない。人の体験評価が必要なときは、Codexが遊べる短いビルドと確認内容を用意する。

## 作業に応じた参照

入口は [AGENTS.md](../AGENTS.md) の権限・Git規約と [PROJECT_STATUS.md](PROJECT_STATUS.md) の現在地・担当。そこから今回の判断に必要な節を読む。小さな文書修正やツール復旧に仕様全文を読み直す必要はない。

| 今回の作業 | 読む資料・使うスキル |
|---|---|
| 次工程を選ぶ／ゲーム開発を引き継ぐ | PROJECT_STATUS、[ROADMAP](ROADMAP.md) の対象工程・依存。全体の経験や規則の関係が不明なら [GAME_SPEC](GAME_SPEC.md) 全体を確認 |
| ゲーム挙動を実装・変更する | GAME_SPECの対象規則と関係する例外、[DECISIONS](DECISIONS.md)／[OPEN_QUESTIONS](OPEN_QUESTIONS.md) の該当ID、実コード・シーン。unity-verified-developmentと [VALIDATION](VALIDATION.md) の必要な検証 |
| 見た目・音・体験を検討する | GAME_SPECの該当節、最新VIS決定、[VISUAL_REFERENCES](VISUAL_REFERENCES.md)、[PLAYTEST_PLAN](PLAYTEST_PLAN.md)。設計比較・人の評価にはhorror-design-review |
| Unity接続・環境の復旧 | [DEVELOPMENT](DEVELOPMENT.md) の該当手順、[KNOWN_ISSUES](KNOWN_ISSUES.md)。MCP操作にはunity-mcp-orchestrator。Windows実画面・実入力は [接続確認](DEVELOPMENT.md#windows-computer-useの接続確認) とインストール済みcomputer-useスキル |
| 文書・運用の修正 | 対象文書と参照元、関係するWORK決定。スキル編集にはskill-creator。Unity起動は不要 |
| 過去の不具合・証跡を調べる | [PROJECT_HISTORY](PROJECT_HISTORY.md) の対象工程、[ENVIRONMENT_REPORT](ENVIRONMENT_REPORT.md) は環境導入当時の履歴 |

見出しやIDを検索して必要な範囲から読み、依存先や矛盾が見つかったら広げる。同じ連続作業で変わっていない全文・成功ログを繰り返し出力しない。進捗の要約をゲーム仕様の代用にしない。

## 作業の完了と継続

開始時にPROJECT_STATUSへ対象・期待結果・変更範囲・完了に必要な検証を短く記録する。既存の依頼、ROADMAPと仕様で定まる範囲は確認し直さず進め、重要な新規判断だけを切り分ける。

完了は、依頼された変更に必要な検証と発見した回帰の修正、記録、WORK-005に沿った保存まで終えた状態。実装だけ・コンパイルだけを完成にしない。検証の選び方は [VALIDATION](VALIDATION.md#変更に応じた検証) を参照し、合格後の拡大・再実行は新しい変更・失敗・未解決の懸念がある場合に限る。

継続開発の依頼では、単位完了後も承認済み範囲の次へ進む。人の判断待ちは依存部分だけ止め、何を判断するかと必要な根拠を具体化する。対象全体の完了、明示停止、または独立して進められる作業がない実際のblockerで区切る。未検証・人の未受入は残したまま明記する。

## 利用枠を節約する運用 — WORK-003

- 基本の運用案はSol medium / Fast無効。**最新のユーザー指定を優先する。現在はVIS-004以降のAstra継続・全面引継ぎ保留が有効**で、工程の区切りだけでSolへ戻さない。文書はモデル設定を自動変更しない。
- 通常Sol運用で、異なる根拠のある仮説を2回検証しても進展がない場合、または状態・扉・期限・保存の整合性に具体的な難所が残る場合、CodexがAstra mediumへの引継ぎを準備する。権限・接続・人の設計判断待ちをモデル変更で解決すると扱わない。
- 引継ぎには対象仕様、再現条件、変更、試行と結果、未解決点、完了条件を残す。「この難所だけ解決して止める」は限定レビューを依頼された場合に使う。開発継続を任されているtaskを、初回実装や検証直前で止めるためには使わない。
- Lunaへの細分化、Astra常時監督、サブエージェント起動をこの運用から自動的に追加しない。モデル比較・節約効果は未実測。SolでもAstraでも同じ仕様・証拠・承認境界を使う。

## 記録の正本

- ゲームの必須規則はGAME_SPEC、決定理由と委任はDECISIONS、保留はOPEN_QUESTIONS。現在地はPROJECT_STATUS、過去の詳細はPROJECT_HISTORY。仕様を別の作業メモへ重複コピーしない。
- Gitは [AGENTS.mdのGit workflow](../AGENTS.md#git-workflow) に集約。通常commit／既存upstreamへのpushはWORK-005で委任済み。新規公開・リリース・破壊的操作は別の判断。
- artifactsはAssets外・Git対象外の証拠保存先。別PCへ自動同期されず、自動削除方針は未合意。

今回の運用整理の根拠と確認範囲は [2026-09-15運用監査](WORKFLOW_AUDIT_2026-09-15.md)。これはゲーム仕様やモデル設定の変更記録ではない。
