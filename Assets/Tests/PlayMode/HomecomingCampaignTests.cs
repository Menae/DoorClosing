#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GraduationProject.Tests
{
    public sealed class HomecomingCampaignTests
    {
        [UnityTest]
        public IEnumerator FixedNightsRandomRetryAndRuntimeCopies_PreserveAuthorAssets()
        {
            var savedRandom=UnityEngine.Random.state;
            var root=new GameObject("Campaign composition test");
            var campaign=root.AddComponent(GameAccess.Type("HomecomingCampaign"));
            object Call(string name)=>campaign.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(campaign,null);
            UnityEngine.Object[] Attempt()=>((IEnumerable)Call("CreateAttempt")).Cast<UnityEngine.Object>().ToArray();
            string Label(UnityEngine.Object definition)=>definition.GetType().GetProperty("DebugLabel").GetValue(definition).ToString();
            string Category(UnityEngine.Object definition)=>definition.GetType().GetProperty("Category").GetValue(definition).ToString();
            float Grace(UnityEngine.Object definition)=>(float)definition.GetType().GetProperty("GraceSeconds").GetValue(definition);
            var one=new[]{"lure","provocation","hijack"}.Select(n=>AssetDatabase.LoadMainAssetAtPath("Assets/Data/"+n+".asset")).ToArray();
            var two=new[]{"lure_wet","provocation_voice","hijack_space"}.Select(n=>AssetDatabase.LoadMainAssetAtPath("Assets/Data/"+n+".asset")).ToArray();
            var originals=one.Concat(two).ToArray(); var json=originals.Select(EditorJsonUtility.ToJson).ToArray();
            try
            {
                var so=new SerializedObject(campaign);
                foreach(var entry in new[]{("firstNight",one),("secondNight",two)})
                {
                    var list=so.FindProperty(entry.Item1); list.arraySize=3;
                    for(int i=0;i<3;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=entry.Item2[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                Call("AdvanceNight"); var first=Attempt();
                Assert.That(first.Select(Label),Is.EqualTo(one.Select(Label)));
                Assert.That(first.Select(Grace),Is.EqualTo(one.Select(Grace)));
                Call("AdvanceNight"); var second=Attempt(); yield return null;
                Assert.That(first.All(o=>o==null),Is.True,"Previous night copies released");
                Assert.That(second.Select(Label),Is.EqualTo(two.Select(Label)));
                for(int i=0;i<3;i++) Assert.That(Grace(second[i]),Is.EqualTo(Grace(two[i])*.9f).Within(.0001f));
                Call("AdvanceNight");
                var sequences=new System.Collections.Generic.HashSet<string>();
                var seen=new System.Collections.Generic.HashSet<string>();
                UnityEngine.Object[] previous=second;
                for(int seed=0;seed<32;seed++)
                {
                    UnityEngine.Random.InitState(seed); var attempt=Attempt();
                    Assert.That(attempt.Select(Category).Distinct().Count(),Is.EqualTo(3));
                    sequences.Add(string.Join("|",attempt.Select(Label))); foreach(var label in attempt.Select(Label)) seen.Add(label);
                    yield return null;
                    Assert.That(previous.All(o=>o==null),Is.True,"Retry releases old run definitions"); previous=attempt;
                }
                Assert.That(sequences.Count,Is.GreaterThan(10),"Both expression and order vary across seeded attempts");
                Assert.That(seen.Count,Is.EqualTo(6));
                Assert.That(originals.Select(EditorJsonUtility.ToJson),Is.EqualTo(json),"Author assets remain unchanged");
                UnityEngine.Object.Destroy(root); yield return null;
                Assert.That(previous.All(o=>o==null),Is.True,"Scene exit releases final definitions");
            }
            finally { UnityEngine.Random.state=savedRandom; if(root!=null) UnityEngine.Object.Destroy(root); }
        }
    }
}
#endif
