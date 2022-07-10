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
			[HarmonyPrefix]
			[HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile),
				new Type[] {
					typeof(Stream),
#if HONEY_API
					typeof (int)
#endif
				}),]
			static void PreLoadCoordinate(ChaFileCoordinate __instance)
			{
				if(cfg.enable.Value)
					if(MakerAPI.InsideMaker || cfg.enableInGame.Value)
						if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)

							foreach(var hnd in CharacterApi.RegisteredHandlers)
								if(hnd.ControllerType == typeof(CharaMorpherController))
									foreach(CharaMorpherController ctrl in hnd.Instances)
									{
										if(ctrl && !ctrl.reloading && ctrl.initLoadFinished)
										{
											Logger.LogDebug("Coordinate being loaded");
											//for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
											ctrl.MorphChangeUpdate(forceReset: true);
											ctrl.GetComponent<BoneController>().NeedsFullRefresh = true;

										}
									}
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.LoadFile),
				new Type[] {
					typeof(Stream),
#if HONEY_API
					typeof (int)
#endif
				}),]
			static void PostLoadCoordinate(ChaFileCoordinate __instance)
			{
				if(cfg.enable.Value)
					if(MakerAPI.InsideMaker || cfg.enableInGame.Value)
						if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)

							foreach(var hnd in CharacterApi.RegisteredHandlers)
								if(hnd.ControllerType == typeof(CharaMorpherController))
									foreach(CharaMorpherController ctrl in hnd.Instances)
									{

										if(ctrl && !ctrl.reloading && ctrl.initLoadFinished)
										{
											Logger.LogDebug("Coordinate has been selected");
											for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
												ctrl.StartCoroutine(ctrl.CoMorphUpdate(10));

											ctrl.StartCoroutine(ctrl.CoFullBoneRrfresh(10));
										}
									}
			}
#endif



			static Coroutine m_lastClothsUpdate, m_lastClothsFBR;
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

				var saveWindow = FindObjectOfType<CharaCustom.CharaCustom>();
				CvsCaptureMenu capture = null;

				if(saveWindow)
					capture = saveWindow.GetComponentInChildren<CvsCaptureMenu>();
				if(capture)
					if(capture.isActiveAndEnabled)
					{
						void donothing() { Logger.LogDebug("I see nothing, I hear nothing, I DO NOTHING!!!!"); };
						donothing();//this is very helpful
					}
					else
#endif
				if(cfg.enable.Value)
					if(MakerAPI.InsideMaker || cfg.enableInGame.Value)
						if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)
							if(ctrl && !ctrl.reloading && ctrl.initLoadFinished)
							{
								if(cfg.debug.Value) Logger.LogDebug("The Post hook gets called");
								if(m_lastClothsUpdate != null)
									Instance.StopCoroutine(m_lastClothsUpdate);
								m_lastClothsUpdate = ctrl.StartCoroutine(ctrl.CoMorphUpdate(7, forceChange: true));

								if(m_lastClothsFBR != null)
									Instance.StopCoroutine(m_lastClothsFBR);
								m_lastClothsFBR = ctrl.StartCoroutine(ctrl.CoFullBoneRrfresh(8));

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

				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
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



			[HarmonyPrefix]
			[HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
			static void OnButtonClick(Button __instance)
			{
				//  CharaMorpher.CharaMorpher_Core.Logger.LogDebug($"Button Name: {ctrler.name.ToLower()}");

				if(!__instance.interactable) return;

				if(!MakerAPI.InsideMaker) return;

				OnSaveLoadClick(__instance);
				OnExitSaveClick(__instance);
				OnLoadClick(__instance);
			}

			/// <summary>
			/// Resets the character before a new one is loaded
			/// </summary>
			/// <param name="__instance"></param>
			static void OnLoadClick(Button __instance)
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
						foreach(var hnd in CharacterApi.RegisteredHandlers)
							if(hnd.ControllerType == typeof(CharaMorpherController))
								foreach(CharaMorpherController ctrl in hnd.Instances)
								{
									Logger.LogDebug("The Load Button was called!!!");
									//Instance.StopAllCoroutines();
									for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
										ctrl.MorphChangeUpdate(forceReset: true);

									ctrl.GetComponent<BoneController>().NeedsFullRefresh = true;

									ctrl.reloading = true;
									//ctrl.StartCoroutine(ctrl.CoMorphUpdate(delay:10,forceReset: true, forceChange: true));

									//	ctrl.ForceCardReload();
								}
			}

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
				if(ctrler.name.ToLower().Contains("override") || ctrler.name.ToLower().Contains("save")
					|| ctrler.name.ToLower().Contains("load") || ctrler.name.ToLower().Contains("screenshot"))
#endif

					if(cfg.enable.Value && !cfg.saveWithMorph.Value)
						if(KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame || cfg.enableInGame.Value)
							if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)
								foreach(var hnd in CharacterApi.RegisteredHandlers)
									if(hnd.ControllerType == typeof(CharaMorpherController))
										foreach(CharaMorpherController ctrl in hnd.Instances)
										{
											if(cfg.debug.Value) Logger.LogDebug("The Overwrite Button was called!!!");
											//Instance.StopAllCoroutines();
											for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
												ctrl.MorphChangeUpdate(forceReset: true);

											ctrl.GetComponent<BoneController>().NeedsFullRefresh = true;

											//ctrl.StartCoroutine(ctrl.CoMorphUpdate(delay:10,forceReset: true, forceChange: true));
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
								if(!MakerAPI.InsideMaker || MakerAPI.GetMakerSex() != 0 || cfg.enableInMaleMaker.Value)
									foreach(var hnd in CharacterApi.RegisteredHandlers)
										if(hnd.ControllerType == typeof(CharaMorpherController))
											foreach(CharaMorpherController ctrl in hnd.Instances)
											{
												if(cfg.debug.Value) Logger.LogDebug("The Exiting Button was called!!!");
												//	Instance.StopAllCoroutines();
												for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
													ctrl.MorphChangeUpdate();
												ctrl.GetComponent<BoneController>().NeedsFullRefresh = true;

											}
			}
		}
	}
}