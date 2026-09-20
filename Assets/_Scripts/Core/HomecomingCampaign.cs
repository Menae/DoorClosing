using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>In-memory four-night composition; RunManager still owns one encounter run.</summary>
public sealed class HomecomingCampaign : MonoBehaviour
{
    [SerializeField] private bool playFullStory = true;
    [SerializeField] private BeatDefinition[] firstNight;
    [SerializeField] private BeatDefinition[] secondNight;
    [SerializeField, Range(.5f, 1f)] private float laterGraceScale = .9f;
    private readonly List<BeatDefinition> copies = new List<BeatDefinition>();
    private readonly Queue<BeatDefinition> pendingExtras = new Queue<BeatDefinition>();
    // Presentation jitter and Editor tooling must not consume the encounter lottery.
    private System.Random random = new System.Random();

    public bool FullStory => enabled && playFullStory;
    public int CurrentNight { get; private set; }
    public bool HasFollowingNight => CurrentNight < 3;
    public int Attempt { get; private set; }
    public int NormalStopDraws { get; private set; }
    public int PendingExtras => pendingExtras.Count;

    internal void ResetStory() { ReleaseCopies(); DiscardPendingExtras(); CurrentNight = 0; Attempt = 0; }
    internal void RestoreNight(int night)
    {
        if (night < 0 || night > 3) throw new ArgumentOutOfRangeException(nameof(night));
        ResetStory(); CurrentNight = night;
    }
    internal void AdvanceNight()
    {
        if (!HasFollowingNight) throw new InvalidOperationException("All four nights are complete.");
        ReleaseCopies(); DiscardPendingExtras(); CurrentNight++; Attempt = 0;
    }

    // Only the normal-travel owners call this after accepting a new stop edge.
    internal void DrawForNormalStop()
    {
        if (!FullStory || CurrentNight == 0) return;
        ValidateNight(firstNight); ValidateNight(secondNight);
        NormalStopDraws++;
        if (random.NextDouble() >= 1.0 / 3.0) return;
        int index = random.Next(6);
        var definition = index < 3 ? firstNight[index] : secondNight[index - 3];
        pendingExtras.Enqueue(definition);
        Debug.Log("[Campaign] Extra queued: " + definition.DebugLabel, this);
    }

    internal BeatDefinition TakePendingExtra()
    {
        if (pendingExtras.Count == 0) return null;
        var copy = pendingExtras.Dequeue().CopyForRun(CurrentNight >= 2 ? laterGraceScale : 1f);
        copies.Add(copy);
        return copy;
    }

    internal void DiscardPendingExtras() { pendingExtras.Clear(); NormalStopDraws = 0; }

    internal List<BeatDefinition> CreateAttempt()
    {
        ValidateNight(firstNight); ValidateNight(secondNight);
        ReleaseCopies(); Attempt++;
        var selected = new List<BeatDefinition>();
        if (CurrentNight == 1) selected.AddRange(firstNight);
        else if (CurrentNight == 2) selected.AddRange(secondNight);
        else if (CurrentNight == 3)
        {
            foreach (var category in new[] { AnomalyCategory.Lure, AnomalyCategory.Provocation, AnomalyCategory.Hijack })
            {
                var source = random.Next(2) == 0 ? firstNight : secondNight;
                selected.Add(Array.Find(source, b => b.Category == category));
            }
            for (int i = selected.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                var value = selected[i]; selected[i] = selected[other]; selected[other] = value;
            }
        }
        else throw new InvalidOperationException("Introduction must finish before encounters.");

        foreach (var definition in selected)
            copies.Add(definition.CopyForRun(CurrentNight >= 2 ? laterGraceScale : 1f));
        Debug.Log("[Campaign] Night " + CurrentNight + " attempt " + Attempt + ": " + string.Join(", ", copies.ConvertAll(b => b.DebugLabel)), this);
        return new List<BeatDefinition>(copies);
    }

    private static void ValidateNight(BeatDefinition[] definitions)
    {
        if (definitions == null || definitions.Length != 3)
            throw new InvalidOperationException("Each authored night requires exactly three encounters.");
        var categories = new HashSet<AnomalyCategory>();
        foreach (var definition in definitions)
            if (definition == null || definition.Category == AnomalyCategory.Normal || !categories.Add(definition.Category))
                throw new InvalidOperationException("Each authored night requires one Lure, one Provocation and one Hijack.");
    }

    private void ReleaseCopies()
    {
        foreach (var copy in copies) if (copy != null) Destroy(copy);
        copies.Clear();
    }
    private void OnDestroy() => ReleaseCopies();
}
