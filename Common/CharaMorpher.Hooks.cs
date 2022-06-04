using System;
using System.Collections;
using System.Collections.Generic;
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
			static Coroutine m_lastClothsUpdate = null;

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
					if(ctrl && !ctrl.reloading && ctrl.initLoadFinished)
					{
						Logger.LogDebug("The Post hook gets called");
						if(m_lastClothsUpdate != null)
							Instance.StopCoroutine(m_lastClothsUpdate);
						m_lastClothsUpdate = Instance.StartCoroutine(ctrl.CoMorphUpdate(1, forceChange: true));
					}


			}


#if KOI_API
			[HarmonyPostfix]
			[HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile),
				new Type[] { typeof(BinaryReader), typeof(bool), typeof(bool) }),]
			static void GetCharaPngs(ChaFile __instance)
			{
				if(KoikatuAPI.GetCurrentGameMode() != GameMode.Maker) return;

				var _png = (byte[])__instance.pngData?.Clone();
				var _facePng = (byte[])__instance.facePngData?.Clone();

				if(_png != null)
					Logger.LogDebug("Character png file exists");
				if(_facePng != null)
					Logger.LogDebug("Character face png file exists");

				IEnumerator DelayedPngSet(CharaMorpherController ctrl, byte[] png, byte[] facePng)
				{
					for(int a = 0; a < 0; ++a)
						yield return null;
					ctrl.ChaFileControl.pngData = png ?? ctrl.ChaFileControl.pngData;
					ctrl.ChaFileControl.facePngData = facePng ?? ctrl.ChaFileControl.facePngData;
					yield break;
				}

				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//if(m_lastpngload != null)
							//	Instance.StopCoroutine(m_lastpngload);
							Instance.StartCoroutine(DelayedPngSet(ctrl, _png, _facePng));
						}

			}
#endif



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