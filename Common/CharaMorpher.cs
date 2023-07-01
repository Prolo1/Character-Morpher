using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
//using BepInEx.Preloader.Patching;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;


using KKAPI;
using UnityEngine.Events;
//using UniRx;
using UnityEngine;
using System.Runtime.InteropServices;
using KKABMX.Core;
using Manager;


using KKAPI.Utilities;
using KKAPI.Maker.UI;



#if HONEY_API

using AIChara;
#endif

using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.CharaMorpherController;
using static Character_Morpher.CharaMorpherGUI;
using static Character_Morpher.MorphUtil;
using TMPro;
using UnityEngine.UI;
using UniRx;

/***********************************************
  Features:

 * Morph body features
 * Morph face features     
 * Morph ABMX body features
 * Morph ABMX face features
 * Added QoL file explorer search for morph target in maker
 * Can choose to enable/disable in-game use (this affects all but male character[s])
 * Can choose to enable/disable use in male maker

  Planned:                                           
 * Save morph changes to card (w/o changing card parameters)
************************************************/


/**
 */
namespace Character_Morpher
{


	// Specify this as a plugin that gets loaded by BepInEx
	[BepInPlugin(GUID, ModName, Version)]
	// Tell BepInEx that we need KKAPI to run, and that we need the latest version of it.
	// Check documentation of KoikatuAPI.VersionConst for more info.
	[BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
	// Tell BepInEx that we need KKABMX to run, and that we need the latest version of it.
	// Check documentation of KoikatuAPI.VersionConst for more info.
	[BepInDependency(KKABMX.Core.KKABMX_Core.GUID, KKABMX.Core.KKABMX_Core.Version)]
	// Tell BepInEx that we need ExtendedSave to run, and that we need the latest version of it.
	// Check documentation of KoikatuAPI.VersionConst for more info.
	[BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID, ExtendedSave.Version)]
	public partial class CharaMorpher_Core : BaseUnityPlugin
	{

		// Expose both your GUID and current version to allow other plugins to easily check for your presence and version, for example by using the BepInDependency attribute.
		// Be careful with public const fields! Read more: https://stackoverflow.com/questions/55984
		// Avoid changing GUID unless absolutely necessary. Plugins that rely on your plugin will no longer recognize it, and if you use it in function controllers you will lose all data saved to cards before the change!
		public const string ModName = "Character Morpher";
		public const string GUID = "prolo.chararmorpher";//never change this
		public const string Version = "1.0.0";

		public const string strDivider = ":";
		public const string defaultStr = "(Default)" + strDivider;

		internal static CharaMorpher_Core Instance;
		internal static new ManualLogSource Logger;
		internal static OnNewImage OnNewTargetImage = new OnNewImage();
		internal static OnValueChange OnInternalSliderValueChange = new OnValueChange();
		internal static OnControlSetValueChange OnInternalControlSetAdded = new OnControlSetValueChange();

		public Dictionary<string, List<MorphSliderData>> controlCategories = new Dictionary<string, List<MorphSliderData>>();
		public static MorphConfig cfg;
		public struct MorphConfig
		{
			//ABMX
			public ConfigEntry<bool> enableABMX { set; get; }


			//Main
			public ConfigEntry<bool> enable { set; get; }
			public ConfigEntry<KeyboardShortcut> enableKey { set; get; }
			public ConfigEntry<KeyboardShortcut> prevControlKey { set; get; }
			public ConfigEntry<KeyboardShortcut> nextControlKey { set; get; }
			public ConfigEntry<bool> enableInMaleMaker { get; set; }
			public ConfigEntry<bool> enableInGame { set; get; }
			public ConfigEntry<bool> linkOverallABMXSliders { set; get; }
			public ConfigEntry<bool> enableCalcTypes { set; get; }
			public ConfigEntry<bool> saveAsMorphData { set; get; }
			public ConfigEntry<bool> preferCardMorphDataMaker { set; get; }
			public ConfigEntry<bool> preferCardMorphDataGame { set; get; }
			public ConfigEntry<bool> onlyMorphCharWithDataInGame { set; get; }

			public ConfigEntry<string> pathBtn { set; get; }
			public ConfigEntry<string> charDir { set; get; }
			public ConfigEntry<string> imageName { set; get; }
			public ConfigEntry<uint> sliderExtents { set; get; }
			public ConfigEntry<string> currentControlName { set; get; }
			public ConfigEntry<string> controlSets { set; get; }


			//Advanced
			public Dictionary<string, Dictionary<string, ConfigEntry<MorphSliderData>>> defaults { set; get; }
			//public Dictionary<string, Dictionary<string, ConfigEntry<Tuple<string, int>>>> defaultModes { set; get; }

			//Advanced (show up below main) 
			public ConfigEntry<bool> debug { set; get; }
			public ConfigEntry<bool> resetOnLaunch { set; get; }
			public ConfigEntry<bool> hideAdvIndexes { set; get; }
			public ConfigEntry<bool> easyMorphBtnOverallSet { set; get; }
			public ConfigEntry<bool> easyMorphBtnEnableDefaulting { set; get; }
			public ConfigEntry<bool> oldControlsConversion { set; get; }


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


		void Awake()
		{
			Instance = this;
			Logger = base.Logger;

			ForeGrounder.SetCurrentForground();

			//Adding new Type for Config list!
			string splitstr = "\\:/";
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


			string femalepath = Path.Combine(Paths.GameRootPath, "/UserData/chara/female/");

			int bodyBoneAmount = ChaFileDefine.cf_bodyshapename.Length - 1;
			int faceBoneAmount = ChaFileDefine.cf_headshapename.Length - 1;
			//Logger.LogDebug($"Body bones amount: {bodyBoneAmount}");
			//Logger.LogDebug($"Face bones amount: {faceBoneAmount}");

			//	Instance.Config.Reload();//get controls from disk

			int index = 0;//easier to input index order values
			string main = "__Main__", advanced = "_Advanced_";
			var saveCfgAuto =
				Instance.Config.SaveOnConfigSet;
			Instance.Config.SaveOnConfigSet = false;

			cfg = new MorphConfig
			{
				enable = Config.Bind(main, "Enable", false, new ConfigDescription("Allows the plugin to run (may need to reload character/scene if results are not changing)", null, new ConfigurationManagerAttributes { Order = --index, })),

				enableABMX = Config.Bind(main, "Enable ABMX", true, new ConfigDescription("Allows ABMX to be affected", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInMaleMaker = Config.Bind(main, "Enable in Male Maker", false, new ConfigDescription("Allows the plugin to run while in male maker (enable before launching maker)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInGame = Config.Bind(main, "Enable in Game", true, new ConfigDescription("Allows the plugin to run while in main game", null, new ConfigurationManagerAttributes { Order = --index })),
				linkOverallABMXSliders = Config.Bind(main, "Link Overall Base Sliders to ABMX Sliders", true, new ConfigDescription("Allows ABMX overall sliders to be affected by their base counterpart (i.e. Body:50% * ABMXBody:100% = ABMXBody:50%)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableCalcTypes = Config.Bind(main, "Enable Calculation Types", false, new ConfigDescription("Enables quadratic mode where value gets squared (i.e. 1.2 = 1.2^2 = 1.44)", null, new ConfigurationManagerAttributes { Order = --index })),
				saveAsMorphData = Config.Bind(main, "Save As Morph Data", false,
				new ConfigDescription("Allows the card to save using morph data. " +
				"If true, card is set to default values and Morph Ext. data will be saved to card while keeping any accessory/clothing changes, " +
				"else the card is saved normally w/o Morph Ext. data and saved as seen (must be set before saving)", null, new ConfigurationManagerAttributes { Order = --index })),
				preferCardMorphDataMaker = Config.Bind(main, "Prefer Card Morph Data (Maker)", true, new ConfigDescription("Allows the mod to use data from card instead of default data (If false card uses default Morph card data)", null, new ConfigurationManagerAttributes { Order = --index })),
				preferCardMorphDataGame = Config.Bind(main, "Prefer Card Morph Data (Game)", true, new ConfigDescription("Allows the card to use data from card instead of default data (If false card uses default Morph card data)", null, new ConfigurationManagerAttributes { Order = --index })),
				onlyMorphCharWithDataInGame = Config.Bind(main, "Only Morph Characters With Save Data (Game)", false, new ConfigDescription("Only allows cards that have morph data saved to it to be changed in game (If true all cards not saved with CharaMorph Data will not morph AT ALL!)", null, new ConfigurationManagerAttributes { Order = --index })),

				charDir = Config.Bind(main, "Directory Path", femalepath, new ConfigDescription("Directory where character is stored", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				imageName = Config.Bind(main, "Card Name", "sample.png", new ConfigDescription("The character card used to morph", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				sliderExtents = Config.Bind(main, "Slider Extents", 200u, new ConfigDescription("How far the slider values go above default (e.i. setting value to 10 gives values -10 -> 110)", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true })),
				enableKey = Config.Bind(main, "Toggle Enable Keybinding", new KeyboardShortcut(KeyCode.Return, KeyCode.RightShift), new ConfigDescription("Enable/Disable toggle button", null, new ConfigurationManagerAttributes { Order = --index })),
				prevControlKey = Config.Bind(main, "Prev. control Keybinding", new KeyboardShortcut(), new ConfigDescription("Switch to the prev. control set", null, new ConfigurationManagerAttributes { Order = --index })),
				nextControlKey = Config.Bind(main, "Next control Keybinding", new KeyboardShortcut(), new ConfigDescription("Switch to the next control set", null, new ConfigurationManagerAttributes { Order = --index })),
				pathBtn = Config.Bind(main, "Set Morph Target", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = --index, HideDefaultButton = true, CustomDrawer = MorphUtil.MyImageButtonDrawer, ObjToStr = (o) => "", StrToObj = (s) => null })),
				currentControlName = Config.Bind(main, "Current Control Name", defaultStr, new ConfigDescription("", tags: new ConfigurationManagerAttributes { Order = --index, Browsable = false, })),
				controlSets = Config.Bind(main, "Control Sets", "", new ConfigDescription("", tags: new ConfigurationManagerAttributes { Order = --index, HideDefaultButton = true, CustomDrawer = MorphUtil.MySelectionListDrawer, ObjToStr = (o) => "", StrToObj = (s) => null })),


				//you don't need to see this in game
				defaults = new Dictionary<string, Dictionary<string, ConfigEntry<MorphSliderData>>>(),
				//defaultModes = new Dictionary<string, Dictionary<string, ConfigEntry<Tuple<string, int>>>>(),

				//Advanced
				debug = Config.Bind(advanced, "Debug Logging", false, new ConfigDescription("Allows debug logs to be written to the log file", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),
				resetOnLaunch = Config.Bind(advanced, "Reset On Launch", true, new ConfigDescription("Will reset advanced values to defaults after launch", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),
				hideAdvIndexes = Config.Bind(advanced, "Hide Index Settings", true, new ConfigDescription("Will hide the index settings below these ones", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),

				easyMorphBtnOverallSet = Config.Bind(advanced, "Enable Easy Morph Button Overall Set", true, new ConfigDescription("Sets the overall sliders whenever an Easy Morph button is pressed, everything else otherwise", null, new ConfigurationManagerAttributes { Order = --index, Browsable = false, IsAdvanced = true })),
				easyMorphBtnEnableDefaulting = Config.Bind(advanced, "Enable Easy Morph Defaulting", true, new ConfigDescription("Defaults everything not set by Easy Morph button to 100%", null, new ConfigurationManagerAttributes { Order = --index, Browsable = false, IsAdvanced = true })),
				oldControlsConversion = Config.Bind(advanced, "Convert Old Data (On Next Startup)", true,
				new ConfigDescription("This will attempt to convert old data (V1 and below) to the new current format. " +
				"This should happen automatically the first time but can be done again if need be",
				null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),
			};

			{   //Advanced

				cfg.debug.ConfigDefaulter();

				//Tests
				cfg.unknownTest = Config.Bind("_Testing_", "Unknown Test value", 20, new ConfigDescription("Used for whatever the hell I WANT (if you see this I forgot to take it out). RESETS ON GAME LAUNCH", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//	cfg.initialMorphTest = Config.Bind("_Testing_", "Init morph value", 1.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.multiUpdateEnableTest = Config.Bind("_Testing_", "Multi Update Enable value", 5u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.multiUpdateSliderTest = Config.Bind("_Testing_", "Multi Update Slider value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();


#if KOI_API
				//cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.initialMorphFaceTest = Config.Bind("_Testing_", "Init morph Face value", 0.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.initialMorphBodyTest = Config.Bind("_Testing_", "Init morph Body value", 0.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.reloadTest = Config.Bind("_Testing_", "Reload delay value", 22u, new ConfigDescription("Used to change the amount of frames to delay before loading. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
#elif HONEY_API
				cfg.initialMorphFaceTest = Config.Bind("_Testing_", "Init morph Face value", 0.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.initialMorphBodyTest = Config.Bind("_Testing_", "Init morph Body value", 0.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//cfg.multiUpdateEnableTest = Config.Bind("_Testing_", "Multi Update Enable value", 5u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				//cfg.multiUpdateSliderTest = Config.Bind("_Testing_", "Multi Update Slider value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.reloadTest = Config.Bind("_Testing_", "Reload delay value", 22u, new ConfigDescription("Used to change the amount of frames to delay before loading. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
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
						if(setting.Key.Key.Contains("Index"))
							if(setting.Key != cfg.hideAdvIndexes.Definition)
								(setting.Value.Description.Tags[0] as ConfigurationManagerAttributes).Browsable = !cfg.hideAdvIndexes.Value;
				}
				cfg.hideAdvIndexes.SettingChanged += (m, n) => { hideIndexes(); };
				hideIndexes();
			}

			Instance.Config.Save();
			Instance.Config.SaveOnConfigSet = saveCfgAuto;

			//populate defaults
			MorphUtil.PopulateDefaultSettings(defaultStr);
			MorphUtil.UpdateDefaultsList();

			//if it's needed
			if(cfg.unknownTest != null)
				cfg.unknownTest.SettingChanged += (m, n) =>
				{

				};

			//This works so it stays (and It just works 😂)
			void KeyUpdates()
			{
				IEnumerator CoKeyUpdates()
				{
					//I dare you to stop this
					yield return new WaitWhile(() =>
					{
						if(cfg.enableKey.Value.IsDown())
							cfg.enable.Value = !cfg.enable.Value;

						if(cfg.prevControlKey.Value.IsDown() || cfg.nextControlKey.Value.IsDown())
						{
							var tmp = SwitchControlSet(ControlsList, cfg.currentControlName.Value);

							if(cfg.prevControlKey.Value.IsDown())
								tmp--;
							if(cfg.nextControlKey.Value.IsDown())
								tmp++;

							tmp = tmp < 0 ? ControlsList.Length - 1 : tmp % ControlsList.Length;

							SwitchControlSet(ControlsList, tmp, false);//this is PEAK 3AM programming 🤣🤣

							//Logger.LogMessage("KEY WAS PRESSED!!!!");

							foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
								for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
									StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1));
						}

						return true;
					});
				}

				StartCoroutine(CoKeyUpdates());
			}
			KeyUpdates();

			cfg.charDir.SettingChanged += (m, n) =>
			{

				string path = Path.Combine(MorphUtil.MakeDirPath(cfg.charDir.Value), MorphUtil.MakeDirPath(cfg.imageName.Value));
				foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					if(File.Exists(path))
						if(ctrl.isInitLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
				}
			};

			cfg.imageName.SettingChanged += (m, n) =>
			{

				string path = Path.Combine(MorphUtil.MakeDirPath(cfg.charDir.Value), MorphUtil.MakeDirPath(cfg.imageName.Value));
				foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					if(File.Exists(path))
						if(ctrl.isInitLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
				}
			};

			cfg.currentControlName.SettingChanged += (m, n) =>
			{
				//	OnInternalSliderValueChange.Invoke();
				//	foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				//	{
				//		for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
				//			StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1, forceReset: !cfg.enable.Value));
				//	}
			};

			cfg.preferCardMorphDataMaker.SettingChanged += (m, n) =>
			{
				if(MakerAPI.InsideMaker)
					foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
						if(ctrl.isInitLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
			};

			cfg.preferCardMorphDataGame.SettingChanged += (m, n) =>
			{
				if(!MakerAPI.InsideMaker)
					foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
						if(ctrl.isInitLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
			};

			cfg.onlyMorphCharWithDataInGame.SettingChanged += (m, n) =>
			{
				if(!MakerAPI.InsideMaker)
					foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
						for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
							StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1, forceReset: !cfg.enable.Value));
			};

			cfg.enable.SettingChanged += (m, n) =>
			{
				foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1, forceReset: !cfg.enable.Value));
			};

			cfg.enableInGame.SettingChanged += (m, n) =>
			{
				foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
			};

			cfg.enableABMX.SettingChanged += (m, n) =>
			{
				foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
				}
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

						foreach(var ctrl in GetFuncCtrlOfType<CharaMorpherController>())
						{
							string name =
							(!cfg.preferCardMorphDataGame.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
							name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDivider), 0, name.Length));


							//Logger.LogDebug($"lastUCMD: {lastUCMD}");
							yield return new WaitWhile(() => ctrl.isReloading);

							var tmpCtrls =
							!cfg.preferCardMorphDataGame.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1);
							tmpCtrls.currentSet = ctrl.controls.currentSet;

							ctrl.controls.Copy(!lastUCMD ? ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1));

							SoftSave(lastUCMD, ctrl);

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


			if(StudioAPI.InsideStudio) return;

			/*
				Register your logic that depends on a character.
				A new instance of this component will be added to ALL characters in the game.
				The GUID will be used as the ID of the extended data saved to character
				cards, scenes and game saves, so make sure it's unique and do not change it!
			 */
			CharacterApi.RegisterExtraBehaviour<CharaMorpherController>(GUID);
			CharaMorpherGUI.Initialize();
			Hooks.Init();



		}
	}
	public class MorphSliderData
	{
		public MorphSliderData() { }
		public MorphSliderData(string dataName, float data = 0, MorphCalcType calc = MorphCalcType.LINEAR, bool isABMX = false)
		{
			this.dataName = dataName;
			this.data = data;
			this.calcType = calc;
			this.isABMX = isABMX;
		}
		public string dataName;
		public float data = 0;
		public bool isABMX = false;
		public MorphCalcType calcType = MorphCalcType.LINEAR;

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

	public class OnValueChange : UnityEvent { }
	public class OnControlSetValueChange : UnityEvent<string> { }
	public class OnNewImage : UnityEvent<string, byte[]> { }

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

			//	CharaMorpher_Core.Logger.LogDebug($"Process ptr 1 set to: {ptr}");
		}

		/// <summary>
		/// reverts back to last window specified by SetCurrentForground
		/// </summary>
		public static void RevertForground()
		{
			//	CharaMorpher_Core.Logger.LogDebug($"process ptr: {ptr}");

			if(ptr != IntPtr.Zero)
				SwitchToThisWindow(ptr, true);
		}



		[DllImport("user32.dll")]
		static extern IntPtr GetActiveWindow();
		[DllImport("user32.dll")]
		static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
	}

	public static class MorphUtil
	{
		public static ManualLogSource Logger { get => CharaMorpher_Core.Logger; }


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
			try
			{ return enu.Count() > 0 ? enu.First() : (T)(object)null; }
			catch { return (T)(object)null; }
		}     //I love loopholes 🤣
		public static T FirstOrNull<T>(this IEnumerable<T> enu, Func<T, bool> predicate)
		{
			try
			{ return enu.Count() > 0 ? enu.First(predicate) : (T)(object)null; }
			catch { return (T)(object)null; }
		}   //I love loopholes 🤣



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


						if(!((IDictionary<ConfigDefinition, ConfigEntryBase>)file).TryGetValue(key2, out var value))
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
				new[]{"Overall Voice","Vioce Default"},
				new[]{"Overall Skin Colour","Skin Default"},
				new[]{"Base Skin Colour","Base Skin Default"},
				new[]{"Sunburn Colour","Sunburn Default"},

				new[]{"Overall Body","Body  Default"},
				new[]{"Head","Head  Default"},
				new[]{"Boobs","Boobs Default"},
				new[]{"Boob Phys.","Boob Phys. Default"},
				new[]{"Torso","Torso Default"},
				new[]{"Arms","Arms  Default"},
				new[]{"Butt","Butt  Default"},
				new[]{"Legs","Legs  Default"},
				new[]{"Body Other","Body Other Default"},


				new[]{"Overall Face","Face  Default"},
				new[]{"Ears","Ears  Default"},
				new[]{"Eyes","Eyes  Default"},
				new[]{"Nose","Nose  Default"},
				new[]{"Mouth","Mouth Default"},
				new[]{"Face Other","Face Other Default"},

				new[]{"ABMX Overall Body","ABMX  Body Default"},
				new[]{"ABMX Boobs","ABMX  Boobs Default"},
				new[]{"ABMX Torso","ABMX  Torso Default"},
				new[]{"ABMX Arms","ABMX  Arms Default"},
				new[]{"ABMX Hands","ABMX  Hands Default"},
				new[]{"ABMX Butt","ABMX  Butt Default"},
				new[]{"ABMX Legs","ABMX  Legs Default"},
				new[]{"ABMX Feet","ABMX  Feet Default"},
				new[]{"ABMX Genitals","ABMX  Genitals Default"},
				new[]{"ABMX Body Other","ABMX  Body Other Default"},

				new[]{"ABMX Overall Head","ABMX  Head Default"},
				new[]{"ABMX Ears","ABMX  Ears Default"},
				new[]{"ABMX Eyes","ABMX  Eyes Default"},
				new[]{"ABMX Nose","ABMX  Nose Default"},
				new[]{"ABMX Mouth","ABMX  Mouth Default"},
				new[]{"ABMX Hair","ABMX  Hair Default"},
				new[]{"ABMX Head Other","ABMX  Head Other Default"},
			};


		/// <summary>
		/// 
		/// </summary>
		public static void UpdateDefaultsList()
		{
			var orphaned = Instance.Config.GetUnorderedOrphanedEntries("Defaults");
			var defList = orphaned.Attempt((k) => k.Key).ToList();

			orphaned = Instance.Config.GetUnorderedOrphanedEntries("Mode Defaults");
			var modeDefList = orphaned.Attempt((k) => k.Key);


			var saveCfgAuto =
				Instance.Config.SaveOnConfigSet;
			Instance.Config.SaveOnConfigSet = false;


			foreach(var val in defList)
			{
				var slotName = val.Key.Substring(0, val.Key.LastIndexOf(strDivider) + 1)?.Trim();
				var settingName = val.Key.Substring(val.Key.LastIndexOf(strDivider) + 1);

				//CharaMorpher_Core.Logger.LogDebug($"For start");
				//CharaMorpher_Core.Logger.LogDebug($"val.key: {val.Key}");

				if(!cfg.defaults.TryGetValue(slotName, out var tmp) && !slotName.IsNullOrEmpty())
					PopulateDefaultSettings(slotName);

				if(!cfg.defaults.TryGetValue(slotName, out tmp))
					cfg.defaults[slotName] = new Dictionary<string, ConfigEntry<MorphSliderData>>();

				if(!Instance.controlCategories.TryGetValue(slotName, out var tmp2))
					Instance.controlCategories[slotName] =
						new List<MorphSliderData>(Instance.controlCategories[defaultStr]);

				ConfigEntry<MorphSliderData> lastConfig = null;

				string convertStr = oldConversionList.FirstOrNull((a) => a[1] == settingName)?[0] ?? "";

				if(!cfg.defaults[slotName].Any((k) => k.Value.Definition.Key == val.Key))
					cfg.defaults[slotName].Add(
						convertStr,
						lastConfig = Instance.Config.Bind(val, Instance.controlCategories[slotName].
						AddNReturn(new MorphSliderData(convertStr)).Clone(),
						new ConfigDescription("", tags: new ConfigDescription("Set default value on maker startup", null,
						new ConfigurationManagerAttributes
						{
							Order = -Instance.controlCategories[slotName].Count
		,
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
							if(!(convertStr = oldConversionList.FirstOrNull((a) => a[1] == val.Key)?[0] ?? null).IsNullOrEmpty())
								cfg.defaults[defaultStr][convertStr]?.Value.SetData(lastConfig?.Value.data * .01f ?? 0);

						if(!(cfg.defaults[defaultStr].Values.ToList().
							FirstOrNull((v) => (v.Definition.Key + " Mode").Contains(val.Key)) == null))
							if(!(convertStr = oldConversionList.FirstOrNull((a) => (a[1] + " Mode") == val.Key)?[0] ?? null).IsNullOrEmpty())
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

				//	CharaMorpher_Core.Logger.LogDebug($"UpdateDefaultsList Name: {name}");
			}

			Instance.Config.Save();
			Instance.Config.SaveOnConfigSet = saveCfgAuto;
			cfg.oldControlsConversion.Value = false;
		}

		public static int SwitchControlSet(in string[] selection, int val, bool keepProgress = true)
		{
			//var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();
			if(selection.Length <= 0) return -1;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"current slot [{cfg.currentControlName.Value}]");


			var name = selection[val = Mathf.Clamp(val, 0, selection.Length - 1)] + strDivider;

			//Replace the new set with the last settings before changing the setting name (loading will call the actual settings)
			if(cfg.currentControlName.Value != name)
				foreach(var ctrl1 in GetFuncCtrlOfType<CharaMorpherController>())
				{

					if(MakerAPI.InsideMaker && keepProgress && ctrl1.controls.all.  //made so you don't loose your																              
						TryGetValue(ctrl1.controls.currentSet, out var tmp))        //progress when switching in maker
						ctrl1.controls.all[name] = tmp.ToDictionary(k => k.Key, v => v.Value.Clone());

					ctrl1.controls.currentSet = name;
				}

			if(cfg.currentControlName.Value != name)
				cfg.currentControlName.Value = name;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"new slot [{cfg.currentControlName.Value}]");
			return val;
		}
		public static int SwitchControlSet(in string[] selection, string val, bool keepProgress = true) =>
			SwitchControlSet(selection,
				Array.IndexOf(selection, val.Trim().LastIndexOf(strDivider) == val.Trim().Length - strDivider.Length ?
					val.Trim().Substring(0, val.Trim().LastIndexOf(strDivider)) : val.Trim()), keepProgress);//will automatically remove "strDevider" if at end of string"
		public static void UpdateDropdown(ICollection<string> selector)
		{

			selector?.Clear();
			foreach(var key in cfg.defaults.Keys)
				selector?.Add(key);

			//	CharaMorpher_Core.Logger.LogDebug($"current List [{string.Join(", ", selector?.ToArray() ?? new string[] { })}]");

			//selecter.Value = 
			SwitchControlSet(cfg.defaults.Keys.ToArray(), selectedMod);

		}

		public static void PopulateDefaultSettings(string name)
		{
			var ctrl1 = MorphUtil.GetFuncCtrlOfType<CharaMorpherController>()?.FirstOrNull();
			if(name.LastIndexOf(strDivider) != (name.Length - strDivider.Length)) name = name + strDivider;

			Instance.controlCategories[name] = new List<MorphSliderData> { };//init list

			if(!MakerAPI.InsideMaker || !cfg.preferCardMorphDataMaker.Value || ctrl1?.ctrls2 == null)
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
					{settingName = "Overall Voice",  Instance.Config.Bind("Defaults", $"{name} "+"Vioce Default",ctrlCat.AddNReturn(new MorphSliderData(settingName, 00f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},

					{ settingName = "Overall Skin Colour",Instance.Config.Bind("Defaults", $"{name} "+"Skin Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Base Skin Colour",Instance.Config.Bind("Defaults", $"{name} "+"Base Skin Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 00f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Sunburn Colour",Instance.Config.Bind("Defaults", $"{name} "+"Sunburn Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 00f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},

					{settingName = "Overall Body", Instance.Config.Bind("Defaults", $"{name} "+"Body  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					 new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Head",Instance.Config.Bind("Defaults", $"{name} "+"Head  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Boobs",Instance.Config.Bind("Defaults", $"{name} "+"Boobs Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Boob Phys.",Instance.Config.Bind("Defaults", $"{name} "+"Boob Phys. Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{ settingName = "Torso",Instance.Config.Bind("Defaults", $"{name} "+"Torso Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Arms",Instance.Config.Bind("Defaults", $"{name} "+"Arms  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Butt",Instance.Config.Bind("Defaults", $"{name} "+"Butt  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Legs",Instance.Config.Bind("Defaults", $"{name} "+"Legs  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Body Other",Instance.Config.Bind("Defaults", $"{name} "+"Body Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},


					{ settingName = "Overall Face",Instance.Config.Bind("Defaults", $"{name} "+"Face  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Ears",Instance.Config.Bind("Defaults", $"{name} "+"Ears  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Eyes",Instance.Config.Bind("Defaults", $"{name} "+ "Eyes  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1 , Browsable = false }))
					},{settingName = "Nose",Instance.Config.Bind("Defaults", $"{name} "+"Nose  Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Mouth",Instance.Config.Bind("Defaults", $"{name} "+"Mouth Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "Face Other",Instance.Config.Bind("Defaults", $"{name} "+"Face Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1 , Browsable = false }))
					},

					{ settingName = "ABMX Overall Body",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Body Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Boobs",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Boobs Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{ settingName = "ABMX Torso",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Torso Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Arms",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Arms Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Hands",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Hands Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Butt",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Butt Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Legs",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Legs Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Feet",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Feet Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Genitals",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Genitals Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Body Other",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Body Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},

					{ settingName = "ABMX Overall Head",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Head Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 100f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Ears",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Ears Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Eyes",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Eyes Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Nose",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Nose Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Mouth",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Mouth Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Hair",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Hair Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},{settingName = "ABMX Head Other",Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Head Other Default", ctrlCat.AddNReturn(new MorphSliderData(settingName, 50f *.01f)).Clone(), new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -ctrlCat.Count + 1, Browsable = false }))
					},
				};

				foreach(var val in cfg.defaults[name])
					val.Value.Value.dataName = val.Key + "";

				//CharaMorpher_Core.Logger.LogDebug($"Current List: [{string.Join(", ", Instance.controlCategories[name].Attempt((v)=>v.Value))}]");

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

		public static string AddNewSetting(string baseName = "Slot")
		{
			int count = 1;
			var ctrl1 = MorphUtil.GetFuncCtrlOfType<CharaMorpherController>()?.FirstOrNull();

			string name = "Error" + strDivider;
			var defList = !MakerAPI.InsideMaker || !cfg.preferCardMorphDataMaker.Value || ctrl1?.ctrls2 == null ?
				Instance.controlCategories.Keys.ToList() :
				ctrl1?.controls?.all?.Keys?.ToList() ?? Instance.controlCategories.Keys.ToList();
			//var modeDefList = Instance.Config.Where((k) => k.Key.Section == "Mode Defaults");

			if(baseName.IsNullOrEmpty()) baseName = defaultStr.Substring(0, defaultStr.LastIndexOf(strDivider));

			var tmp = 0;
			foreach(var chara in baseName.Reverse())
			{
				if(!Regex.IsMatch($"{chara}", @"\d")) break;
				++tmp;
			}
			baseName = baseName.Substring(0, baseName.Length - tmp).Trim();

			//find new empty slot name
			while(defList?.Any((k) =>
			k.Contains(name = $"{baseName} {count}{strDivider}")) ?? false) ++count;



			//	CharaMorpher_Core.Logger.LogDebug("creating Defaults");

			PopulateDefaultSettings(name);

			//CharaMorpher_Core.Logger.LogDebug("creating Controls");
			foreach(var ctrl2 in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
			{
				ctrl2.controls.all[name] = new Dictionary<string, MorphSliderData>();

				var data = (!ctrl2.isUsingExtMorphData ? ctrl2.ctrls1 : (ctrl2.ctrls2 ?? ctrl2.ctrls1))?.all;
				if(!data.ContainsKey(name))
					data[name] = new Dictionary<string, MorphSliderData>();

			}
			foreach(var ctrl2 in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				foreach(var ctrl in Instance.controlCategories[name])
				{
					var tmp2 = Instance.controlCategories[defaultStr].Find(v => v.dataName == ctrl.dataName);
					ctrl2.controls.all[name][ctrl.dataName] = tmp2.Clone();

					var data = (!MakerAPI.InsideMaker || !cfg.preferCardMorphDataMaker.Value ? ctrl2.ctrls1 : (ctrl2.ctrls2 ?? ctrl2.ctrls1))?.all;
					if(!data[name].ContainsKey(ctrl.dataName))
					{
						data[name][ctrl.dataName] = tmp2.Clone();
					}
				}

			CharaMorpher_Core.Logger.LogMessage($"Created {name}");

			return name;
		}

		public static void RemoveCurrentSetting(string baseName)
		{
			var ctrl = MorphUtil.GetFuncCtrlOfType<CharaMorpherController>()?.FirstOrNull();

			string name = baseName.IsNullOrEmpty() ? cfg.currentControlName.Value : baseName;

			if(name == defaultStr) return;

			//CharaMorpher_Core.Logger.LogDebug("remove Controls");

			foreach(CharaMorpherController ctrl1 in GetFuncCtrlOfType<CharaMorpherController>())
			{
				var obj = ctrl1.controls.all;
				if(obj.ContainsKey(name))
					obj[name].Clear();
				obj.Remove(name);

				var data = (!MakerAPI.InsideMaker || !cfg.preferCardMorphDataMaker.Value ? ctrl1.ctrls1 : (ctrl1.ctrls2 ?? ctrl1.ctrls1))?.all;
				if(data.ContainsKey(name))
					data[name].Clear();
				data.Remove(name);

				//(!MakerAPI.InsideMaker||!cfg.useCardMorphDataMaker.Value ? ctrl1.ctrls1 : ctrl1.ctrls2 ?? ctrl1.ctrls1)?.all?.Remove(name);
				//	CharaMorpher_Core.Logger.LogDebug($"Controls List: [{string.Join(", ", obj.Keys.ToArray())}]");
			}

			if(!MakerAPI.InsideMaker || !cfg.preferCardMorphDataMaker.Value || ctrl?.ctrls2 == null)
			{
				//	CharaMorpher_Core.Logger.LogDebug("remove Defaults");
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

			CharaMorpher_Core.Logger.LogMessage($"removed [{name}]");
			//	CharaMorpher_Core.Logger.LogDebug($"Current List: [{string.Join(", ", ControlsList)}]");


		}

		/// <summary>
		/// makes sure a path fallows the format "this/is/a/path" and not "this//is\\a/path" or similar
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public static string MakeDirPath(this string dir)
		{

			dir = (dir ?? "").Trim().Replace('\\', '/').Replace("//", "/");

			if((dir.LastIndexOf('.') < dir.LastIndexOf('/'))
				&& dir.Last() != '/')
				dir += '/';

			return dir;
		}

		/// <summary>
		/// Returns a list of the regestered handeler specified. returns empty list otherwise 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> GetFuncCtrlOfType<T>()
		{
			foreach(var hnd in CharacterApi.RegisteredHandlers)
				if(hnd.ControllerType == typeof(T))
					return hnd.Instances.Cast<T>();

			return new T[] { };
		}

		/// <summary>
		/// Defaults the ConfigEntry on game launch
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
		/// Defaults the ConfigEntry on game launch
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		public static ConfigEntry<T> ConfigDefaulter<T>(this ConfigEntry<T> v1) => v1.ConfigDefaulter((T)v1.DefaultValue);

		static RenderTexture tmpTex = new RenderTexture((int)150, (int)200, 0);
		static string lastPath = null;
		internal static void MyImageButtonDrawer(ConfigEntryBase entry)
		{
			// Make sure to use GUILayout.ExpandWidth(true) to use all available space
			GUILayout.BeginVertical();

			if(GUILayout.Button(new GUIContent(entry.Definition.Key, entry.Description.Description), GUILayout.ExpandWidth(true)))
				CharaMorpherGUI.GetNewImageTarget();

			if(tmpTex)
			{
				//tmpTex.Release();
				tmpTex.autoGenerateMips = false;
				tmpTex.antiAliasing = 4;
				tmpTex.filterMode = FilterMode.Bilinear;

				if(lastPath != CharaMorpherGUI.TargetPath)
					Graphics.Blit(CharaMorpherGUI.TargetPath.CreateTexture(), tmpTex);
			}

			GUILayout.Box(tmpTex, GUILayout.Width(150), GUILayout.Height(200));

			if(lastPath != CharaMorpherGUI.TargetPath) lastPath = CharaMorpherGUI.TargetPath;

			GUILayout.EndVertical();
		}

		public static string[] ControlsList
		{
			get
			{
				var val = ((!MakerAPI.InsideMaker || !cfg.preferCardMorphDataMaker.Value) ?
					Instance?.controlCategories?.Keys.ToList() :
					(GetFuncCtrlOfType<CharaMorpherController>()?.First()?.controls?.all?.Keys?.ToList()
					?? Instance?.controlCategories?.Keys.ToList()))
					.Attempt((k) => k.LastIndexOf(strDivider) >= 0 ? k.Substring(0, k.LastIndexOf(strDivider)) : throw new Exception())
					.ToArray();

				Array.Sort(val ?? new string[] { });
				return val;
			}
		}

		static int selectedMod = -1;
		static bool selectingMod = false;
		static Vector2 scrollview = Vector2.zero;
		internal static void MySelectionListDrawer(ConfigEntryBase entry)
		{
			//if(selectedMod < 0)
			selectedMod = Array.IndexOf(ControlsList, cfg.currentControlName.Value.
				Substring(0, cfg.currentControlName.Value.LastIndexOf(strDivider)));

			if(ControlsList.Length > 0 &&
				selectedMod < 0) selectedMod = SwitchControlSet(ControlsList, 0);

			if(selectedMod < 0) return;

			try
			{
				GUILayout.BeginVertical();

				GUILayout.Space(3);
				bool btn;
				int maxWidth = 350;
				if(ControlsList.Length > 0)
					if((btn = GUILayout.Button(new GUIContent { text = $"Selected Control Set: {ControlsList[selectedMod]}", tooltip = "select the control set" },
						 GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(maxWidth))) || selectingMod)
					{
						selectingMod = !(btn && selectingMod);//if dropdown btn was pressed

						scrollview = GUILayout.BeginScrollView(scrollview, false, false,
							GUILayout.ExpandWidth(true),
							GUILayout.ExpandHeight(true), GUILayout.Height(150), GUILayout.MaxHeight(200), GUILayout.MaxWidth(maxWidth));

						var select = GUILayout.SelectionGrid(selectedMod, ControlsList, 1, GUILayout.ExpandWidth(true));
						if(select != selectedMod) { selectingMod = false; select = SwitchControlSet(ControlsList, select); }
						selectedMod = select;

						GUILayout.EndScrollView();
					}


				GUILayout.Space(5);

				var addPress = GUILayout.Button(new GUIContent { text = "Add New Slot", tooltip = "Add a new slot to the controls list" }, GUILayout.ExpandWidth(true));
				var removePress = GUILayout.Button(new GUIContent { text = "Remove Current Slot", tooltip = "Remove the currently selected control from the list" }, GUILayout.ExpandWidth(true));

				if(addPress)
				{
					//CharaMorpher_Core.Logger.LogDebug("Trying to add a comp from drawer");
					AddNewSetting();
					//UpdateDrpodown(ControlsList);
					//	CharaMorpher_Core.Logger.LogDebug("execution got to this point");
				}

				if(removePress)
				{
					//CharaMorpher_Core.Logger.LogDebug("Trying to remove a comp from drawer");
					//switch control before deletion
					int tmp = selectedMod;
					if(selectedMod >= ControlsList.Length - 1)
						tmp = ControlsList.Length - 2;

					RemoveCurrentSetting(ControlsList[selectedMod] + strDivider);

					selectedMod = SwitchControlSet(ControlsList, tmp);

					//	UpdateDrpodown(ControlsList);
					//CharaMorpher_Core.Logger.LogDebug("execution got to this point");
					//scrollview = Vector2.zero;
				}
				GUILayout.EndVertical();
			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.LogError(e);
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

		public static BaseGuiEntry OnGUIExists(this BaseGuiEntry gui, UnityAction<BaseGuiEntry> act)
		{
			IEnumerator func(BaseGuiEntry gui1, UnityAction<BaseGuiEntry> act1)
			{
				if(!gui1.Exists)
					yield return new WaitUntil(() => gui1.Exists);//the thing neeeds to exist first

				act1(gui);

				yield break;
			}
			Instance.StartCoroutine(func(gui, act));

			return gui;
		}

		static CurrentSaveLoadController saveLoad = new CurrentSaveLoadController();
		public static PluginData SaveExtData(this CharaMorpherController ctrl) => saveLoad.Save(ctrl);
		public static PluginData LoadExtData(this CharaMorpherController ctrl, PluginData data = null)
		{
			//ImageConversion.LoadImage
			if(cfg.debug.Value)
				Logger.LogDebug("loading extended data...");
			var tmp = CharaMorpherGUI.MorphLoadToggle ? saveLoad.Load(ctrl, data) : null;
			if(cfg.debug.Value)
				Logger.LogDebug("extended data loaded");

			string path = Path.Combine(
				cfg.charDir.Value.MakeDirPath(),
				cfg.imageName.Value.MakeDirPath());


			//if(!check)
			//	tmp = null;

			if(cfg.debug.Value)
				Logger.LogDebug($"Load check status: {ctrl.isUsingExtMorphData}");

			var ctrler = (CharaMorpherController)ctrl;
			OnNewTargetImage.Invoke(path, ctrl.isUsingExtMorphData ? ctrler?.m_data2?.main?.pngData : null);

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

		public static void SoftSave(bool ucmd, CharaMorpherController ctrl = null/*, string spec = null*/)
		{
			if(!MakerAPI.InsideMaker) return;

			if(!ctrl)
				ctrl = GetFuncCtrlOfType<CharaMorpherController>().FirstOrNull();

			if(!ctrl) return;//return if ctrl is null

			if(ucmd && ctrl.isReloading)
				ctrl.ctrls2 = ctrl?.controls.Clone();//needs to be done this way

			var listCtrl = !ucmd ? ctrl.ctrls1 : (ctrl.ctrls2 ?? ctrl.ctrls1);

			//var list = ((!ucmd ? ctrl.ctrls1 : ctrl.ctrls2 ?? ctrl.ctrls1)?.all);
			//var listCpy = list.ToDictionary((k) => k.Key, (e1) => e1.Value.ToDictionary(k => k.Key, e2 => e2.Value));
			//if(listCtrl?.Copy(ctrl.controls) ?? false)
			//	CharaMorpher_Core.Logger.LogDebug("SoftSave Saved successfully");
			//else
			//	CharaMorpher_Core.Logger.LogDebug("SoftSave Failed... successfully");


			//CharaMorpher_Core.Logger.LogDebug($"ctrls1 Saved:\n [{string.Join(",\n ", ctrl.ctrls1?.all?.Keys?.ToArray() ?? new string[] { })}]");
			//CharaMorpher_Core.Logger.LogDebug($"ctrls2 Saved:\n [{string.Join(",\n ", ctrl.ctrls2?.all?.Keys?.ToArray() ?? new string[] { })}]");

			//foreach(var def in list.Keys.ToList())
			//	if(spec == null || def == spec)
			//		foreach(var def2 in list[def].Keys.ToList())
			//			list[def][def2] = Tuple.Create
			//				(ctrl.controls.all[def][def2].Item1,
			//				ctrl.controls.all[def][def2].Item2);

		}

		/// <summary>
		/// gets the text of the first Text or TMP_Text component in a game object or it's children.
		///  If no component return null. 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string GetTextFromTextComponent(this GameObject obj) =>
			obj?.GetComponentInChildren<TMP_Text>()?.text ??
			obj?.GetComponentInChildren<Text>()?.text ?? null;

	}

}