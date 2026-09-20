using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Small, versioned local store. It never repairs or overwrites unreadable data.
internal sealed class CampaignSaveStore : IDisposable
{
    [Serializable] internal sealed class ProgressData
    {
        public bool hasRun, completed;
        public int night, deaths;
        public double elapsedSeconds;
    }

    [Serializable] internal sealed class ProfileData
    {
        public bool cleared;
        public double bestSeconds;
        public int bestDeaths;
        public float sensitivity, fov, brightness, volume;
        public bool inverted, displaySet, fullscreen;
        public int width, height;

        internal static ProfileData Defaults(float fieldOfView) => new ProfileData
        { sensitivity = .12f, fov = fieldOfView, volume = 1 };

        internal void RecordClear(ProgressData result)
        {
            bestSeconds = cleared ? Math.Min(bestSeconds, result.elapsedSeconds) : result.elapsedSeconds;
            bestDeaths = cleared ? Math.Min(bestDeaths, result.deaths) : result.deaths;
            cleared = true;
        }
    }

    [Serializable] private sealed class Envelope
    {
        public int version;
        public string kind, payload, checksum;
    }

    internal ProgressData Progress { get; set; } = new ProgressData();
    internal ProfileData Profile { get; private set; }
    internal string Error { get; private set; }
    internal bool Available => Error == null && ownership != null;
    private readonly string directory;
    private FileStream ownership;

    internal CampaignSaveStore(string directory, float defaultFov)
    {
        this.directory = directory;
        Profile = ProfileData.Defaults(defaultFov);
        try
        {
            Directory.CreateDirectory(directory);
            ownership = new FileStream(Path.Combine(directory, "session.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Progress = Read("progress", Progress);
            Profile = Read("profile", Profile);
            Validate(Progress); Validate(Profile);
            // Complete progress is written first. A crash before the profile write is recoverable.
            if (Progress.completed) { Profile.RecordClear(Progress); SaveProfile(); }
        }
        catch (Exception ex) when (IsStorageFailure(ex)) { Fail(ex); }
    }

    internal bool SaveProgress() => Write("progress", Progress);
    internal bool SaveProfile() => Write("profile", Profile);

    private T Read<T>(string kind, T missing)
    {
        string path = Path.Combine(directory, kind + ".json");
        if (!File.Exists(path))
        {
            if (File.Exists(path + ".bak")) throw new InvalidDataException(kind + ": main missing; backup preserved");
            return missing;
        }
        var value = Decode<T>(path, kind);
        // Do not replace an unknown/corrupt backup on the next write either.
        if (File.Exists(path + ".bak")) Decode<T>(path + ".bak", kind);
        return value;
    }

    private static T Decode<T>(string path, string kind)
    {
        if (new FileInfo(path).Length > 262144) throw new InvalidDataException(kind + ": unexpected size");
        var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
        if (envelope == null || envelope.version != 1 || envelope.kind != kind || string.IsNullOrEmpty(envelope.payload)
            || envelope.checksum != Digest(envelope.payload)) throw new InvalidDataException(kind + ": unreadable or unsupported format");
        var result = JsonUtility.FromJson<T>(envelope.payload);
        Validate(result);
        return result;
    }

    private bool Write<T>(string kind, T value)
    {
        if (!Available) return false;
        try
        {
            Validate(value);
            string payload = JsonUtility.ToJson(value);
            string json = JsonUtility.ToJson(new Envelope { version = 1, kind = kind, payload = payload, checksum = Digest(payload) }, true);
            string path = Path.Combine(directory, kind + ".json");
            // Detect edits made by tools outside the cooperating game process.
            if (File.Exists(path)) Decode<T>(path, kind);
            if (File.Exists(path + ".bak")) Decode<T>(path + ".bak", kind);
            string pending = path + ".pending-" + Guid.NewGuid().ToString("N");
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(pending, path, path + ".bak");
            else File.Move(pending, path);
            return true;
        }
        catch (Exception ex) when (IsStorageFailure(ex)) { Fail(ex); return false; }
    }

    private static void Validate(object value)
    {
        if (value is ProgressData progress)
        {
            if (progress.night < 0 || progress.night > 3 || progress.deaths < 0 || !Finite(progress.elapsedSeconds) || progress.elapsedSeconds < 0
                || (progress.completed && (!progress.hasRun || progress.night != 3))) throw new InvalidDataException("Invalid progress");
        }
        else if (value is ProfileData profile)
        {
            if (!Range(profile.sensitivity, .04, .24) || !Range(profile.fov, 50, 85) || !Range(profile.brightness, -1, 1)
                || !Range(profile.volume, 0, 1) || !Finite(profile.bestSeconds) || profile.bestSeconds < 0 || profile.bestDeaths < 0
                || (profile.displaySet && !((profile.width == 1280 && profile.height == 720) || (profile.width == 1920 && profile.height == 1080))))
                throw new InvalidDataException("Invalid profile");
        }
        else throw new InvalidDataException("Missing save data");
    }

    private static bool Range(double value, double min, double max) => Finite(value) && value >= min - .000001 && value <= max + .000001;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static string Digest(string value)
    { using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value))); }
    private static bool IsStorageFailure(Exception ex) => ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Security.SecurityException;
    private void Fail(Exception ex) { Error = ex.Message; }
    public void Dispose() { ownership?.Dispose(); ownership = null; }
}
