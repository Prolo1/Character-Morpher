
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Il2CppSystem.Collections;
using System.Text;

using ConfigurationManager;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

using ProloAPI;
using ProloAPI.Extensions;
using BepInEx.Configuration;
using BepInEx.Logging;
using ProloAPI.Utilities;
using static ChaFileDefine;
using static ProloAPI.Utilities.PGUI;
using static ProloAPI.Utilities.PGeneral;

//using CharaMorphUnityPlugin = ProloAPI.ProloUnityPluginIL2CPP<Character_Morpher_IL2CPP.CharaMorpherIL2CPP_Core>;
using UnityEngine.Events;
using Character;
namespace Character_Morpher_IL2CPP
{
	using System.Text.RegularExpressions;

	using BepInEx.Unity.IL2CPP.Utils.Collections;

	using ILLGames.Extensions;

	using static UnityEngine.ImageConversion;
	using static CharacterMorpherIL2CPP_GUI;

	[BepInPlugin(GUID, ModName, Version)]
	public class CharaMorpherIL2CPP_Manager : ProloPluginManager<CharaMorpherIL2CPP_Core>
	{
		// Expose both your GUID and current version to allow other plugins to easily check for your presence and version, for example by using the BepInDependency attribute.
		// Be careful with public const fields! Read more: https://stackoverflow.com/questions/55984
		// Avoid changing GUID unless absolutely necessary. Plugins that rely on your plugin will no longer recognize it, and if you use it in function controllers you will lose all data saved to cards before the change!
		public const string ModName = "Character Morpher";
		public const string GUID = "prolo.chararmorpher";//never change this
		public const string Version = "1.2.1.7";

		public override void Load()
		{
			base.Load();
			var name = Assembly.GetAssembly(this.GetType())?.FullName;
			Log.LogInfo($"Name of This Game: {name}");
		}
	}

	public partial class CharaMorpherIL2CPP_Core : ProloBaseUnityPluginIL2CPP
	{

		#region variables

		private const bool testing = false;

		public const string strDiv = ":";
		public const string defaultStr = "(Default)" + strDiv;

		//public static new MorphConfig cfg { get => ProloUnityPlugin.cfg; }
		//public static new CharacterMorpherIL2CPP_Core Instance { get => ProloUnityPlugin.Instance; }
		//public static new ManualLogSource Logger { get => ProloUnityPlugin.Logger; }


		internal static OnNewImage OnNewTargetImage { get; private set; }
		internal static OnValueChange<MorphControls> OnInternalSliderValueChange { get; private set; }
		internal static OnControlSetValueChange OnInternalControlListChanged { get; private set; }


		public static new CharaMorpherIL2CPP_Core Instance { get => (CharaMorpherIL2CPP_Core)(Instances?.First((inst) => inst is CharaMorpherIL2CPP_Core)); }
		public static new ManualLogSource Logger { get => (Instances?.First((inst) => inst is CharaMorpherIL2CPP_Core))?.Logger; }
		public static MorphConfig cfg;


		internal static Texture2D UIGoku = null;
		internal static Texture2D iconBG = null;

		public readonly Dictionary<string, List<MorphSliderData>> controlCategories = new Dictionary<string, List<MorphSliderData>>();
		public struct MorphConfig : IConfiguration
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
			public ConfigEntry<bool> userDefaultAsDefault { set; get; }
			public ConfigEntry<bool> enableTooltips { set; get; }
			public ConfigEntry<bool> nukeStudio { set; get; }
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

		//public CharacterMorpherIL2CPP_Core()
		//{
		//
		//	Debug.Log("Logging does work so that is good");
		//	ForeGrounder.SetCurrentForground();
		//
		//	info = new ProloInfo { GUID = GUID, ModName = ModName, Version = Version };
		//	Instance = this;
		//	//cfg = cfg;
		//	SetApiInst(this);
		//
		//	Logger.LogInfo("Logging does work so that is good");
		//
		//}

		protected void Awake()
		{
			OnNewTargetImage = new OnNewImage();
			OnInternalSliderValueChange = new OnValueChange<MorphControls>();
			OnInternalControlListChanged = new OnControlSetValueChange();


			Logger?.LogInfo(ProInfo);
			Logger?.LogInfo("Logging does work so that is good");



			//Logger.LogInfo("got past dependencies");
			//Embedded Resources
			using(MemoryStream memStream = new MemoryStream())
			{
				var assembly = Assembly.GetExecutingAssembly();
				var resources = assembly.GetManifestResourceNames();
				MemoryStream ResourceGrabber(string name, Assembly ass = null, string[] res = null, MemoryStream mem = null)
				{
					/**This stuff will be used later*/
					//Logger.LogDebug($"\nResources:\n[{string.Join(", ", resources)}]");
					ass = ass ?? Assembly.GetExecutingAssembly();
					res = res ?? ass.GetManifestResourceNames();
					mem = mem ?? new MemoryStream();

					var data = ass.GetManifestResourceStream(res.FirstOrDefault((txt) => (txt.ToLower()).Contains(name)) ?? " ");

					mem.SetLength(0);//Clear Buffer 
					data?.CopyTo(mem);

					return mem;
				}

				//	var data = assembly.GetManifestResourceStream(resources.FirstOrDefault((txt) => (txt.ToLower()).Contains("ultra instinct")) ?? " ");
				ResourceGrabber("ultra instinct", assembly, resources, memStream);
				UIGoku = new Texture2D(1, 1);

				UIGoku.LoadImage(memStream?.GetBuffer());
				UIGoku.Compress(false);
				UIGoku.Apply();

				//data = assembly.GetManifestResourceStream(resources.FirstOrDefault((txt) => (txt.ToLower()).Contains("studio morph icon.png")) ?? " ");
				ResourceGrabber("studio morph icon.png", assembly, resources, memStream);
				iconBG = new Texture2D(1, 1);
				iconBG.LoadImage(memStream?.GetBuffer());
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

			//Logger.LogInfo("got past resources");
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

				MorphSliderData.CreateTypeConverter();
			}

			//Logger.LogInfo("got past Type converters");

			string femalepath = Path.Combine(Paths.GameRootPath, "UserData/chara/female/").MakeDirPath();
			int bodyBoneAmount =
			//cf_bodyshapename.Length - 1;
			sizeof(BodyShapeIdx) / 4 - 1;
			int faceBoneAmount =
			//cf_headshapename.Length - 1;
			sizeof(FaceShapeIdx) / 4 - 1;
			Logger?.LogDebug($"Body bones amount: {bodyBoneAmount + 1}");
			Logger?.LogDebug($"Face bones amount: {faceBoneAmount + 1}");


			int index = 0, secIndex = 0, secIndex2 = 99;//easier to input index order values

			#region config alias
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
			#endregion
			//Logger.LogInfo("got past variable settings");
			var Config = manager.Plugin.Config;

			var saveCfgAuto = Config.SaveOnConfigSet;
			Config.SaveOnConfigSet = false;


			//Config settings
			//Logger.LogInfo("Got to setting the config");
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
				userDefaultAsDefault = Config.Bind(main, "User Default As Default", true,
				new ConfigDescription("Enabling this option allows new slots to be populated " +
				"with the same values as the '[default]' slot", null,
				new ConfigurationManagerAttributes { Order = --index, Category = mainx })),
				enableTooltips = Config.Bind(main, "Enable Tooltips", true,
				new ConfigDescription("Enables tooltips in Maker and Studio so you can see whatever the hell these buttons do", null,
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
					CustomDrawer = ButtonDrawer(onClick: () =>
					{
						var ctrls = GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>();

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
					CustomDrawer = MyImageButtonDrawer,
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
					CustomDrawer = MySelectionListDrawer,
					ObjToStr = (o) => "",
					StrToObj = (s) => null
				})),

				//you don't need to see this in game
				defaults = new Dictionary<string, Dictionary<string, ConfigEntry<MorphSliderData>>>(),
				//defaultModes = new Dictionary<string, Dictionary<string, ConfigEntry<Tuple<string, int>>>>(),


				//Studio
				nukeStudio = Config.Bind(stud, "Nuke Studio", true,
				new ConfigDescription("Studio implementation will not work at all (will take affect on next launch)", tags:
				new ConfigurationManagerAttributes()
				{
					Order = --index,
					Category = studx,
				})),
				studioWinRec = Config.Bind(stud, "Studio Win Rec", CharacterMorpherIL2CPP_GUI.winRec,
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
					Browsable = false
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

			//Logger.LogInfo("Finished setting the config");
			//Advanced
			{
				//	var cfg = this.cfg;

				cfg.debug.ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.nukeStudio.ConfigDefaulter(true);
				cfg.makerViewportUISpace = Config.Bind(adv, "Viewport UI Space",
#if HONEY_API
					.73f,
#else
					.86f,
#endif
					new ConfigDescription("Increase / decrease the Fashion Line viewport size ",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes()
					{
						Order = index--,
						ShowRangeAsPercent = false,
						IsAdvanced = true,
						Category = advx
					})).ConfigDefaulter(cfg.resetOnLaunch.Value);

				//Tests
				cfg.unknownTest = Config.Bind(tst, "Unknown Test value", 20,
					new ConfigDescription("Used for whatever the hell I WANT (if you see this I forgot to take it out). RESETS ON GAME LAUNCH", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				//	cfg.initialMorphTest = Config.Bind(tst, "Init morph value", 1.00f, new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH", new AcceptableValueRange<float>(0, 1), new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.multiUpdateEnableTest = Config.Bind(tst, "Multi Update Enable value", 5u,
					new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.multiUpdateSliderTest = Config.Bind(tst, "Multi Update Slider value", 0u,
					new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);


#if KOI_API
				//cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.initialMorphFaceTest = Config.Bind(tst, "Init morph Face value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.initialMorphBodyTest = Config.Bind(tst, "Init morph Body value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.reloadTest = Config.Bind(tst, "Reload delay value", 22u,
					new ConfigDescription("Used to change the amount of frames to delay before loading. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
#elif HONEY_API
				cfg.initialMorphFaceTest = Config.Bind(tst, "Init morph Face value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.initialMorphBodyTest = Config.Bind(tst, "Init morph Body value", 0.00f,
					new ConfigDescription("Used for calculations on reload. Changing this may cause graphical errors (or fix them). RESETS ON GAME LAUNCH",
					new AcceptableValueRange<float>(0, 1),
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				//cfg.multiUpdateTest = Config.Bind("_Testing_", "Multi Update value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				//cfg.multiUpdateEnableTest = Config.Bind("_Testing_", "Multi Update Enable value", 5u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				//cfg.multiUpdateSliderTest = Config.Bind("_Testing_", "Multi Update Slider value", 0u, new ConfigDescription("Used to determine how many extra updates are done per-frame. RESETS ON GAME LAUNCH (fixes odd issue)", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
				cfg.reloadTest = Config.Bind(tst, "Reload delay value", 22u,
					new ConfigDescription("Used to change the amount of frames to delay before loading. RESETS ON GAME LAUNCH (fixes odd issue)", null,
					new ConfigurationManagerAttributes { Order = --index, Category = tstx, Browsable = testing, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);
#endif
				//	cfg.fullBoneResetTest = Config.Bind("_Testing_", "Full Bone Reset Delay", 3u, new ConfigDescription("Used to determine how long to wait for full bone reset. RESETS ON GAME LAUNCH", null, new ConfigurationManagerAttributes { Order = --index, IsAdvanced = true, ShowRangeAsPercent = false })).ConfigDefaulter(cfg.resetOnLaunch.Value);


				cfg.headIndex = new List<ConfigEntry<int>>{
					Config.Bind("Adv1 Head", $"Head Index {index=1}", (int)BodyShapeIdx.HeadSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv1 Head", $"Head Index {++index}", (int)BodyShapeIdx.NeckSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
			 	};

				cfg.brestIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv2 Brest", $"Brest Index {index=1}", (int)BodyShapeIdx.AreolaBulge, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					 Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustRotY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustSharp, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.NipStand, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.NipWeight, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),

					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustForm1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv2 Brest", $"Brest Index {++index}", (int)BodyShapeIdx.BustForm2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced = true })).ConfigDefaulter(cfg.resetOnLaunch.Value),

				};

				cfg.torsoIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv3 Torso", $"Torso Index {index=1}",  (int)BodyShapeIdx.BodyLowW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.BodyLowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.BodyShoulderW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.BodyShoulderZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.BodyUpW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.BodyUpZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.WaistUpW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.WaistUpZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),


					Config.Bind("Adv3 Torso", $"Torso Index {++index}",  (int)BodyShapeIdx.Belly, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),

				  };

				cfg.armIndex = new List<ConfigEntry<int>>
				{

						Config.Bind("Adv4 Arm", $"Arm Index {index=1}", (int)BodyShapeIdx.ArmFront, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),

						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.ArmUpSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.ShoulderW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.ShoulderZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.BodyShoulderW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.BodyShoulderZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),


						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.ArmUpSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv4 Arm", $"Arm Index {++index}", (int)BodyShapeIdx.ElbowSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index , IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),


				 };

				cfg.buttIndex = new List<ConfigEntry<int>>
				{
						Config.Bind("Adv5 Butt", $"Butt Index {index=1}", (int)BodyShapeIdx.HipSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv5 Butt", $"Butt Index {++index}", (int)BodyShapeIdx.HipRotX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv5 Butt", $"Butt Index {++index}", (int)BodyShapeIdx.WaistY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv5 Butt", $"Butt Index {++index}", (int)BodyShapeIdx.WaistLowZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),

				 };

				cfg.legIndex = new List<ConfigEntry<int>>
				{
						Config.Bind("Adv6 Leg", $"Leg Index {index=1}", (int)BodyShapeIdx.Calf, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(cfg.resetOnLaunch.Value),



						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)BodyShapeIdx.AnkleSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)BodyShapeIdx.ThighLowSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)BodyShapeIdx.ThighUpSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})) .ConfigDefaulter(cfg.resetOnLaunch.Value),


						 Config.Bind("Adv6 Leg", $"Leg Index {++index}", (int)BodyShapeIdx.KneeSize , new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, bodyBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),

				  };

				cfg.earIndex = new List<ConfigEntry<int>>
				{

					Config.Bind("Adv7 Ear", $"Ear Index {index=1}", (int)FaceShapeIdx.EarLowForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)FaceShapeIdx.EarRotY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)FaceShapeIdx.EarRotZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)FaceShapeIdx.EarSize, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv7 Ear", $"Ear Index {++index}", (int)FaceShapeIdx.EarUpForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),


				};

				cfg.eyeIndex = new List<ConfigEntry<int>>
				{

						Config.Bind("Adv8 Eye", $"Eye Index {index=1}", (int)FaceShapeIdx.EyeH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyeW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyeX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyeY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyeZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),


						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyebrowInForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyebrowOutForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyebrowRotZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyebrowX, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyebrowY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyelidsLowForm1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyelidsLowForm2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyelidsLowForm3, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyelidsUpForm1, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyelidsUpForm2, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyelidsUpForm3, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
						Config.Bind("Adv8 Eye", $"Eye Index {++index}", (int)FaceShapeIdx.EyeTilt, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, 58), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),

				};

				cfg.noseIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv9 Nose", $"Nose Index {index=1}", (int)FaceShapeIdx.NoseBridgeH, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)FaceShapeIdx.NoseTipH , new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv9 Nose", $"Nose Index {++index}", (int)FaceShapeIdx.NoseY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
				};

				cfg.mouthIndex = new List<ConfigEntry<int>>
				{
					Config.Bind("Adv10 Mouth", $"Mouth Index {index=1}", (int)FaceShapeIdx.MouthCornerFormY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)FaceShapeIdx.MouthCornerFormRot, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)FaceShapeIdx.MouthLowForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)FaceShapeIdx.MouthUpForm, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)FaceShapeIdx.MouthW, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)FaceShapeIdx.MouthY, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),
					Config.Bind("Adv10 Mouth", $"Mouth Index {++index}", (int)FaceShapeIdx.MouthZ, new ConfigDescription("for testing only", new AcceptableValueRange<int>(0, faceBoneAmount), new ConfigurationManagerAttributes { Order = -index, IsAdvanced=true})).ConfigDefaulter(cfg.resetOnLaunch.Value),

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

			Config.Save();
			Config.SaveOnConfigSet = saveCfgAuto;

			Logger.LogInfo("Got to populating defaults");

			//populate defaults
			PopulateDefaultSettings(defaultStr);
			UpdateDefaultsList();


			Logger.LogInfo("Got to making path");
			string p = Path.Combine(cfg.charDir.Value.MakeDirPath(), cfg.imageName.Value.MakeDirPath());
			Logger.LogInfo("Got to making texture");
			CharacterMorpherIL2CPP_GUI.morphTex = p.CreateTexture();

			Logger.LogInfo("Got to making callbacks");

			//if it's needed
			if(cfg.unknownTest != null)
				cfg.unknownTest.SettingChanged += (m, n) =>
				{

				};

			cfg.charDir.SettingChanged += (m, n) =>
			{

				string path = Path.Combine(cfg.charDir.Value.MakeDirPath(), cfg.imageName.Value.MakeDirPath());
				foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
				{
					if(File.Exists(path))
						if(ctrl.IsInitLoadFinished)
							ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5).WrapToIl2Cpp());
				}
			};

			cfg.imageName.SettingChanged += (m, n) =>
			{

				string path = Path.Combine(cfg.charDir.Value.MakeDirPath(), cfg.imageName.Value.MakeDirPath());
				foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
				{
					if(File.Exists(path))
						if(ctrl.IsInitLoadFinished)
							ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5).WrapToIl2Cpp());
				}
			};

			//cfg.currentControlSetName.SettingChanged += (m, n) =>
			//{
			//	CharacterMorpherIL2CPP_GUI.UpdateGUISelectList();
			//};

			cfg.preferCardMorphDataMaker.SettingChanged += (m, n) =>
			{
				//if(MakerAPI.InsideMaker || StudioAPI.InsideStudio)
				//	foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherControllerIL2CPP>())
				//		if(ctrl.IsInitLoadFinished)
				//			ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
			};

			cfg.preferCardMorphDataGame.SettingChanged += (m, n) =>
			{
				//if(!MakerAPI.InsideMaker && !StudioAPI.InsideStudio)
				//	foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherControllerIL2CPP>())
				//		if(ctrl.IsInitLoadFinished)
				//			ctrl?.StartCoroutine(ctrl?.CoMorphTargetUpdate(5));
			};

			cfg.onlyMorphCharWithDataInGame.SettingChanged += (m, n) =>
			{
				//	if(!MakerAPI.InsideMaker && !StudioAPI.InsideStudio)
				//		foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherControllerIL2CPP>())
				//			for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
				//				ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1, forceReset: !ctrl.Enable));
			};

			cfg.enable.SettingChanged += (m, n) =>
			{
				foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl?.StartCoroutine(ctrl.CoMorphChangeUpdate(delay: a + 1, forceReset: !cfg.enable.Value).WrapToIl2Cpp());

				Logger.LogMessage(cfg.enable.Value ?
									"Character Morpher Enabled" :
									"Character Morpher Disabled");


			};

			cfg.enableInGame.SettingChanged += (m, n) =>
			{
				foreach(CharacterMorpherIL2CPP_Controller ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1).WrapToIl2Cpp());
			};

			cfg.enableABMX.SettingChanged += (m, n) =>
			{
				//if(!ABMXDependency.IsInTargetVersionRange)
				//{
				//	//if(cfg.enableABMX.Value)
				//	//	cfg.enableABMX.Value = false;
				//
				//
				//	return;
				//}

				//foreach(CharacterMorpherControllerIL2CPP ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherControllerIL2CPP>())
				//{
				//	for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
				//		ctrl?.StartCoroutine(ctrl?.CoMorphChangeUpdate(a + 1));
				//}
			};

			Logger.LogInfo("Got to here maybe 1?");
			cfg.studioWinRec.SettingChanged += (m, n) =>
			{
				if(!cfg.studioWinRec.Value.Equals(winRec))
					winRec = new Rect(cfg.studioWinRec.Value);
			};

			Logger.LogInfo("Got to here maybe 2?");
			//cfg.makerViewportUISpace.SettingChanged += (m, n) =>
			//{
			//	select.ResizeCustomUIViewport(cfg.makerViewportUISpace.Value);
			//};

			Logger.LogInfo("Got to here maybe 3?");
			// useCardMorphDataGame()
			{
				Coroutine tmp = null;
				bool lastUCMD = cfg.preferCardMorphDataGame.Value;//this is needed

				cfg.preferCardMorphDataGame.SettingChanged += (m, n) =>
				{
					//if(MakerAPI.InsideMaker)
					//{
					//	lastUCMD = cfg.preferCardMorphDataGame.Value;//this is needed
					//	return;
					//}


					System.Collections.IEnumerator CoUCMD()
					{

						foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
						{
							string name =
							(!cfg.preferCardMorphDataGame.Value ?
							ctrl?.ctrls1 : (ctrl?.ctrls2 ?? ctrl?.ctrls1))?.currentSet;
							name = name.Substring(0, Mathf.Clamp(name.LastIndexOf(strDiv), 0, name.Length));


							//Logger.LogDebug($"lastUCMD: {lastUCMD}");
							yield return new WaitWhile(new Func<bool>(() => ctrl.IsReloading));

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
					tmp = StartCoroutine(CoUCMD().WrapToIl2Cpp());
				};

			}

			Logger.LogInfo("Got to nuke studio?");

			if(cfg.nukeStudio.Value) return;//this is one of my favourite lines 🤣


			Logger.LogInfo("Got to regester things?");
			/*
				Register your logic that depends on a character.
				A new instance of this component will be added to ALL characters in the game.
				The GUID will be used as the ID of the extended data saved to character
				cards, scenes and game saves, so make sure it's unique and do not change it!
			 */
			CharacterMorpherIL2CPP_GUI.Initialize();
			Hooks.Init();
			ProloHooks.characterCreatedEvent
				.AddListener(new Action<Human>(h => { h.component.GetOrAddComponent<CharacterMorpherIL2CPP_Controller>(); }));
			Logger.LogInfo("finished everything");
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
				var ctrl = GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>().FirstOrNull();

				//if(MakerAPI.InsideMaker && ctrl)
				ctrl.Enable = !ctrl.morphEnable;

				//else if(StudioAPI.InsideStudio)
				//	foreach(var ctrler in StudioAPI.GetSelectedControllers<CharacterMorpherControllerIL2CPP>())
				//		ctrler.Enable = !ctrler.morphEnable;
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

				foreach(var ctrl in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(ctrl?.CoMorphChangeUpdate(delay: a + 1).WrapToIl2Cpp());

				Logger?.LogMessage($"Switched to new slot [{cfg.currentControlSetName.Value}]");
			}
		}

		#region game functions
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
			var orphaned = Instance.manager.Plugin.Config.GetUnorderedOrphanedEntries("Defaults");
			var defList = orphaned.Select((k) => k.Key).ToList();

			//orphaned = Instance.Config.GetUnorderedOrphanedEntries("Mode Defaults");
			//var modeDefList = orphaned.Attempt((k) => k.Key);


			var saveCfgAuto =
				Instance.manager.Plugin.Config.SaveOnConfigSet;
			Instance.manager.Plugin.Config.SaveOnConfigSet = false;

			foreach(var val in defList)
			{
				var slotName = val.Key.Substring(0, val.Key.LastIndexOf(strDiv) + 1)?.Trim();
				var settingName = val.Key.Substring(val.Key.LastIndexOf(strDiv) + 1);

				//Logger.LogDebug($"For start");
				//Logger.LogDebug($"val.key: {val.Key}");



				if(!cfg.defaults.TryGetValue(slotName, out var tmp) && !slotName.IsNullOrEmpty())
					PopulateDefaultSettings(slotName);

				if(!cfg.defaults.TryGetValue(slotName, out var tmp2))
					cfg.defaults[slotName] = new Dictionary<string, ConfigEntry<MorphSliderData>>();

				if(!Instance.controlCategories.TryGetValue(slotName, out var tmp3))
					Instance.controlCategories[slotName] =
						new List<MorphSliderData>(Instance.controlCategories[defaultStr]);

				ConfigEntry<MorphSliderData> lastConfig = null;

				var oldData = oldConversionList.FirstOrNull((a) => a[1] == settingName);
				string convertStr = oldData?[0].Trim() ?? "";
				bool isAbmx = bool.Parse(oldData?[2] ?? bool.FalseString);

				if(!cfg.defaults[slotName].Any((k) => k.Value.Definition.Key == val.Key))
					cfg.defaults[slotName].Add(
						convertStr,
						lastConfig = Instance.manager.Plugin.Config.Bind(val, Instance.controlCategories[slotName].
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
					lastConfig = (ConfigEntry<MorphSliderData>)Instance.manager.Plugin.Config[val];

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
						Instance.manager.Plugin.Config.Remove(val);
						if(val.Key.LastIndexOf(" Mode") == (val.Key.Length - " Mode".Length))
							Instance.manager.Plugin.Config.Bind(val, calc, new ConfigDescription("", tags:
						new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false, }));
						else
							Instance.manager.Plugin.Config.Bind(val, data, new ConfigDescription("", tags:
						new ConfigurationManagerAttributes { IsAdvanced = true, Browsable = false, }));
					}
				}

				//	Logger.LogDebug($"UpdateDefaultsList Name: {name}");
			}

			Instance.manager.Plugin.Config.Save();
			Instance.manager.Plugin.Config.SaveOnConfigSet = saveCfgAuto;
			cfg.oldControlsConversion.Value = false;
		}

		public static int SwitchControlSet(string[] selection, int val, bool keepProgress = true, CharacterMorpherIL2CPP_Controller ctrl = null)
		{
			//var ctrl = GetAllChaFuncCtrlOfType<CharacterMorpherControllerIL2CPP>().First();
			if(selection is null || selection.Length < 1) return -1;

			if(cfg.debug.Value) Logger.LogDebug($"current slot [{(ctrl?.controls?.currentSet ?? cfg.currentControlSetName.Value)}]");

			var name = selection[val = Mathf.Clamp(val, 0, selection.Length - 1)].Trim();

			name = (name.LastIndexOf(strDiv) == name.Length - strDiv.Length ?
					name.Substring(0, name.LastIndexOf(strDiv)) : name) + strDiv;

			//Replace the new set with the last settings before changing the setting name (loading will call the actual settings)
			if((ctrl?.controls?.currentSet ?? cfg.currentControlSetName.Value) != name)
				if(ctrl)
				{
					if(keepProgress && ctrl.controls.all.  //made so you don't loose your																              
								TryGetValue(ctrl.controls.currentSet, out var tmp))        //progress when switching in maker
						ctrl.controls.all[name] = tmp.ToDictionary(k => k.Key, v => v.Value.Clone());

					ctrl.controls.currentSet = name;
				}
				else
				{
					foreach(var ctrl1 in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
					{

						if(keepProgress && ctrl1.controls.all.  //made so you don't loose your																              
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
		public static int SwitchControlSet(string[] selection, string val, bool keepProgress = true, CharacterMorpherIL2CPP_Controller ctrl = null) =>
			selection is null ? -1 : SwitchControlSet(selection,
				val is null ? -1 :
				Array.IndexOf(selection, val.Trim().LastIndexOf(strDiv) == val.Trim().Length - strDiv.Length ?
					val.Trim().Substring(0, val.Trim().LastIndexOf(strDiv)) : val.Trim()), keepProgress, ctrl);//will automatically remove "strDivider" if at end of string"
		public static void UpdateDropdown(ICollection<string> selector)
		{

			selector?.Clear();
			foreach(var key in cfg.defaults.Keys)
				selector?.Add(key);

			//	Logger.LogDebug($"current List [{string.Join(", ", selector?.ToArray() ?? new string[] { })}]");

			//selecter.Value = 
			SwitchControlSet(cfg.defaults.Keys.ToArray(), selectedMod);

		}

		public static void PopulateDefaultSettings(string name, bool useUserDefault = false)
		{
			var ctrl1 = GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>()?.FirstOrNull();
			if(name.LastIndexOf(strDiv) != (name.Length - strDiv.Length)) name += strDiv;

			Instance.controlCategories[name] = new List<MorphSliderData> { };//init list

			//START MANUAL CONFIG SAVE
			var saveCfgAuto = Instance.manager.Plugin.Config.SaveOnConfigSet;
			Instance.manager.Plugin.Config.SaveOnConfigSet = false;

			try
			{
				//use user defaults
				if(useUserDefault)
					foreach(var thing in cfg.defaults[name])
						thing.Value.Value = cfg.defaults[defaultStr][thing.Key].Value.Clone();

			}
			catch(Exception e) { Logger.LogError(e); }

			//END MANUAL CONFIG SAVE
			Instance.manager.Plugin.Config.SaveOnConfigSet = saveCfgAuto;
			Instance.manager.Plugin.Config.Save();
		}

		public static string AddNewSetting(string baseName = "Slot", CharacterMorpherIL2CPP_Controller ctrl1 = null, bool useUserDefault = false)
		{
			int count = 1;
			ctrl1 = ctrl1 ?? GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>()?.FirstOrNull();

			string name = "Error" + strDiv;
			var defList = Instance.controlCategories.Keys.ToList();
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

			//	Logger.LogDebug("creating Defaults");
			PopulateDefaultSettings(name, useUserDefault);

			//Logger.LogDebug("creating Controls");
			foreach(var ctrl2 in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
			{
				ctrl2.controls.all[name] = new Dictionary<string, MorphSliderData>();

				var data = ctrl2.ctrls1?.all;
				if(!data.ContainsKey(name))
					data[name] = new Dictionary<string, MorphSliderData>();

			}
			foreach(var ctrl2 in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
				foreach(var ctrl in Instance.controlCategories[name])
				{
					var tmp2 = Instance.controlCategories[defaultStr].Find(v => v.dataName == ctrl.dataName);
					ctrl2.controls.all[name][ctrl.dataName] = tmp2.Clone();

					var data =
						ctrl2.ctrls1?.all;

					if(!data[name].ContainsKey(ctrl.dataName))
						data[name][ctrl.dataName] = tmp2.Clone();
				}

			Logger.LogMessage($"Created {name}");

			return name;
		}

		public static void RemoveCurrentSetting(string baseName, CharacterMorpherIL2CPP_Controller ctrl = null)
		{
			ctrl = ctrl ?? GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>()?.FirstOrNull();

			string name = baseName.IsNullOrEmpty() ? cfg.currentControlSetName.Value : baseName;

			if(name == defaultStr) return;

			//Logger.LogDebug("remove Controls");

			foreach(CharacterMorpherIL2CPP_Controller ctrl1 in GetAllChaFuncCtrlOfType<CharacterMorpherIL2CPP_Controller>())
			{
				var obj = ctrl1.controls.all;
				if(obj.ContainsKey(name))
					obj[name].Clear();
				obj.Remove(name);

				var data = ctrl1.ctrls1?.all;
				if(data.ContainsKey(name))
					data[name].Clear();
				data.Remove(name);

				//(!MakerAPI.InsideMaker||!cfg.useCardMorphDataMaker.Value ? ctrl1.ctrls1 : ctrl1.ctrls2 ?? ctrl1.ctrls1)?.all?.Remove(name);
				//	Logger.LogDebug($"Controls List: [{string.Join(", ", obj.Keys.ToArray())}]");
			}



			Logger.LogMessage($"removed [{name}]");
			//	Logger.LogDebug($"Current List: [{string.Join(", ", ControlsList)}]");


		}

		static Texture tmpTex = null;
		static string lastPath = null;
		internal static void MyImageButtonDrawer(ConfigEntryBase entry)
		{

			// Make sure to use GUILayout.ExpandWidth(true) to use all available space
			GUILayout.BeginVertical();

			string path = Path.Combine(cfg.charDir.Value.MakeDirPath(), cfg.imageName.Value.MakeDirPath());
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

			if(GUILayout.Button(new GUIContent(entry.Definition.Key, null, entry.Description.Description), GUILayout.ExpandWidth(true)))
				CharacterMorpherIL2CPP_GUI.GetNewImageTarget();

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();


			GUILayout.EndVertical();
		}

		private static string[] LastControlsList = null;
		public static string[] ControlsList
		{
			get
			{
				var val =
				   Instance?.controlCategories?.Keys.ToList()
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
					//Logger.LogDebug("Trying to add a comp from drawer");
					AddNewSetting();
					//UpdateDropdown(ControlsList);
					//	Logger.LogDebug("execution got to this point");
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
				Logger.LogError(e);
			}

		}

		#endregion

	}


	#region User Classes


	[Serializable]
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
		object _lock = new object();

		public static void CreateTypeConverter()
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
									calcType = int.TryParse(vals[1], out var result2) ? (MorphCalcType)result2 : MorphCalcType.LINEAR,
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

		public void Copy(MorphSliderData src, bool copyAbmxState = true)
		{
			var tmp = src.Clone();

			//lock(_lock)
			{
				dataName = tmp.dataName;
				data = tmp.data;
				calcType = tmp.calcType;
				if(copyAbmxState)
					isABMX = tmp.isABMX;
			}
		}

		public override string ToString() =>
			$"name:{dataName}\n" +
			$"data:{data}\n" +
			$"is Abmx:{isABMX}\n" +
			$"calc. Type:{calcType}";

	}

	public class OnValueChange<T> : ProloEvent<T> { }
	public class OnControlSetValueChange : ProloEvent<string[]> { }
	public class OnNewImage : ProloEvent<string, byte[]> { }

	public class ProloEvent
	{
		private event Action _event;
		public void AddListener(Action action) => _event += action;
		public void RemoveListener(Action action) => _event -= action;
		public void Invoke() => _event?.Invoke();
	}
	public class ProloEvent<T0>
	{
		private event Action<T0> _event;
		public void AddListener(Action<T0> action) => _event += action;
		public void RemoveListener(Action<T0> action) => _event -= action;
		public void Invoke(T0 t0) => _event?.Invoke(t0);
	}
	public class ProloEvent<T0, T1>
	{
		private event Action<T0, T1> _event;
		public void AddListener(Action<T0, T1> action) => _event += action;
		public void RemoveListener(Action<T0, T1> action) => _event -= action;
		public void Invoke(T0 t0, T1 t1) => _event?.Invoke(t0, t1);
	}


	/// <summary>
	///   Class that specifies how a setting should be displayed inside the ConfigurationManager
	///   settings window. Usage: You can use this copy of the class instead of including
	///   it in your own plugin. Make a new instance, assign any fields that you want to
	///   override, and pass it as a tag for your setting. If a field is null (default),
	///   it will be ignored and won't change how the setting is displayed. If a field
	///   is non-null (you assigned a value to it), it will override default behavior.
	/// </summary>
	/// <remarks>
	///  You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
	///  You can optionally remove fields that you won't use from this class, it's the
	///  same as leaving them null.
	/// </remarks>
	public sealed class ConfigurationManagerAttributes
	{
		//
		// Summary:
		//     Custom setting draw action that allows polling keyboard input with the Input
		//     class. Note: Make sure to focus on your UI control when you are accepting input
		//     so user doesn't type in the search box or in another setting (best to do this
		//     on every frame). If you don't draw any selectable UI controls You can use `GUIUtility.keyboardControl
		//     = -1;` on every frame to make sure that nothing is selected.
		//
		// Parameters:
		//   setting:
		//     Setting currently being set (if available).
		//
		//   isCurrentlyAcceptingInput:
		//     Set this ref parameter to true when you want the current setting drawer to receive
		//     Input events. The value will persist after being set, use it to see if the current
		//     instance is being edited. Remember to set it to false after you are done!
		public delegate void CustomHotkeyDrawerFunc(ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);

		//
		// Summary:
		//     Should the setting be shown as a percentage (only use with value range settings).
		public bool? ShowRangeAsPercent;

		//
		// Summary:
		//     Custom setting editor (OnGUI code that replaces the default editor provided by
		//     ConfigurationManager). See below for a deeper explanation. Using a custom drawer
		//     will cause many of the other fields to do nothing.
		public Action<ConfigEntryBase> CustomDrawer;

		//
		// Summary:
		//     Custom setting editor that allows polling keyboard input with the Input (or UnityInput)
		//     class. Use either CustomDrawer or CustomHotkeyDrawer, using both at the same
		//     time leads to undefined behaviour.
		public CustomHotkeyDrawerFunc CustomHotkeyDrawer;

		//
		// Summary:
		//     Show this setting in the settings screen at all? If false, don't show.
		public bool? Browsable;

		//
		// Summary:
		//     Category the setting is under. Null to be directly under the plugin.
		public string Category;

		//
		// Summary:
		//     If set, a "Default" button will be shown next to the setting to allow resetting
		//     to default.
		public object DefaultValue;

		//
		// Summary:
		//     Force the "Reset" button to not be displayed, even if a valid DefaultValue is
		//     available.
		public bool? HideDefaultButton;

		//
		// Summary:
		//     Force the setting name to not be displayed. Should only be used with a KKAPI.Utilities.ConfigurationManagerAttributes.CustomDrawer
		//     to get more space. Can be used together with KKAPI.Utilities.ConfigurationManagerAttributes.HideDefaultButton
		//     to gain even more space.
		public bool? HideSettingName;

		//
		// Summary:
		//     Optional description shown when hovering over the setting. Not recommended, provide
		//     the description when creating the setting instead.
		public string Description;

		//
		// Summary:
		//     Name of the setting.
		public string DispName;

		//
		// Summary:
		//     Order of the setting on the settings list relative to other settings in a category.
		//     0 by default, higher number is higher on the list.
		public int? Order;

		//
		// Summary:
		//     Only show the value, don't allow editing it.
		public bool? ReadOnly;

		//
		// Summary:
		//     If true, don't show the setting by default. User has to turn on showing advanced
		//     settings or search for it.
		public bool? IsAdvanced;

		//
		// Summary:
		//     Custom converter from setting type to string for the built-in editor textboxes.
		public Func<object, string> ObjToStr;

		//
		// Summary:
		//     Custom converter from string to setting type for the built-in editor textboxes.
		public Func<string, object> StrToObj;
	}
	#endregion

}
