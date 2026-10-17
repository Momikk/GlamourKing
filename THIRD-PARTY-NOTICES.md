# Third-party software

SuitKing includes a source snapshot of [KamiToolKit](https://github.com/Dalamud-DailyRoutines/KamiToolKit), originally by [MidoriKami](https://github.com/MidoriKami/KamiToolKit), distributed under the MIT license.

Upstream commit: `e6f316bcb06da25020f2d300345b339789fa7efb` (Dalamud-DailyRoutines fork).

The full copyright and MIT license text are preserved in [vendor/KamiToolKit/LICENSE](vendor/KamiToolKit/LICENSE) and included in the plugin download as `licenses/KamiToolKit-MIT.txt`. SuitKing itself remains AGPL-3.0-only.

Local changes: build with Dalamud.NET.Sdk 15 against the CN development assemblies; isolate the dependency assembly as SuitKing.KamiToolKit; use nullable annotations without the .NET 10.0.203 flow-analysis compiler crash; replace two collection `with` expressions with equivalent owned ReadOnlySeString copies. No Daily Routines module code is copied or required at runtime.
## Cabinet workflow references

The cabinet candidate lookup and native storage entry point were checked against [HaselTweaks CabinetQuickStoreWindow](https://github.com/Haselnussbomber/HaselTweaks/blob/main/HaselTweaks/Windows/CabinetQuickStoreWindow.cs) (AGPL-3.0) and [FFXIVClientStructs](https://github.com/aers/FFXIVClientStructs). SuitKing implements its own inventory filtering, batch controller and native UI; neither reference project is bundled as a runtime dependency. This project is distributed under AGPL-3.0-only.
