using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			{
				int index = 0;
				sliders.Clear();
				float min = -cfg.sliderExtents.Value * .01f, max = 1 + cfg.sliderExtents.Value * .01f;

				e.AddControl(new MakerText("Body Controls", category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Overall Body", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				   (ctrl) => CharaMorpherController.controls.body,
					(ctrl, val) => { CharaMorpherController.controls.body = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.head,
				  (ctrl, val) => { CharaMorpherController.controls.head = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Boobs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.boobs,
				  (ctrl, val) => { CharaMorpherController.controls.boobs = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Butt", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.butt,
				  (ctrl, val) => { CharaMorpherController.controls.butt = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Torso", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.torso,
				  (ctrl, val) => { CharaMorpherController.controls.torso = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Arms", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.arms,
				  (ctrl, val) => { CharaMorpherController.controls.arms = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Legs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.legs,
				  (ctrl, val) => { CharaMorpherController.controls.legs = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });



				e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
				e.AddControl(new MakerText("Face Controls", category, CharaMorpher_Core.Instance));

				sliders.Add(e.AddControl(new MakerSlider(category, "Overall Face", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				   (ctrl) => CharaMorpherController.controls.face,
					(ctrl, val) => { CharaMorpherController.controls.face = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Ears", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.ears,
				  (ctrl, val) => { CharaMorpherController.controls.ears = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Eyes", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.eyes,
				  (ctrl, val) => { CharaMorpherController.controls.eyes = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Mouth", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.mouth,
				  (ctrl, val) => { CharaMorpherController.controls.mouth = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				abmxIndex = index;//may use this later

				e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
				e.AddControl(new MakerText("ABMX Body", category, CharaMorpher_Core.Instance));

				sliders.Add(e.AddControl(new MakerSlider(category, "Body", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxBody,
				  (ctrl, val) => { CharaMorpherController.controls.abmxBody = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Boobs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxBoobs,
				  (ctrl, val) => { CharaMorpherController.controls.abmxBoobs = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Butt", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxButt,
				  (ctrl, val) => { CharaMorpherController.controls.abmxButt = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Torso", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxTorso,
				  (ctrl, val) => { CharaMorpherController.controls.abmxTorso = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Arms", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxArms,
				  (ctrl, val) => { CharaMorpherController.controls.abmxArms = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Hands", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxHands,
				  (ctrl, val) => { CharaMorpherController.controls.abmxHands = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Legs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxLegs,
				  (ctrl, val) => { CharaMorpherController.controls.abmxLegs = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Feet", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxFeet,
				  (ctrl, val) => { CharaMorpherController.controls.abmxFeet = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Genitals", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxGenitals,
				  (ctrl, val) => { CharaMorpherController.controls.abmxGenitals = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });





				e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
				e.AddControl(new MakerText("ABMX Head", category, CharaMorpher_Core.Instance));

				sliders.Add(e.AddControl(new MakerSlider(category, "Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxHead,
				  (ctrl, val) => { CharaMorpherController.controls.abmxHead = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Ears", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxEars,
				  (ctrl, val) => { CharaMorpherController.controls.abmxEars = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Eyes", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxEyes,
				  (ctrl, val) => { CharaMorpherController.controls.abmxEyes = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Mouth", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxMouth,
				  (ctrl, val) => { CharaMorpherController.controls.abmxMouth = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Hair", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (ctrl) => CharaMorpherController.controls.abmxHair,
				  (ctrl, val) => { CharaMorpherController.controls.abmxHair = (float)Math.Round(val, 2); inst.StartCoroutine(ctrl.CoMorphUpdate(0)); });
			}

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
			   });

			e.AddControl(new MakerButton("Load Default", category, CharaMorpher_Core.Instance))
			   .OnClick.AddListener(
			  () =>
			  {
				  int count = 0;
				  foreach(var slider in sliders)
					  slider.Value = cfg.defaults[count++].Value * .01f;
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
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
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
			var texPath = strings[0];
			CharaMorpher_Core.Logger.LogDebug($"texture path: {Path.Combine(Path.GetDirectoryName(texPath), Path.GetFileName(texPath))}");

			if(string.IsNullOrEmpty(texPath)) return;

			CharaMorpher_Core.Instance.cfg.charDir.Value = Path.GetDirectoryName(texPath);
			CharaMorpher_Core.Instance.cfg.imageName.Value = Path.GetFileName(texPath);

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
