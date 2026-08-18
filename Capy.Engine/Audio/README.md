# 🔊 Capy.Engine.Audio — Аудио-система и пространственный звук

Модуль **Audio** обеспечивает автоматическую загрузку, регистрацию и воспроизведение аудиофайлов `.ogg` (включая глобальные треки, привязку к игрокам, объектам и схематикам `ProjectMER`).

---

## 🎵 Загрузка аудиоклипов (`AudioRegistry`)

1. Поместите файлы формата `.ogg` в директорию:
   * `EXILED/Plugins/CapyLib/Audio/` (поддерживаются подпапки).
2. При запуске плагина `AudioRegistry.RegisterClips()` автоматически регистрирует все файлы по их именам без расширения в нижнем регистре (например, `alert.ogg` -> `alert`).

---

## 🎮 Воспроизведение звука (`AudioToggle`)

```csharp
using Capy.Engine.Audio;

// 1. Глобальное воспроизведение на весь сервер (музыка конца раунда, сирена)
AudioToggle.CreateGlobal("round_end_theme", volume: 0.8f);

// 2. Пространственный 3D-звук, привязанный к игроку
AudioToggle.CreateForPlayer(
    player: player, 
    clipName: "footsteps_heavy", 
    min: 1f, 
    max: 20f, 
    volume: 1f
);

// 3. Звук, привязанный к схематику ProjectMER (например, кастомный генератор)
AudioToggle.CreateForSchematic(schematic, "generator_hum", min: 3f, max: 15f);

// 4. Остановка воспроизведения
AudioToggle.StopGlobal();
AudioToggle.StopForPlayer(player);
```
