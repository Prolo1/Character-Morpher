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
using TMPro;

using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Chara;
using KKABMX.Core;
using UniRx;
using UniRx.Triggers;

#if HONEY_API
using CharaCustom;
using AIChara;
#else
using ChaCustom;
#endif

using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.Morph_Util;
using static KKAPI.Maker.MakerAPI;
using static KKAPI.Studio.StudioAPI;

namespace Character_Morpher
{
	class CharaMorpher_GUI : MonoBehaviour
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

		#region Main Game
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
		private readonly static List<UnityAction<MorphControls>> sliderValActions = new List<UnityAction<MorphControls>>();
		//static Dictionary<EventHandler, List<EventHandler>> handlerList = new Dictionary<EventHandler, List<EventHandler>>();
		static EventHandler enableEvent = null;
		static EventHandler enableABMXEvent = null;
		static EventHandler saveAsMorphDataEvent = null;
		static EventHandler linkOverallSlidersEvent = null;
		static EventHandler enableCalcTypesEvent = null;
		static EventHandler lastUCMDEvent = null;
		static EventHandler loadInitMorphCharacterEvent = null;
		static EventHandler currentControlNameEvent = null;
		static EventHandler easyMorphOverallEvent = null;
		static EventHandler easyMorphDefaultingEvent = null;
		static UnityAction<string[]> controlSetChangedAct = null;
		static UnityAction<bool, bool, bool> loadDefaultValues = null;

		private static bool m_morphLoadToggle = true;
		public static bool MorphLoadToggle
		{
			get => !InsideMaker || m_morphLoadToggle;
			private set => m_morphLoadToggle = value;
		}
		private static MorphMakerDropdown select = null;
		#endregion

		#region Studio
		internal static Rect winRec = new Rect(105, 535, 440, 455);//{ "x":103, "y":534, "width":439, "height":457 }
		internal static Texture2D morphTex = null;
		static bool enableStudioUI = false;
		static UnityEvent customStudioUI = new UnityEvent();
		#endregion

		#endregion

		void OnGUI()
		{
			if(!StudioLoaded || !enableStudioUI) return;

			var bgTex = greyTex;

			GUI.DrawTexture(winRec = GUI.Window(CharaMorpher_Core.GUID.GetHashCode(),
				winRec, id =>
				{
					//var studioCtrl = Studio.Studio.Instance;
					//var camCtrl = studioCtrl.cameraCtrl;

					customStudioUI.Invoke();

					winRec = IMGUIUtils.DragResizeEatWindow(id, winRec);


					if(!cfg.studioWinRec.Value.Equals(winRec))
						cfg.studioWinRec.Value = new Rect(winRec);
				}, ModName),
				bgTex,
				ScaleMode.StretchToFill);
		}

		internal static void Initialize()
		{
			Cleanup();
			CharaMorpher_Controller ctrl = null;

			if(InsideStudio)
			{

				StudioLoadedChanged += (m, n) =>
				{
					var obj = new GameObject();
					obj.AddComponent<CharaMorpher_GUI>();
					obj.transform.SetAsLastSibling();
					obj.name = "CharaMorpher_GUI";

					CustomToolbarButtons.AddLeftToolbarToggle
						(new Texture2D(32, 32),
						onValueChanged: val =>
						{
							enableStudioUI = val;
							//init = false;
						}).OnGUIExists(gui =>
						{
							//Toggle image bi-pass
							iconBG.filterMode = FilterMode.Bilinear;

							var btn = gui.ControlObject.GetComponentInChildren<Button>();
							btn.image.sprite =
							Sprite.Create(iconBG,
							new Rect(0, 0, iconBG.width, iconBG.height),
							Vector2.one * .5f);

							btn.image.color = Color.white;
						});
				};

				#region Init Stuff

				var midScrollPos = Vector2.zero;
				var topScrollPos = Vector2.zero;
				var toolPos = Vector2.zero;
				var init = false;
				var selectedTool = -1;
				//var lastSelecCharNum = 0;
				winRec = new Rect(cfg.studioWinRec.Value);
				Func<int> dropdown = null;

				string[] searchHits = new string[] { "overall", "abmx" };
				var tmpSliderLableStyle = (GUIStyle)null;
				void CreatSlider(string settingName, CharaMorpher_Controller ctrl1, float min = 0, float max = 1)
				{
					var visualName = "" + settingName;
					if(tmpSliderLableStyle == null)
						tmpSliderLableStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.BoldAndItalic };
					//var visualNameLow = visualName.ToLower();

					tmpSliderLableStyle.normal.textColor = Color.cyan;

					//find section index
					if(settingName.ToLower().Contains(searchHits[1]))
					{
						abmxIndex = abmxIndex >= 0 ? abmxIndex : sliders.Count;
						if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"ABMX index: {abmxIndex}");

						tmpSliderLableStyle.normal.textColor = Color.yellow;
						if(!cfg.enableABMX.Value || !ABMXDependency.InTargetVersionRange) return;
					}

					//add space after separator
					if(settingName.ToLower().Contains(searchHits[0]))
					{
						GUILayout.Space(20);//create space

						string part = Regex.Replace(visualName, searchHits[0], visualName.ToLower().Contains(searchHits[1]) ? "" : "Base", RegexOptions.IgnoreCase).
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

							ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data =
								GUILayout.HorizontalSlider(ctrl1
								.controls.all[ctrl1
								.controls.currentSet][settingName].data, min, max);


							if(float.TryParse(GUILayout.TextField(
									$"{ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data:f2}", GUILayout.Width(w2), GUILayout.ExpandHeight(true)),
									out var result))
							{

								if(ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data != result)
								{
									ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data = result;
									for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
										ctrl1?.StartCoroutine(ctrl1?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

									if(cfg.debug.Value) Morph_Util.Logger.LogDebug("controls Changed");
								}
							}

							if(GUILayout.Button("Reset", GUILayout.Width(w2)))
							{
								ctrl1.controls.all[ctrl1.controls.currentSet][settingName].data =
								(float)(!ctrl1.IsUsingExtMorphData ? ctrl1.ctrls1 :
								(ctrl1.ctrls2 ?? ctrl1.ctrls1))
								?.Clone()?.all[ctrl1.controls.currentSet][settingName].data;

								for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
									ctrl1?.StartCoroutine(ctrl1?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

								if(cfg.debug.Value) Morph_Util.Logger.LogDebug("controls Reset");
							}
						}

						catch(Exception e)
						{
							Morph_Util.Logger.LogInfo($"Current Slot: {ctrl1.controls.currentSet}");
							Morph_Util.Logger.LogInfo($"Setting Name: {settingName}");
							Morph_Util.Logger.LogError(e);
						}

						GUILayout.EndHorizontal();
					}

					////add separator after overall control
					//if((settingName.ToLower().Contains(searchHits[0])))
					//	GUILayout.Label(new string('─', settingName.Length * 2));//create separator line

				}

				void CreateShapeSlider(string settingName, CharaMorpher_Controller ctrl1)
				{
					CreatSlider(settingName, ctrl1, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f);
				}

				void CreateVoiceSlider(string settingName, CharaMorpher_Controller ctrl1)
				{
					CreatSlider(settingName, ctrl1);
				}

				Coroutine tmp = null;
				bool lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed
				cfg.preferCardMorphDataMaker.SettingChanged +=
				lastUCMDEvent = (s, o) =>
				{
					IEnumerator CoUCMD()
					{
						yield return ctrl?.StartCoroutine(CoUCMDCommon(ctrl, lastUCMD));

						lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed

						yield break;
					}

					if(tmp != null)
						ctrl.StopCoroutine(tmp);
					tmp = ctrl.StartCoroutine(CoUCMD());
				};

				#endregion

				//var allCtrls = (IEnumerable<CharaMorpher_Controller>)null;
				var selectedCtrls = (IEnumerable<CharaMorpher_Controller>)null;
				var refcomp = new UnityObjRefEqualsCompare<CharaMorpher_Controller>();//required

				//Update Loop
				customStudioUI.AddListener(() =>
				{
					try
					{
						GUILayout.BeginVertical();
						bool selectedCharacterChanged = false;
						var tmpSelectedCtrls = GetSelectedControllers<CharaMorpher_Controller>();

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
						var enable = GUILayout.Toggle(cfg.enable.Value, "Enable");
						var enableABMX = GUILayout.Toggle(cfg.enableABMX.Value, "Enable ABMX");
						var charEnable = ctrl ? GUILayout.Toggle(ctrl.morphEnable, "Chara. Enable") : true;
						var charEnableABMX = ctrl ? GUILayout.Toggle(ctrl.morphEnableABMX, "Chara. Enable ABMX") : true;

						var linkOverallABMXSliders = GUILayout.Toggle(cfg.linkOverallABMXSliders.Value, "Link Overall Sliders to ABMX Overall Sliders");

						var preferCardMorphDataMaker = GUILayout.Toggle(cfg.preferCardMorphDataMaker.Value, "Use Card Morph Data");
						var loadInitMorphCharacter = GUILayout.Toggle(cfg.loadInitMorphCharacter.Value, "Load Init. Character");

						//Update checks
						{
							if(enable != cfg.enable.Value)
								cfg.enable.Value = enable;
							if(enableABMX != cfg.enableABMX.Value)
								cfg.enableABMX.Value = enableABMX;

							//char enables
							if(ctrl)
							{
								if(charEnable != ctrl.morphEnable)
									ctrl.Enable = charEnable;

								if(charEnableABMX != ctrl.morphEnableABMX)
									ctrl.EnableABMX = charEnableABMX;
							}

							if(linkOverallABMXSliders != cfg.linkOverallABMXSliders.Value)
							{
								cfg.linkOverallABMXSliders.Value = linkOverallABMXSliders;

								if(ctrl && ctrl.isInitLoadFinished)
									for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
										ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)

							}

							if(preferCardMorphDataMaker != cfg.preferCardMorphDataMaker.Value)
							{
								cfg.preferCardMorphDataMaker.Value = preferCardMorphDataMaker;

								string p = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
								morphTex = ctrl?.IsUsingExtMorphData ?? false ? ctrl?.m_data2.main.pngData?.LoadTexture() ?? Texture2D.blackTexture : p.CreateTexture();
								if(ctrl)
								{
									//dim card image
									if(ctrl.IsUsingExtMorphData)
									{
										var pix = morphTex.GetPixels();
										foreach(var i in pix)
											i.AlphaMultiplied(.5f);
										morphTex.SetPixels(pix);
										morphTex.Apply();
									}
								}
							}

							if(loadInitMorphCharacter != cfg.loadInitMorphCharacter.Value)
								cfg.loadInitMorphCharacter.Value = loadInitMorphCharacter;
						}
						GUILayout.EndScrollView();

						GUILayout.Space(10);//create space
						#endregion

						#region Tabs

						var names = selectedCtrls.Attempt(a => TranslationHelper.TryTranslate(a.ChaFileControl.parameter.fullname, out var trans) ? trans : a.ChaFileControl.parameter.fullname).ToArray();
						var h = 25.0f;
						var bar = 15.0f;

						toolPos = GUILayout.BeginScrollView(toolPos, true, false, new GUIStyle(GUI.skin.horizontalScrollbar), GUIStyle.none, GUILayout.Height(h + bar), GUILayout.ExpandWidth(true));
						//	GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));

						var tabstyle = new GUIStyle(GUI.skin.button)
						{

							padding = new RectOffset(5, 5, 5, 0),
							alignment = TextAnchor.UpperLeft

						};

						//tabstyle.wordWrap = true;

						var selec = GUILayout.Toolbar(selectedTool, names, tabstyle, GUILayout.ExpandHeight(false), GUILayout.Width(winRec.width * 0.2f * names.Length));

						//tab changed
						if(selec != selectedTool || selectedCharacterChanged)
						{
							selectedTool = selec;

							if(names.Length > 0 && !names.InRange(selec))
								selectedTool = selec = Mathf.Clamp(selec, 0, names.Length);

							ctrl = selectedCtrls.InRange(selec) ? selectedCtrls.ElementAt(selec) : null;

							//	Morph_Util.Logger.LogMessage(ctrl ? "New Tab Selected" : "No Tab selected");

							//Code Here...
							string p = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
							morphTex = ctrl?.IsUsingExtMorphData ?? false ? ctrl?.m_data2.main.pngData?.LoadTexture() ?? Texture2D.blackTexture : p.CreateTexture(); ;
							if(ctrl)
							{
								//dim card image
								if(ctrl.IsUsingExtMorphData)
								{
									var pix = morphTex.GetPixels();
									foreach(var i in pix)
										i.AlphaMultiplied(.5f);
									morphTex.SetPixels(pix);
									morphTex.Apply();
								}
							}
						}

						//GUILayout.EndHorizontal();
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
						{
							GetNewImageTarget();
							string p = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
							morphTex = ctrl?.IsUsingExtMorphData ?? false ? ctrl?.m_data2.main.pngData?.LoadTexture() ?? Texture2D.blackTexture : p.CreateTexture();
							if(ctrl)
							{
								//dim card image
								if(ctrl.IsUsingExtMorphData)
								{
									var pix = morphTex.GetPixels();
									foreach(var i in pix)
										i.AlphaMultiplied(.5f);
									morphTex.SetPixels(pix);
									morphTex.Apply();
								}
							}
						}

						GUILayout.EndVertical();
						GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
						#endregion

						#region Slider Stuff


						foreach(var cat in Instance.controlCategories[defaultStr])
							if(cat.dataName.ToLower().Contains("voice"))
								CreateVoiceSlider(cat.dataName, ctrl);
							else
								CreateShapeSlider(cat.dataName, ctrl);


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
							CharaMorpher_Controller last = null;
							dropdown = Morph_Util.GUILayoutDropdownDrawer(
								scrollHeight: 93 * .5f,
								content: (ctn, index) => new GUIContent { text = $"Current Slot: {ctrl?.controls?.currentSet ?? cfg.currentControlSetName.Value ?? "None"} " },
								listUpdate: (old) =>
								{
									if(last != ctrl || !init)
										return
										(ctrl?.controls?.all?.Keys.ToList() ??
										Instance?.controlCategories?.Keys.ToList())
										.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
										k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
										.ToArray() ??
										new string[0];
									return old;
								},
								modSelected: (selected) =>
								{
									if(last != ctrl)
									{
										selected = SwitchControlSet((
										ctrl?.controls?.all?.Keys?.ToList() ??
										Instance?.controlCategories?.Keys.ToList())
										.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
										k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
										.ToArray() ??
										new string[0], ctrl?.controls?.currentSet);
										last = ctrl;
									}

									return selected;
								},
								onSelect: (selected) =>
								SwitchControlSet((
										ctrl?.controls?.all?.Keys?.ToList() ??
										Instance?.controlCategories?.Keys.ToList())
										.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
										k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
										.ToArray() ??
										new string[0], selected));

						}

						dropdown?.Invoke();
						#endregion

						#region Buttons
						var MorphBackupCtrl = (!ctrl?.IsUsingExtMorphData ?? true ? ctrl?.ctrls1 :
									(ctrl?.ctrls2 ?? ctrl?.ctrls1));

						GUILayout.BeginHorizontal();
						if(GUILayout.Button("Load From Current Slot") && ctrl)
						{
							ctrl.controls.Copy(MorphBackupCtrl);
							ctrl.controls.currentSet = cfg.currentControlSetName.Value;

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

							Morph_Util.Logger.LogMessage($"Loaded CharaMorpher {ctrl.controls.currentSet}");
							Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
						}

						if(GUILayout.Button("Save To Current Slot") && ctrl)
						{
							MorphBackupCtrl.Copy(ctrl.controls);
							Morph_Util.Logger.LogMessage($"Saved as CharaMorpher {ctrl.controls.currentSet}");
							Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
						}
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						if(GUILayout.Button("Add New Slot"))
						{
							AddNewSetting(ctrl1: ctrl);

							//update dropdown list				 
							//UpdateGUISelectList();
							SwitchControlSet((ctrl?.controls?.all?.Keys?.ToList() ??
										Instance?.controlCategories?.Keys.ToList())
										.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
										k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
										.ToArray() ??
										new string[0], ctrl?.controls.currentSet);
						}

						if(GUILayout.Button("Remove Current Slot"))
						{
							//switch control before deletion
							var list = (ctrl?.controls?.all?.Keys?.ToList() ??
										Instance?.controlCategories?.Keys.ToList())
										.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ?
										k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
										.ToArray() ??
										new string[0];

							int tmpVal = select.Value;
							if(select.Value >= list.Length - 1)
								tmpVal = list.Length - 2;

							RemoveCurrentSetting(ctrl?.controls?.currentSet ?? cfg.currentControlSetName.Value, ctrl);

							//UpdateGUISelectList();
							SwitchControlSet(list, tmpVal, ctrl: ctrl);
						}
						GUILayout.EndHorizontal();

						#endregion

						GUILayout.EndVertical();
						#endregion

						GUILayout.EndVertical();

						init = true;//initial run complete
					}
					catch(Exception e) { Morph_Util.Logger.LogError(e); }
				});

			}
			else
			{

				RegisterCustomSubCategories += (s, e) =>
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
				MakerBaseLoaded += (s, e) => { AddCharaMorpherMenu(e); };
				MakerFinishedLoading += (s, e) =>
				{
					var allCvs =

#if HONEY_API
					((CvsSelectWindow[])Resources.FindObjectsOfTypeAll(typeof(CvsSelectWindow)))
					.OrderBy((k) => k.transform.GetSiblingIndex())//I just want them in the right order
					.Attempt(p => p.items)
					.Aggregate((l, r) => l.Concat(r).ToArray());//should flatten array


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
					btn?.onClick?.AddListener(() => GetMakerBase().drawMenu.ChangeMenuFunc());
#endif
				};
				MakerExiting += (s, e) => { Cleanup(); };
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

		}

		private static void Cleanup()
		{
			abmxIndex = -1;
			m_morphLoadToggle = true;
			select = null;
			customStudioUI.RemoveAllListeners();
			sliders.Clear();
			modes.Clear();


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
			if(easyMorphOverallEvent != null)
				cfg.easyMorphBtnOverallSet.SettingChanged -= easyMorphOverallEvent;
			if(easyMorphDefaultingEvent != null)
				cfg.easyMorphBtnEnableDefaulting.SettingChanged -= easyMorphDefaultingEvent;

			if(controlSetChangedAct != null)
				OnInternalControlListChanged.RemoveListener(controlSetChangedAct);

			foreach(var act in sliderValActions)
				OnInternalSliderValueChange.RemoveListener(act);
			sliderValActions.Clear();

		}

		private static void AddCharaMorpherMenu(RegisterCustomControlsEvent e)
		{
			Cleanup();//must be called (its now called elsewhere but this can stay)

			var inst = Instance;

			if(GetMakerSex() == 0 && !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

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
			e.AddControl(new MakerText("Enablers", category, Instance));

			e.AddControl(new MakerToggle(category, "Enable", cfg.enable.Value, Instance))
			  .OnGUIExists((gui) =>
			  cfg.enable.SettingChanged +=
			  enableEvent = (s, o) =>
			  gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enable.Value))
			  ?.BindToFunctionController<CharaMorpher_Controller, bool>(
				  (ctrl) => cfg.enable.Value,
				  (ctrl, val) =>
				  {
					  if(val != cfg.enable.Value)
						  cfg.enable.Value = val;
					  ShowEnabledSliders();
				  });

			if(ABMXDependency.InTargetVersionRange)
				e.AddControl(new MakerToggle(category, "Enable ABMX", cfg.enableABMX.Value, Instance))
				  .OnGUIExists((gui) =>
				  cfg.enableABMX.SettingChanged +=
				  enableABMXEvent = (s, o) =>
				  gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enableABMX.Value))
				  ?.BindToFunctionController<CharaMorpher_Controller, bool>(
					  (ctrl) => cfg.enableABMX.Value,
					  (ctrl, val) =>
					  {
						  if(val != cfg.enableABMX.Value)
							  cfg.enableABMX.Value = val;
						  ShowEnabledSliders();
					  });

			e.AddControl(new MakerToggle(category, "Chara. Enable", true, Instance))
			  .OnGUIExists((gui) =>
			  {
				  var ctrl = GetCharacterControl().GetComponentInParent<CharaMorpher_Controller>();
				  ctrl.ObserveEveryValueChanged(v => v.morphEnable).Subscribe(
					  val =>
					  {

						  gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(val);
						  Morph_Util.Logger.LogMessage($"Chara. {(val ? "Enabled" : "Disabled")}");
					  });

				  gui?.BindToFunctionController<CharaMorpher_Controller, bool>(
						(ctrl1) => ctrl1.morphEnable,
						(ctrl1, val) =>
						{
							if(val != ctrl1.morphEnable)
								ctrl1.Enable = val;
							ShowEnabledSliders();
						});
			  });

			if(ABMXDependency.InTargetVersionRange)
				e.AddControl(new MakerToggle(category, "Chara. Enable ABMX", true, Instance))
				  .OnGUIExists((gui) =>
				  {
					  var ctrl = GetCharacterControl().GetComponentInParent<CharaMorpher_Controller>();
					  ctrl.ObserveEveryValueChanged(v => v.morphEnableABMX).Subscribe(
						  val =>
						  {
							  gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(val);
							  Morph_Util.Logger.LogMessage($"Chara ABMX {(val ? "Enabled" : "Disabled")}");
						  });
					  gui?.BindToFunctionController<CharaMorpher_Controller, bool>(
							(ctrl1) => ctrl1.morphEnableABMX,
							(ctrl1, val) =>
							{
								if(val != ctrl1.morphEnableABMX)
									ctrl1.EnableABMX = val;
								ShowEnabledSliders();
							});
				  });


			e.AddControl(new MakerToggle(category, "Save Ext. Data", cfg.saveExtData.Value, Instance))
			   .OnGUIExists((gui) =>
			   cfg.saveExtData.SettingChanged +=
			   saveAsMorphDataEvent = (s, o) =>
			   gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.saveExtData.Value)
		  ).BindToFunctionController<CharaMorpher_Controller, bool>
		  ((ctrl) => cfg.saveExtData.Value,
		  (ctrl, val) => { if(val != cfg.saveExtData.Value) cfg.saveExtData.Value = val; });


			e.AddControl(new MakerToggle(category, "Link Overall Sliders to ABMX Overall Sliders", cfg.linkOverallABMXSliders.Value, Instance))
			  .OnGUIExists((gui) =>
			  cfg.linkOverallABMXSliders.SettingChanged +=
			  linkOverallSlidersEvent = (s, o) =>
			  gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.linkOverallABMXSliders.Value))
		  .BindToFunctionController<CharaMorpher_Controller, bool>
			  ((ctrl) => cfg.linkOverallABMXSliders.Value,
			  (ctrl, val) =>
			  {
				  if(!ctrl || !ctrl.isInitLoadFinished) return;
				  cfg.linkOverallABMXSliders.Value = val;
				  for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
					  ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)

				  //	ctrl.StartCoroutine(ctrl.CoResetFace(delayFrames: (int)cfg.multiUpdateEnableTest.Value + 1));//this may be necessary (it is)
				  //	ctrl.StartCoroutine(ctrl.CoResetHeight(delayFrames: (int)cfg.multiUpdateEnableTest.Value + 1));//this may be necessary (it is)
			  });

			var enableQuadManip = e.AddControl(new MakerToggle(category, "Enable Calculation Types", cfg.enableCalcTypes.Value, Instance))
				.OnGUIExists((gui) =>
					cfg.enableCalcTypes.SettingChanged +=
					enableCalcTypesEvent = (s, o) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.enableCalcTypes.Value)
				);

			e.AddControl(new MakerToggle(category, "Use Card Morph Data", cfg.preferCardMorphDataMaker.Value, Instance))
				.OnGUIExists((gui) =>
				{
					gui?.ValueChanged?.Subscribe((_1) =>
					{
						if(cfg.preferCardMorphDataMaker.Value != _1)
							cfg.preferCardMorphDataMaker.Value = _1;
					});

					Coroutine tmp = null;
					bool lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed
					cfg.preferCardMorphDataMaker.SettingChanged +=
					lastUCMDEvent = (s, o) =>
					{

						var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>()?.First();

						IEnumerator CoUCMD()
						{

							yield return ctrl.StartCoroutine(CoUCMDCommon(ctrl, lastUCMD));

							lastUCMD = cfg.preferCardMorphDataMaker.Value;//this is needed

							gui?.SetValue(cfg.preferCardMorphDataMaker.Value);

							yield break;
						}

						if(tmp != null)
							ctrl.StopCoroutine(tmp);
						tmp = ctrl.StartCoroutine(CoUCMD());
					};
				});

			e.AddControl(new MakerToggle(category, "Load Init. Character", cfg.loadInitMorphCharacter.Value, Instance))
				.OnGUIExists((gui) =>
				{
					var tgl = (MakerToggle)gui;

					tgl.BindToFunctionController<CharaMorpher_Controller, bool>(
						(ctrl) => cfg.loadInitMorphCharacter.Value,
						(ctrl, val) =>
						{
							if(cfg.loadInitMorphCharacter.Value != val)
								cfg.loadInitMorphCharacter.Value = val;
						});

					cfg.loadInitMorphCharacter.SettingChanged +=
					loadInitMorphCharacterEvent += (m, n) =>
					gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.loadInitMorphCharacter.Value);

				});

			e.AddControl(new MakerButton("Reset To Original Shape", category, Instance))
				.OnGUIExists((gui) =>
				{
					var btn = (MakerButton)gui;

					btn.OnClick.AddListener(() =>
					{
						var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().First();
						ctrl.ResetOriginalShape();
					});
				});

			e.AddControl(new MakerSeparator(category, Instance));
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

				//find section index
				if(Regex.IsMatch(settingName, searchHits[1], RegexOptions.IgnoreCase))
				{
					abmxIndex = abmxIndex >= 0 ? abmxIndex : sliders.Count;
					if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"ABMX index: {abmxIndex}");

					return null;

				}

				//add space after separator
				if(Regex.IsMatch(settingName, searchHits[0], RegexOptions.IgnoreCase))
				{
					e.AddControl(new MakerText("", category, Instance));//create space

					string part = Regex.Replace(visualName, searchHits[0],
						Regex.IsMatch(visualName, searchHits[1], RegexOptions.IgnoreCase) ? "" : "Base", RegexOptions.IgnoreCase);

					part = Regex.Replace(part, "  ", " ", RegexOptions.IgnoreCase);
					e.AddControl(new MakerText($"{part} Controls".Trim(), category, Instance));
				}


				if(Regex.Match(visualName, "other", RegexOptions.IgnoreCase).Success)
					visualName = "Other";
				else
					//remove search hits from the slider name
					foreach(var hit in searchHits)
						if(hit != searchHits[0])
							visualName = Regex.Replace(visualName, hit, "", RegexOptions.IgnoreCase);


				//setup slider
				var currSlider = sliders.AddNReturn(e.AddControl(new MorphMakerSlider(category, visualName.Trim(), min, max, (float)cfg.defaults[cfg.currentControlSetName.Value][settingName].Value.data, Instance)));
				currSlider.BindToFunctionController<CharaMorpher_Controller, float>(
						(ctrl) => ctrl.controls.all[ctrl.controls.currentSet][settingName].data,
						(ctrl, val) =>
						{
							//	Morph_Util.Logger.LogDebug($"called slider");
							if(!ctrl) return;
							if(!ctrl.isInitLoadFinished || ctrl.isReloading) return;
							if(ctrl.controls.all[ctrl.controls.currentSet][settingName].data == (float)Math.Round(val, 2)) return;

							if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"ctrl.controls.all[{ctrl.controls.currentSet}][{settingName}]");
							if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"{settingName} Value: {(float)Math.Round(val, 2)}");
							ctrl.controls.all[ctrl.controls.currentSet][settingName].
							SetData(currSlider.StoreDefault = (float)Math.Round(val, 2));

							//	Morph_Util.Logger.LogDebug($"edited slider");

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)
						});


				//mode dropdown 
				var ting = Enum.GetNames(typeof(MorphCalcType));
				var currMode = modes.AddNReturn(e.AddControl(new MorphMakerDropdown("", ting, category, (int)cfg.defaults[cfg.currentControlSetName.Value][settingName].Value.calcType, Instance)));
				currMode.BindToFunctionController<CharaMorpher_Controller, int>(
						(ctrl) => (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType,
						(ctrl, val) =>
						{
							if(!ctrl) return;
							if(!ctrl.isInitLoadFinished || ctrl.isReloading) return;
							if((int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType == val) return;

							if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"{settingName} Value: {val}");
							ctrl.controls.all[ctrl.controls.currentSet][settingName].SetCalcType((MorphCalcType)val);

							for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
								ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a));//this may be necessary (it is)

							Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
						});

				currSlider.ModSettingName = currMode.ModSettingName = settingName;

				//make sure values can be changed internally
				OnInternalSliderValueChange.AddListener(sliderValActions.AddNReturn((_) =>
				{
					if(cfg.debug.Value) Morph_Util.Logger.LogDebug("controls updating");

					CharaMorpher_Controller ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>()?.FirstOrNull(k => k != null);//first one only

					if(ctrl == null) return;

					currSlider.OnGUIExists((gui) =>
					{
						MorphMakerSlider slider = (MorphMakerSlider)gui;
						if(slider.Value != ctrl.controls.all[ctrl.controls.currentSet][settingName].data)
						//if(currSlider.ControlObject)
						{
							slider.Value = slider.StoreDefault = ctrl.controls.all[ctrl.controls.currentSet][settingName].data;
							if(cfg.debug.Value)
								Morph_Util.Logger.LogDebug($"Slider control [{slider.ModSettingName}] changed: {slider.Value}");
						}
					});

					currMode.OnGUIExists((gui) =>
					{
						MorphMakerDropdown dropdown = (MorphMakerDropdown)gui;
						if(dropdown.Value != (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType)
						//	if(currMode.ControlObject)
						{
							dropdown.Value = dropdown.StoreDefault = (int)ctrl.controls.all[ctrl.controls.currentSet][settingName].calcType;
							if(cfg.debug.Value)
								Morph_Util.Logger.LogDebug($"Calc control [{dropdown.ModSettingName}] changed: {dropdown.Value}");
						}
					});

				}));

				//add separator after overall control
				if(Regex.IsMatch(settingName, searchHits[0], RegexOptions.IgnoreCase))
					e.AddControl(new MakerSeparator(category, Instance));//create separator line

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
				CreatSlider(settingName, -cfg.sliderExtents.Value * .01f, 1 + cfg.sliderExtents.Value * .01f)?.
					OnGUIExists((gui) =>
					{
						var slid = gui.ControlObject.
							GetComponentInChildren<Slider>();

						IEnumerator CoBoodyAfterRefresh()
						{
							if(!slid.interactable) yield break;
							for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
								inst.StartCoroutine(GetCharacterControl().
								GetComponent<CharaMorpher_Controller>().CoMorphChangeUpdate(delay: a + 1));//this may be necessary (it is)

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

				IEnumerator CoVoiceAfterFullRefresh()
				{

					yield return new WaitWhile(
						() => ABMXDependency.InTargetVersionRange && GetCharacterControl().GetComponent<BoneController>().NeedsFullRefresh);
					charaCustom.PlayVoice();
				}

				CreatSlider(settingName)?.
					OnGUIExists((gui) =>
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


			e.AddControl(new MakerText("", category, Instance));//create space

			//e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			#endregion

			#region Slider Visibility

			IEnumerator CoModeDisable(bool val, uint start = 0, uint end = int.MaxValue)
			{

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"CoModeDisable Called!");
				yield return new WaitWhile(() =>
				{
					for(int a = (int)start; a < Math.Min(modes.Count, (int)end); ++a)
						if(modes?[a]?.ControlObject == null) return true;
					return false;
				});

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Modes are visible: {val}");
				for(int a = (int)start; a < modes.Count; ++a)
					if((bool)sliders?[a]?.ControlObject?.activeSelf)
					{
						if(!val) { modes[a].StoreDefault = modes[a].Value; modes[a].Value = 0; }
						else modes[a].ApplyStoredSetting();

						modes[a].ControlObject?.SetActive(val);
					}
			}

			IEnumerator CoSliderDisable(bool val, uint start = 0, uint end = int.MaxValue)
			{

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"CoSliderDisable Called!");
				yield return new WaitWhile(() =>
				{
					for(int a = (int)start; a < Math.Min(sliders.Count, (int)end); ++a)
						if(sliders?[a]?.ControlObject == null) return true;
					return false;
				});

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"sliders are visible: {val}");
				for(int a = (int)start; a < sliders.Count; ++a)
				{
					sliders?[a]?.ControlObject?.SetActive(val);
					modes?[a]?.ControlObject?.SetActive(val);
				}

				inst.StartCoroutine(CoModeDisable(cfg.enableCalcTypes.Value));
			}


			void ShowEnabledSliders()
			{
				inst.StartCoroutine(CoSliderDisable(cfg.enable.Value, start: 0));
				inst.StartCoroutine(CoSliderDisable(cfg.enable.Value && cfg.enableABMX.Value && ABMXDependency.InTargetVersionRange,
					start: (uint)abmxIndex, end: int.MaxValue));
			}



			enableQuadManip?.BindToFunctionController<CharaMorpher_Controller, bool>(
				(ctrl) => cfg.enableCalcTypes.Value,
				(ctrl, val) =>
				{
					if(val != cfg.enableCalcTypes.Value)
						cfg.enableCalcTypes.Value = val;
					ShowEnabledSliders();
				});

			ShowEnabledSliders();
			#endregion

			#region Save/Load Buttons

			IEnumerator ChangeGUILayout(BaseGuiEntry gui)
			{
				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("moving object");

				yield return new WaitWhile(() => gui?.ControlObject?.GetComponentInParent<ScrollRect>()?.transform == null);

				var par = gui.ControlObject.GetComponentInParent<ScrollRect>()?.transform;


				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Parent: " + par);

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


				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("setting as last");
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

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Adding buttons");

			var sep = e.AddControl(new MakerSeparator(category, Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui)));

			select = e.AddControl(
				new MorphMakerDropdown("Selected Slot", ControlsList, category, 0, Instance))
				.OnGUIExists(
				(gui) =>
				{
					MorphMakerDropdown mmd = (MorphMakerDropdown)gui;

					Instance.StartCoroutine(ChangeGUILayout(gui));
					mmd.ValueChanged?.Subscribe((val) =>
					{
						mmd.Value = SwitchControlSet(mmd.Options, val);
						Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
					});
					mmd.Value = SwitchControlSet(mmd.Options, cfg.currentControlSetName.Value);
				});


			e.AddControl(new MakerButton("Add New Slot", category, Instance))
				   .OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui))).
				   OnClick.AddListener(() =>
				   {
					   AddNewSetting();

					   //update dropdown list				 
					   UpdateGUISelectList();
				   });


			e.AddControl(new MakerButton("Remove Current Slot", category, Instance))
				.OnGUIExists((gui) => Instance.StartCoroutine(ChangeGUILayout(gui))).
				OnClick.AddListener(() =>
				{

					//switch control before deletion
					int tmp = select.Value;
					if(select.Value >= ControlsList.Length - 1)
						tmp = ControlsList.Length - 2;

					RemoveCurrentSetting(null);

					UpdateGUISelectList();
					select.Value = SwitchControlSet(select.Options, tmp);
				});


			e.AddControl(new MakerButton("Save To Slot", category, Instance))
			   .OnGUIExists((gui) => { Instance.StartCoroutine(ChangeGUILayout(gui)); }).
			   OnClick.AddListener(() =>
			   {
				   foreach(var slider in sliders)
					   slider.ApplyDefault();
				   foreach(var mode in modes)
					   mode.ApplyStoredSetting();

				   var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().First();
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

				   ctrl.SoftSaveControls(cfg.preferCardMorphDataMaker.Value, defaultSave: false);
				   Morph_Util.Logger.LogMessage($"Saved as CharaMorpher {ctrl.controls.currentSet}");

				   Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);
			   });


			e.AddControl(new MakerButton("Load From Slot", category, Instance))
			   .OnGUIExists((gui) =>
			   {
				   Instance.StartCoroutine(ChangeGUILayout(gui));
				   loadDefaultValues = (bool showMessage, bool playSound, bool runUpdate) =>
				   {
					   var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().First();

					   var data = (!ctrl.IsUsingExtMorphData ? ctrl.ctrls1 : (ctrl.ctrls2 ?? ctrl.ctrls1))?.Clone()?.all;
					   var listCtrls = ctrl?.controls;
					   var list = listCtrls?.all;
					   var name = cfg.currentControlSetName.Value;

					   if(list?.ContainsKey(name) ?? false)
						   foreach(var def2 in list[name].Keys.ToList())
						   {
							   if(cfg.debug.Value)
							   {
								   Morph_Util.Logger.LogDebug($"Data Expected: data[{name}][{def2}]");
								   Morph_Util.Logger.LogDebug($"Data Key1:\n data[{string.Join(",\n ", data?.Keys.ToArray())}]");
								   Morph_Util.Logger.LogDebug($"Data Key2:\n data[{string.Join(",\n ", data?[data.Keys.ElementAt(0)].Keys.ToArray())}]");
							   }

							   var val = data[name][def2].data;
							   var cal = data[name][def2].calcType;

							   list[name][def2] = data[name][def2].Clone();
						   }

					   OnInternalSliderValueChange.Invoke(listCtrls);

					   foreach(var slider in sliders)
						   slider.ApplyDefault();
					   foreach(var mode in modes)
						   mode.ApplyStoredSetting();

					   if(runUpdate)
						   for(int b = -1; b < cfg.multiUpdateEnableTest.Value;)
							   ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: ++b));//this may be necessary 

					   if(showMessage)
						   Morph_Util.Logger.LogMessage($"Loaded CharaMorpher: {name}");

					   if(playSound)
						   Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_l);
				   };
			   }).
			   OnClick.AddListener(() =>
			   {
				   loadDefaultValues?.Invoke(true, true, true);
			   });


			if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Finished adding buttons");
			//e.AddControl(new MakerSeparator(category, CharaMorpher_Core.Instance));
			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			#endregion

		}

		private static void EnableABMX_SettingChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private static void ImageControls(RegisterCustomControlsEvent e, BaseUnityPlugin owner)
		{
			e.AddControl(new MakerText("Morph Target", category, Instance));


			var img = e.AddControl(new MakerImage(null, category, owner)
			{ Height = 200, Width = 150, Texture = Morph_Util.CreateTexture(TargetPath), });
			IEnumerator CoSetTexture(string path, byte[] png = null)
			{
				for(int a = 0; a < 4; ++a)
					yield return null;
				yield return new WaitUntil(() => img.Exists);

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"The CoSetTexture was called");
				img.Texture = path?.CreateTexture(png);
				//(img.Texture as Texture2D).Resize(150, 200);
				img.ControlObject.GetComponentInChildren<RawImage>().color = Color.white * ((!png.IsNullOrEmpty()) ? .65f : 1);
			}

			OnNewTargetImage.AddListener(
				(path, png) =>
				{
					if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Calling OnNewTargetImage callback");
					Instance.StartCoroutine(CoSetTexture(path, png));
				});

			var button = e.AddControl(new MakerButton($"Set New Morph Target", category, owner));
			button.OnClick.AddListener(() =>
			{
				ForeGrounder.SetCurrentForground();

				GetNewImageTarget();
			});

			e.AddControl(new MakerSeparator(category, Instance));
		}

		private static void ButtonDefaults(RegisterCustomControlsEvent e, BaseUnityPlugin owner)
		{

			//e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
			e.AddControl(new MakerText("Easy Morph Buttons", category, Instance));

			((MakerToggle)e.AddControl(new MakerToggle(category, "Control overall sliders with Morph Buttons", cfg.easyMorphBtnOverallSet.Value, owner))
				.OnGUIExists((gui) =>
				cfg.easyMorphBtnOverallSet.SettingChanged += easyMorphOverallEvent += (s, o) =>
				gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.easyMorphBtnOverallSet.Value))
			).BindToFunctionController<CharaMorpher_Controller, bool>(
				(ctrl) => cfg.easyMorphBtnOverallSet.Value,
				(ctrl, val) => cfg.easyMorphBtnOverallSet.Value = val
			);

			((MakerToggle)e.AddControl(new MakerToggle(category, "Other values default to 100%", cfg.easyMorphBtnEnableDefaulting.Value, owner))
				.OnGUIExists((gui) =>
				cfg.easyMorphBtnEnableDefaulting.SettingChanged += easyMorphDefaultingEvent += (s, o) =>
				gui?.ControlObject?.GetComponentInChildren<Toggle>()?.Set(cfg.easyMorphBtnEnableDefaulting.Value))
			).BindToFunctionController<CharaMorpher_Controller, bool>(
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

					foreach(CharaMorpher_Controller ctrl in GetFuncCtrlOfType<CharaMorpher_Controller>())
					{
						//	ctrl.StopAllCoroutines();

						//	Morph_Util.Logger.LogDebug($"Mod Category:{ctrl.controls.currentSet}");
						if(reset)
							for(int a = 0; a < ctrl.controls.all[ctrl.controls.currentSet].Count; ++a)
							{
								//Morph_Util.Logger.LogDebug($"Mod name:{ctrl.controls.all[ctrl.controls.currentSet].Keys.ElementAt(a)}");
								//	var cal = ctrl.controls.all[ctrl.controls.currentSet][ctrl.controls.all.Keys.ElementAt(a)].calcType;
								ctrl.controls.all[ctrl.controls.currentSet][ctrl.controls.all[ctrl.controls.currentSet].Keys.ElementAt(a)].SetData(1f);
							}

						var tmp = swap ? ctrl.controls.overall : ctrl.controls.notOverall;
						for(int a = 0; a < tmp.Count(); ++a)
						{
							//var cal = ctrl.controls.all[ctrl.controls.currentSet][tmp.ElementAt(a).Key].calcType;
							ctrl.controls.all[ctrl.controls.currentSet][tmp.ElementAt(a).Key].SetData(percent * .01f);
						}

						for(int a = -1; a < cfg.multiUpdateEnableTest.Value;)
							ctrl.StartCoroutine(ctrl.CoMorphChangeUpdate(++a));


						Morph_Util.Logger.LogMessage($"Morphed to {percent}%");
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
			e.AddControl(new MakerSeparator(category, Instance));
			//	e.AddControl(new MakerText("", category, CharaMorpher_Core.Instance));//create space
		}

		public static void UpdateGUISelectList()
		{
			if(!InsideMaker && !InsideStudio) return;
			if(select == null) return;

			var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().FirstOrNull();

			select.Options = ControlsList;
			select.Value = SwitchControlSet(select.Options, ctrl?.controls.currentSet);
		}

		public static void LoadCurrentDefaultValues(bool showMessage = true, bool playSound = true, bool runUpdate = true) => loadDefaultValues?.Invoke(showMessage, playSound, runUpdate);

		#region Other
		static IEnumerator CoUCMDCommon(CharaMorpher_Controller ctrl, bool lastUCMD)
		{

			string name =
			(!cfg.preferCardMorphDataMaker.Value ?
			ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
			name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDiv), 0, name.Length));


			{
				//	Morph_Util.Logger.LogDebug($"lastUCMD: {lastUCMD}");
				yield return new WaitWhile(() => ctrl.isReloading);

				var tmpCtrls =
				!cfg.preferCardMorphDataMaker.Value ?
				ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1);
				tmpCtrls.currentSet = ctrl.controls.currentSet;

				ctrl.controls.Copy(!lastUCMD ? ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

				ctrl.SoftSaveControls(lastUCMD);
				ctrl.controls.Copy(!cfg.preferCardMorphDataMaker.Value ?
				ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));


				//Morph_Util.Logger.LogDebug($"Next lastUCMD: {lastUCMD}");
			}

			if(!name.IsNullOrEmpty())
				SwitchControlSet(ControlsList, name);

			UpdateGUISelectList();
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

		//private static string MakeDirPath(string path) => Morph_Util.MakeDirPath(path);

		/// <summary>
		/// Called after a file is chosen in file explorer menu  
		/// </summary>
		/// <param name="strings: ">the info returned from file explorer. strings[0] returns the full file path</param>
		private static void OnImageTargetObtained(string[] strings)
		{

			ForeGrounder.RevertForground();
			if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Enters accept");
			if(strings == null || strings.Length == 0) return;
			var texPath = strings[0].MakeDirPath();

			if(cfg.debug.Value)
			{
				Morph_Util.Logger.LogDebug($"Original path: {texPath}");
				Morph_Util.Logger.LogDebug($"texture path: {Path.Combine(Path.GetDirectoryName(texPath), Path.GetFileName(texPath))}");
			}

			if(string.IsNullOrEmpty(texPath)) return;

			cfg.charDir.Value = Path.GetDirectoryName(texPath).MakeDirPath();
			cfg.imageName.Value = texPath.Substring(texPath.LastIndexOf('/') + 1).MakeDirPath();//not sure why this happens on hs2?

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Exit accept");
		}

		public static void GetNewImageTarget()
		{
			var paths = OpenFileDialog.ShowDialog("Set Morph Target",
				TargetDirectory.MakeDirPath("/", "\\"),
				FileFilter,
				FileExt,
				OpenFileDialog.SingleFileFlags);

			OnImageTargetObtained(paths);

			Illusion.Game.Utils.Sound.Play(paths?.Any() ?? false ? Illusion.Game.SystemSE.ok_l : Illusion.Game.SystemSE.cancel);
		}

		#endregion

		#region MakerObjects
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
					UnityEngine.Component dropdown = ControlObject?.GetComponentInChildren<TMP_Dropdown>();
					if(!dropdown) dropdown = ControlObject?.GetComponentInChildren<Dropdown>();
					if(!dropdown) return;

					if(dropdown is TMP_Dropdown dropdown1)
						dropdown1.options = value.Attempt((k) =>
						new TMP_Dropdown.OptionData(k.LastIndexOf(strDiv) >= 0 ? k.Substring(0, k.LastIndexOf(strDiv)) : k)).ToList();

					else if(dropdown is Dropdown dropdown2)
						dropdown2.options = value.Attempt((k) =>
						new Dropdown.OptionData(k.LastIndexOf(strDiv) >= 0 ? k.Substring(0, k.LastIndexOf(strDiv)) : k)).ToList();
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
			public UnityEngine.Component GetDropdown()
			{
				UnityEngine.Component dropdown = ControlObject?.GetComponentInChildren<TMP_Dropdown>();
				if(!dropdown) dropdown = ControlObject?.GetComponentInChildren<Dropdown>();

				return dropdown;
			}
		}

		public class MorphMakerButton
		{

		}
		#endregion
	}
}
