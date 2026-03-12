using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(PackRatFPSFix.Core), "PackRat FPS Fix", "1.0.0", "LeeT")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace PackRatFPSFix
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            try
            {
                var packratAsm = FindPackRatAssembly();
                if (packratAsm == null)
                {
                    LoggerInstance.Warning("PackRat not found — PackRat FPS Fix has nothing to patch.");
                    return;
                }

                var updateMethod = packratAsm
                    .GetType("PackRat.Patches.HandoverScreenPatch")
                    ?.GetMethod("Update", BindingFlags.Public | BindingFlags.Static);

                if (updateMethod == null)
                {
                    LoggerInstance.Warning("Could not find HandoverScreenPatch.Update — PackRat version may be incompatible.");
                    return;
                }

                var prefix = new HarmonyMethod(typeof(Core).GetMethod(nameof(SkipUpdate), BindingFlags.NonPublic | BindingFlags.Static));
                HarmonyInstance.Patch(updateMethod, prefix: prefix);

                LoggerInstance.Msg("Patched PackRat HandoverScreenPatch.Update — blocked per-frame ReplaceVehicleTextEverywhere (Open postfix already handles it).");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to patch PackRat: {ex}");
            }
        }

        private static Assembly FindPackRatAssembly()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "PackRat-IL2CPP")
                    return asm;
            }
            return null;
        }

        // Block the Update entirely — the Open postfix already calls
        // ReplaceVehicleTextEverywhere and ReapplyHeaderNextFrame.
        // Running it every frame causes massive FPS drops due to
        // GetComponentsInChildren + reflection on every UI component.
        private static bool SkipUpdate()
        {
            return false;
        }
    }
}
