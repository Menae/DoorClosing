using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GraduationProject.Tests.Editor
{
    public class SerializationTests
    {
        private static Assembly GameAssembly => AppDomain.CurrentDomain.GetAssemblies()
            .Single(a => a.GetName().Name == "Assembly-CSharp");

        [Test]
        public void ProjectBehaviours_HaveNoDuplicateSerializedFieldNames()
        {
            var collisions = new List<string>();
            foreach (var type in GameAssembly.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(t)))
            {
                var names = new HashSet<string>();
                for (var ancestor = type; ancestor != null && ancestor != typeof(MonoBehaviour); ancestor = ancestor.BaseType)
                foreach (var field in ancestor.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsDefined(typeof(NonSerializedAttribute))) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField)) && !field.IsDefined(typeof(SerializeReference))) continue;
                    if (!names.Add(field.Name)) collisions.Add(type.Name + "." + field.Name);
                }
            }
            Assert.That(collisions, Is.Empty, "Unity cannot build components with duplicate serialized names.");
        }

        [Test]
        public void ExistingLurePrefab_PreservesBothSavedRevealColors()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Anomalies/Standin_Lure.prefab");
            Assert.That(prefab, Is.Not.Null);
            var component = prefab.GetComponent(GameAssembly.GetType("LureAnomaly", true));
            using (var serialized = new SerializedObject(component))
            {
                // Both entries were red in the pre-change YAML. Keep them distinct and preserved.
                Assert.That(serialized.FindProperty("revealColor").colorValue, Is.EqualTo(Color.red));
                Assert.That(serialized.FindProperty("hallwayRevealColor").colorValue, Is.EqualTo(Color.red));
            }
        }
    }
}
