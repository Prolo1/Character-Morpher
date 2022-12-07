﻿using System;
using System.Collections;
using System.Linq;
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
using KKAPI.Maker;
using KKABMX;
using KKABMX.Core;
using UnityEngine;
using UnityEngine.UI;

#if HONEY_API
using CharaCustom;
using AIChara;
#else
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

#if KOI_API
			/*
			[HarmonyPostfix]
			[HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.CreateChara)),]
			static void FastReload(ChaControl __instance)
			{
				foreach(CharaMorpherController ctrl in MyUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					if(ctrl.ChaFileControl != __instance.chaFile ||
						CharaMorpherController.morphTarget?.extraCharacter?.chaFile == ctrl.ChaFileControl) continue;

					CharaMorpher_Core.Logger.LogDebug("Please load first... I kinda need this");
					ctrl.OnCharaReload(KoikatuAPI.GetCurrentGameMode());

					break;
				}
			}
			*/

			/*
			//static Coroutine coFaceFix=null;
			[HarmonyPostfix]
			[HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
			static void faceFix(ChaControl __instance)
			{
				var ctrl = __instance?.GetComponent<CharaMorpherController>();
				if(MakerAPI.InsideMaker || ((!ctrl?.initLoadFinished) ?? true)) return;
				if((ctrl?.reloading ?? true)) return;


				//	if(coFaceFix != null) Instance.StopCoroutine(coFaceFix);
				ctrl?.MorphChangeUpdate();
			}
			*/


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

				foreach(CharaMorpherController ctrl in MyUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					//if(m_lastpngload != null)
					//	Instance.StopCoroutine(m_lastpngload);
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

				Logger.LogDebug("toggle was pressed");
				OnFaceBonemodToggleClick(__instance);
				OnBodyBonemodToggleClick(__instance);
			}

			static void OnFaceBonemodToggleClick(Toggle __instance)
			{
				var txtPro = __instance?.GetComponentInChildren<TMPro.TMP_Text>();
				var txt = __instance?.GetComponentInChildren<Text>();

				if(txtPro ? txtPro.text.ToLower().Contains("face bonemod") : false ||
					txt ? txt.text.ToLower().Contains("face bonemod") : false)
				{
					if(cfg.debug.Value) Logger.LogDebug("Change to face bonemod toggle");
					CharaMorpherController.faceBonemodTgl = __instance.isOn;
				}
			}

			static void OnBodyBonemodToggleClick(Toggle __instance)
			{
				var txtPro = __instance.GetComponentInChildren<TMPro.TMP_Text>();
				var txt = __instance.GetComponentInChildren<Text>();

				if(txtPro ? txtPro.text.ToLower().Contains("body bonemod") : false ||
					txt ? txt.text.ToLower().Contains("body bonemod") : false)
				{
					if(cfg.debug.Value) Logger.LogDebug("Change to body bonemod toggle");
					CharaMorpherController.bodyBonemodTgl = __instance.isOn;
				}
			}


			[HarmonyPrefix]
			[HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
			static void OnPreButtonClick(Button __instance)
			{
				//  CharaMorpher.CharaMorpher_Core.Logger.LogDebug($"Button Name: {ctrler.name.ToLower()}");

				if(!__instance.interactable) return;

				if(!MakerAPI.InsideMaker) return;

				OnSaveLoadClick(__instance);
				OnExitSaveClick(__instance);
				//	OnCharaLoadClick(__instance);
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
				if(ctrler.GetComponentInParent<CvsO_CharaLoad>())
					if(ctrler.name.ToLower().Contains("overwrite"))
#elif KOI_API

				if(ctrler.GetComponentInParent<CustomCharaFile>())
					if(ctrler.name.ToLower().Contains("load"))
#endif
						foreach(CharaMorpherController ctrl in MyUtil.GetFuncCtrlOfType<CharaMorpherController>())
						{
							Logger.LogDebug("The Chara Load Button was called!!!");

							for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
								ctrl.MorphChangeUpdate(forceReset: true);

						}
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
				if(ctrler.GetComponentInParent<CvsC_ClothesLoad>())
					if(ctrler.name.ToLower().Contains("overwrite"))
#elif KOI_API

				if(ctrler.GetComponentInParent<CustomCoordinateFile>())
					if(ctrler.name.ToLower().Contains("load"))
#endif
						foreach(CharaMorpherController ctrl in MyUtil.GetFuncCtrlOfType<CharaMorpherController>())
						{
							Logger.LogDebug("The Coord Load Button was called!!!");

							for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
								ctrl.MorphChangeUpdate();
						}
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
				if(ctrler.transform.parent?.parent?.GetComponentInParent<CharaCustom.CustomCharaWindow>())
					if(ctrler.name.ToLower().Contains("overwrite") || ctrler.name.ToLower().Contains("save"))
#elif KOI_API
				//	if(ctrler.transform.parent.parent.GetComponentInParent<ChaCustom.cvs>())
				if(ctrler.name.ToLower().Contains("reload")) return;
				if(ctrler.name.ToLower().Contains("override") || ctrler.name.ToLower().Contains("save")
					|| ctrler.name.ToLower().Contains("load") || ctrler.name.ToLower().Contains("screenshot"))
#endif

					if(cfg.enable.Value && !cfg.saveWithMorph.Value)
						if(KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame || cfg.enableInGame.Value)
							if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)
								foreach(CharaMorpherController ctrl in MyUtil.GetFuncCtrlOfType<CharaMorpherController>())
								{
									if(cfg.debug.Value) Logger.LogDebug("The Overwrite Button was called!!!");

									for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
										ctrl.MorphChangeUpdate(forceReset: true);
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
				if(ctrler.name.ToLower().Contains("exit") || ctrler.name.Contains("No")/*fixes issue with finding false results*/)
#elif KOI_API
				if(ctrler.name.ToLower().Contains("exit") || ctrler.name.Contains("No")/*fixes issue with finding false results*/)
#endif
					if(cfg.enable.Value && !cfg.saveWithMorph.Value)
						if(KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame || cfg.enableInGame.Value)
							if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)

								foreach(CharaMorpherController ctrl in MyUtil.GetFuncCtrlOfType<CharaMorpherController>())
								{
									if(cfg.debug.Value) Logger.LogDebug("The Exiting Button was called!!!");

									for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
										ctrl.MorphChangeUpdate();
								}
			}
		}
	}
}