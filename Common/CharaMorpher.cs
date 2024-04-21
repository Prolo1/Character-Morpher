using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
//using BepInEx.Preloader.Patching;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Utilities;
using KKAPI.Maker.UI;
using KKABMX.Core;
using ExtensibleSaveFormat;
//using HarmonyLib;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UniRx;



#if HONEY_API

using AIChara;
#endif

using static Character_Morpher.CharaMorpher_Core;
//using static Character_Morpher.CharaMorpher_Controller;
using static Character_Morpher.CharaMorpher_GUI;
using static Character_Morpher.Morph_Util;
using static BepInEx.Logging.LogLevel;

/***********************************************
  Features:

 * Morph body features
 * Morph face features     
 * Morph ABMX body features
 * Morph ABMX face features
 * Easy Morph Buttons
 * Save morph changes to card (w/o changing card parameters)
 * Added QoL file explorer search for morph target in maker
 * Can choose to enable/disable in-game use (this affects all but male character[s])
 * Can choose to enable/disable use in male maker
 * Works in studio (Not all features there yet)
 * Added per-character enablers that get saved with the character (persistent in game)
 
  Planned:    
 * ¯\_(ツ)_/¯
 * 
************************************************/

/**
 */
namespace Character_Morpher
{
	#region Dependencies
	[
	// Tell BepInEx that we need KKAPI to run, and that we need the latest version of it.
	// Check documentation of KoikatuAPI.VersionConst for more info.
	BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst),
	// Tell BepInEx that we need ExtendedSave to run, and that we need the latest version of it.
	// Check documentation of KoikatuAPI.VersionConst for more info.
	BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID, ExtendedSave.Version),
	// Tell BepInEx that we need KKABMX to run, and that we need the latest version of it.
	// Check documentation of KoikatuAPI.VersionConst for more info.
	BepInDependency(KKABMX.Core.KKABMX_Core.GUID, BepInDependency.DependencyFlags.SoftDependency),
	]
	#endregion
	// Specify this as a plugin that gets loaded by BepInEx
	[BepInPlugin(GUID, ModName, Version)]
	public partial class CharaMorpher_Core : BaseUnityPlugin
	{
		#region variables

		// Expose both your GUID and current version to allow other plugins to easily check for your presence and version, for example by using the BepInDependency attribute.
		// Be careful with public const fields! Read more: https://stackoverflow.com/questions/55984
		// Avoid changing GUID unless absolutely necessary. Plugins that rely on your plugin will no longer recognize it, and if you use it in function controllers you will lose all data saved to cards before the change!
		public const string ModName = "Character Morpher";
		public const string GUID = "prolo.chararmorpher";//never change this
		public const string Version = "1.2.1";

		public const string strDiv = ":";
		public const string defaultStr = "(Default)" + strDiv;


		internal static CharaMorpher_Core Instance;
		internal static new ManualLogSource Logger;
		internal static OnNewImage OnNewTargetImage = new OnNewImage();
		internal static OnValueChange<MorphControls> OnInternalSliderValueChange = new OnValueChange<MorphControls>();
		internal static OnControlSetValueChange OnInternalControlListChanged = new OnControlSetValueChange();

		internal static DependencyInfo<KKABMX_Core> ABMXDependency;

		internal static Texture2D UIGoku = null;
		internal static Texture2D iconBG = null;

		public Dictionary<string, List<MorphSliderData>> controlCategories = new Dictionary<string, List<MorphSliderData>>();
		public static MorphConfig cfg;

		public struct MorphConfig
		{
			//ABMX
			public ConfigEntry<bool> enableABMX { set; get; }


			//Main
			public ConfigEntry<bool> enable { set; get; }
			public ConfigEntry<KeyboardShortcut> enableKey { set; get; }
			public ConfigEntry<KeyboardShortcut> enableCharKey { set; get; }
			public ConfigEntry<KeyboardShortcut> prevControlKey { set; get; }
			public ConfigEntry<KeyboardShortcut> nextControlKey { set; get; }
			public ConfigEntry<bool> enableInMaleMaker { get; set; }
			public ConfigEntry<bool> enableInGame { set; get; }
			public ConfigEntry<bool> linkOverallABMXSliders { set; get; }
			public ConfigEntry<bool> enableCalcTypes { set; get; }
			public ConfigEntry<bool> saveExtData { set; get; }
			public ConfigEntry<bool> preferCardMorphDataMaker { set; get; }
			public ConfigEntry<bool> preferCardMorphDataGame { set; get; }
			public ConfigEntry<bool> loadInitMorphCharacter { set; get; }
			public ConfigEntry<bool> onlyMorphCharWithDataInGame { set; get; }
			public ConfigEntry<string> resetToOrigShapeBtn { set; get; }


			public ConfigEntry<string> pathBtn { set; get; }
			public ConfigEntry<string> charDir { set; get; }
			public ConfigEntry<string> imageName { set; get; }
			public ConfigEntry<uint> sliderExtents { set; get; }
			public ConfigEntry<string> currentControlSetName { set; get; }
			public ConfigEntry<string> controlSets { set; get; }

			//Studio
			public ConfigEntry<Rect> studioWinRec { set; get; }
			//public ConfigEntry<bool> studioOneAtATime { set; get; }



			//Advanced
			public Dictionary<string, Dictionary<string, ConfigEntry<MorphSliderData>>> defaults { set; get; }

			//Advanced (show up below main) 
			public ConfigEntry<bool> debug { set; get; }
			public ConfigEntry<bool> resetOnLaunch { set; get; }
			public ConfigEntry<bool> hideAdvIndexes { set; get; }
			public ConfigEntry<bool> easyMorphBtnOverallSet { set; get; }
			public ConfigEntry<bool> easyMorphBtnEnableDefaulting { set; get; }
			public ConfigEntry<bool> oldControlsConversion { set; get; }
			public ConfigEntry<float> makerViewportUISpace { get; set; }


			//tests

			public ConfigEntry<int> unknownTest { internal set; get; }
			//	public ConfigEntry<float> initialMorphTest { internal set; get; }
			public ConfigEntry<float> initialMorphFaceTest { get; internal set; }
			public ConfigEntry<float> initialMorphBodyTest { get; internal set; }
			public ConfigEntry<float> initalBoobTest { internal set; get; }
			public ConfigEntry<float> initalFaceTest { internal set; get; }
			public ConfigEntry<uint> reloadTest { internal set; get; }
			//public ConfigEntry<uint> multiUpdateTest { internal set; get; }
			public ConfigEntry<uint> multiUpdateEnableTest { get; internal set; }
			public ConfigEntry<uint> multiUpdateSliderTest { get; internal set; }
			//public ConfigEntry<uint> fullBoneResetTest { internal set; get; }

			//indexes 
			public List<ConfigEntry<int>> headIndex { set; get; }
			public List<ConfigEntry<int>> earIndex { set; get; }
			public List<ConfigEntry<int>> eyeIndex { set; get; }
			public List<ConfigEntry<int>> mouthIndex { set; get; }
			public List<ConfigEntry<int>> brestIndex { set; get; }
			public List<ConfigEntry<int>> torsoIndex { set; get; }
			public List<ConfigEntry<int>> armIndex { set; get; }
			public List<ConfigEntry<int>> buttIndex { set; get; }
			public List<ConfigEntry<int>> legIndex { set; get; }
			public List<ConfigEntry<int>> noseIndex { set; get; }
		}
		#endregion

		void Awake()
		{
			Instance = this;
			Logger = base.Logger;
			ForeGrounder.SetCurrentForground();


			//Soft dependency variables
			{
				ABMXDependency = new DependencyInfo<KKABMX_Core>(new Version(KKABMX_Core.Version));

				if(!ABMXDependency.InTargetVersionRange)
					Logger.Log(Warning | Message, $"Some [{ModName}] functionality may be locked due to the " +
						$"absence of [{nameof(KKABMX_Core)}] " +
						$"or the use of an incorrect version\n" +
						$"{ABMXDependency}");
			}

			//Embedded Resources
			using(MemoryStream memStream = new MemoryStream())
			{
				/**This stuff will be used later*/
				var assembly = Assembly.GetExecutingAssembly();
				var resources = assembly.GetManifestResourceNames();
				Logger.LogDebug($"\nResources:\n[{string.Join(", ", resources)}]");


				var data = assembly.GetManifestResourceStream(resources.FirstOrDefault((txt) => (txt.ToLower()).Contains("ultra instinct")) ?? " ");
#if KK
				memStream.SetLength(0);//Clear Buffer 
				memStream.Write(data.ReadAllBytes(), 0, (int)data.Length);//write Buffer
#else
				memStream.SetLength(0);//Clear Buffer 
				data?.CopyTo(memStream);
#endif
				UIGoku =
					memStream?.GetBuffer()?
					.LoadTexture();
				memStream.SetLength(0);
				UIGoku.Compress(false);
				UIGoku.Apply();

				data = assembly.GetManifestResourceStream(resources.FirstOrDefault((txt) => (txt.ToLower()).Contains("studio morph icon.png")) ?? " ");
#if KK
				memStream.SetLength(0);//Clear Buffer 
				memStream.Write(data.ReadAllBytes(), 0, (int)data.Length);//write Buffer
#else
				memStream.SetLength(0);//Clear Buffer 
				data?.CopyTo(memStream);
#endif
				iconBG =
					memStream?.GetBuffer()?
					.LoadTexture();
				memStream.SetLength(0);
				iconBG.Compress(false);
				iconBG.Apply();

				//data = assembly.GetManifestResourceStream(resources.FirstOrDefault((txt) => (txt.ToLower()).Contains("ultra instinct")) ?? " ");
				//data?.CopyTo(memStream);
				//iconBG =
				//	memStream?.GetBuffer()?
				//	.LoadTexture();
				//memStreme.SetLength(0);

				//data = assembly.GetManifestResourceStream(resources.FirstOrDefault((txt) => txt.ToLower().Contains("icon.png")));
				//data.CopyTo(memStream);
				//icon =
				//	memStream?.GetBuffer()?
				//	.LoadTexture();
				//memStreme.SetLength(0);
				//icon.Compress(false);
				//icon.Apply();

			}

			//Type Converters
			{
				TomlTypeConverter.AddConverter(
				   typeof(Rect),
				   new TypeConverter()
				   {
					   ConvertToString = (o, t) =>
					   {
						   var rec = (Rect)o;

						   return string.Format("{0:f0}:{1:f0}:{2:f0}:{3:f0}", rec.x, rec.y, rec.width, rec.height);
					   },
					   ConvertToObject = (s, t) =>
					   {

						   var values = s.Split(':');

						   return new Rect(
							   float.Parse(values[0]),
							   float.Parse(values[1]),
							   float.Parse(values[2]),
							   float.Parse(values[3]));
					   },
				   });
			}


			string femalepath = Morph_Util.MakeDirPath
				(Path.Combine(Paths.GameRootPath, "UserData/chara/female/"));

			int bodyBoneAmount = ChaFileDefine.cf_bodyshapename.Length - 1;
			int faceBoneAmount = ChaFileDefine.cf_headshapename.Length - 1;
			//Logger.LogDebug($"Body bones amount: {bodyBoneAmount+1}");
			//Logger.LogDebug($"Face bones amount: {faceBoneAmount+1}");


			int index = 0, secIndex = 0, secIndex2 = 99;//easier to input index order values


			string main =
			"__Main__";
			string mainx =
			$"{secIndex++:d2}. " + "Main";

			string stud = "_Studio_";
			string studx =
			$"{secIndex++:d2}. " + "Studio";

			string tst = "_Testing_";
			string tstx =
			$"{secIndex2--:d2}. " + "Testing";
			string adv = "_Advanced_";
			string advx =
			$"{secIndex2--:d2}. " + "Advanced";

			var saveCfgAuto =
				Instance.Config.SaveOnConfigSet;
			Instance.Config.SaveOnConfigSet = false;

			cfg = new MorphConfig
			{
				//Main
				enable = Config.Bind(main, "Enable", false,
				new ConfigDescription("Allows the plugin to run (may need to reload character/scene if results are not changing)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),

				enableABMX = Config.Bind(main, "Enable ABMX", true,
				new ConfigDescription("Allows ABMX to be affected", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				enableInMaleMaker = Config.Bind(main, "Enable in Male Maker", true,
				new ConfigDescription("Allows the plugin to run while in male maker (enable before launching maker)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				enableInGame = Config.Bind(main, "Enable in Game", true,
				new ConfigDescription("Allows the plugin to run while in main game", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				linkOverallABMXSliders = Config.Bind(main, "Link Overall Base Sliders to Overall ABMX Sliders", true,
				new ConfigDescription("Allows ABMX overall sliders to be affected by their base counterpart (i.e. Body:50% * ABMXBody:100% = ABMXBody:50%)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				enableCalcTypes = Config.Bind(main, "Enable Calculation Types", false,
				new ConfigDescription("Enables quadratic mode where value gets squared (i.e. 1.2 = 1.2^2 = 1.44)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				saveExtData = Config.Bind(main, "Save Ext. Data", true,
				new ConfigDescription("Allows the card to save using ext. data. " +
				"If true, card is saved as seen with Morph Ext. data added to the card (card will look the same for those who don't have the mod), " +
				"else the card is saved normally w/o Morph Ext. data and saved as seen (must be set before saving)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				preferCardMorphDataMaker = Config.Bind(main, "Use Card Morph Data (Maker)", true,
				new ConfigDescription("Allows the mod to use data from card instead of default data " +
				"(If false card uses default Morph card data \nNote: the image will go dark if using card data", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, })),
				preferCardMorphDataGame = Config.Bind(main, "Use Card Morph Data (Game)", true,
				new ConfigDescription("Allows the mod to use data from card instead of default data " +
				"(If false card uses default Morph card data) \nNote: the image will go dark if using card data", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),
				loadInitMorphCharacter = Config.Bind(main, "Load Init. Character", true,
				new ConfigDescription("If the character had extra work done to it before it was saved, " +
				"when loaded you will see those changes", null,
				new ConfigurationManagerAttributes() { Order = --index, Category = mainx, })),
				onlyMorphCharWithDataInGame = Config.Bind(main, "Only Morph Characters With Save Data (Game)", false,
				new ConfigDescription("Only allows cards that have morph data saved to it to be changed in game " +
				"(If true all cards not saved with CharaMorph Data will not morph AT ALL!)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),

				charDir = Config.Bind(main, "Directory Path", femalepath,
				new ConfigDescription("Directory where character is stored", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, DefaultValue = true, Browsable = true })),
				imageName = Config.Bind(main, "Card Name", "sample.png",
				new ConfigDescription("The character card used to morph", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, DefaultValue = true, Browsable = true })),
				sliderExtents = Config.Bind(main, "Slider Extents", 200u,
				new ConfigDescription("How far the slider values go above default " +
				"(e.i. setting value to 10 gives values -10 -> 110)", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, DefaultValue = true })),
				enableKey = Config.Bind(main, "Toggle Enable Keybinding", new KeyboardShortcut(KeyCode.Return, KeyCode.RightShift),
				new ConfigDescription("Enable/Disable toggle button", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),
				enableCharKey = Config.Bind(main, "Toggle Chara. Enable Keybinding", new KeyboardShortcut(KeyCode.Return, KeyCode.RightControl, KeyCode.RightShift),
				new ConfigDescription("Enable/Disable toggle button", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),
				prevControlKey = Config.Bind(main, "Prev. control Keybinding", new KeyboardShortcut(),
				new ConfigDescription("Switch to the prev. control set", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),
				nextControlKey = Config.Bind(main, "Next control Keybinding", new KeyboardShortcut(),
				new ConfigDescription("Switch to the next control set", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),

				resetToOrigShapeBtn = Config.Bind(main, "Reset To Orig. Shape", "",
				new ConfigDescription("Resets cards to the state they were in when it was loaded (Only works if mod is enabled)", null,
				new ConfigurationManagerAttributes
				{
					Order = --index,
					Category = mainx,
					HideDefaultButton = true,
					HideSettingName = true,
					CustomDrawer = Morph_Util.ButtonDrawer(onClick: () =>
					{
						var ctrls = GetFuncCtrlOfType<CharaMorpher_Controller>();

						foreach(var ctrl in ctrls)
							ctrl.ResetOriginalShape();
					}),
					ObjToStr = (o) => "",
					StrToObj = (s) => null
				})),
				pathBtn = Config.Bind(main, "Set Default Morph Target", "",
				new ConfigDescription("", null,
				new ConfigurationManagerAttributes
				{
					Order = --index,
					Category = mainx,
					HideDefaultButton = true,
					CustomDrawer = Morph_Util.MyImageButtonDrawer,
					ObjToStr = (o) => "",
					StrToObj = (s) => null
				})),
				currentControlSetName = Config.Bind(main, "Current Control Name", defaultStr,
				new ConfigDescription("", tags:
				new ConfigurationManagerAttributes { Order = --index, Category = mainx, Browsable = false, })),
				controlSets = Config.Bind(main, "Control Sets", "",
				new ConfigDescription("", tags:
				new ConfigurationManagerAttributes
				{
					Order = --index,
					Category = mainx,
					HideDefaultButton = true,
					CustomDrawer = Morph_Util.MySelectionListDrawer,
					ObjToStr = (o) => "",
					StrToObj = (s) => null
				})),

				//you don't need to see this in game
				defaults = new Dictionary<string, Dictionary<string, ConfigEntry<MorphSliderData>>>(),
				//defaultModes = new Dictionary<string, Dictionary<string, ConfigEntry<Tuple<string, int>>>>(),


				//Studio
				studioWinRec = Config.Bind(stud, "Studio Win Rec", CharaMorpher_GUI.winRec,
				new ConfigDescription("", tags:
				new ConfigurationManagerAttributes()
				{
					Order = --index,
					Category = studx,
					CustomDrawer = (draw) =>
					{

						Rect tmp = new Rect((Rect)draw.BoxedValue);
						GUILayout.BeginHorizontal();

						GUILayout.Label("X", GUILayout.ExpandWidth(false));
						//tmp.x = GUILayout.HorizontalSlider(tmp.x, 0, Screen.width, GUILayout.ExpandWidth(true));
						float.TryParse(GUILayout.TextField(string.Format("{0:f0}", tmp.x)), out tmp.m_XMin);

						GUILayout.Label("Y", GUILayout.ExpandWidth(false));
						//tmp.y = GUILayout.HorizontalSlider(tmp.y, 0, Screen.height, GUILayout.ExpandWidth(true));
						float.TryParse(GUILayout.TextField(string.Format("{0:f0}", tmp.y)), out tmp.m_YMin);

						GUILayout.Label("Width", GUILayout.ExpandWidth(false));
						//tmp.width = GUILayout.HorizontalSlider(tmp.width, 0, Screen.width, GUILayout.ExpandWidth(true));
						float.TryParse(GUILayout.TextField(string.Format("{0:f0}", tmp.width)), out tmp.m_Width);

						GUILayout.Label("Height", GUILayout.ExpandWidth(false));
						//tmp.height = GUILayout.HorizontalSlider(tmp.height, 0, Screen.height, GUILayout.ExpandWidth(true));
						float.TryParse(GUILayout.TextField(string.Format("{0:f0}", tmp.height)), out tmp.m_Height);


						GUILayout.EndHorizontal();

						if(cfg.studioWinRec.Value != tmp)
							cfg.studioWinRec.Value = tmp;
					},
					Browsable = StudioAPI.InsideStudio
				})),
				//studioOneAtATime = Config.Bind(stud, "studio One At A Time", true,
				//new ConfigDescription("", tags:
				//new ConfigurationManagerAttributes() { Order = --index, Category = studx, Browsable = StudioAPI.InsideStudio })),

				//Advanced
				resetOnLaunch = Config.Bind(adv, "Reset On Launch", true,
				new ConfigDescription("Will reset all advanced values to defaults after next launch", null,
				new ConfigurationManagerAttributes { Order = --index, Category = advx, IsAdvanced = true })),
				debug = Config.Bind(adv, "Debug Logging", false,
				new ConfigDescription("Allows debug logs to be written to the log file", null,
				new ConfigurationManagerAttributes { Order = --index, Category = advx, IsAdvanced = true })),
				hideAdvIndexes = Config.Bind(adv, "Hide Index Settings", true,
				new ConfigDescription("Will hide the index settings below these ones", null,
				new ConfigurationManagerAttributes { Order = --index, Category = advx, IsAdvanced = true })),

				easyMorphBtnOverallSet = Config.Bind(adv, "Enable Easy Morph Button Overall Set", true,
				new ConfigDescription("Sets the overall sliders whenever an Easy Morph button is pressed, everything else otherwise", null,
				new ConfigurationManagerAttributes { Order = --index, Category = advx, Browsable = false, IsAdvanced = true })),
				easyMorphBtnEnableDefaulting = Config.Bind(adv, "Enable Easy Morph Defaulting", true,
				new ConfigDescription("Defaults everything not set by Easy Morph button to 100%", null,
				new ConfigurationManagerAttributes { Order = --index, Category = advx, Browsable = false, IsAdvanced = true })),
				oldControlsConversion = Config.Bind(adv, "Convert Old Data (On Next Startup)", true,
				new ConfigDescription("This will attempt to convert old data (V1 and below) to the new current format. " +
				"This should happen automatically the first time but can be done again if need be", null,
				new ConfigurationManagerAttributes { Order = --index, Category = advx, IsAdvanced = true })),
			};

			bool testing = true;
			//Advanced
			{

				cfg.debug.ConfigDefaulter();

				cfg.makerViewportUISpace = Config.Bind(adv, "Viewport UI Space", .43f,
					new ConfigDescription("Increase / decrease the Fashion Line viewport size ",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes()
					{
						Order = index--,
						ShowRangeAsPercent = false,
						IsAdvanced = true,
						Category = advx
					})).ConfigDefaulter();

				//Tests
				cfg.unknownTest = Config.Bind(tst, "Unknown Test value", 20,
					new ConfigDescription("Used for whatever the hell I WANT (if you see this I forgot to take it out). RESETS ON GAME LAUNCH", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//	cfg.initialMorphTest = Config.Bind(tst, "Init morph value", 1.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.multiUpdateEnableTest = Config.Bind(tst, "Multi Update Enable value", 5u,
					new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.multiUpdateSliderTest = Config.Bind(tst, "Multi Update Slider value", 0u,
					new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();


#if KOI_API
				//cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.initialMorphFaceTest = Config.Bind(tst, "Init morph Face value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.initialMorphBodyTest = Config.Bind(tst, "Init morph Body value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.reloadTest = Config.Bind(tst, "Reload delay value", 22u,
					new ConfigDescription("Used to change the amount of frames to delay before loading. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
#elif HONEY_API
				cfg.initialMorphFaceTest = Config.Bind(tst, "Init morph Face value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.initialMorphBodyTest = Config.Bind(tst, "Init morph Body value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//cfg.multiUpdateEnableTest = Config.Bind("_Testing_", "Multi Update Enable value", 5u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//cfg.multiUpdateSliderTest = Config.Bind("_Testing_", "Multi Update Slider value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.reloadTest = Config.Bind(tst, "Reload delay value", 22u,
					new ConfigDescription("Used to change the amount of frames to delay before loading. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
#endif
				//	cfg.fullBoneResetTest = Config.Bind("_Testing_", "Full Bone Reset Delay", 3u, new ConfigDescription("Used to determine how long to wait for full bone reset. RESETS ON GAME LAUNCH", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();


				cfg.headIndex = new List<ConfigEntry<int>>{
					Config.Bind("Adv1 Head", $"Head Index {index=1}", (int)ChaFileDefine.BodyShapeIdx.HeadSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv1 Head", $"Head Index {++index}", (int)ChaFileDefine.BodyShapeIdx.NeckW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv1 Head", $"Head Index {++index}", (int)ChaFileDefine.BodyShapeIdx.NeckZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced = true })).ConfigDefaulter(),
				};

				cfg.brestIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv2 Brest", $"Brest Index {index=1}", (int)ChaFileDefine.BodyShapeIdx.AreolaBulge, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustRotY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustSharp, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.NipStand, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.NipWeight, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
#if KOI_API
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)ChaFileDefine.BodyShapeIdx.BustForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(),
#endif
				};

				cfg.torsoIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv3 Torso", $"Torso Index {index=1}",  (int)ChaFileDefine.BodyShapeIdx.BodyLowW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.BodyLowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.BodyShoulderW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.BodyShoulderZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.BodyUpW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.BodyUpZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.WaistUpW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.WaistUpZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.WaistY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
				
#if KOI_API
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)ChaFileDefine.BodyShapeIdx.Belly, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#endif
				  };

				cfg.armIndex = new List<ConfigEntry<int>>
				{

						Config.Bind("Adv4 Arm", $"Arm Index {index=1}", (int)ChaFileDefine.BodyShapeIdx.ArmLow, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
#if HONEY_API
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ArmUp, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.Shoulder, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),

#elif KOI_API
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ArmUpW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ArmUpZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ElbowW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ElbowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ShoulderW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ShoulderZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(),
			
#endif
				 };

				cfg.buttIndex = new List<ConfigEntry<int>>
				{
						Config.Bind("Adv5 Butt", $"Butt Index {index=1}", (int)ChaFileDefine.BodyShapeIdx.Hip, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv5 Butt", $"Butt Index {++index}", (int)ChaFileDefine.BodyShapeIdx.HipRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv5 Butt", $"Butt Index {++index}", (int)ChaFileDefine.BodyShapeIdx.WaistLowW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv5 Butt", $"Butt Index {++index}", (int)ChaFileDefine.BodyShapeIdx.WaistLowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),

				 };

				cfg.legIndex = new List<ConfigEntry<int>>
				{
						Config.Bind("Adv6 Leg", $"Leg Index {index=1}", (int)ChaFileDefine.BodyShapeIdx.Calf, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(),
					
#if HONEY_API
									
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.Ankle, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ThighLow, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ThighUp, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(),
#elif KOI_API
								
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.AnkleW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.AnkleZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.KneeLowW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.KneeLowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ThighLowW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ThighLowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ThighUpW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)ChaFileDefine.BodyShapeIdx.ThighUpZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#endif
				  };

				cfg.earIndex = new List<ConfigEntry<int>>
				{

					Config.Bind("Adv7 Ear", $"Ear Index {index=1}", (int)ChaFileDefine.FaceShapeIdx.EarLowForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EarRotY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EarRotZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EarSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EarUpForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),


				};

				cfg.eyeIndex = new List<ConfigEntry<int>>
				{

						Config.Bind("Adv8 Eye", $"Eye Index {index=1}", (int)ChaFileDefine.FaceShapeIdx.EyeH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeInX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeOutY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),


#if HONEY_API
											
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeInY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidForm01, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidForm02, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeOutX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeRotY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeRotZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#elif KOI_API
									
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyebrowInForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyebrowOutForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyebrowRotZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyebrowX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyebrowY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidsLowForm1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidsLowForm2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidsLowForm3, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidsUpForm1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidsUpForm2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyelidsUpForm3, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)ChaFileDefine.FaceShapeIdx.EyeTilt, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#endif
				};

				cfg.noseIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv9 Nose", $"Nose Index {index=1}", (int)ChaFileDefine.FaceShapeIdx.NoseBridgeH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
				
#if HONEY_API
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseAllRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseAllW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseAllY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseAllZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseBridgeForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseBridgeW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseWingRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseWingRotZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseWingW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseWingY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseWingZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#elif KOI_API
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseTipH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)ChaFileDefine.FaceShapeIdx.NoseY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#endif
				};

				cfg.mouthIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv10 Mouth", $"Mouth Index {index=1}", (int)ChaFileDefine.FaceShapeIdx.MouthCornerForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)ChaFileDefine.FaceShapeIdx.MouthLowForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)ChaFileDefine.FaceShapeIdx.MouthUpForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)ChaFileDefine.FaceShapeIdx.MouthW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)ChaFileDefine.FaceShapeIdx.MouthY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)ChaFileDefine.FaceShapeIdx.MouthZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#if HONEY_API
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)ChaFileDefine.FaceShapeIdx.MouthH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(),
#endif
			   };


				void hideIndexes()
				{
					foreach(var setting in Config)
						if(setting.Key.Key.Contains("Index") && setting.Key != cfg.hideAdvIndexes.Definition)
							(setting.Value.Description.Tags[0] as ConfigurationManagerAttributes).Browsable = !cfg.hideAdvIndexes.Value;
				}

				hideIndexes();
				cfg.hideAdvIndexes.SettingChanged += (m, n) => { hideIndexes(); };
			}

			Instance.Config.Save();
			Instance.Config.SaveOnConfigSet = saveCfgAuto;

			//populate defaults
			PopulateDefaultSettings(defaultStr);
			UpdateDefaultsList();

			//if it's needed
			if(cfg.unknownTest != null)
				cfg.unknownTest.SettingChanged += (m, n) =>
				{

				};

			string p = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
			CharaMorpher_GUI.morphTex = p.CreateTexture();
			cfg.charDir.SettingChanged += (m, n) =>
			{

				string path = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
				foreach(var ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
				{
					if(File.Exists(path))
						if(ctrl.isInitLoadFinished)
							ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
				}
			};

			cfg.imageName.SettingChanged += (m, n) =>
			{

				string path = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
				foreach(var ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
				{
					if(File.Exists(path))
						if(ctrl.isInitLoadFinished)
							ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
				}
			};

			cfg.currentControlSetName.SettingChanged += (m, n) =>
			{
				UpdateGUISelectList();
			};

			cfg.preferCardMorphDataMaker.SettingChanged += (m, n) =>
			{
				if(MakerAPI.InsideMaker || StudioAPI.InsideStudio)
					foreach(var ctrl in GetFuncCtrlOfType<CharaMorpher_Controller>())
						if(ctrl.isInitLoadFinished)
							ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
			};

			cfg.preferCardMorphDataGame.SettingChanged += (m, n) =>
			{
				if(!MakerAPI.InsideMaker && !StudioAPI.InsideStudio)
					foreach(var ctrl in GetFuncCtrlOfType<CharaMorpher_Controller>())
						if(ctrl.isInitLoadFinished)
							ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
			};

			cfg.onlyMorphCharWithDataInGame.SettingChanged += (m, n) =>
			{
				if(!MakerAPI.InsideMaker && !StudioAPI.InsideStudio)
					foreach(var ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
						for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
							ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1, forceReset: !ctrl.Enable));
			};

			cfg.enable.SettingChanged += (m, n) =>
			{
				foreach(var ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl?.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a + 1, forceReset: !cfg.enable.Value));

				Logger.LogMessage(cfg.enable.Value ?
									"Character Morpher Enabled" :
									"Character Morpher Disabled");


			};

			cfg.enableInGame.SettingChanged += (m, n) =>
			{
				foreach(CharaMorpher_Controller ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
			};

			cfg.enableABMX.SettingChanged += (m, n) =>
			{
				if(!ABMXDependency.InTargetVersionRange)
				{
					//if(cfg.enableABMX.Value)
					//	cfg.enableABMX.Value = false;


					return;
				}

				foreach(CharaMorpher_Controller ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
				{
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
				}
			};

			cfg.studioWinRec.SettingChanged += (m, n) =>
			{
				if(!cfg.studioWinRec.Value.Equals(winRec))
					winRec = new Rect(cfg.studioWinRec.Value);
			};

			cfg.makerViewportUISpace.SettingChanged += (m, n) =>
			{
				CharaMorpher_GUI.select.ResizeCustomUIViewport();
			};

			// useCardMorphDataGame()
			{
				Coroutine tmp = null;
				bool lastUCMD = cfg.preferCardMorphDataGame.Value;//this is needed

				cfg.preferCardMorphDataGame.SettingChanged += (m, n) =>
				{
					if(MakerAPI.InsideMaker)
					{
						lastUCMD = cfg.preferCardMorphDataGame.Value;//this is needed
						return;
					}


					IEnumerator CoUCMD()
					{

						foreach(var ctrl in GetFuncCtrlOfType<CharaMorpher_Controller>())
						{
							string name =
							(!cfg.preferCardMorphDataGame.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
							name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDiv), 0, name.Length));


							//Logger.LogDebug($"lastUCMD: {lastUCMD}");
							yield return new WaitWhile(() => ctrl.isReloading);

							var tmpCtrls =
							!cfg.preferCardMorphDataGame.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1);
							tmpCtrls.currentSet = ctrl.controls.currentSet;

							ctrl.controls.Copy(!lastUCMD ? ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

							ctrl.SoftSaveControls(lastUCMD);

							ctrl.controls.Copy(!cfg.preferCardMorphDataGame.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

							lastUCMD = cfg.preferCardMorphDataGame.Value;//this is needed
																		 //Logger.LogDebug($"Next lastUCMD: {lastUCMD}");

							if(!name.IsNullOrEmpty())
								SwitchControlSet(ControlsList, name);
						}
						//	select.Options = ControlsList;

						yield break;
					}

					if(tmp != null)
						StopCoroutine(tmp);
					tmp = StartCoroutine(CoUCMD());
				};

			}


			//if(StudioAPI.InsideStudio) return;

			/*
				Register your logic that depends on a character.
				A new instance of this component will be added to ALL characters in the game.
				The GUID will be used as the ID of the extended data saved to character
				cards, scenes and game saves, so make sure it's unique and do not change it!
			 */
			CharacterApi.RegisterExtraBehaviour<CharaMorpher_Controller>(GUID);
			CharaMorpher_GUI.Initialize();
			Hooks.Init();
		}

		void Update()
		{
			//Key Updates
			if(cfg.enableKey.Value.IsDown())
			{
				cfg.enable.Value = !cfg.enable.Value;
			}

			if(cfg.enableCharKey.Value.IsDown())
			{
				var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().FirstOrNull();

				if(MakerAPI.InsideMaker && ctrl)
					ctrl.Enable = !ctrl.morphEnable;

				else if(StudioAPI.InsideStudio)
					foreach(var ctrler in StudioAPI.GetSelectedControllers<CharaMorpher_Controller>())
						ctrler.Enable = !ctrler.morphEnable;
			}

			if(cfg.enable.Value && (cfg.prevControlKey.Value.IsDown() || cfg.nextControlKey.Value.IsDown()))
			{
				var tmp = SwitchControlSet(ControlsList, cfg.currentControlSetName.Value);

				if(cfg.prevControlKey.Value.IsDown())
					tmp--;
				if(cfg.nextControlKey.Value.IsDown())
					tmp++;

				tmp = tmp < 0 ? ControlsList.Length - 1 : tmp % ControlsList.Length;

				SwitchControlSet(ControlsList, tmp, false);//this is PEAK 3AM programming 🤣🤣

				//Logger.LogMessage("KEY WAS PRESSED!!!!");

				foreach(var ctrl in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1));

				Logger.LogMessage($"Switched to new slot [{cfg.currentControlSetName.Value}]");
			}
		}
	}

	public static class Morph_Util
	{
		internal static ManualLogSource Logger { get => CharaMorpher_Core.Logger; }

		static Texture2D _greyTex = null;
		public static Texture2D greyTex
		{
			get
			{
				if(_greyTex != null) return _greyTex;

				_greyTex = new Texture2D(1, 1);
				var pixels = _greyTex.GetPixels();
				for(int a = 0; a < pixels.Length; ++a)
					pixels[a] = Color.black;
				_greyTex.SetPixels(pixels);
				_greyTex.Apply();

				return _greyTex;
			}
		}

		/// <summary>
		/// Adds a value to the end of a list and returns it
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public static T AddNReturn<T>(this ICollection<T> list, T val)
		{
			list.Add(val);
			return list.Last();
		}

		class StringComparer : IEqualityComparer<string>
		{
			public bool Equals(string x, string y) => x == y;

			public int GetHashCode(string obj) => obj.GetHashCode();

		}
		class ConfigDefinitionComparer : IEqualityComparer<ConfigDefinition>
		{
			//this is probably done somewhere but here for future reference 
			public bool Equals(ConfigDefinition x, ConfigDefinition y)
			{
				return x.Key == y.Key;
			}

			public int GetHashCode(ConfigDefinition obj) => obj.GetHashCode();
		}

		public static T FirstOrNull<T>(this IEnumerable<T> enu)
		{
			//I love loopholes 🤣
			try
			{ return enu.Count() > 0 ? enu.First() : (T)(object)null; }
			catch { return (T)(object)null; }
		}
		public static T FirstOrNull<T>(this IEnumerable<T> enu, Func<T, bool> predicate)
		{
			//I love loopholes 🤣
			try
			{ return enu.Count() > 0 ? enu.First(predicate) : (T)(object)null; }
			catch { return (T)(object)null; }
		}
		public static T LastOrNull<T>(this IEnumerable<T> enu)
		{
			try
			{ return enu.Count() > 0 ? enu.Last() : (T)(object)null; }
			catch { return (T)(object)null; }
		}     //I love loopholes 🤣
		public static T LastOrNull<T>(this IEnumerable<T> enu, Func<T, bool> predicate)
		{
			try
			{ return enu.Count() > 0 ? enu.Last(predicate) : (T)(object)null; }
			catch { return (T)(object)null; }
		}   //I love loopholes 🤣

		public static bool InRange<T>(this IEnumerable<T> list, int index) => index >= 0 && index < list.Count();
		public static bool InRange<T>(this T src, T min, T max) where T : IComparable
			=> src.CompareTo(max) <= 0 && src.CompareTo(min) >= 0;


		public static List<KeyValuePair<ConfigDefinition, string>> GetUnorderedOrphanedEntries(this ConfigFile file, string sec = "")
		{
			Dictionary<ConfigDefinition, string> OrphanedEntries = new Dictionary<ConfigDefinition, string>();
			List<KeyValuePair<ConfigDefinition, string>> orderedOrphanedEntries = new List<KeyValuePair<ConfigDefinition, string>>();
			string section = string.Empty;
			string[] array = File.ReadAllLines(file.ConfigFilePath);
			for(int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if(text.StartsWith("#"))
				{
					continue;
				}

				if(text.StartsWith("[") && text.EndsWith("]"))
				{
					section = text.Substring(1, text.Length - 2);
					continue;
				}

				string[] array2 = text.Split(new char[1] { '=' }, 2);
				if(sec == section || sec.IsNullOrEmpty())
					if(array2.Length == 2)
					{
						string key = array2[0].Trim();
						string text2 = array2[1].Trim();
						ConfigDefinition key2 = new ConfigDefinition(section, key);
						if(!((IDictionary<ConfigDefinition, ConfigEntryBase>)file).TryGetValue(key2, out _))
						{
							OrphanedEntries[key2] = text2;
							orderedOrphanedEntries.Add(new KeyValuePair<ConfigDefinition, string>(key2, text2));
						}
					}

			}

			return orderedOrphanedEntries;
		}
		public static List<string[]> oldConversionList { get; } =
			new List<string[]>
			{
				//I should have ordered this differently but to late now 😝
				new[]{"Overall Voice","Vioce Default",bool.FalseString},
				new[]{"Overall Skin Colour","Skin Default",bool.FalseString},
				new[]{"Base Skin Colour","Base Skin Default",bool.FalseString},
				new[]{"Sunburn Colour","Sunburn Default",bool.FalseString},

				new[]{"Overall Body","Body  Default",bool.FalseString},
				new[]{"Head","Head  Default",bool.FalseString},
				new[]{"Boobs","Boobs Default",bool.FalseString},
				new[]{"Boob Phys.","Boob Phys. Default",bool.FalseString},
				new[]{"Torso","Torso Default",bool.FalseString},
				new[]{"Arms","Arms  Default",bool.FalseString},
				new[]{"Butt","Butt  Default",bool.FalseString},
				new[]{"Legs","Legs  Default",bool.FalseString},
				new[]{"Body Other","Body Other Default",bool.FalseString},


				new[]{"Overall Face","Face  Default",bool.FalseString},
				new[]{"Ears","Ears  Default",bool.FalseString},
				new[]{"Eyes","Eyes  Default", bool.FalseString },
				new[]{"Nose","Nose  Default",bool.FalseString},
				new[]{"Mouth","Mouth Default", bool.FalseString },
				new[]{"Face Other","Face Other Default", bool.FalseString },

				new[]{"ABMX Overall Body","ABMX  Body Default",bool.TrueString},
				new[]{"ABMX Boobs","ABMX  Boobs Default",bool.TrueString},
				new[]{"ABMX Torso","ABMX  Torso Default",bool.TrueString},
				new[]{"ABMX Arms","ABMX  Arms Default",bool.TrueString},
				new[]{"ABMX Hands","ABMX  Hands Default", bool.TrueString },
				new[]{"ABMX Butt","ABMX  Butt Default", bool.TrueString },
				new[]{"ABMX Legs","ABMX  Legs Default", bool.TrueString },
				new[]{"ABMX Feet","ABMX  Feet Default", bool.TrueString },
				new[]{"ABMX Genitals","ABMX  Genitals Default", bool.TrueString },
				new[]{"ABMX Body Other","ABMX  Body Other Default", bool.TrueString },

				new[]{"ABMX Overall Head","ABMX  Head Default", bool.TrueString },
				new[]{"ABMX Ears","ABMX  Ears Default", bool.TrueString },
				new[]{"ABMX Eyes","ABMX  Eyes Default", bool.TrueString },
				new[]{"ABMX Nose","ABMX  Nose Default", bool.TrueString },
				new[]{"ABMX Mouth","ABMX  Mouth Default", bool.TrueString },
				new[]{"ABMX Hair","ABMX  Hair Default", bool.TrueString },
				new[]{"ABMX Head Other","ABMX  Head Other Default", bool.TrueString },
			};


		/// <summary>
		/// 
		/// </summary>
		public static void UpdateDefaultsList()
		{
			var orphaned = Instance.Config.GetUnorderedOrphanedEntries("Defaults");
			var defList = orphaned.Attempt((k) => k.Key).ToList();

			//orphaned = Instance.Config.GetUnorderedOrphanedEntries("Mode Defaults");
			//var modeDefList = orphaned.Attempt((k) => k.Key);


			var saveCfgAuto =
				Instance.Config.SaveOnConfigSet;
			Instance.Config.SaveOnConfigSet = false;


			foreach(var val in defList)
			{
				var slotName = val.Key.Substring(0, val.Key.LastIndexOf(strDiv) + 1)?.Trim();
				var settingName = val.Key.Substring(val.Key.LastIndexOf(strDiv) + 1);

				//Morph_Util.Logger.LogDebug($"For start");
				//Morph_Util.Logger.LogDebug($"val.key: {val.Key}");

				if(!cfg.defaults.TryGetValue(slotName, out var tmp) && !slotName.IsNullOrEmpty())
					PopulateDefaultSettings(slotName);

				if(!cfg.defaults.TryGetValue(slotName, out tmp))
					cfg.defaults[slotName] = new Dictionary<string, ConfigEntry<MorphSliderData>>();

				if(!Instance.controlCategories.TryGetValue(slotName, out var tmp2))
					Instance.controlCategories[slotName] =
						new List<MorphSliderData>(Instance.controlCategories[defaultStr]);

				ConfigEntry<MorphSliderData> lastConfig = null;

				var oldData = oldConversionList.FirstOrNull((a) => a[1] == settingName);
				string convertStr = oldData?[0].Trim() ?? "";
				bool isAbmx = bool.Parse(oldData?[2] ?? bool.FalseString);

				if(!cfg.defaults[slotName].Any((k) => k.Value.Definition.Key == val.Key))
					cfg.defaults[slotName].Add(
						convertStr,
						lastConfig = Instance.Config.Bind(val, Instance.controlCategories[slotName].
						AddNReturn(new MorphSliderData(convertStr)).Clone(),
						new ConfigDescription("", tags: new ConfigDescription("Set default value on maker startup", null,
						new ConfigurationManagerAttributes
						{
							Order = -Instance.controlCategories[slotName].Count,
							Browsable = false
						}))));

				//Logger.LogDebug($"settingName: [{settingName}]");
				//Logger.LogDebug($"convertStr: [{convertStr}]\n");
				if(lastConfig == null)
					lastConfig = (ConfigEntry<MorphSliderData>)Instance.Config[val];

				if(lastConfig.Value.dataName.IsNullOrEmpty())
					lastConfig.Value.dataName = convertStr;


				if(slotName.IsNullOrEmpty())
				{
					//lastConfig = (ConfigEntry<MorphSliderData>)Instance.Config[val];
					convertStr = null;

					if(cfg.oldControlsConversion.Value)
					{

						if(!(cfg.defaults[defaultStr].Values.ToList().
							FirstOrNull((v) => v.Definition.Key.Contains(val.Key)) == null))
							if(!(convertStr = oldConversionList.FirstOrNull((a) => a[1] == val.Key)?[0].Trim() ?? null).IsNullOrEmpty())
								cfg.defaults[defaultStr][convertStr]?.Value.SetData(lastConfig?.Value.data * .01f ?? 0);

						if(!(cfg.defaults[defaultStr].Values.ToList().
							FirstOrNull((v) => (v.Definition.Key + " Mode").Contains(val.Key)) == null))
							if(!(convertStr = oldConversionList.FirstOrNull((a) => (a[1] + " Mode") == val.Key)?[0].Trim() ?? null).IsNullOrEmpty())
								cfg.defaults[defaultStr][convertStr]?.Value.SetCalcType(lastConfig?.Value.calcType ?? MorphCalcType.LINEAR);
					}

					cfg.defaults.Remove(slotName);
					Instance.controlCategories.Remove(slotName);

					//re-save old data in original format
					{

						var data = lastConfig?.Value?.data ?? 0;
						int calc = (int)(lastConfig?.Value?.calcType ?? 0);
						Instance.Config.Remove(val);
						if(val.Key.LastIndexOf(" Mode") == (val.Key.Length - " Mode".Length))
							Instance.Config.Bind(val, calc, new ConfigDescription("", tags:
						new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false, }));
						else
							Instance.Config.Bind(val, data, new ConfigDescription("", tags:
						new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false, }));
					}
				}

				//	Morph_Util.Logger.LogDebug($"UpdateDefaultsList Name: {name}");
			}

			Instance.Config.Save();
			Instance.Config.SaveOnConfigSet = saveCfgAuto;
			cfg.oldControlsConversion.Value = false;
		}

		public static int SwitchControlSet(string[] selection, int val, bool keepProgress = true, CharaMorpher_Controller ctrl = null)
		{
			//var ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().First();
			if(selection is null || selection.Length < 1) return -1;

			if(cfg.debug.Value) Logger.LogDebug($"current slot [{(ctrl?.controls?.currentSet ?? cfg.currentControlSetName.Value)}]");

			var name = selection[val = Mathf.Clamp(val, 0, selection.Length - 1)].Trim();

			name = (name.LastIndexOf(strDiv) == name.Length - strDiv.Length ?
					name.Substring(0, name.LastIndexOf(strDiv)) : name) + strDiv;

			//Replace the new set with the last settings before changing the setting name (loading will call the actual settings)
			if((ctrl?.controls?.currentSet ?? cfg.currentControlSetName.Value) != name)
				if(ctrl)
				{
					if((MakerAPI.InsideMaker || StudioAPI.InsideStudio) && keepProgress && ctrl.controls.all.  //made so you don't loose your																              
								TryGetValue(ctrl.controls.currentSet, out var tmp))        //progress when switching in maker
						ctrl.controls.all[name] = tmp.ToDictionary(k => k.Key, v => v.Value.Clone());

					ctrl.controls.currentSet = name;
				}
				else
				{
					foreach(var ctrl1 in GetFuncCtrlOfType<CharaMorpher_Controller>())
					{

						if((MakerAPI.InsideMaker || StudioAPI.InsideStudio) && keepProgress && ctrl1.controls.all.  //made so you don't loose your																              
							TryGetValue(ctrl1.controls.currentSet, out var tmp))        //progress when switching in maker
							ctrl1.controls.all[name] = tmp.ToDictionary(k => k.Key, v => v.Value.Clone());

						ctrl1.controls.currentSet = name;
					}
				}

			if(cfg.currentControlSetName.Value != name)
				cfg.currentControlSetName.Value = name;

			//Logger.LogMessage($"Switched to new slot [{cfg.currentControlName.Value}]");
			return val;
		}
		public static int SwitchControlSet(string[] selection, string val, bool keepProgress = true, CharaMorpher_Controller ctrl = null) =>
			selection is null ? -1 : SwitchControlSet(selection,
				val is null ? -1 :
				Array.IndexOf(selection, val.Trim().LastIndexOf(strDiv) == val.Trim().Length - strDiv.Length ?
					val.Trim().Substring(0, val.Trim().LastIndexOf(strDiv)) : val.Trim()), keepProgress, ctrl);//will automatically remove "strDivider" if at end of string"
		public static void UpdateDropdown(ICollection<string> selector)
		{

			selector?.Clear();
			foreach(var key in cfg.defaults.Keys)
				selector?.Add(key);

			//	Morph_Util.Logger.LogDebug($"current List [{string.Join(", ", selector?.ToArray() ?? new string[] { })}]");

			//selecter.Value = 
			SwitchControlSet(cfg.defaults.Keys.ToArray(), selectedMod);

		}

		public static void PopulateDefaultSettings(string name)
		{
			var ctrl1 = Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>()?.FirstOrNull();
			if(name.LastIndexOf(strDiv) != (name.Length - strDiv.Length)) name += strDiv;

			Instance.controlCategories[name] = new List<MorphSliderData> { };//init list

			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) || !cfg.preferCardMorphDataMaker.Value || ctrl1?.ctrls2 == null)
			{
				var saveCfgAuto =
				Instance.Config.SaveOnConfigSet;
				Instance.Config.SaveOnConfigSet = false;

				int defaultIndex = -1;
				string settingName = null;
				//new ConfigurationManagerAttributes
				//{
				//	Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, settingName)).Key,
				//	Browsable = false,
				//
				//};
				var ctrlCat = Instance.controlCategories[name];
				cfg.defaults[name] = new Dictionary<string, ConfigEntry<MorphSliderData>>()
				{
					{settingName = "Overall Voice",  Instance.Config.Bind("Defaults", $"{name} "+"Vioce Default",ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 00f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},

					{ settingName = "Overall Skin Colour",Instance.Config.Bind("Defaults", $"{name} "+"Skin Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Base Skin Colour",Instance.Config.Bind("Defaults", $"{name} "+"Base Skin Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 00f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Sunburn Colour",Instance.Config.Bind("Defaults", $"{name} "+"Sunburn Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 00f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},

					{settingName = "Overall Body", Instance.Config.Bind("Defaults", $"{name} "+"Body  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					 new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Head",Instance.Config.Bind("Defaults", $"{name} "+"Head  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Boobs",Instance.Config.Bind("Defaults", $"{name} "+"Boobs Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Boob Phys.",Instance.Config.Bind("Defaults", $"{name} "+"Boob Phys. Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{ settingName = "Torso",Instance.Config.Bind("Defaults", $"{name} "+"Torso Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Arms",Instance.Config.Bind("Defaults", $"{name} "+"Arms  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Butt",Instance.Config.Bind("Defaults", $"{name} "+"Butt  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Legs",Instance.Config.Bind("Defaults", $"{name} "+"Legs  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Body Other",Instance.Config.Bind("Defaults", $"{name} "+"Body Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},


					{ settingName = "Overall Face",Instance.Config.Bind("Defaults", $"{name} "+"Face  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Ears",Instance.Config.Bind("Defaults", $"{name} "+"Ears  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Eyes",Instance.Config.Bind("Defaults", $"{name} "+ "Eyes  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1 , Browsable = false }))
					},{settingName = "Nose",Instance.Config.Bind("Defaults", $"{name} "+"Nose  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Mouth",Instance.Config.Bind("Defaults", $"{name} "+"Mouth Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Face Other",Instance.Config.Bind("Defaults", $"{name} "+"Face Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1 , Browsable = false }))
					},

					{ settingName = "ABMX Overall Body",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Body Default", ctrlCat.AddNReturn(new MorphSliderData(settingName,data: 100f *.01f,isABMX:true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Boobs",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Boobs Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f,isABMX:true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{ settingName = "ABMX Torso",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Torso Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f,isABMX:true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Arms",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Arms Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Hands",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Hands Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Butt",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Butt Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Legs",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Legs Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Feet",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Feet Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Genitals",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Genitals Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Body Other",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Body Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},

					{ settingName = "ABMX Overall Head",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Head Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 100f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Ears",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Ears Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Eyes",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Eyes Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Nose",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Nose Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Mouth",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Mouth Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Hair",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Hair Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data: 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Head Other",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Head Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, data : 50f *.01f, isABMX : true)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},
				};

				foreach(var val in cfg.defaults[name])
					val.Value.Value.dataName = val.Key + "";

				//Morph_Util.Logger.LogDebug($"Current List: [{string.Join(", ", Instance.controlCategories[name].Attempt((v)=>v.Value))}]");

				Instance.Config.Save();
				Instance.Config.SaveOnConfigSet = saveCfgAuto;
			}

			//foreach(var val in cfg.defaults[name])
			//{
			//	var lastConfig = val.Value;
			//	if((int)lastConfig.Value.data == (int)lastConfig.Value.calcType) //This part is only useful for me
			//	{
			//		var settingName = val.Key.Substring(val.Key.LastIndexOf(strDivider) + 1).TrimStart();
			//		var convertStr = (oldConversionList.FirstOrNull((a) => a.Item2 == settingName)?.Item1 ?? "");
			//
			//		lastConfig.Value.dataName = convertStr;
			//		lastConfig.Value.data = lastConfig.Value.data ;
			//		lastConfig.Value.calcType = MorphCalcType.LINEAR;
			//	}
			//}
		}

		public static string AddNewSetting(string baseName = "Slot", CharaMorpher_Controller ctrl1 = null)
		{
			int count = 1;
			ctrl1 = ctrl1 ?? Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>()?.FirstOrNull();

			string name = "Error" + strDiv;
			var defList = (!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) || !cfg.preferCardMorphDataMaker.Value || ctrl1?.ctrls2 == null ?
				Instance.controlCategories.Keys.ToList() :
				ctrl1?.controls?.all?.Keys?.ToList() ?? Instance.controlCategories.Keys.ToList();
			//var modeDefList = Instance.Config.Where((k) => k.Key.Section == "Mode Defaults");

			if(baseName.IsNullOrEmpty()) baseName = defaultStr.Substring(0, defaultStr.LastIndexOf(strDiv));

			var tmp = 0;
			foreach(var chara in baseName.Reverse())
			{
				//Int32.;
				if(!Regex.IsMatch($"{chara}", @"\d")) break;
				++tmp;
			}
			baseName = baseName.Substring(0, baseName.Length - tmp).Trim();

			//find new empty slot name
			while(defList?.Any((k) =>
			k.Contains(name = $"{baseName} {count}{strDiv}")) ?? false) ++count;



			//	Morph_Util.Logger.LogDebug("creating Defaults");

			PopulateDefaultSettings(name);

			//Morph_Util.Logger.LogDebug("creating Controls");
			foreach(var ctrl2 in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
			{
				ctrl2.controls.all[name] = new Dictionary<string, MorphSliderData>();

				var data = (!ctrl2.IsUsingExtMorphData ? ctrl2.ctrls1 : (ctrl2.ctrls2 ?? ctrl2.ctrls1))?.all;
				if(!data.ContainsKey(name))
					data[name] = new Dictionary<string, MorphSliderData>();

			}
			foreach(var ctrl2 in Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>())
				foreach(var ctrl in Instance.controlCategories[name])
				{
					var tmp2 = Instance.controlCategories[defaultStr].Find(v => v.dataName == ctrl.dataName);
					ctrl2.controls.all[name][ctrl.dataName] = tmp2.Clone();

					var data = ((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) || !cfg.preferCardMorphDataMaker.Value ? ctrl2.ctrls1 : (ctrl2.ctrls2 ?? ctrl2.ctrls1))?.all;
					if(!data[name].ContainsKey(ctrl.dataName))
					{
						data[name][ctrl.dataName] = tmp2.Clone();
					}
				}

			Morph_Util.Logger.LogMessage($"Created {name}");

			return name;
		}

		public static void RemoveCurrentSetting(string baseName, CharaMorpher_Controller ctrl = null)
		{
			ctrl = ctrl ?? Morph_Util.GetFuncCtrlOfType<CharaMorpher_Controller>()?.FirstOrNull();

			string name = baseName.IsNullOrEmpty() ? cfg.currentControlSetName.Value : baseName;

			if(name == defaultStr) return;

			//Morph_Util.Logger.LogDebug("remove Controls");

			foreach(CharaMorpher_Controller ctrl1 in GetFuncCtrlOfType<CharaMorpher_Controller>())
			{
				var obj = ctrl1.controls.all;
				if(obj.ContainsKey(name))
					obj[name].Clear();
				obj.Remove(name);

				var data = ((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) || !cfg.preferCardMorphDataMaker.Value ? ctrl1.ctrls1 : (ctrl1.ctrls2 ?? ctrl1.ctrls1))?.all;
				if(data.ContainsKey(name))
					data[name].Clear();
				data.Remove(name);

				//(!MakerAPI.InsideMaker||!cfg.useCardMorphDataMaker.Value ? ctrl1.ctrls1 : ctrl1.ctrls2 ?? ctrl1.ctrls1)?.all?.Remove(name);
				//	Morph_Util.Logger.LogDebug($"Controls List: [{string.Join(", ", obj.Keys.ToArray())}]");
			}

			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) || !cfg.preferCardMorphDataMaker.Value || ctrl?.ctrls2 == null)
			{
				//	Morph_Util.Logger.LogDebug("remove Defaults");
				foreach(var def in cfg.defaults)
					foreach(var val in def.Value)
						if(def.Key == name)
							Instance.Config.Remove(val.Value.Definition);

				//	foreach(var def in cfg.defaultModes)
				//		foreach(var val in def.Value)
				//			if(def.Key == name)
				//				Instance.Config.Remove(val.Definition);


				cfg.defaults.Remove(name);
				//cfg.defaultModes.Remove(name);
				Instance.controlCategories.Remove(name);

				if(!Instance.Config.SaveOnConfigSet)
					Instance.Config.Save();//save to disk
			}

			Morph_Util.Logger.LogMessage($"removed [{name}]");
			//	Morph_Util.Logger.LogDebug($"Current List: [{string.Join(", ", ControlsList)}]");


		}


		/// <summary>
		/// makes sure a path fallows the format "this/is/a/path" and not "this//is\\a/path" or similar
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="oldslash"></param>
		/// <param name="newslash"></param>
		/// <returns></returns>
		public static string MakeDirPath(this string dir, string oldslash = "\\", string newslash = "/")
		{

			dir = (dir ?? "").Trim().Replace(oldslash, newslash).Replace(newslash + newslash, newslash);

			if((dir.LastIndexOf('.') < dir.LastIndexOf(newslash))
				&& dir.Substring(dir.Length - newslash.Length) != newslash)
				dir += newslash;

			return dir;
		}

		/// <summary>
		/// Returns a list of the registered handler specified. returns empty list otherwise 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> GetFuncCtrlOfType<T>()
		{
			foreach(var hnd in CharacterApi.RegisteredHandlers
				.Where(reg => reg.ControllerType == typeof(T)))
				return hnd.Instances.Cast<T>();

			return new T[] { };
		}

		/// <summary>
		/// Defaults the ConfigEntry on game launch using default value specified
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		public static ConfigEntry<T> ConfigDefaulter<T>(this ConfigEntry<T> v1, T v2)
		{
			if(v1 == null || !CharaMorpher_Core.cfg.resetOnLaunch.Value) return v1;

			v1.Value = v2;
			v1.SettingChanged += (m, n) => { if(v2 != null) v2 = v1.Value; };
			return v1;
		}

		/// <summary>
		/// Defaults the ConfigEntry on game launch using default value in ConfigEntry
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		public static ConfigEntry<T> ConfigDefaulter<T>(this ConfigEntry<T> v1) => v1?.ConfigDefaulter((T)v1.DefaultValue);

		static Texture tmpTex = null;
		static string lastPath = null;
		internal static void MyImageButtonDrawer(ConfigEntryBase entry)
		{
			// Make sure to use GUILayout.ExpandWidth(true) to use all available space

			GUILayout.BeginVertical();

			string path = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));
			if(lastPath != path)
			{
				lastPath = path;
				tmpTex = path.CreateTexture();

				if(tmpTex)
					tmpTex.filterMode = FilterMode.Bilinear;
			}

			//image
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUILayout.Box(tmpTex, GUILayout.Width(150), GUILayout.Height(200));

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			//button
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if(GUILayout.Button(new GUIContent(entry.Definition.Key, entry.Description.Description), GUILayout.ExpandWidth(true)))
				CharaMorpher_GUI.GetNewImageTarget();

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();


			GUILayout.EndVertical();
		}

		public static Action<ConfigEntryBase> ButtonDrawer(string name = null, string tip = null, Action onClick = null, bool vertical = true)
		{
			return new Action<ConfigEntryBase>((cfgEntry) =>
			{
				if(vertical)
					GUILayout.BeginVertical();
				else
					GUILayout.BeginHorizontal();

				GUILayout.Space(5);

				if(GUILayout.Button(new GUIContent { text = name ?? cfgEntry.Definition.Key, tooltip = tip ?? cfgEntry.Description.Description }, GUILayout.ExpandWidth(true)) && onClick != null)
					onClick();

				GUILayout.Space(5);

				if(vertical)
					GUILayout.EndVertical();
				else
					GUILayout.EndHorizontal();

			});
		}

		public static Action<ConfigEntryBase> DropdownDrawer(string name = null, string tip = null, string[] items = null, int initIndex = 0, Func<string[], string[]> listUpdate = null, Func<int, int> onSelect = null, bool vertical = true)
		{
			int selectedItem = initIndex;
			bool selectingItem = false;
			Vector2 scrollview = Vector2.zero;

			return new Action<ConfigEntryBase>((cfgEntry) =>
			{
				if(vertical)
					GUILayout.BeginVertical();
				else
					GUILayout.BeginHorizontal();

				items = listUpdate != null ? listUpdate(items) : items;

				if((Math.Max(-1, Math.Min(items.Length - 1, selectedItem))) < 0)
					selectedItem = Math.Max(0, Math.Min
					(items.Length - 1, selectedItem));

				if(selectedItem < 0) return;


				try
				{
					GUILayout.Space(3);
					bool btn;
					int maxWidth = 350, maxHeight = 200;
					if(items.Length > 0)
						if((btn = GUILayout.Button(new GUIContent { text = name ?? $"{cfgEntry.Definition.Key} {items[selectedItem]}", tooltip = tip ?? cfgEntry.Description.Description },
							 GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(maxWidth))) || selectingItem)
						{
							selectingItem = !(btn && selectingItem);//if dropdown btn was pressed

							scrollview = GUILayout.BeginScrollView(scrollview, false, false,
								GUILayout.ExpandWidth(true),
								GUILayout.ExpandHeight(true), GUILayout.Height(150), GUILayout.MaxHeight(maxHeight), GUILayout.MaxWidth(maxWidth));

							var select = GUILayout.SelectionGrid(selectedItem, items, 1, GUILayout.ExpandWidth(true));
							if(select != selectedItem) { selectingItem = false; select = onSelect != null ? onSelect(select) : select; }
							selectedItem = select;

							GUILayout.EndScrollView();
						}

					GUILayout.Space(5);
				}
				catch(Exception e)
				{
					Morph_Util.Logger.LogError(e);
				}

				if(vertical)
					GUILayout.EndVertical();
				else
					GUILayout.EndHorizontal();
			});
		}

		private static string[] LastControlsList = null;
		public static string[] ControlsList
		{
			get
			{
				var val = (((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) || !cfg.preferCardMorphDataMaker.Value) ?
					Instance?.controlCategories?.Keys.ToList() :
					(GetFuncCtrlOfType<CharaMorpher_Controller>()?.FirstOrNull()?.controls?.all?.Keys?.ToList()
					?? Instance?.controlCategories?.Keys.ToList()))
					.Attempt((k) => k.LastIndexOf(strDiv) >= 0 ? k.Substring(0, k.LastIndexOf(strDiv)) : throw new Exception())
					.ToArray();
				Array.Sort(val ?? (val = new string[] { }));

				if(val != LastControlsList)
					OnInternalControlListChanged.Invoke(val);

				LastControlsList = val;
				return val;
			}
		}

		static int selectedMod = -1;
		static Vector2 scrollview = Vector2.zero;
		static readonly Func<int> mySelectionListDrawerFunc =
			GUILayoutDropdownDrawer(
							scrollHeight: 93 * .5f,
							content: (ctn, index) => new GUIContent { text = $"Current Slot: {cfg.currentControlSetName.Value ?? "None"} " },
							listUpdate: (old) => ControlsList,
							onSelect: (selected) => SwitchControlSet(ControlsList, selected));

		internal static void MySelectionListDrawer(ConfigEntryBase entry)
		{

			try
			{
				GUILayout.BeginVertical();

				selectedMod = mySelectionListDrawerFunc();

				var addPress = GUILayout.Button(new GUIContent { text = "Add New Slot", tooltip = "Add a new slot to the controls list" }, GUILayout.ExpandWidth(true));
				var removePress = GUILayout.Button(new GUIContent { text = "Remove Current Slot", tooltip = "Remove the currently selected control from the list" }, GUILayout.ExpandWidth(true));

				if(addPress)
				{
					//Morph_Util.Logger.LogDebug("Trying to add a comp from drawer");
					AddNewSetting();
					//UpdateDropdown(ControlsList);
					//	Morph_Util.Logger.LogDebug("execution got to this point");
				}

				if(removePress)
				{
					//switch control before deletion
					int tmp = selectedMod;
					if(selectedMod >= ControlsList.Length - 1)
						tmp = ControlsList.Length - 2;

					RemoveCurrentSetting(ControlsList[selectedMod] + strDiv);

					selectedMod = SwitchControlSet(ControlsList, tmp);

				}
				GUILayout.EndVertical();
			}
			catch(Exception e)
			{
				Morph_Util.Logger.LogError(e);
			}

		}

		/// <summary>
		/// Crates Image Texture based on path
		/// </summary>
		/// <param name="path">directory path to image (i.e. C:/path/to/image.png)</param>
		/// <param name="data">raw image data that will be read instead of path if not null or empty</param>
		/// <returns>An Texture2D created from path if passed, else a black texture</returns>
		public static Texture2D CreateTexture(this string path, byte[] data = null) =>
			(!data.IsNullOrEmpty() || !File.Exists(path)) ?
			data?.LoadTexture(TextureFormat.RGBA32) ?? Texture2D.blackTexture :
			File.ReadAllBytes(path)?.LoadTexture(TextureFormat.RGBA32) ??
			Texture2D.blackTexture;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gui"></param>
		/// <param name="act"></param>
		/// <returns></returns>
		public static T OnGUIExists<T>(this T gui, UnityAction<T> act) where T : BaseGuiEntry
		{
			if(gui == null) return gui;

			IEnumerator func(T gui1, UnityAction<T> act1)
			{
				if(!gui1.Exists)
					yield return new WaitUntil(() => gui1.Exists);//the thing needs to exist first

				act1(gui);

				yield break;
			}
			Instance.StartCoroutine(func(gui, act));

			return gui;
		}

		public static readonly CurrentSaveLoadManager saveLoad = new CurrentSaveLoadManager();
		public static PluginData SaveExtData(this CharaMorpher_Controller ctrl) => saveLoad.Save(ctrl);
		public static PluginData LoadExtData(this CharaMorpher_Controller ctrl, PluginData data = null)
		{
			//ImageConversion.LoadImage
			if(cfg.debug.Value)
				Logger.LogDebug("loading extended data...");
			var tmp = CharaMorpher_GUI.MorphLoadToggle ? saveLoad.Load(ctrl, data) : null;
			if(cfg.debug.Value)
				Logger.LogDebug("extended data loaded");

			string path = Path.Combine(
				cfg.charDir.Value.MakeDirPath(),
				cfg.imageName.Value.MakeDirPath());


			//if(!check)
			//	tmp = null;

			if(cfg.debug.Value)
				Logger.LogDebug($"Load check status: {ctrl.IsUsingExtMorphData}");

			var ctrler = (CharaMorpher_Controller)ctrl;
			OnNewTargetImage.Invoke(path, ctrl.IsUsingExtMorphData ? ctrler?.m_data2?.main?.pngData : null);

			return tmp;
		}
		public static PluginData Copy(this PluginData source)
		{
			return new PluginData
			{
				version = source.version,
				data = source.data.ToDictionary((p) => p.Key, (p) => p.Value),
			};
		}


		public static Graphic GetTextComponentInChildren(this GameObject obj)
		{
			return (Graphic)obj?.GetComponentInChildren<TMP_Text>() ??
			 obj?.GetComponentInChildren<Text>();
		}
		public static Graphic GetTextComponentInChildren(this UIBehaviour obj)
		{
			return obj.gameObject.GetTextComponentInChildren();
		}

		/// <summary>
		/// gets the text of the first Text or TMP_Text component in a game object or it's children.
		///  If no component return null. 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string GetTextFromTextComponent(this GameObject obj)
			=>
			obj?.GetComponentInChildren<TMP_Text>()?.text ??
			obj?.GetComponentInChildren<Text>()?.text ?? null;

		/// <summary>
		/// gets the text of the first Text or TMP_Text component in a game object or it's children.
		///  If no component return null. 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string GetTextFromTextComponent(this UIBehaviour obj)
			=> obj.gameObject.GetTextFromTextComponent();

		/// <summary>
		/// sets the text of the first Text or TMP_Text component in a game object or it's children.
		///  If no component does nothing. 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static void SetTextFromTextComponent(this GameObject obj, string txt) =>
		((Component)obj?.GetComponentInChildren<TMP_Text>() ??
			obj?.GetComponentInChildren<Text>())?
			.SetTextFromTextComponent(txt);

		/// <summary>
		/// sets the text of the first Text or TMP_Text component in a game object or it's children.
		///  If no component does nothing. 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static void SetTextFromTextComponent(this Component obj, string txt)
		{
			Component comp;
			if(comp = obj?.GetComponentInChildren<TMP_Text>())
				((TMP_Text)comp).text = (txt);
			else if(comp = obj?.GetComponentInChildren<Text>())
				((Text)comp).text = (txt);
		}

		public static Func<int> GUILayoutDropdownDrawer(Func<string[], int, GUIContent> content, string[] items = null, int initSelection = 0, float scrollHeight = 150, Func<string[], string[]> listUpdate = null, Func<int, int> modSelected = null, Func<int, int> onSelect = null, bool vertical = true)
		{
			int selectedItem = initSelection;
			//var select = selectedItem;
			bool selectingItem = false;
			Vector2 scrollpos = Vector2.zero;


			return new Func<int>(() =>
			{
				void BeginDirection(bool invert = false, params GUILayoutOption[] opt)
				{
					if(vertical && !invert)
						GUILayout.BeginVertical(opt);
					else
						GUILayout.BeginHorizontal(opt);
				}

				void EndDirection(bool invert = false)
				{
					if(vertical && !invert)
						GUILayout.EndVertical();
					else
						GUILayout.EndHorizontal();
				}


				BeginDirection();

				items = listUpdate?.Invoke(items) ?? items;

				if(!items?.InRange(selectedItem) ?? false)
					selectedItem = Math.Max(0, Math.Min
					(items.Length - 1, selectedItem));

				if(!items?.InRange(selectedItem) ?? true)
				{

					EndDirection();
					return -1;
				}

				try
				{
					GUILayout.Space(3);
					bool btn;
					//int maxWidth = 350, maxHeight = 200;
					if(items.Length > 0)
					{
						var tmpcontent = content?.Invoke(items, selectedItem);
						tmpcontent.text += " ▼";//▼▾
						if((btn = GUILayout.Button(tmpcontent ?? new GUIContent("▼"),
							 GUILayout.ExpandWidth(vertical), GUILayout.ExpandHeight(!vertical))) || selectingItem)
						{
							selectingItem = !(btn && selectingItem);//if dropdown btn was pressed

							var rec = new Rect(GUILayoutUtility.GetLastRect());
							GUILayout.Space(scrollHeight);
							rec.y += rec.height;

							var recContent = new Rect(rec) { height = items.Length * (rec.height) };
							rec.height = GUILayoutUtility.GetLastRect().height;


							scrollpos = GUI.BeginScrollView(rec, scrollpos, recContent, false, false, GUIStyle.none, GUI.skin.verticalScrollbar
								//GUILayout.Height(rec.height),
								//GUILayout.ExpandWidth(true),
								//GUILayout.ExpandHeight(true)
								);

							recContent.x += (rec.width * .15f * .5f);
							recContent.width *= .85f;
							var select = GUI.SelectionGrid(recContent, selectedItem, items, 1
							  //GUILayout.Height(recView.height),
							  //GUILayout.ExpandWidth(true),
							  //GUILayout.ExpandHeight(true)
							  );


							if(select != selectedItem) { selectingItem = false; select = onSelect != null ? onSelect(select) : select; }
							selectedItem = select;

							GUI.EndScrollView();

						}
					}

					selectedItem = modSelected?.Invoke(selectedItem) ?? selectedItem;

					GUILayout.Space(5);
				}
				catch(Exception e)
				{
					CharaMorpher_Core.Logger.LogError(e);
				}

				EndDirection();

				return selectedItem;
			});
		}

		public static GameObject ScaleToParent2D(this GameObject obj, float pwidth = 1, float pheight = 1, bool changewidth = true, bool changeheight = true)
		{
			RectTransform rectTrans = null;

			rectTrans = obj?.GetComponent<RectTransform>();

			if(rectTrans == null) return obj;

			//var rectTrans = par.GetComponent<RectTransform>();
			rectTrans.anchorMin = new Vector2(
				changewidth ? 0 + (1 - pwidth) : rectTrans.anchorMin.x,
				changeheight ? 0 + (1 - pheight) : rectTrans.anchorMin.y);
			rectTrans.anchorMax = new Vector2(
				changewidth ? 1 - (1 - pwidth) : rectTrans.anchorMax.x,
				changeheight ? 1 - (1 - pheight) : rectTrans.anchorMax.y);

			rectTrans.localPosition = Vector3.zero;//The location of this line matters

			rectTrans.offsetMin = new Vector2(
				changewidth ? 0 : rectTrans.offsetMin.x,
				changeheight ? 0 : rectTrans.offsetMin.y);
			rectTrans.offsetMax = new Vector2(
				changewidth ? 0 : rectTrans.offsetMax.x,
				changeheight ? 0 : rectTrans.offsetMax.y);
			//rectTrans.pivot = new Vector2(0.5f, 0.5f);

			return obj;
		}

		public static T ScaleToParent2D<T>(this T comp, float pwidth = 1, float pheight = 1, bool width = true, bool height = true) where T : Component
		{
			comp?.gameObject.ScaleToParent2D(pwidth: pwidth, pheight: pheight, changewidth: width, changeheight: height);
			return comp;
		}

		public static IEnumerable<T> GetComponentsInChildren<T>(this GameObject obj, int depth) =>
		 obj.GetComponentsInChildren<T>().Attempt((v1) =>
		(((Component)(object)v1).transform.HierarchyLevelIndex() - obj.transform.HierarchyLevelIndex()) < (depth + 1) ?
		v1 : (T)(object)((T)(object)null).GetType());
		public static IEnumerable<T> GetComponentsInChildren<T>(this Component obj, int depth) =>
			obj.gameObject.GetComponentsInChildren<T>(depth);

		public static int HierarchyLevelIndex(this Transform obj) => obj.parent ? obj.parent.HierarchyLevelIndex() + 1 : 0;
		public static int HierarchyLevelIndex(this GameObject obj) => obj.transform.HierarchyLevelIndex();


		public static T AddToCustomGUILayout<T>(this T gui, bool topUI = false, float pWidth = -1, float viewpercent = -1, bool newVertLine = true) where T : BaseGuiEntry
		{
			gui.OnGUIExists(g =>
			{
				Instance.StartCoroutine(g.AddToCustomGUILayoutCO
				(topUI, pWidth, viewpercent, newVertLine));
			});
			return gui;
		}

		static IEnumerator AddToCustomGUILayoutCO<T>(this T gui, bool topUI = false, float pWidth = -1, float viewpercent = -1, bool newVertLine = true) where T : BaseGuiEntry
		{
			if(cfg.debug.Value) Logger.LogDebug("moving object");

			yield return new WaitWhile(() => gui?.ControlObject?.GetComponentInParent<ScrollRect>()?.transform == null);

			//	newVertLine = horizontal ? newVertLine : true;
#if HONEY_API
			if(gui is MakerText)
			{
				var piv = (Vector2)gui.ControlObject?
					.GetComponentInChildren<Text>()?
					.rectTransform.pivot;
				piv.x = -.5f;
				piv.y = 1f;
			}
#endif

			var ctrlObj = gui.ControlObject;

			var scrollRect = ctrlObj.GetComponentInParent<ScrollRect>();
			var par = ctrlObj.GetComponentInParent<ScrollRect>().transform;


			if(cfg.debug.Value) Logger.LogDebug("Parent: " + par);


			//setup VerticalLayoutGroup
			var vlg = scrollRect.gameObject.GetOrAddComponent<VerticalLayoutGroup>();

#if HONEY_API
			vlg.childAlignment = TextAnchor.UpperLeft;
#else
			vlg.childAlignment = TextAnchor.UpperCenter;
#endif
			var pad = 10;//(int)cfg.unknownTest.Value;//10
			vlg.padding = new RectOffset(pad, pad + 5, 0, 0);
			vlg.childControlWidth = true;
			vlg.childControlHeight = true;
			vlg.childForceExpandWidth = true;
			vlg.childForceExpandHeight = false;

			//This fixes the KOI_API rendering issue & enables scrolling over viewport (not elements tho)
			//Also a sizing issue in Honey_API
#if KOI_API
			scrollRect.GetComponent<Image>().sprite = scrollRect.content.GetComponent<Image>()?.sprite;
			scrollRect.GetComponent<Image>().color = (Color)scrollRect.content.GetComponent<Image>()?.color;


			scrollRect.GetComponent<Image>().enabled = true;
			scrollRect.GetComponent<Image>().raycastTarget = true;
			var img = scrollRect.content.GetComponent<Image>();
			if(!img)
				img = scrollRect.viewport.GetComponent<Image>();
			img.enabled = false;
#elif HONEY_API
			//		scrollRect.GetComponent<RectTransform>().sizeDelta =
			//		  scrollRect.transform.parent.GetComponentInChildren<Image>().rectTransform.sizeDelta;
#endif

			//Setup LayoutElements 
			scrollRect.verticalScrollbar.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
			scrollRect.content.GetOrAddComponent<LayoutElement>().ignoreLayout = true;

			var viewLE = scrollRect.viewport.GetOrAddComponent<LayoutElement>();
#if !KK
			viewLE.layoutPriority = 1;
#endif
			viewLE.minWidth = -1;
			viewLE.flexibleWidth = -1;
			gui.ResizeCustomUIViewport(viewpercent);


			Transform layoutObj = null;
			//Create  LayoutElement
			//if(horizontal)
			{
				//Create Layout Element GameObject
				par = newVertLine ?
					GameObject.Instantiate<GameObject>(new GameObject("LayoutElement"), par)?.transform :
					par.GetComponentsInChildren<HorizontalLayoutGroup>(2)
					.LastOrNull((elem) => elem.GetComponent<HorizontalLayoutGroup>())?.transform.parent ??
					GameObject.Instantiate<GameObject>(new GameObject("LayoutElement"), par)?.transform;

				layoutObj = par = par.gameObject.GetOrAddComponent<RectTransform>().transform;//May need this line (I totally do)


				//calculate base GameObject sizeing
				var ele = par.GetOrAddComponent<LayoutElement>();
				ele.minWidth = -1;
				ele.minHeight = -1;
				ele.preferredHeight = Math.Max(ele?.preferredHeight ?? -1, ctrlObj.GetOrAddComponent<LayoutElement>()?.minHeight ?? ele?.preferredHeight ?? -1);
				ele.preferredWidth =
#if HONEY_API
				scrollRect.GetComponent<RectTransform>().rect.width;
#else
				//viewLE.minWidth;
				0;
#endif

				par.GetComponentInParent<VerticalLayoutGroup>().CalculateLayoutInputHorizontal();
				par.GetComponentInParent<VerticalLayoutGroup>().CalculateLayoutInputVertical();


				//Create and Set Horizontal Layout Settings

				par = par.GetComponentsInChildren<HorizontalLayoutGroup>(2)?
					.FirstOrNull((elem) => elem.gameObject.GetComponent<HorizontalLayoutGroup>())?.transform ??
					GameObject.Instantiate<GameObject>(new GameObject("HorizontalLayoutGroup"), par)?.transform;
				par = par.gameObject.GetOrAddComponent<RectTransform>().transform;//May need this line (I totally do)


				var layout = par.GetOrAddComponent<HorizontalLayoutGroup>();


				layout.childControlWidth = true;
				layout.childControlHeight = true;
				layout.childForceExpandWidth = true;
				layout.childForceExpandHeight = true;
				layout.childAlignment = TextAnchor.MiddleCenter;

				par?.ScaleToParent2D();

			}


			if(cfg.debug.Value) Logger.LogDebug("setting as first/last");

			//remove extra LayoutElements
			var rList = ctrlObj.GetComponents<LayoutElement>();
			for(int a = 1; a < rList.Length; ++a)
				GameObject.DestroyImmediate(rList[a]);

			//change child layoutelements
			foreach(var val in ctrlObj.GetComponentsInChildren<LayoutElement>())
				if(val.gameObject != ctrlObj)
					val.flexibleWidth = val.minWidth = val.preferredWidth = -1;


			//edit layoutgroups
			foreach(var val in ctrlObj.GetComponentsInChildren<HorizontalLayoutGroup>())
			//	if(val.gameObject != ctrlObj)
			{
				val.childControlWidth = true;
				val.childForceExpandWidth = true;

			}

			//Set this object's Layout settings
			ctrlObj.transform.SetParent(par, false);
			ctrlObj.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
			var apos = ctrlObj.GetComponent<RectTransform>().anchoredPosition; apos.x = 0;
			if(topUI)
			{
				if(layoutObj?.GetSiblingIndex() != scrollRect.viewport.transform.GetSiblingIndex() - 1)
					layoutObj?.SetSiblingIndex
						(scrollRect.viewport.transform.GetSiblingIndex());
			}
			else
				layoutObj?.SetAsLastSibling();

			//if(ctrlObj.GetComponent<LayoutElement>())
			//	GameObject.Destroy(ctrlObj.GetComponent<LayoutElement>());
			var thisLE = ctrlObj.GetOrAddComponent<LayoutElement>();
#if !KK
			thisLE.layoutPriority = 5;
#endif
			thisLE.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			bool check = thisLE.transform.childCount > 1 &&
				!thisLE.GetComponent<HorizontalOrVerticalLayoutGroup>();
			if(check)
			{
				var tmp = GameObject.Instantiate(new GameObject(), thisLE.transform);
				var hlog = tmp.AddComponent<HorizontalLayoutGroup>();
				hlog.childAlignment = TextAnchor.MiddleLeft;
				hlog.childControlHeight = true;
				hlog.childControlWidth = false;
				hlog.childForceExpandHeight = false;
				hlog.childForceExpandWidth = true;

				for(int a = 0; a < thisLE.transform.childCount; ++a)
					if(thisLE.transform.GetChild(a) != tmp.transform)
						thisLE.transform.GetChild(a--).SetParent(tmp.transform);

			}
			if(thisLE.transform.childCount == 1)
				thisLE.transform.GetChild(0).ScaleToParent2D();


			thisLE.flexibleWidth = -1;
			thisLE.flexibleHeight = -1;
			thisLE.minWidth = -1;
			//thisLE.minHeight = -1;

			thisLE.preferredWidth =
#if HONEY_API
				  pWidth > 0 ? scrollRect.rectTransform.rect.width * pWidth : -1;
#else
			//	horizontal && horiScale > 0 ? viewLE.minWidth * horiScale : -1;
			0;
#endif
			//thisLE.preferredHeight = ctrlObj.GetComponent<RectTransform>().rect.height;


			//Reorder Scrollbar
			if(!topUI)
			{
				scrollRect.verticalScrollbar?.transform.SetAsLastSibling();
				scrollRect.horizontalScrollbar?.transform.SetAsLastSibling();
			}

			vlg.SetLayoutVertical();
			LayoutRebuilder.MarkLayoutForRebuild(scrollRect.GetComponent<RectTransform>());
			yield break;
		}

		static Coroutine resizeco;
		public static void ResizeCustomUIViewport<T>(this T template, float viewpercent = -1) where T : BaseGuiEntry
		{
			if(viewpercent >= 0 && cfg.makerViewportUISpace.Value != viewpercent)
				cfg.makerViewportUISpace.Value = viewpercent;
			viewpercent = cfg.makerViewportUISpace.Value;

			if(template != null)
				template.OnGUIExists((gui) =>
				{
					IEnumerator func()
					{

						var ctrlObj = gui?.ControlObject;
						if(ctrlObj == null) yield break;

						yield return new WaitUntil(() =>
						ctrlObj?.GetComponentInParent<ScrollRect>() != null);

						var scrollRect = ctrlObj?.GetComponentInParent<ScrollRect>();

						var viewLE = scrollRect.viewport.GetOrAddComponent<LayoutElement>();
						float vHeight = Mathf.Abs(scrollRect.rectTransform.rect.height);
						viewLE.minHeight = vHeight * viewpercent;

						LayoutRebuilder.MarkLayoutForRebuild(scrollRect.rectTransform);
					}

					if(resizeco != null) Instance.StopCoroutine(resizeco);
					resizeco = Instance.StartCoroutine(func());
				});

		}


	}

	#region User Classes
	public class MorphSliderData
	{
		public MorphSliderData() { TypeCreator(); }
		public MorphSliderData(string dataName, float data = 0, MorphCalcType calc = MorphCalcType.LINEAR, bool isABMX = false)
		{
			this.dataName = dataName;
			this.data = data;
			this.calcType = calc;
			this.isABMX = isABMX;

			TypeCreator();
		}
		public string dataName;
		public float data = 0;
		public bool isABMX = false;
		public MorphCalcType calcType = MorphCalcType.LINEAR;

		static void TypeCreator()
		{
			//Adding new Type for Config list!
			string splitstr = "\\:/";
			if(!TomlTypeConverter.CanConvert(typeof(MorphSliderData)))
				TomlTypeConverter.AddConverter(typeof(MorphSliderData),
					new TypeConverter
					{
						ConvertToObject = (s, t) =>
						{
							var vals = s.Split(new string[] { splitstr }, StringSplitOptions.None);
							if(vals.Length > 4)
								for(int a = 1; a <= (vals.Length - 4); ++a)
									vals[0] += vals[a];
							if(vals.Length < 3)
								return new MorphSliderData
								{
									dataName = "",
									data = float.TryParse(vals[0], out var result1) ? result1 : 0.0f,
									calcType = int.TryParse(vals[0], out var result2) ? (MorphCalcType)result2 : MorphCalcType.LINEAR,
								};
							return new MorphSliderData
							{
								dataName = vals[0],
								data = float.Parse(vals[1]) * 0.01f,
								calcType = (MorphCalcType)int.Parse(vals[2]),
								isABMX = bool.Parse(vals.Length == 4 ? vals[3] : "false"),
							};
						},

						ConvertToString = (o, t) =>
						{
							var val = (MorphSliderData)o;

							return
							$"{val.dataName}{splitstr}{val.data * 100}{splitstr}" +
							$"{(int)val.calcType}{splitstr}{val.isABMX}";
						}
					});
		}

		public MorphSliderData SetData(float data) { this.data = data; return this; }
		public MorphSliderData SetCalcType(MorphCalcType calcType) { this.calcType = calcType; return this; }

		public MorphSliderData Clone() =>
			new MorphSliderData()
			{
				dataName = dataName + "",
				data = data + 0,
				calcType = calcType + 0,
				isABMX = isABMX
			};

		public void Copy(MorphSliderData src)
		{
			var tmp = src.Clone();
			dataName = tmp.dataName;
			data = tmp.data;
			calcType = tmp.calcType;
			isABMX = tmp.isABMX;
		}
	}

	public class OnValueChange<T> : UnityEvent<T> { }
	public class OnControlSetValueChange : UnityEvent<string[]> { }
	public class OnNewImage : UnityEvent<string, byte[]> { }

	public class DependencyInfo<T> where T : BaseUnityPlugin
	{
		public DependencyInfo(Version minTargetVer = null, Version maxTargetVer = null)
		{
			plugin = (T)GameObject.FindObjectOfType(typeof(T));
			Exists = plugin != null;
			MinTargetVersion = minTargetVer ?? new Version();
			MaxTargetVersion = maxTargetVer ?? new Version();
			InTargetVersionRange = Exists &&
				((CurrentVersion = plugin?.Info.Metadata.Version
				?? new Version()) >= MinTargetVersion);

			if(maxTargetVer != null && maxTargetVer >= MinTargetVersion)
				InTargetVersionRange &= Exists && (CurrentVersion <= MaxTargetVersion);
		}

		/// <summary>
		/// plugin reference
		/// </summary>
		public readonly T plugin = null;
		/// <summary>
		/// does the mod exist
		/// </summary>
		public bool Exists { get; } = false;
		/// <summary>
		/// Current version matches or exceeds the min target mod version. 
		/// if a max is set it will also make sure the mod is within range.
		/// </summary>
		public bool InTargetVersionRange { get; } = false;
		/// <summary>
		/// min version this mod expects
		/// </summary>
		public Version MinTargetVersion { get; } = null;
		/// <summary>
		/// max version this mod expects
		/// </summary>
		public Version MaxTargetVersion { get; } = null;
		/// <summary>
		/// version that is actually downloaded in the game
		/// </summary>
		public Version CurrentVersion { get; } = null;

		public void PrintExistsMsg()
		{

		}

		public override string ToString()
		{
			return
				$"Plugin Name: {plugin?.Info.Metadata.Name ?? "Null"}\n" +
				$"Current version: {CurrentVersion?.ToString() ?? "Null"}\n" +
				$"Min Target Version: {MinTargetVersion}\n" +
				$"Max Target Version: {MaxTargetVersion}\n";
		}
	}


	/// <summary>
	/// utility to bring process to foreground (used for the file select)
	/// </summary>
	public class ForeGrounder
	{
		static IntPtr ptr = IntPtr.Zero;

		/// <summary>
		/// set window to go back to
		/// </summary>
		public static void SetCurrentForground()
		{
			ptr = GetActiveWindow();

			//	Morph_Util.Logger.LogDebug($"Process ptr 1 set to: {ptr}");
		}

		/// <summary>
		/// reverts back to last window specified by SetCurrentForeground
		/// </summary>
		public static void RevertForground()
		{
			//	Morph_Util.Logger.LogDebug($"process ptr: {ptr}");

			if(ptr != IntPtr.Zero)
				SwitchToThisWindow(ptr, true);
		}



		[DllImport("user32.dll")]
		static extern IntPtr GetActiveWindow();
		[DllImport("user32.dll")]
		static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
	}
	#endregion
}