namespace Capy.Engine.Audio;

public static class AudioRegistry
{
    private static readonly List<string> LoadedClipNames = new();

    public static IReadOnlyList<string> LoadedClips => LoadedClipsList();

    public static string AudioPath => Path.Combine(Paths.Plugins, "CapyLib", "Audio");

    public static void RegisterClips()
    {
        if (!Directory.Exists(AudioPath))
        {
            Directory.CreateDirectory(AudioPath);
            return;
        }

        string[] audioFiles = Directory.GetFiles(AudioPath, "*.ogg", SearchOption.AllDirectories);

        foreach (string filePath in audioFiles)
        {
            try
            {
                string relative = filePath.Substring(AudioPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string clipName = relative.Replace('\\', '.').Replace('/', '.');

                if (clipName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                    clipName = clipName.Substring(0, clipName.Length - 4);

                clipName = clipName.ToLowerInvariant();

                AudioClipStorage.LoadClip(filePath, clipName);

                if (!LoadedClipNames.Contains(clipName))
                    LoadedClipNames.Add(clipName);

                Log.Debug($"[CapyLib:Audio] Загружен аудиоклип: {clipName}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CapyLib:Audio] Ошибка загрузки клипа {filePath}: {ex.Message}");
            }
        }

        Log.Info($"[CapyLib:Audio] Загружено аудиоклипов: {LoadedClipNames.Count}");
    }

    public static void UnloadAll()
    {
        foreach (string clipName in LoadedClipNames)
        {
            try
            {
                AudioClipStorage.AudioClips.Remove(clipName);
            }
            catch { }
        }

        LoadedClipNames.Clear();
    }

    private static IReadOnlyList<string> LoadedClipsList() => LoadedClipNames;
}
