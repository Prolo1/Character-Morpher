using System;
using System.Resources;
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


namespace Character_Morpher
{
	class CharaMorpherGUI
	{

		#region Data
		private static MakerCategory category;

		private static Coroutine lastExtent;
		public static readonly string subCategoryName = "Morph";
		public static readonly string displayName = "Chara Morph";


#if HONEY_API
		public static CvsO_Type charaCustom { get; private set; } = null;
		public static CvsB_ShapeBreast boobCustom { get; private set; } = null;
		public static CvsB_ShapeWhole bodyCustom { get; private set; } = null;
		public static CvsF_ShapeWhole faceCustom { get; private set; } = null;
#else
		public static CvsChara charaCustom { get; private set; } = null;
		public static CvsBreast boobCustom { get; private set; } = null;
		public static CvsBodyShapeAll bodyCustom { get; private set; } = null;
		public static CvsFaceShapeAll faceCustom { get; private set; } = null;
#endif

		private static int abmxIndex = -1;
		private readonly static List<MorphMakerSlider> sliders = new List<MorphMakerSlider>();
		private readonly static List<MorphMakerDropdown> modes = new List<MorphMakerDropdown>();
		private readonly static List<UnityAction> sliderValActions = new List<UnityAction>();
		static EventHandler lastUCMDEvent = null;
		static EventHandler loadInitMorphCharacterEvent = null;
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
		internal static MorphMakerDropdown select = null;
		#endregion

		internal static void Initialize()
		{
			MakerAPI.RegisterCustomSubCategories += (s, e) =>
			{
				//Create custom category 

#if HONEY_API
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
				var allCvs =

#if HONEY_API
				((CvsSelectWindow[])Resources.FindObjectsOfTypeAll(typeof(CvsSelectWindow)))
				.OrderBy((k) => k.transform.GetSiblingIndex())//I just want them in the right order
				.Attempt(p => p.items)
				.Aggregate((l, r) => l.Concat(r).ToArray());//should flaten array


				bodyCustom = (CvsB_ShapeWhole)allCvs.FirstOrNull((p) => p.cvsBase is CvsB_ShapeWhole)?.cvsBase;
				faceCustom = (CvsF_ShapeWhole)allCvs.FirstOrNull((p) => p.cvsBase is CvsF_ShapeWhole)?.cvsBase;
				boobCustom = (CvsB_ShapeBreast)allCvs.FirstOrNull((p) => p.cvsBase is CvsB_ShapeBreast)?.cvsBase;
				charaCustom = (CvsO_Type)allCvs.FirstOrNull((p) => p.cvsBase is CvsO_Type)?.cvsBase;


#else
				0;//don't remove this!
				bodyCustom = (CvsBodyShapeAll)Resources.FindObjectsOfTypeAll(typeof(CvsBodyShapeAll))[0];
				faceCustom = (CvsFaceShapeAll)Resources.FindObjectsOfTypeAll(typeof(CvsFaceShapeAll))[0];
				boobCustom = (CvsBreast)Resources.FindObjectsOfTypeAll(typeof(CvsBreast))[0];
				charaCustom = (CvsChara)Resources.FindObjectsOfTypeAll(typeof(CvsChara))[0];
#endif

#if HONEY_API
				//Force the floating settings window to show up

				var btn = allCvs?.FirstOrNull(p => p?.btnItem?.gameObject?.GetTextFromTextComponent() == displayName).btnItem;
				btn?.onClick?.AddListener(() => MakerAPI.GetMakerBase().drawMenu.ChangeMenuFunc());
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
		
		private static void Cleanup()
		{
			abmxIndex = -1;
			m_morphLoadToggle = true;
			select = null;
			sliders.Clear();
			modes.Clear();

			if(lastUCMDEvent != null)
				cfg.preferCardMorphDataMaker.SettingChanged -= lastUCMDEvent;
			if(saveAsMorphDataEvent != null)
				cfg.saveExtData.SettingChanged -= saveAsMorphDataEvent;
			if(enableCalcTypesEvent != null)
				cfg.enableCalcTypes.SettingChanged -= enableCalcTypesEvent;
			if(enableEvent != null)
				cfg.enable.SettingChanged -= enableEvent;
			if(currentControlNameEvent != null)
				cfg.currentControlName.SettingChanged -= currentControlNameEvent;
			if(loadInitMorphCharacterEvent != null)
				cfg.loadInitMorphCharacter.SettingChanged -= loadInitMorphCharacterEvent;

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

			var saveExtData = (MakerToggle)e.AddControl(new MakerToggle(category, "Save Ext. Data", cfg.saveExtData.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
					cfg.saveExtData.SettingChanged +=
					saveAsMorphDataEvent = (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.saveExtData.Value)
				);

			saveExtData.BindToFunctionController<CharaMorpherController, bool>(
				(ctrl) => cfg.saveExtData.Value,
				(ctrl, val) => { if(val != cfg.saveExtData.Value) cfg.saveExtData.Value = val; });


			var linkoverallabmxsliders = e.AddControl(new MakerToggle(category, "Link Overall Sliders to ABMX Overall Sliders", cfg.linkOverallABMXSliders.Value, CharaMorpher_Core.Instance));
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



			var enableQuadManip = (MakerToggle)e.AddControl(new MakerToggle(category, "Enable Calculation Types", cfg.enableCalcTypes.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
					cfg.enableCalcTypes.SettingChanged +=
					enableCalcTypesEvent = (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enableCalcTypes.Value)
				);



			e.AddControl(new MakerToggle(category, "Prefer Card Morph Data", cfg.preferCardMorphDataMaker.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
				{
					var toggle = (MakerToggle)gui;
					toggle?.ValueChanged?.Subscribe((_1) =>
					{
						if(cfg.preferCardMorphDataMaker.Value != _1)
							cfg.preferCardMorphDataMaker.Value = _1;
					});

					Coroutine tmp = null;
					bool lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed
					cfg.preferCardMorphDataMaker.SettingChanged +=
					lastUCMDEvent = (s, o) =>
					{
						var ctrl = GetFuncCtrlOfType<CharaMorpherController>()?.First();

						IEnumerator CoUCMD()
						{

							string name =
							(!cfg.preferCardMorphDataMaker.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
							name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDivider), 0, name.Length));


							{
							//	CharaMorpher_Core.Logger.LogDebug($"lastUCMD: {lastUCMD}");
								yield return new WaitWhile(() => ctrl.isReloading);

								var tmpCtrls =
								!cfg.preferCardMorphDataMaker.Value ?
								ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1);
								tmpCtrls.currentSet = ctrl.controls.currentSet;

								ctrl.controls.Copy(!lastUCMD ? ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

								SoftSave(lastUCMD);
								ctrl.controls.Copy(!cfg.preferCardMorphDataMaker.Value ?
								ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

								lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed
						//		CharaMorpher_Core.Logger.LogDebug($"Next lastUCMD: {lastUCMD}");
							}

							if(!name.IsNullOrEmpty())
								SwitchControlSet(ControlsList, name);
							select.Options = ControlsList;
							toggle?.SetValue(cfg.preferCardMorphDataMaker.Value);

							yield break;
						}

						if(tmp != null)
							ctrl.StopCoroutine(tmp);
						tmp = ctrl.StartCoroutine(CoUCMD());
					};
				});

			e.AddControl(new MakerToggle(category, "Load Init. Character", cfg.loadInitMorphCharacter.Value, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
				{
					var tgl = (MakerToggle)gui;

					tgl.BindToFunctionController<CharaMorpherController, bool>(
						(ctrl) => cfg.loadInitMorphCharacter.Value,
						(ctrl, val) => cfg.loadInitMorphCharacter.Value = val);

					cfg.loadInitMorphCharacter.SettingChanged +=
					loadInitMorphCharacterEvent += (m, n) =>
					{
						tgl.Value = cfg.loadInitMorphCharacter.Value;
					};
				});

			e.AddControl(new MakerButton("Reset To Original Shape", category, CharaMorpher_Core.Instance))
				.OnGUIExists((gui) =>
				{
					var btn = (MakerButton)gui;

					btn.OnClick.AddListener(() =>
					{
						var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();
						ctrl.ResetOriginalShape();
					});
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
			MorphMakerSlider CreatSlider(string settingName,
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
					abmxIndex = abmxIndex >= 0 ? abmxIndex : sliders.Count + 1;

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
				var currSlider = sliders.AddNReturn(e.AddControl(new MorphMakerSlider(category, visualName.Trim(), min, max, (float)cfg.defaults[cfg.currentControlName.Value][settingName].Value.data, CharaMorpher_Core.Instance)));
				currSlider.BindToFunctionController<CharaMorpherController, float>(
						(ctrl) => ctrl.controls.all[ctrl.controls.currentSet][settingName].data,
						(ctrl, val) =>
						{
							//	CharaMorpher_Core.Logger.LogDebug($"called slider");
							if(!ctrl) return;
							if(!ctrl.isInitLoadFinished || ctrl.isReloading) return;
							if(ctrl.controls.all[ctrl.controls.currentSet][settingName].data == (float)Math.Round(val, 2)) return;

							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"ctrl.controls.all[{ctrl.controls.currentSet}][{settingName}]");
							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {(float)Math.Round(val, 2)}");
							ctrl.controls.all[ctrl.controls.currentSet][settingName].
							SetData(currSlider.StoreDefault = (float)Math.Round(val, 2));

							//	CharaMorpher_Core.Logger.LogDebug($"edited slider");

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)
						});


				//mode dropdown 
				var ting = Enum.GetNames(typeof(MorphCalcType));
				var currMode = modes.AddNReturn(e.AddControl(new MorphMakerDropdown("", ting, category, (int)cfg.defaults[cfg.currentControlName.Value][settingName].Value.calcType, Instance)));
				currMode.BindToFunctionController<CharaMorpherController, int>(
						(ctrl) => (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType,
						(ctrl, val) =>
						{
							if(!ctrl) return;
							if(!ctrl.isInitLoadFinished || ctrl.isReloading) return;
							if((int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType == val) return;

							if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"{settingName} Value: {val}");
							ctrl.controls.all[ctrl.controls.currentSet][settingName].SetCalcType((MorphCalcType)val);

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)

							Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
						});

				currSlider.ModSettingName = currMode.ModSettingName = settingName;

				//make sure values can be changed internally
				OnInternalSliderValueChange.AddListener(sliderValActions.AddNReturn(() =>
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("controls updating");

					CharaMorpherController ctrl = GetFuncCtrlOfType<CharaMorpherController>()?.FirstOrNull(k => k != null);//first one only

					if(ctrl == null) return;

					currSlider.OnGUIExists((gui) =>
					{
						MorphMakerSlider slider = (MorphMakerSlider)gui;
						if(slider.Value != ctrl.controls.all[ctrl.controls.currentSet][settingName].data)
						//if(currSlider.ControlObject)
						{
							slider.Value = slider.StoreDefault = ctrl.controls.all[ctrl.controls.currentSet][settingName].data;
							//	if(cfg.debug.Value) 
							CharaMorpher_Core.Logger.LogDebug($"Slider control changed: {slider.Value}");
						}
					});

					currMode.OnGUIExists((gui) =>
					{
						MorphMakerDropdown dropdown = (MorphMakerDropdown)gui;
						if(dropdown.Value != (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType)
						//	if(currMode.ControlObject)
						{
							dropdown.Value = dropdown.StoreDefault = (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType;
							//	if(cfg.debug.Value) 
							CharaMorpher_Core.Logger.LogDebug($"Calc control changed: {dropdown.Value}");
						}
					});

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

			void CreateShapeSlider(string settingName)
			{
				CreatSlider(settingName, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f).
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
			void CreateVoiceSlider(string settingName)
			{

				var mySlider = CreatSlider(settingName);

				IEnumerator CoVoiceAfterFullRefresh()
				{
					yield return new WaitWhile(
						() => MakerAPI.GetCharacterControl().GetComponent<BoneController>().NeedsFullRefresh);
					//	MakerAPI.GetMakerBase().playSampleVoice = true;
					//					MakerAPI.GetMakerBase().
					//#if HONEY_API
					//					playVoiceBackup.
					//#endif
					//					playSampleVoice = true;
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

			foreach(var ctrl in inst.controlCategories[defaultStr])
				if(Regex.IsMatch(ctrl.dataName, "voice", RegexOptions.IgnoreCase))
					CreateVoiceSlider(ctrl.dataName);
				else
					CreateShapeSlider(ctrl.dataName);


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
			   new MorphMakerDropdown("Selected Slot", ControlsList, category, 0, Instance))
			   .OnGUIExists(
			   (gui) =>
			   {
				   MorphMakerDropdown mmd = (MorphMakerDropdown)gui;
				   // var drop = mmd.ControlObject?.GetComponentInChildren<TMP_Dropdown>().ite ??
				   //	   mmd.ControlObject?.GetComponentInChildren<Dropdown>().OnUpdateSelectedAsObservable();


				   //  cfg.currentControlName = null;

				   cfg.currentControlName.SettingChanged +=
				   currentControlNameEvent = (s, o) =>
				   {
					   //UpdateDrpodown(select.Options);
					   select.Options = ControlsList.ToArray();
					   var name = cfg.currentControlName.Value;
					   select.Value = SwitchControlSet(mmd.Options, name);

					   // OnInternalSliderValueChange.Invoke();

					   //   var ctrl = GetFuncCtrlOfType<CharaMorpherController>()?.First();
					   //   var val = (!ctrl.isUsingExtMorphData ? ctrl.ctrls1 : ctrl.ctrls2 ?? ctrl.ctrls1).all;
					   //   foreach(var slider in sliders)
					   //   {
					   //	   slider.StoreDefault = val[name][slider.ModSettingName].Item1;
					   //	   slider.ApplyDefault();
					   //   }

				   };

				   Instance.StartCoroutine(ChangeGUILayout(gui));
				   mmd.ValueChanged?.Subscribe((val) => { mmd.Value = SwitchControlSet(mmd.Options, val); Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel); });//This loops back to TmpThing()
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
				.OnGUIExists((gui) => { Instance.StartCoroutine(ChangeGUILayout(gui)); })).
				OnClick.AddListener(() =>
				{
					foreach(var slider in sliders)
						slider.ApplyDefault();
					foreach(var mode in modes)
						mode.ApplyStoredSetting();

					var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();
					//int count = 0;
					//cfg.defaults[ctrl.controls.currentSet] = new List<ConfigEntry<float>>();
					if(!cfg.preferCardMorphDataMaker.Value || ctrl.ctrls2 == null)
						foreach(var def in inst.controlCategories[ctrl.controls.currentSet])
						{

							cfg.defaults[ctrl.controls.currentSet][def.dataName].Value.data =
							ctrl.controls.all[ctrl.controls.currentSet][def.dataName].data;//this should work
																						   //count++;
						}


					if(!cfg.preferCardMorphDataMaker.Value || ctrl.ctrls2 == null)
						foreach(var def in inst.controlCategories[ctrl.controls.currentSet])
							cfg.defaults[ctrl.controls.currentSet][def.dataName].Value.calcType =
							ctrl.controls.all[ctrl.controls.currentSet][def.dataName].calcType;

					SoftSave(cfg.preferCardMorphDataMaker.Value);
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

							var val = data[name][def2].data;
							var cal = data[name][def2].calcType;

							list[name][def2] = data[name][def2].Clone();


						}

					OnInternalSliderValueChange.Invoke();

					foreach(var slider in sliders)
						slider.ApplyDefault();
					foreach(var mode in modes)
						mode.ApplyStoredSetting();

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

		private static void ButtonDefaults(RegisterCustomControlsEvent e, BaseUnityPlugin owner)
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

						//	CharaMorpher_Core.Logger.LogDebug($"Mod Category:{ctrl.controls.currentSet}");
						if(reset)
							for(int a = 0; a < ctrl.controls.all[ctrl.controls.currentSet].Count; ++a)
							{
								//CharaMorpher_Core.Logger.LogDebug($"Mod name:{ctrl.controls.all[ctrl.controls.currentSet].Keys.ElementAt(a)}");
								//	var cal = ctrl.controls.all[ctrl.controls.currentSet][ctrl.controls.all.Keys.ElementAt(a)].calcType;
								ctrl.controls.all[ctrl.controls.currentSet][ctrl.controls.all[ctrl.controls.currentSet].Keys.ElementAt(a)].SetData(1f);
							}

						var tmp = swap ? ctrl.controls.overall : ctrl.controls.notOverall;
						for(int a = 0; a < tmp.Count(); ++a)
						{
							//var cal = ctrl.controls.all[ctrl.controls.currentSet][tmp.ElementAt(a).Key].calcType;
							ctrl.controls.all[ctrl.controls.currentSet][tmp.ElementAt(a).Key].SetData(percent*.01f);
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
