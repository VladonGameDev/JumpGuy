# Unity project — правила работы

Версия: Unity 2022.3.22f1, URP/HDRP: <укажи>, язык: C#.

## Структура
- `Assets/Scripts/` — игровой код C#
- `Assets/Scenes/` — сцены
- `Assets/Prefabs/` — префабы
- `Packages/manifest.json` — зависимости
- `ProjectSettings/` — настройки проекта

## Читать разрешено
- `Assets/**` (кроме `Assets/**/*.meta`, если не спрошено явно)
- `ProjectSettings/*.asset`, `Packages/manifest.json`
- `Assets/**/*.asmdef`, `*.csproj`, `*.sln`

## НИКОГДА не читать и не искать
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build/`, `Builds/`
- `UserSettings/`, `.vs/`, `.idea/`, `MemoryCaptures/`, `Recordings/`
- `*.meta` пачками, `*.unity` сцены целиком (могут быть в мегабайтах)

## Поиск
- Всегда сужай `path` до конкретной папки: `Assets/Scripts`
- Для C# используй `grep(..., include: "*.cs")`
- Никогда не запускай `glob` с `**/*` от корня проекта

## Код
- Не менять `.meta` вручную
- Не редактировать `Library/` и `Packages/` (кэш и пакеты)
- После правки C# не пытайся «собрать проект» — сборка в Unity, не в CLI