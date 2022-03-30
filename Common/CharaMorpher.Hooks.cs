﻿using System;
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

                {
                    CharaMorpher_Core.Logger.LogDebug("The hook gets called");
                    // CharaMorpher_Core.Logger.LogDebug("remove existing bone mods");

                    ctrl.MorphChangeUpdate();
                }
            }

//#if HS2
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
            static void OnSaveLoadClick(Button __instance)
            {
                //reset character to default before saving or loading character 
                var ctrler = __instance.gameObject;
                if(ctrler.name.ToLower().Contains("overwrite")|| ctrler.name.ToLower().Contains("save"))
                    foreach(var hnd in KKAPI.Chara.CharacterApi.RegisteredHandlers)
                        if(hnd.ControllerType == typeof(CharaMorpherController))
                            foreach(CharaMorpherController ctrl in hnd.Instances)
                            {
                                CharaMorpher.CharaMorpher_Core.Logger.LogDebug("The Overwrite Button was called!!!");
                                ctrl.MorphChangeUpdate();
                                ctrl.UpdateMorphValues(true);
                            }
            }
//#endif

            //   [HarmonyPrefix]
            //    [HarmonyPatch(typeof(MPCharCtrl), nameof(MPCharCtrl.OnClickRoot), typeof(int))]
            //
            //
            //   [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ReloadNoAsync), new Type[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
            //   [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ReloadAsync), new Type[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
            // //  [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), new Type[] { typeof(Stream), typeof(bool), typeof(bool) })]
            // //  [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), new Type[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
            //   static void resetBones(ChaControl __instance)
            //   {
            //     
            // 
            //   }
        }
    }
}