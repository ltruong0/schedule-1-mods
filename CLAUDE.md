# Schedule 1 Modding Project

## Overview
MelonLoader mods for Schedule I (IL2CPP/Unity game). Built on macOS, deployed to Windows gaming PC.

## Build Setup
- **SDK**: .NET 6 via Homebrew at `/opt/homebrew/Cellar/dotnet@6/6.0.136_1/bin/dotnet`
- **Build command**: `dotnet build -c Release` from a mod's directory
- **Decompiler**: `DOTNET_ROOT=/opt/homebrew/Cellar/dotnet@6/6.0.136_1/libexec /Users/leetruong/.dotnet/tools/ilspycmd <dll>`
- **Output**: `<ModName>/bin/Release/net6.0/<ModName>.dll` → copy to `Schedule I/Mods/` on gaming PC

## Reference Assemblies (not in git)
- `Il2CppAssemblies/` — IL2CPP unhollowed game assemblies from MelonLoader
- `net6/` — MelonLoader runtime DLLs (MelonLoader.dll, Il2CppInterop.Runtime.dll, 0Harmony.dll, etc.)
- `ModManager&PhoneApp.dll` — Community mod manager for in-game settings UI
- `PackRat-IL2CPP.dll` — Reference mod (good example of config patterns)

## Game Info
- **Game**: Schedule I by TVGS
- **MelonInfo assembly attribute**: `[assembly: MelonGame("TVGS", "Schedule I")]`
- **Main game assembly**: `Il2CppAssemblies/Assembly-CSharp.dll`
- **Game namespaces** use `Il2Cpp` prefix when referenced from mods (e.g. `Il2CppScheduleOne.Equipping`)

## Key Game Classes
- `ScheduleOne.Equipping.Equippable_RangedWeapon` — Base ranged weapon (M1911 uses this directly)
- `ScheduleOne.Equipping.Equippable_PumpShotgun` — Shotgun subclass
- `ScheduleOne.Equipping.Equippable_Revolver` — Revolver subclass
- `ScheduleOne.Equipping.Equippable_MeleeWeapon` — Melee weapons
- `ScheduleOne.ItemFramework.ItemDefinition` — Item definitions
- `ScheduleOne.UI.Phone.*` — In-game phone UI apps

## Mod Manager Integration (ModManagerPhoneApp)
The community mod manager auto-discovers settings via MelonPreferences. Critical details:

### Category naming (IMPORTANT)
`FindCategoriesForMod` matches categories where **Identifier.StartsWith(modName)** (case-insensitive).
It checks both the raw `MelonInfo` name and the name with spaces removed.
- Example: MelonInfo name `"M1911 Magazine Mod"` → checks `"M1911 Magazine Mod"` and `"M1911MagazineMod"`
- So category identifier must start with one of those strings

### Custom config file
Use `Category.SetFilePath(Path.Combine("UserData", "MyMod.cfg"))` to save to own file instead of shared MelonPreferences.cfg.

### Settings saved event
Subscribe to `ModSettingsEvents.OnPhonePreferencesSaved` to react when user saves settings in the phone app.
(`OnPreferencesSaved` is deprecated)

## Modding Patterns

### IL2CPP considerations
- Harmony patches on property getters/setters may not work reliably with IL2CPP
- Prefer direct field/property modification via `OnUpdate()` or similar callbacks
- Use `Object.FindObjectsOfType<T>()` to find active game objects
- Use `instance.GetIl2CppType().Name` to check IL2CPP runtime type

### .csproj template
Required references for most mods:
- `net6/MelonLoader.dll`
- `net6/Il2CppInterop.Runtime.dll`
- `net6/0Harmony.dll`
- `Il2CppAssemblies/Assembly-CSharp.dll`
- `Il2CppAssemblies/Il2Cppmscorlib.dll`
- `Il2CppAssemblies/UnityEngine.CoreModule.dll`
- `ModManager&PhoneApp.dll` (for mod manager integration)

### Exploring game code
Use `strings` on DLLs to find class/method names, or use ilspycmd to decompile reference mods.
