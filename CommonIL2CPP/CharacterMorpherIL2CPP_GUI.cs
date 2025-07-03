using System;
using System.Resources;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using BepInEx;
using BepInEx.Configuration;

using UniRx;
using UniRx.Triggers;
using ProloAPI;//leave it here
using ProloAPI.Extensions;//leave it here
using ProloAPI.Utilities;//leave it here



using static ProloAPI.Utilities.PGUI;
using static ProloAPI.Utilities.PGeneral;
using System.Reflection;
using ILLGames.Extensions;
using BepInEx.Unity.IL2CPP.Utils;
//using ProloAPI.Extensions;//leave it here 
//using Studio;
//using XUnity.AutoTranslator.Plugin.Core;
//	using static XUnity.AutoTranslator.Plugin.Core.TranslationHelper;
using static XUnity.AutoTranslator.Plugin.Core.AutoTranslator;


namespace Character_Morpher_IL2CPP
{
	using static Character_Morpher_IL2CPP.CharaMorpherIL2CPP_Core;
	using static Character_Morpher_IL2CPP.CharaMorpherIL2CPP_Manager;

	class CharacterMorpherIL2CPP_GUI : ProloGUIBehaviour<CharacterMorpherIL2CPP_GUI>
	{
		#region Classes
		class RefEqualsCompare<T> : IEqualityComparer<T>
		{

			public bool Equals(T x, T y)
			{
				return object.ReferenceEquals(x, y);
			}

			public int GetHashCode(T obj)
			{
				return obj.GetHashCode();
			}

		}
		class UnityObjRefEqualsCompare<T> : IEqualityComparer<T> where T : UnityEngine.Object
		{

			public bool Equals(T x, T y)
			{
				return x.GetInstanceID() == y.GetInstanceID();
			}

			public int GetHashCode(T obj)
			{
				return obj.GetHashCode();
			}

		}
		class IEnumerableElementCompare<T> : IEqualityComparer<IEnumerable<T>>
		{

			public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
			{
				if(x.Count() != y.Count()) return false;
				for(int a = 0; a < x.Count(); ++a)
					if(x.ElementAt(a).GetHashCode() != y.ElementAt(a).GetHashCode()) return false;

				return true;
			}

			public int GetHashCode(IEnumerable<T> obj)
			{
				return obj.GetHashCode();
			}
		}

		class OnlyKeyCompare<K, V> : IEqualityComparer<KeyValuePair<K, V>>
		{

			public bool Equals(KeyValuePair<K, V> x, KeyValuePair<K, V> y)
			{

				return (object)x.Key != (object)y.Key;
			}

			public int GetHashCode(KeyValuePair<K, V> obj)
			{
				return obj.Key.GetHashCode();
			}
		}

		#endregion

		#region Data 
		static string tooltip = null;

		#region Main Game
		private static Coroutine lastExtent;

		public static readonly string subCategoryName = "Morph";
		public static readonly string displayName = "Chara Morph";

#if HONEY_API
		public static CvsO_Type charaCustom { get; private set; } = null;
		public static CvsB_ShapeBreast boobCustom { get; private set; } = null;
		public static CvsB_ShapeWhole bodyCustom { get; private set; } = null;
		public static CvsF_ShapeWhole faceCustom { get; private set; } = null;
#elif KOI_API
		public static CvsChara charaCustom { get; private set; } = null;
		public static CvsBreast boobCustom { get; private set; } = null;
		public static CvsBodyShapeAll bodyCustom { get; private set; } = null;
		public static CvsFaceShapeAll faceCustom { get; private set; } = null;
#endif

		private static int abmxIndex = -1;
		//private readonly static List<MorphMakerSlider> sliders = new List<MorphMakerSlider>();
		//private readonly static List<MorphMakerDropdown> modes = new List<MorphMakerDropdown>();
		private readonly static List<Action<MorphControls>> sliderValActions = new List<Action<MorphControls>>();
		//static Dictionary<EventHandler, List<EventHandler>> handlerList = new Dictionary<EventHandler, List<EventHandler>>();
		static EventHandler enableEvent = null;
		static EventHandler enableABMXEvent = null;
		static EventHandler saveAsMorphDataEvent = null;
		static EventHandler linkOverallSlidersEvent = null;
		static EventHandler enableCalcTypesEvent = null;
		static EventHandler lastUCMDEvent = null;
		static EventHandler loadInitMorphCharacterEvent = null;
		static EventHandler userDefaultAsDefaultEvent = null;
		static EventHandler currentControlNameEvent = null;
		static EventHandler easyMorphOverallEvent = null;
		static EventHandler easyMorphDefaultingEvent = null;
		static EventHandler enableTooltipsEvent = null;
		static Action<string[]> controlSetChangedAct = null;
		static Action<bool, bool, bool> loadDefaultValues = null;

		private static bool m_morphLoadToggle = true;
		private static bool m_morphCharaSpecificEnablesToggle = false;
		public static bool MorphCharaSpecificEnablesToggle
		{
			get => true;
			private set => m_morphCharaSpecificEnablesToggle = value;
		}

		public static bool MorphLoadToggle
		{
			get => true;
			private set => m_morphLoadToggle = value;
		}
		//internal static MorphMakerDropdown select = null;
		#endregion

		#region Studio
		internal static Rect winRec = new Rect(105, 535, 440, 455);//{ "x":103, "y":534, "width":439, "height":457 }
		internal static Texture2D morphTex = null;
		internal static CharacterMorpherIL2CPP_Controller morphCtrl = null;
		static bool enableStudioUI = false;
		static UnityEvent customStudioUI = new UnityEvent();
		#endregion

		#endregion

		GUIStyle tmpSty;

		internal static void ImidiateGUI()
		{
			var bgTex = greyTex;

			GUI.DrawTexture(winRec = GUI.Window(GUID.GetHashCode(),
				winRec, new Action<int>(id =>
				{
					//var studioCtrl = Studio.Studio.Instance;
					//var camCtrl = studioCtrl.cameraCtrl;

					customStudioUI.Invoke();



					GUI.DragWindow();
					if(winRec.Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y)))
						Input.ResetInputAxes();

					if(!cfg.studioWinRec.Value.Equals(winRec))
						cfg.studioWinRec.Value = new Rect(winRec);
				}), ModName),
				bgTex,
				ScaleMode.StretchToFill);
		}

		internal static void Initialize()
		{
			Cleanup();
			void OnLoad()
			{
				var obj = new GameObject();
				obj.AddComponent<CharacterMorpherIL2CPP_GUI>();
				obj.transform.SetAsLastSibling();
				obj.name = "CharaMorpher_GUI";
				 
			};
			OnLoad();

			Instance.guiEvent.AddListener(new Action(ImidiateGUI));

			#region Init Stuff

			var midScrollPos = Vector2.zero;
			var topScrollPos = Vector2.zero;
			var toolPos = Vector2.zero;
			var init = false;
			var selectedChar = -1;
			//var lastSelecCharNum = 0;
			winRec = new Rect(cfg.studioWinRec.Value);
			Func<int> dropdown = null;

			string[] searchHits = new string[] { "overall", "abmx" };
			var tmpSliderLableStyle = (GUIStyle)null;
			var tmpSliderValStyle = (GUIStyle)null;


			void CreatSlider(string settingName, CharacterMorpherIL2CPP_Controller ctrl1, float min = 0, float max = 1)
			{
				var Logger = CharaMorpherIL2CPP_Core.Logger;

				var visualName = "" + settingName;
				if(tmpSliderLableStyle == null)
					tmpSliderLableStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
				//var visualNameLow = visualName.ToLower();

				tmpSliderLableStyle.normal.textColor = Color.cyan;

				//find section index
				if(settingName.ToLower().Contains(searchHits[1]))
				{
					//abmxIndex = abmxIndex >= 0 ? abmxIndex : sliders.Count;
					//if(cfg.debug.Value) Logger.LogDebug($"ABMX index: {abmxIndex}");

					tmpSliderLableStyle.normal.textColor = Color.yellow;
					//if(!cfg.enableABMX.Value || !ABMXDependency.IsInTargetVersionRange) return;
				}

				//add space after separator
				if(settingName.ToLower().Contains(searchHits[0]))
				{
					GUILayout.Space(20);//create space

					string part = Regex.Replace(visualName, searchHits[0], visualName.ToLower().
						Contains(searchHits[1]) ? "" : "Base", RegexOptions.IgnoreCase).
						Replace("  ", " ").Trim();

					GUILayout.Label($"{part} Controls", tmpSliderLableStyle);

					GUILayout.Space(10);//create space
				}


				if(visualName.ToLower().Contains("other"))
					visualName = "Other";
				else
					//remove search hits from the slider name
					foreach(var hit in searchHits)
						if(hit != searchHits[0])
							visualName = Regex.Replace(visualName, hit, "", RegexOptions.IgnoreCase);


				//Slider
				if(ctrl1)
				{

					GUILayout.BeginHorizontal();

					try
					{

						float w = winRec.width * .25f;
						float w2 = winRec.width * .1f;
						//	var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerLeft };

						GUILayout.Label($"{visualName}:", GUILayout.Width(w));


						var dat = ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data;

						ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data =
							GUILayout.HorizontalSlider(ctrl1
							.controls.all[ctrl1
							.controls.currentSet][settingName].data, min, max);

						if(tmpSliderValStyle == null)
							tmpSliderValStyle = new GUIStyle(GUI.skin.textField);
						tmpSliderValStyle.alignment = TextAnchor.MiddleRight;


						var txtval = GUILayout.TextField
								($"{ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data * 100:0.}",
								tmpSliderValStyle, GUILayout.Width(w2), GUILayout.ExpandHeight(true));
						if(double.TryParse(txtval.IsNullOrEmpty() ? "0" : txtval, out var val))
						{
							//val.ToString(); 
							var slideUpdate = dat != ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data;
							var valUpdate = dat != ((float)(val * .01));
							if((valUpdate || slideUpdate))
							{
								//var dif = (ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data * 100) - val;
								//val += dif;
								//val=Mathf.Round(val);

								if(cfg.debug.Value) Logger.LogInfo($"{visualName}: {(dat)}:{((float)(val * .01))}");

								ctrl1.controls.all[ctrl1.controls.currentSet][settingName].SetData((float)(val * .01));

								for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
									ctrl1?.StartCoroutine(ctrl1?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

								if(cfg.debug.Value) Logger.LogDebug("controls Changed");
							}
						}

						if(GUILayout.Button("Reset", GUILayout.Width(w2)))
						{
							ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data =
							(float)(ctrl1.ctrls1)
							?.Clone()?.all[ctrl1.controls.currentSet][settingName].data;

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl1?.StartCoroutine(ctrl1?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

							if(cfg.debug.Value) Logger.LogDebug("controls Reset");
						}
					}

					catch(Exception e)
					{
						Logger.LogInfo($"Current Slot: {ctrl1.controls.currentSet}");
						Logger.LogInfo($"Setting Name: {settingName}");
						Logger.LogError(e);
					}

					GUILayout.EndHorizontal();
				}

				////add separator after overall control
				//if((settingName.ToLower().Contains(searchHits[0])))
				//	GUILayout.Label(new string('─', settingName.Length * 2));//create separator line

			}

			void CreateShapeSlider(string settingName, CharacterMorpherIL2CPP_Controller ctrl1)
			{
				CreatSlider(settingName, ctrl1, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f);
			}

			void CreateVoiceSlider(string settingName, CharacterMorpherIL2CPP_Controller ctrl1)
			{
				CreatSlider(settingName, ctrl1);
			}

			Coroutine tmp = null;
			bool lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed
															   //var allCtrls = (IEnumerable<CharacterMorpherIL2CPP_Controller>)null;
			var selectedCtrls = (IEnumerable<CharacterMorpherIL2CPP_Controller>)null;
			var refcomp = new UnityObjRefEqualsCompare<CharacterMorpherIL2CPP_Controller>();//required
			int skipFrames = 0;

			cfg.preferCardMorphDataMaker.SettingChanged +=
			lastUCMDEvent = (s, o) =>
			{
				IEnumerator CoUCMD()
				{
					yield return morphCtrl?.StartCoroutine(CoUCMDCommon(morphCtrl, lastUCMD));

					lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed

					yield break;
				}

				if(tmp != null)
					morphCtrl.StopCoroutine(tmp);
				tmp = morphCtrl.StartCoroutine(CoUCMD());
			};

			OnNewTargetImage.AddListener(new Action<string, byte[]>((str, data) =>
			{
				morphTex = str?.CreateTexture(data);



			}));

			#endregion
			var tabstyle = new GUIStyle(GUI.skin.button)
			{

				padding = new RectOffset() { left = 5, right = 5, top = 5, bottom = 0 },
				alignment = TextAnchor.UpperLeft

			};
			var inst = CharaMorpherIL2CPP_Core.Instance;

			//Game Update Loop
			customStudioUI.AddListener(new Action(() =>
			{
				var Logger = CharaMorpherIL2CPP_Core.Logger;
				if((skipFrames = Math.Max(-1, --skipFrames)) > -1)
					return;
				try
				{
					GUILayout.BeginVertical();
					var selectedCharacterChanged = false;
					var tmpSelectedCtrls = GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>();

					if((!selectedCtrls?.SequenceEqual(tmpSelectedCtrls, refcomp)) ?? true)
					{
						selectedCtrls = tmpSelectedCtrls.ToList();

						//todo: Code for change in selected controllers
						selectedCharacterChanged = true;
					}

					#region Top

					#region Toggles
					topScrollPos = GUILayout.BeginScrollView(topScrollPos, GUILayout.Height(winRec.height * .20f));

					GUILayout.Label("Enables:");
					var enable = GUILayout.Toggle(cfg.enable.Value, GUIContent.Temp("Enable", cfg.enable.Description.Description));
					var enableABMX = GUILayout.Toggle(cfg.enableABMX.Value, new GUIContent { text = "Enable ABMX", tooltip = cfg.enableABMX.Description.Description });
					var charEnable = morphCtrl ? GUILayout.Toggle(morphCtrl.morphEnable, new GUIContent { text = "Chara. Enable", tooltip = "Character specific enable button (gets saved with card)" }) : false;
					var charEnableABMX = morphCtrl ? GUILayout.Toggle(morphCtrl.morphEnableABMX, new GUIContent { text = "Chara. Enable ABMX", tooltip = "Character specific enable ABMX button (gets saved with card)" }) : false;
					var saveExtData = GUILayout.Toggle(cfg.saveExtData.Value, new GUIContent { text = "Save Ext. Data", tooltip = cfg.saveExtData.Description.Description });
					var linkOverallABMXSliders = GUILayout.Toggle(cfg.linkOverallABMXSliders.Value, new GUIContent { text = "Link Overall Sliders to ABMX Overall Sliders", tooltip = cfg.linkOverallABMXSliders.Description.Description });

					var preferCardMorphDataMaker = GUILayout.Toggle(cfg.preferCardMorphDataMaker.Value, new GUIContent { text = "Use Card Morph Data", tooltip = cfg.preferCardMorphDataMaker.Description.Description });
					var loadInitMorphCharacter = GUILayout.Toggle(cfg.loadInitMorphCharacter.Value, new GUIContent { text = "Load Init. Character", tooltip = cfg.loadInitMorphCharacter.Description.Description });
					var useDefaultAsDefault = GUILayout.Toggle(cfg.userDefaultAsDefault.Value, new GUIContent { text = "Use Default as Default", tooltip = cfg.userDefaultAsDefault.Description.Description });

					var enableTooltip = GUILayout.Toggle(cfg.enableTooltips.Value, GUIContent.Temp("Enable Tooltips", cfg.enableTooltips.Description.Description));

					//Update checks
					{
						if(enable != cfg.enable.Value)
							cfg.enable.Value = enable;
						if(enableABMX != cfg.enableABMX.Value)
							cfg.enableABMX.Value = enableABMX;

						//char enables
						if(morphCtrl)
						{
							if(charEnable != morphCtrl.morphEnable)
								morphCtrl.Enable = charEnable;

							if(charEnableABMX != morphCtrl.morphEnableABMX)
								morphCtrl.EnableABMX = charEnableABMX;
						}

						if(saveExtData != cfg.saveExtData.Value)
							cfg.saveExtData.Value = saveExtData;

						if(linkOverallABMXSliders != cfg.linkOverallABMXSliders.Value)
						{
							cfg.linkOverallABMXSliders.Value = linkOverallABMXSliders;

							if(morphCtrl && morphCtrl.IsInitLoadFinished)
								for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
									morphCtrl.StartCoroutine(morphCtrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)
						}

						if(preferCardMorphDataMaker != cfg.preferCardMorphDataMaker.Value)
							cfg.preferCardMorphDataMaker.Value = preferCardMorphDataMaker;

						if(loadInitMorphCharacter != cfg.loadInitMorphCharacter.Value)
							cfg.loadInitMorphCharacter.Value = loadInitMorphCharacter;

						if(useDefaultAsDefault != cfg.userDefaultAsDefault.Value)
							cfg.userDefaultAsDefault.Value = useDefaultAsDefault;

						if(enableTooltip != cfg.enableTooltips.Value)
							cfg.enableTooltips.Value = enableTooltip;
					}

					GUILayout.EndScrollView();

					GUILayout.Space(10);//create space
					#endregion

					#region Tabs   
					var names = selectedCtrls.Attempt(a => Default.TryTranslate(a.HumanData.Parameter.fullname, out var trans) ? trans : a.HumanData.Parameter.fullname).ToArray();
					var h = 25.0f;
					var bar = 15.0f;

					toolPos = GUILayout.BeginScrollView(toolPos, true, false, new GUIStyle(GUI.skin.horizontalScrollbar), GUIStyle.none, GUILayout.Height(h + bar), GUILayout.ExpandWidth(true));
					//	GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));



					//tabstyle.wordWrap = true;

					var selec = GUILayout.Toolbar(selectedChar, names, tabstyle, GUILayout.ExpandHeight(false), GUILayout.Width(winRec.width * 0.2f * names.Length));

					//tab changed
					if(selec != selectedChar || selectedCharacterChanged)
					{
						selectedChar = selec;

						if(names.Length > 0 && !names.InRange(selec))
							selectedChar = selec = Mathf.Clamp(selec, 0, names.Length);

						var mctrl = selectedCtrls.InRange(selec) ? selectedCtrls.ElementAt(selec) : null;
						skipFrames = (morphCtrl == null) != (mctrl == null) ? 3 : 0;
						morphCtrl = mctrl;
						//	Logger.LogMessage(ctrl ? "New Tab Selected" : "No Tab selected");

						//Code Here...
						string p = Path.Combine(cfg.charDir.Value.MakeDirPath(), cfg.imageName.Value.MakeDirPath());
						OnNewTargetImage.Invoke(p, null);


					}

					GUILayout.EndScrollView();

					#endregion
					#endregion

					#region Mid

					#region ScrollView
					midScrollPos = GUILayout.BeginScrollView(midScrollPos, GUILayout.ExpandWidth(true));

					#region Morph Image
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					GUILayout.BeginVertical();

					float w = winRec.width * .5f;
					var boxStyle = new GUIStyle(GUI.skin.box);


					GUILayout.Box(morphTex, GUILayout.Width(w), GUILayout.Height(w * 1.333f));

					if(GUILayout.Button("Set New Morph Target"))
						GetNewImageTarget();

					if(GUILayout.Button("Clear Morph Target"))
						morphCtrl.MorphTargetUpdate(clearTarget: true);

					GUILayout.EndVertical();
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					#endregion

					#region Slider Stuff


					foreach(var cat in inst.controlCategories[defaultStr])
						if(cat.dataName.ToLower().Contains("voice"))
							CreateVoiceSlider(cat.dataName, morphCtrl);
						else
							CreateShapeSlider(cat.dataName, morphCtrl);


					#endregion

					GUILayout.EndScrollView();
					GUILayout.Space(20);//create space
					#endregion

					#endregion

					#region Bot

					GUILayout.BeginVertical(GUILayout.Height(winRec.height * .20f), GUILayout.ExpandWidth(true));

					#region Dropdown
					//dropdown init
					if(!init)
					{
						CharacterMorpherIL2CPP_Controller last = null;
						dropdown = GUILayoutDropdownDrawer(
							scrollHeight: 93 * .5f,
							content: (ctn, index) => new GUIContent { text = $"Current Slot: {morphCtrl?.controls?.currentSet ?? cfg.currentControlSetName.Value ?? "None"} " },
							listUpdate: (old) =>
							{
								if(last != morphCtrl || !init)
									return
									(morphCtrl?.controls?.all?.Keys.ToList() ??
									inst?.controlCategories?.Keys.ToList())
									.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
									k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
									.ToArray() ??
									new string[0];
								return old;
							},
							modSelected: (selected) =>
							{
								if(last != morphCtrl)
								{
									selected = SwitchControlSet((
									morphCtrl?.controls?.all?.Keys?.ToList() ??
									inst?.controlCategories?.Keys.ToList())
									.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
									k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
									.ToArray() ??
									new string[0], morphCtrl?.controls?.currentSet);
									last = morphCtrl;
								}

								return selected;
							},
							onSelect: (selected) =>
							SwitchControlSet((
									morphCtrl?.controls?.all?.Keys?.ToList() ??
									inst?.controlCategories?.Keys.ToList())
									.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
									k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
									.ToArray() ??
									new string[0], selected));

					}

					var selected = dropdown?.Invoke() ?? 0;
					#endregion

					#region Buttons
					var MorphBackupCtrl = morphCtrl?.ctrls1;

					GUILayout.BeginHorizontal();
					if(GUILayout.Button("Load From Current Slot") && morphCtrl)
					{
						morphCtrl.controls.Copy(MorphBackupCtrl);
						morphCtrl.controls.currentSet = cfg.currentControlSetName.Value;

						for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
							morphCtrl.StartCoroutine(morphCtrl.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

						Logger.LogMessage($"Loaded CharaMorpher {morphCtrl.controls.currentSet}");
					}

					if(GUILayout.Button("Save To Current Slot") && morphCtrl)
					{
						MorphBackupCtrl.Copy(morphCtrl.controls);
						Logger.LogMessage($"Saved as CharaMorpher {morphCtrl.controls.currentSet}");
					}

					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					if(GUILayout.Button("Add New Slot"))
					{
						AddNewSetting(ctrl1: morphCtrl, useUserDefault: useDefaultAsDefault);

						//update dropdown list				 
						//UpdateGUISelectList();
						SwitchControlSet((morphCtrl?.controls?.all?.Keys?.ToList() ??
									inst?.controlCategories?.Keys.ToList())
									.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
									k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
									.ToArray() ??
									new string[0], morphCtrl?.controls.currentSet);
					}

					if(GUILayout.Button("Remove Current Slot"))
					{
						//switch control before deletion
						var list = (morphCtrl?.controls?.all?.Keys?.ToList() ??
									inst?.controlCategories?.Keys.ToList())
									.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
									k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
									.ToArray() ??
									new string[0];

						int tmpVal = selected;
						if(selected >= list.Length - 1)
							tmpVal = list.Length - 2;

						RemoveCurrentSetting(morphCtrl?.controls?.currentSet ?? cfg.currentControlSetName.Value, morphCtrl);

						//UpdateGUISelectList();
						SwitchControlSet(list, tmpVal, ctrl: morphCtrl);
					}
					GUILayout.EndHorizontal();
					#endregion

					GUILayout.EndVertical();
					#endregion

					GUILayout.EndVertical();


					init = true;//initial run complete
				}
				catch(Exception e) { Logger.LogError(e); }

				IMGUITooltipMsg(enableTip: cfg.enableTooltips.Value);
			}));

		}

		private static void Cleanup()
		{
			abmxIndex = -1;
			m_morphLoadToggle = true;
			//	select = null;
			customStudioUI.RemoveAllListeners();
			//	sliders.Clear();
			//	modes.Clear();


			if(enableEvent != null)
				cfg.enable.SettingChanged -= enableEvent;
			if(enableABMXEvent != null)
				cfg.enableABMX.SettingChanged -= enableABMXEvent;
			if(lastUCMDEvent != null)
				cfg.preferCardMorphDataMaker.SettingChanged -= lastUCMDEvent;
			if(linkOverallSlidersEvent != null)
				cfg.linkOverallABMXSliders.SettingChanged -= linkOverallSlidersEvent;
			if(saveAsMorphDataEvent != null)
				cfg.saveExtData.SettingChanged -= saveAsMorphDataEvent;
			if(enableCalcTypesEvent != null)
				cfg.enableCalcTypes.SettingChanged -= enableCalcTypesEvent;
			if(currentControlNameEvent != null)
				cfg.currentControlSetName.SettingChanged -= currentControlNameEvent;
			if(loadInitMorphCharacterEvent != null)
				cfg.loadInitMorphCharacter.SettingChanged -= loadInitMorphCharacterEvent;
			if(userDefaultAsDefaultEvent != null)
				cfg.userDefaultAsDefault.SettingChanged -= userDefaultAsDefaultEvent;
			if(easyMorphOverallEvent != null)
				cfg.easyMorphBtnOverallSet.SettingChanged -= easyMorphOverallEvent;
			if(easyMorphDefaultingEvent != null)
				cfg.easyMorphBtnEnableDefaulting.SettingChanged -= easyMorphDefaultingEvent;
			if(enableTooltipsEvent != null)
				cfg.enableTooltips.SettingChanged -= enableTooltipsEvent;

			if(controlSetChangedAct != null)
				OnInternalControlListChanged.RemoveListener(controlSetChangedAct);

			foreach(var act in sliderValActions)
				OnInternalSliderValueChange.RemoveListener(act);
			sliderValActions.Clear();

		}


		public static void LoadCurrentDefaultValues(bool showMessage = true, bool playSound = true, bool runUpdate = true) => loadDefaultValues?.Invoke(showMessage, playSound, runUpdate);

		#region Other
		static IEnumerator CoUCMDCommon(CharacterMorpherIL2CPP_Controller ctrl, bool lastUCMD)
		{


			string name =
			(!cfg.preferCardMorphDataMaker.Value ?
			ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
			name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDiv), 0, name.Length));


			{
				//	Logger.LogDebug($"lastUCMD: {lastUCMD}");
				yield return new WaitWhile(new Func<bool>(() => ctrl.IsReloading));

				var tmpCtrls =
				!cfg.preferCardMorphDataMaker.Value ?
				ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1);
				tmpCtrls.currentSet = ctrl.controls.currentSet + "";

				ctrl.controls.Copy(!lastUCMD ? ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

				ctrl.SoftSaveControls(lastUCMD);
				ctrl.controls.Copy(!cfg.preferCardMorphDataMaker.Value ?
				ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));


				//Logger.LogDebug($"Next lastUCMD: {lastUCMD}");
			}

			if(!name.IsNullOrEmpty())
				SwitchControlSet(ControlsList, name);

		}

		#endregion

		#region Image Stuff

		#region File Data
		public const string FileExt = ".png";
		public const string FileFilter = "Character Images (*.png)|*.png";

		private static readonly string _defaultOverlayDirectory = Path.Combine(Directory.GetCurrentDirectory(), "/UserData/chara/").MakeDirPath();
		public static string TargetDirectory { get => Path.GetDirectoryName(TargetPath).MakeDirPath(); }

		public static string TargetPath
		{
			get
			{
				//	var tmp = Path.GetFileName(cfg.imageName.Value).MakeDirPath();
				//	var path = Path.Combine(Path.GetDirectoryName(cfg.charDir.Value).MakeDirPath(), tmp);
				return Path.Combine(cfg.charDir.Value.MakeDirPath(), cfg.imageName.Value.MakeDirPath());
			}
		}
		#endregion

		//private static string MakeDirPath(string path) => MakeDirPath(path);

		/// <summary>
		/// Called after a file is chosen in file explorer menu  
		/// </summary>
		/// <param name="strings: ">the info returned from file explorer. strings[0] returns the full file path</param>
		private static void OnImageTargetObtained(string[] strings)
		{

			ForeGrounder.RevertForground();
			if(cfg.debug.Value) Logger.LogDebug($"Enters accept");
			if(strings == null || strings.Length == 0) return;
			var texPath = strings[0].MakeDirPath();

			if(cfg.debug.Value)
			{
				Logger.LogDebug($"Original path: {texPath}");
				Logger.LogDebug($"texture path: {Path.Combine(Path.GetDirectoryName(texPath), Path.GetFileName(texPath))}");
			}

			if(string.IsNullOrEmpty(texPath)) return;

			cfg.charDir.Value = Path.GetDirectoryName(texPath).MakeDirPath();
			cfg.imageName.Value = texPath.Substring(texPath.LastIndexOf('/') + 1).MakeDirPath();//not sure why this happens on hs2?

			foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
				if(ctrl.IsInitLoadFinished)
					ctrl.StartCoroutine(ctrl.CoMorphTargetUpdate(5));

			if(cfg.debug.Value) Logger.LogDebug($"Exit accept");
		}

		public static void GetNewImageTarget()
		{
			var paths = OpenFileDialog.ShowDialog("Set Morph Target",
				TargetDirectory.MakeDirPath("/", "\\"),
				FileFilter,
				FileExt,
				OpenFileDialog.SingleFileFlags);

			OnImageTargetObtained(paths);

			//	Illusion.Game.Utils.Sound.Play(paths?.Any() ?? false ? Illusion.Game.SystemSE.ok_l : Illusion.Game.SystemSE.cancel);
		}

		#endregion


	}
}
