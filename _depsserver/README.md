# 📦 Серверные зависимости CapyLib (Server Dependencies)

В этой папке собраны все необходимые DLL-зависимости, которые **должны быть установлены на самом сервере SCP:SL**.

---

## 📂 Куда загружать на сервере:

Скопируйте все `.dll` файлы из этой папки в:
- `EXILED/Plugins/dependencies/` (или `EXILED/dependencies/` в зависимости от версии EXILED).

---

## 📋 Содержимое пакета:

1. **Аудио-движок**:
   - `AudioPlayerApi.dll`
   - `NVorbis.dll`
   - `ProjectMER.dll`

2. **UI и подсказки (Hints)**:
   - `HintServiceMeow-Exiled.dll`
   - `HintServiceMeow.dll`

3. **Базы данных**:
   - `LiteDB.dll` (локальная база данных)
   - `MongoDB.Driver.dll`, `MongoDB.Bson.dll`, `DnsClient.dll`, `SharpCompress.dll`, `Snappier.dll`, `ZstdSharp.dll` (сетевая база данных MongoDB)

4. **Системные BCL библиотеки**:
   - `System.Text.Json.dll`
   - `Microsoft.Bcl.AsyncInterfaces.dll`
   - `Microsoft.Extensions.Logging.Abstractions.dll`
   - `System.Buffers.dll`
   - `System.Memory.dll`
   - `System.Numerics.Vectors.dll`
   - `System.Runtime.CompilerServices.Unsafe.dll`
   - `System.Threading.Tasks.Extensions.dll`
   - `System.Text.Encoding.CodePages.dll`
   - `System.Diagnostics.DiagnosticSource.dll`
   - `System.Security.AccessControl.dll`
   - `System.Security.Principal.Windows.dll`
   - `Microsoft.Win32.Registry.dll`
   - `LabApi.dll`
