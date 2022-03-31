using System;
using System.IO;
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
using UnityEngine;
using UnityEngine.UI;
#if HS2||AI
using AIChara;
using HS2;
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
#if HS2
                var saveWindow = GameObject.FindObjectOfType<CharaCustom.CharaCustom>();
                if(saveWindow.GetComponentInChildren<CharaCustom.CvsCaptureMenu>().isActiveAndEnabled)
                {
                    void donothing() { CharaMorpher_Core.Logger.LogDebug("I see nothing, I hear nothing, I DO NOTHING!!!!"); };
                    donothing();//this is very helpful
                }
                else
#endif
                if(!ctrl.reloading)
                {
                    CharaMorpher_Core.Logger.LogDebug("The hook gets called");
                    ctrl.MorphChangeUpdate();
                }
            }


            [HarmonyPrefix]
            [HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
            static void OnButtonClick(Button __instance)
            {
                //  CharaMorpher.CharaMorpher_Core.Logger.LogDebug($"Button Name: {ctrler.name.ToLower()}");
                OnSaveLoadClick(__instance);
                OnExitSaveClick(__instance);
            }


            static void OnSaveLoadClick(Button __instance)
            {

                //reset character to default before saving or loading character 
                var ctrler = __instance.gameObject;
#if HS2
                if(ctrler.GetComponentInParent<CharaCustom.CustomCharaWindow>())
                    if(ctrler.name.ToLower().Contains("overwrite") || ctrler.name.ToLower().Contains("save"))
#elif KKSS
                if(ctrler.name.ToLower().Contains("override") || ctrler.name.ToLower().Contains("save") 
                    || ctrler.name.ToLower().Contains("load") || ctrler.name.ToLower().Contains("screenshot"))
#endif
                        if(!CharaMorpher_Core.Instance.cfg.saveWithMorph.Value)
                            foreach(var hnd in KKAPI.Chara.CharacterApi.RegisteredHandlers)
                                if(hnd.ControllerType == typeof(CharaMorpherController))
                                    foreach(CharaMorpherController ctrl in hnd.Instances)
                                    {
                                        CharaMorpher.CharaMorpher_Core.Logger.LogDebug("The Overwrite Button was called!!!");
                                        ctrl.MorphChangeUpdate(true);
                                    }
            }

            static void OnExitSaveClick(Button __instance)
            {
                //Set character back to normal if save was canceled
                var ctrler = __instance.gameObject;
#if HS2
                if(ctrler.name.ToLower().Contains("no") ||
                    ctrler.GetComponentInParent<CharaCustom.CustomCharaWindow>() && (ctrler.name.ToLower().Contains("exit")))
#elif KKSS
                if(ctrler.name.ToLower().Contains("exit") || ctrler.name.ToLower().Contains("no") /*|| ctrler.name.ToLower().Contains("load")*/)
#endif
                    foreach(var hnd in KKAPI.Chara.CharacterApi.RegisteredHandlers)
                        if(hnd.ControllerType == typeof(CharaMorpherController))
                            foreach(CharaMorpherController ctrl in hnd.Instances)
                            {
                                CharaMorpher.CharaMorpher_Core.Logger.LogDebug("The Overwrite Button was called!!!");
                                ctrl.MorphChangeUpdate();
                            }
            }
        }
    }
}