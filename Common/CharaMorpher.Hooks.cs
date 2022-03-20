
using BepInEx;
using BepInEx.Configuration;
using ConfigurationManager.Utilities;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI;
using KKABMX;
using KKABMX.Core;
#if HS2||AI
using AIChara;
#endif

namespace CharaMorpher
{
    public partial class CharaMorpher_Core
    {
        private static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateClothesStateAll))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesStateAll))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesStateNext))]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesStatePrev))]
            static void clothsStateUpdate(ChaControl __instance)
            {
                var ctrl = __instance.GetComponent<CharaMorpherController>();
                if(!ctrl.reloading)
                {
                    CharaMorpher_Core.Logger.LogDebug("The hook gets called");
                    ctrl.MorphChangeUpdate();
                }
            }
        }
    }
}