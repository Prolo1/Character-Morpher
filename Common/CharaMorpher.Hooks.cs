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
#if HS2 || AI
using AIChara;
//using HS2;
#endif

namespace Character_Morpher
{
	public partial class CharaMorpher_Core
	{
		private static class Hooks
		{
			[HarmonyPostfix]
			// [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateClothesStateAll))]
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesStateAll))]
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
			//  [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesStateNext))]
			//  [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesStatePrev))]
#if !(HS2 || AI)
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType),
				new Type[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
#endif
			static void clothsStateUpdate(ChaControl __instance)
			{
				var ctrl = __instance.GetComponent<CharaMorpherController>();
#if HS2 || AI
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
				if(Instance.cfg.enable.Value)
					if(ctrl)
						if(!ctrl.reloading)
						{
							Logger.LogDebug("The hook gets called");
							ctrl.StartCoroutine(ctrl.CoMorphUpdate(1));
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
#elif KKS || KK
				if(ctrler.name.ToLower().Contains("override") || ctrler.name.ToLower().Contains("save")
					|| ctrler.name.ToLower().Contains("load") || ctrler.name.ToLower().Contains("screenshot"))
#endif

					if(Instance.cfg.enable.Value)
						if(!Instance.cfg.saveWithMorph.Value)
							if((KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame) ? Instance.cfg.enableInGame.Value : true)
								foreach(var hnd in CharacterApi.RegisteredHandlers)
									if(hnd.ControllerType == typeof(CharaMorpherController))
										foreach(CharaMorpherController ctrl in hnd.Instances)
										{
											Logger.LogDebug("The Overwrite Button was called!!!");
											ctrl.MorphChangeUpdate(true);
										}
			}

			static void OnExitSaveClick(Button __instance)
			{
				//Set character back to normal if save was canceled
				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
#if HS2 || AI
				if(ctrler.name.ToLower().Contains("exit") || ctrler.name.ToLower().Contains("no"))
#elif KKS || KK
				if(ctrler.name.ToLower().Contains("exit") || ctrler.name.ToLower().Contains("no"))
#endif
					if(Instance.cfg.enable.Value)
						if(!Instance.cfg.saveWithMorph.Value)
							if((KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame) ? Instance.cfg.enableInGame.Value : true)
								foreach(var hnd in CharacterApi.RegisteredHandlers)
									if(hnd.ControllerType == typeof(CharaMorpherController))
										foreach(CharaMorpherController ctrl in hnd.Instances)
										{
											Logger.LogDebug("The Exiting Button was called!!!");
											ctrl.MorphChangeUpdate();
										}
			}
		}
	}
}