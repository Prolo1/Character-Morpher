
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KKAPI;
using KKAPI.Chara;
using KKABMX.Core;
using KKAPI.Maker;
using ExtensibleSaveFormat;
using UnityEngine.UI;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

#if HS2 || AI
using CharaCustom;
using AIChara;
#else
using ChaCustom;
#endif

using UnityEngine;



namespace Character_Morpher
{
	public class CharaMorpherController : CharaCustomFunctionController
	{
		internal class MorphData
		{
			internal class AMBXSections
			{
				public List<BoneModifier> body = new List<BoneModifier>();
				public List<BoneModifier> face = new List<BoneModifier>();
				public List<BoneModifier> other = new List<BoneModifier>();

				public void Populate(CharaCustomFunctionController charaControl)
				{
					var boneCtrl = charaControl.GetComponent<BoneController>();
					var charaCtrl = charaControl.ChaControl;

					//Store Bonemod Extended Data
					{//helps get rid of data sooner
						var data = boneCtrl?.GetExtendedData();
						var newModifiers = ReadBoneModifiers(data);
						body = new List<BoneModifier>(newModifiers);
						face = new List<BoneModifier>(newModifiers);
					}

					//split up body & head bones
					{
						var headRoot = charaCtrl.objHeadBone.transform.parent.parent;

						var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name));
						headBones.Add(headRoot.name);
						body.RemoveAll(x => headBones.Contains(x.BoneName));


						var bodyBones = new HashSet<string>(charaCtrl.objBodyBone.transform.parent.parent.
							GetComponentsInChildren<Transform>().Select(x => x.name).Except(headBones));
						face.RemoveAll(x => bodyBones.Contains(x.BoneName));

						//CharaMorpher.Logger.LogDebug($"Head root: {headRoot.name}");
						//    foreach(var part in face)
						//        CharaMorpher.Logger.LogDebug($"Face: {part.BoneName}");

						//    CharaMorpher.Logger.LogDebug($"body root: {charaCtrl.objBodyBone.transform.parent.parent.name}");
						//foreach(var part in body)
						//    CharaMorpher.Logger.LogDebug($"Body: {part.BoneName}");

					}
				}

				public void Clear()
				{
					body?.Clear();
					face?.Clear();
					other?.Clear();
				}
				public AMBXSections Copy()
				{
					return new AMBXSections()
					{
						body = new List<BoneModifier>(body ?? new List<BoneModifier>()),
						face = new List<BoneModifier>(face ?? new List<BoneModifier>()),
						other = new List<BoneModifier>(other ?? new List<BoneModifier>())
					};
				}
			}

			public ChaFile main = new ChaFile();
			public AMBXSections abmx = new AMBXSections();
#if KK
			public int id = -1;
#else
			public string id = null;
#endif
			public void Clear()
			{
#if KK
				id = -1;
#else
				id = null;
#endif
				main = new ChaFile();
				abmx.Clear();
			}

			public MorphData Copy()
			{
				var tmp = new ChaFile();
				tmp.CopyAll(main);
#if HS2 || AI
				//CopyAll will not copy this data in hs2
				tmp.dataID = main.dataID;
#endif
				return new MorphData() { id = id, main = tmp, abmx = abmx.Copy() };
			}

			public void Copy(MorphData data)
			{
				if(data == null) return;
				main.CopyAll(data.main);
				abmx = data.abmx.Copy();
				id = data.id;
			}

			public void Copy(CharaMorpherController data)
			{
#if HS2 || AI
				//CopyAll will not copy this data in hs2
				main.dataID = data.ChaControl.chaFile.dataID;
#endif
				main.CopyAll(data.ChaFileControl);
				abmx.Populate(data);
#if HS2 || AI
				string cardID = data.ChaControl.chaFile.dataID;
#elif KKS
				string cardID = data.ChaControl.chaFile.about.dataID;
#elif KK //not sure if this will work
				int cardID = data.ChaControl.chaFile.loadProductNo;
#endif
				id = cardID;
			}
		}

		internal struct MorphControls
		{
			//Main
			public float body;
			public float head;
			public float boobs;
			public float torso;
			public float arms;
			public float butt;
			public float legs;

			public float face;
			public float eyes;
			public float mouth;
			public float ears;

			//ABMX
			public float abmxBody;
			public float abmxTorso;
			public float abmxBoobs;
			public float abmxButt;
			public float abmxArms;
			public float abmxHands;
			public float abmxGenitals;
			public float abmxLegs;
			public float abmxFeet;

			public float abmxHead;
			public float abmxEyes;
			public float abmxMouth;
			public float abmxEars;
			public float abmxHair;
		}
		internal static MorphControls controls = new MorphControls()
		{

			//Main
			body = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			head = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			boobs = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			butt = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			torso = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			arms = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			legs = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,

			face = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			ears = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			eyes = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			mouth = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,

			//ABMX
			abmxBody = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxBoobs = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxButt = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxTorso = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxArms = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxHands = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxLegs = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxFeet = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxGenitals = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,

			abmxHead = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxEars = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxEyes = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxMouth = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
			abmxHair = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
		};

		private static MorphData charData = null;
		private static string lastCharDir = "";
		private static DateTime lastDT = new DateTime();

		private readonly MorphData m_data1 = new MorphData(), m_data2 = new MorphData();
		private static int morphindex = 0;//get defaults from config

		/// <summary>
		/// Called after the model has been updated for the first time
		/// </summary>
		public bool initLoadFinished { get; private set; } = false;

		/// <summary>
		/// In the process of reloading. set to false after complete
		/// </summary>
		public bool reloading { get; private set; } = false;

		//this is a tuple list btw (of bones found in abmx and online... somewhere)
#if KK || KKS
		public static readonly List<KeyValuePair<string, string>> bonecatagories = new List<KeyValuePair<string, string>>()
#else
		public static readonly List<(string, string)> bonecatagories = new List<(string, string)>()
#endif

#if KK || KKS
		#region KKBones
		{
             //ABMX
        
                 //other head
    new KeyValuePair<string, string>("cf_J_NoseBase_rx"  , "nose"    ),
	new KeyValuePair<string, string>("cf_J_Nose_tip"     , "nose"    ),
	new KeyValuePair<string, string>("cf_J_NoseBridge_rx", "nose"    ),
	new KeyValuePair<string, string>("cf_J_NoseBridge_ty", "nose"    ),
	new KeyValuePair<string, string>("cf_J_megane_rx_ear", ""    ),

   //Head;
    new KeyValuePair<string, string>("cf_J_FaceBase"     , ""     ),
	new KeyValuePair<string, string>("cf_s_head"         , ""     ),
	new KeyValuePair<string, string>("cf_J_FaceUp_ty"    , ""     ),
	new KeyValuePair<string, string>("cf_J_FaceUp_tz"    , ""     ),
	new KeyValuePair<string, string>("cf_J_FaceLow_sx"   , ""     ),
	new KeyValuePair<string, string>("cf_J_FaceLow_tz"   , ""     ),
	new KeyValuePair<string, string>("cf_hit_head"       , ""     ),
	new KeyValuePair<string, string>("cf_J_Chin_Base"    , ""     ),
	new KeyValuePair<string, string>("cf_J_ChinLow"      , ""     ),
	new KeyValuePair<string, string>("cf_J_ChinTip_Base" , ""     ),
	new KeyValuePair<string, string>("cf_J_CheekUpBase"  , ""     ),
	new KeyValuePair<string, string>("cf_J_CheekUp2_L"   , ""     ),
	new KeyValuePair<string, string>("cf_J_CheekUp_s_L"  , ""     ),
	new KeyValuePair<string, string>("cf_J_CheekLow_s_L" , ""     ),
   
    //Mouth;
    new KeyValuePair<string, string>("cf_J_MouthBase_rx" ,"mouth" ),
	new KeyValuePair<string, string>("cf_J_MouthBase_ty" ,"mouth" ),
	new KeyValuePair<string, string>("cf_J_Mouth_L"      ,"mouth" ),
	new KeyValuePair<string, string>("cf_J_Mouthup"      ,"mouth" ),
	new KeyValuePair<string, string>("cf_J_MouthLow"     ,"mouth" ),
	new KeyValuePair<string, string>("cf_J_MouthCavity"  ,"mouth" ),


   //Ears;

	new KeyValuePair<string, string>("cf_J_EarBase_ry_L" , "ears" ),
	new KeyValuePair<string, string>("cf_J_EarUp_L"      , "ears" ),
	new KeyValuePair<string, string>("cf_J_EarLow_L"     , "ears" ),
   
    //Eyes;
    new KeyValuePair<string, string>("cf_J_Mayu_L"       , "eyes"),
	new KeyValuePair<string, string>("cf_J_MayuMid_s_L"  , "eyes"),
	new KeyValuePair<string, string>("cf_J_MayuTip_s_L"  , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye_tz"       , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye_rz_L"     , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye_tx_L"     , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye01_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye02_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye03_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye04_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye05_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye06_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye07_s_L"    , "eyes"),
	new KeyValuePair<string, string>("cf_J_Eye08_s_L"    , "eyes"),
  
    //Body;
    new KeyValuePair<string, string>("cf_n_height"       , "Body"                   ),
	new KeyValuePair<string, string>("cf_s_neck"         , "Neck"                   ),
	new KeyValuePair<string, string>("cf_d_sk_top"       , "Whole Skirt"            ),
	new KeyValuePair<string, string>("cf_d_sk_00_00"     , "Skirt Front"            ),
	new KeyValuePair<string, string>("cf_d_sk_07_00"     , "Skirt Front Sides"      ),
	new KeyValuePair<string, string>("cf_d_sk_06_00"     , "Skirt Sides"            ),
	new KeyValuePair<string, string>("cf_d_sk_05_00"     , "Skirt Back Sides"       ),
	new KeyValuePair<string, string>("cf_d_sk_04_00"     , "Skirt Back"             ),
   
    //Boobs;
    new KeyValuePair<string, string>("cf_d_bust01_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_d_bust02_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_d_bust03_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bust00_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bust01_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bust02_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bust03_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_hit_bust02_L"   , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bnip01_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bnip025_L"    , "boobs"   ),
	new KeyValuePair<string, string>("cf_d_bnip01_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bnip02_L"     , "boobs"   ),
	new KeyValuePair<string, string>("cf_s_bnipacc_L"    , "boobs"   ),
 
    //Torso;
    new KeyValuePair<string, string>("cf_s_spine03"      , "torso"  ),
	new KeyValuePair<string, string>("cf_s_spine02"      , "torso"  ),
	new KeyValuePair<string, string>("cf_s_spine01"      , "torso"  ),
	new KeyValuePair<string, string>("cf_hit_spine01"    , "torso"  ),
	new KeyValuePair<string, string>("cf_hit_spine02_L"  , "torso"  ),
	new KeyValuePair<string, string>("cf_hit_berry"      , "torso"  ),
	new KeyValuePair<string, string>("cf_hit_waist_L"    , "torso"  ),
	new KeyValuePair<string, string>("cf_j_spine01"      , "torso"  ),
	new KeyValuePair<string, string>("cf_j_spine02"      , "torso"  ),
	new KeyValuePair<string, string>("cf_j_spine03"      , "torso"  ),
	new KeyValuePair<string, string>("cf_s_waist01"      , "torso"  ),
	new KeyValuePair<string, string>("cf_s_waist02"      , "torso"  ),
   
    //Butt;
    new KeyValuePair<string, string>("cf_s_siri_L"       , "butt"          ),
	new KeyValuePair<string, string>("cf_hit_siri_L"     , "butt"          ),
 
    //Arms;
    new KeyValuePair<string, string>("cf_s_shoulder02_L" , "arms"  ),
	new KeyValuePair<string, string>("cf_hit_shoulder_L" , "arms"  ),
	new KeyValuePair<string, string>("cf_j_shoulder_L"   , "arms"  ),
	new KeyValuePair<string, string>("cf_j_arm00_L"      , "arms"  ),
	new KeyValuePair<string, string>("cf_s_arm01_L"      , "arms"  ),
	new KeyValuePair<string, string>("cf_s_arm02_L"      , "arms"  ),
	new KeyValuePair<string, string>("cf_s_arm03_L"      , "arms"  ),
	new KeyValuePair<string, string>("cf_j_forearm01_L"  , "arms"  ),
	new KeyValuePair<string, string>("cf_s_forearm01_L"  , "arms"  ),
	new KeyValuePair<string, string>("cf_s_forearm02_L"  , "arms"  ),
	new KeyValuePair<string, string>("cf_s_wrist_L"      , "arms"  ),
    
    //Hands;
    new KeyValuePair<string, string>("cf_j_hand_L"       , "hands"        ),
	new KeyValuePair<string, string>("cf_s_hand_L"       , "hands"        ),
	new KeyValuePair<string, string>("cf_hit_arm_L"      , "hands"        ),
  
    //Legs;
    new KeyValuePair<string, string>("cf_j_waist01"      , "legs"  ),
	new KeyValuePair<string, string>("cf_j_waist02"      , "legs"  ),
	new KeyValuePair<string, string>("cf_j_thigh00_L"    , "legs"  ),
	new KeyValuePair<string, string>("cf_s_thigh01_L"    , "legs"  ),
	new KeyValuePair<string, string>("cf_s_thigh02_L"    , "legs"  ),
	new KeyValuePair<string, string>("cf_s_thigh03_L"    , "legs"  ),
	new KeyValuePair<string, string>("cf_hit_thigh01_L"  , "legs"  ),
	new KeyValuePair<string, string>("cf_hit_thigh02_L"  , "legs"  ),
	new KeyValuePair<string, string>("cf_j_leg01_L"      , "legs"  ),
	new KeyValuePair<string, string>("cf_s_leg01_L"      , "legs"  ),
	new KeyValuePair<string, string>("cf_s_leg02_L"      , "legs"  ),
	new KeyValuePair<string, string>("cf_s_leg03_L"      , "legs"  ),
   //Feet;
    new KeyValuePair<string, string>("cf_j_foot_L"       , "feet"          ),
	new KeyValuePair<string, string>("cf_j_leg03_L"      , "feet"          ),
	new KeyValuePair<string, string>("cf_j_toes_L"       , "feet"          ),

  //Genitals;
    new KeyValuePair<string, string>("cf_d_kokan"        , "genitals"           ),
	new KeyValuePair<string, string>("cf_j_kokan"        , "genitals"           ),
	new KeyValuePair<string, string>("cm_J_dan100_00"    , "genitals"           ),
	new KeyValuePair<string, string>("cm_J_dan109_00"    , "genitals"           ),
	new KeyValuePair<string, string>("cm_J_dan_f_L"      , "genitals"           ),
	new KeyValuePair<string, string>("cf_j_ana"          , "genitals"           ),
			 }
		#endregion
#elif AI || HS2
		#region AIBones
            {           
                //Torso
                ("cf_J_Spine00,False,1,1,1,1                                                 ", "torso"),
				("cf_J_Spine00_s,False,1,1,1,1                                               ", "torso"),
				("cf_J_Spine01,False,1,1,1,1 Waist & Above                                   ", "torso"),
				("cf_J_Spine01_s,False,1,1,1,1 Waist                                         ", "torso"),
				("cf_J_Spine01s,False,1,1,1,1                                                ", "torso"),
				("cf_J_Spine02,False,1,1,1,1 Ribcage & Above                                 ", "torso"),
				("cf_J_Spine02_s,False,1,1,1,1                                               ", "torso"),
				("cf_J_Spine03,False,1,1,1,1 Neck Delta & Above                              ", "torso"),
				("cf_J_Spine03_s,False,1,1,1,1                                               ", "torso"),
				("cf_J_SpineSk00,False,1,1,1,1                                               ", "torso"),
				("cf_J_SpineSk00_dam,False,1,1,1,1                                           ", "torso"),
				("cf_J_SpineSk01,False,1,1,1,1                                               ", "torso"),
				("cf_J_SpineSk05,False,1,1,1,1                                               ", "torso"),
				("cf_J_SpineSk06,False,1,1,1,1                                               ", "torso"),


                //Boobs                
                ("cf_hit_Mune02_s_L,False,1,1,1,1 Hitbox - Breast (Left)                     ",    "boobs"),
				("cf_hit_Mune02_s_R,False,1,1,1,1 Hitbox - Breast (Right)                    ",    "boobs"),
				("cf_J_Mune_Nip_R,False,1,1,1,1                                              ",    "boobs"),
				("cf_J_Mune_Nip_s_R,False,1,1,1,1                                            ",    "boobs"),
				("cf_J_Mune_Nip01_s_L,False,1,1,1,1 Nipple (Left)                            ",    "boobs"),
				("cf_J_Mune_Nip01_s_R,False,1,1,1,1 Nipple (Right)                           ",    "boobs"),
				("cf_J_Mune_Nip02_s_L,False,1,1,1,1 Nipple Tip (Left)                        ",    "boobs"),
				("cf_J_Mune_Nip02_s_R,False,1,1,1,1 Nipple Tip (Right)                       ",    "boobs"),
				("cf_J_Mune_Nip03_s_L,False,1,1,1,1                                          ",    "boobs"),
				("cf_J_Mune_Nip03_s_R,False,1,1,1,1                                          ",    "boobs"),
				("cf_J_Mune_Nipacs01_L,False,1,1,1,1                                         ",    "boobs"),
				("cf_J_Mune_Nipacs01_R,False,1,1,1,1                                         ",    "boobs"),
				("cf_J_Mune00_d_L,False,1,1,1,1 Middle part of Breast (Left)                 ",    "boobs"),
				("cf_J_Mune00_d_R,False,1,1,1,1 Middle part of Breast (Right)                ",    "boobs"),
				("cf_J_Mune00_s_L,False,1,1,1,1 Breast Closest to Chest (Left)               ",    "boobs"),
				("cf_J_Mune00_s_R,False,1,1,1,1 Breast Closest to Chest (Right)              ",    "boobs"),
				("cf_J_Mune00_t_L,False,1,1,1,1 Outer part of Breast (Left)                  ",    "boobs"),
				("cf_J_Mune00_t_R,False,1,1,1,1 Outer part of Breast (Right)                 ",    "boobs"),
				("cf_J_Mune01_d_L,False,1,1,1,1                                              ",    "boobs"),
				("cf_J_Mune01_d_R,False,1,1,1,1                                              ",    "boobs"),
				("cf_J_Mune01_s_L,False,1,1,1,1 Middle of Breast (Left)                      ",    "boobs"),
				("cf_J_Mune01_s_R,False,1,1,1,1 Middle of Breast (Right)                     ",    "boobs"),
				("cf_J_Mune01_t_L,False,1,1,1,1 Outer part of Breast (Left)                  ",    "boobs"),
				("cf_J_Mune01_t_R,False,1,1,1,1 Outer part of Breast (Right)                 ",    "boobs"),
				("cf_J_Mune02_s_L,False,1,1,1,1 Outer part of Breast (Left)                  ",    "boobs"),
				("cf_J_Mune02_s_R,False,1,1,1,1 Outer part of Breast (Right)                 ",    "boobs"),
				("cf_J_Mune02_t_L,False,1,1,1,1 Tip of Breast (Left)                         ",    "boobs"),
				("cf_J_Mune02_t_R,False,1,1,1,1 Tip of Breast (Right)                        ",    "boobs"),
				("cf_J_Mune03_s_L,False,1,1,1,1 Tip of Breast (Left)                         ",    "boobs"),
				("cf_J_Mune03_s_R,False,1,1,1,1 Tip of Breast (Right)                        ",    "boobs"),
				("cf_J_Mune04_s_L,False,1,1,1,1 Areola (Left)                                ",    "boobs"),
				("cf_J_Mune04_s_R,False,1,1,1,1 Areola (Right)                               ",    "boobs"),



                //Butt
                ("cf_hit_Kosi02_s,False,1,1,1,1 Hitbox - Hips                                ",   "butt"),
				("cf_hit_Siri_s_L,False,1,1,1,1 Hitbox - Butt (Left)                         ",   "butt"),
				("cf_hit_Siri_s_R,False,1,1,1,1 Hitbox - Butt (Right)                        ",   "butt"),
				("cf_J_Hips,False,1,1,1,1 Scale                                              ",   "butt"),
				("cf_J_SiriDam_L,False,1,1,1,1 Butt (Left)                                   ",   "butt"),
				("cf_J_SiriDam_R,False,1,1,1,1 Butt (Right)                                  ",   "butt"),
				("cf_J_SiriDam00_L,False,1,1,1,1                                             ",   "butt"),
				("cf_J_SiriDam01_L,False,1,1,1,1                                             ",   "butt"),
				("cf_J_SiriDam01_R,False,1,1,1,1                                             ",   "butt"),
				("cf_J_SiriDam02_L,False,1,1,1,1                                             ",   "butt"),
				("cf_J_Sirilow_L,False,1,1,1,1                                               ",   "butt"),
				("cf_J_Sirilow_s_L,False,1,1,1,1                                             ",   "butt"),
				("cf_J_Siriopen_s_L,False,1,1,1,1 Butt Apart (Left)                          ",   "butt"),
				("cf_J_Siriopen_s_R,False,1,1,1,1 Butt Apart (Right)                         ",   "butt"),
				("cf_J_SiriTop_L,False,1,1,1,1                                               ",   "butt"),
				("cf_J_Siriulow_L,False,1,1,1,1                                              ",   "butt"),
				("cf_J_Siriulow_s_L,False,1,1,1,1                                            ",   "butt"),
				("cf_J_Siriup_L,False,1,1,1,1                                                ",   "butt"),
				("cf_J_SiriUp_L,False,1,1,1,1                                                ",   "butt"),
				("cf_J_SiriUP_L,False,1,1,1,1                                                ",   "butt"),
				("cf_J_SiriUP_R,False,1,1,1,1                                                ",   "butt"),
				("cf_J_Siriup00_L,False,1,1,1,1                                              ",   "butt"),
				("cf_J_SiriUp00_L,False,1,1,1,1                                              ",   "butt"),
				("cf_J_SiriUp00_R,False,1,1,1,1                                              ",   "butt"),
				("cf_J_SiriUp01_L,False,1,1,1,1                                              ",   "butt"),
				("cf_J_SiriUP01_L,False,1,1,1,1                                              ",   "butt"),
				("cf_J_SiriUP01_R,False,1,1,1,1                                              ",   "butt"),
				("cf_J_SiriUp01_s_L,False,1,1,1,1                                            ",   "butt"),
				("cf_J_Siriups_L,False,1,1,1,1                                               ",   "butt"),
				("cf_J_Siri_L,False,1,1,1,1 Overall Butt (Left)                              ",   "butt"),
				("cf_J_Siri_R,False,1,1,1,1 Overall Butt (Right)                             ",   "butt"),
				("cf_J_Siri_s_L,False,1,1,1,1 Overall Butt/Butt Tone (Left) [Ignores Skirt]  ",   "butt"),
				("cf_J_Siri_s_R,False,1,1,1,1 Overall Butt/Butt Tone (Right) [Ignores Skirt] ",   "butt"),

            


                //Arms                                
                ("cf_J_ArmElbo_low_s_L,False,1,1,1,1 Elbow (Left)                             ","arms"),
				("cf_J_ArmElbo_low_s_R,False,1,1,1,1 Elbow (Right)                            ","arms"),
				("cf_J_ArmLow01_L,False,1,1,1,1 Forearm (Left)                               ", "arms"),
				("cf_J_ArmLow01_R,False,1,1,1,1 Forearm (Right)                              ", "arms"),
				("cf_J_ArmLow01_s_L,False,1,1,1,1 Upper Forearm Tone (Left)                   ","arms"),
				("cf_J_ArmLow01_s_R,False,1,1,1,1 Upper Forearm Tone (Right)                  ","arms"),
				("cf_J_ArmLow02_s_L,False,1,1,1,1 Lower Forearm Tone (Left)                   ","arms"),
				("cf_J_ArmLow02_s_R,False,1,1,1,1 Lower Forearm Tone (Left)                   ","arms"),
				("cf_J_ArmUp00_L,False,1,1,1,1 Overall Arm (Left)                            ", "arms"),
				("cf_J_ArmUp00_R,False,1,1,1,1 Overall Arm (Right)                           ", "arms"),
				("cf_J_ArmUp01_s_L,False,1,1,1,1 Upper Humerus (Left)                         ","arms"),
				("cf_J_ArmUp01_s_R,False,1,1,1,1 Lower Humerus (Right)                        ","arms"),
				("cf_J_ArmUp02_s_L,False,1,1,1,1 Upper Humerus (Left)                         ","arms"),
				("cf_J_ArmUp02_s_R,False,1,1,1,1 Lower Humerus (Right)                        ","arms"),
				("cf_J_ArmUp03_s_L,False,1,1,1,1 Upper Humerus (Left)                        ", "arms"),
				("cf_J_ArmUp03_s_R,False,1,1,1,1 Lower Humerus (Right)                       ", "arms"),
				("cf_J_Shoulder_L,False,1,1,1,1 Shoulder & Arm Scale (Left)                  ", "arms"),
				("cf_J_Shoulder_R,False,1,1,1,1 Shoulder & Arm Scale (Right)                 ", "arms"),
				("cf_J_Shoulder02_s_L,False,1,1,1,1 Shoulder Tone (Left)                     ", "arms"),
				("cf_J_Shoulder02_s_R,False,1,1,1,1 Shoulder Tone (Right)                    ", "arms"),
				("cf_J_ShoulderIK_L,False,1,1,1,1 Arm Length & Shoulder Elevation (Left)     ", "arms"),
				("cf_J_ShoulderIK_R,False,1,1,1,1 Arm Length & Shoulder Elevation (Right)    ", "arms"),


                //Hands             
                ("cf_J_Hand_L,False,1,1,1,1 Hand & Wirst (Left)                              ","hands"),
				("cf_J_Hand_R,False,1,1,1,1 Hand & Wirst (Right)                             ","hands"),
				("cf_J_Hand_s_L,False,1,1,1,1 Hand (Left)                                    ","hands"),
				("cf_J_Hand_s_R,False,1,1,1,1 Hand (Right)                                   ","hands"),
				("cf_J_Hand_Wrist_s_L,False,1,1,1,1 Wirst (Left)                             ","hands"),
				("cf_J_Hand_Wrist_s_R,False,1,1,1,1 Wirst (Right)                            ","hands"),


                //genitals
                ("cf_J_Kokan,False,1,1,1,1 Pussy                                             ","genitals"),
				("cf_J_Ana,False,1,1,1,1 Anus                                                ","genitals"),
				("cm_J_dan_s,False,1,1,1,1 (Penis & Balls)                                   ","genitals"),
				("cm_J_dan100_00,False,1,1,1,1 (Penis)                                       ","genitals"),
				("cm_J_dan_f_top,False,1,1,1,1 (Balls)                                       ","genitals"),
				("cm_J_dan_f_L,False,1,1,1,1 (Left Nut)                                      ","genitals"),
				("cm_J_dan_f_R,False,1,1,1,1 (Right Nut)                                     ","genitals"),

             
                //legs
                ("cf_hit_LegUp01_s_L,False,1,1,1,1 Hitbox - Thigh (Left)                     ", "legs"),
				("cf_hit_LegUp01_s_R,False,1,1,1,1 Hitbox - Thigh (Right)                    ", "legs"),
				("cf_J_Kosi01,False,1,1,1,1 Waist & Below                                    ", "legs"),
				("cf_J_Kosi01_s,False,1,1,1,1 Pelvis [Ignores Skirt]                         ", "legs"),
				("cf_J_Kosi02,False,1,1,1,1 Hips & Below                                     ", "legs"),
				("cf_J_Kosi02_s,False,1,1,1,1 Hips [Ignores Skirt]                           ", "legs"),
				("cf_J_Kosi03,False,1,1,1,1                                                  ", "legs"),
				("cf_J_Kosi03_s,False,1,1,1,1                                                ", "legs"),
				("cf_J_Kosi04_s,False,1,1,1,1                                                ", "legs"),
				("cf_J_LegDam_L,False,1,1,1,1                                                ", "legs"),
				( "cf_J_LegKnee_back_s_L,False,1,1,1,1 Back of Knee (Left)                    ","legs"),
				("cf_J_LegKnee_back_s_R,False,1,1,1,1 Back of Knee (Right)                   ", "legs"),
				("cf_J_LegKnee_dam_L,False,1,1,1,1 Front of Knee (Left)                      ", "legs"),
				("cf_J_LegKnee_dam_R,False,1,1,1,1 Front of Knee (Right)                     ", "legs"),
				("cf_J_LegKnee_low_s_L,False,1,1,1,1 Knee Tone (Left)                        ", "legs"),
				("cf_J_LegKnee_low_s_R,False,1,1,1,1 Knee Tone (Right)                       ", "legs"),
				("cf_J_LegLow01_L,False,1,1,1,1 Knees & Below                                ", "legs"),
				("cf_J_LegLow01_R,False,1,1,1,1 Knees & Below                                ", "legs"),
				("cf_J_LegLow01_s_L,False,1,1,1,1 Calf (Left)                                ", "legs"),
				("cf_J_LegLow01_s_R,False,1,1,1,1 Calf (Right)                               ", "legs"),
				("cf_J_LegLow02_s_L,False,1,1,1,1 Lower Calf (Left)                          ", "legs"),
				("cf_J_LegLow02_s_R,False,1,1,1,1 Lower Calf (Right)                         ", "legs"),
				("cf_J_LegLow03_s_L,False,1,1,1,1 Ankle (Left)                               ", "legs"),
				("cf_J_LegLow03_s_R,False,1,1,1,1 Ankle (Right)                              ", "legs"),
				("cf_J_LegLowDam_L,False,1,1,1,1                                             ", "legs"),
				("cf_J_LegLowRoll_L,False,1,1,1,1 Lower Leg Length (Left)                    ", "legs"),
				("cf_J_LegLowRoll_R,False,1,1,1,1 Lower Leg Length (Right)                   ", "legs"),
				("cf_J_LegUp_L,False,1,1,1,1                                                 ", "legs"),
				("cf_J_LegUp00_L,False,1,1,1,1 Overall Leg (Left)                            ", "legs"),
				("cf_J_LegUp00_R,False,1,1,1,1 Overall Leg (Right)                           ", "legs"),
				("cf_J_LegUp01_s_L,False,1,1,1,1 Upper Thigh (Left)                          ", "legs"),
				("cf_J_LegUp01_s_R,False,1,1,1,1 Upper Thigh (Right)                         ", "legs"),
				("cf_J_LegUp02_s_L,False,1,1,1,1 Lower Thigh (Left)                          ", "legs"),
				("cf_J_LegUp02_s_R,False,1,1,1,1 Lower Thigh (Right)                         ", "legs"),
				("cf_J_LegUp03_L,False,1,1,1,1 Knee (Left)                                   ", "legs"),
				("cf_J_LegUp03_R,False,1,1,1,1 Knee (Right)                                  ", "legs"),
				("cf_J_LegUp03_s_L,False,1,1,1,1 Above the Knee (Left)                       ", "legs"),
				("cf_J_LegUp03_s_R,False,1,1,1,1 Above the Knee (Right)                      ", "legs"),
				("cf_J_LegUpDam_L,False,1,1,1,1 Upper Hip (Left)                             ", "legs"),
				("cf_J_LegUpDam_R,False,1,1,1,1 Upper Hip (Right)                            ", "legs"),
				("cf_J_LegUpDam_s_L,False,1,1,1,1 Upper Hip (Left) [Not as good]             ", "legs"),
				("cf_J_LegUpDam_s_R,False,1,1,1,1 Upper Hip (Right) [Not as good]            ", "legs"),
				("cf_J_LegUpLow_R,False,1,1,1,1                                              ", "legs"),
				("cf_J_LegUpLow_s_L,False,1,1,1,1                                            ", "legs"),
              

                //feet              
                ("cf_J_Foot01_L,False,1,1,1,1 Foot & Ankle (Left)                            ","feet"),
				("cf_J_Foot01_R,False,1,1,1,1 Foot & Ankle (Right)                           ","feet"),
				("cf_J_Foot02_L,False,1,1,1,1 Foot (Left)                                    ","feet"),
				("cf_J_Foot02_R,False,1,1,1,1 Foot (Right)                                   ","feet"),
				("cf_J_Toes01_L,False,1,1,1,1 Toes (Left)                                    ","feet"),
				("cf_J_Toes01_R,False,1,1,1,1 Toes (Right)                                   ","feet"),
                
            
                //eyes
                ("cf_J_Eye_r_L,False,1,1,1,1 Eye (Left)                                      ","eyes"),
				("cf_J_Eye_r_R,False,1,1,1,1 Eye (Right)                                     ","eyes"),
				("cf_J_eye_rs_L,False,1,1,1,1 Eyeball (Left)                                 ","eyes"),
				("cf_J_eye_rs_R,False,1,1,1,1 Eyeball (Right)                                ","eyes"),
				("cf_J_Eye_s_L,False,1,1,1,1 Overall Eye 1 (Left)                            ","eyes"),
				("cf_J_Eye_s_R,False,1,1,1,1 Overall Eye 1 (Right)                           ","eyes"),
				("cf_J_Eye_t_L,False,1,1,1,1 Overall Eye 2 (Left)                            ","eyes"),
				("cf_J_Eye_t_R,False,1,1,1,1 Overall Eye 2 (Right)                           ","eyes"),
				("cf_J_Eye01_L,False,1,1,1,1 Eyelid 1 (Left)                                 ","eyes"),
				("cf_J_Eye01_R,False,1,1,1,1 Eyelid 1 (Right)                                ","eyes"),
				("cf_J_Eye02_L,False,1,1,1,1 Upper Eyelid (Left)                             ","eyes"),
				("cf_J_Eye02_R,False,1,1,1,1 Upper Eyelid (Right)                            ","eyes"),
				("cf_J_Eye03_L,False,1,1,1,1 Eyelid Outer Corner (Left)                      ","eyes"),
				("cf_J_Eye03_R,False,1,1,1,1 Eyelid Outer Corner (Right)                     ","eyes"),
				("cf_J_Eye04_L,False,1,1,1,1 Lower Eyelid (Left)                             ","eyes"),
				("cf_J_Eye04_R,False,1,1,1,1 Lower Eyelid (Right)                            ","eyes"),
				("cf_J_EyePos_rz_L,False,1,1,1,1 Eyeball (Left)                              ","eyes"),
				("cf_J_EyePos_rz_R,False,1,1,1,1 Eyeball (Right)                             ","eyes"),
				("cf_J_Mayu_L,False,1,1,1,1 Eyebrow (Left)                                   ","eyes"),
				("cf_J_Mayu_R,False,1,1,1,1 Eyebrow (Right)                                  ","eyes"),
				("cf_J_MayuMid_s_L,False,1,1,1,1 Eyebrow Middle (Left)                       ","eyes"),
				("cf_J_MayuMid_s_R,False,1,1,1,1 Eyebrow Middle (Right)                      ","eyes"),
				("cf_J_MayuTip_s_L,False,1,1,1,1 Eyebrow End (Left)                          ","eyes"),
				("cf_J_MayuTip_s_R,False,1,1,1,1 Eyebrow End (Right)                         ","eyes"),
				("cf_J_pupil_s_L,False,1,1,1,1 Pupil (Left)                                  ","eyes"),
				("cf_J_pupil_s_R,False,1,1,1,1 Pupil (Right)                                 ","eyes"),
                


                //mouth
                ("cf_J_Mouth_L,False,1,1,1,1 Mouth (Left)                                    ","mouth"),
				("cf_J_Mouth_R,False,1,1,1,1 Mouth (Right)                                   ","mouth"),
				("cf_J_MouthBase_s,False,1,1,1,1  Lips                                       ","mouth"),
				("cf_J_MouthBase_tr,False,1,1,1,1 Mouth (with teeth)                         ","mouth"),
				("cf_J_MouthCavity,False,1,1,1,1 Teeth                                       ","mouth"),
				("cf_J_MouthLow,False,1,1,1,1 Lower Lip Tone                                 ","mouth"),
				("cf_J_Mouthup,False,1,1,1,1 Upper Lip Tone                                  ","mouth"),
                


                //ears
                ("cf_J_EarBase_s_L,False,1,1,1,1 Overall Ear (Left)                          ", "ears"),
				("cf_J_EarBase_s_R,False,1,1,1,1 Overall Ear (Right)                         ", "ears"),
				("cf_J_EarLow_L,False,1,1,1,1 Lower Ear (Left)                               ", "ears"),
				("cf_J_EarLow_R,False,1,1,1,1 Lower Ear (Right)                              ", "ears"),
				("cf_J_EarRing_L,False,1,1,1,1 Earring (Left)                                ", "ears"),
				("cf_J_EarRing_R,False,1,1,1,1 Earring (Right)                               ", "ears"),
				("cf_J_EarUp_L,False,1,1,1,1 Upper Ear (Left)                                ", "ears"),
				("cf_J_EarUp_R,False,1,1,1,1 Upper Ear (Right)                               ", "ears"),




                //hair
                ("cf_hairS,False,1,1,1,1 Side Hair                                           ", "hair"),
				("cf_j_hair_camp1_F_L_2,False,1,1,1,1                                        ", "hair"),
				("cf_j_hair_camp1_F_R_2,False,1,1,1,1                                        ", "hair"),
				("cf_J_hairB,False,1,1,1,1                                                   ", "hair"),
				("cf_J_hairB_top,False,1,1,1,1 Hair Top                                      ", "hair"),
				("cf_J_hairBC_s,False,1,1,1,1 Back Hair (Middle)                             ", "hair"),
				("cf_J_hairBL_00,False,1,1,1,1                                               ", "hair"),
				("cf_J_hairBL_s,False,1,1,1,1 Pigtails (Left)                                ", "hair"),
				("cf_J_hairBR_00,False,1,1,1,1                                               ", "hair"),
				("cf_J_hairBR_s,False,1,1,1,1 Pigtails (Right)                               ", "hair"),
				("cf_J_hairF_top,False,1,1,1,1 Bangs                                         ", "hair"),
				("cf_J_hairFC_s,False,1,1,1,1 Bangs (Middle)                                 ", "hair"),
				("cf_J_hairFL_s,False,1,1,1,1 Bangs (Left)                                   ", "hair"),
				("cf_J_hairFR_s,False,1,1,1,1 Bangs (Right)                                  ", "hair"),
				("cf_J_hairS,False,1,1,1,1 Side Hair                                         ", "hair"),
				("cf_J_hairS_top,False,1,1,1,1 Side Hair                                     ", "hair"),
                



                //other
                ("cf_J_CheekLow_L,False,1,1,1,1 Lower Cheek (Left)                            ",""),
				("cf_J_CheekLow_R,False,1,1,1,1 Lower Cheek (Right)                           ",""),
				("cf_J_CheekUp_L,False,1,1,1,1 Upper Cheek (Left)                             ",""),
				("cf_J_CheekUp_R,False,1,1,1,1 Upper Cheek (Right)                            ",""),
				("cf_J_Chin_rs,False,1,1,1,1 Jaw                                              ",""),
				("cf_J_ChinLow,False,1,1,1,1                                                  ",""),
				("cf_J_ChinTip_s,False,1,1,1,1 Chin                                           ",""),
				("cf_J_FaceBase,False,1,1,1,1 Overall Face                                   ", ""),
				("cf_J_FaceLow_s,False,1,1,1,1 Lower Face                                    ", ""),
				("cf_J_FaceLowBase,False,1,1,1,1 Lower Face Tone                             ", ""),
				("cf_J_FaceUp_ty,False,1,1,1,1 Upper Face                                    ", ""),
				("cf_J_FaceUp_tz,False,1,1,1,1 Upper Face Tone                               ", ""),
				("cf_J_Head,False,1,1,1,1 Head Scale                                         ", ""),
				("cf_J_Head_s,False,1,1,1,1 Overall Head                                     ", ""),
				("cf_J_megane,False,1,1,1,1 Glasses                                          ", ""),
				("cf_J_Neck,False,1,1,1,1 Head & Neck                                        ", ""),
				("cf_J_Neck_s,False,1,1,1,1 Neck [Don't Use]                                 ", ""),
				("cf_J_Nose_t,False,1,1,1,1 Upper Nose                                       ", ""),
				("cf_J_Nose_tip,False,1,1,1,1 Nose Tip                                       ", ""),
				("cf_J_NoseBase_s,False,1,1,1,1 Nose                                         ", ""),
				("cf_J_NoseBase_trs,False,1,1,1,1 Nose & Bridge                              ", ""),
				("cf_J_NoseBridge_s,False,1,1,1,1 Bridge                                     ", ""),
				("cf_J_NoseBridge_t,False,1,1,1,1 Bridge                                     ", ""),
				("cf_J_NoseWing_tx_L,False,1,1,1,1 Nostril (Left)                            ", ""),
				("cf_J_NoseWing_tx_R,False,1,1,1,1 Nostril (Right)                           ", ""),
				("cf_J_Root,False,1,1,1,1 Scale of Character                                 ", ""),
				("cf_J_sk_00_00_dam,False,1,1,1,1 Front of Skirt (Middle)                    ", ""),
				("cf_J_sk_01_00_dam,False,1,1,1,1 Front of Skirt (Right)                     ", ""),
				("cf_J_sk_02_00_dam,False,1,1,1,1 Side of Skirt (Right)                      ", ""),
				("cf_J_sk_03_00_dam,False,1,1,1,1 Back of Skirt (Right)                      ", ""),
				("cf_J_sk_04_00_dam,False,1,1,1,1 Back of Skirt (Middle)                     ", ""),
				("cf_J_sk_05_00_dam,False,1,1,1,1 Back of Skirt (Left)                       ", ""),
				("cf_J_sk_06_00_dam,False,1,1,1,1 Side of Skirt (Left)                       ", ""),
				("cf_J_sk_07_00_dam,False,1,1,1,1 Front of Skirt (Left)                      ", ""),
				("cf_J_sk_siri_dam,False,1,1,1,1 Back of Skirt                               ", ""),
				("cf_J_sk_top,False,1,1,1,1 Overall Skirt                                    ", ""),
				("cf_N_height,False,1,1,1,1 Height                                           ", ""),

			  }
		#endregion
#endif
;
		public void ForceCardReload()
		{

			ChaControl.chaFile.SetCustomBytes(m_data1.main.GetCustomBytes(), m_data1.main.loadVersion);
			MorphChangeUpdate();
			if(KoikatuAPI.GetCurrentGameMode() != GameMode.Maker) return;


#if AI || HS2
			////Head Direction
			//if(DefaultHeadDirection.Value != HeadDirection.Pose)
			//	CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/necklook/items/tgl02").GetComponent<Toggle>().isOn = true;
			//
			////Pose
			//if(DefaultPose.Value != 1)
			//{
			//	var pose = CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/pose/items/inpNo").GetComponent<InputField>();
			//	pose.text = DefaultPose.Value.ToString();
			//	pose.onEndEdit.Invoke(DefaultPose.Value.ToString());
			//}
#else
			ChaControl.neckLookCtrl.neckLookScript.lookType = NECK_LOOK_TYPE_VER2.TARGET;
			foreach(var target in ChaControl.eyeLookCtrl.eyeLookScript.eyeTypeStates)
				target.lookType = EYE_LOOK_TYPE.TARGET;


			////Head Direction
			//int tmp = CustomBase.Instance.customCtrl.cmpDrawCtrl.ddNeckLook.value;
			//CustomBase.Instance.customCtrl.cmpDrawCtrl.ddNeckLook.value = 0;
			//CustomBase.Instance.customCtrl.cmpDrawCtrl.ddNeckLook.value = tmp;
			//
			////Pose
			////if(DefaultPose.Value != 0)
			//{
			//	tmp = CustomBase.Instance.customCtrl.cmpDrawCtrl.ddPose.value;
			//	CustomBase.Instance.customCtrl.cmpDrawCtrl.ddPose.value = 0;
			//	CustomBase.Instance.customCtrl.cmpDrawCtrl.ddPose.value = tmp;
			//
			//}
#endif

		}


		public IEnumerator CoMorphReload(int delayFrames = 10, bool abmxOnly = false)
		{
			for(int a = 0; a < delayFrames; ++a)
				yield return null;
			reloading = true;//just in-case

			//CharaMorpher_Core.Logger.LogDebug("Reloading After character loaded");
			OnCharaReload(KoikatuAPI.GetCurrentGameMode(), abmxOnly);

			//CharaMorpher_Core.Logger.LogDebug("Morphing model...");

			StartCoroutine(CoMorphUpdate((int)Mathf.Round(delayFrames * .5f)));

			reloading = false;
			initLoadFinished = true;
			yield return null;//not sure if it would error w\o this if delay was 0
		}

		public IEnumerator CoMorphUpdate(int delayFrames = 6, bool forceReset = false, bool forceChange = false)
		{
			var tmp = reloading;
			if(forceChange)
				reloading = true;

			for(int a = 0; a < delayFrames; ++a)
				yield return null;

			//CharaMorpher_Core.Logger.LogDebug("Updating morph values after card save/load");
			MorphChangeUpdate(forceReset);

			if(forceChange)
				reloading = tmp;

			yield return null;//not sure if it would error w\o this if delay was 0


		}
		public IEnumerator CoMorphAfterABMX(bool forcereset = false, bool abmxRefresh = false)
		{
			var boneCtrl = ChaControl.GetComponent<BoneController>();
			while(boneCtrl && (boneCtrl.NeedsFullRefresh || boneCtrl.NeedsBaselineUpdate)) yield return null;

			//CharaMorpher_Core.Logger.LogDebug("Updating morph values after ABMX");
			MorphChangeUpdate(forcereset);

			yield return null;
		}

		private string MakeDirPath(string path) => CharaMorpher_Core.MakeDirPath(path);

		/// <summary>
		/// Called whenever base character data needs to be updated for calculations
		/// </summary>
		/// <param name="currentGameMode">game mode state</param>
		/// <param name="abmxOnly">Only change ABMX data for current character (base character data is not changed)</param>
		public void OnCharaReload(GameMode currentGameMode, bool abmxOnly = false)
		{

			var cfg = CharaMorpher_Core.Instance.cfg;
			var boneCtrl = ChaControl.GetComponent<BoneController>();

			if(!abmxOnly)
			{
				//clear original data
				CharaMorpher_Core.Logger.LogDebug("clear data");
				m_data1.Clear();
				m_data2.Clear();
			}

			//store picked character data
			CharaMorpher_Core.Logger.LogDebug("replace data 1");
			if(abmxOnly)
				m_data1.abmx.Populate(this);
			else
				m_data1.Copy(this); //get all character data!!!

			UpdateMorphTarget();

			//CharaMorpher_Core.Logger.LogDebug("Morphing model...");
			////Update the model
			boneCtrl.NeedsFullRefresh = true;

			//make sure this always sends
			//var tmp = reloading;
			//reloading = true;
			StartCoroutine(CoMorphUpdate(8,forceChange: true));
			//reloading = tmp;

		}

		public void UpdateMorphTarget()
		{
			var cfg = CharaMorpher_Core.Instance.cfg;
			//create path to morph target
			string path = Path.Combine(MakeDirPath(cfg.charDir.Value), MakeDirPath(cfg.imageName.Value));

			// CharaMorpher_Core.Logger.LogDebug($"image path: {path}");

			//Get referenced character data (only needs to be loaded once)
			if(File.Exists(path))
				if(charData == null ||
					lastCharDir != path ||
					File.GetLastWriteTime(path).Ticks != lastDT.Ticks)
				{
					lastDT = File.GetLastWriteTime(path);
					lastCharDir = path;
					charData = new MorphData();

					CharaMorpher_Core.Logger.LogDebug("load morph target");
					ChaFileControl.LoadCharaFile(path/*, 255 female; 0 male*/);

					CharaMorpher_Core.Logger.LogDebug("copy morph target");
					charData.Copy(this);

					CharaMorpher_Core.Logger.LogDebug("reset original data");
					//Reset original character data
					ChaControl.chaFile.CopyAll(m_data1.main);

#if HS2 || AI
					//CopyAll will not copy this data in hs2/ai
					ChaControl.chaFile.dataID = m_data1.main.dataID;
#endif
					//ChaControl.Load();
				}

			if(KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame && ChaControl.sex != 1/*(allowed in maker not in main game)*/)
				return;

			CharaMorpher_Core.Logger.LogDebug("replace data 2");
			m_data2.Copy(charData);
		}

		/// <inheritdoc/>
		protected override void OnReload(GameMode currentGameMode, bool keepState)
		{
			if(keepState) return;

			CharaMorpher_Core.Logger.LogDebug("Character start reloading...");
			//reloading = true;

			//stop all rouge co-routines (probably not needed)
			StopAllCoroutines();

			//only use coroutine here (or not)
			//StartCoroutine(CoMorphReload(30));
			OnCharaReload(currentGameMode);
			//reloading = false;

			//  ChaControl.chaFile.coordinate[0].clothes.parts[0].colorInfo[0].;
			//  ChaCustom.CustomSelectInfoComponent thm;

		}

		///<inheritdoc/>
		protected override void OnCardBeingSaved(GameMode currentGameMode)
		{
			if(!CharaMorpher_Core.Instance.cfg.saveWithMorph.Value)
			{
				reloading = true;//just in-case
				StartCoroutine(CoMorphReload(6, abmxOnly: true));
			}
			else
				StartCoroutine(CoMorphUpdate());
		}

		/// <inheritdoc/> 
		protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
		{
			StartCoroutine(CoMorphUpdate());
		}

		//Taken from ABMX to get the data from card more easily 
		internal static List<BoneModifier> ReadBoneModifiers(PluginData data)
		{
			if(data != null)
			{
				try
				{
					switch(data.version)
					{
					case 2:
						return MessagePack.LZ4MessagePackSerializer.Deserialize<List<BoneModifier>>(
								(byte[])data.data["boneData"]);
					//TODO: get the old data converter
#if KK || EC || KKS
					case 1:
						CharaMorpher_Core.Logger.LogDebug("[KKABMX] Loading legacy embedded ABM data");
						return ABMXOldDataConverter.MigrateOldExtData(data);
#endif

					default:
						throw new NotSupportedException($"[KKABMX] Save version {data.version} is not supported");
					}
				}
				catch(Exception ex)
				{
					CharaMorpher_Core.Logger.LogError("[KKABMX] Failed to load extended data - " + ex);
				}
			}
			return new List<BoneModifier>();
		}


		/// <summary>
		/// Update bones/shapes whenever a change is made to the sliders
		/// </summary>
		/// <param name="forceReset: ">reset regardless of other perimeters</param>
		public void MorphChangeUpdate(bool forceReset = false)
		{
			var cfg = CharaMorpher_Core.Instance.cfg;

			if(m_data1?.main == null) return;
			{


#if HS2 || AI
				string storedID = m_data1.id, cardID = ChaControl.chaFile.dataID;
#elif KKS
				string storedID = m_data1.id, cardID = ChaControl.chaFile.about.dataID;
#elif KK //not sure if this will work (it didn't but it's just an optimization)
				string storedID = m_data1.id.ToString(), cardID = ChaControl.chaID.ToString();
#endif
				//	CharaMorpher_Core.Logger.LogDebug($"file is: {cardID}");
				//	CharaMorpher_Core.Logger.LogDebug($"stored file is: {storedID}");

				if(cardID == null || cardID != storedID) return;
			}

			if(KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame && ChaControl.sex != 1/*(allowed in maker as of now)*/)
				return;

			if(KoikatuAPI.GetCurrentGameMode() == GameMode.Maker &&
				MakerAPI.GetMakerSex() != 1 && !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

			//check if I need to change values period
			if(!reloading && (!cfg.enable.Value ||
				(KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame ?
				!cfg.enableInGame.Value : false))) return;

			var charaCtrl = ChaControl;
			var boneCtrl = charaCtrl.GetComponent<BoneController>();

			#region Merge results

			//add non-existent bones to other lists

			//Body
			BoneModifierMatching(ref m_data1.abmx.body, ref m_data2.abmx.body);
			BoneModifierMatching(ref m_data2.abmx.body, ref m_data1.abmx.body);

			//Face
			BoneModifierMatching(ref m_data1.abmx.face, ref m_data2.abmx.face);
			BoneModifierMatching(ref m_data2.abmx.face, ref m_data1.abmx.face);

			//current body
			BoneModifierMatching(ref boneCtrl, m_data1.abmx.body);
			BoneModifierMatching(ref boneCtrl, m_data1.abmx.face);

			//sort list
			m_data1.abmx.body.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
			m_data2.abmx.body.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
			m_data1.abmx.face.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
			m_data2.abmx.face.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));

			#endregion

			bool reset = !cfg.enable.Value;
			reset = KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame ? reset || !cfg.enableInGame.Value : reset;

			UpdateMorphValues(true);//may fix issues(maybe... ¯\_(ツ)_/¯)
			UpdateMorphValues(forceReset ? true : reset);
		}

		private void UpdateMorphValues(bool reset)
		{
			var cfg = CharaMorpher_Core.Instance.cfg;
			var charaCtrl = ChaControl;
			var boneCtrl = charaCtrl.GetComponent<BoneController>();

			byte enable = (byte)(reset ? 0 : 1);

			//update obscure values
			{
				//not sure how to update this :\
				charaCtrl.fileBody.areolaSize = (m_data1.main.custom.body.areolaSize +
							enable * controls.body * controls.boobs * (m_data2.main.custom.body.areolaSize - m_data1.main.custom.body.areolaSize));

				charaCtrl.fileBody.bustSoftness = (m_data1.main.custom.body.bustSoftness +
							enable * controls.body * controls.boobs * (m_data2.main.custom.body.bustSoftness - m_data1.main.custom.body.bustSoftness));

				charaCtrl.fileBody.bustWeight = (m_data1.main.custom.body.bustWeight +
							enable * controls.body * controls.boobs * (m_data2.main.custom.body.bustWeight - m_data1.main.custom.body.bustWeight));

#if HS2 || AI
				charaCtrl.ChangeNipColor();
				charaCtrl.ChangeNipGloss();
				charaCtrl.ChangeNipKind();
				charaCtrl.ChangeNipScale();
#elif KKS || KK
				charaCtrl.ChangeSettingAreolaSize();
				charaCtrl.ChangeSettingNipColor();
				charaCtrl.ChangeSettingNipGlossPower();
				charaCtrl.ChangeSettingNip();
#endif

				charaCtrl.UpdateBustSoftnessAndGravity();
			}

			//CharaMorpher_Core.Logger.LogDebug($"data 1 body bones: {m_data1.abmx.body.Count}");
			//CharaMorpher_Core.Logger.LogDebug($"data 2 body bones: {m_data2.abmx.body.Count}");
			//CharaMorpher_Core.Logger.LogDebug($"data 1 face bones: {m_data1.abmx.face.Count}");
			//CharaMorpher_Core.Logger.LogDebug($"data 2 face bones: {m_data2.abmx.face.Count}");
			//CharaMorpher_Core.Logger.LogDebug($"chara bones: {boneCtrl.Modifiers.Count}");
			//CharaMorpher_Core.Logger.LogDebug($"body parts: {m_data1.main.custom.body.shapeValueBody.Length}");
			//CharaMorpher_Core.Logger.LogDebug($"face parts: {m_data1.main.custom.face.shapeValueFace.Length}");



			//float MyLerp(float a, float b, float t) => a + t * (b - a);//this is not right but needed it for testing

			//value update loop
			for(int a = 0; a < Mathf.Max(new float[]
			{
				m_data1.main.custom.body.shapeValueBody.Length,
				m_data1.main.custom.face.shapeValueFace.Length,
				m_data1.abmx.body.Count, m_data1.abmx.face.Count
			});
			++a)
			{
				float result = 0;

				enable = (byte)(reset || !cfg.enableABMX.Value ? 0 : 1);

				#region ABMX

				//Body
				if(a < m_data1.abmx.body.Count)
				{
					//  CharaMorpher_Core.Logger.LogDebug($"looking for body values");

					var bone1 = m_data1.abmx.body[a];
					var bone2 = m_data2.abmx.body[a];
					var current = boneCtrl.Modifiers.Find((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));

					//  CharaMorpher.Logger.LogDebug($"found values");
					//    CharaMorpher_Core.Logger.LogDebug($"current = {current.BoneName}");

					float modVal = 0;

					//remove L/R from bone name
					string content = bone1.BoneName.Trim().ToLower();

					string[] fingerNames = new[]
					 {
                            //HS2
                            "cf_J_Hand_Thumb",
							"cf_J_Hand_Index",
							"cf_J_Hand_Middle",
							"cf_J_Hand_Ring",
							"cf_J_Hand_Little",
                            //KK
                            "cf_j_thumb",
							"cf_j_index",
							"cf_j_middle",
							"cf_j_ring",
							"cf_j_little"
						};

					if(fingerNames.ToList().FindIndex((k) => content.Contains(k.Trim().ToLower())) >= 0)
						modVal = controls.abmxHands;
					else
					{

						string ending1 = "";
						string ending2 = "";
						int end = content.LastIndexOf("_");
						int end2 = -1;
						if(end >= 0)
						{
							ending1 = content.Substring(content.LastIndexOf("_"));
							end2 = content.Substring(0, end).LastIndexOf("_");
						}
						if(end2 >= 0)
							ending2 = content.Substring(end - (end - (end2)));

						// CharaMorpher_Core.Logger.LogDebug($"the result of ending 2 = {ending2}");

						if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
							content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

						// CharaMorpher_Core.Logger.LogDebug($"content of bone = {content ?? "... this is null"}");
#if KK || KKS
						switch(bonecatagories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
						switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
#endif
						{
						case "torso":
							modVal = controls.abmxTorso;
							break;
						case "boobs":
							modVal = controls.abmxBoobs;
							break;
						case "butt":
							modVal = controls.abmxButt;
							break;
						case "arms":
							modVal = controls.abmxArms;
							break;
						case "hands":
							modVal = controls.abmxHands;
							break;
						case "genitals":
							modVal = controls.abmxGenitals;
							break;
						case "legs":
							modVal = controls.abmxLegs;
							break;
						case "feet":
							modVal = controls.abmxFeet;
							break;

						default:
							modVal = 1;
							break;
						}
					}

					UpdateBoneModifier(ref current, bone1, bone2, modVal, sectVal: controls.body * controls.abmxBody, enable: enable);

				}

				//face
				if(a < m_data1.abmx.face.Count)
				{
					//   CharaMorpher_Core.Logger.LogDebug($"looking for face values");
					var bone1 = m_data1.abmx.face[a];
					var bone2 = m_data2.abmx.face[a];
					var current = boneCtrl.Modifiers.Find((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));

					float modVal = 0;

					//remove L/R from bone name
					string content = bone1.BoneName.Trim().ToLower();
					string ending1 = "";
					string ending2 = "";
					int end = content.LastIndexOf("_");
					int end2 = -1;
					if(end >= 0)
					{
						ending1 = content.Substring(content.LastIndexOf("_"));
						end2 = content.Substring(0, end).LastIndexOf("_");
					}
					if(end2 >= 0)
						ending2 = content.Substring(end - (end - (end2)));

					// CharaMorpher_Core.Logger.LogDebug($"the result of ending 2 = {ending2}");

					if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
						content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

					//  CharaMorpher_Core.Logger.LogDebug($"content of bone = {content ?? "... this is null"}");


#if KK || KKS
					switch(bonecatagories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
					switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
#endif
					{

					case "eyes":
						modVal = controls.abmxEyes;
						break;
					case "mouth":
						modVal = controls.abmxMouth;
						break;
					case "ears":
						modVal = controls.abmxEars;
						break;
					case "hair":
						modVal = controls.abmxHair;
						break;


					default:
						modVal = 1;
						break;
					}

					UpdateBoneModifier(ref current, bone1, bone2, modVal, sectVal: controls.face * controls.abmxHead, enable: enable);
				}
				#endregion

				enable = (byte)(reset ? 0 : 1);

				#region Main

				//Body Shape
				// CharaMorpher_Core.Logger.LogDebug($"updating body");
				if(a < m_data1.main.custom.body.shapeValueBody.Length)
				{
					//Value Update
					{
						float
							d1 = m_data1.main.custom.body.shapeValueBody[a],
							d2 = m_data2.main.custom.body.shapeValueBody[a];

						if(cfg.headIndex.Value == a)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body * controls.head);
						//result = MyLerp(d1, d2,
						//	  enable * controls.body * controls.head);//lerp, may change it later
						else
						if(cfg.torsoIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body * controls.torso);
						//result = MyLerp(d1, d2,
						//  enable * controls.body * controls.torso);//lerp, may change it later
						else
						if(cfg.buttIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body * controls.butt);
						//result = MyLerp(d1, d2,
						//enable * controls.body * controls.butt);//lerp, may change it later
						else
						if(cfg.legIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body * controls.legs);
						//result = MyLerp(d1, d2,
						// enable * controls.body * controls.legs);//lerp, may change it later
						else
						if(cfg.armIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body * controls.arms);
						//result = MyLerp(d1, d2,
						// enable * controls.body * controls.arms);//lerp, may change it later
						else
						if(cfg.brestIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body * controls.boobs);
						//result = MyLerp(d1, d2,
						//   enable * controls.body * controls.boobs);//lerp, may change it later

						else
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.body);
						//result = MyLerp(d1, d2,
						//enable * controls.body);//lerp, may change it later
					}

					// CharaMorpher_Core.Logger.LogDebug($"Loaded Body Part 1: {m_data1.main.custom.body.shapeValueBody[a]} at index {a}");
					//CharaMorpher.Logger.LogDebug($"Loaded Body Part 2: {m_data2.main.custom.body.shapeValueBody[a]} at index {a}");

					//load values to character
					charaCtrl.fileCustom.body.shapeValueBody[a] = result;
					charaCtrl.SetShapeBodyValue(a, result);
				}

				//Face Shape
				// CharaMorpher_Core.Logger.LogDebug($"updating face");
				if(a < m_data1.main.custom.face.shapeValueFace.Length)
				{
					//Value Update
					{
						float
							d1 = m_data1.main.custom.face.shapeValueFace[a],
							d2 = m_data2.main.custom.face.shapeValueFace[a];
						//	var tmp =  d1 + enable * controls.face * controls.eyes*(d2 - d1);//test equasion

						if(cfg.eyeIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.face * controls.eyes);
						//result = MyLerp(d1, d2,
						//	enable * controls.face * controls.eyes);
						else
						 if(cfg.mouthIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.face * controls.mouth);
						//result = MyLerp(d1, d2,
						// enable * controls.face * controls.mouth);
						else
						  if(cfg.earIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.face * controls.ears);
						//result = MyLerp(d1, d2,
						// enable * controls.face * controls.ears);
						else
							result = Mathf.LerpUnclamped(d1, d2,
								enable * controls.face);
						//result = MyLerp(d1, d2,
						//  enable * controls.face);
					}

					// CharaMorpher_Core.Logger.LogDebug($"Loaded Face Part 1: {m_data1.main.custom.face.shapeValueFace[a]} at index {a}");
					//CharaMorpher.Logger.LogDebug($"Loaded Face Part 2: {m_data1.main.custom.face.shapeValueFace[a]}");


					//load values to character
					//charaCtrl.fileCustom.face.shapeValueFace[a] = result;
					charaCtrl.SetShapeFaceValue(a, result);
				}
				#endregion

				//  CharaMorpher_Core.Logger.LogDebug("");
			}


			charaCtrl.UpdateShapeBody();
			charaCtrl.UpdateShapeFace();

			//if(reset)
			//{
			//	boneCtrl.NeedsFullRefresh = true;
			//	boneCtrl.NeedsBaselineUpdate = true;
			//}
#if KKS || KK

			//charaCtrl.ChangeSettingBodyDetail();
			//charaCtrl.ChangeSettingFaceDetail();
			//charaCtrl.ChangeSettingNip();
#endif

			// initialLoad = false;
		}

		/// <summary>
		/// Adds all bones from bone2 to bone1
		/// </summary>
		/// <param name="bone1"></param>
		/// <param name="bone2"></param>
		private void BoneModifierMatching(ref List<BoneModifier> bone1, ref List<BoneModifier> bone2)
		{

			foreach(var bone in bone2)
			{
				string content = bone.BoneName.Trim().ToLower();
				if(bone1.FindIndex((k) => k.BoneName.Trim().ToLower() == content) < 0)
				{
					string name = bone.BoneName;

					bone1.Add(new BoneModifier(name));
					if(bone.IsCoordinateSpecific())
						bone1.Last().MakeCoordinateSpecific(bone.CoordinateModifiers.Length);
				}
			}
		}

		/// <summary>
		/// Adds all bones from bone2 to bone1
		/// </summary>
		/// <param name="bone1"></param>
		/// <param name="bone2"></param>
		private void BoneModifierMatching(ref BoneController bone1, List<BoneModifier> bone2)
		{

			foreach(var bone in bone2)
			{
				string content = bone.BoneName.Trim().ToLower();

				if(bone1.Modifiers.FindIndex((k) => k.BoneName.Trim().ToLower() == content) < 0)
				{
					string name = bone.BoneName;

					bone1.AddModifier(new BoneModifier(name));
					if(bone.IsCoordinateSpecific())
						bone1.Modifiers.Last().MakeCoordinateSpecific(bone.CoordinateModifiers.Length);
				}
			}
		}

		/// <summary>
		/// Update BoneModifier values by lerping between 2 bones
		/// </summary>
		/// <param name="current">bone to change</param>
		/// <param name="bone1">initial lerp bone</param>
		/// <param name="bone2">target lerp bone</param>
		/// <param name="modVal">target amount (0 -> 1)</param>
		/// <param name="sectVal">control target amount (optional)</param>
		/// <param name="enable"></param>
		private void UpdateBoneModifier(ref BoneModifier current, BoneModifier bone1, BoneModifier bone2, float modVal, float sectVal = 1, byte enable = 1)
		{
			int count = 0;//may use this in other mods
						  //  CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
			foreach(var mod in current.CoordinateModifiers)
			{

				//  CharaMorpher_Core.Logger.LogDebug($"in for loop");
				var inRange = count < bone2.CoordinateModifiers.Length;


				mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].PositionModifier, bone2.CoordinateModifiers[inRange ? count : 0].PositionModifier,
					Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

				mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].RotationModifier, bone2.CoordinateModifiers[inRange ? count : 0].RotationModifier,
					Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

				mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].ScaleModifier, bone2.CoordinateModifiers[inRange ? count : 0].ScaleModifier,
					Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

				mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].LengthModifier, bone2.CoordinateModifiers[inRange ? count : 0].LengthModifier,
					Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

				//   CharaMorpher_Core.Logger.LogDebug($"updated values");
				if(count == 0)
				{
					//CharaMorpher.Logger.LogDebug($"lerp Value {a}: {enable * modVal}");
					//CharaMorpher.Logger.LogDebug($"{current.BoneName} modifiers!!");
					//CharaMorpher.Logger.LogDebug($"Body Bone 1 scale {a}: {bone1.CoordinateModifiers[count].ScaleModifier}");
					//CharaMorpher.Logger.LogDebug($"Body Bone 2 scale {a}: {bone2.CoordinateModifiers[count].ScaleModifier}");
					//CharaMorpher.Logger.LogDebug($"Result scale {a}: {mod.ScaleModifier}");
				}

				++count;
			}

			var boneCtrl = ChaControl.GetComponent<BoneController>();
			//   CharaMorpher_Core.Logger.LogDebug($"applying values");
			current.Apply(boneCtrl.CurrentCoordinate.Value, null, true);
		}
	}

	/// <summary>
	/// Needed to copy this class from ABMX in case old card is loaded (Taken directly from source)
	/// </summary>
	internal static class ABMXOldDataConverter
	{
		private const string ExtDataBoneDataKey = "boneData";

		public static List<BoneModifier> MigrateOldExtData(PluginData pluginData)
		{
			if(pluginData == null) return null;
			if(!pluginData.data.TryGetValue(ExtDataBoneDataKey, out var value)) return null;
			if(!(value is string textData)) return null;

			return MigrateOldStringData(textData);
		}

		public static List<BoneModifier> MigrateOldStringData(string textData)
		{
			if(string.IsNullOrEmpty(textData)) return null;
			return DeserializeToModifiers(textData.Split());
		}

		private static List<BoneModifier> DeserializeToModifiers(IEnumerable<string> lines)
		{
			string GetTrimmedName(string[] splitValues)
			{
				// Turn cf_d_sk_top__1 into cf_d_sk_top 
				var boneName = splitValues[1];
				return boneName[boneName.Length - 2] == '_' && boneName[boneName.Length - 3] == '_'
					? boneName.Substring(0, boneName.Length - 3)
					: boneName;
			}

			var query = from lineText in lines
						let trimmedText = lineText?.Trim()
						where !string.IsNullOrEmpty(trimmedText)
						let splitValues = trimmedText.Split(',')
						where splitValues.Length >= 6
						group splitValues by GetTrimmedName(splitValues);

			var results = new List<BoneModifier>();

			foreach(var groupedBoneDataEntries in query)
			{
				var groupedOrderedEntries = groupedBoneDataEntries.OrderBy(x => x[1]).ToList();

				var coordinateModifiers = new List<BoneModifierData>(groupedOrderedEntries.Count);

				foreach(var singleEntry in groupedOrderedEntries)
				{
					try
					{
						//var boneName = singleEntry[1];
						//var isEnabled = bool.Parse(singleEntry[2]);
						var x = float.Parse(singleEntry[3]);
						var y = float.Parse(singleEntry[4]);
						var z = float.Parse(singleEntry[5]);

						var lenMod = singleEntry.Length > 6 ? float.Parse(singleEntry[6]) : 1f;

						coordinateModifiers.Add(new BoneModifierData(new Vector3(x, y, z), lenMod));
					}
					catch(Exception ex)
					{
						CharaMorpher_Core.Logger.LogError($"Failed to load legacy line \"{string.Join(",", singleEntry)}\" - {ex.Message}");
					}
				}

				if(coordinateModifiers.Count == 0)
					continue;

				const int kkCoordinateCount = 7;
				if(coordinateModifiers.Count > kkCoordinateCount)
					coordinateModifiers.RemoveRange(0, coordinateModifiers.Count - kkCoordinateCount);
				if(coordinateModifiers.Count > 1 && coordinateModifiers.Count < kkCoordinateCount)
					coordinateModifiers.RemoveRange(0, coordinateModifiers.Count - 1);

				results.Add(new BoneModifier(groupedBoneDataEntries.Key, coordinateModifiers.ToArray()));
			}

			return results;
		}
	}
}
