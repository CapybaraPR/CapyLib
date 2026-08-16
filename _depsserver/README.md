# 📦 Серверные зависимости CapyLib (Server Dependencies)

В этой папке собраны **только внешние сторонние библиотеки**, которых нет в стандартном SCP:SL / Unity / EXILED.

---

## 📂 Куда загружать на сервере:

Скопируйте нужные `.dll` файлы в папку зависимостей EXILED:
- `EXILED/Plugins/dependencies/` (или `EXILED/dependencies/`).

---

## 📋 Содержимое пакета:

1. **Аудио-движок (пространственный 3D-звук и воспроизведение .ogg)**:
   - `AudioPlayerApi.dll`
   - `NVorbis.dll`
   - `ProjectMER.dll` (интеграция со схематиками)

2. **UI и экранные подсказки (Hints)**:
   - `HintServiceMeow-Exiled.dll`
   - `HintServiceMeow.dll`

3. **Базы данных**:
   - `LiteDB.dll` *(локальная embedded база данных)*
   - `MongoDB.Driver.dll`, `MongoDB.Bson.dll`, `DnsClient.dll`, `SharpCompress.dll`, `Snappier.dll`, `ZstdSharp.dll` *(для распределённой MongoDB)*

4. **Интеграции**:
   - `LabApi.dll` *(при использовании LabApi)*

> ℹ️ **Примечание**: Все стандартные системные библиотеки (`System.*`, `Microsoft.*`, `UnityEngine.*`, `0Harmony`, `YamlDotNet`, `Mirror` и т.д.) уже встроены в сам сервер SCP:SL (`SCPSL_Data/Managed/`) и базовый EXILED. Загружать их отдельно не требуется.
