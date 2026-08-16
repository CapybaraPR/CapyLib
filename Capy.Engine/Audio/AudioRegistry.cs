namespace Capy.Engine.Audio;

/// <summary>
/// Автоматический загрузчик и регистратор аудиофайлов .ogg в AudioPlayerApi.
/// </summary>
public static class AudioRegistry
{
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
                string clipName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                AudioClipStorage.LoadClip(filePath, clipName);
                Log.Debug($"[CapyLib:Audio] Загружен аудиоклип: {clipName}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CapyLib:Audio] Ошибка загрузки клипа {filePath}: {ex.Message}");
            }
        }

        Log.Info($"[CapyLib:Audio] Загружено аудиоклипов: {audioFiles.Length}");
    }
}
