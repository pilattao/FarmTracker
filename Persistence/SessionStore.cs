using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FarmTracker.Model;

namespace FarmTracker.Persistence;

/// <summary>Persists each session as one JSON file under &lt;dir&gt;/sessions/. Atomic writes
/// (temp + move). Loads newest-first, skips corrupt files, prunes to a cap. Pure .NET (no ExileCore);
/// unit-tested against a temp directory.</summary>
public sealed class SessionStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private readonly string _dir;            // <config>/sessions
    private readonly Action<string> _logError;

    public SessionStore(string baseDir, Action<string> logError)
    {
        _dir = Path.Combine(baseDir, "sessions");
        _logError = logError;
    }

    public void Save(Session s)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, FileName(s.StartUtc));
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(s, Json));
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex) { _logError($"session save failed: {ex.Message}"); }
    }

    /// <summary>Loads all persisted sessions newest-first, skipping corrupt files. SIDE EFFECT: prunes
    /// excess session files on disk down to <paramref name="cap"/> and removes any orphaned .tmp files
    /// left by an interrupted Save.</summary>
    public List<Session> LoadAll(int cap)
    {
        var result = new List<Session>();
        try
        {
            if (!Directory.Exists(_dir)) return result;

            foreach (var tmp in Directory.GetFiles(_dir, "*.tmp"))
            {
                try { File.Delete(tmp); }
                catch (Exception ex) { _logError($"skip orphan temp {Path.GetFileName(tmp)}: {ex.Message}"); }
            }

            foreach (var f in Directory.GetFiles(_dir, "*.json"))
            {
                try
                {
                    var s = JsonSerializer.Deserialize<Session>(File.ReadAllText(f), Json);
                    if (s != null) result.Add(s);
                }
                catch (Exception ex) { _logError($"skip corrupt session {Path.GetFileName(f)}: {ex.Message}"); }
            }
        }
        catch (Exception ex) { _logError($"session load failed: {ex.Message}"); }

        result.Sort((a, b) => b.StartUtc.CompareTo(a.StartUtc));  // newest first
        Prune(cap);
        if (result.Count > cap) result = result.Take(cap).ToList();
        return result;
    }

    public void Prune(int cap)
    {
        try
        {
            if (cap <= 0 || !Directory.Exists(_dir)) return;
            var files = Directory.GetFiles(_dir, "*.json")
                .OrderByDescending(f => f)   // filename is yyyyMMdd-HHmmss -> lexical == chronological
                .ToList();
            foreach (var f in files.Skip(cap)) File.Delete(f);
        }
        catch (Exception ex) { _logError($"session prune failed: {ex.Message}"); }
    }

    private static string FileName(DateTime startUtc) => startUtc.ToString("yyyyMMdd-HHmmss") + ".json";
}
