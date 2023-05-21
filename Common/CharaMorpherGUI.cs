using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


using BepInEx;
using BepInEx.Configuration;
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
using TMPro;
using UnityEngine.UI;
using UnityEngine.UI.Collections;
using UnityEngine.Events;

using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.MorphUtil;
using KKABMX.Core;
//using static Illusion.Utils;

namespace Character_Morpher
{
	class CharaMorpherGUI
	{
		private static MakerCategory category;

		private static Coroutine lastExtent;
		public static readonly string subCategoryName = "Morph";
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
				category = new MakerCategory(peram.CategoryName, subCategoryName, displayName: displayName);

				e.AddSubCategory(category);
			};
			MakerAPI.MakerBaseLoaded += (s, e) => { AddCharaMorpherMenu(e); };
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
				GameObject.Find($"CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuOption/Scroll View/Viewport/Content/Category(Clone)/CategoryTop/{subCategoryName}");

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
		private readonly static List<MorphMakerDropdown> modes = new List<MorphMakerDropdown>();
		private readonly static List<UnityAction> sliderValActions = new List<UnityAction>();
		static EventHandler lastUCMDEvent = null;
		static EventHandler saveAsMorphDataEvent = null;
		static EventHandler enableCalcTypesEvent = null;
		static EventHandler enableEvent = null;
		static EventHandler currentControlNameEvent = null;


		private static bool m_morphLoadToggle = true;
		public static bool MorphLoadToggle
		{
			get => !MakerAPI.InsideMaker || m_morphLoadToggle;
			private set => m_morphLoadToggle = value;
		}
		public static MorphMakerDropdown select = null;

		private static void Cleanup()
		{
			abmxIndex = -1;
			m_morphLoadToggle = true;
			sliders.Clear();
			modes.Clear();
			if(lastUCMDEvent != null)
				cfg.useCardMorphDataMaker.SettingChanged -= lastUCMDEvent;
			if(saveAsMorphDataEvent != null)
				cfg.saveAsMorphData.SettingChanged -= saveAsMorphDataEvent;
			if(enableCalcTypesEvent != null)
				cfg.enableCalcTypes.SettingChanged -= enableCalcTypesEvent;
			if(enableEvent != null)
				cfg.enable.SettingChanged -= enableEvent;
			if(currentControlNameEvent != null)
				cfg.currentControlName.SettingChanged -= currentControlNameEvent;

			foreach(var act in sliderValActions)
				OnInternalSliderValueChange.RemoveListener(act);
			sliderValActions.Clear();
		}

		private static void AddCharaMorpherMenu(RegisterCustomControlsEvent e)
		{
			Cleanup();//must be called (its now called elsewhere but this can stay)

			var inst = Instance;

			if(MakerAPI.GetMakerSex() == 0 && !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

			#region Load Toggles

			e.AddLoadToggle(new MakerLoadToggle("Chara Morph."))
				.OnGUIExists((gui) =>
				{
					var tgl = (MakerLoadToggle)gui;
					tgl.ValueChanged.Subscribe((b) => MorphLoadToggle = b);

					MorphLoadToggle = tgl.Value;
				});
			#endregion

			#region Enables

			e.AddControl(new MakerText("Enablers", category, CharaMorpher_Core.Instance));
			var enable = e.AddControl(new MakerToggle(category, "Enable", cfg.enable.Value, CharaMorpher_Core.Instance));

			var enableabmx = e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, CharaMorpher_Core.Instance));

			var linkoverallabmxsliders = e.AddControl(new MakerToggle(category, "Link Overall Base Sliders to ABMX Sliders", cfg.linkOverallABMXSliders.Value, CharaMorpher_Core.Instance));
			linkoverallabmxsliders.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.linkOverallABMXSliders.Value,
				(ctrl, val) =>
				{
					if(!ctrl || !ctrl.isInitLoadFinished) return;
					cfg.linkOverallABMXSliders.Value = val;
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)

					//	ctrl.StartCoroutine(ctrl.CoResetFace(delayFrames: (int)cfg.multiUpdateEnableTest.Value + 1));//this may be necessary (it is)
					//	ctrl.StartCoroutine(ctrl.CoResetHeight(delayFrames: (int)cfg.multiUpdateEnableTest.Value + 1));//this may be necessary (it is)
				});

			var saveAsMorph = (MakerToggle)e.AddControl(new MakerToggle(category, "Enable Save As Morph Data", cfg.saveAsMorphData.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
					cfg.saveAsMorphData.SettingChanged +=
					saveAsMorphDataEvent = (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.saveAsMorphData.Value)
				);


			saveAsMorph.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.saveAsMorphData.Value,
				(ctrl, val) => { if(val != cfg.saveAsMorphData.Value) cfg.saveAsMorphData.Value = val; });



			var enableQuadManip = (MakerToggle)e.AddControl(new MakerToggle(category, "Enable Calculation Types", cfg.enableCalcTypes.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
					cfg.enableCalcTypes.SettingChanged +=
					enableCalcTypesEvent = (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enableCalcTypes.Value)
				);



			e.AddControl(new MakerToggle(category, "Use Card Morph Data", cfg.useCardMorphDataMaker.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
				{
					var toggle = (MakerToggle)gui;
					toggle?.ValueChanged?.Subscribe((_1) =>
					{
						if(cfg.useCardMorphDataMaker.Value != _1)
							cfg.useCardMorphDataMaker.Value = _1;
					});

					Coroutine tmp = null;
					bool lastUCMD = cfg.useCardMorphDataMaker.Value;//this is needed
					cfg.useCardMorphDataMaker.SettingChanged +=
					lastUCMDEvent = (s, o) =>
					{
						var ctrl = GetFuncCtrlOfType<CharaMorpherController>()?.First();

						IEnumerator CoUCMD()
						{

							string name =
							(!cfg.useCardMorphDataMaker.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
							name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDivider), 0, name.Length));


							{
								CharaMorpher_Core.Logger.LogDebug($"lastUCMD: {lastUCMD}");
								yield return new WaitWhile(() => ctrl.isReloading);
								var tmpCtrls = ctrl.controls.Clone();

								ctrl.controls.Copy(!lastUCMD ? ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

								SoftSave(lastUCMD);
								ctrl.controls.Copy(!cfg.useCardMorphDataMaker.Value ?
								ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

								lastUCMD = cfg.useCardMorphDataMaker.Value;//this is needed
								CharaMorpher_Core.Logger.LogDebug($"Next lastUCMD: {lastUCMD}");
							}

							if(!name.IsNullOrEmpty())
								SwitchControlSet(ControlsList, name);
							select.Options = ControlsList;
							toggle?.SetValue(cfg.useCardMorphDataMaker.Value);

							yield break;
						}

						if(tmp != null)
							ctrl.StopCoroutine(tmp);
						tmp = ctrl.StartCoroutine(CoUCMD());
					};
				});

			e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Easy Morph Stuff
			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			ImageControls(e, inst);

			ButtonDefaults(e, inst);
			#endregion

			#region Sliders
			//creates a slider that controls the bodies' shape
			MorphMakerSlider CreatSlider(string settingName, int index,
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
				var currSlider = sliders.AddNReturn(e.AddControl(new MorphMakerSlider(category, visualName.Trim(), min, max, (float)cfg.defaults[defaultStr][index].Value * .01f, CharaMorpher_Core.Instance)));
				currSlider.BindToFunctionController<CharaMorpherController, float>(
						(ctrl) => ctrl.controls.all[ctrl.controls.currentSet][settingName].Item1,
						(ctrl, val) =>
						{
							CharaMorpher_Core.Logger.LogDebug($"called slider");
							if(!ctrl) return;
							if(!ctrl.isInitLoadFinished || ctrl.isReloading) return;
							if(ctrl.controls.all[ctrl.controls.currentSet][settingName].Item1 == (float)Math.Round(val, 2)) return;

							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"ctrl.controls.all[{ctrl.controls.currentSet}][{settingName}]");
							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {(float)Math.Round(val, 2)}");
							ctrl.controls.all[ctrl.controls.currentSet][settingName] = Tuple.Create(currSlider.StoreDefault =
								(float)Math.Round(val, 2), ctrl.controls.all[ctrl.controls.currentSet][settingName].Item2);

							CharaMorpher_Core.Logger.LogDebug($"edited slider");

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)
						});


				//mode dropdown 
				var ting = Enum.GetNames(typeof(MorphCalcType));
				var currMode = modes.AddNReturn(e.AddControl(new MorphMakerDropdown("", ting, category, cfg.defaultModes[defaultStr][index].Value, Instance)));
				currMode.BindToFunctionController<CharaMorpherController, int>(
						(ctrl) => (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].Item2,
						(ctrl, val) =>
						{
							if(!ctrl) return;
							if(!ctrl.isInitLoadFinished || ctrl.isReloading) return;
							if((int)ctrl.controls.all[ctrl.controls.currentSet][settingName].Item2 == val) return;

							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {val}");
							ctrl.controls.all[ctrl.controls.currentSet][settingName] = Tuple.Create(ctrl.controls.all[ctrl.controls.currentSet][settingName].Item1, (MorphCalcType)val);

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)
						});

				currSlider.ModSettingName = currMode.ModSettingName = settingName;

				//make sure values can be changed internally
				OnInternalSliderValueChange.AddListener(sliderValActions.AddNReturn(() =>
				{
					//if(cfg.debug.Value)
					CharaMorpher_Core.Logger.LogDebug("controls updating");

					CharaMorpherController ctrl = null;
					try
					{
						ctrl = GetFuncCtrlOfType<CharaMorpherController>()?.First(k => k != null);//first one only
					}
					catch { }

					if(ctrl == null) return;
					bool change = false;
					currSlider.OnGUIExists((gui) =>
					{
						MorphMakerSlider slider = (MorphMakerSlider)gui;
						if(slider.Value != ctrl.controls.all[ctrl.controls.currentSet][settingName].Item1)
						//if(currSlider.ControlObject)
						{
							slider.Value = slider.StoreDefault = ctrl.controls.all[ctrl.controls.currentSet][settingName].Item1;
							//	if(cfg.debug.Value) 
							CharaMorpher_Core.Logger.LogDebug($"Slider control changed: {slider.Value}");
							change = true;
						}
					});

					currMode.OnGUIExists((gui) =>
					{
						MorphMakerDropdown dropdown = (MorphMakerDropdown)gui;
						if(dropdown.Value != (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].Item2)
						//	if(currMode.ControlObject)
						{
							dropdown.Value = dropdown.StoreDefault = (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].Item2;
							//	if(cfg.debug.Value) 
							CharaMorpher_Core.Logger.LogDebug($"Calc control changed: {dropdown.Value}");
							change = true;
						}
					});

					//	if(change)
					//		for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
					//			ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)
					//
				}));

				//add separator after overall control
				if(Regex.IsMatch(settingName, searchHits[0], RegexOptions.IgnoreCase))
					e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));//create separator line


				return currSlider;
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
				CreatSlider(settingName, index, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f).
					OnGUIExists((gui) =>
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

				var mySlider = CreatSlider(settingName, index);

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

			foreach(var ctrl in CharaMorpher_Core.Instance.controlCategories[defaultStr])
				if(Regex.IsMatch(ctrl.Value, "voice", RegexOptions.IgnoreCase))
					CreateVoiceSlider(ctrl.Value, ctrl.Key);
				else
					CreateShapeSlider(ctrl.Value, ctrl.Key);


			e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space

			//e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Slider Visibility

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
					{
						if(!val) { modes[a].StoreDefault = modes[a].Value; modes[a].Value = 0; }
						else modes[a].ApplyStoredSetting();

						modes[a].ControlObject?.SetActive(val);
					}
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

			cfg.enable.SettingChanged += enableEvent =
			(s, o) => enable?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enable.Value);


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
				//viewLE.transform.Cast<RectTransform>();

				viewLE.flexibleHeight = 0;


				var elements = par.GetComponentsInChildren<LayoutElement>();
				foreach(var ele in elements)
				{
					ele.preferredHeight = ele.GetComponent<RectTransform>().rect.height;
					ele.preferredWidth = ele.GetComponent<RectTransform>().rect.width;
				}
				//test
				elements = par.GetComponentInChildren<ScrollRect>().content.GetComponentsInChildren<LayoutElement>();
				foreach(var ele in elements)
				{
					ele.preferredHeight = ele.GetComponent<RectTransform>().rect.height;
					ele.preferredWidth = par.GetComponentInChildren<ScrollRect>().GetComponent<RectTransform>().rect.width * .95f;
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

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Adding buttons");

			var sep = e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)));


			select = ((MorphMakerDropdown)e.AddControl(
			   new MorphMakerDropdown("Selected", ControlsList, category, 0, Instance))
			   .OnGUIExists(
			   (gui) =>
			   {
				   MorphMakerDropdown mmd = (MorphMakerDropdown)gui;

				   //  cfg.currentControlName = null;

				   cfg.currentControlName.SettingChanged +=
				   currentControlNameEvent = (s, o) =>
				   {
					   //UpdateDrpodown(select.Options);
					   select.Options = ControlsList.ToArray();
					   var name = cfg.currentControlName.Value;
					   select.Value = SwitchControlSet(mmd.Options, name);

					   // OnInternalSliderValueChange.Invoke();

					   var ctrl = GetFuncCtrlOfType<CharaMorpherController>()?.First();
					   var val = (!ctrl.isUsingExtMorphData ? ctrl.ctrls1 : ctrl.ctrls2 ?? ctrl.ctrls1).all;
					   foreach(var slider in sliders)
					   {
						   slider.StoreDefault = val[name][slider.ModSettingName].Item1;
						   slider.ApplyDefault();
					   }
					   
				   };

				   Instance.StartCoroutine(ChangeGUILayout(gui));
				   mmd.ValueChanged?.Subscribe((val) => { mmd.Value = SwitchControlSet(mmd.Options, val); });//This loops back to TmpThing()
				   mmd.Value = SwitchControlSet(mmd.Options, cfg.currentControlName.Value);
			   }));


			((MakerButton)e.AddControl(new MakerButton("Add New Slot", category, Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)))).
				OnClick.AddListener(() =>
				{
					AddNewSetting();

					//update dropdown list					
					//UpdateDrpodown(select.Options);
					select.Options = ControlsList.ToArray();
				});


			((MakerButton)e.AddControl(new MakerButton("Remove Current Slot", category, Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)))).
				OnClick.AddListener(() =>
				{
					//	var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();

					//switch control before deletion
					int tmp = select.Value;
					if(select.Value >= ControlsList.Length - 1)
						tmp = ControlsList.Length - 2;

					RemoveCurrentSetting(null);

					select.Options = ControlsList.ToArray();

					select.Value = SwitchControlSet(select.Options, tmp);
					//UpdateDrpodown(select.Options);
				});


			((MakerButton)e.AddControl(new MakerButton("Save Default", category, Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)))).
				OnClick.AddListener(() =>
				{
					foreach(var slider in sliders)
						slider.ApplyDefault();
					foreach(var mode in modes)
						mode.ApplyStoredSetting();

					var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();
					int count = 0;
					//cfg.defaults[ctrl.controls.currentSet] = new List<ConfigEntry<float>>();
					if(!cfg.useCardMorphDataMaker.Value || ctrl.ctrls2 == null)
						foreach(var def in cfg.defaults[ctrl.controls.currentSet])
						{
							////this is a redundancy that can protect against out of order settings
							//var index = sliders.FindIndex(k => k.ModSettingName ==
							//Instance.controlCategories[ctrl.controls.currentSet][count].Value);

							def.Value = sliders[count].Value * 100f;
							count++;
						}
					//cfg.defaultModes[ctrl.controls.currentSet] = new List<ConfigEntry<int>>();
					count = 0;
					if(!cfg.useCardMorphDataMaker.Value || ctrl.ctrls2 == null)
						foreach(var def in cfg.defaultModes[ctrl.controls.currentSet])
							def.Value = modes[count++].Value;

					SoftSave(cfg.useCardMorphDataMaker.Value);
					CharaMorpher_Core.Logger.LogMessage($"Saved as CharaMorpher {ctrl.controls.currentSet}");

					Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
				});

			((MakerButton)e.AddControl(new MakerButton("Load Default", category, Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)))).
				OnClick.AddListener(() =>
				{
					var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();

					var data = (!ctrl.isUsingExtMorphData ? ctrl.ctrls1 : (ctrl.ctrls2 ?? ctrl.ctrls1))?.Clone()?.all;
					var listCtrls = ctrl?.controls;
					var list = listCtrls?.all;
					var name = cfg.currentControlName.Value;

					if(list?.ContainsKey(name) ?? false)
						foreach(var def2 in list[name].Keys.ToList())
						{
							//CharaMorpher_Core.Logger.LogDebug($"Data Expected: data[{name}][{def2}]");
							//CharaMorpher_Core.Logger.LogDebug($"Data Key1:\n data[{string.Join(",\n ", data?.Keys.ToArray())}]");
							//CharaMorpher_Core.Logger.LogDebug($"Data Key2:\n data[{string.Join(",\n ", data?[data.Keys.ElementAt(0)].Keys.ToArray())}]");

							var val = data[name][def2].Item1;
							var cal = data[name][def2].Item2;

							list[name][def2] = Tuple.Create(val, cal);
						
						}

					OnInternalSliderValueChange.Invoke();
				
					for(int b = -1; b < cfg.multiUpdateEnableTest.Value;)
						ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: ++b));//this may be necessary 

					CharaMorpher_Core.Logger.LogMessage($"Loaded CharaMorpher: {name}");
					Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
				});


			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Finished adding buttons");
			//e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

		}

		private static void ImageControls(RegisterCustomControlsEvent e, BaseUnityPlugin owner)
		{
			e.AddControl(new MakerText("Morph Target", category, CharaMorpher_Core.Instance));


			var img = e.AddControl(new MakerImage(null, category, owner)
			{ Height = 200, Width = 150, Texture = MorphUtil.CreateTexture(TargetPath), });
			IEnumerator CoSetTexture(string path, byte[] png = null)
			{
				for(int a = 0; a < 4; ++a)
					yield return null;
				yield return new WaitUntil(() => img.Exists);

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"The CoSetTexture was called");
				img.Texture = path?.CreateTexture(png);
				//(img.Texture as Texture2D).Resize(150, 200);
				img.ControlObject.GetComponentInChildren<RawImage>().color = Color.white * ((!png.IsNullOrEmpty()) ? .65f : 1);
			}

			CharaMorpher_Core.OnNewTargetImage.AddListener(
				(path, png) =>
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Calling OnNewTargetImage callback");
					CharaMorpher_Core.Instance.StartCoroutine(CoSetTexture(path, png));
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

					foreach(CharaMorpherController ctrl in GetFuncCtrlOfType<CharaMorpherController>())
					{
						//	ctrl.StopAllCoroutines();
						if(reset)
							for(int a = 0; a < ctrl.controls.all[ctrl.controls.currentSet].Count; ++a)
							{
								var cal = ctrl.controls.all[ctrl.controls.currentSet][ctrl.controls.all.Keys.ElementAt(a)].Item2;
								ctrl.controls.all[ctrl.controls.currentSet][ctrl.controls.all.Keys.ElementAt(a)] = Tuple.Create(1f, cal);
							}

						var tmp = swap ? ctrl.controls.overall : ctrl.controls.notOverall;
						for(int a = 0; a < tmp.Count(); ++a)
						{
							var cal = ctrl.controls.all[ctrl.controls.currentSet][tmp.ElementAt(a).Key].Item2;
							ctrl.controls.all[ctrl.controls.currentSet][tmp.ElementAt(a).Key] = Tuple.Create(percent * .01f, cal);
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

			cfg.charDir.Value = Path.GetDirectoryName(texPath).MakeDirPath();
			cfg.imageName.Value = texPath.Substring(texPath.LastIndexOf('/') + 1).MakeDirPath();//not sure why this happens on hs2?

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Exit accept");
		}

		public const string FileExt = ".png";
		public const string FileFilter = "Character Images (*.png)|*.png";

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
			public float StoreDefault { get => m_storedDefault; internal set { m_storedDefault = value; } }
			public string ModSettingName { internal set; get; } = null;

			public MorphMakerSlider(MakerCategory category, string settingName, float minValue, float maxValue, float defaultValue, BaseUnityPlugin owner)
				: base(category, settingName, minValue, maxValue, defaultValue, owner)
			{
				m_storedDefault = defaultValue;
			}
			~MorphMakerSlider()
			{

			}
			public void ApplyDefault() => DefaultValue = StoreDefault;
		}

		public class MorphMakerDropdown : MakerDropdown
		{
			int m_storedDefault;
			public int StoreDefault { get => m_storedDefault; internal set { m_storedDefault = value; } }
			public string ModSettingName { internal set; get; } = null;


			public new string[] Options
			{
				get =>
					ControlObject?.GetComponentInChildren<TMP_Dropdown>()?.options?.Select((k) => k.text)?.ToArray() ??
					ControlObject?.GetComponentInChildren<Dropdown>()?.options?.Select((k) => k.text)?.ToArray() ??
					base.Options;

				set
				{
					Component dropdown = ControlObject?.GetComponentInChildren<TMP_Dropdown>();
					if(!dropdown) dropdown = ControlObject?.GetComponentInChildren<Dropdown>();
					if(!dropdown) return;

					if(dropdown is TMP_Dropdown)
						((TMP_Dropdown)dropdown).options = value.Attempt((k) =>
						new TMP_Dropdown.OptionData(k.LastIndexOf(strDivider) >= 0 ? k.Substring(0, k.LastIndexOf(strDivider)) : k)).ToList();

					else if(dropdown is Dropdown)
						((Dropdown)dropdown).options = value.Attempt((k) =>
						new Dropdown.OptionData(k.LastIndexOf(strDivider) >= 0 ? k.Substring(0, k.LastIndexOf(strDivider)) : k)).ToList();
				}
			}

			public new int Value
			{
				get => ControlObject?.GetComponentInChildren<TMP_Dropdown>()?.value ??
					ControlObject?.GetComponentInChildren<Dropdown>()?.value ?? base.Value;
				set => base.Value = value;
			}

			public MorphMakerDropdown(string settingName, string[] options, MakerCategory category, int initialValue, BaseUnityPlugin owner)
				: base(settingName, options, category, initialValue, owner)
			{
				StoreDefault = initialValue;
			}
			public void ApplyStoredSetting() => Value = StoreDefault;
			public Component GetDropdown()
			{
				Component dropdown = ControlObject?.GetComponentInChildren<TMP_Dropdown>();
				if(!dropdown) dropdown = ControlObject?.GetComponentInChildren<Dropdown>();

				return dropdown;
			}
		}

	}
}
