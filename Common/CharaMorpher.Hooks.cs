using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI;
using KKAPI.Maker;
using KKABMX;
using KKABMX.Core;
using UnityEngine;
using UnityEngine.UI;
using Manager;


#if HONEY_API
using CharaCustom;
using AIChara;
#else
using StrayTech;
using ChaCustom;
#endif

namespace Character_Morpher
{
	public partial class CharaMorpher_Core
	{
		private static class Hooks
		{

			public static void Init()
			{
				Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
			}


			static void UpdateCurrentCharacters(bool forcereset = false)
			{

				if(MakerAPI.InsideMaker || cfg.enableInGame.Value)//Make sure the in-game flag is checked
					foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
					{
						if(!ctrl) continue;
						if(ctrl.initLoadFinished && !ctrl.reloading)
							ctrl.MorphChangeUpdate(forceReset: forcereset);
					}
			}


#if KOI_API

#if KK
		
			[HarmonyPrefix]
			[HarmonyPatch(typeof(SimpleFade), nameof(SimpleFade.FadeSet))]
			static void OnSceneLoad()
			{
				if(!MakerAPI.InsideMaker) UpdateCurrentCharacters(true);
			}


			[HarmonyPostfix]
			[HarmonyPatch(typeof(SimpleFade), nameof(SimpleFade.FadeSet))]
			static void OnSceneFadeIn(SimpleFade __instance)
			{
				IEnumerator after(SimpleFade inst)
				{
					yield return new WaitUntil(() => inst.IsEnd);
					for(int a = -1; a < cfg.reloadTest.Value; ++a)
						yield return null;

					if(inst._Fade == SimpleFade.Fade.Out)
						if(!MakerAPI.InsideMaker) UpdateCurrentCharacters();
					
				}

				Instance?.StartCoroutine(after(__instance));
			}

#else

			[HarmonyPrefix]
			[HarmonyPatch(typeof(FadeCanvas), nameof(FadeCanvas.StartAysnc),
				new Type[] { typeof(FadeCanvas.Fade), typeof(float), typeof(bool), typeof(bool), }),]
			static void OnSceneLoad()
			{
				if(!MakerAPI.InsideMaker) UpdateCurrentCharacters(true);
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(FadeCanvas), nameof(FadeCanvas.StartAysnc),
				new Type[] { typeof(FadeCanvas.Fade), typeof(float), typeof(bool), typeof(bool), }),]
			static void OnSceneUnLoad(FadeCanvas.Fade __0)
			{
				IEnumerator after()
				{
					for(int a = -1; a < cfg.reloadTest.Value; ++a)
						yield return null;

					if(!MakerAPI.InsideMaker) UpdateCurrentCharacters();
				}

				//if((__0 & FadeCanvas.Fade.Out) > 0)
				if(__0 == FadeCanvas.Fade.Out)
					if(!MakerAPI.InsideMaker) Instance?.StartCoroutine(after());
			}
#endif

			[HarmonyPostfix]
			[HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile),
				new Type[] { typeof(BinaryReader), typeof(bool), typeof(bool) }),]
			static void GetCharaPngs(ChaFile __instance)
			{
				if(KoikatuAPI.GetCurrentGameMode() != GameMode.Maker) return;

				var _png = (byte[])__instance.pngData?.Clone();
				var _facePng = (byte[])__instance.facePngData?.Clone();

				if(_png != null)
					if(cfg.debug.Value) Logger.LogDebug("Character png file exists");
				if(_facePng != null)
					if(cfg.debug.Value) Logger.LogDebug("Character face png file exists");

				IEnumerator DelayedPngSet(CharaMorpherController ctrl, byte[] png, byte[] facePng)
				{
					for(int a = 0; a < 0; ++a)
						yield return null;

					ctrl.ChaFileControl.pngData = png ?? ctrl.ChaFileControl.pngData;
					ctrl.ChaFileControl.facePngData = facePng ?? ctrl.ChaFileControl.facePngData;
					yield break;
				}

				foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{

#if !KK
					if(ctrl.ChaControl.chaFile == __instance)
#endif
						Instance.StartCoroutine(DelayedPngSet(ctrl, _png, _facePng));
				}

			}
#endif


			[HarmonyPostfix]
			[HarmonyPatch(typeof(Toggle), nameof(Toggle.OnPointerClick))]
			static void OnPostToggleClick(Toggle __instance)
			{
				if(!__instance.interactable) return;

				if(!MakerAPI.InsideMaker) return;

				OnFaceBonemodToggleClick(__instance);
				OnBodyBonemodToggleClick(__instance);
			}

			static void OnFaceBonemodToggleClick(Toggle __instance)
			{
				var txtPro = __instance?.GetComponentInChildren<TMPro.TMP_Text>();
				var txt = __instance?.GetComponentInChildren<Text>();

				if((txtPro ? txtPro.text.ToLower().Contains("face bonemod") : false) ||
					(txt ? txt.text.ToLower().Contains("face bonemod") : false))
				{
					if(cfg.debug.Value) Logger.LogDebug("Change to face bonemod toggle");
					CharaMorpherController.faceBonemodTgl = __instance.isOn;
				}

			}

			static void OnBodyBonemodToggleClick(Toggle __instance)
			{
				var txtPro = __instance.GetComponentInChildren<TMPro.TMP_Text>();
				var txt = __instance.GetComponentInChildren<Text>();

				if((txtPro ? txtPro.text.ToLower().Contains("body bonemod") : false) ||
					(txt ? txt.text.ToLower().Contains("body bonemod") : false))
				{
					if(cfg.debug.Value) Logger.LogDebug("Change to body bonemod toggle");
					CharaMorpherController.bodyBonemodTgl = __instance.isOn;
				}
			}


			[HarmonyPrefix]
			[HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
			static void OnPreButtonClick(Button __instance)
			{


				if(!__instance.interactable) return;

				if(!MakerAPI.InsideMaker) return;

				OnSaveLoadClick(__instance);
				OnExitSaveClick(__instance);

				OnCoordLoadClick(__instance);
			}

			/// <summary>
			/// Resets the character before a new one is loaded
			/// </summary>
			/// <param name="__instance"></param>
			static void OnCharaLoadClick(Button __instance)
			{
				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
				//reset character to default before saving or loading character 
#if HONEY_API
				if(!(ctrler.GetComponentInParent<CvsO_CharaLoad>())) return;
				if(!(ctrler.name.ToLower().Contains("overwrite"))) return;
#elif KOI_API

				if(!(ctrler.GetComponentInParent<CustomCharaFile>())) return;
				if(!(ctrler.name.ToLower().Contains("load"))) return;
#endif
				if(cfg.debug.Value) Logger.LogDebug("The Chara Load Button was called!!!");
				UpdateCurrentCharacters(true);
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="__instance"></param>
			static void OnCoordLoadClick(Button __instance)
			{
				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
				//reset character to default before saving or loading character 
#if HONEY_API
				if(!(ctrler.GetComponentInParent<CvsC_ClothesLoad>())) return;
				if(!(ctrler.name.ToLower().Contains("overwrite"))) return;
#elif KOI_API

				if(!(ctrler.GetComponentInParent<CustomCoordinateFile>())) return;
				if(!(ctrler.name.ToLower().Contains("load"))) return;
#endif
				if(cfg.debug.Value) Logger.LogDebug("The Coord Load Button was called!!!");
				UpdateCurrentCharacters();
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="__instance"></param>
			static void OnSaveLoadClick(Button __instance)
			{

				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
				//reset character to default before saving or loading character 
#if HONEY_API
				if(!(ctrler.transform.parent?.parent?.GetComponentInParent<CharaCustom.CustomCharaWindow>())) return;
				if(!(ctrler.name.ToLower().Contains("overwrite") || ctrler.name.ToLower().Contains("save"))) return;
#elif KOI_API

				if(ctrler.name.ToLower().Contains("reload")) return;//edge case
				if(!(ctrler.name.ToLower().Contains("override") || ctrler.name.ToLower().Contains("save")
					|| ctrler.name.ToLower().Contains("load") || ctrler.name.ToLower().Contains("screenshot"))) return;
#endif

				if(cfg.debug.Value) Logger.LogDebug("The Overwrite Button was called!!!");
				if(cfg.enable.Value && cfg.saveAsMorphData.Value)
				{
					UpdateCurrentCharacters(true);
					UpdateCurrentCharacters();
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="__instance"></param>
			static void OnExitSaveClick(Button __instance)
			{
				//Set character back to normal if save was canceled
				var ctrler = __instance.gameObject;
				if(!ctrler || ctrler.name.IsNullOrEmpty()) return;
#if HONEY_API
				if(!(ctrler.name.ToLower().Contains("exit") || ctrler.name.Contains("No")/*fixes issue with finding false results*/)) return;
#elif KOI_API
				if(!(ctrler.name.ToLower().Contains("exit") || ctrler.name.Contains("No")/*fixes issue with finding false results*/)) return;
#endif
				if(cfg.debug.Value) Logger.LogDebug("The Exiting Button was called!!!");
				if(cfg.enable.Value && cfg.saveAsMorphData.Value)
					UpdateCurrentCharacters();
			}


		}
	}
}