using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;


using KKAPI;
using KKAPI.MainGame;
using KKAPI.Utilities;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Chara;
using UniRx;
using UniRx.Triggers;

#if HONEY_API
using CharaCustom;
using AIChara;
#else
using ChaCustom;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ADV.Commands.Base;
using static KKAPI.Utilities.OpenFileDialog;
using static Character_Morpher.CharaMorpher_Core;

namespace Character_Morpher
{
	class CharaMorpherGUI
	{
		//public class NewImageEvent : UnityEvent<string> { }

		static MakerCategory category;
		internal static void Initialize()
		{
			MakerAPI.RegisterCustomSubCategories += (s, e) =>
			{
				sliders.Clear();
				//Create custom category 

#if HS2 || AI
				MakerCategory peram = MakerConstants.Parameter.Type;
#else
				MakerCategory peram = MakerConstants.Parameter.Character;
#endif
				category = new MakerCategory(peram.CategoryName, "Morph", int.MaxValue, "Chara Morph");
				e.AddSubCategory(category);
			};
			MakerAPI.MakerBaseLoaded += (s, e) => { OnSliderValueChange.RemoveAllListeners(); AddCharaMorpherMenu(e); };
			MakerAPI.MakerFinishedLoading += (s, e) =>
			{

#if HONEY_API
				charaCustom = (CvsO_Type)Resources.FindObjectsOfTypeAll(typeof(CvsO_Type))[0];
#else
				charaCustom = (CvsChara)Resources.FindObjectsOfTypeAll(typeof(CvsChara))[0];
#endif
				//charactortop
			};
		}
#if HONEY_API
		static CvsO_Type charaCustom = null;
#else
		static CvsChara charaCustom = null;
#endif
		static int abmxIndex = -1;
		static List<MakerSlider> sliders = new List<MakerSlider>();

		private static void AddCharaMorpherMenu(RegisterCustomControlsEvent e)
		{
			var inst = CharaMorpher_Core.Instance;

			if(MakerAPI.GetMakerSex() != 1 && !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

			#region Enables

			e.AddControl(new MakerText("Enablers", category, CharaMorpher_Core.Instance));
			var enable = e.AddControl(new MakerToggle(category, "Enable", cfg.enable.Value, CharaMorpher_Core.Instance));

			var enableabmx = e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, CharaMorpher_Core.Instance));

			var saveWithMorph = e.AddControl(new MakerToggle(category, "Enable Save With Morph", cfg.saveWithMorph.Value, CharaMorpher_Core.Instance));
			saveWithMorph.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.saveWithMorph.Value,
				(ctrl, val) => { cfg.saveWithMorph.Value = val; });

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion


			ImageControls(e, inst);

			ButtonDefaults(e, inst);

			#region Sliders

			//creates a slider that controls the bodies' shape
			void CreatSlider(string settingName, int index,
				float min = 0, float max = 1)
			{
				string[] searchHits = new string[] { "overall", "abmx" };
				string visualName = string.Copy(settingName);

				//add space after separator
				if(Regex.IsMatch(settingName, searchHits[0], RegexOptions.IgnoreCase))
				{
					e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space

					string part = Regex.Replace(visualName, searchHits[0], "", RegexOptions.IgnoreCase);
					part = Regex.Replace(part, "  ", " ", RegexOptions.IgnoreCase);
					e.AddControl(new MakerText($"{part} Controls", category, CharaMorpher_Core.Instance));
				}

				//find section index
				if(
					Regex.IsMatch(settingName, searchHits[1], RegexOptions.IgnoreCase)
				)
				{
					abmxIndex = abmxIndex >= 0 ? abmxIndex : index;

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"ABMX index: {abmxIndex}");
				}

				//remove search hits from the slider name
				foreach(var hit in searchHits)
					visualName = Regex.Replace(visualName, hit, "", RegexOptions.IgnoreCase);

				//setup slider
				sliders.Add(e.AddControl(new MakerSlider(category, visualName, min, max, (float)cfg.defaults[index].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
								   (ctrl) => ctrl.controls.all[settingName],
									(ctrl, val) =>
									{
										if(!ctrl) return;
										if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {ctrl.controls.all[settingName]}");
										ctrl.controls.all[settingName] = (float)Math.Round(val, 2);
										inst.StartCoroutine(ctrl.CoMorphUpdate(0));
									});

				var currSlider = sliders.Last();
				OnSliderValueChange.AddListener(() =>
				{
					CharaMorpher_Core.Logger.LogDebug("controls updating");

					foreach(var hnd in CharacterApi.RegisteredHandlers)
						if(hnd.ControllerType == typeof(CharaMorpherController))
							foreach(CharaMorpherController ctrl in hnd.Instances)
								if(currSlider.Value != ctrl.controls.all[settingName])
									if(currSlider.ControlObject)
									{
										currSlider.Value = ctrl.controls.all[settingName];
										CharaMorpher_Core.Logger.LogDebug("control changed");
									}
				});



				//add separator after overall control
				if(Regex.IsMatch(settingName, searchHits[0], RegexOptions.IgnoreCase))
					e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));//create separator line

			}

			void CreateShapeSlider(string settingName, int index)
			{
				CreatSlider(settingName, index, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f);

			}
			void CreateVoiceSlider(string settingName, int index)
			{
				CreatSlider(settingName, index);
				IEnumerator CoOnSliderExists(MakerSlider slider)
				{
					yield return new WaitUntil(() => slider.Exists);//the thing neeeds to exist first

					slider.ControlObject.GetComponentInChildren<Slider>().
						OnPointerUpAsObservable().Subscribe((p) => { charaCustom.PlayVoice(); });
					slider.ControlObject.GetComponentInChildren<Slider>().
						OnSubmitAsObservable().Subscribe((p) => { charaCustom.PlayVoice(); });
				}
				inst.StartCoroutine(CoOnSliderExists(sliders.Last()));
			}

			foreach(var ctrl in CharaMorpher_Core.Instance.controlCategories)
				if(Regex.IsMatch(ctrl.Value, "voice", RegexOptions.IgnoreCase))
					CreateVoiceSlider(ctrl.Value, ctrl.Key);
				else
					CreateShapeSlider(ctrl.Value, ctrl.Key);


			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Init Slider Visibility

			IEnumerator CoSliderDisable(bool val, uint start = 0, uint end = int.MaxValue)
			{

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"CoSlider Called!");
				yield return new WaitWhile(() =>
				{
					for(int a = (int)start; a < Math.Min(sliders.Count, (int)end); ++a)
						if(sliders?[a]?.ControlObject == null) return true;
					return false;
				});

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"sliders are visable: {val}");
				for(int a = (int)start; a < sliders.Count; ++a) sliders?[a]?.ControlObject?.SetActive(val);
			}

			void ShowEnabledSliders()
			{
				inst.StartCoroutine(CoSliderDisable(cfg.enable.Value, start: 0));
				inst.StartCoroutine(CoSliderDisable(cfg.enable.Value && cfg.enableABMX.Value,
					start: (uint)abmxIndex, end: int.MaxValue));
			}

			enable.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.enable.Value,
				(ctrl, val) =>
				{
					cfg.enable.Value = val;
					ShowEnabledSliders();
				});

			enableabmx.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.enableABMX.Value,
				(ctrl, val) =>
				{
					cfg.enableABMX.Value = val;
					ShowEnabledSliders();
				});

			ShowEnabledSliders();
			#endregion



			#region Save/Load Buttons

			CharaMorpher_Core.Logger.LogDebug($"Adding buttons");
			e.AddControl(new MakerButton("Save Default", category, CharaMorpher_Core.Instance))
			   .OnClick.AddListener(
			   () =>
			   {
				   int count = 0;
				   foreach(var def in cfg.defaults)
					   def.Value = sliders[count++].Value * 100f;
				   CharaMorpher_Core.Logger.LogMessage("Saved CharaMorpher Default");
			   });

			e.AddControl(new MakerButton("Load Default", category, CharaMorpher_Core.Instance))
			   .OnClick.AddListener(
			  () =>
			  {
				  int count = 0;
				  foreach(var slider in sliders)
					  slider.Value = (float)cfg.defaults[count++].Value * .01f;
				  CharaMorpher_Core.Logger.LogMessage("Loaded CharaMorpher Default");
			  });
			CharaMorpher_Core.Logger.LogDebug($"Finished adding buttons");
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

		}

		private static void ImageControls(RegisterCustomControlsEvent e, BepInEx.BaseUnityPlugin owner)
		{


			Texture2D createTexture(string path) =>
			File.Exists(path) ?
			File.ReadAllBytes(path)?.LoadTexture(TextureFormat.RGBA32) ?? Texture2D.blackTexture : Texture2D.blackTexture;


			var img = e.AddControl(new MakerImage(null, category, owner)
			{ Height = 200, Width = 150, Texture = createTexture(TargetPath), });
			IEnumerator CoSetTexture(string path)
			{
				for(int a = 0; a < 4; ++a)
					yield return null;

				CharaMorpher_Core.Logger.LogDebug($"The SetTextureCo was called");
				img.Texture = createTexture(path);
			}

			CharaMorpher_Core.OnNewTargetImage.AddListener(
				path =>
				{
					CharaMorpher_Core.Logger.LogDebug($"Calling OnNewTargetImage callback");
					CharaMorpher_Core.Instance.StartCoroutine(CoSetTexture(path));
				});

			var button = e.AddControl(new MakerButton($"Set New Morph Target", category, owner));
			button.OnClick.AddListener(() =>
			{
				ForeGrounder.SetCurrentForground();

				{

					var paths = OpenFileDialog.ShowDialog("Set Morph Target",
						TargetDirectory,
						FileFilter,
						FileExt,
						OpenFileDialog.SingleFileFlags);

					OnFileObtained(paths);
				}
			});

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
		}

		private static void ButtonDefaults(RegisterCustomControlsEvent e, BepInEx.BaseUnityPlugin owner)
		{
			
			//Force Reset Button
			var button = e.AddControl(new MakerButton($"Force Character Reset (WIP)", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	ctrl.StopAllCoroutines();
							ctrl.ForceCardReload();

						}
			});


			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space

			// easy morph buttons
			button = e.AddControl(new MakerButton($"Morph 0%", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	ctrl.StopAllCoroutines();
							for(int a = 0; a < ctrl.controls.all.Count; ++a)
								ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = 0;

							//var tmp = ctrl.controls.overall;
							//for(int a = 0; a < tmp.Count(); ++a)
							//	ctrl.controls.all[tmp.ElementAt(a).Key] = 0;

							Instance.StartCoroutine(ctrl.CoMorphUpdate(0));
							CharaMorpher_Core.Logger.LogMessage("Morphed to 0%");
						}
			});
		
			button = e.AddControl(new MakerButton($"Morph 25%", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	ctrl.StopAllCoroutines();
							for(int a = 0; a < ctrl.controls.all.Count; ++a)
								ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = 1;

							var tmp = ctrl.controls.overall;
							for(int a = 0; a < tmp.Count(); ++a)
								ctrl.controls.all[tmp.ElementAt(a).Key] = .25f;

							Instance.StartCoroutine(ctrl.CoMorphUpdate(0));
							CharaMorpher_Core.Logger.LogMessage("Morphed to 25%");
						}
			});

			button = e.AddControl(new MakerButton($"Morph 50%", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	ctrl.StopAllCoroutines();
							for(int a = 0; a < ctrl.controls.all.Count; ++a)
								ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = 1;

							var tmp = ctrl.controls.overall;
							for(int a = 0; a < tmp.Count(); ++a)
								ctrl.controls.all[tmp.ElementAt(a).Key] = 0.5f;

							Instance.StartCoroutine(ctrl.CoMorphUpdate(0));

							CharaMorpher_Core.Logger.LogMessage("Morphed to 50%");

						}
			});
	
			button = e.AddControl(new MakerButton($"Morph 75%", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	ctrl.StopAllCoroutines();
							for(int a = 0; a < ctrl.controls.all.Count; ++a)
								ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = 1;

							var tmp = ctrl.controls.overall;
							for(int a = 0; a < tmp.Count(); ++a)
								ctrl.controls.all[tmp.ElementAt(a).Key] = 0.75f;

							Instance.StartCoroutine(ctrl.CoMorphUpdate(0));

							CharaMorpher_Core.Logger.LogMessage("Morphed to 75%");

						}
			});

			button = e.AddControl(new MakerButton($"Morph 100%", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	ctrl.StopAllCoroutines();
							for(int a = 0; a < ctrl.controls.all.Count; ++a)
								ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = 1;

							//var tmp = ctrl.controls.overall;
							//for(int a = 0; a < tmp.Count(); ++a)
							//	ctrl.controls.all[tmp.ElementAt(a).Key] = 1;

							Instance.StartCoroutine(ctrl.CoMorphUpdate(0));
							CharaMorpher_Core.Logger.LogMessage("Morphed to 100%");
						}
			});


			//Add Ending
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			//	e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
		}

		static string MakeDirPath(string path) => CharaMorpher_Core.MakeDirPath(path);

		/// <summary>
		/// Called after a file is chosen in file explorer menu  
		/// </summary>
		/// <param name="strings: ">the info returned from file explorer. strings[0] returns the full file path</param>
		private static void OnFileObtained(string[] strings)
		{

			ForeGrounder.RevertForground();
			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Enters accept");
			if(strings == null || strings.Length == 0) return;
			var texPath = MakeDirPath(strings[0]);

			if(cfg.debug.Value)
			{
				CharaMorpher_Core.Logger.LogDebug($"Original path: {texPath}");
				CharaMorpher_Core.Logger.LogDebug($"texture path: {Path.Combine(Path.GetDirectoryName(texPath), Path.GetFileName(texPath))}");
			}


			if(string.IsNullOrEmpty(texPath)) return;

			cfg.charDir.Value = MakeDirPath(Path.GetDirectoryName(texPath));
			cfg.imageName.Value = MakeDirPath((texPath.Substring(texPath.LastIndexOf('/') + 1)));//not sure why this happens on hs2?

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Exit accept");
		}

		public const string FileExt = ".png";
		public const string FileFilter = "Character Images (*.png)|*.png|All files|*.*";

		private static readonly string _defaultOverlayDirectory = Path.Combine(BepInEx.Paths.GameRootPath, "/UserData/chara/");
		public static string TargetDirectory { get => MakeDirPath(Path.GetDirectoryName(TargetPath)); }

		public static string TargetPath
		{
			get
			{
				var tmp = MakeDirPath(Path.GetFileName(cfg.imageName.Value));
				tmp.Substring(tmp.LastIndexOf('/') + 1);
				var path = Path.Combine(MakeDirPath(Path.GetDirectoryName(cfg.charDir.Value)), tmp);

				return File.Exists(path) ? path : Path.Combine(_defaultOverlayDirectory, tmp);
			}
		}




	}
}
