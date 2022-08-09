using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Diagnostics;

#if HONEY_API

using AIChara;
#endif



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
 * Save morph changes to card (w/o changing card perameters)
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
		public const string GUID = "prolo.chararmorpher";
		public const string Version = "0.2.0.1";

		internal static CharaMorpher_Core Instance;
		internal static new ManualLogSource Logger;
		internal static OnNewImage OnNewTargetImage = new OnNewImage();
		internal static OnValueChange OnSliderValueChange = new OnValueChange();
		//internal static Subject<string> OnNewTargetImage = new Subject<string>();

		public static MyConfig cfg;
		public struct MyConfig
		{
			//ABMX
			public ConfigEntry<bool> enableABMX { set; get; }


			//Main
			public ConfigEntry<bool> enable { set; get; }
			public ConfigEntry<bool> enableInMaleMaker { get; set; }
			public ConfigEntry<bool> enableInGame { set; get; }
			public ConfigEntry<bool> linkOverallABMXSliders { set; get; }
			public ConfigEntry<bool> saveWithMorph { set; get; }
			public ConfigEntry<string> charDir { set; get; }
			public ConfigEntry<string> imageName { set; get; }
			public ConfigEntry<uint> sliderExtents { set; get; }
			public ConfigEntry<bool> debug { set; get; }
			public ConfigEntry<bool> resetOnLaunch { set; get; }

			public List<ConfigEntry<float>> defaults { set; get; }

			//Advanced (show up below main) 

			//tests
			public ConfigEntry<float> initialMorphTest { internal set; get; }
			public ConfigEntry<float> initalBoobTest { internal set; get; }
			public ConfigEntry<float> initalFaceTest { internal set; get; }
			public ConfigEntry<uint> multiUpdateTest { internal set; get; }
			public ConfigEntry<uint> fullBoneResetTest { internal set; get; }

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


		public List<KeyValuePair<int, string>> controlCategories = new List<KeyValuePair<int, string>>();
		void Awake()
		{
			Instance = this;
			Logger = base.Logger;

			ForeGrounder.SetCurrentForground();


			string femalepath = Path.Combine(Paths.GameRootPath, "/UserData/chara/");

			int bodyBoneAmount = ChaFileDefine.cf_bodyshapename.Length - 1;
			int faceBoneAmount = ChaFileDefine.cf_headshapename.Length - 1;


			Logger.LogDebug($"Body bones amount: {bodyBoneAmount}");
			Logger.LogDebug($"Face bones amount: {faceBoneAmount}");

			int index = 0, defaultIndex = -1;//easier to input index order values
			cfg = new MyConfig
			{
				enable = Config.Bind("_Main_", "Enable", false, new ConfigDescription("Allows the plugin to run (may need to reload character/scene if results are not changing)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInMaleMaker = Config.Bind("_Main_", "Enable in Male Maker", false, new ConfigDescription("Allows the plugin to run while in male maker (enable before launching maker)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInGame = Config.Bind("_Main_", "Enable in Game", true, new ConfigDescription("Allows the plugin to run while in main game", null, new ConfigurationManagerAttributes { Order = --index })),
				enableABMX = Config.Bind("_Main_", "Enable ABMX", true, new ConfigDescription("Allows ABMX to be affected", null, new ConfigurationManagerAttributes { Order = --index })),
				linkOverallABMXSliders = Config.Bind("_Main_", "Link Overall ABMX Sliders", true, new ConfigDescription("Allows ABMX overall sliders to be affected by its counterpart (i.e. Body:50% * ABMXBody:100% = ABMXBody:50%)", null, new ConfigurationManagerAttributes { Order = --index })),
				saveWithMorph = Config.Bind("_Main_", "Save With Morph", true, new ConfigDescription("Allows the card to save as seen in maker (must be set before saving. If false card is set to default card values)", null, new ConfigurationManagerAttributes { Order = --index })),
				charDir = Config.Bind("_Main_", "Directory Path", femalepath, new ConfigDescription("Directory where character is stored", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				imageName = Config.Bind("_Main_", "Card Name", "sample.png", new ConfigDescription("The character card used to morph", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				sliderExtents = Config.Bind("_Main_", "Slider Extents", 200u, new ConfigDescription("How far the slider values go above default (e.i. setting value to 10 gives values -10 -> 110)", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true })),
				resetOnLaunch = Config.Bind("_Testing_", "Reset On Launch", true, new ConfigDescription("will reset advanced values to defaults after launch", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),

				//you don't need to see this in game
				defaults = new List<ConfigEntry<float>>{
					Config.Bind("Defaults", "Vioce Default" , 00f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Overall Voice")).Key , Browsable=false})),

					Config.Bind("Defaults", "Skin Default" , 100f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Overall Skin Colour")).Key , Browsable=false })),
					Config.Bind("Defaults", "Base Skin Default" , 00f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Base Skin Colour")).Key , Browsable=false })),
					Config.Bind("Defaults", "Sunburn Default" , 00f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Sunburn Colour")).Key , Browsable=false })),

					Config.Bind("Defaults", "Body  Default" , 100f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex, "Overall Body")).Key , Browsable=false })),
					Config.Bind("Defaults", "Head  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Head")).Key, Browsable=false })),
					Config.Bind("Defaults", "Boobs Default", 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Boobs")).Key , Browsable=false})),
					Config.Bind("Defaults", "Butt  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Butt")).Key, Browsable=false })),
					Config.Bind("Defaults", "Torso Default", 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Torso")).Key , Browsable=false})),
					Config.Bind("Defaults", "Arms  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Arms")).Key, Browsable=false })),
					Config.Bind("Defaults", "Legs  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Legs")).Key, Browsable=false })),

					Config.Bind("Defaults", "Face  Default" , 100f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Overall Face")).Key , Browsable=false})),
					Config.Bind("Defaults", "Ears  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Ears")).Key , Browsable=false})),
					Config.Bind("Defaults", "Eyes  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Eyes")).Key , Browsable=false})),
					Config.Bind("Defaults", "Nose  Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Nose")).Key , Browsable=false})),
					Config.Bind("Defaults", "Mouth Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "Mouth")).Key , Browsable=false})),


					Config.Bind("Defaults", "ABMX  Body Default" , 100f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Overall Body")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Boobs Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Boobs")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Butt Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Butt")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Torso Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Torso")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Arms Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Arms")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Hands Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order =-controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Hands")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Legs Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Legs")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Feet Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Feet")).Key, Browsable=false})),
					Config.Bind("Defaults", "ABMX  Genitals Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Genitals")).Key , Browsable=false})),


					Config.Bind("Defaults", "ABMX  Head Default" , 100f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Overall Head ")).Key , Browsable=false})),
					Config.Bind("Defaults", "ABMX  Ears Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Ears")).Key , Browsable=false})),
					Config.Bind("Defaults", "ABMX  Eyes Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Eyes")).Key , Browsable=false})),
					Config.Bind("Defaults", "ABMX  Nose Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Nose ")).Key , Browsable=false})),
					Config.Bind("Defaults", "ABMX  Mouth Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Mouth")).Key , Browsable=false})),
					Config.Bind("Defaults", "ABMX  Hair Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Hair")).Key , Browsable=false})),

				},
			};

			//Advanced
			{
				cfg.debug = Config.Bind("_Testing_", "Debug Logging", false, new ConfigDescription("Allows debug logs to be written to the log file", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })).ConfigDefaulter();

#if KOI_API
				cfg.initialMorphTest = Config.Bind("_Testing_", "Init morph value", 1.00f, new ConfigDescription("Used for calculations on reload. RESETS ON GAME LAUNCH (0.47 workes best)", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 5u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
#elif HONEY_API
				cfg.initialMorphTest = Config.Bind("_Testing_", "Init morph value", 1.00f, new ConfigDescription("Used for calculations on reload. RESETS ON GAME LAUNCH (0.0 workes best)", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
				cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();
#endif
				cfg.fullBoneResetTest = Config.Bind("_Testing_", "Full Bone Reset Delay", 3u, new ConfigDescription("Used to determine how long to wait for full bone reset. RESETS ON GAME LAUNCH", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter();


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


			//	Logger.enable = cfg.debug multiUpdateTest  .Value;
			//	cfg.debug.SettingChanged  fullBoneResetTest+= (m,n) => { Logger.enable = cfg.debug.Value; };

			cfg.charDir.SettingChanged += (m, n) =>
			{

				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{

							if(ctrl.initLoadFinished)
								StartCoroutine(ctrl?.CoMorphTargetUpdate(5));

						}
				//Logger.LogDebug("");
				string path = Path.Combine(MakeDirPath(cfg.charDir.Value), MakeDirPath(cfg.imageName.Value));
				if(File.Exists(path))
					OnNewTargetImage.Invoke(path);
			};

			cfg.imageName.SettingChanged += (m, n) =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//StopAllCoroutines();
							if(ctrl.initLoadFinished)
								StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
							//	StartCoroutine(ctrl?.CoFullBoneRrfresh(6 + (int)cfg.fullBoneResetTest.Value));
							///	StartCoroutine(ctrl?.CoMorphReload(abmxOnly: true));
						}
				string path = Path.Combine(MakeDirPath(cfg.charDir.Value), MakeDirPath(cfg.imageName.Value));
				if(File.Exists(path))
					OnNewTargetImage.Invoke(path);
			};

			cfg.enable.SettingChanged += (m, n) =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	StopAllCoroutines();
							for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
								StartCoroutine(ctrl?.CoMorphUpdate(3 + a + 1));

							//	StartCoroutine(ctrl?.CoFullBoneRrfresh(3 + (int)cfg.fullBoneResetTest.Value));

						}
			};
			cfg.enableInGame.SettingChanged += (m, n) =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	StopAllCoroutines();
							for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
								StartCoroutine(ctrl?.CoMorphUpdate(3 + a + 1));

							//	StartCoroutine(ctrl?.CoFullBoneRrfresh(3 + (int)cfg.fullBoneResetTest.Value));

						}
			};

			cfg.enableABMX.SettingChanged += (m, n) =>
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//	StopAllCoroutines();
							for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
								StartCoroutine(ctrl?.CoMorphUpdate(3 + a + 1));
							//	StartCoroutine(ctrl?.CoFullBoneRrfresh(3 + (int)cfg.fullBoneResetTest.Value));

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


		/// <summary>
		/// makes sure a path fallows the format "this/is/a/path" and not "this//is\\a/path" or similar
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public static string MakeDirPath(string dir)
		{
			dir = dir.Trim().Replace('\\', '/').Replace("//", "/");

			if((dir.LastIndexOf('.') < dir.LastIndexOf('/'))
				&& dir.Last() != '/')
				dir += '/';

			return dir;
		}

		public class OnValueChange : UnityEvent { }
		public class OnNewImage : UnityEvent<string> { }
	}


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

	internal static class MyUtil
	{
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
		public static ConfigEntry<T> ConfigDefaulter<T>(this ConfigEntry<T> v1) =>
			v1.ConfigDefaulter((T)v1.DefaultValue);


	}

}