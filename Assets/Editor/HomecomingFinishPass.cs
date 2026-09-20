using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    public static class HomecomingFinishPass
    {
        [MenuItem("Tools/Unity Agent/Add Homecoming Finishing Details")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != HomecomingSceneBuilder.ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires saved Homecoming in Edit Mode.");
            var roots = scene.GetRootGameObjects();
            var fsm = roots.SelectMany(r => r.GetComponentsInChildren<BeatStateMachine>(true)).Single();
            if (fsm.GetComponent<CabinPowerPresentation>() != null) throw new InvalidOperationException("Already installed; preserve author settings.");
            var power = fsm.gameObject.AddComponent<CabinPowerPresentation>();
            var so = new SerializedObject(power);
            so.FindProperty("cabinLight").objectReferenceValue = roots.Single(r => r.name == "Building").transform.Find("CeilingLight").GetComponent<Light>();
            so.ApplyModifiedPropertiesWithoutUndo();
            var visual = roots.Single(r => r.name == "HomeCorridor").transform.Find("InteriorVisuals");
            var sixth = UnityEngine.Object.Instantiate(visual.Find("Residence1_-8.2").gameObject, visual).transform;
            sixth.name = "Residence806"; sixth.position = new Vector3(1.578f, 0, -10.4f);
            var copyRoot = new GameObject("14_住戸番号", typeof(GameTextCollection));
            copyRoot.transform.SetParent(roots.Single(r => r.name == "テキスト編集").transform, false);
            var copy = copyRoot.GetComponent<GameTextCollection>();
            string[] names = { "Residence-1_-4.0", "Residence1_-4.0", "Residence-1_-8.2", "Residence1_-8.2", "Residence806" };
            string[] numbers = { "801", "802", "803", "804", "806" };
            for (int i = 0; i < names.Length; i++)
            {
                var door = visual.Find(names[i]);
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube); plate.name = "室番号プレート"; plate.transform.SetParent(door, false);
                plate.transform.localPosition = new Vector3(0, 1.9f, -.027f); plate.transform.localScale = new Vector3(.17f, .065f, .009f);
                UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
                plate.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/ApartmentVisuals/SatinSteel.mat");
                var label = new GameObject("室番号_" + numbers[i], typeof(TextMeshPro)).GetComponent<TextMeshPro>(); label.transform.SetParent(door, false);
                label.transform.localPosition = new Vector3(0, 1.9f, -.034f); label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
                label.fontSize = .28f; label.color = new Color(.04f, .05f, .045f); label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(.15f, .055f);
                copy.Add("residence." + numbers[i], numbers[i] + "の室番号", numbers[i], label);
            }
            copy.Apply(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            HomecomingCreditsAuthoring.Install();
        }
    }
}
