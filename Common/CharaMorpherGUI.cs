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

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ADV.Commands.Base;


namespace Character_Morpher
{
	class CharaMorpherGUI
	{
		public class NewImageEvent : UnityEvent<string> { }

		internal static void Initialize() =>
				MakerAPI.RegisterCustomSubCategories += AddCharaMorpherMenu;

		static int abmxIndex = 0;
		static List<MakerSlider> sliders = new List<MakerSlider>();

		private static void AddCharaMorpherMenu(object sender, RegisterSubCategoriesEvent e)
		{
			var inst = CharaMorpher_Core.Instance;
			var cfg = inst.cfg;

			if(MakerAPI.GetMakerSex() != 1 && !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

#if HS2 || AI
			MakerCategory peram = MakerConstants.Parameter.Type;
#else
			MakerCategory peram = MakerConstants.Parameter.Character;
#endif
			MakerCategory category = new MakerCategory(peram.CategoryName, "Morph", int.MaxValue, "Chara Morph");
			e.AddSubCategory(category);

			#region Enables

			{
				e.AddControl(new MakerText("Enablers", category, CharaMorpher_Core.Instance));
				var enable = e.AddControl(new MakerToggle(category, "Enable", cfg.enable.Value, CharaMorpher_Core.Instance));
				enable.BindToFunctionController<CharaMorpherController, bool>(
					(ctrl) => cfg.enable.Value,
					(ctrl, val) => { cfg.enable.Value = val; for(int a = 0; a < sliders.Count; ++a) sliders[a].ControlObject.SetActive(val); });

				var enableabmx = e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, CharaMorpher_Core.Instance));
				enableabmx.BindToFunctionController<CharaMorpherController, bool>(
					(ctrl) => cfg.enableABMX.Value,
					(ctrl, val) => { cfg.enableABMX.Value = val; for(int a = abmxIndex; a < sliders.Count; ++a) sliders[a].ControlObject.SetActive(cfg.enable.Value && val); });

				var saveWithMorph = e.AddControl(new MakerToggle(category, "Enable Save With Morph", cfg.saveWithMorph.Value, CharaMorpher_Core.Instance));
				saveWithMorph.BindToFunctionController<CharaMorpherController, bool>(
					(ctrl) => cfg.saveWithMorph.Value,
					(ctrl, val) => { cfg.saveWithMorph.Value = val; });
			}

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

			ImageControls(e, category, inst);



			#region Sliders
#if true

			//creates a slider that controls the bodies' shape
			void CreateShapeSlider(string settingName, int index)
			{
				float min = -cfg.sliderExtents.Value * .01f, max = 1 + cfg.sliderExtents.Value * .01f;
				string[] hits = new string[] { "overall", "abmx" };
				string visualName = string.Copy(settingName);

				if(settingName.ToLower().Contains(hits[0]))
				{
					e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space

					string part = Regex.Replace(visualName, hits[0], "", RegexOptions.IgnoreCase);
					part = Regex.Replace(part, "  ", " ", RegexOptions.IgnoreCase);
					e.AddControl(new MakerText($"{part} Controls", category, CharaMorpher_Core.Instance));
				}

				foreach(var hit in hits)
					visualName = Regex.Replace(visualName, hit, "", RegexOptions.IgnoreCase);

				sliders.Add(e.AddControl(new MakerSlider(category, visualName, min, max, cfg.defaults[index].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
								   (ctrl) => ctrl.controls.all[settingName],
									(ctrl, val) => { ctrl.controls.all[settingName] = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				if(settingName.ToLower().Contains(hits[0]))
					e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));//create separator line

			}

			foreach(var ctrl in CharaMorpher_Core.Instance.controlCategories)
				CreateShapeSlider(ctrl.Value, ctrl.Key);


#endif

			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Init Slider Visibility

			CharaMorpher_Core.Logger.LogDebug($"Setting initial activity part1");
			for(int a = 0; a < sliders.Count; ++a)
				sliders?[a]?.ControlObject?.SetActive(cfg.enable.Value);

			CharaMorpher_Core.Logger.LogDebug($"Setting initial activity part2");
			for(int a = abmxIndex; a < sliders.Count; ++a)
				sliders?[a]?.ControlObject?.SetActive(cfg.enable.Value && cfg.enableABMX.Value);
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
					  slider.Value = cfg.defaults[count++].Value * .01f;
				  CharaMorpher_Core.Logger.LogMessage("Loaded CharaMorpher Default");
			  });
			CharaMorpher_Core.Logger.LogDebug($"Finished adding buttons");
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

		}

		private static void ImageControls(RegisterSubCategoriesEvent e, MakerCategory category, BepInEx.BaseUnityPlugin owner)
		{
			var cfg = CharaMorpher_Core.Instance.cfg;


			Texture2D createTexture(string path) =>
			File.Exists(path) ?
			File.ReadAllBytes(path)?.LoadTexture(TextureFormat.RGBA32) ?? Texture2D.blackTexture : Texture2D.blackTexture;


			var img = e.AddControl(new MakerImage(null, category, owner)
			{ Height = 200, Width = 150, Texture = createTexture(CharaMorpher_Core.MakeDirPath(Path.Combine(OverlayDirectory, cfg.imageName.Value))), });
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

			var button = e.AddControl(new MakerButton($"Set New Image Target", category, owner));
			button.OnClick.AddListener(() =>
			{
				ForeGrounder.SetCurrentForground();

				OpenFileDialog.Show(
				strings => OnFileAccept(strings),
				"Set Lookup Texture",
				OverlayDirectory,
				FileFilter,
				FileExt);
			});

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			//	e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
		}

		private static void ButtonDefaults(RegisterSubCategoriesEvent e, MakerCategory category, BepInEx.BaseUnityPlugin owner)
		{
			throw new NotImplementedException();

#pragma warning disable CS0162 // Unreachable code detected

			//Force Reset Button
			var button = e.AddControl(new MakerButton($"Force Character Reset", category, owner));
			button.OnClick.AddListener(() =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							ctrl.StopAllCoroutines();
							ctrl.ForceCardReload();

						}
			});
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space


			//Add Ending
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
#pragma warning restore CS0162 // Unreachable code detected
		}

		/// <summary>
		/// Called after a file is chosen in file explorer menu  
		/// </summary>
		/// <param name="strings: ">the info returned from file explorer. strings[0] returns the full file path</param>
		private static void OnFileAccept(string[] strings)
		{
			CharaMorpher_Core.Logger.LogDebug($"Enters accept");
			if(strings == null || strings.Length == 0) return;
			string MakeDirPath(string path) => CharaMorpher_Core.MakeDirPath(path);
			var texPath = MakeDirPath(strings[0]);

			CharaMorpher_Core.Logger.LogDebug($"Original path: {texPath}");
			CharaMorpher_Core.Logger.LogDebug($"texture path: {Path.Combine(Path.GetDirectoryName(texPath), Path.GetFileName(texPath))}");

			if(string.IsNullOrEmpty(texPath)) return;

			CharaMorpher_Core.Instance.cfg.charDir.Value = Path.GetDirectoryName(texPath);
			CharaMorpher_Core.Instance.cfg.imageName.Value = Path.GetFileName(texPath.Substring(texPath.LastIndexOf('/') + 1));//not sure why this happens on hs2?

			ForeGrounder.RevertForground();
			CharaMorpher_Core.Logger.LogDebug($"Exit accept");
		}

		public const string FileExt = ".png";
		public const string FileFilter = "Character Images (*.png)|*.png|All files|*.*";

		private static readonly string _defaultOverlayDirectory = Path.Combine(BepInEx.Paths.GameRootPath, "UserData/chara");
		public static string OverlayDirectory
		{
			get
			{
				var path = CharaMorpher_Core.MakeDirPath(CharaMorpher_Core.Instance.cfg.charDir.Value);
				return Directory.Exists(path) ? path : _defaultOverlayDirectory;
			}
		}


	}
}
