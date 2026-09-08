# GraduationProject

Communicate in Japanese. Existing first-person atmospheric horror for Windows; Unity 6000.3.14f1 / URP 17.3.0.

## Working agreement
- At every task start, resume, or handoff, read docs/START_HERE.md, docs/PROJECT_STATUS.md, the full docs/GAME_SPEC.md, docs/ROADMAP.md, and relevant docs/DECISIONS.md / docs/OPEN_QUESTIONS.md. Conversation memory is not the shared project record.
- The 2026-09-07 design baseline is accepted for documentation and development planning. Follow its requirements; prototype presentation and numeric tuning remain provisional until human evaluation. See WORK-001 for the user's Codex-led implementation delegation and its limits.
- Before implementation, record the active roadmap task, owner/session, intended player behavior, affected files and checks in docs/PROJECT_STATUS.md. Update it after each meaningful unit and before stopping. Never mark untested work complete. Record interrupted operations before retrying them.
- Use a single implementation/Unity operator in this checkout. PROJECT_STATUS.md is a coordination record, not an atomic lock. If another task is active, verify ownership before mutations; do not steal ownership based only on elapsed time. Other chats can review read-only.
- Continue the requested development scope through routine reversible steps without asking after every unit. Ask only for material unapproved design, severe consequences, or necessary human feedback. User pause instructions take immediate precedence.
- Read docs/GAME_SPEC.md, docs/DECISIONS.md and relevant docs/OPEN_QUESTIONS.md before changing behavior.
- On 2026-09-06 the user delegated routine, reversible engineering decisions for this development environment. Proceed and verify without repeated approval; report choices and practical effects. Do not invent the game's intended experience.
- Confirm changes with severe consequences or expensive reversal: engine/render-pipeline migration, replacing major game systems, destructive data/history changes, paid commitments, public release, or broad access/security changes. Explain consequences in plain Japanese. Use the available user-input tool for design choices and the actual permission mechanism for access grants.
- Keep approved requirements, observed implementation and hypotheses separate. A recommendation, AI agreement or unanswered question is not a user decision.
- Preserve user work. Inspect status and diff first; do not reset, clean, stash or commit their work incidentally.
- Follow docs/START_HERE.md "利用枠を節約する運用 — WORK-003": Sol medium is the normal operating recommendation; Codex owns escalation diagnosis and prepares a concise handoff when Astra is needed. Do not make the user classify every task. Keep required verification and approvals; prefer brief reports. This does not automatically change model settings or authorize subagents.

## Unity workflow
- Prefer Unity MCP. Read docs/DEVELOPMENT.md for startup, recovery and the SDK client fallback when native tools are absent.
- Identify the project and scene before mutations. Wait for import/compilation/domain reload; check Console after changes.
- Preserve .meta GUIDs. Use Unity APIs/MCP for scenes and prefabs where practical; never broadly rewrite serialized files or unintentionally save Play changes.
- One operator per Editor. Do not open the same project in another Editor/batch process. Separate worktrees need separate Library folders and explicit routing.
- Follow docs/VALIDATION.md and unity-verified-development. Distinguish compilation, logic tests, simulated input, actual visual inspection, Windows Player checks and human playtests.
- Direct SubmitAction calls and debug-correct keys do not verify normal Input System → Raycast → click → commit behavior. Existing hold-based tests describe the old implementation, not the approved click specification.
- Keep test/debug code out of ordinary release builds. Do not incidentally add production services, telemetry or runtime dependencies.
- Save generated evidence outside Assets in artifacts/. Record scene, code version, input path, resolution and state. No automatic evidence deletion without an agreed policy.
- Announce Computer Use foreground control. Check the selected Unity/Player window and final Game View composition, not just a camera-only render.
- For Windows Computer Use, follow docs/DEVELOPMENT.md "Windows Computer Useの接続確認" and the installed computer-use skill: discover `mcp__node_repl__js`, initialize `@oai/sky`, then enumerate windows. An empty `cua_repl` app list or its native-API restriction does not establish that the separate Windows plugin is unavailable. Check the supported plugin route before reporting a blocker; never bypass an actual permission denial.

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
Report changed files, actual checks and evidence, failures/skips and limitations. Never call a timed-out, unavailable or merely compiled check passed. Technical verification cannot prove fun or fear.
