using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    public static class GameTextAuthoring
    {
        public const string RootName = "★文章編集_ここから";
        [MenuItem("Tools/Unity Agent/文章編集を開く")]
        public static void Open()
        {
            var root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (root == null) { Install(); root = SceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == RootName); }
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before editing scene copy.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PlayableDemoBuilder.ScenePath && scene.path != M1NormalRouteBuilder.ScenePath && scene.path != M2VerticalSliceBuilder.ScenePath)
                throw new InvalidOperationException("Open PlayableDemo, M1NormalRoute or M2VerticalSlice first.");
            var roots = scene.GetRootGameObjects();
            var root = roots.FirstOrDefault(g => g.name == RootName);
            if (root == null) { root = new GameObject(RootName, typeof(GameTextCollection)); Undo.RegisterCreatedObjectUndo(root, "文章編集を配置"); }
            root.transform.SetAsFirstSibling();
            GameTextCollection Group(string name)
            {
                var child = root.transform.Find(name);
                if (child == null) { var go = new GameObject(name, typeof(GameTextCollection)); go.transform.SetParent(root.transform, false); child = go.transform; Undo.RegisterCreatedObjectUndo(go, "文章編集を配置"); }
                var group = child.GetComponent<GameTextCollection>(); Undo.RecordObject(group, "文章編集を更新"); return group;
            }
            var components = roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
            string Read(Component c, string field, string fallback) => c == null ? fallback : new SerializedObject(c).FindProperty(field)?.stringValue ?? fallback;
            TMP_Text View(Component c, string field) => c == null ? null : new SerializedObject(c).FindProperty(field)?.objectReferenceValue as TMP_Text;
            T One<T>() where T : Component => components.OfType<T>().FirstOrDefault();
            if (One<DemoSession>() != null)
            {
            Group("03_翌夜・結果").Add("menu.NextNight.1", "翌夜の見出し", "翌夜");
            Group("03_翌夜・結果").Add("menu.NextNight.2", "翌夜の本文", "いつもの８階へ、帰りましょう。\n気になるときは、入口の掲示を読み返せます。");
            Group("03_翌夜・結果").Add("menu.NextNight.3", "夜を始めるボタン", "夜を始める");
            Group("03_翌夜・結果").Add("menu.AfterHome.1", "クリア見出し", "帰宅しました");
            Group("03_翌夜・結果").Add("menu.AfterHome.2", "クリア本文", "デモはここまでです。\nお疲れさまでした。");
            Group("03_翌夜・結果").Add("menu.AfterHome.3", "再プレイボタン", "最初から遊ぶ");
            Group("03_翌夜・結果").Add("menu.AfterHome.4", "終了ボタン", "終了");
            Group("03_翌夜・結果").Add("menu.Restart.1", "読み込み表示", "読み込み中");
            Group("01_起動画面").Add("menu.ShowTitle.1", "タイトル", "帰宅 / プレイアブルデモ");
            Group("01_起動画面").Add("menu.ShowTitle.2", "導入本文", "あなたの自宅は８階です。\nまずは普段どおりに帰り、廊下の様子を覚えてください。\n\n乗る前に、エレベーター横の注意書きをご確認ください。");
            Group("01_起動画面").Add("menu.ShowTitle.3", "操作説明", "WASD：移動　Shift：走る　マウス：視点\n左クリック：操作　Esc：一時停止");
            Group("01_起動画面").Add("menu.ShowTitle.4", "開始ボタン", "はじめる");
            Group("01_起動画面").Add("menu.ShowTitle.5", "設定ボタン", "設定");
            Group("01_起動画面").Add("menu.ShowTitle.6", "終了ボタン", "終了");
            Group("02_一時停止・確認").Add("menu.ShowPause.1", "ポーズ見出し", "一時停止");
            Group("02_一時停止・確認").Add("menu.ShowPause.2", "操作・掲示の説明", "WASD：移動　Shift：走る　左クリック：操作\n掲示は入口のエレベーター横にあります。");
            Group("02_一時停止・確認").Add("menu.ShowPause.3", "再開ボタン", "再開");
            Group("02_一時停止・確認").Add("menu.ShowPause.4", "設定ボタン", "設定");
            Group("02_一時停止・確認").Add("menu.ShowPause.5", "やり直しボタン", "最初からやり直す");
            Group("02_一時停止・確認").Add("menu.ShowPause.6", "やり直し確認：見出し", "最初からやり直しますか？");
            Group("02_一時停止・確認").Add("menu.ShowPause.7", "やり直し確認：本文", "今回の進行は失われ、通常の帰宅から始まります。");
            Group("02_一時停止・確認").Add("menu.ShowPause.8", "やり直し確認：決定", "やり直す");
            Group("02_一時停止・確認").Add("menu.ShowPause.9", "やり直し確認：戻る", "戻る");
            Group("02_一時停止・確認").Add("menu.ShowPause.10", "終了ボタン", "終了");
            Group("02_一時停止・確認").Add("menu.ShowPause.11", "終了確認：見出し", "ゲームを終了しますか？");
            Group("02_一時停止・確認").Add("menu.ShowPause.12", "終了確認：本文", "このデモは進行を保存しません。");
            Group("02_一時停止・確認").Add("menu.ShowPause.13", "終了確認：決定", "終了する");
            Group("02_一時停止・確認").Add("menu.ShowPause.14", "終了確認：戻る", "戻る");
            Group("04_設定画面").Add("menu.ShowSettings.1", "設定見出し", "設定");
            Group("04_設定画面").Add("menu.ShowSettings.2", "感度の項目名", "視点感度");
            Group("04_設定画面").Add("menu.ShowSettings.3", "上下反転の接頭辞", "上下反転：");
            Group("04_設定画面").Add("menu.ShowSettings.4", "視野角の項目名", "視野角");
            Group("04_設定画面").Add("menu.ShowSettings.5", "明るさの項目名", "明るさ");
            Group("04_設定画面").Add("menu.ShowSettings.6", "音量の項目名", "音量");
            Group("04_設定画面").Add("menu.ShowSettings.7", "画面切替ボタン", "全画面／ウィンドウを切り替える");
            Group("04_設定画面").Add("menu.ShowSettings.8", "720pボタン", "表示サイズ：1280 × 720");
            Group("04_設定画面").Add("menu.ShowSettings.9", "1080pボタン", "表示サイズ：1920 × 1080");
            Group("04_設定画面").Add("menu.ShowSettings.10", "設定の説明", "設定は今回のプレイ中に適用されます。");
            Group("04_設定画面").Add("menu.ShowSettings.11", "戻るボタン", "戻る");
                Group("04_設定画面").Add("settings.on", "上下反転：有効", "オン");
                Group("04_設定画面").Add("settings.off", "上下反転：無効", "オフ");
            }
            var dynamic = Group("05_案内・怪異・階数");
            var cabin = One<CabinInformationDisplay>();
            var run = One<RunManager>();
            var floor = One<FloorIndicator>();
            dynamic.Add("cabin.standby", "かご内ご案内：待機中", Read(cabin, "standbyMessage", ""), View(cabin, "content"));
            dynamic.Add("floor.format", "現在階の表示書式", Read(floor, "displayFormat", "{0}"));
            dynamic.Add("run.clear", "帰宅時の表示", Read(run, "clearMessage", "帰宅しました"), View(run, "nightClearText"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Anomalies/Standin_Provocation.prefab");
            var provocation = prefab != null ? prefab.GetComponent<ProvocationAnomaly>() : null;
            dynamic.Add("provocation.normal", "停止を誘う放送：通常", Read(provocation, "normalAnnouncement", "安全確認のため\n非常停止を\n押してください"));
            dynamic.Add("provocation.revealed", "停止を誘う放送：正体判明後", Read(provocation, "revealedAnnouncement", "押して。押して。\n押して。"));
            dynamic.Add("lure.plate", "偽廊下の表札（表札を使用する構成のみ）", "708");
            dynamic.Add("lure.broken", "偽廊下の表札：正体判明後", "7#8");
            dynamic.Add("normal.plate", "通常到着の表札（表札を使用する構成のみ）", "703");
            var excluded = new System.Collections.Generic.HashSet<TMP_Text>();
            foreach (var c in components.Where(c => c is FloorIndicator || c is CabinInformationDisplay || c is RunManager))
            {
                var so = new SerializedObject(c); var prop = so.GetIterator();
                while (prop.Next(true)) if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue is TMP_Text text) excluded.Add(text);
            }
            foreach (var text in components.OfType<TMP_Text>())
            {
                if (excluded.Contains(text) || text.GetComponentInParent<EditableNotice>() != null) continue;
                string path = text.name;
                for (var parent = text.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
                string category = path.StartsWith("Building/") ? "06_操作盤・かご内表示" : "07_ロビー・廊下・その他表示";
                Group(category).Add("scene." + path, path, text.text, text);
            }
            Group("08_掲示物の見出し・本文").notices = components.OfType<EditableNotice>().ToArray();
            var ordered = root.transform.Cast<Transform>().OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < ordered.Length; i++) ordered[i].SetSiblingIndex(i);
            foreach (var group in root.GetComponentsInChildren<GameTextCollection>()) { group.Apply(); EditorUtility.SetDirty(group); }
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
