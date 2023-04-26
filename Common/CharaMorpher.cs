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
using UniRx;
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
using static Illusion.Component.UI.MouseButtonCheck;

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

		public Dictionary<string, List<KeyValuePair<int, string>>> controlCategories = new Dictionary<string, List<KeyValuePair<int, string>>>();
		public static MorphConfig cfg;
		public struct MorphConfig
		{
			//ABMX
			public ConfigEntry<bool> enableABMX { set; get; }


			//Main
			public ConfigEntry<bool> enable { set; get; }
			public ConfigEntry<KeyboardShortcut> enableKey { set; get; }
			public ConfigEntry<bool> enableInMaleMaker { get; set; }
			public ConfigEntry<bool> enableInGame { set; get; }
			public ConfigEntry<bool> linkOverallABMXSliders { set; get; }
			public ConfigEntry<bool> enableCalcTypes { set; get; }
			public ConfigEntry<bool> saveAsMorphData { set; get; }
			public ConfigEntry<bool> useCardMorphDataMaker { set; get; }
			public ConfigEntry<bool> useCardMorphDataGame { set; get; }

			public ConfigEntry<string> pathBtn { set; get; }
			public ConfigEntry<string> charDir { set; get; }
			public ConfigEntry<string> imageName { set; get; }
			public ConfigEntry<uint> sliderExtents { set; get; }
			public ConfigEntry<string> currentControlName { set; get; }
			public ConfigEntry<string> controlSets { set; get; }



			public Dictionary<string, List<ConfigEntry<float>>> defaults { set; get; }
			public Dictionary<string, List<ConfigEntry<int>>> defaultModes { set; get; }

			//Advanced (show up below main) 
			public ConfigEntry<bool> debug { set; get; }
			public ConfigEntry<bool> resetOnLaunch { set; get; }
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


			string femalepath = Path.Combine(Paths.GameRootPath, "/UserData/chara/female/");

			int bodyBoneAmount = ChaFileDefine.cf_bodyshapename.Length - 1;
			int faceBoneAmount = ChaFileDefine.cf_headshapename.Length - 1;


			//Logger.LogDebug($"Body bones amount: {bodyBoneAmount}");
			//Logger.LogDebug($"Face bones amount: {faceBoneAmount}");

			//	Instance.Config.Reload();//get controls from disk

			int index = 0, defaultIndex = -1;//easier to input index order values
			string main = "__Main__", advanced = "_Advanced_";
			cfg = new MorphConfig
			{
				enable = Config.Bind(main, "Enable", false, new ConfigDescription("Allows the plugin to run (may need to reload character/scene if results are not changing)", null, new ConfigurationManagerAttributes { Order = --index })),

				enableABMX = Config.Bind(main, "Enable ABMX", true, new ConfigDescription("Allows ABMX to be affected", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInMaleMaker = Config.Bind(main, "Enable in Male Maker", false, new ConfigDescription("Allows the plugin to run while in male maker (enable before launching maker)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInGame = Config.Bind(main, "Enable in Game", true, new ConfigDescription("Allows the plugin to run while in main game", null, new ConfigurationManagerAttributes { Order = --index })),
				linkOverallABMXSliders = Config.Bind(main, "Link Overall Base Sliders to ABMX Sliders", true, new ConfigDescription("Allows ABMX overall sliders to be affected by their base counterpart (i.e. Body:50% * ABMXBody:100% = ABMXBody:50%)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableCalcTypes = Config.Bind(main, "Enable Calculation Types", false, new ConfigDescription("Enables quadratic mode where value gets squared (i.e. 1.2 = 1.2^2 = 1.44)", null, new ConfigurationManagerAttributes { Order = --index })),
				saveAsMorphData = Config.Bind(main, "Save As Morph Data", false,
				new ConfigDescription("Allows the card to save using morph data. " +
				"If true, card is set to default values and Morph Ext. data will be saved to card while keeping any accessory/clothing changes, " +
				"else the card is saved normally w/o Morph Ext. data and saved as seen (must be set before saving)", null, new ConfigurationManagerAttributes { Order = --index })),
				useCardMorphDataMaker = Config.Bind(main, "Use Card Morph Data (Maker)", true, new ConfigDescription("Allows the mod to use data from card instead of default data (If false card uses default Morph card data)", null, new ConfigurationManagerAttributes { Order = --index })),
				useCardMorphDataGame = Config.Bind(main, "Use Card Morph Data (Game)", true, new ConfigDescription("Allows the card to use data from card instead of default data (If false card uses default Morph card data)", null, new ConfigurationManagerAttributes { Order = --index })),

				charDir = Config.Bind(main, "Directory Path", femalepath, new ConfigDescription("Directory where character is stored", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				imageName = Config.Bind(main, "Card Name", "sample.png", new ConfigDescription("The character card used to morph", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				sliderExtents = Config.Bind(main, "Slider Extents", 200u, new ConfigDescription("How far the slider values go above default (e.i. setting value to 10 gives values -10 -> 110)", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true })),
				enableKey = Config.Bind(main, "Toggle Enable Keybinding", new KeyboardShortcut(KeyCode.Return, KeyCode.RightShift), new ConfigDescription("Enable/Disable toggle button", null, new ConfigurationManagerAttributes { Order = --index })),
				pathBtn = Config.Bind(main, "Set Morph Target", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = --index, HideDefaultButton = true, CustomDrawer = MorphUtil.MyImageButtonDrawer, ObjToStr = (o) => "", StrToObj = (s) => null })),
				currentControlName = Config.Bind(main, "Current Control Name", defaultStr, new ConfigDescription("", tags: new ConfigurationManagerAttributes { Order = --index, Browsable = false, })),
				controlSets = Config.Bind(main, "Control Sets", "", new ConfigDescription("", tags: new ConfigurationManagerAttributes { Order = --index, HideDefaultButton = true, CustomDrawer = MorphUtil.MySelectionListDrawer, ObjToStr = (o) => "", StrToObj = (s) => null })),


				//you don't need to see this in game
				defaults = new Dictionary<string, List<ConfigEntry<float>>>(),
				defaultModes = new Dictionary<string, List<ConfigEntry<int>>>(),

				//Advanced
				debug = Config.Bind(advanced, "Debug Logging", false, new ConfigDescription("Allows debug logs to be written to the log file", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),
				resetOnLaunch = Config.Bind(advanced, "Reset On Launch", true, new ConfigDescription("will reset advanced values to defaults after launch", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),
				easyMorphBtnOverallSet = Config.Bind(advanced, "Enable Easy Morph Button Overall Set", true, new ConfigDescription("Sets the overall sliders whenever an Easy Morph button is pressed, everything else otherwise", null, new ConfigurationManagerAttributes { Order = --index, Browsable = false, IsAdvanced = true })),
				easyMorphBtnEnableDefaulting = Config.Bind(advanced, "Enable Easy Morph Defaulting", true, new ConfigDescription("Defaults everything not set by Easy Morph button to 100%", null, new ConfigurationManagerAttributes { Order = --index, Browsable = false, IsAdvanced = true })),
				oldControlsConversion = Config.Bind(advanced, "Convert Old Data (On Next Startup)", true,
				new ConfigDescription("This will attempt to convert old data (V1 and below) to the new current format. " +
				"This should happen automatically the first time but can be done again if need be",
				null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),
			};

			//Advanced
			{

				cfg.debug.ConfigDefaulter();

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

			};

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
						return true;
					});
				}

				StartCoroutine(CoKeyUpdates());
			}
			KeyUpdates();

			cfg.charDir.SettingChanged += (m, n) =>
			{
				bool check = !(MakerAPI.InsideMaker ?
				cfg.useCardMorphDataMaker.Value :
				cfg.useCardMorphDataGame.Value);

				string path = Path.Combine(MorphUtil.MakeDirPath(cfg.charDir.Value), MorphUtil.MakeDirPath(cfg.imageName.Value));
				foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					if(File.Exists(path))
						if(ctrl.initLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
				}
			};

			cfg.imageName.SettingChanged += (m, n) =>
			{
				bool check = !(MakerAPI.InsideMaker ?
				cfg.useCardMorphDataMaker.Value :
				cfg.useCardMorphDataGame.Value);

				string path = Path.Combine(MorphUtil.MakeDirPath(cfg.charDir.Value), MorphUtil.MakeDirPath(cfg.imageName.Value));
				foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					if(File.Exists(path))
						if(ctrl.initLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
				}
			};

			cfg.useCardMorphDataMaker.SettingChanged += (m, n) =>
			{
				if(MakerAPI.InsideMaker)
					foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
					{
						if(ctrl.initLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
					}
			};

			cfg.useCardMorphDataGame.SettingChanged += (m, n) =>
			{
				if(!MakerAPI.InsideMaker)
					foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
					{
						if(ctrl.initLoadFinished)
							StartCoroutine(ctrl?.CoMorphTargetUpdate(5));

					}
			};

			cfg.enable.SettingChanged += (m, n) =>
			{
				foreach(var ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{

					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
				}
			};

			cfg.enableInGame.SettingChanged += (m, n) =>
			{

				foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
				}
			};

			cfg.enableABMX.SettingChanged += (m, n) =>
			{
				foreach(CharaMorpherController ctrl in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				{
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
				}
			};



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

	public class OnValueChange : UnityEvent { }
	public class OnControlSetValueChange : UnityEvent<string> { }
	public class OnNewImage : UnityEvent<string, byte[]> { }

	/// <summary>
	/// utility to bring process to foreground (mainly the game after file select)
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
		public static Dictionary<ConfigDefinition, string> GetOrphanedEntries(this ConfigFile file, string sec = "")
		{
			Dictionary<ConfigDefinition, string> OrphanedEntries = new Dictionary<ConfigDefinition, string>();
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
						}
					}

			}

			return OrphanedEntries;
		}


		/// <summary>
		/// 
		/// </summary>
		public static void UpdateDefaultsList()
		{

			var orphaned = Instance.Config.GetOrphanedEntries("Defaults");
			var defList = orphaned.Keys.ToList();

			orphaned = Instance.Config.GetOrphanedEntries("Mode Defaults");
			var modeDefList = orphaned.Keys.ToList();


			foreach(var val in defList)
			{
				var name = val.Key.Substring(0, val.Key.LastIndexOf(strDivider) + 1);


				if(!cfg.defaults.TryGetValue(name, out var tmp))
					cfg.defaults[name] = new List<ConfigEntry<float>>();

				if(!Instance.controlCategories.TryGetValue(name, out var tmp2))
					Instance.controlCategories[name] = new List<KeyValuePair<int, string>>(Instance.controlCategories[defaultStr]);

				if(!cfg.defaults[name].Any((k) => k.Definition.Key == val.Key))
					cfg.defaults[name].Add(Instance.Config.Bind(val, 0f, new ConfigDescription("", tags:
						new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false, })));


				if(name.IsNullOrEmpty())
				{

					int num = -1;
					if(cfg.oldControlsConversion.Value)
						if((num = cfg.defaults[defaultStr].
							FindIndex((v) => v.Definition.Key.Contains(val.Key))) >= 0)
							cfg.defaults[defaultStr][num].Value = cfg.defaults[name].Last().Value;


					cfg.defaults.Remove(name);
					Instance.controlCategories.Remove(name);
				}

				//	CharaMorpher_Core.Logger.LogDebug($"UpdateDefaultsList Name: {name}");
			}

			foreach(var val in modeDefList)
			{
				var name = val.Key.Substring(0, val.Key.LastIndexOf(strDivider) + 1);

				if(!cfg.defaultModes.TryGetValue(name, out var tmp))
					cfg.defaultModes[name] = new List<ConfigEntry<int>>();

				if(!cfg.defaultModes[name].Any((k) => k.Definition.Key == val.Key))
					cfg.defaultModes[name].Add(Instance.Config.Bind(val, 0, new ConfigDescription("", tags:
						new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false, })));

				if(name.IsNullOrEmpty())
				{
					int num = -1;
					if(cfg.oldControlsConversion.Value)
						if((num = cfg.defaultModes[defaultStr].
							FindIndex((v) => v.Definition.Key.Contains(val.Key))) >= 0)
							cfg.defaultModes[defaultStr][num].Value = cfg.defaultModes[name].Last().Value;

					cfg.defaultModes.Remove(name);
				}
			}

			cfg.oldControlsConversion.Value = false;
		}

		public static int SwitchControlSet(in string[] selection, int val)
		{
			//var ctrl = GetFuncCtrlOfType<CharaMorpherController>().First();
			if(selection.Length <= 0) return -1;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"current slot [{cfg.currentControlName.Value}]");


			var name = selection[val = Mathf.Clamp(val, 0, selection.Length - 1)] + strDivider;

			//Replace the new set with the last settings before changing the setting name (loading will call the actual settings)
			if(cfg.currentControlName.Value != name)
				foreach(var ctrl1 in GetFuncCtrlOfType<CharaMorpherController>())
				{

					if(ctrl1.controls.all.TryGetValue(ctrl1.controls.currentSet, out var tmp))
						ctrl1.controls.all[name] =
					new Dictionary<string, Tuple<float, MorphCalcType>>(tmp);

					ctrl1.controls.currentSet = name;
				}

			if(cfg.currentControlName.Value != name)
				cfg.currentControlName.Value = name;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"new slot [{cfg.currentControlName.Value}]");
			return val;
		}
		public static int SwitchControlSet(in string[] selection, string val) =>
			SwitchControlSet(selection,
				Array.IndexOf(selection, val.Trim().LastIndexOf(strDivider) == val.Trim().Length - strDivider.Length ?
					val.Trim().Substring(0, val.Trim().LastIndexOf(strDivider)) : val.Trim()));//will automatically remove "strDevider" if at end of string"
		public static void UpdateDrpodown(ICollection<string> selector)
		{

			selector?.Clear();
			foreach(var key in cfg.defaults.Keys)
				selector?.Add(key);

			CharaMorpher_Core.Logger.LogDebug($"current List [{string.Join(", ", selector?.ToArray() ?? new string[] { })}]");

			//selecter.Value = 
			SwitchControlSet(cfg.defaults.Keys.ToArray(), selectedMod);

		}

		public static void PopulateDefaultSettings(string name)
		{

			int defaultIndex = -1;

			Instance.controlCategories[name] = new List<KeyValuePair<int, string>> { };//init list

			if(!MakerAPI.InsideMaker || !cfg.useCardMorphDataMaker.Value)
			{
				cfg.defaults[name] = new List<ConfigEntry<float>>
			{
				Instance.Config.Bind("Defaults", $"{name} "+"Vioce Default", (00f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Overall Voice")).Key, Browsable = false })),

				Instance.Config.Bind("Defaults", $"{name} "+"Skin Default", (100f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Overall Skin Colour")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+ "Base Skin Default", (00f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Base Skin Colour")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Sunburn Default", (00f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Sunburn Colour")).Key, Browsable = false })),

				Instance.Config.Bind("Defaults", $"{name} "+"Body  Default", (100f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Overall Body")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Head  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Head")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Boobs Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Boobs")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Boob Phys. Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Boob Phys.")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Torso Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Torso")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Arms  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Arms")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Butt  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Butt")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Legs  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Legs")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Body Other Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Body Other")).Key, Browsable = false })),

				Instance.Config.Bind("Defaults", $"{name} "+"Face  Default", (100f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Overall Face")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Ears  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Ears")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+ "Eyes  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Eyes")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Nose  Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Nose")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Mouth Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Mouth")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"Face Other Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Face Other")).Key, Browsable = false })),


				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Body Default", (100f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Overall Body")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Boobs Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Boobs")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Torso Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Torso")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Arms Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Arms")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Hands Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Hands")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Butt Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Butt")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Legs Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Legs")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Feet Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Feet")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Genitals Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Genitals")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Body Other Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Body Other")).Key, Browsable = false })),


				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Head Default", (100f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Overall Head ")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Ears Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Ears")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Eyes Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Eyes")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Nose Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Nose ")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Mouth Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Mouth")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Hair Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Hair")).Key, Browsable = false })),
				Instance.Config.Bind("Defaults", $"{name} "+"ABMX  Head Other Default", (50f), new ConfigDescription("Set default value on maker startup", null,
				new ConfigurationManagerAttributes { Order = -Instance.controlCategories[name].AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "ABMX Head Other")).Key, Browsable = false })),

			};
				cfg.defaultModes[name] = new List<ConfigEntry<int>>();//will link up with the defaults
				foreach(var mode in cfg.defaults[name])
				{
					var atrib = (ConfigurationManagerAttributes)mode.Description.Tags[0];
					cfg.defaultModes[name].Add
					(
						Instance.Config.Bind("Mode Defaults", $"{mode.Definition.Key} Mode", (int)MorphCalcType.LINEAR,
						new ConfigDescription("Set default value on maker startup", null, atrib))
					);
				}
			}
		}

		public static string AddNewSetting(string baseName = "Slot")
		{
			int count = 1;
			var ctrl1 = MorphUtil.GetFuncCtrlOfType<CharaMorpherController>().First();

			string name = "Error" + strDivider;
			var defList = !MakerAPI.InsideMaker || !cfg.useCardMorphDataMaker.Value ?
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



			CharaMorpher_Core.Logger.LogDebug("creating Defaults");

			PopulateDefaultSettings(name);

			CharaMorpher_Core.Logger.LogDebug("creating Controls");
			foreach(var ctrl2 in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				ctrl2.controls.all[name] = new Dictionary<string, Tuple<float, MorphCalcType>>();

			foreach(var ctrl2 in MorphUtil.GetFuncCtrlOfType<CharaMorpherController>())
				foreach(var ctrl in Instance.controlCategories[name])
					ctrl2.controls.all[name][ctrl.Value] = Tuple.Create(cfg.defaults[name][ctrl.Key].Value * .01f, (MorphCalcType)cfg.defaultModes[name][ctrl.Key].Value);

			//	MorphUtil.UpdateDefaultsList();
			CharaMorpher_Core.Logger.LogMessage($"Created {name}");

			return name;
		}

		public static void RemoveCurrentSetting(string baseName)
		{
			//var ctrl = MorphUtil.GetFuncCtrlOfType<CharaMorpherController>().First();

			string name = baseName.IsNullOrEmpty() ? cfg.currentControlName.Value : baseName;

			if(name == defaultStr) return;

			CharaMorpher_Core.Logger.LogDebug("remove Controls");

			foreach(CharaMorpherController ctrl1 in GetFuncCtrlOfType<CharaMorpherController>())
			{
				var obj = ctrl1.controls.all;
				if(obj.ContainsKey(name))
					obj[name].Clear();
				obj.Remove(name);

				//(!MakerAPI.InsideMaker||!cfg.useCardMorphDataMaker.Value ? ctrl1.ctrls1 : ctrl1.ctrls2 ?? ctrl1.ctrls1)?.all?.Remove(name);
				CharaMorpher_Core.Logger.LogDebug($"Controls List: [{string.Join(", ", ctrl1.controls.all.Keys.ToArray())}]");
			}

			if(!MakerAPI.InsideMaker || !cfg.useCardMorphDataMaker.Value)
			{
				CharaMorpher_Core.Logger.LogDebug("remove Defaults");
				foreach(var def in cfg.defaults)
					foreach(var val in def.Value)
						if(def.Key == name)
							Instance.Config.Remove(val.Definition);

				foreach(var def in cfg.defaultModes)
					foreach(var val in def.Value)
						if(def.Key == name)
							Instance.Config.Remove(val.Definition);


				cfg.defaults.Remove(name);
				cfg.defaultModes.Remove(name);
				Instance.controlCategories.Remove(name);

				if(!Instance.Config.SaveOnConfigSet)
					Instance.Config.Save();//save to disk
			}

			CharaMorpher_Core.Logger.LogMessage($"removed [{name}]");
			CharaMorpher_Core.Logger.LogDebug($"Current List: [{string.Join(", ", ControlsList)}]");


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
				var val = (!MakerAPI.InsideMaker || !cfg.useCardMorphDataMaker.Value ? Instance?.controlCategories?.Keys.ToList() :
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
					if((btn = GUILayout.Button(new GUIContent { text = $"Selected Control Set: {ControlsList[selectedMod]}", tooltip = "select the effected mod" },
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
					else
					{
						//	scrollview = Vector2.zero;
					}
				GUILayout.Space(5);

				var addPress = GUILayout.Button(new GUIContent { text = "Add New Slot", tooltip = "Add a new slot to the controls list" }, GUILayout.ExpandWidth(true));
				var removePress = GUILayout.Button(new GUIContent { text = "Remove Current Slot", tooltip = "Remove the currently selected control from the list" }, GUILayout.ExpandWidth(true));

				if(addPress)
				{
					CharaMorpher_Core.Logger.LogDebug("Trying to add a comp from drawer");
					AddNewSetting();
					//UpdateDrpodown(ControlsList);
					CharaMorpher_Core.Logger.LogDebug("execution got to this point");
				}

				if(removePress)
				{
					CharaMorpher_Core.Logger.LogDebug("Trying to remove a comp from drawer");
					//switch control before deletion
					int tmp = selectedMod;
					if(selectedMod >= ControlsList.Length - 1)
						tmp = ControlsList.Length - 2;

					RemoveCurrentSetting(ControlsList[selectedMod] + strDivider);

					selectedMod = SwitchControlSet(ControlsList, tmp);

					//	UpdateDrpodown(ControlsList);
					CharaMorpher_Core.Logger.LogDebug("execution got to this point");
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
				yield return new WaitUntil(() => gui1.Exists);//the thing neeeds to exist first
				act1(gui);
			}
			Instance.StartCoroutine(func(gui, act));

			return gui;
		}

		static CurrentSaveLoadController saveLoad = new CurrentSaveLoadController();
		public static PluginData SaveExtData(this CharaCustomFunctionController ctrl) => saveLoad.Save(ctrl);
		public static PluginData LoadExtData(this CharaCustomFunctionController ctrl, PluginData data = null)
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

			bool check = !(!(MakerAPI.InsideMaker ?
				cfg.useCardMorphDataMaker.Value :
				cfg.useCardMorphDataGame.Value) ||
				tmp == null);

			//if(!check)
			//	tmp = null;

			if(cfg.debug.Value)
				Logger.LogDebug($"Load check status: {check}");

			var ctrler = (CharaMorpherController)ctrl;
			OnNewTargetImage.Invoke(path, check ? ctrler?.m_data2?.main?.pngData : null);

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
	}

}