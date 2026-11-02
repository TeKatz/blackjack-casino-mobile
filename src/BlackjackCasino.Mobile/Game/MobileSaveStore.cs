using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlackjackCasino.Mobile.Game;

/// <summary>Private, versioned Android save with checksum, atomic replacement and one backup.</summary>
public sealed class MobileSaveStore(string directory)
{
    private readonly object gate = new();
    private string SavePath => Path.Combine(directory, "mobile-profile-v1.json");
    private string BackupPath => SavePath + ".bak";
    public string? RecoveryNotice { get; private set; }

    public MobileState? Load()
    {
        lock (gate)
        {
            RecoveryNotice = null;
            if (!File.Exists(SavePath) && !File.Exists(BackupPath)) { return null; }
            if (File.Exists(SavePath))
            {
                try { return Read(SavePath); }
                catch (Exception exception) when (IsCorrupt(exception)) { }
            }
            if (File.Exists(BackupPath))
            {
                try
                {
                    MobileState recovered = Read(BackupPath);
                    RecoveryNotice = "Recovered the previous local save. The most recent action may be missing.";
                    return recovered;
                }
                catch (Exception exception) when (IsCorrupt(exception)) { }
            }
            throw new InvalidDataException("The local profile and its backup could not be read. They have been preserved. Please report this before reinstalling the app.");
        }
    }

    public void Save(MobileState state)
    {
        // Refuse invalid state, including unknown versions, before replacing any file.
        _ = BlackjackSession.Restore(state);
        string data = MobileJson.Serialize(state);
        string content = MobileJson.Serialize(new SaveEnvelope { Data = data, Sha256 = Hash(data) });
        lock (gate)
        {
            Directory.CreateDirectory(directory);
            string temporary = SavePath + ".tmp";
            string backupTemporary = BackupPath + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                    4096, FileOptions.WriteThrough))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(content);
                    stream.Write(bytes);
                    stream.Flush(flushToDisk: true);
                }
                if (File.Exists(SavePath))
                {
                    bool valid = true;
                    try { _ = Read(SavePath); }
                    catch (Exception exception) when (IsCorrupt(exception)) { valid = false; }
                    if (valid)
                    {
                        File.Copy(SavePath, backupTemporary, overwrite: true);
                        File.Move(backupTemporary, BackupPath, overwrite: true);
                    }
                    else
                    {
                        // Never replace the known-good backup with the broken primary.
                        string quarantine = SavePath + ".corrupt-" + Guid.NewGuid().ToString("N");
                        File.Copy(SavePath, quarantine);
                    }
                }
                File.Move(temporary, SavePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary)) { File.Delete(temporary); }
                if (File.Exists(backupTemporary)) { File.Delete(backupTemporary); }
            }
        }
    }

    private static MobileState Read(string path)
    {
        if (new FileInfo(path).Length > 2_000_000) { throw new InvalidDataException("Save is too large."); }
        SaveEnvelope envelope = MobileJson.Deserialize<SaveEnvelope>(File.ReadAllText(path));
        if (envelope.Version != 1) { throw new NotSupportedException("This save requires a newer app version. Existing files were left unchanged."); }
        if (envelope.Data is null || envelope.Sha256 is null || Hash(envelope.Data) != envelope.Sha256)
        { throw new InvalidDataException("Save checksum does not match."); }
        MobileState saved = MobileJson.Deserialize<MobileState>(envelope.Data);
        _ = BlackjackSession.Restore(saved);
        return saved;
    }

    private static string Hash(string data) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
    private static bool IsCorrupt(Exception exception) => exception is JsonException or InvalidDataException or ArgumentException or OverflowException;

    private sealed class SaveEnvelope
    {
        public SaveEnvelope() { }
        public int Version { get; set; } = 1;
        public string? Data { get; set; }
        public string? Sha256 { get; set; }
    }
}
