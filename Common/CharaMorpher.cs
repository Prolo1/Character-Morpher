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
		public static SetValues sv;

		public struct MyConfig
		{
			//ABMX
			public ConfigEntry<bool> enableABMX { set; get; }


			//Main
			public ConfigEntry<bool> enable { set; get; }
			public ConfigEntry<bool> enableInMaleMaker { get; set; }
			public ConfigEntry<bool> enableInGame { set; get; }
			public ConfigEntry<bool> saveWithMorph { set; get; }
			public ConfigEntry<string> charDir { set; get; }
			public ConfigEntry<string> imageName { set; get; }
			public ConfigEntry<uint> sliderExtents { set; get; }
			public ConfigEntry<bool> debug { set; get; }

			public List<ConfigEntry<float>> defaults { set; get; }

			//Advanced (show up below main) 

			//tests
			public ConfigEntry<float> initialMorphTest { internal set; get; }
			public ConfigEntry<float> initalBoobTest { get; internal set; }
			public ConfigEntry<float> initalFaceTest { get; internal set; }
			public ConfigEntry<uint> multiUpdateTest { internal set; get; }
			//indexes 
			public ConfigEntry<int> headIndex { set; get; }
			public List<ConfigEntry<int>> earIndex { set; get; }
			public List<ConfigEntry<int>> eyeIndex { set; get; }
			public List<ConfigEntry<int>> mouthIndex { set; get; }
			public List<ConfigEntry<int>> brestIndex { set; get; }
			public List<ConfigEntry<int>> torsoIndex { set; get; }
			public List<ConfigEntry<int>> armIndex { set; get; }
			public List<ConfigEntry<int>> buttIndex { set; get; }
			public List<ConfigEntry<int>> legIndex { set; get; }
		}
		public struct SetValues
		{

		}

		public List<KeyValuePair<int, string>> controlCategories = new List<KeyValuePair<int, string>>();
		void Awake()
		{
			Instance = this;
			Logger = base.Logger;

			ForeGrounder.SetCurrentForground();


			string femalepath = Path.Combine(Paths.GameRootPath, "/UserData/chara/");


			int index = 0, defaultIndex = -1;//easier to input index order values
			cfg = new MyConfig
			{
				enable = Config.Bind("_Main_", "Enable", false, new ConfigDescription("Allows the plugin to run (may need to reload character/scene if results are not changing)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInMaleMaker = Config.Bind("_Main_", "Enable in Male Maker", false, new ConfigDescription("Allows the plugin to run while in male maker (enable before launching maker)", null, new ConfigurationManagerAttributes { Order = --index })),
				enableInGame = Config.Bind("_Main_", "Enable in Game", true, new ConfigDescription("Allows the plugin to run while in main game", null, new ConfigurationManagerAttributes { Order = --index })),
				enableABMX = Config.Bind("_Main_", "Enable ABMX", true, new ConfigDescription("Allows ABMX to be affected (may need to reload card if results become wonky)", null, new ConfigurationManagerAttributes { Order = --index })),
				saveWithMorph = Config.Bind("_Main_", "Save With Morph", true, new ConfigDescription("Allows the card to save as seen in maker (must be set before saving. If false card is set to default card values)", null, new ConfigurationManagerAttributes { Order = --index })),
				charDir = Config.Bind("_Main_", "Directory Path", femalepath, new ConfigDescription("Directory where character is stored", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				imageName = Config.Bind("_Main_", "Card Name", "sample.png", new ConfigDescription("The character card used to morph", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
				sliderExtents = Config.Bind("_Main_", "Slider Extents", 200u, new ConfigDescription("How far the slider values go above default (e.i. setting value to 10 gives values -10 -> 110)", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true })),
				debug = Config.Bind("_Main_", "Debug Logging", false, new ConfigDescription("Allows debug logs to be written to the log file", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true })),

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
					Config.Bind("Defaults", "ABMX  Mouth Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Mouth")).Key , Browsable=false})),
					Config.Bind("Defaults", "ABMX  Hair Default" , 50f, new ConfigDescription("Set default value on maker startup", null,
					new ConfigurationManagerAttributes  { Order = -controlCategories.AddNReturn(new KeyValuePair<int, string>(++defaultIndex , "ABMX Hair")).Key , Browsable=false})),

				},

#if KOI_API
				initialMorphTest = Config.Bind("_Testing_", "Init morph value", 0.47f, new ConfigDescription("Used for calculations on reload (0.47 workes best)", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = index, IsAdvanced = true, ShowRangeAsPercent = false })),
				multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 5u, new ConfigDescription("Used to determine how many extra updates are done per-frame (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = index, IsAdvanced = true, ShowRangeAsPercent = false })),
#elif HONEY_API
				initialMorphTest = Config.Bind("_Testing_", "Init morph value", 0.0f, new ConfigDescription("Used for calculations on reload (0.0 workes best)", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = index, IsAdvanced = true, ShowRangeAsPercent = false })),
				multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 1u, new ConfigDescription("Used to determine how many extra updates are done per-frame (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = index, IsAdvanced = true, ShowRangeAsPercent = false })),
#endif
				initalBoobTest = Config.Bind("_Testing_", "Init Boob value", 0.05f, new ConfigDescription("Used for calculations on reload (HS2 Only)", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = index, IsAdvanced = true, ShowRangeAsPercent = false })),
				initalFaceTest = Config.Bind("_Testing_", "Init Face value", 0.05f, new ConfigDescription("Used for calculations on reload (HS2 Only)", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = index, IsAdvanced = true, ShowRangeAsPercent = false })),


				headIndex = Config.Bind("Adv1 Head", "Head Index", (int)ChaFileDefine.BodyShapeIdx.HeadSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = index, IsAdvanced = true })),

				brestIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv2 Brest", $"Brest Index {index=1}", 1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 3, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 4, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 5, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 6, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 7, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 8, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
				},
#elif KOI_API
{
					Config.Bind("Adv2 Brest", $"Brest Index {index=1}", 4, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 5, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 6, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 7, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 8, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 9, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 10, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 11, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 12, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", 13, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
				   },
#endif
				torsoIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv3 Torso", $"Torso Index {index=1}", 14, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 15, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 16, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 17, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 18, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 19, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 20, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#elif KOI_API
{
					Config.Bind("Adv3 Torso", $"Torso Index {index=1}", 14, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 15, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 16, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 17, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 18, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 19, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 20, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 21, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 22, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}", 23, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#endif
				armIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv4 Arm", $"Arm Index {index=1}", 12, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 13, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 29, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 30, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 31, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
				  },
#elif KOI_API
 {
					Config.Bind("Adv4 Arm", $"Arm Index {index=1}", 37, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 38, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 39, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 40, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 41, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 42, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv4 Arm", $"Arm Index {++index}", 43, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#endif
				buttIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv5 Butt", $"Butt Index {index=1}", 21, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv5 Butt", $"Butt Index {++index}", 22, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv5 Butt", $"Butt Index {++index}", 23, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv5 Butt", $"Butt Index {++index}", 24, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#elif KOI_API
 {
					Config.Bind("Adv5 Butt", $"Butt Index {index=1}", 26, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
					Config.Bind("Adv5 Butt", $"Butt Index {++index}", 27, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#endif
				legIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv6 Leg", $"Leg Index {index=1}", 25, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 26, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 27, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 28, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#elif KOI_API
{
					Config.Bind("Adv6 Leg", $"Leg Index {index=1}", 24, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 25, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 28, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 29, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 30, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 31, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 32, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 33, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 34, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 35, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv6 Leg", $"Leg Index {++index}", 36, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				  },
#endif
				earIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv7 Ear", $"Ear Index {index=1}", 54, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 55, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 56, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 57, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 58, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				},
#elif KOI_API
 {
					Config.Bind("Adv7 Ear", $"Ear Index {index=1}", 47, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 48, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 49, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 50, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", 51, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				},
#endif
				eyeIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv8 Eye", $"Eye Index {index=1}", 19, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 20, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 21, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 22, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 23, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 24, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 25, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 26, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 27, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 28, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 29, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 30, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 31, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				},
#elif KOI_API
{
					Config.Bind("Adv8 Eye", $"Eye Index {index=1}", 19, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 20, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 21, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 22, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 23, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 24, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 25, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 26, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 27, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 28, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 29, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 30, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 31, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 32, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 33, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 34, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 35, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 36, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv8 Eye", $"Eye Index {++index}", 37, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
				},
#endif
				mouthIndex = new List<ConfigEntry<int>>
#if HONEY_API
                {
					Config.Bind("Adv9 Mouth", $"Mouth Index {index=1}", 47, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 48, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 49, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 50, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 51, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 52, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 53, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
			   },
#elif KOI_API
 {
					Config.Bind("Adv9 Mouth", $"Mouth Index {index=1}", 41, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 42, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 43, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 44, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 45, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
					Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 46, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
			   },
#endif
			};

			//	Logger.enable = cfg.debug.Value;
			//	cfg.debug.SettingChanged += (m,n) => { Logger.enable = cfg.debug.Value; };

			cfg.charDir.SettingChanged += (m, n) =>
			{

				foreach(var hnd in CharacterApi.RegisteredHandlers)
					if(hnd.ControllerType == typeof(CharaMorpherController))
						foreach(CharaMorpherController ctrl in hnd.Instances)
						{
							//StopAllCoroutines();
							StartCoroutine(ctrl?.CoMorphTargetUpdate());
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
							StartCoroutine(ctrl?.CoMorphTargetUpdate());
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
								StartCoroutine(ctrl?.CoMorphUpdate(3));
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
								StartCoroutine(ctrl?.CoMorphUpdate(3));
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
								StartCoroutine(ctrl?.CoMorphUpdate(3));
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
		/// makes sure a path fallows the format "this/is/a/path" and not "this\is\a\path" or similar
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public static string MakeDirPath(string dir)
		{
			dir = dir.Trim().Replace('\\', '/').Replace("//", "/");

			if((dir.LastIndexOf('.') <= dir.LastIndexOf('/'))
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
	}

}