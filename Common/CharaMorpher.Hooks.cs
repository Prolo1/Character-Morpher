using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
//using ConfigurationManager.Utilities;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI;
using KKABMX;
using KKABMX.Core;
using UnityEngine;
using UnityEngine.UI;
#if HONEY_API
using AIChara;
//using HS2;
#endif

namespace Character_Morpher
{
	public partial class CharaMorpher_Core
	{
		private static class Hooks
		{
			static Coroutine lastClothsUpdate = null;

			[HarmonyPostfix]
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState)),]
#if !HONEY_API
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType),
				new Type[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
#endif
			static void PostClothsStateUpdate(ChaControl __instance)
			{
				var ctrl = __instance.GetComponent<CharaMorpherController>();
#if HONEY_API
				var saveWindow = GameObject.FindObjectOfType<CharaCustom.CharaCustom>();
				CharaCustom.CvsCaptureMenu capture = null;
				if(saveWindow)
					capture = saveWindow.GetComponentInChildren<CharaCustom.CvsCaptureMenu>();
				if(capture)
					if(capture.isActiveAndEnabled)
					{
						void donothing() { CharaMorpher_Core.Logger.LogDebug("I see nothing, I hear nothing, I DO NOTHING!!!!"); };
						donothing();//this is very helpful
					}
					else
#endif
				if(cfg.enable.Value)
					if(ctrl && !ctrl.reloading)
					{
						Logger.LogDebug("The Post hook gets called");
						if(lastClothsUpdate != null)
							Instance.StopCoroutine(lastClothsUpdate);
						lastClothsUpdate = Instance.StartCoroutine(ctrl.CoMorphUpdate(1, forceChange: true));
					}
			}

			[HarmonyPrefix]
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState)),]

#if !HONEY_API
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType),
				new Type[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
#endif
			static void PreClothsStateUpdate(ChaControl __instance)
			{
				var ctrl = __instance.GetComponent<CharaMorpherController>();
#if HONEY_API
				var saveWindow = GameObject.FindObjectOfType<CharaCustom.CharaCustom>();
				CharaCustom.CvsCaptureMenu capture = null;
				if(saveWindow)
					capture = saveWindow.GetComponentInChildren<CharaCustom.CvsCaptureMenu>();
				if(capture)
					if(capture.isActiveAndEnabled)
					{
						void donothing() { if(cfg.debug.Value) Logger.LogDebug("I see nothing, I hear nothing, I DO NOTHING!!!!"); };
						donothing();//this is very helpful
					}
					else
#endif
				if(cfg.enable.Value)
					if(ctrl && !ctrl.reloading)
					{
						Logger.LogDebug("The Pre hook gets called");
						//Instance.StopAllCoroutines();
						ctrl.MorphChangeUpdate(forceReset: true);
					}
			}


			[HarmonyPrefix]
			[HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
			static void OnButtonClick(Button __instance)
			{
				//  CharaMorpher.CharaMorpher_Core.Logger.LogDebug($"Button Name: {ctrler.name.ToLower()}");

				if(KoikatuAPI.GetCurrentGameMode() != GameMode.Maker) return;

				OnSaveLoadClick(__instance);
				OnExitSaveClick(__instance);
			}


			static void OnSaveLoadClick(Button __instance)
			{

				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
				//reset character to default before saving or loading character 
#if HS2 || AI
				if(ctrler.transform.parent.parent.GetComponentInParent<CharaCustom.CustomCharaWindow>())
					if(ctrler.name.ToLower().Contains("overwrite") || ctrler.name.ToLower().Contains("save"))
#elif KOI_API
				if(ctrler.name.ToLower().Contains("override") || ctrler.name.ToLower().Contains("save")
					|| ctrler.name.ToLower().Contains("load") || ctrler.name.ToLower().Contains("screenshot"))
#endif

					if(cfg.enable.Value)
						if(!cfg.saveWithMorph.Value)
							if(KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame || cfg.enableInGame.Value)
								foreach(var hnd in CharacterApi.RegisteredHandlers)
									if(hnd.ControllerType == typeof(CharaMorpherController))
										foreach(CharaMorpherController ctrl in hnd.Instances)
										{
											if(cfg.debug.Value) Logger.LogDebug("The Overwrite Button was called!!!");
											//Instance.StopAllCoroutines();
											ctrl.MorphChangeUpdate(true);
										}
			}

			static void OnExitSaveClick(Button __instance)
			{
				//Set character back to normal if save was canceled
				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
#if HONEY_API
				if(ctrler.name.ToLower().Contains("exit") || ctrler.name.Contains("No")/*fixes issue with finding false results*/)
#elif KOI_API
				if(ctrler.name.ToLower().Contains("exit") || ctrler.name.Contains("No")/*fixes issue with finding false results*/)
#endif
					if(cfg.enable.Value)
						if(!cfg.saveWithMorph.Value)
							if(KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame || cfg.enableInGame.Value)
								foreach(var hnd in CharacterApi.RegisteredHandlers)
									if(hnd.ControllerType == typeof(CharaMorpherController))
										foreach(CharaMorpherController ctrl in hnd.Instances)
										{
											if(cfg.debug.Value) Logger.LogDebug("The Exiting Button was called!!!");
											//	Instance.StopAllCoroutines();
											ctrl.MorphChangeUpdate();
										}
			}
		}
	}
}