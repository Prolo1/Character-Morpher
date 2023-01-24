using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;



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
using UnityEngine.UI.Collections;
using UnityEngine.Events;

using UnityEngine.UI;


using static Character_Morpher.CharaMorpher_Core;
using KKABMX.Core;
using Illusion.Extensions;
using BepInEx;

namespace Character_Morpher
{
	class CharaMorpherGUI
	{
		private static MakerCategory category;

		private static Coroutine lastExtent;
		public static readonly string subCatagoryName = "Morph";
		public static readonly string displayName = "Chara Morph";
		
		internal static void Initialize()
		{
			MakerAPI.RegisterCustomSubCategories += (s, e) =>
			{
				//Create custom category 

#if HS2 || AI
				MakerCategory peram = MakerConstants.Parameter.Type;
#else
				MakerCategory peram = MakerConstants.Parameter.Character;
#endif
				category = new MakerCategory(peram.CategoryName, subCatagoryName, displayName: displayName);

				e.AddSubCategory(category);
			};
			MakerAPI.MakerBaseLoaded += (s, e) => { OnSliderValueChange.RemoveAllListeners(); AddCharaMorpherMenu(e); };
			MakerAPI.MakerFinishedLoading += (s, e) =>
			{


#if HONEY_API
				charaCustom = (CvsO_Type)Resources.FindObjectsOfTypeAll(typeof(CvsO_Type))[0];
				boobCustom = (CvsB_ShapeBreast)Resources.FindObjectsOfTypeAll(typeof(CvsB_ShapeBreast))[0];
				bodyCustom = (CvsB_ShapeWhole)Resources.FindObjectsOfTypeAll(typeof(CvsB_ShapeWhole))[0];
				faceCustom = (CvsF_ShapeWhole)Resources.FindObjectsOfTypeAll(typeof(CvsF_ShapeWhole))[0];
#else
				charaCustom = (CvsChara)Resources.FindObjectsOfTypeAll(typeof(CvsChara))[0];
				bodyCustom = (CvsBodyShapeAll)Resources.FindObjectsOfTypeAll(typeof(CvsBodyShapeAll))[0];
				boobCustom = (CvsBreast)Resources.FindObjectsOfTypeAll(typeof(CvsBreast))[0];
				faceCustom = (CvsFaceShapeAll)Resources.FindObjectsOfTypeAll(typeof(CvsFaceShapeAll))[0];
#endif

#if HONEY_API

				GameObject subcatagory =
				//I hate this as much as you lol
				GameObject.Find($"CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuOption/Scroll View/Viewport/Content/Category(Clone)/CategoryTop/{subCatagoryName}");

				subcatagory?
				.GetComponent<UI_ButtonEx>()?
				.onClick?.AddListener(() => charaCustom.customBase.drawMenu.ChangeMenuFunc());
#endif
			};
			MakerAPI.MakerExiting += (s, e) => { Cleanup(); };
			cfg.sliderExtents.SettingChanged += (m, n) =>
			{
				IEnumerator CoEditExtents(uint start = 0, uint end = int.MaxValue)
				{
					yield return new WaitWhile(() =>
					{
						for(int a = (int)start; a < Math.Min(sliders.Count, (int)end); ++a)
							if(sliders?[a]?.ControlObject == null) return true;
						return false;
					});

					float
					min = -cfg.sliderExtents.Value * .01f,
					max = 1 + cfg.sliderExtents.Value * .01f;
					int count = 0;
					foreach(var slider in sliders)
					{
						if(count++ < start) continue;
						if(count - 1 >= Math.Min(sliders.Count, (int)end)) break;


						slider.ControlObject.GetComponentInChildren<Slider>().minValue = min;
						slider.ControlObject.GetComponentInChildren<Slider>().maxValue = max;


					}
				}

				if(lastExtent != null)
					Instance.StopCoroutine(lastExtent);
				lastExtent = Instance.StartCoroutine(CoEditExtents(start: 1));
			};
		}

#if HONEY_API
		static private CvsO_Type charaCustom = null;
		static public CvsB_ShapeBreast boobCustom = null;
		static public CvsB_ShapeWhole bodyCustom = null;
		static public CvsF_ShapeWhole faceCustom = null;
#else
		static private CvsChara charaCustom = null;
		static public CvsBreast boobCustom = null;
		static public CvsBodyShapeAll bodyCustom = null;
		static public CvsFaceShapeAll faceCustom = null;
#endif
		private static int abmxIndex = -1;
		private readonly static List<MorphMakerSlider> sliders = new List<MorphMakerSlider>();
		private readonly static List<MakerDropdown> modes = new List<MakerDropdown>();

		private static void Cleanup()
		{
			sliders.Clear();
			modes.Clear();
		}

		private static void AddCharaMorpherMenu(RegisterCustomControlsEvent e)
		{
			Cleanup();


			var inst = Instance;

			if(MakerAPI.GetMakerSex() == 0 && !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

			#region Enables

			e.AddControl(new MakerText("Enablers", category, CharaMorpher_Core.Instance));
			var enable = e.AddControl(new MakerToggle(category, "Enable", cfg.enable.Value, CharaMorpher_Core.Instance));

			var enableabmx = e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, CharaMorpher_Core.Instance));

			var linkoverallabmxsliders = e.AddControl(new MakerToggle(category, "Link Overall Base Sliders to ABMX Sliders", cfg.linkOverallABMXSliders.Value, CharaMorpher_Core.Instance));
			linkoverallabmxsliders.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.linkOverallABMXSliders.Value,
				(ctrl, val) =>
				{
					if(!ctrl || !ctrl.initLoadFinished) return;
					cfg.linkOverallABMXSliders.Value = val;
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)

					//	ctrl.StartCoroutine(ctrl.CoResetFace(delayFrames: (int)cfg.multiUpdateEnableTest.Value + 1));//this may be necessary (it is)
					//	ctrl.StartCoroutine(ctrl.CoResetHeight(delayFrames: (int)cfg.multiUpdateEnableTest.Value + 1));//this may be necessary (it is)
				});

			var saveWithMorph = (MakerToggle)e.AddControl(new MakerToggle(category, "Enable Save As Seen", cfg.saveWithMorph.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
					cfg.saveWithMorph.SettingChanged += (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.saveWithMorph.Value)
				);

			saveWithMorph.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.saveWithMorph.Value,
				(ctrl, val) => { if(val != cfg.saveWithMorph.Value) cfg.saveWithMorph.Value = val; });



			var enableQuadManip = (MakerToggle)e.AddControl(new MakerToggle(category, "Enable Calculation Types", cfg.enableCalcTypes.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
					cfg.enableCalcTypes.SettingChanged += (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enableCalcTypes.Value)
				);
			enableQuadManip.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.enableCalcTypes.Value,
				(ctrl, val) => { cfg.enableCalcTypes.Value = val; });

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
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

					string part = Regex.Replace(visualName, searchHits[0],
						Regex.IsMatch(visualName, searchHits[1], RegexOptions.IgnoreCase) ? "" : "Base", RegexOptions.IgnoreCase);

					part = Regex.Replace(part, "  ", " ", RegexOptions.IgnoreCase);
					e.AddControl(new MakerText($"{part} Controls".Trim(), category, CharaMorpher_Core.Instance));
				}

				//find section index
				if(Regex.IsMatch(settingName, searchHits[1], RegexOptions.IgnoreCase))
				{
					abmxIndex = abmxIndex >= 0 ? abmxIndex : index;

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"ABMX index: {abmxIndex}");
				}

				if(Regex.Match(visualName, "other", RegexOptions.IgnoreCase).Success)
					visualName = "Other";
				else
					//remove search hits from the slider name
					foreach(var hit in searchHits)
						if(hit != searchHits[0])
							visualName = Regex.Replace(visualName, hit, "", RegexOptions.IgnoreCase);


				//setup slider
				var currSlider = sliders.AddNReturn(e.AddControl(new MorphMakerSlider(category, visualName.Trim(), min, max, (float)cfg.defaults[index].Value * .01f, CharaMorpher_Core.Instance)));
				currSlider.BindToFunctionController<CharaMorpherController, float>(
						(ctrl) => ctrl.controls.all[settingName].Item1,
						(ctrl, val) =>
						{
							if(!ctrl) return;
							if(!ctrl.initLoadFinished || ctrl.reloading) return;
							if(ctrl.controls.all[settingName].Item1 == (float)Math.Round(val, 2)) return;

							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {(float)Math.Round(val, 2)}");
							ctrl.controls.all[settingName] = Tuple.Create(currSlider.StoreDefault = (float)Math.Round(val, 2), ctrl.controls.all[settingName].Item2);

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)


						});



				//mode dropdown 
				var ting = new string[] { "Linear", "Quadratic" };
				var currMode = modes.AddNReturn(e.AddControl(new MakerDropdown("", ting, category, 0, CharaMorpher_Core.Instance)));
				currMode.BindToFunctionController<CharaMorpherController, int>(
						(ctrl) => (int)ctrl.controls.all[settingName].Item2,
						(ctrl, val) =>
						{
							if(!ctrl) return;
							if(!ctrl.initLoadFinished || ctrl.reloading) return;
							if((int)ctrl.controls.all[settingName].Item2 == val) return;

							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {val}");
							ctrl.controls.all[settingName] = Tuple.Create(ctrl.controls.all[settingName].Item1, (MorphCalcType)val);

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)
						});





				//make sure values can be changed internally
				OnSliderValueChange.AddListener(() =>
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("controls updating");

					foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())//first one only
					{
						if(currSlider.Value != ctrl.controls.all[settingName].Item1)
							if(currSlider.ControlObject)
							{
								currSlider.Value = ctrl.controls.all[settingName].Item1;
								if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Slider control changed");
							}

						if(currMode.Value != (int)ctrl.controls.all[settingName].Item2)
							if(currMode.ControlObject)
							{
								currMode.Value = (int)ctrl.controls.all[settingName].Item2;
								if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Calc control changed");
							}

						break;
					}
				});

				//add separator after overall control
				if(Regex.IsMatch(settingName, searchHits[0], RegexOptions.IgnoreCase))
					e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));//create separator line

			}

			void OnSliderTextboxEdit(BaseGuiEntry slider, UnityAction<string> act)
			{
				var txtPro = slider.ControlObject?.GetComponentInChildren<TMPro.TMP_InputField>();
				var txt = slider.ControlObject?.GetComponentInChildren<InputField>();

				txtPro?.onEndEdit.AddListener(act);
				txt?.onEndEdit.AddListener(act);
			}
			void OnSliderResetClicked(BaseGuiEntry slider, UnityAction act)
			{
				var btn = slider.ControlObject?.GetComponentsInChildren<Button>().Last();

				btn?.onClick.AddListener(act);
			}

			void CreateShapeSlider(string settingName, int index)
			{
				CreatSlider(settingName, index, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f);

				sliders.Last().OnGUIExists((gui) =>
				{
					var slid = gui.ControlObject.
						GetComponentInChildren<Slider>();

					IEnumerator CoBoodyAfterRefresh()
					{
						if(!slid.interactable) yield break;
						for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
							inst.StartCoroutine(MakerAPI.GetCharacterControl().
							GetComponent<CharaMorpherController>().CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

					}

					gui.ControlObject.
					GetComponentInChildren<Slider>().OnPointerUpAsObservable().Subscribe(
						(p) => inst.StartCoroutine(CoBoodyAfterRefresh()));

					OnSliderTextboxEdit((MakerSlider)gui,
						(p) => inst.StartCoroutine(CoBoodyAfterRefresh()));

					OnSliderResetClicked((MakerSlider)gui,
						() => inst.StartCoroutine(CoBoodyAfterRefresh()));

				});

			}
			void CreateVoiceSlider(string settingName, int index)
			{
				CreatSlider(settingName, index);

				var mySlider = sliders.Last();
				IEnumerator CoVoiceAfterFullRefresh()
				{
					yield return new WaitWhile(
						() => MakerAPI.GetCharacterControl().GetComponent<BoneController>().NeedsFullRefresh);
					charaCustom.PlayVoice();
				}

				mySlider.OnGUIExists((gui) =>
				{
					gui.ControlObject.GetComponentInChildren<Slider>().
						OnPointerUpAsObservable().Subscribe((p) =>
						{
							inst.StartCoroutine(CoVoiceAfterFullRefresh());
						});

					OnSliderTextboxEdit((MakerSlider)gui,
						(p) =>
						{
							inst.StartCoroutine(CoVoiceAfterFullRefresh());
						});

					OnSliderResetClicked((MakerSlider)gui,
						() =>
						{
							inst.StartCoroutine(CoVoiceAfterFullRefresh());
						});

				});
			}

			foreach(var ctrl in CharaMorpher_Core.Instance.controlCategories)
				if(Regex.IsMatch(ctrl.Value, "voice", RegexOptions.IgnoreCase))
					CreateVoiceSlider(ctrl.Value, ctrl.Key);
				else
					CreateShapeSlider(ctrl.Value, ctrl.Key);


			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space

			//e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Init Slider Visibility

			IEnumerator CoSliderDisable(bool val, uint start = 0, uint end = int.MaxValue)
			{

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"CoSliderDisable Called!");
				yield return new WaitWhile(() =>
				{
					for(int a = (int)start; a < Math.Min(sliders.Count, (int)end); ++a)
						if(sliders?[a]?.ControlObject == null) return true;
					return false;
				});

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"sliders are visible: {val}");
				for(int a = (int)start; a < sliders.Count; ++a)
				{
					sliders?[a]?.ControlObject?.SetActive(val);
					modes?[a]?.ControlObject?.SetActive(val);
				}

				inst.StartCoroutine(CoModeDisable(cfg.enableCalcTypes.Value));
			}

			IEnumerator CoModeDisable(bool val, uint start = 0, uint end = int.MaxValue)
			{

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"CoModeDisable Called!");
				yield return new WaitWhile(() =>
				{
					for(int a = (int)start; a < Math.Min(modes.Count, (int)end); ++a)
						if(modes?[a]?.ControlObject == null) return true;
					return false;
				});

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Modes are visible: {val}");
				for(int a = (int)start; a < modes.Count; ++a)
					if((bool)sliders?[a]?.ControlObject?.activeSelf)
						modes?[a]?.ControlObject?.SetActive(val);

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

			cfg.enable.SettingChanged +=
			(s, o) =>
			{ enable?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enable.Value); };



			enableabmx.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.enableABMX.Value,
				(ctrl, val) =>
				{
					cfg.enableABMX.Value = val;
					ShowEnabledSliders();
				});

			enableQuadManip.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.enableCalcTypes.Value,
				(ctrl, val) =>
				{
					cfg.enableCalcTypes.Value = val;
					ShowEnabledSliders();


				});

			ShowEnabledSliders();
			#endregion

			#region Save/Load Buttons

			IEnumerator ChangeGUILayout(BaseGuiEntry gui)
			{
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("moving object");

				yield return new WaitWhile(() => gui?.ControlObject?.GetComponentInParent<ScrollRect>()?.transform == null);

				var par = gui.ControlObject.GetComponentInParent<ScrollRect>()?.transform;


				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Parent: " + par);

				//This fixes the KOI_API rendering issue & enables scrolling over viewport
#if KOI_API
				par.GetComponentInChildren<ScrollRect>().GetComponent<Image>().sprite = par.GetComponentInChildren<ScrollRect>().content.GetComponent<Image>()?.sprite;
				par.GetComponentInChildren<ScrollRect>().GetComponent<Image>().color = (Color)par.GetComponentInChildren<ScrollRect>().content.GetComponent<Image>()?.color;


				par.GetComponentInChildren<ScrollRect>().GetComponent<Image>().enabled = true;
				par.GetComponentInChildren<ScrollRect>().GetComponent<Image>().raycastTarget = true;
				var img = par.GetComponentInChildren<ScrollRect>().content.GetComponent<Image>();
				if(!img)
					img = par.GetComponentInChildren<ScrollRect>().viewport.GetComponent<Image>();
				img.enabled = false;
#endif

				//Setup LayoutElements 
				par.GetComponentInChildren<ScrollRect>().verticalScrollbar.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
				par.GetComponentInChildren<ScrollRect>().content.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
				//par.GetComponent<ScrollRect>().viewport.GetOrAddComponent<LayoutElement>().minHeight = par.GetComponent<RectTransform>().rect.height;
				var viewLE = par.GetComponentInChildren<ScrollRect>().viewport.GetOrAddComponent<LayoutElement>();
				viewLE.minWidth = par.GetComponentInChildren<ScrollRect>().GetComponent<RectTransform>().rect.width * .95f;
				viewLE.transform.Cast<RectTransform>();

				viewLE.flexibleHeight = 0;


				var elements = par.GetComponentsInChildren<LayoutElement>();
				foreach(var ele in elements)
				{
					ele.preferredHeight = ele.GetComponent<RectTransform>().rect.height;
					ele.preferredWidth = ele.GetComponent<RectTransform>().rect.width;
				}
				

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("setting as last");
				gui.ControlObject.transform.SetParent(par);
				gui.ControlObject.transform.SetAsLastSibling();
				var thisLE = gui.ControlObject.GetOrAddComponent<LayoutElement>();
				thisLE.flexibleWidth = 0;
				thisLE.preferredWidth = par.GetComponentInChildren<ScrollRect>().content.rect.width * .95f;
				thisLE.minWidth =
#if HONEY_API
					par.GetComponent<RectTransform>().rect.width;
#else
					viewLE.minWidth;
#endif


				thisLE.flexibleHeight = 0;
				thisLE.preferredHeight = gui.ControlObject.GetComponent<RectTransform>().rect.height;

				//setup VerticalLayoutGroup
				var vlg = par.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
#if HONEY_API
				vlg.childAlignment = TextAnchor.UpperCenter;
#else
				vlg.childAlignment = TextAnchor.LowerCenter;
#endif
				var pad = 10;//(int)cfg.unknownTest.Value;//10
				vlg.padding = new RectOffset(pad, pad + 5, 0, 0);
				vlg.childControlHeight = true;
				vlg.childControlWidth = true;
				vlg.childForceExpandHeight = false;
				vlg.childForceExpandWidth = false;

				//Reorder scrollbar
				par.GetComponentInChildren<ScrollRect>().verticalScrollbar.transform.SetAsLastSibling();
				yield break;
			}


			var sep = e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)));

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Adding buttons");
			var btn1 = (MakerButton)e.AddControl(new MakerButton("Save Default", category, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)));

			btn1.OnClick.AddListener(
			  () =>
			  {

				  foreach(var slider in sliders)
					  slider.ApplyDefault();

				  int count = 0;
				  foreach(var def in cfg.defaults)
					  def.Value = sliders[count++].Value * 100f;

				  count = 0;
				  foreach(var def in cfg.defaultModes)
					  def.Value = modes[count++].Value;

				  CharaMorpher_Core.Logger.LogMessage("Saved as CharaMorpher default");

				  Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
			  });

			var btn2 = (MakerButton)e.AddControl(new MakerButton("Load Default", category, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)));

			btn2.OnClick.AddListener(
			  () =>
			  {
				  foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				  {
					  for(int a = 0; a < ctrl.controls.all.Count; ++a)
					  {
						  var cal = (MorphCalcType)cfg.defaultModes[a].Value;
						  ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = Tuple.Create((float)cfg.defaults[a].Value * .01f, cal);
					  }

					  for(int b = -1; b < cfg.multiUpdateEnableTest.Value;)
						  ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: ++b));//this may be necessary 

					
					  break;
				  }
				  int count = 0;
				  foreach(var slider in sliders)
					  slider.Value = (float)cfg.defaults[count++].Value * .01f;


				  CharaMorpher_Core.Logger.LogMessage("Loaded CharaMorpher default");
				  Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
			  });


			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Finished adding buttons");
			//e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

		}

		private static void ImageControls(RegisterCustomControlsEvent e, BepInEx.BaseUnityPlugin owner)
		{
			e.AddControl(new MakerText("Morph Target", category, CharaMorpher_Core.Instance));


			var img = e.AddControl(new MakerImage(null, category, owner)
			{ Height = 200, Width = 150, Texture = MorphUtil.CreateTexture(TargetPath), });
			IEnumerator CoSetTexture(string path)
			{
				for(int a = 0; a < 4; ++a)
					yield return null;

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"The CoSetTexture was called");
				img.Texture = MorphUtil.CreateTexture(path);
			}

			CharaMorpher_Core.OnNewTargetImage.AddListener(
				path =>
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Calling OnNewTargetImage callback");
					CharaMorpher_Core.Instance.StartCoroutine(CoSetTexture(path));
				});

			var button = e.AddControl(new MakerButton($"Set New Morph Target", category, owner));
			button.OnClick.AddListener(() =>
			{
				ForeGrounder.SetCurrentForground();

				GetNewImageTarget();
			});

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
		}

		private static void ButtonDefaults(RegisterCustomControlsEvent e, BepInEx.BaseUnityPlugin owner)
		{

			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			e.AddControl(new MakerText("Easy Morph Buttons", category, CharaMorpher_Core.Instance));

			var tgl = e.AddControl(new MakerToggle(category, "Control overall sliders with Morph Buttons", cfg.easyMorphBtnOverallSet.Value, owner));
			tgl.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.easyMorphBtnOverallSet.Value,
				(ctrl, val) => cfg.easyMorphBtnOverallSet.Value = val
				);
			var tgl2 = e.AddControl(new MakerToggle(category, "Other values default to 100%", cfg.easyMorphBtnEnableDefaulting.Value, owner));
			tgl2.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.easyMorphBtnEnableDefaulting.Value,
				(ctrl, val) => cfg.easyMorphBtnEnableDefaulting.Value = val
				);
			void CreateMorphButton(int percent)
			{
				var button = e.AddControl(new MakerButton($"Morph {percent}%", category, owner));
				button.OnClick.AddListener(() =>
				{
					bool
					swap = cfg.easyMorphBtnOverallSet.Value,
					reset = cfg.easyMorphBtnEnableDefaulting.Value;

					foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
					{
						//	ctrl.StopAllCoroutines();
						if(reset)
							for(int a = 0; a < ctrl.controls.all.Count; ++a)
							{
								var cal = ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)].Item2;
								ctrl.controls.all[ctrl.controls.all.Keys.ElementAt(a)] = Tuple.Create(1f, cal);
							}

						var tmp = swap ? ctrl.controls.overall : ctrl.controls.notOverall;
						for(int a = 0; a < tmp.Count(); ++a)
						{
							var cal = ctrl.controls.all[tmp.ElementAt(a).Key].Item2;
							ctrl.controls.all[tmp.ElementAt(a).Key] = Tuple.Create(percent * .01f, cal);
						}

						for(int a = -1; a < cfg.multiUpdateEnableTest.Value;)
							ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(++a));

					
						CharaMorpher_Core.Logger.LogMessage($"Morphed to {percent}%");
						break;
					}
					Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
				});

			}

			// easy morph buttons
			CreateMorphButton(0);
			CreateMorphButton(25);
			CreateMorphButton(50);
			CreateMorphButton(75);
			CreateMorphButton(100);



			//Add Ending
			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			//	e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
		}

		private static string MakeDirPath(string path) => MorphUtil.MakeDirPath(path);

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

		private static readonly string _defaultOverlayDirectory = Path.Combine(BepInEx.Paths.GameRootPath, "UserData/chara/");
		public static string TargetDirectory { get => MakeDirPath(Path.GetDirectoryName(TargetPath)); }

		public static string TargetPath
		{
			get
			{
				var tmp = MakeDirPath(Path.GetFileName(cfg.imageName.Value));
				var path = Path.Combine(MakeDirPath(Path.GetDirectoryName(cfg.charDir.Value)), tmp);

				return File.Exists(path) ? path : Path.Combine(_defaultOverlayDirectory, tmp);
			}
		}
		public static void GetNewImageTarget()
		{

			var paths = OpenFileDialog.ShowDialog("Set Morph Target",
				TargetDirectory,
				FileFilter,
				FileExt,
				OpenFileDialog.SingleFileFlags);

			OnFileObtained(paths);
			Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
		}


		public class MorphMakerSlider : MakerSlider
		{
			float m_storedDefault;
			public float StoreDefault { get => m_storedDefault; set { m_storedDefault = value; } }

			public MorphMakerSlider(MakerCategory category, string settingName, float minValue, float maxValue, float defaultValue, BaseUnityPlugin owner)
				: base(category, settingName, minValue, maxValue, defaultValue, owner)
			{
				m_storedDefault = defaultValue;
			}

			public void ApplyDefault() => DefaultValue = StoreDefault;
		}

	}
}
