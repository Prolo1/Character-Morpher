using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

using KKAPI;
using KKAPI.Utilities;
using KKAPI.Chara;
using KKAPI.Maker;
using KKABMX.Core;
using ExtensibleSaveFormat;
using MessagePack.Resolvers;
using MessagePack;

//using static System.Tuple;
using Manager;
//using UniRx;

#if HONEY_API
using CharaCustom;
using AIChara;
//using AIProject;
#else
using ChaCustom;
//using StrayTech;
#endif

using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.Morph_Util;
using static Character_Morpher.CharaMorpher_Controller;
using static Character_Morpher.CharaMorpher_GUI;
using static Character_Morpher.CurrentSaveLoadManager;
using KKAPI.Studio;
using System.ComponentModel;

namespace Character_Morpher
{
	public class CharaMorpher_Controller : CharaCustomFunctionController
	{
		#region Data
		private PluginData m_extData = null;
		private static string lastCharDir = "";
		private static DateTime lastDT = new DateTime();

		internal static MorphData morphCharData = null;
		internal MorphControls
			controls = new MorphControls(),
			ctrls1 = null, ctrls2 = null;
		//internal static readonly MorphTarget morphTarget = new MorphTarget();
		private static bool m_faceBonemodTgl = true, m_bodyBonemodTgl = true;
		internal static bool faceBonemodTgl
		{
			get { return !MakerAPI.InsideMaker || m_faceBonemodTgl; }
			set { m_faceBonemodTgl = value; }
		}
		internal static bool bodyBonemodTgl
		{
			get { return !MakerAPI.InsideMaker || m_bodyBonemodTgl; }
			set { m_bodyBonemodTgl = value; }
		}
		static bool regesteredResolver = false;

		public readonly MorphData m_data1 = new MorphData(), m_data2 = new MorphData(), m_initalData = new MorphData();
		public static bool CanUseCardMorphData
		{
			get => ((MakerAPI.InsideMaker || StudioAPI.InsideStudio) ?
				cfg.preferCardMorphDataMaker.Value :
				cfg.preferCardMorphDataGame.Value);
		}
		public bool IsUsingExtMorphData
		{
			get => ((MakerAPI.InsideMaker || StudioAPI.InsideStudio) ?
				cfg.preferCardMorphDataMaker.Value :
				cfg.preferCardMorphDataGame.Value) &&
				m_extData != null;
		}

		internal bool morphEnable = true;
		internal bool morphEnableABMX = true;

		public bool Enable
		{
			get => cfg.enable.Value && morphEnable;
			set
			{
				morphEnable = value;
				if(isInitLoadFinished)
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(CoMorphChangeUpdate(delay: a));//this may be necessary (it is)
			}
		}

		public bool EnableABMX
		{
			get => cfg.enableABMX.Value && morphEnableABMX;
			set
			{
				morphEnableABMX = value;
				if(isInitLoadFinished)
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						StartCoroutine(CoMorphChangeUpdate(delay: a));//this may be necessary (it is)
			}
		}

		/// <summary>
		/// Called after the model has finished being loaded for the first time
		/// </summary>
		public bool isInitLoadFinished { get; private set; } = false;

		/// <summary>
		/// In the process of reloading. set to false after complete
		/// </summary>
		public bool isReloading { get; internal set; } = true;

		/// <summary>
		/// makes sure most main functions don't run when creating template character
		/// </summary>
		public bool isDummy { get; internal set; } = false;

		internal bool ResetCheck
		{
			get
			{
				bool reset = !Enable && !isReloading;
				return KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame ?
						(reset || !cfg.enableInGame.Value) : reset;
			}
		}

		#region I don't want to see this
#if KOI_API

		public static readonly List<KeyValuePair<string, string>> boneDatabaseCategories = new List<KeyValuePair<string, string>>()
#else
		//this is a tuple list btw (of bones found in abmx mod and online... somewhere)

		public static readonly List<(string, string)> boneDatabaseCategories = new List<(string, string)>()
#endif

#if KOI_API
		
		#region KKBones
		{
            //ABMX
		   
			//Body;
		    new KeyValuePair<string, string>("cf_n_height"       , ""  ),
			new KeyValuePair<string, string>("cf_d_sk_top"       , ""      ),
			new KeyValuePair<string, string>("cf_d_sk_00_00"     , ""      ),
			new KeyValuePair<string, string>("cf_d_sk_07_00"     , ""      ),
			new KeyValuePair<string, string>("cf_d_sk_06_00"     , ""      ),
			new KeyValuePair<string, string>("cf_d_sk_05_00"     , ""      ),
			new KeyValuePair<string, string>("cf_d_sk_04_00"     , ""      ),
		   
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
			new KeyValuePair<string, string>("cf_j_spine01"      , "torso"  ),
			new KeyValuePair<string, string>("cf_j_spine02"      , "torso"  ),
			new KeyValuePair<string, string>("cf_j_spine03"      , "torso"  ),
		   
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
			new KeyValuePair<string, string>("k_f_armupL_00"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupL_01"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupL_02"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupL_03"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupR_00"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupR_01"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupR_02"     , "arms" ),
			new KeyValuePair<string, string>("k_f_armupR_03"     , "arms" ),
		    
		    //Hands;
		    new KeyValuePair<string, string>("cf_j_hand_L" , "hands"),
			new KeyValuePair<string, string>("cf_s_hand_L" , "hands"),
			new KeyValuePair<string, string>("cf_hit_arm_L", "hands"),
			new KeyValuePair<string, string>("k_f_handL_00", "hands" ),
			new KeyValuePair<string, string>("k_f_handL_01", "hands" ),
			new KeyValuePair<string, string>("k_f_handL_02", "hands" ),
			new KeyValuePair<string, string>("k_f_handL_03", "hands" ),
			new KeyValuePair<string, string>("k_f_handR_00", "hands" ),
			new KeyValuePair<string, string>("k_f_handR_01", "hands" ),
			new KeyValuePair<string, string>("k_f_handR_02", "hands" ),
			new KeyValuePair<string, string>("k_f_handR_03", "hands" ),
		  
		    //Butt;
			new KeyValuePair<string, string>("cf_t_hips"         , "butt"),
			new KeyValuePair<string, string>("cf_s_siri_L"       , "butt"),
			new KeyValuePair<string, string>("cf_hit_siri_L"     , "butt"),
		 
		    //Legs;
			new KeyValuePair<string, string>("cf_hit_waist_L"    , "legs"),
			new KeyValuePair<string, string>("cf_s_waist01"      , "legs"),
			new KeyValuePair<string, string>("cf_s_waist02"      , "legs"),
			new KeyValuePair<string, string>("cf_j_waist01"      , "legs"),
			new KeyValuePair<string, string>("cf_j_waist02"      , "legs"),
			new KeyValuePair<string, string>("cf_j_thigh00_L"    , "legs"),
			new KeyValuePair<string, string>("cf_s_thigh01_L"    , "legs"),
			new KeyValuePair<string, string>("cf_s_thigh02_L"    , "legs"),
			new KeyValuePair<string, string>("cf_s_thigh03_L"    , "legs"),
			new KeyValuePair<string, string>("cf_hit_thigh01_L"  , "legs"),
			new KeyValuePair<string, string>("cf_hit_thigh02_L"  , "legs"),
			new KeyValuePair<string, string>("cf_j_leg01_L"      , "legs"),
			new KeyValuePair<string, string>("cf_s_leg01_L"      , "legs"),
			new KeyValuePair<string, string>("cf_s_leg02_L"      , "legs"),
			new KeyValuePair<string, string>("k_f_legupL_00"     , "legs"),
			new KeyValuePair<string, string>("k_f_legupR_00"     , "legs"),
			new KeyValuePair<string, string>("k_f_leglowL_00"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowL_01"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowL_02"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowL_03"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowR_00"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowR_01"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowR_02"    ,"legs" ),
			new KeyValuePair<string, string>("k_f_leglowR_03"    ,"legs" ),
		
			//Feet;
			new KeyValuePair<string, string>("cf_s_leg03_L"      , "feet"),
			new KeyValuePair<string, string>("cf_j_foot_L"       , "feet"),
			new KeyValuePair<string, string>("cf_j_leg03_L"      , "feet"),
			new KeyValuePair<string, string>("cf_j_toes_L"       , "feet"),
		
			//Genitals;
		    new KeyValuePair<string, string>("cf_d_kokan"        , "genitals"),
			new KeyValuePair<string, string>("cf_j_kokan"        , "genitals"),
			new KeyValuePair<string, string>("cm_J_dan100_00"    , "genitals"),
			new KeyValuePair<string, string>("cm_J_dan109_00"    , "genitals"),
			new KeyValuePair<string, string>("cm_J_dan_f_L"      , "genitals"),
			new KeyValuePair<string, string>("cf_j_ana"          , "genitals"),
		
		
			//Head;
			new KeyValuePair<string, string>("cf_j_head",     "" ),
			new KeyValuePair<string, string>("cf_s_head"         , ""     ),
			new KeyValuePair<string, string>("cf_hit_head"       , ""     ),
			new KeyValuePair<string, string>("cf_s_neck"         , ""  ),
			new KeyValuePair<string, string>("cf_J_FaceBase"     , ""     ),
			new KeyValuePair<string, string>("cf_J_FaceUp_ty"    , ""     ),
			new KeyValuePair<string, string>("cf_J_FaceUp_tz"    , ""     ),
			new KeyValuePair<string, string>("cf_J_FaceLow_sx"   , ""     ),
			new KeyValuePair<string, string>("cf_J_FaceLow_tz"   , ""     ),
			new KeyValuePair<string, string>("cf_J_Chin_Base"    , ""     ),
			new KeyValuePair<string, string>("cf_J_ChinLow"      , ""     ),
			new KeyValuePair<string, string>("cf_J_ChinTip_Base" , ""     ),
			new KeyValuePair<string, string>("cf_J_CheekUpBase"  , ""     ),
			new KeyValuePair<string, string>("cf_J_CheekUp2_L"   , ""     ),
			new KeyValuePair<string, string>("cf_J_CheekUp_s_L"  , ""     ),
			new KeyValuePair<string, string>("cf_J_CheekLow_s_L" , ""     ),
		   
			//Nose;
		    new KeyValuePair<string, string>("cf_J_NoseBase_rx"  , "nose"    ),
			new KeyValuePair<string, string>("cf_J_Nose_tip"     , "nose"    ),
			new KeyValuePair<string, string>("cf_J_NoseBridge_rx", "nose"    ),
			new KeyValuePair<string, string>("cf_J_NoseBridge_ty", "nose"    ),
		
		    //Mouth;
		    new KeyValuePair<string, string>("cf_J_MouthBase_rx" ,"mouth" ),
			new KeyValuePair<string, string>("cf_J_MouthBase_ty" ,"mouth" ),
			new KeyValuePair<string, string>("cf_J_Mouth_L"      ,"mouth" ),
			new KeyValuePair<string, string>("cf_J_Mouthup"      ,"mouth" ),
			new KeyValuePair<string, string>("cf_J_MouthLow"     ,"mouth" ),
			new KeyValuePair<string, string>("cf_J_MouthCavity"  ,"mouth" ),
		
		
		   //Ears;
			new KeyValuePair<string, string>("cf_J_megane_rx_ear", "ears" ),//this should be an ear
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

		}
		#endregion
#elif HONEY_API
		
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
			
				
				
				//Butt
				("cf_hit_Siri_s_L,False,1,1,1,1 Hitbox - Butt (Left)                         ",   "butt"),
				("cf_hit_Siri_s_R,False,1,1,1,1 Hitbox - Butt (Right)                        ",   "butt"),
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


				//legs
                ("cf_hit_LegUp01_s_L,False,1,1,1,1 Hitbox - Thigh (Left)                     "  , "legs"),
				("cf_hit_LegUp01_s_R,False,1,1,1,1 Hitbox - Thigh (Right)                    "  , "legs"),
				("cf_hit_Kosi02_s,False,1,1,1,1 Hitbox - Hips                                "  , "legs"),
				("cf_J_Hips,False,1,1,1,1 Scale                                              "  , "legs"),
				("cf_J_Kosi01,False,1,1,1,1 Waist & Below                                    "  , "legs"),
				("cf_J_Kosi01_s,False,1,1,1,1 Pelvis [Ignores Skirt]                         "  , "legs"),
				("cf_J_Kosi02,False,1,1,1,1 Hips & Below                                     "  , "legs"),
				("cf_J_Kosi02_s,False,1,1,1,1 Hips [Ignores Skirt]                           "  , "legs"),
				("cf_J_Kosi03,False,1,1,1,1                                                  "  , "legs"),
				("cf_J_Kosi03_s,False,1,1,1,1                                                "  , "legs"),
				("cf_J_Kosi04_s,False,1,1,1,1                                                "  , "legs"),
				("cf_J_LegDam_L,False,1,1,1,1                                                "  , "legs"),
				( "cf_J_LegKnee_back_s_L,False,1,1,1,1 Back of Knee (Left)                   "  ,"legs"),
				("cf_J_LegKnee_back_s_R,False,1,1,1,1 Back of Knee (Right)                   "  , "legs"),
				("cf_J_LegKnee_dam_L,False,1,1,1,1 Front of Knee (Left)                      "  , "legs"),
				("cf_J_LegKnee_dam_R,False,1,1,1,1 Front of Knee (Right)                     "  , "legs"),
				("cf_J_LegKnee_low_s_L,False,1,1,1,1 Knee Tone (Left)                        "  , "legs"),
				("cf_J_LegKnee_low_s_R,False,1,1,1,1 Knee Tone (Right)                       "  , "legs"),
				("cf_J_LegLow01_L,False,1,1,1,1 Knees & Below                                "  , "legs"),
				("cf_J_LegLow01_R,False,1,1,1,1 Knees & Below                                "  , "legs"),
				("cf_J_LegLow01_s_L,False,1,1,1,1 Calf (Left)                                "  , "legs"),
				("cf_J_LegLow01_s_R,False,1,1,1,1 Calf (Right)                               "  , "legs"),
				("cf_J_LegLow02_s_L,False,1,1,1,1 Lower Calf (Left)                          "  , "legs"),
				("cf_J_LegLow02_s_R,False,1,1,1,1 Lower Calf (Right)                         "  , "legs"),
				("cf_J_LegLow03_s_L,False,1,1,1,1 Ankle (Left)                               "  , "legs"),
				("cf_J_LegLow03_s_R,False,1,1,1,1 Ankle (Right)                              "  , "legs"),
				("cf_J_LegLowDam_L,False,1,1,1,1                                             "  , "legs"),
				("cf_J_LegLowRoll_L,False,1,1,1,1 Lower Leg Length (Left)                    "  , "legs"),
				("cf_J_LegLowRoll_R,False,1,1,1,1 Lower Leg Length (Right)                   "  , "legs"),
				("cf_J_LegUp_L,False,1,1,1,1                                                 "  , "legs"),
				("cf_J_LegUp00_L,False,1,1,1,1 Overall Leg (Left)                            "  , "legs"),
				("cf_J_LegUp00_R,False,1,1,1,1 Overall Leg (Right)                           "  , "legs"),
				("cf_J_LegUp01_s_L,False,1,1,1,1 Upper Thigh (Left)                          "  , "legs"),
				("cf_J_LegUp01_s_R,False,1,1,1,1 Upper Thigh (Right)                         "  , "legs"),
				("cf_J_LegUp02_s_L,False,1,1,1,1 Lower Thigh (Left)                          "  , "legs"),
				("cf_J_LegUp02_s_R,False,1,1,1,1 Lower Thigh (Right)                         "  , "legs"),
				("cf_J_LegUp03_L,False,1,1,1,1 Knee (Left)                                   "  , "legs"),
				("cf_J_LegUp03_R,False,1,1,1,1 Knee (Right)                                  "  , "legs"),
				("cf_J_LegUp03_s_L,False,1,1,1,1 Above the Knee (Left)                       "  , "legs"),
				("cf_J_LegUp03_s_R,False,1,1,1,1 Above the Knee (Right)                      "  , "legs"),
				("cf_J_LegUpDam_L,False,1,1,1,1 Upper Hip (Left)                             "  , "legs"),
				("cf_J_LegUpDam_R,False,1,1,1,1 Upper Hip (Right)                            "  , "legs"),
				("cf_J_LegUpDam_s_L,False,1,1,1,1 Upper Hip (Left) [Not as good]             "  , "legs"),
				("cf_J_LegUpDam_s_R,False,1,1,1,1 Upper Hip (Right) [Not as good]            "  , "legs"),
				("cf_J_LegUpLow_R,False,1,1,1,1                                              "  , "legs"),
				("cf_J_LegUpLow_s_L,False,1,1,1,1                                            "  , "legs"),
				("cf_J_Legsk_01_00"                                                             , "legs"),
				("cf_J_Legsk_01_01"                                                             , "legs"),
				("cf_J_Legsk_01_02"                                                             , "legs"),
				("cf_J_Legsk_01_03"                                                             , "legs"),
				("cf_J_Legsk_01_04"                                                             , "legs"),
				("cf_J_Legsk_01_05"                                                             , "legs"),
				("cf_J_Legsk_02_00"                                                             , "legs"),
				("cf_J_Legsk_02_01"                                                             , "legs"),
				("cf_J_Legsk_02_02"                                                             , "legs"),
				("cf_J_Legsk_02_03"                                                             , "legs"),
				("cf_J_Legsk_02_04"                                                             , "legs"),
				("cf_J_Legsk_02_05"                                                             , "legs"),
				("cf_J_Legsk_03_00"                                                             , "legs"),
				("cf_J_Legsk_03_01"                                                             , "legs"),
				("cf_J_Legsk_03_02"                                                             , "legs"),
				("cf_J_Legsk_03_03"                                                             , "legs"),
				("cf_J_Legsk_03_04"                                                             , "legs"),
				("cf_J_Legsk_03_05"                                                             , "legs"),
				("cf_J_Legsk_05_00"                                                             , "legs"),
				("cf_J_Legsk_05_01"                                                             , "legs"),
				("cf_J_Legsk_05_02"                                                             , "legs"),
				("cf_J_Legsk_05_03"                                                             , "legs"),
				("cf_J_Legsk_05_04"                                                             , "legs"),
				("cf_J_Legsk_05_05"                                                             , "legs"),
				("cf_J_Legsk_06_00"                                                             , "legs"),
				("cf_J_Legsk_06_01"                                                             , "legs"),
				("cf_J_Legsk_06_02"                                                             , "legs"),
				("cf_J_Legsk_06_03"                                                             , "legs"),
				("cf_J_Legsk_06_04"                                                             , "legs"),
				("cf_J_Legsk_06_05"                                                             , "legs"),
				("cf_J_Legsk_07_00"                                                             , "legs"),
				("cf_J_Legsk_07_01"                                                             , "legs"),
				("cf_J_Legsk_07_02"                                                             , "legs"),
				("cf_J_Legsk_07_03"                                                             , "legs"),
				("cf_J_Legsk_07_04"                                                             , "legs"),
				("cf_J_Legsk_07_05"                                                             , "legs"),


                //feet              
                ("cf_J_Foot01_L,False,1,1,1,1 Foot & Ankle (Left)                            ","feet"),
				("cf_J_Foot01_R,False,1,1,1,1 Foot & Ankle (Right)                           ","feet"),
				("cf_J_Foot02_L,False,1,1,1,1 Foot (Left)                                    ","feet"),
				("cf_J_Foot02_R,False,1,1,1,1 Foot (Right)                                   ","feet"),
				("cf_J_Toes01_L,False,1,1,1,1 Toes (Left)                                    ","feet"),
				("cf_J_Toes01_R,False,1,1,1,1 Toes (Right)                                   ","feet"),
				


				 //genitals
                ("cf_J_Kokan,False,1,1,1,1 Pussy                                             ","genitals"),
				("cf_J_Ana,False,1,1,1,1 Anus                                                ","genitals"),
				("cm_J_dan_s,False,1,1,1,1 (Penis & Balls)                                   ","genitals"),
				("cm_J_dan100_00,False,1,1,1,1 (Penis)                                       ","genitals"),
				("cm_J_dan101_00															 ","genitals"),
				("cm_J_dan109_00															 ","genitals"),
				("cm_J_dan_f_top,False,1,1,1,1 (Balls)                                       ","genitals"),
				("cm_J_dan_f_L,False,1,1,1,1 (Left Nut)                                      ","genitals"),
				("cm_J_dan_f_R,False,1,1,1,1 (Right Nut)                                     ","genitals"),
             

            
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
                
				//nose
				("cf_J_Nose_t,False,1,1,1,1 Upper Nose                                       ", "nose"),
				("cf_J_Nose_tip,False,1,1,1,1 Nose Tip                                       ", "nose"),
				("cf_J_NoseBase_s,False,1,1,1,1 Nose                                         ", "nose"),
				("cf_J_NoseBase_trs,False,1,1,1,1 Nose & Bridge                              ", "nose"),
				("cf_J_NoseBridge_s,False,1,1,1,1 Bridge                                     ", "nose"),
				("cf_J_NoseBridge_t,False,1,1,1,1 Bridge                                     ", "nose"),
				("cf_J_NoseWing_tx_L,False,1,1,1,1 Nostril (Left)                            ", "nose"),
				("cf_J_NoseWing_tx_R,False,1,1,1,1 Nostril (Right)                           ", "nose"),

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
				("cf_J_hair_FLa_01",            "hair"),
				("cf_J_hair_FLa_02",            "hair"),
				("cf_J_hair_FRa_01",            "hair"),
				("cf_J_hair_FRa_02",            "hair"),
				("cf_J_hair_BCa_01",            "hair"),
                
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
				("cf_J_sk_00_00",               ""),
				("cf_J_sk_00_01",               ""),
				("cf_J_sk_00_02",               ""),
				("cf_J_sk_00_03",               ""),
				("cf_J_sk_00_04",               ""),
				("cf_J_sk_00_05",               ""),
				("cf_J_sk_04_00",               ""),
				("cf_J_sk_04_01",               ""),
				("cf_J_sk_04_02",               ""),
				("cf_J_sk_04_03",               ""),
				("cf_J_sk_04_04",               ""),
				("cf_J_sk_04_05",               ""),


			  }
		#endregion
#endif
;
		#endregion

		#endregion

		protected override void Awake()
		{
			base.Awake();

			if(isDummy) return;

			var core = Instance;


			//Morph_Util.Logger.LogDebug($"Control set: {controls.currentSet}");
			foreach(var category in core.controlCategories)
			{
				//		Morph_Util.Logger.LogDebug($"In for loop 1 Awake");
				if(!controls.all.TryGetValue(category.Key, out var tmp))
					controls.all[category.Key] = new Dictionary<string, MorphSliderData>();

				foreach(var ctrl in category.Value)
				{
					//Morph_Util.Logger.LogDebug($"In for loop 2 Awake");
					//Morph_Util.Logger.LogDebug($"defaults: [{category.Key}][{ctrl.dataName}]");
					//Morph_Util.Logger.LogDebug($"data: {cfg.defaults[category.Key][ctrl.dataName].Value.data}");

					controls.all[category.Key][ctrl.dataName] = cfg.defaults[category.Key][ctrl.dataName].Value.Clone();
					//controls.all[category.Key][ctrl.dataName].data *= .01f;
					//	Morph_Util.Logger.LogDebug($"\ncontrol set: {category.Key}\ncontrol name: {ctrl.Value}");
				}
			}

			ctrls1 = controls.Clone();
			controls.setIsMainControls = true;

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("dictionary has default values");

		}

		public void LateUpdate()
		{
			if(isDummy) return;

			if((!m_data1.abmx.isSplit || !m_data2.abmx.isSplit)
				&& isInitLoadFinished && BoneSplitCheck())
				MorphChangeUpdate();
		}


		/// <summary>
		/// Called whenever base character data needs to be updated for calculations
		/// </summary>
		/// <param name="currentGameMode">game mode state</param>
		/// <param name="abmxOnly">Only change ABMX data for current character (base character data is not changed)</param>
		public void OnCharaReload(GameMode currentGameMode)
		{
			if(isReloading || isDummy) return;
			isReloading = true;

			var boneCtrl = GetComponent<BoneController>();
			int val = (int)cfg.reloadTest.Value;
			var tmpCtrlName = "" + cfg.currentControlSetName.Value;

			//make sure to save current controls
			SoftSaveControls(true, false);

			//clear data 
			{
				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("clear data");
				m_data1.Clear();
				m_data2.Clear();
				//ctrls1 = null;
				morphEnable = morphEnableABMX = true;
				ctrls2 = null;
				m_extData = null;
				m_initalData.Clear();
				controls.Copy(ctrls1);//needs to be reset each load

			}

			//make sure to reset the current controls
			controls.currentSet = tmpCtrlName;

			#region Get Character Info

			//store picked character data
			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("replace data 1");


			m_data1.Copy(this); //get all character data!!!


			//store png data
			m_data1.main.pngData = ChaFileControl.pngData;
#if KOI_API
			m_data1.main.facePngData = ChaFileControl.facePngData;
#endif
			m_initalData.Copy(m_data1);

			if((MakerAPI.InsideMaker && isInitLoadFinished) || !MakerAPI.InsideMaker)//for the initial character in maker
			{
				MorphTargetUpdate();
				MorphChangeUpdate(initReset: true, updateValues: true, abmx: true);
			}

			#endregion


			ResetHeight();


			//post update 
			IEnumerator CoReloadComplete(int delayFrames)
			{
				if(cfg.debug.Value) Morph_Util.Logger.LogMessage("CoReload Started");

				isReloading = true;//just in case
				for(int a = -1; a < delayFrames; ++a)
					yield return null;


				MorphTargetUpdate();


				isInitLoadFinished = true;
				isReloading = false;
				for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
					StartCoroutine(CoMorphChangeUpdate(a + 1));

				if(IsUsingExtMorphData && cfg.loadInitMorphCharacter.Value)
				{
					var isCurData = LZ4MessagePackSerializer.Deserialize<bool>
					((byte[])m_extData.data[saveLoad.DataKeys[((int)LoadDataType.HoldsFigureData)]], CompositeResolver.Instance);


					if(isCurData)
						for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
							StartCoroutine(CoResetOriginalBody(
								(int)cfg.multiUpdateEnableTest.Value + a + 1, data: m_initalData));
				}

				if(m_extData != null && !MakerAPI.InsideMaker)
					CharaMorpher_Core.Logger
						.LogMessage("Character Morph Data found in this card!");


				if(cfg.debug.Value) Morph_Util.Logger.LogMessage("CoReload Completed");
				yield break;
			}
			StartCoroutine(CoReloadComplete(val));//I just need to do this stuff later
		}


		/// <summary>
		/// updates the morph target to a specified target if path has changed or card has been updated
		/// </summary>
		/// <param name="ctrl"></param>
		public void MorphTargetUpdate()
		{
			if(isDummy) return;

			//create path to morph target
			string path = Path.Combine(Morph_Util.MakeDirPath(cfg.charDir.Value), Morph_Util.MakeDirPath(cfg.imageName.Value));


			//load Ext. card data

			//Get referenced character data (only needs to be loaded once)

			if((File.Exists(path)) &&
				(!MorphTarget.initalize ||
				lastCharDir != path ||
				File.GetLastWriteTime(path).Ticks != lastDT.Ticks))
			{
				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Initializing secondary character");

				lastDT = File.GetLastWriteTime(path);
				lastCharDir = path;
				morphCharData = new MorphData();

				//initialize secondary model
				MorphTarget.initalize = true;

				//MorphTarget.extraCharacter?.gameObject?.SetActive(false);

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("load morph target");
				MorphTarget.chaFile.LoadCharaFile(path);

				morphCharData.Copy(this, true);
			}

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("replace data 2");

			ctrls2 = null;
			m_extData = this.LoadExtData(m_extData);


			if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Morph check status: {IsUsingExtMorphData}");
			if(!IsUsingExtMorphData)
				m_data2.Copy(morphCharData);
			//	this.LoadExtData();

			morphTex = IsUsingExtMorphData ? m_data2.main.pngData?.LoadTexture() ?? Texture2D.blackTexture : path.CreateTexture();
			//dim card image
			if(IsUsingExtMorphData)
			{
				var pix = morphTex.GetPixels();
				foreach(var i in pix)
					i.AlphaMultiplied(.5f);
				morphTex.SetPixels(pix);
				morphTex.Apply();
			}

			CharaMorpher_GUI.UpdateGUISelectList();
		}


		#region Character Updates

		/// <summary>
		/// Gets the slider data for each part of the body
		/// </summary>
		/// <param name="contain"></param>
		/// <param name="abmx"></param>
		/// <returns></returns>
		private MorphSliderData GetControlValue(string contain, bool abmx = false, bool overall = false, bool fullVal = false)
		{
			var tmp = controls.all[controls.currentSet].ToList();
			if(fullVal)
				tmp = controls.fullVal[controls.currentSet].ToList();
			if(overall)
				tmp = tmp.Where(p => p.Key.ToLower().Contains("overall")).ToList();

			return (abmx ?
				tmp.Find(m => m.Key.ToLower().Contains("abmx") && Regex.IsMatch(m.Key, contain, RegexOptions.IgnoreCase)) :
				tmp.Find(m => !m.Key.ToLower().Contains("abmx") && Regex.IsMatch(m.Key, contain, RegexOptions.IgnoreCase))).Value
				;
		}

		/// <summary>
		/// Makes sure both ABMX lists have the same data
		/// </summary>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		public void MergeABMXLists(MorphData data1 = null, MorphData data2 = null)
		{
			if(!ABMXDependency.InTargetVersionRange) return;

			var charaCtrl = ChaControl;
			var boneCtrl = charaCtrl.GetComponent<BoneController>();
			data1 = data1 ?? m_data1;
			data2 = data2 ?? m_data2;
			//add non-existent bones to other lists
			if(BoneSplitCheck(true))
			{

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("balancing bone lists...");

				//Body
				BoneModifierMatching(ref data1.abmx.body, ref data2.abmx.body);
				BoneModifierMatching(ref data2.abmx.body, ref data1.abmx.body);

				//Face
				BoneModifierMatching(ref data1.abmx.face, ref data2.abmx.face);
				BoneModifierMatching(ref data2.abmx.face, ref data1.abmx.face);

				//current body
				BoneModifierMatching(ref boneCtrl, data1.abmx.body);
				BoneModifierMatching(ref boneCtrl, data1.abmx.face);

				//sort list
				data1.abmx.body.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
				data2.abmx.body.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
				data1.abmx.face.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
				data2.abmx.face.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));

#if KOI_API
				charaCtrl.LateUpdateForce();
#endif
			}
		}

		/// <summary>
		/// Update bones/shapes whenever a change is made to the sliders and balances internal lists
		/// </summary>
		/// <param name="forceReset: ">reset regardless of other perimeters</param>
		public void MorphChangeUpdate(bool forceReset = false, bool initReset = false, bool updateValues = true, bool abmx = true)
		{
			if(isDummy) return;

			var currGameMode = KoikatuAPI.GetCurrentGameMode();

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"is data copied check?");
			if(m_data1?.main == null) return;

			MergeABMXLists(null, null);

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("update values check?");
			if(!updateValues) return;


			MorphValuesUpdate(forceReset || ResetCheck, initReset: initReset, abmx: abmx);
		}

		public void ResetOriginalShape(MorphData data = null)
		{

			if(data == null)
				data = m_initalData;

			MergeABMXLists(null, data);

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("mod enabled check?");
			if(!Enable) return;

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("not male in main game check?");
			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) && ChaControl.sex != 1/*(allowed in maker as of now)*/)
				return;

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("not male in maker check?");
			if(MakerAPI.InsideMaker && ChaControl.sex != 1
				&& !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Morph only character with save data in game check?");
			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) &&
				cfg.onlyMorphCharWithDataInGame.Value &&
				!IsUsingExtMorphData) return;

			ObscureUpdateValues(false, replace: true, mainData2: data?.main);
			MainUpdateValues(false, replace: true, mainData2: data?.main);
			AbmxUpdateValues(false, replace: true, abmxData2: data?.abmx);

			ChaControl.updateShape = true;//this should update the model better

			//Slider Defaults set
			if(MakerAPI.InsideMaker)
				SetDefaultSliders();

			//This may be needed (it is for keeping the character on the ground)
			if(!isReloading) ResetHeight();
		}

		Coroutine coTexUpdate = null;
		/// <summary>
		/// Update values for the entire body (use MorphChangeUpdate() instead to make sure lists are balanced)
		/// </summary>
		/// <param name="reset"></param>
		/// <param name="initReset"></param>
		/// <param name="abmx"></param>
		private void MorphValuesUpdate(bool reset, bool initReset = false, bool abmx = true)
		{
			var currGameMode = KoikatuAPI.GetCurrentGameMode();

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("not male in main game check?");
			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) && ChaControl.sex != 1/*(allowed in maker as of now)*/)
				return;

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("not male in maker check?");
			if(MakerAPI.InsideMaker && ChaControl.sex != 1
				&& !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Morph only character with save data in game check?");
			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) &&
				cfg.onlyMorphCharWithDataInGame.Value &&
				!IsUsingExtMorphData)
				reset = true;

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("All Checks passed?");

			if(cfg.debug.Value)
			{
				Morph_Util.Logger.LogDebug($"data 1 body bones: {m_data1.abmx.body.Count}");
				Morph_Util.Logger.LogDebug($"data 2 body bones: {m_data2.abmx.body.Count}");
				Morph_Util.Logger.LogDebug($"data 1 face bones: {m_data1.abmx.face.Count}");
				Morph_Util.Logger.LogDebug($"data 2 face bones: {m_data2.abmx.face.Count}");
				Morph_Util.Logger.LogDebug($"chara bones: {ChaControl?.GetComponent<BoneController>().GetAllModifiers().Count()}");
				Morph_Util.Logger.LogDebug($"body parts: {m_data1.main.custom.body.shapeValueBody.Length}");
				Morph_Util.Logger.LogDebug($"face parts: {m_data1.main.custom.face.shapeValueFace.Length}");
			}

			reset = initReset || reset;
			var chaCtrl = ChaControl;
			//float enable = (reset ? (initReset ? cfg.initialMorphBodyTest.Value : 0) : 1);

			if(cfg.debug.Value)
				Morph_Util.Logger.LogDebug($"setting obscure values: {controls.currentSet} {controls.all[controls.currentSet].Count}");

			bool charDisabled = !Enable;
			//update obscure values//
			ObscureUpdateValues(reset || charDisabled, initReset);

			//value update loops//
			if(cfg.debug.Value)
				Morph_Util.Logger.LogDebug($"setting Main values");

			//Main			 
			MainUpdateValues(reset || charDisabled, initReset);

			if(cfg.debug.Value)
				Morph_Util.Logger.LogDebug($"setting ABMX values");

			charDisabled |= !EnableABMX;
			//ABMX
			if(abmx)
				AbmxUpdateValues(reset || charDisabled, initReset);

			chaCtrl.updateShape = true;//this should update the model better

			//Slider Defaults set
			if(MakerAPI.InsideMaker)
				SetDefaultSliders();

			//This may be needed (it is for keeping the character on the ground)
			if(!isReloading) ResetHeight();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reset"></param>
		/// <param name="initReset"></param>
		/// <param name="replace"></param>
		/// <param name="mainData1"></param>
		/// <param name="mainData2"></param>
		private void ObscureUpdateValues(bool reset, bool initReset = false, bool replace = false, ChaFileControl mainData1 = null, ChaFileControl mainData2 = null)
		{
			var chaCtrl = ChaControl;
			float enable = (reset ? (initReset ? cfg.initialMorphBodyTest.Value : 0) : 1);

			mainData1 = mainData1 ?? m_data1?.main;
			mainData2 = mainData2 ?? m_data2?.main;

			{


				//not sure how to update this :\ (well it works so don't question it)
				chaCtrl.fileBody.areolaSize = Mathf.LerpUnclamped(mainData1.custom.body.areolaSize, mainData2.custom.body.areolaSize,
				replace ? 1f : (reset ? enable : enable * GetControlValue("body").data * GetControlValue("Boobs").data));

				chaCtrl.fileBody.bustSoftness = Mathf.LerpUnclamped(mainData1.custom.body.bustSoftness, mainData2.custom.body.bustSoftness,
				replace ? 1f : (reset ? enable : enable * GetControlValue("body").data * GetControlValue("Boob Phys.").data));

				chaCtrl.fileBody.bustWeight = Mathf.LerpUnclamped(mainData1.custom.body.bustWeight, mainData2.custom.body.bustWeight,
				replace ? 1f : (reset ? enable : enable * GetControlValue("body").data * GetControlValue("Boob Phys.").data));


				if(cfg.debug.Value)
					Morph_Util.Logger.LogDebug($"gets here");

				//Skin Colour
				bool newcol = false;
				var col1 = Color.LerpUnclamped(
#if KOI_API
					mainData1.custom.body.skinMainColor, mainData2.custom.body.skinMainColor,
#elif HONEY_API
					mainData1.custom.body.skinColor, mainData2.custom.body.skinColor,
#endif
			replace ? 1f : (reset ? enable : enable * GetControlValue("skin").data * GetControlValue("base skin").data));


				if(cfg.debug.Value)
					Morph_Util.Logger.LogDebug($"gets here");
#if KOI_API
				newcol |= chaCtrl.fileBody.skinMainColor != col1;
				chaCtrl.fileBody.skinMainColor = col1;
#elif HONEY_API
				newcol |= chaCtrl.fileBody.skinColor != col1;
				chaCtrl.fileBody.skinColor = col1;
#endif

				var col2 = Color.LerpUnclamped(mainData1.custom.body.sunburnColor, mainData2.custom.body.sunburnColor,
								replace ? 1f : (reset ? enable : enable * GetControlValue("skin").data * GetControlValue("sunburn").data));

				newcol |= chaCtrl.fileBody.sunburnColor != col2;
				chaCtrl.fileBody.sunburnColor = col2;

				if(cfg.debug.Value)
					Morph_Util.Logger.LogDebug($"gets here");

				//colour update
				if(isInitLoadFinished && newcol)
				{
					chaCtrl.AddUpdateCMBodyColorFlags
#if HONEY_API
						(inpBase: true, inpSunburn: true, inpPaint01: false, inpPaint02: false);
#elif KOI_API
					(inpBase: true, inpSub: true, inpSunburn: true, inpNail: true, inpPaint01: false, inpPaint02: false);
#endif

					chaCtrl.AddUpdateCMFaceColorFlags
						(true, false, false, false, false, false, false);

					if(!MakerAPI.InsideMaker)
					{

						chaCtrl.AddUpdateCMBodyTexFlags
#if HONEY_API
							(true, true, true, true);
#elif KOI_API
						(true, true, true, true, true);
#endif
						chaCtrl.AddUpdateCMFaceTexFlags
							(true, true, true, true, true, true, true);
					}


					//reset the textures in game
					IEnumerator UpdateTextures()
					{
						for(int a = -1; a < cfg.multiUpdateSliderTest.Value; ++a)
							yield return new WaitForEndOfFrame();

						chaCtrl.CreateBodyTexture();
						chaCtrl.CreateFaceTexture();

						yield break;
					}
					if(coTexUpdate != null)
						StopCoroutine(coTexUpdate);
					coTexUpdate = StartCoroutine(UpdateTextures());
				}


				//Voice
#if HS2
				chaCtrl.fileParam2.voiceRate = Mathf.Lerp(mainData1.parameter2.voiceRate, mainData2.parameter2.voiceRate,
				replace ? 1f : (reset ? enable : enable * GetControlValue("voice").data));
#endif

				chaCtrl.fileParam.voiceRate = Mathf.Lerp(mainData1.parameter.voiceRate, mainData2.parameter.voiceRate,
				replace ? 1f : (reset ? enable : enable * GetControlValue("voice").data));

				if(cfg.debug.Value)
				{

					Morph_Util.Logger.LogDebug($"data1   voice rate: {mainData1.parameter.voiceRate}");
					Morph_Util.Logger.LogDebug($"data2   voice rate: {mainData2.parameter.voiceRate}");
					Morph_Util.Logger.LogDebug($"current voice rate: {chaCtrl.fileParam.voiceRate}");

#if HS2
					Morph_Util.Logger.LogDebug($"data1   voice rate2: {mainData1.parameter2.voiceRate}");
					Morph_Util.Logger.LogDebug($"data2   voice rate2: {mainData2.parameter2.voiceRate}");
					Morph_Util.Logger.LogDebug($"current voice rate2: {chaCtrl.fileParam2.voiceRate}");
#endif
				}


			}

		}

		/// <summary>
		/// Updates main values independently
		/// </summary>
		/// <param name="reset"></param>
		/// <param name="initReset"></param>
		/// <param name="replace"></param>
		/// <param name="mainData1"></param>
		/// <param name="mainData2"></param>
		private void MainUpdateValues(bool reset, bool initReset = false, bool replace = false, ChaFileControl mainData1 = null, ChaFileControl mainData2 = null)
		{
			mainData1 = mainData1 ?? m_data1?.main;
			mainData2 = mainData2 ?? m_data2?.main;


			reset = initReset || reset;
			float enable;
			for(int a = 0; a < Mathf.Max(new float[]
			{
				mainData1.custom.body.shapeValueBody.Length,
				mainData1.custom.face.shapeValueFace.Length,
			});
			++a)
			{
				float result = 0;


				enable = (reset ? (initReset ? cfg.initialMorphBodyTest.Value : 0) : 1);
				//Body Shape
				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"updating body Shape");
				if(a < mainData1.custom.body.shapeValueBody.Length)
				{
					//Value Update
					{
						result = ChaControl.GetShapeBodyValue(a);

						float
							d1 = mainData1.custom.body.shapeValueBody[a],
							d2 = mainData2.custom.body.shapeValueBody[a];
						try
						{
							var bodyValue = GetControlValue("body", fullVal: initReset, overall: true);
							if(replace)
							{
								result = d2;
							}
							else
							if(cfg.headIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = bodyValue.data * GetControlValue("head", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("head", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.torsoIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = bodyValue.data * GetControlValue("torso", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("torso", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.buttIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = bodyValue.data * GetControlValue("butt", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("butt", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.legIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = bodyValue.data * GetControlValue("legs", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("legs", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.armIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = bodyValue.data * GetControlValue("arms", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("arms", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.brestIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = bodyValue.data * GetControlValue("boobs", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("boobs", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							{
								var val = bodyValue.data * GetControlValue("body other", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("body other", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
						}
						catch(Exception e)
						{
							Morph_Util.Logger.LogError($"This object is causing an error: {e}");
						}
					}

					//load values to character
					if(result != ChaControl.GetShapeBodyValue(a))
						ChaControl.SetShapeBodyValue(a, result);
				}

				enable = (reset ? (initReset ? cfg.initialMorphFaceTest.Value : 0) : 1);

				//Face Shape
				if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"updating face Shape");
				if(a < mainData1.custom.face.shapeValueFace.Length)
				{
					//Value Update
					{
						result = ChaControl.GetShapeFaceValue(a);

						float
							d1 = mainData1.custom.face.shapeValueFace[a],
							d2 = mainData2.custom.face.shapeValueFace[a];
						try
						{
							var faceValue = GetControlValue("face", fullVal: initReset, overall: true);

							if(replace)
							{
								result = d2;
							}
							else
							if(cfg.eyeIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = faceValue.data * GetControlValue("eyes", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("eyes", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.mouthIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = faceValue.data * GetControlValue("mouth", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("mouth", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.earIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = faceValue.data * GetControlValue("ears", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("ears", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							if(cfg.noseIndex.FindIndex(find => (find.Value == a)) >= 0)
							{
								var val = faceValue.data * GetControlValue("nose", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("nose", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
							else
							{
								var val = faceValue.data * GetControlValue("face other", fullVal: initReset).data;
								result = Mathf.LerpUnclamped(d1, d2,
								(reset ? enable : (val * (GetControlValue("face other", fullVal: initReset).calcType == MorphCalcType.QUADRATIC &&
								cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1))));
							}
						}
						catch(Exception e)
						{
							Morph_Util.Logger.LogError($"error:\n {e}");
						}
					}

					//load values to character
					if(result != ChaControl.GetShapeFaceValue(a))
						ChaControl.SetShapeFaceValue(a, result);
				}
			}
		}

		/// <summary>
		/// Updates ABMX values independently
		/// </summary>
		/// <param name="reset"></param>
		/// <param name="initReset"></param>
		private void AbmxUpdateValues(bool reset, bool initReset = false, bool replace = false, MorphData.AMBXSections abmxData1 = null, MorphData.AMBXSections abmxData2 = null)
		{
			if(!ABMXDependency.InTargetVersionRange) return;

			abmxData1 = abmxData1 ?? m_data1?.abmx;
			abmxData2 = abmxData2 ?? m_data2?.abmx;


			if(!abmxData1.isSplit || !abmxData2.isSplit) return;
			if(abmxData1.body.Count != abmxData2.body.Count ||
				abmxData1.face.Count != abmxData2.face.Count) return;

			var boneCtrl = ChaControl?.GetComponent<BoneController>();
			reset = initReset || reset;
			float enable;
			for(int a = 0; a < Mathf.Max(new float[]
				{
				abmxData1.body.Count, abmxData1.face.Count
				});
				++a)
			{
				#region Body
				enable = ((reset || !EnableABMX) ? (initReset ? cfg.initialMorphBodyTest.Value : 0) : 1);
				if(a < abmxData1.body.Count)
				{
					if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"looking for body values");

					var bone1 = abmxData1.body[a];
					var bone2 = abmxData2.body[a];
					var current = boneCtrl.GetAllModifiers().First((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));

					var modVal = new MorphSliderData();

					//remove L/R from bone name
					string content = bone1.BoneName.Trim().ToLower();

					string[] fingerNames = new[]
					{
#if HONEY_API
						//HS2
						"cf_J_Hand_Thumb",
						"cf_J_Hand_Index",
						"cf_J_Hand_Middle",
						"cf_J_Hand_Ring",
						"cf_J_Hand_Little",
#elif KOI_API
							
						//KK
                        "cf_j_thumb",
						"cf_j_index",
						"cf_j_middle",
						"cf_j_ring",
						"cf_j_little"
#endif
					};

					if(Array.FindIndex(fingerNames, (k) => content.Contains(k.Trim().ToLower())) >= 0)
						modVal = GetControlValue("hands", true, fullVal: initReset);
					else
					{

						string ending1 = "";
						string ending2 = "";
						int end = content.LastIndexOf("_");
						int end2 = -1;
						if(end >= 0)
						{
							ending1 = content.Substring(end);
							end2 = content.Substring(0, end).LastIndexOf("_");
						}
						if(end2 >= 0)
							ending2 = content.Substring(end - (end - (end2)) + 1);



						if(ending1 == "_l" || ending1 == "_r" || Regex.IsMatch(ending2 = ending2, @"l_\d\d") || Regex.IsMatch(ending2 = ending2, @"r_\d\d"))
							content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

#if KOI_API
						switch(boneDatabaseCategories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
						switch(boneDatabaseCategories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
#endif
						{
						case "torso":
							modVal = GetControlValue("Torso", true, fullVal: initReset);
							break;
						case "boobs":
							modVal = GetControlValue("Boobs", true, fullVal: initReset);
							break;
						case "butt":
							modVal = GetControlValue("Butt", true, fullVal: initReset);
							break;
						case "arms":
							modVal = GetControlValue("Arms", true, fullVal: initReset);
							break;
						case "hands":
							modVal = GetControlValue("Hands", true, fullVal: initReset);
							break;
						case "genitals":
							modVal = GetControlValue("Genitals", true, fullVal: initReset);
							break;
						case "legs":
							modVal = GetControlValue("Legs", true, fullVal: initReset);
							break;
						case "feet":
							modVal = GetControlValue("Feet", true, fullVal: initReset);
							break;

						default:
							modVal = GetControlValue("body other", true, fullVal: initReset);
							break;
						}
					}

					if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Morphing Bone...");
					if(replace)
						UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
							enable: 1, reset: reset);
					else
						UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
							sectVal: (cfg.linkOverallABMXSliders.Value ?
							GetControlValue("body", fullVal: initReset, overall: true).data : 1) *
							GetControlValue("Body", abmx: true, fullVal: initReset).data,
							enable: enable, reset: reset);

				}
				#endregion

				#region Head
				enable = ((reset || !EnableABMX) ? (initReset ? cfg.initialMorphFaceTest.Value : 0) : 1);
				if(a < abmxData1.face.Count)
				{
					if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"looking for face values");

					var bone1 = abmxData1.face[a];
					var bone2 = abmxData2.face[a];
					var current = boneCtrl.GetAllModifiers().First((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));

					var modVal = new MorphSliderData();

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



					if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
						content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));




#if KOI_API
					switch(boneDatabaseCategories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
					switch(boneDatabaseCategories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
#endif
					{

					case "eyes":
						modVal = GetControlValue("Eyes", true, fullVal: initReset);
						break;
					case "nose":
						modVal = GetControlValue("Nose", true, fullVal: initReset);
						break;
					case "mouth":
						modVal = GetControlValue("Mouth", true, fullVal: initReset);
						break;
					case "ears":
						modVal = GetControlValue("Ears", true, fullVal: initReset);
						break;
					case "hair":
						modVal = GetControlValue("Hair", true, fullVal: initReset);
						break;


					default:
						modVal = GetControlValue("head other", true, fullVal: initReset);
						break;
					}

					if(cfg.debug.Value) Morph_Util.Logger.LogDebug($"Morphing Bone...");
					if(replace)
						UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
							enable: 1, reset: reset);
					else
						UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
						sectVal: (cfg.linkOverallABMXSliders.Value ?
						GetControlValue("face", fullVal: initReset).data : 1) *
						GetControlValue("head", true, fullVal: initReset).data,
						enable: enable, reset: reset);
				}
				#endregion
			}


		}

		/// <summary>
		/// makes sure values are set based on internal values
		/// </summary>
		private void SetDefaultSliders()
		{
			var mkBase = MakerAPI.GetMakerBase();
			//	var bodycustum = CharaMorpher_GUI.bodyCustom;
			//	var facecustum = CharaMorpher_GUI.faceCustom;
			//	var boobcustum = CharaMorpher_GUI.boobCustom;

			if(mkBase && !isReloading)
			{
				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Resetting CVS Sliders");


				bodyCustom?.CalculateUI();
				faceCustom?.CalculateUI();
				boobCustom?.CalculateUI();
				charaCustom?.CalculateUI();

				mkBase.updateCvsChara = true;
#if HONEY_API
				mkBase.updateCvsBodyShapeWhole = true;
				mkBase.updateCvsFaceShapeWhole = true;

				mkBase.updateCvsBodyShapeBreast = true;
				//#else
				//				mkBase.updateCvsBodyShapeAll = true;
				//				mkBase.updateCvsFaceShapeAll = true;
				//				//mkBase.updateCvsBodyAll = true;
				//				//mkBase.updateCvsFaceAll = true;
				//				mkBase.updateCvsBreast = true;
#endif


			}
		}


		#endregion

		#region ABMX Modification
		/// <summary>
		/// Adds all bones from bone2 to bone1
		/// </summary>
		/// <param name="bone1"></param>
		/// <param name="bone2"></param>
		private void BoneModifierMatching(ref List<BoneModifier> bone1, ref List<BoneModifier> bone2)
		{
			if(!ABMXDependency.InTargetVersionRange) return;
			foreach(var bone in bone2)
			{
				string content = bone.BoneName.Trim().ToLower();
				if(bone1.FindIndex((k) => k.BoneName.Trim().ToLower() == content) < 0)
				{
					string name = bone.BoneName;
					BoneModifier nBone;

					bone1.Add(nBone = new BoneModifier(name, bone.BoneLocation));
					if(bone.IsCoordinateSpecific())
						nBone.MakeCoordinateSpecific(bone.CoordinateModifiers.Length);
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
			if(!ABMXDependency.InTargetVersionRange) return;
			foreach(var bone in bone2)
			{
				string content = bone.BoneName.Trim().ToLower();

				if((bone1.GetAllModifiers().Count((k) => k.BoneName.Trim().ToLower() == content) - 1) < 0)
				{
					string name = bone.BoneName;
					BoneModifier nBone = null;
					try
					{
						bone1.AddModifier(nBone = new BoneModifier(name, bone.BoneLocation));
					}
					catch { }
					if(bone.IsCoordinateSpecific())
						nBone?.MakeCoordinateSpecific(bone.CoordinateModifiers.Length);
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
		private void UpdateBoneModifier(ref BoneModifier current, BoneModifier bone1, BoneModifier bone2, MorphSliderData modVal, bool reset, float sectVal = 1, float enable = 1, int index = 0)
		{
			if(!ABMXDependency.InTargetVersionRange) return;
			try
			{
				var lerpVal = (reset ? enable : (
					enable * sectVal * (modVal?.data ?? 1f) *
					((modVal?.calcType ?? MorphCalcType.LINEAR) == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(modVal.data) : 1f)));

				int count = 0;//may use this method in other mods
				bool check = false;
				foreach(var mod in current?.CoordinateModifiers)
				{

					var inRange1 = count < bone1.CoordinateModifiers.Length;
					var inRange2 = count < bone2.CoordinateModifiers.Length;


					var pos = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange1 ? count : 0].PositionModifier,
																bone2.CoordinateModifiers[inRange2 ? count : 0].PositionModifier,
																lerpVal);

					check |= pos != mod.PositionModifier;

					var rot = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange1 ? count : 0].RotationModifier,
																bone2.CoordinateModifiers[inRange2 ? count : 0].RotationModifier,
																lerpVal);

					check |= rot != mod.RotationModifier;

					var scale = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange1 ? count : 0].ScaleModifier,
															bone2.CoordinateModifiers[inRange2 ? count : 0].ScaleModifier,
															lerpVal);

					check |= scale != mod.ScaleModifier;

					var len = Mathf.LerpUnclamped(bone1.CoordinateModifiers[inRange1 ? count : 0].LengthModifier,
															bone2.CoordinateModifiers[inRange2 ? count : 0].LengthModifier,
															lerpVal);

					check |= len != mod.LengthModifier;


					mod.PositionModifier = pos;
					mod.RotationModifier = rot;
					mod.ScaleModifier = scale;
					mod.LengthModifier = len;
					if(cfg.debug.Value)
					{
						if(count == 0)
						{
							Morph_Util.Logger.LogDebug($"~updated values~");
							Morph_Util.Logger.LogDebug($"lerp Value {index}: {enable * modVal.data}");
							Morph_Util.Logger.LogDebug($"{current.BoneName} modifiers!!");
							Morph_Util.Logger.LogDebug($"Body Bone 1 scale {index}: {bone1.CoordinateModifiers[count].ScaleModifier}");
							Morph_Util.Logger.LogDebug($"Body Bone 2 scale {index}: {bone2.CoordinateModifiers[count].ScaleModifier}");
							Morph_Util.Logger.LogDebug($"Result scale {index}: {mod.ScaleModifier}");
						}
					}

					++count;
				}


				var boneCtrl = GetComponent<BoneController>();
				if(check)
				{

#if HS2
					current.Apply(boneCtrl.CurrentCoordinate.Value, null);
#else
					current.Apply(boneCtrl.CurrentCoordinate.Value, null);
#endif
					boneCtrl.NeedsBaselineUpdate = true;//may be needed to update abmx sliders (unsure)
				}

			}
			catch(Exception e)
			{
				Morph_Util.Logger.LogError($"Error: {e.TargetSite} went boom... {e.Message}");
			}
		}
		#endregion

		#region CustomController Overrides
		bool initReset = true;//needed
		/// <inheritdoc/>
		protected override void OnReload(GameMode currentGameMode, bool keepState)
		{
			if(keepState) return;

			if(initReset && !isInitLoadFinished)
				initReset = isReloading = false;

			//for in game load correction
			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) && isInitLoadFinished)
				MorphChangeUpdate(forceReset: true);

			//if(!isReloading)

			//if(MakerAPI.InsideMaker || !initLoadFinished)
			OnCharaReload(currentGameMode);
		}

		/// <inheritdoc/>
		protected override void OnCardBeingSaved(GameMode currentGameMode)
		{
			if(cfg.enable.Value && cfg.saveExtData.Value)
				this.SaveExtData();
		}

		/// <inheritdoc/>
		protected override void OnDestroy()
		{
			//if(!dummy)
			//	MorphTarget.initalize = false;
			base.OnDestroy();
		}
		#endregion

		#region Coroutine Helpers
		public IEnumerator CoResetFace(int delayFrames, bool forceReset = false)
		{
			for(int a = 0; a < delayFrames; ++a)
				yield return null;

			ResetFace(forceReset);

			yield break;
		}

		public IEnumerator CoReloadChara()
		{
			for(int a = 0; a < 7; ++a)
				yield return null;

			OnCharaReload(KoikatuAPI.GetCurrentGameMode());

			yield break;
		}

		public IEnumerator CoMorphTargetUpdate(int delay = 10, bool updateValues = true, bool initReset = false)
		{
			for(int a = 0; a < delay; ++a)
				yield return null;

			MorphTargetUpdate();

			yield return null;

			if(isReloading) yield break;

			for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
			{
				if(isReloading)
					yield return new WaitWhile(() => isReloading);
				MorphChangeUpdate(updateValues: updateValues, initReset: initReset);
			}


			yield break;
		}

		public IEnumerator CoMorphChangeUpdate(int delay = 6, bool forceReset = false, bool initReset = false, bool forceChange = false)
		{
			for(int a = 0; a < delay; ++a)
				yield return null;

			if(!isReloading || forceChange)
			{
				MorphChangeUpdate(forceReset: forceReset, initReset: initReset);
			}
			else
			{
				if(isReloading)
					yield return new WaitWhile(() => isReloading);
				MorphChangeUpdate(forceReset: forceReset, initReset: initReset);
			}


			yield break;
		}

		public IEnumerator CoMorphAfterABMX(int delay = 5, bool forceReset = false, bool forceChange = false)
		{
			var boneCtrl = GetComponent<BoneController>();

			yield return new WaitWhile(() => boneCtrl.NeedsFullRefresh || boneCtrl.NeedsBaselineUpdate);

			if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Updating morph values after ABMX");

			yield return StartCoroutine(CoMorphChangeUpdate(delay, forceReset, forceChange: forceChange));

			yield break;
		}

		public IEnumerator CoResetOriginalBody(int delay, Coroutine co = null, MorphData data = null)
		{
			if(co != null)
			{
				yield return co;
				yield return null;
			}
			else
				for(int a = 0; a < delay; ++a) yield return null;


			ResetOriginalShape(data);


			yield break;
		}
		#endregion

		#region Misc.

		bool resettingHeihgt = false;
		/// <summary>
		/// Don't ask me why this works it just does
		/// </summary>
		internal void ResetHeight()
		{
			//reset the height using shoes (yes I beat them into the ground with their own shoes)
			if(resettingHeihgt) return;

			resettingHeihgt = true;
#if KOI_API
			var tmpstate1 = ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.shoes_inner];
			var tmpstate2 = ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.shoes_outer];
#else
			var tmpstate = ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.shoes];
#endif

#if KOI_API
			void heightReset(byte shoestate1, byte shoestate2)
#else
			void heightReset(byte shoestate)
#endif
			{

#if KOI_API
				ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes_inner, shoestate1);
				ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes_outer, shoestate2);
#else
				ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes, shoestate);
#endif
			}


#if KOI_API
			heightReset(1, 1);
#else
			heightReset(1);
#endif

#if KOI_API
			IEnumerator CoAfterReset(byte state1, byte state2)
#else
			IEnumerator CoAfterReset(byte state)
#endif
			{
				if(isReloading)
					for(int a = -1; a < (int)cfg.reloadTest.Value; ++a)
						yield return new WaitForEndOfFrame();
#if KOI_API
				heightReset(state1, state2);
#else
				heightReset(state);
#endif
				resettingHeihgt = false;
				yield break;
			}
#if KOI_API
			StartCoroutine(CoAfterReset(tmpstate1, tmpstate2));
#else
			StartCoroutine(CoAfterReset(tmpstate));
#endif
		}

		/// <summary>
		/// dumb fix for a dumb issue. seems legit.
		/// </summary>
		internal void ResetFace(bool forceReset = false)
		{

			//	if(ResetCheck() || forceReset) return;

			int val = 500;

			void Reset(int valu)
			{

				//for(int a = 0; a < m_data1.main.custom.face.shapeValueFace.Length; ++a)
				//	ChaControl.SetShapeBodyValue(a, ChaControl.GetShapeBodyValue(a) + value);
				//ChaControl.LateUpdateForce();




				//Use if prior don't work
				var
				tmp = GetControlValue("eyes");
				controls.all[controls.currentSet][tmp.dataName].SetData(tmp.data + valu);
				tmp = GetControlValue("mouth");
				controls.all[controls.currentSet][tmp.dataName].SetData(tmp.data + valu);
				tmp = GetControlValue("ears");
				controls.all[controls.currentSet][tmp.dataName].SetData(tmp.data + valu);
				tmp = GetControlValue("nose");
				controls.all[controls.currentSet][tmp.dataName].SetData(tmp.data + valu);
				tmp = GetControlValue("face other");
				controls.all[controls.currentSet][tmp.dataName].SetData(tmp.data + valu);



				MorphValuesUpdate(false, abmx: false);
				ChaControl.LateUpdateForce();
			}

			Reset(-val);
			Reset(val * 2);
			Reset(-val);
			if(ResetCheck || forceReset)
				MorphValuesUpdate(true, abmx: false);
		}

		/// <summary>
		/// checks if ABMX data has been split to body/head 
		/// </summary>
		/// <param name="onlyCheck"></param>
		/// <returns></returns>
		bool BoneSplitCheck(bool onlyCheck = false)
		{

			if(!onlyCheck && (!m_data1.abmx.isSplit || !m_data2.abmx.isSplit))
			{
				if(!m_data1.abmx.isSplit)
				{
					if(!m_data1.abmx.isLoaded)
						m_data1.abmx.Populate(this, false);
					m_data1.abmx.BoneSplit(this, ChaControl);
				}
				if(!m_data2.abmx.isSplit)
				{
					if(!m_data2.abmx.isLoaded)
						m_data2.abmx.Populate(this, true);
					m_data2.abmx.BoneSplit(this, ChaControl, true);

				}
			}
			return m_data1.abmx.isSplit && m_data2.abmx.isSplit;
		}

		public void SoftSaveControls(bool saveCMD, bool defaultSave = true)
		{
			if((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio)) return;

			//if(!ctrl)
			//	ctrl = GetFuncCtrlOfType<CharaMorpher_Controller>().FirstOrNull();

			//if(!ctrl) return;//return if ctrl is null

			if(saveCMD && isReloading)
				ctrls2 = controls.Clone();//needs to be done this way (to get initialized)

			var tmp = controls.Clone();

			if(defaultSave)
				LoadCurrentDefaultValues(false, false, false);

			var listCtrl =
			(((!MakerAPI.InsideMaker && !StudioAPI.InsideStudio) ?
			cfg.preferCardMorphDataGame.Value :
			cfg.preferCardMorphDataMaker.Value) && saveCMD) ?
			(ctrls2 ?? ctrls1) : ctrls1;

			listCtrl?.Copy(controls);

			if(defaultSave)
				controls = tmp;
		}

		#endregion

	}

	#region Extra Data
	public enum MorphCalcType : int
	{
		LINEAR,
		QUADRATIC
	}

	internal class MorphTarget
	{
		private static ChaControl _extraCharacter = null;
		private static BoneController _bonectrl = null;

		/// <summary>
		/// true: creates a new instance if one is not created. false: destroys the current instance
		/// </summary>
		public static bool initalize
		{
			set
			{
				if(value)
				{
					if(_extraCharacter == null)
					{

						Transform parent = null;
						parent = GetFuncCtrlOfType<CharaMorpher_Controller>()?.First()?.transform.parent;
						//_extraCharacter = new ChaControl();

						_extraCharacter =

#if HONEY_API
							Character.Instance.CreateChara(1, parent?.gameObject, -10);
#elif KK
							Character.Instance.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#elif KKS
							Character.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#endif

						if(!(_extraCharacter?.gameObject)) { _extraCharacter = null; return; }

						//remove character from internal list
#if KKS
						Character.DeleteChara(_extraCharacter, entryOnly: true);
#else
						Character.Instance?.DeleteChara(_extraCharacter, entryOnly: true);
#endif

						if(ABMXDependency.InTargetVersionRange)
							_bonectrl = _extraCharacter?.GetComponent<BoneController>();

						//This is needed so extracharacter is not immediately destroyed
						var ctrler = _extraCharacter?.GetComponent<CharaMorpher_Controller>();
						if(ctrler)
						{

							if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Destroying dummy chara controller");
							ctrler.isDummy = true;
							ctrler.enabled = false;
							GameObject.Destroy(ctrler);//change back to Destroy if issues arise
						}

						_extraCharacter.gameObject.SetActive(false);
						if(cfg.debug.Value) Morph_Util.Logger.LogDebug("created new Morph character instance");
					}

					return;
				}


				if(_bonectrl) _bonectrl.hideFlags = HideFlags.None;
				if(_bonectrl) GameObject.Destroy(_bonectrl);
				if(_extraCharacter) GameObject.Destroy(_extraCharacter?.gameObject);

				_extraCharacter = null;
			}
			get { return _extraCharacter != null; }
		}
		public static ChaControl extraCharacter { get => _extraCharacter; }

		public static ChaFileControl chaFile { get => extraCharacter?.chaFile; }
	}

	[Serializable]
	public class MorphData
	{
		[Serializable]
		public class AMBXSections
		{
			public List<BoneModifier> body = new List<BoneModifier>();
			public List<BoneModifier> face = new List<BoneModifier>();
			private bool m_isLoaded = false;
			private bool m_isSplit = false;

			public bool isLoaded { get => m_isLoaded; private set => m_isLoaded = value; }
			public bool isSplit { get => m_isSplit; private set => m_isSplit = value; }


			public void Populate(CharaMorpher_Controller morphControl, bool useTargetData = false)
			{
				if(!ABMXDependency.InTargetVersionRange) return;

				var boneCtrl = useTargetData ? MorphTarget.extraCharacter?.GetComponent<BoneController>() : morphControl?.GetComponent<BoneController>();
				var charaCtrl = morphControl?.ChaControl;

				if(isLoaded) return;
				//Store Bonemod Extended Data
				{//helps get rid of data sooner

					if(!boneCtrl) Morph_Util.Logger.LogDebug("Bone controller doesn't exist");
					if(!charaCtrl) Morph_Util.Logger.LogDebug("Character controller doesn't exist");

					//This is the second dumbest fix
					//(I was changing the player character's bones when this was true ¯\_(ツ)_/¯)
					var data = boneCtrl?.GetExtendedData(!useTargetData);

					var newModifiers = data.ReadBoneModifiers();
					//body bonemods on
					if(useTargetData || bodyBonemodTgl)
						body = new List<BoneModifier>(newModifiers);
					//face bonemods on
					if(useTargetData || faceBonemodTgl)
						face = new List<BoneModifier>(newModifiers);

					isLoaded = !!boneCtrl;//it can be shortened to just "boneCtrl" if I want
				}

				if(cfg.debug.Value)
				{
					if(useTargetData) Morph_Util.Logger.LogDebug("Character 2:");
					else Morph_Util.Logger.LogDebug("Character 1:");
					foreach(var part in body) Morph_Util.Logger.LogDebug("Bone: " + part.BoneName);
				}

				BoneSplit(morphControl, charaCtrl, useTargetData);
			}

			//split up body & head bones
			public void BoneSplit(CharaMorpher_Controller charaControl, ChaControl bodyCharaCtrl, bool useTargetData = false)
			{
				if(!ABMXDependency.InTargetVersionRange) return;

				var ChaControl = charaControl?.GetComponent<ChaControl>();
				var ChaFileControl = ChaControl?.chaFile;

				if(!bodyCharaCtrl?.objHeadBone) return;
				if(isSplit || !isLoaded) return;

				if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Splitting bones apart (this is gonna hurt)");


				var headRoot = bodyCharaCtrl.objHeadBone.transform.parent.parent;

				var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name)) { /*Additional*/headRoot.name };

				//Load Body
				if(useTargetData || bodyBonemodTgl)
					body.RemoveAll(x => headBones.Contains(x.BoneName));

				//Load face
				if(useTargetData || faceBonemodTgl)
				{
					var bodyBones = new HashSet<string>(bodyCharaCtrl.objTop.transform.
						GetComponentsInChildren<Transform>().Select(x => x.name).Except(headBones));
					face.RemoveAll(x => bodyBones.Contains(x.BoneName));
				}

				isSplit = true;
			}

			public void ForceSplitStatus(bool force = true) { isSplit = force; isLoaded = force; }


			public void Clear()
			{

				if(bodyBonemodTgl)
					body?.Clear();
				if(faceBonemodTgl)
					face?.Clear();



				isLoaded = false;
				isSplit = false;
			}

			public AMBXSections Clone()
			{
				return new AMBXSections()
				{
					body = new List<BoneModifier>(body ?? new List<BoneModifier>()),
					face = new List<BoneModifier>(face ?? new List<BoneModifier>()),

					m_isSplit = m_isSplit,
					m_isLoaded = m_isLoaded,
				};
			}
		}

		public ChaFileControl main = new ChaFileControl();
		public AMBXSections abmx = new AMBXSections();

		public void Clear()
		{
			main = new ChaFileControl();
			abmx.Clear();
		}

		public MorphData Clone()
		{
			var tmp = new ChaFileControl();
			try
			{
				tmp.CopyAll(main);

				tmp.pngData = main?.pngData?.ToArray();//copy
#if KOI_API
				tmp.facePngData = main?.facePngData?.ToArray();//copy
#endif
			}
			catch(Exception e) { Morph_Util.Logger.LogError("Could not copy character data:\n" + e); }

#if HONEY_API
			//CopyAll will not copy this data in hs2
			tmp.dataID = main.dataID;
#endif

			return new MorphData() { main = tmp, abmx = abmx.Clone() };
		}

		public bool Copy(MorphData data)
		{
			if(data == null) return false;

			var tmp = data.Clone();
			//Morph_Util.Logger.LogDebug($"Face Bones: \n[{string.Join(",\n ", tmp.abmx.face.Attempt((k) => k.BoneName + " : " + k.CoordinateModifiers[0].ScaleModifier.ToString()).ToArray())}]");
			//Morph_Util.Logger.LogDebug($"Body Bones: \n[{string.Join(",\n ", tmp.abmx.body.Attempt((k) => k.BoneName + " : " + k.CoordinateModifiers[0].ScaleModifier.ToString()).ToArray())}]");
			this.main = tmp.main;
			this.abmx = tmp.abmx;

			return true;
		}

		public bool Copy(CharaMorpher_Controller data, bool useTargetData = false)
		{
			try
			{

#if HONEY_API
				//CopyAll will not copy this data in hs2/AI
				main.dataID = useTargetData ? MorphTarget.chaFile.dataID : data.ChaControl.chaFile.dataID;
#endif

				main.CopyAll(useTargetData ? MorphTarget.chaFile : data.ChaFileControl);
				main.pngData = (useTargetData ? MorphTarget.chaFile.pngData :
					data.ChaFileControl.pngData)?.ToArray();
#if KOI_API
				main.facePngData = (useTargetData ? MorphTarget.chaFile.facePngData :
					data.ChaFileControl.facePngData)?.ToArray();
#endif
			}
			catch(Exception e) { Morph_Util.Logger.LogError("Could not copy character data:\n" + e); return false; }

			abmx.Populate(data, useTargetData);


			return true;
		}
	}

	[Serializable]
	public class MorphControls
	{
		Dictionary<string, Dictionary<string, MorphSliderData>> _all, _lastAll;
		public string currentSet { get; internal set; } = cfg.currentControlSetName.Value;
		public bool setIsMainControls { get; set; } = false;

		Coroutine post = null;


		public Dictionary<string, Dictionary<string, MorphSliderData>> all
		{
			get
			{
				if(_all == null)
				{
					_all = new Dictionary<string, Dictionary<string, MorphSliderData>>();
					_lastAll = new Dictionary<string, Dictionary<string, MorphSliderData>>();
				}

				IEnumerator CoPost()
				{
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value + 10; ++a)
						yield return null;

					try
					{
						bool Check()
						{
							if(_all.Count != _lastAll.Count)
								return true;

							if(!_all.TryGetValue(currentSet, out var tmp1)) return true;

							if(_all[currentSet].Count != _lastAll[currentSet].Count)
								return true;

							for(int a = 0; a < _all[currentSet].Count; ++a)
							{
								var name = _all[currentSet].Keys.ElementAt(a);
								if(_lastAll.ContainsKey(currentSet) && _lastAll[currentSet].ContainsKey(name))
								{
									//if(_all[currentSet].TryGetValue(_all[currentSet].Keys.ElementAt(a), out var tmp2))
									if(_all[currentSet][name].data !=
										_lastAll[currentSet][name].data)
										return true;
								}
								else return true;
							}

							return false;
						};

						if(_all == null) yield break;

						bool check = Check();
						_lastAll = _all.ToDictionary(k => k.Key, v => v.Value.ToDictionary(k => k.Key, v2 => v2.Value.Clone()));

						//	Morph_Util.Logger.LogInfo($"The change check returned: {check}");
						if(check)
							OnInternalSliderValueChange.Invoke(this);

					}
					catch(Exception e)
					{
						Morph_Util.Logger.LogError($"CoPost failed:\n{e}");
					}

					yield break;
				}

				if(!StudioAPI.InsideStudio)
				{
					if(post != null)
						Instance.StopCoroutine(post);

					if(setIsMainControls)
						post = Instance.StartCoroutine(CoPost());
				}

				return _all;
			}
			set
			{
				_all = value;
				if(_lastAll == null)
					_lastAll = new Dictionary<string, Dictionary<string, MorphSliderData>>();
			}
		}

		/// <summary>
		/// each value is set to one
		/// </summary>
		public Dictionary<string, Dictionary<string, MorphSliderData>> fullVal
		{
			get
			{
				var tmp = all.ToDictionary(curr => curr.Key, curr => curr.Value.ToDictionary(curr2 => curr2.Key, curr2 => curr2.Value.Clone()));
				for(int a = 0; a < tmp[currentSet].Count; ++a)
					tmp[currentSet][tmp[currentSet].Keys.ElementAt(a)].SetData(1f);
				return tmp;
			}
		}

		/// <summary>
		/// each value is set to zero
		/// </summary>
		public Dictionary<string, Dictionary<string, MorphSliderData>> noVal
		{
			get
			{
				var tmp = all.ToDictionary(curr => curr.Key, curr => curr.Value.ToDictionary(curr2 => curr2.Key, curr2 => curr2.Value.Clone()));
				for(int a = 0; a < tmp[currentSet].Count; ++a)
					tmp[currentSet][tmp[currentSet].Keys.ElementAt(a)].SetData(0f);
				return tmp;
			}
		}

		/// <summary>
		/// list of every control with an "overall" in the name
		/// </summary>
		public IEnumerable<KeyValuePair<string, MorphSliderData>> overall
		{
			get
			=> all[currentSet].Where((p) => Regex.IsMatch(p.Key, "overall", RegexOptions.IgnoreCase));
		}

		/// <summary>
		/// list of every control w/o an "overall" in the name
		/// </summary>
		public IEnumerable<KeyValuePair<string, MorphSliderData>> notOverall
		{
			get
			=> all[currentSet].Where((p) => !Regex.IsMatch(p.Key, "overall", RegexOptions.IgnoreCase));
		}

		public void Clear()
		{
			foreach(var item in _all)
				item.Value.Clear();
			_all.Clear();
			if(post != null)
				Instance.StopCoroutine(post);
		}

		public MorphControls Clone() =>
		 new MorphControls
		 {
			 _all = _all?.ToDictionary((x) => x.Key, (y) => y.Value.ToDictionary(x => x.Key, v => v.Value.Clone()))
			 ?? new Dictionary<string, Dictionary<string, MorphSliderData>>(),
			 _lastAll = _lastAll?.ToDictionary((x) => x.Key, (y) => y.Value.ToDictionary(x => x.Key, v => v.Value.Clone()))
			 ?? new Dictionary<string, Dictionary<string, MorphSliderData>>(),

			 currentSet = currentSet + "",
			 setIsMainControls = setIsMainControls,
		 };

		public bool Copy(MorphControls cpy)
		{
			if(cpy == null) return false;
			var tmp = cpy.Clone();
			_all = tmp._all;
			_lastAll = tmp._lastAll;
			currentSet = tmp.currentSet;

			//	Morph_Util.Logger.LogDebug($"Current Save: {currentSet}");
			//	Morph_Util.Logger.LogDebug($"List Names: [{string.Join(", ", all.Keys.ToArray())}]");
			//	Morph_Util.Logger.LogDebug($"List Counts: [{string.Join(", ", all.Values.Attempt((k) => k.Count.ToString()).ToArray())}]");

			return true;
		}
	}

	internal static class ABMXUtils
	{

		/// <summary>
		/// Taken from ABMX to get the data from card more easily 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		internal static List<BoneModifier> ReadBoneModifiers(this PluginData data)
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
						if(cfg.debug.Value) Morph_Util.Logger.LogDebug("[KKABMX] Loading legacy embedded ABM data");
						return ABMXOldDataConverterKoiAPI.MigrateOldExtData(data);
#endif

					default:
						throw new NotSupportedException($"[KKABMX] Save version {data.version} is not supported");
					}
				}
				catch(Exception ex)
				{
					if(cfg.debug.Value) Morph_Util.Logger.LogError("[KKABMX] Failed to load extended data - " + ex);
				}
			}
			return new List<BoneModifier>();
		}

	}

	/// <summary>
	/// Needed to copy this class from ABMX in case old card is loaded 
	/// (Taken directly from source: 
	/// https://github.com/ManlyMarco/ABMX/blob/v5.0.2/Shared_KKEC/Core/OldDataConverter.cs)
	/// </summary> 
	internal static class ABMXOldDataConverterKoiAPI
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
						Morph_Util.Logger.LogError($"ABMX: Failed to load legacy line \"{string.Join(",", singleEntry)}\" - {ex.Message}");
					}
				}

				if(coordinateModifiers.Count == 0)
					continue;

				const int kkCoordinateCount = 7;
				if(coordinateModifiers.Count > kkCoordinateCount)
					coordinateModifiers.RemoveRange(0, coordinateModifiers.Count - kkCoordinateCount);
				if(coordinateModifiers.Count > 1 && coordinateModifiers.Count < kkCoordinateCount)
					coordinateModifiers.RemoveRange(0, coordinateModifiers.Count - 1);

				results.Add(new BoneModifier(groupedBoneDataEntries.Key, BoneLocation.Unknown, coordinateModifiers.ToArray()));
			}

			return results;
		}
	}
	#endregion

}