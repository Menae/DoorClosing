---
name: unity-verified-development
description: Implement or debug this Unity project and verify affected behavior in the Editor or Windows Player.
---

# Unity verified development

このプロジェクトのUnity実装・回帰調査を、承認済み仕様と再現可能な証拠につなぐ。委任・継続・保存の境界は [AGENTS.md](../../../AGENTS.md)。Sceneの保存値がscript既定値を上書きすることに注意する。

## 必要な経路だけ読む

- 期待結果と検証選択: [VALIDATION.md](../../../docs/VALIDATION.md#変更に応じた検証) と対象シナリオ。関連するGAME_SPEC／決定から期待結果を導く。
- MCP操作・接続・復旧: [DEVELOPMENT.md](../../../docs/DEVELOPMENT.md)。Unity MCPを優先し、非表示なら既存SDK clientを使う。操作前に実instance・project path・scene・Editor状態と必要なtool schemaを確認する。
- 画面／Windows実入力: VALIDATIONの「画面・操作の証拠」「Windows Player」とDEVELOPMENTの「Windows Computer Useの接続確認」。対象windowと最終Game Viewを確認する。

## このプロジェクトで間違えやすい境界

- 現行入力はInput System → Raycast → **click → commit**。旧holdは履歴。直接SubmitAction／debug正解入力ではこの経路を検証できない。合成デバイスとOS入力を区別し、失敗時もfixtureのデバイス・設定・時間倍率・生成物・購読を戻す。
- UI・post-processing込みの最終画面を実見する。Camera単体・Scene View・`-nographics`はURPの最終外観の証明にならない。撮影条件・色空間を確認し、撮影の都合でゲームの照明を変えない。
- 同じprojectで別Editor／batchを起動せず、未保存シーンを保護する。import／compile／reload後のConsoleを確認する。テスト用fixtureと製品buildのdebug除外はVALIDATIONに従う。
- 必要な検証、そこで判明した回帰修正、証拠・限界の記録まで進める。合格済み検証の拡大・反復は新たな変更や懸念があるときだけ行う。技術検証と人の知覚・怖さの受入は別。
