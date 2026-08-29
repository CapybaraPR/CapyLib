using ProjectMER.Features.Objects;
using UnityEngine;

namespace Capy.Engine.Audio;

/// <summary>
/// Менеджер воспроизведения глобального и пространственного аудио.
/// </summary>
public static class AudioToggle
{
    public static void CreateGlobal(string clipName, float volume = 1f, bool loop = false)
    {
        if (!ValidateClip(ref clipName)) return;

        var audioPlayer = AudioPlayer.CreateOrGet("CapyGlobalAudio", onIntialCreation: p =>
        {
            p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f, volume: volume);
        });

        audioPlayer.AddClip(clipName, destroyOnEnd: false, loop: loop);
    }

    public static void CreateForPlayer(Player player, string clipName, float min = 0f, float max = 15f, float volume = 1f, bool loop = false)
    {
        if (player == null || !ValidateClip(ref clipName)) return;

        string key = $"CapyPlayer_{player.Id}";
        var audioPlayer = AudioPlayer.CreateOrGet(key, onIntialCreation: p =>
        {
            p.transform.parent = player.GameObject.transform;
            var speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: min, maxDistance: max, volume: volume);
            speaker.transform.parent = player.Transform;
            speaker.transform.localPosition = Vector3.zero;
        });

        audioPlayer.AddClip(clipName, destroyOnEnd: false, loop: loop);
    }

    public static void CreateForGameObject(GameObject gameObject, string clipName, float min = 5f, float max = 20f, float volume = 1f, bool loop = false)
    {
        if (gameObject == null || !ValidateClip(ref clipName)) return;

        string key = $"CapyObj_{gameObject.GetInstanceID()}";
        var audioPlayer = AudioPlayer.CreateOrGet(key, onIntialCreation: p =>
        {
            p.transform.parent = gameObject.transform;
            var speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: min, maxDistance: max, volume: volume);
            speaker.transform.parent = gameObject.transform;
            speaker.transform.localPosition = Vector3.zero;
        });

        audioPlayer.AddClip(clipName, destroyOnEnd: false, loop: loop);
    }

    public static void CreateForSchematic(SchematicObject schematic, string clipName, float min = 5f, float max = 20f, float volume = 1f, bool loop = false)
    {
        if (schematic == null || schematic.gameObject == null) return;
        CreateForGameObject(schematic.gameObject, clipName, min, max, volume, loop);
    }

    public static void CreateForRoom(Room room, string clipName, float min = 5f, float max = 20f, float volume = 1f, bool loop = true)
    {
        if (room == null || !ValidateClip(ref clipName)) return;

        string key = $"CapyRoom_{room.Name}";
        var audioPlayer = AudioPlayer.CreateOrGet(key, onIntialCreation: p =>
        {
            var speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: min, maxDistance: max, volume: volume);
            speaker.transform.position = room.Position;
        });

        audioPlayer.AddClip(clipName, destroyOnEnd: false, loop: loop);
    }

    public static void DestroyGlobal(string? clipName = null) => SimpleDestroy("CapyGlobalAudio", clipName);
    public static void DestroyForPlayer(Player player) => SimpleDestroy($"CapyPlayer_{player?.Id}");
    public static void DestroyForGameObject(GameObject gameObject) => SimpleDestroy($"CapyObj_{gameObject?.GetInstanceID()}");
    public static void DestroyForRoom(Room room) => SimpleDestroy($"CapyRoom_{room?.Name}");

    private static void SimpleDestroy(string key, string? clipName = null)
    {
        if (!AudioPlayer.TryGet(key, out AudioPlayer player)) return;

        if (string.IsNullOrEmpty(clipName))
        {
            player.Destroy();
        }
        else
        {
            player.RemoveClipByName(clipName!.ToLower());
            if (player.ClipsById.Count == 0)
            {
                player.Destroy();
            }
        }
    }

    private static bool ValidateClip(ref string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return false;
        clipName = clipName.ToLower();

        if (!AudioClipStorage.AudioClips.ContainsKey(clipName))
        {
            Log.Warn($"[CapyLib:Audio] Клип '{clipName}' не найден в AudioClipStorage.");
            return false;
        }
        return true;
    }
}
