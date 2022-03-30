using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using Unity.Jobs;

#if HS2

using AIChara;
#endif



/***********************************************
  Features:

 * morph body features
 * morph face features     
 * morph ABMX body features
 * morph ABMX face features

  Planned:
                                           
 * Save morph changes to card
 * Make an in-game version affecting all but male character(s)
************************************************/



namespace CharaMorpher
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
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID, ExtensibleSaveFormat.ExtendedSave.Version)]
    public partial class CharaMorpher_Core : BaseUnityPlugin
    {

        // Expose both your GUID and current version to allow other plugins to easily check for your presence and version, for example by using the BepInDependency attribute.
        // Be careful with public const fields! Read more: https://stackoverflow.com/questions/55984
        // Avoid changing GUID unless absolutely necessary. Plugins that rely on your plugin will no longer recognize it, and if you use it in function controllers you will lose all data saved to cards before the change!
        public const string ModName = "Character Morpher";
        public const string GUID = "prolo.chararmorpher";
        public const string Version = "0.1.21";

        internal static CharaMorpher_Core Instance;
        internal static new ManualLogSource Logger;
        public MyConfig cfg;

        public struct MyConfig
        {
            //ABMX
            public ConfigEntry<bool> enableABMX { set; get; }


            //Main
            public ConfigEntry<bool> enable { set; get; }
            public ConfigEntry<bool> enableInGame { set; get; }
            public ConfigEntry<string> charDir { set; get; }
            public ConfigEntry<string> imageName { set; get; }
            public ConfigEntry<uint> sliderExtents { set; get; }
            //public ConfigEntry<uint> awaitTime { set; get; }

            public List<ConfigEntry<float>> defaults { set; get; }


            //Advanced (show up at the top)
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

        private void Awake()
        {

            Instance = this;
            Logger = base.Logger;


            int index = 0;//easier to input index order values

           string femalepath =UnityEngine.Application.dataPath+ "/../UserData/chara/female/";

            cfg = new MyConfig
            {
                enable = Config.Bind("_Main_", "Enable", true, new ConfigDescription("Allows the plugin to run (may need to reload if results are not changing)", null, new ConfigurationManagerAttributes { Order = --index })),
                enableInGame = Config.Bind("_Main_", "Enable in Game", true, new ConfigDescription("Allows the plugin to run while in main game", null, new ConfigurationManagerAttributes { Order = --index })),
                enableABMX = Config.Bind("_Main_", "Enable ABMX", true, new ConfigDescription("Allows ABMX to be affected (may need to reload scene if results become wonky)", null, new ConfigurationManagerAttributes { Order = --index })),
                charDir = Config.Bind("_Main_", "Directory Path", new ChaFileControl().ConvertCharaFilePath(femalepath, 255), new ConfigDescription("Directory where character is stored", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
                imageName = Config.Bind("_Main_", "Card Name", new ChaFileControl().ConvertCharaFilePath("", 255), new ConfigDescription("The image used to morph", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true, Browsable = true })),
                sliderExtents = Config.Bind("_Main_", "Slider Extents", 200u, new ConfigDescription("How far the slider values go above default (e.i. setting value to 10 gives values -10 -> 110)", null, new ConfigurationManagerAttributes { Order = --index, DefaultValue = true })),
               
                //you don't need to see this in game
                defaults = new List<ConfigEntry<float>>{
                    Config.Bind("Defaults", "Body  Default" , 100f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = index = 0, Browsable=false })),
                    Config.Bind("Defaults", "Head  Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index, Browsable=false })),
                    Config.Bind("Defaults", "Boobs Default", 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "Butt  Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index, Browsable=false })),
                    Config.Bind("Defaults", "Torso Default", 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "Arms  Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index, Browsable=false })),
                    Config.Bind("Defaults", "Legs  Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index, Browsable=false })),

                    Config.Bind("Defaults", "Face  Default" , 100f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "Ears  Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "Eyes  Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "Mouth Default" , 50f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),


                    Config.Bind("Defaults", "ABMX  Body Default" , 100f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index    , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Boobs Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index    , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Butt Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index     , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Torso Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index    , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Arms Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index     , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Hands Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order =--index     , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Legs Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes  { Order = --index    , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Feet Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes  { Order = --index    , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Genitals Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),


                    Config.Bind("Defaults", "ABMX  Head Default" , 100f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Ears Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes  { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Eyes Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes  { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Mouth Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes { Order = --index , Browsable=false})),
                    Config.Bind("Defaults", "ABMX  Hair Default" , 00f, new ConfigDescription("Set default value on maker startup", null, new ConfigurationManagerAttributes  { Order = --index , Browsable=false})),
                },

                headIndex = Config.Bind("Adv1 Head", "Head Index", 1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = index, IsAdvanced = true })),
                brestIndex = new List<ConfigEntry<int>>
#if HS2
                {
                    Config.Bind("Adv2 Brest", $"Brest Index {index=1}", 2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 3, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 4, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 5, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 6, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 7, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 8, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 9, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                    Config.Bind("Adv2 Brest", $"Brest Index {++index}", 10, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })),
                },
#elif KKSS
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
#if HS2
                {
                    Config.Bind("Adv3 Torso", $"Torso Index {index=1}", 14, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv3 Torso", $"Torso Index {++index}", 15, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv3 Torso", $"Torso Index {++index}", 16, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv3 Torso", $"Torso Index {++index}", 17, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv3 Torso", $"Torso Index {++index}", 18, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv3 Torso", $"Torso Index {++index}", 19, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv3 Torso", $"Torso Index {++index}", 20, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                  },
#elif KKSS
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
#if HS2
                {
                    Config.Bind("Adv4 Arm", $"Arm Index {index=1}", 12, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv4 Arm", $"Arm Index {++index}", 13, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
                    Config.Bind("Adv4 Arm", $"Arm Index {++index}", 29, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
                    Config.Bind("Adv4 Arm", $"Arm Index {++index}", 30, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
                    Config.Bind("Adv4 Arm", $"Arm Index {++index}", 31, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
                  },
#elif KKSS
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
#if HS2
                {
                    Config.Bind("Adv5 Butt", $"Butt Index {index=1}", 21, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv5 Butt", $"Butt Index {++index}", 22, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv5 Butt", $"Butt Index {++index}", 23, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv5 Butt", $"Butt Index {++index}", 24, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                  },
#elif KKSS
 {
                    Config.Bind("Adv5 Butt", $"Butt Index {index=1}", 26, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
                    Config.Bind("Adv5 Butt", $"Butt Index {++index}", 27, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 44), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                  },
#endif
                legIndex = new List<ConfigEntry<int>>
#if HS2
                {
                    Config.Bind("Adv6 Leg", $"Leg Index {index=1}", 25, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})),
                    Config.Bind("Adv6 Leg", $"Leg Index {++index}", 26, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv6 Leg", $"Leg Index {++index}", 27, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv6 Leg", $"Leg Index {++index}", 28, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 32), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                  },
#elif KKSS
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
#if HS2
                {
                    Config.Bind("Adv7 Ear", $"Ear Index {index=1}", 54, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 55, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 56, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 57, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 58, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                },
#elif KKSS
 {
                    Config.Bind("Adv7 Ear", $"Ear Index {index=1}", 47, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 48, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 49, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 50, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv7 Ear", $"Ear Index {++index}", 51, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 52), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                },
#endif
                eyeIndex = new List<ConfigEntry<int>>
#if HS2
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
#elif KKSS
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
#if HS2
                {
                    Config.Bind("Adv9 Mouth", $"Mouth Index {index=1}", 47, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 48, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 49, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 50, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 51, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 52, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
                    Config.Bind("Adv9 Mouth", $"Mouth Index {++index}", 53, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})),
               },
#elif KKSS
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

            cfg.enable.SettingChanged += (m, n) =>
            {
                foreach(var hnd in KKAPI.Chara.CharacterApi.RegisteredHandlers)
                    if(hnd.ControllerType == typeof(CharaMorpherController))
                        foreach(CharaMorpherController ctrl in hnd.Instances)
                            ctrl.MorphChangeUpdate();
            };
            cfg.enableInGame.SettingChanged += (m, n) =>
            {
                foreach(var hnd in KKAPI.Chara.CharacterApi.RegisteredHandlers)
                    if(hnd.ControllerType == typeof(CharaMorpherController))
                        foreach(CharaMorpherController ctrl in hnd.Instances)
                            ctrl.MorphChangeUpdate();
            };
            cfg.enableABMX.SettingChanged += (m, n) =>
            {
                foreach(var hnd in KKAPI.Chara.CharacterApi.RegisteredHandlers)
                    if(hnd.ControllerType == typeof(CharaMorpherController))
                        foreach(CharaMorpherController ctrl in hnd.Instances)
                            ctrl.MorphChangeUpdate();
            };


            if(StudioAPI.InsideStudio) return;

            // Register your logic that depends on a character.
            // A new instance of this component will be added to ALL characters in the game.
            // The GUID will be used as the ID of the extended data saved to character
            // cards, scenes and game saves, so make sure it's unique and do not change it!
            CharacterApi.RegisterExtraBehaviour<CharaMorpherController>(GUID);


            CharaMorpherGUI.Initialize();
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);




        }

    }
}

