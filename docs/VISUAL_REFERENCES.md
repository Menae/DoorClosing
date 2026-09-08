# VIS-01 内装試作

2026-09-08、ユーザーが提供した4枚に基づくAstra担当のM1/M2視覚改善。写実度目標は『8番出口』、内装の参照はIMG_3788.JPG / IMG_3821.JPG / IMG_3803.JPG。画像は意匠の参考資料であり、画像内の文言・人物・実在連絡先をゲームへ移植する指示ではない。

## 方針と範囲

GAME-015/WORK-001の試作委任で、淡い化粧板とステンレスの組合せ、白い天井灯、緑灰色の共用部、目地・巾木・枠・掲示を試作。文字は既存Noto Sans JP/TMPを使い、近づいて読む貼り紙と操作時に即読できる階数／ボタンを分ける。遊べる経路と当たり判定を維持。ボタン配置・室内寸法は既存機能試作のままであり、写真の完全再現や目標写実度到達を意味しない。

Adopt: 導入済みURP Lit/TMP。Extend: 既存シーンへの再適用可能なEditor装飾。Build: 固有の板材と掲示。大規模な環境アセットやRendererのフォークは既存シーンとの調整量が大きく今回採らない。新規runtime依存なし。2026-09-08にUnity公式[Lit仕様](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html)と[UniversalRenderingExamples](https://github.com/Unity-Technologies/UniversalRenderingExamples)を確認。後者はカスタムRenderer例であり内装素材集ではないためコード／素材をコピーしない。現行PC_Rendererの既存構成を保持。

## 生成素材

`Assets/ApartmentVisuals/Laminate.png` はbuilt-in image_genで新規生成した化粧板用base color。写真の直接切抜きなし。元の生成画像を保持してプロジェクトへコピー。日本語は画像生成に任せずTMPで描画する。

生成プロンプト:
> Create a production game material BASE COLOR texture, single square seamless tile 1024x1024. Flat orthographic scan of aged Japanese apartment elevator decorative laminate: pale warm ivory/beige fibrous fine irregular mineral paper grain, very subtle thin tan wisps, desaturated and clean enough for an inhabited 1980s building. Realistic close-up surface, fine detail but low contrast, no large stains, no lighting gradient, no directional shadows or highlights, no objects, NO letters, NO panels, NO seams or borders. All four edges tile seamlessly. This will be applied over real 3D geometry under actual Unity lighting; do not render a room or material ball. Save output as usable PNG texture.

今回の素材はbase colorのみ。法線／粗さの専用マップと固有の傷表現、写真どおりの穿孔天井、実寸に合わせた操作盤配置は未作成。正式採用は実画面をユーザーが評価して判断する。

`Assets/ApartmentVisuals/FloorTiles.png` もbuilt-in image_genで生成。初期の立体目地は遠景でちらついたため無効化し、mipmap付き床素材へ切り替えた。生成プロンプト:
> Single square seamless PBR base-color texture for a realistic inhabited old Japanese apartment hallway floor. Orthographic perfectly flat scan, no perspective. Four square greige light gray-beige matte stone composite floor tiles in a precise 2x2 grid. Very thin dark gray grout, 3 millimeter wide relative to 60cm tiles; half-width grout at exterior edges so texture repeats seamlessly. Subtle small stone flecks, mild scuffs and slight tone variation across tiles. Desaturated neutral color, evenly lit albedo only, no cast shadows, no glossy highlights, no vignetting, no objects, no text. Photographic fine surface detail. 1024 square PNG game-ready tileable texture.
