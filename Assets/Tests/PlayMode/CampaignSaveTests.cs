#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace GraduationProject.Tests
{
    public sealed class CampaignSaveTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private string directory;
        [SetUp] public void Setup() => directory = Path.GetFullPath("artifacts/m4-01/storage/" + Guid.NewGuid().ToString("N"));
        private object Store() => Activator.CreateInstance(GameAccess.Type("CampaignSaveStore"), Flags, null, new object[] { directory, 65f }, null);
        internal static object Get(object instance, string property) => instance.GetType().GetProperty(property, Flags).GetValue(instance);
        internal static object Call(object instance, string method, params object[] args) => instance.GetType().GetMethod(method, Flags).Invoke(instance, args);
        internal static void Set(object instance, string field, object value) => instance.GetType().GetField(field, Flags).SetValue(instance, value);
        internal static T Field<T>(object instance, string field) => (T)instance.GetType().GetField(field, Flags).GetValue(instance);
        private static void Close(object store) => ((IDisposable)store).Dispose();

        [Test] public void RoundTrip_Backup_IndependentBests_AndCompletionRecovery()
        {
            var store = Store();
            try
            {
                Assert.That(Get(store, "Available"), Is.True);
                var progress = Get(store, "Progress"); var profile = Get(store, "Profile");
                Set(progress,"hasRun",true); Set(progress,"night",3); Set(progress,"deaths",4); Set(progress,"elapsedSeconds",600d);
                Set(profile,"volume",.23f); Assert.That(Call(store,"SaveProfile"), Is.True);
                Assert.That(Call(store,"SaveProgress"), Is.True);
                Set(progress,"completed",true); Assert.That(Call(store,"SaveProgress"), Is.True);
                // Simulate a crash between completing progress and writing profile.
            }
            finally { Close(store); }
            store = Store();
            try
            {
                var progress = Get(store,"Progress"); var profile = Get(store,"Profile");
                Assert.That(Field<bool>(profile,"cleared"),Is.True); Assert.That(Field<float>(profile,"volume"),Is.EqualTo(.23f));
                Assert.That(Field<double>(profile,"bestSeconds"),Is.EqualTo(600)); Assert.That(Field<int>(profile,"bestDeaths"),Is.EqualTo(4));
                Set(progress,"elapsedSeconds",700d); Set(progress,"deaths",2); Call(profile,"RecordClear",progress);
                Assert.That(Field<double>(profile,"bestSeconds"),Is.EqualTo(600)); Assert.That(Field<int>(profile,"bestDeaths"),Is.EqualTo(2));
                Set(progress,"elapsedSeconds",500d); Set(progress,"deaths",8); Call(profile,"RecordClear",progress);
                Assert.That(Field<double>(profile,"bestSeconds"),Is.EqualTo(500)); Assert.That(Field<int>(profile,"bestDeaths"),Is.EqualTo(2));
                Assert.That(Call(store,"SaveProfile"),Is.True);
                Assert.That(File.Exists(Path.Combine(directory,"progress.json.bak")),Is.True);
                Assert.That(File.Exists(Path.Combine(directory,"profile.json.bak")),Is.True);
            }
            finally { Close(store); }
            store=Store(); try { Assert.That(Field<int>(Get(store,"Profile"),"bestDeaths"),Is.EqualTo(2)); } finally { Close(store); }
        }

        [TestCase("")]
        [TestCase("{broken")]
        [TestCase("{\"version\":99}")]
        public void UnreadableOrFutureData_IsPreserved_AndBlocksWrites(string contents)
        {
            Directory.CreateDirectory(directory); string path=Path.Combine(directory,"progress.json"); File.WriteAllText(path,contents);
            var store=Store();
            try { Assert.That(Get(store,"Available"),Is.False); Assert.That(Call(store,"SaveProgress"),Is.False); Assert.That(File.ReadAllText(path),Is.EqualTo(contents)); }
            finally { Close(store); }
        }

        [Test] public void ConcurrentStore_CannotOverwriteOwner_AndInterruptedPendingIsPreserved()
        {
            var owner=Store(); var other=Store();
            try
            {
                Assert.That(Get(other,"Available"),Is.False); Assert.That(Call(other,"SaveProfile"),Is.False);
                Assert.That(Call(owner,"SaveProgress"),Is.True);
                File.WriteAllText(Path.Combine(directory,"progress.json.pending-interrupted"),"incomplete");
            }
            finally { Close(other); Close(owner); }
            var reopened=Store();
            try { Assert.That(Get(reopened,"Available"),Is.True); Assert.That(Call(reopened,"SaveProgress"),Is.True); }
            finally { Close(reopened); }
            Assert.That(File.ReadAllText(Path.Combine(directory,"progress.json.pending-interrupted")),Is.EqualTo("incomplete"));
        }

        [Test] public void WriteFailure_AndChangedData_LeaveExistingSaveIntact()
        {
            var store=Store();
            try
            {
                Assert.That(Call(store,"SaveProgress"),Is.True);
                string path=Path.Combine(directory,"progress.json"); string original=File.ReadAllText(path);
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
                    Assert.That(Call(store,"SaveProgress"),Is.False);
                Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            }
            finally { Close(store); }
            File.WriteAllText(Path.Combine(directory,"progress.json.bak"),"unknown backup");
            store=Store();
            try { Assert.That(Get(store,"Available"),Is.False); Assert.That(File.ReadAllText(Path.Combine(directory,"progress.json.bak")),Is.EqualTo("unknown backup")); }
            finally { Close(store); }
        }
    }
}
#endif
