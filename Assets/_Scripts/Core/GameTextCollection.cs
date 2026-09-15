using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Scene-owned copy. Keys identify behavior; changing words never changes actions.
[ExecuteAlways, DefaultExecutionOrder(-1000)]
public sealed class GameTextCollection : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string key;
        public string label;
        [TextArea(2, 12)] public string value;
        public TMP_Text target;
    }
    [HideInInspector] public List<Entry> entries = new List<Entry>();
    [HideInInspector] public EditableNotice[] notices = new EditableNotice[0];
    private static readonly List<GameTextCollection> active = new List<GameTextCollection>();
    private bool pending;
    private void OnEnable() { if (!active.Contains(this)) active.Add(this); Apply(); }
    private void OnDisable() => active.Remove(this);
    private void OnValidate() => pending = true;
    private void Update() { if (pending) Apply(); }
    public void Apply()
    {
        pending = false;
        foreach (var entry in entries)
            if (entry.target != null && entry.target.text != (entry.value ?? "")) entry.target.text = entry.value ?? "";
    }
    public static string Get(Component owner, string key, string fallback)
    {
        foreach (var collection in active)
        {
            if (collection == null || collection.gameObject.scene != owner.gameObject.scene) continue;
            foreach (var entry in collection.entries)
                if (entry.key == key) return entry.value ?? "";
        }
        return fallback;
    }
    public void Add(string key, string label, string value, TMP_Text target = null)
    {
        var entry = entries.Find(e => e.key == key);
        if (entry == null) { entry = new Entry { key = key, label = label, value = value }; entries.Add(entry); }
        entry.label = label;
        entry.target = target;
    }
}
