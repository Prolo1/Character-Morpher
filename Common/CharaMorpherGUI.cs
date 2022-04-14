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
using ADV.Commands.Base;
using BepInEx;
using System.Runtime.InteropServices;

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
			//if(MakerAPI.GetMakerSex() != 1) return;//lets try it out in male maker

			var cfg = CharaMorpher_Core.Instance.cfg;

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
					(control) => cfg.enable.Value,
					(control, val) => { cfg.enable.Value = val; for(int a = 0; a < sliders.Count; ++a) sliders[a].ControlObject.SetActive(val); });

				var enableabmx = e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, CharaMorpher_Core.Instance));
				enableabmx.BindToFunctionController<CharaMorpherController, bool>(
					(control) => cfg.enableABMX.Value,
					(control, val) => { cfg.enableABMX.Value = val; for(int a = abmxIndex; a < sliders.Count; ++a) sliders[a].ControlObject.SetActive(cfg.enable.Value && val); });

				var saveWithMorph = e.AddControl(new MakerToggle(category, "Enable Save With Morph", cfg.saveWithMorph.Value, CharaMorpher_Core.Instance));
				saveWithMorph.BindToFunctionController<CharaMorpherController, bool>(
					(control) => cfg.saveWithMorph.Value,
					(control, val) => { cfg.saveWithMorph.Value = val; });
			}

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

			ImageControls(e, category, CharaMorpher_Core.Instance);

			#region Sliders

			{
				int index = 0;
				sliders.Clear();
				float min = -cfg.sliderExtents.Value * .01f, max = 1 + cfg.sliderExtents.Value * .01f;

				e.AddControl(new MakerText("Body Controls", category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Overall Body", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				   (control) => CharaMorpherController.controls.body,
					(control, val) => { CharaMorpherController.controls.body = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.head,
				  (control, val) => { CharaMorpherController.controls.head = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Boobs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.boob,
				  (control, val) => { CharaMorpherController.controls.boob = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Butt", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.butt,
				  (control, val) => { CharaMorpherController.controls.butt = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Torso", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.torso,
				  (control, val) => { CharaMorpherController.controls.torso = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Arms", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.arm,
				  (control, val) => { CharaMorpherController.controls.arm = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Legs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.leg,
				  (control, val) => { CharaMorpherController.controls.leg = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });



				e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
				e.AddControl(new MakerText("Face Controls", category, CharaMorpher_Core.Instance));

				sliders.Add(e.AddControl(new MakerSlider(category, "Overall Face", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				   (control) => CharaMorpherController.controls.face,
					(control, val) => { CharaMorpherController.controls.face = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Ears", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.ear,
				  (control, val) => { CharaMorpherController.controls.ear = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Eyes", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.eyes,
				  (control, val) => { CharaMorpherController.controls.eyes = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Mouth", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.mouth,
				  (control, val) => { CharaMorpherController.controls.mouth = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				abmxIndex = index;//may use this later

				e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
				e.AddControl(new MakerText("ABMX Body", category, CharaMorpher_Core.Instance));

				sliders.Add(e.AddControl(new MakerSlider(category, "Body", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxBody,
				  (control, val) => { CharaMorpherController.controls.abmxBody = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Boobs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxBoobs,
				  (control, val) => { CharaMorpherController.controls.abmxBoobs = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Butt", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxButt,
				  (control, val) => { CharaMorpherController.controls.abmxButt = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Torso", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxTorso,
				  (control, val) => { CharaMorpherController.controls.abmxTorso = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Arms", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxArms,
				  (control, val) => { CharaMorpherController.controls.abmxArms = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Hands", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxHands,
				  (control, val) => { CharaMorpherController.controls.abmxHands = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Legs", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxLegs,
				  (control, val) => { CharaMorpherController.controls.abmxLegs = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Feet", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxFeet,
				  (control, val) => { CharaMorpherController.controls.abmxFeet = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Genitals", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxGenitals,
				  (control, val) => { CharaMorpherController.controls.abmxGenitals = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });





				e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
				e.AddControl(new MakerText("ABMX Head", category, CharaMorpher_Core.Instance));

				sliders.Add(e.AddControl(new MakerSlider(category, "Head", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxHead,
				  (control, val) => { CharaMorpherController.controls.abmxHead = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
				sliders.Add(e.AddControl(new MakerSlider(category, "Ears", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxEars,
				  (control, val) => { CharaMorpherController.controls.abmxEars = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Eyes", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxEyes,
				  (control, val) => { CharaMorpherController.controls.abmxEyes = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Mouth", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxMouth,
				  (control, val) => { CharaMorpherController.controls.abmxMouth = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });

				sliders.Add(e.AddControl(new MakerSlider(category, "Hair", min, max, cfg.defaults[index++].Value * .01f, CharaMorpher_Core.Instance)));
				sliders.Last().BindToFunctionController<CharaMorpherController, float>(
				 (control) => CharaMorpherController.controls.abmxHair,
				  (control, val) => { CharaMorpherController.controls.abmxHair = (float)Math.Round(val, 2); control.MorphChangeUpdate(); });
			}

			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Init Slider Visibility

			CharaMorpher_Core.Logger.LogDebug($"Setting initial activity p1");
			for(int a = 0; a < sliders.Count; ++a)
				sliders?[a]?.ControlObject?.SetActive(cfg.enable.Value);

			CharaMorpher_Core.Logger.LogDebug($"Setting initial activity p2");
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
			File.ReadAllBytes(path)?.LoadTexture(TextureFormat.RGBA32);

			var img = e.AddControl(new MakerImage(null, category, owner)
			{ Height = 200, Width = 150, Texture = createTexture(Path.Combine(cfg.charDir.Value, cfg.imageName.Value)), });
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
