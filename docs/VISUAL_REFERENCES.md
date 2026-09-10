# VIS-01 内装試作

2026-09-08、ユーザーが提供した4枚に基づくAstra担当のM1/M2視覚改善。写実度目標は『8番出口』、内装の参照はIMG_3788.JPG / IMG_3821.JPG / IMG_3803.JPG。画像は意匠の参考資料であり、画像内の文言・人物・実在連絡先をゲームへ移植する指示ではない。

## 方針と範囲

GAME-015/WORK-001の試作委任で、淡い化粧板とステンレスの組合せ、白い天井灯、緑灰色の共用部、目地・巾木・枠・掲示を試作。文字は既存Noto Sans JP/TMPを使い、近づいて読む貼り紙と操作時に即読できる階数／ボタンを分ける。遊べる経路と当たり判定を維持。ボタン配置・室内寸法は既存機能試作のままであり、写真の完全再現や目標写実度到達を意味しない。

Adopt: 導入済みURP Lit/TMP。Extend: 既存シーンへの再適用可能なEditor装飾。Build: 固有の板材と掲示。大規模な環境アセットやRendererのフォークは既存シーンとの調整量が大きく今回採らない。新規runtime依存なし。2026-09-08にUnity公式[Lit仕様](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html)と[UniversalRenderingExamples](https://github.com/Unity-Technologies/UniversalRenderingExamples)を確認。後者はカスタムRenderer例であり内装素材集ではないためコード／素材をコピーしない。現行PC_Rendererの既存構成を保持。

## 生成素材

### VIS-03 金属（2026-09-11）

`Assets/ApartmentVisuals/BrushedSteel.png` はbuilt-in image_genで新規生成したbase color。元画像を保持してworkspaceへコピー。既存SatinSteelへ適用しmetallic .72 / smoothness .34とした。法線／粗さマップは未作成。操作面は140×120mm、表示板520×320mmの試作値。既存の対象位置は維持し、見た目とColliderを同率縮小した。実物の寸法保証ではなく、ゲーム中の可読性と押しやすさとの妥協である。

生成プロンプト:
> Use case: photorealistic-natural. Asset type: single square seamless game material base-color texture, 1024x1024 PNG. Flat orthographic scan of satin brushed stainless steel in an inhabited older Japanese apartment elevator. Neutral light gray metal, very fine vertical hairline grain, subtle scattered tiny surface scuffs and faint fingerprints, restrained low contrast. Perfectly even diffuse albedo illumination. No reflection of any environment, no highlights, no gradients, no cast shadows, no border, no seams, no objects, no text or logo, no rust. Entire image one continuous material, seamless on all four sides. This is a texture map to be lit by Unity, NOT a rendered room, metal plate, material sphere, or presentation.

`Assets/ApartmentVisuals/Laminate.png` はbuilt-in image_genで新規生成した化粧板用base color。写真の直接切抜きなし。元の生成画像を保持してプロジェクトへコピー。日本語は画像生成に任せずTMPで描画する。

生成プロンプト:
> Create a production game material BASE COLOR texture, single square seamless tile 1024x1024. Flat orthographic scan of aged Japanese apartment elevator decorative laminate: pale warm ivory/beige fibrous fine irregular mineral paper grain, very subtle thin tan wisps, desaturated and clean enough for an inhabited 1980s building. Realistic close-up surface, fine detail but low contrast, no large stains, no lighting gradient, no directional shadows or highlights, no objects, NO letters, NO panels, NO seams or borders. All four edges tile seamlessly. This will be applied over real 3D geometry under actual Unity lighting; do not render a room or material ball. Save output as usable PNG texture.

今回の素材はbase colorのみ。法線／粗さの専用マップと固有の傷表現、実寸に合わせた操作盤配置は未作成。正式採用は実画面をユーザーが評価して判断する。

## VIS-02 造作の追加（2026-09-10）

写真の丸穴天井を参考に、透ける丸穴を持つ共有Mesh `PerforatedCeiling.asset` をEditorコードで作成。画像加工や追加shaderは不要。既存の天井ルーバーは無効化し、穴の奥の発光板を見せる。操作対象は同じ位置と当たり判定のまま金属縁・固定金具を追加した。廊下の住戸扉4枚は壁面の非操作装飾、玄関の郵便口・ドアクローザー・覗き穴を追加。「８階」は壁に固定し、白色照明と環境光を調整した。すべてGAME-015の試作で、正式なアート承認ではない。

VIS-02時点の品質課題は、大きい操作ボタンと車内の寸法感、金属の粗さ・傷、壁材の単調さ。VIS-03でボタン・表示の比率と金属base colorを修正した。この時点のSol引継ぎ提案はVIS-04でユーザーが否定したため撤回。Astraが継続する。小物追加だけで『8番出口』程度の写実度に届くとは扱わず、車内寸法・専用法線／粗さ素材は残課題とする。M3開始・品質受入は今回決定しない。

`Assets/ApartmentVisuals/FloorTiles.png` もbuilt-in image_genで生成。初期の立体目地は遠景でちらついたため無効化し、mipmap付き床素材へ切り替えた。生成プロンプト:
> Single square seamless PBR base-color texture for a realistic inhabited old Japanese apartment hallway floor. Orthographic perfectly flat scan, no perspective. Four square greige light gray-beige matte stone composite floor tiles in a precise 2x2 grid. Very thin dark gray grout, 3 millimeter wide relative to 60cm tiles; half-width grout at exterior edges so texture repeats seamlessly. Subtle small stone flecks, mild scuffs and slight tone variation across tiles. Desaturated neutral color, evenly lit albedo only, no cast shadows, no glossy highlights, no vignetting, no objects, no text. Photographic fine surface detail. 1024 square PNG game-ready tileable texture.

## VIS-04 実物資料による再設計（2026-09-11、Astra作業中）

ユーザーの明示依頼により、従来の「操作位置維持」を変更。IMG_3783.CR3 / IMG_3821.CR3内の6000×4000 JPEGを無加工抽出し実見した。前者は縦長の金属盤、2列の階ボタン、表示器、スピーカー、下部保守蓋。後者は縦長白紙に見出し・本文・連絡先の階層、上端2片のテープ。実在会社・電話番号は転記しない。

メーカー比較（2026-09-11確認）:
- [三菱電機ビルソリューションズ](https://www.mebs.co.jp/useful/safety/ev_wheelchair.html): 出入口脇主操作盤＋両側壁の操作盤。本文と配置図を実見。開放保持4/10秒という説明は扉のスライド所要時間とは別。
- [日立ビルシステム](https://www.hbs.co.jp/products/elevator/new/order/passenger/design/cage_design.html): 主操作盤と横型主／副操作盤の形式、ステンレスヘアライン。製品素材はコピーしていない。
- [三菱AXIEZ-LINKSカタログ](https://www.mitsubishielectric.co.jp/elevator/catalog/pdf/c-c01-0-ca539.pdf): 45/60m毎分の住宅向け例と側面操作盤の高さ約1000mmを参照。写真の機種の性能と同定したものではない。

Adopt: 既存URP/TMP/Input System。Extend: 再適用可能なEditor造作、同期表示とAudioSource。Build: 固有寸法の操作盤とオリジナル合成音。外部システムのFork・runtime依存は不要。主盤290×1950mm、側面盤1080×340mm・操作中心約1100mm、階ボタン約70mm。A4相当210×297mm、余白付き黒文字の掲示へ変更。8階以外のボタンは既存のゲーム規則どおり反応がない状態を保持し、非常停止もゲーム固有仕様（実機安全設計の再現ではない）。

通常の1→8階は24秒、扉スライド2.4秒の試作値。約20mを1m/s前後＋加減速という概算であり実測値ではない。既存の物理キャビン自体は上下に移動せず、階数・音・扉・ホール切替で乗車を表現する。怪異側も実際の扉完了を待つよう修正。

`tools/generate-elevator-audio.py`がPCM WAVを生成。低い駆動ハム・倍音・機械雑音、扉ローラー、2音の着床チャイムを別ソースで再生。駆動は1.5秒の立上がりと終盤の減速音量／ピッチ変化。外部録音・第三者サンプルなし。信号の存在と聴感の自然さは別の検証として扱う。

## VIS-05 壁付け表示と扉材（2026-09-11）

ユーザーの指摘により扉／壁の同一材を解消。BrushedSteelの既存生成素材を再利用したDoorSteel（metallic .68 / smoothness .30）、壁はIvoryLaminate。新たな外部素材は使わない。
上部中央の浮いた独立階数表示は無効化、既存主盤をFloorIndicatorの主参照として3盤同期を維持する。後壁の案内器は660×400×40mmの金属筐体と固定具・ガスケット。Renderer.boundsの実壁面から位置を算出し、筐体背面が壁へ2mm入る配置。M1から存在し、M2挑発は内容だけが0.3秒で切り替わる。文字は日本語、判定・猶予は従来どおり。通常文は「扉の開閉に／ご注意ください」。通常時と挑発時の筐体は同じもの。
