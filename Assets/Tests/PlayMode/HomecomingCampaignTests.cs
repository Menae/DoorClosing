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
        public IEnumerator NormalStopLottery_AllSixQueueAndReset_PreserveRegularQuota()
        {
            var random=UnityEngine.Random.state;
            var root=new GameObject("Extra encounter composition test");
            var campaign=root.AddComponent(GameAccess.Type("HomecomingCampaign"));
            object Call(string name)=>campaign.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(campaign,null);
            int Count(string name)=>(int)campaign.GetType().GetProperty(name).GetValue(campaign);
            var originals=new[]{"lure","provocation","hijack","lure_wet","provocation_voice","hijack_space"}
                .Select(n=>AssetDatabase.LoadMainAssetAtPath("Assets/Data/"+n+".asset")).ToArray();
            var json=originals.Select(EditorJsonUtility.ToJson).ToArray();
            try
            {
                var setup=new SerializedObject(campaign);
                for(int night=0;night<2;night++)
                {
                    var list=setup.FindProperty(night==0?"firstNight":"secondNight");list.arraySize=3;
                    for(int i=0;i<3;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=originals[night*3+i];
                }
                setup.ApplyModifiedPropertiesWithoutUndo();
                for(int i=0;i<20;i++) Call("DrawForNormalStop");
                Assert.That(Count("NormalStopDraws"),Is.Zero,"Introduction never draws");
                Call("AdvanceNight"); campaign.GetType().GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(campaign,new System.Random(31005));
                for(int i=0;i<6000;i++) Call("DrawForNormalStop");
                Assert.That(Count("NormalStopDraws"),Is.EqualTo(6000));
                int extraCount=Count("PendingExtras"); Assert.That(extraCount,Is.InRange(1750,2250));
                var regular=((IEnumerable)Call("CreateAttempt")).Cast<UnityEngine.Object>().ToArray();
                Assert.That(regular.Length,Is.EqualTo(3),"Extras cannot consume regular quota");
                Assert.That(Count("PendingExtras"),Is.EqualTo(extraCount),"Initial travel extras survive attempt composition");
                var counts=new System.Collections.Generic.Dictionary<string,int>();
                UnityEngine.Object last=null;
                while(Count("PendingExtras")>0)
                {
                    last=(UnityEngine.Object)Call("TakePendingExtra");
                    string label=last.GetType().GetProperty("DebugLabel").GetValue(last).ToString();
                    counts[label]=counts.TryGetValue(label,out int value)?value+1:1;
                }
                Assert.That(counts.Count,Is.EqualTo(6));
                foreach(var count in counts.Values) Assert.That(count,Is.InRange(240,430),"Seeded uniform candidate distribution");
                Assert.That(Call("TakePendingExtra"),Is.Null);
                Call("DiscardPendingExtras"); Assert.That(Count("NormalStopDraws"),Is.Zero);
                var retry=((IEnumerable)Call("CreateAttempt")).Cast<UnityEngine.Object>().ToArray();
                yield return null; Assert.That(last==null&&regular.All(o=>o==null),Is.True,"Retry releases extra and regular copies");
                for(int i=0;i<50;i++) Call("DrawForNormalStop");
                Call("AdvanceNight"); Assert.That(Count("PendingExtras"),Is.Zero); Assert.That(Count("NormalStopDraws"),Is.Zero);
                Assert.That(originals.Select(EditorJsonUtility.ToJson),Is.EqualTo(json));
            }
            finally { UnityEngine.Random.state=random; UnityEngine.Object.Destroy(root); }
        }

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
                    campaign.GetType().GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(campaign,new System.Random(seed)); var attempt=Attempt();
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
