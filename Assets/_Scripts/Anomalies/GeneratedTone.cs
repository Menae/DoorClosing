using UnityEngine;

public static class GeneratedTone
{
    private const int SampleRate = 44100;

    public static AudioClip CreateMechanicalLoop(string name, float seconds = 1f)
    {
        int length = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        float[] samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)SampleRate;
            float pulse = Mathf.Sin(time * Mathf.PI * 2f * 28f) * 0.11f;
            float hum = Mathf.Sin(time * Mathf.PI * 2f * 56f) * 0.07f;
            float rattle = Mathf.Sin(time * Mathf.PI * 2f * 91f) * Mathf.Sin(time * Mathf.PI * 2f * 3f) * 0.025f;
            samples[i] = pulse + hum + rattle;
        }

        return CreateClip(name, samples);
    }

    public static AudioClip CreateWarningChime(string name, float seconds = 0.7f)
    {
        int length = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        float[] samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)SampleRate;
            float envelope = Mathf.Clamp01(time / 0.025f) * Mathf.Clamp01((seconds - time) / 0.15f);
            float twoTone = Mathf.Sin(time * Mathf.PI * 2f * 660f) + Mathf.Sin(time * Mathf.PI * 2f * 880f) * 0.45f;
            samples[i] = twoTone * envelope * 0.12f;
        }

        return CreateClip(name, samples);
    }

    private static AudioClip CreateClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
