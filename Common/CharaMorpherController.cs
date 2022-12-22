
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Text;
using System.Text.RegularExpressions;


using KKAPI;
using KKAPI.MainGame;
using KKAPI.Utilities;
using KKAPI.Chara;
using KKAPI.Maker;
using KKABMX.Core;
using ExtensibleSaveFormat;

using Manager;
using UniRx;

#if HONEY_API
using CharaCustom;
using AIChara;
#else
using ChaCustom;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.CharaMorpherController;


namespace Character_Morpher
{

	public class CharaMorpherController : CharaCustomFunctionController
	{

		private string MorphTargetLoc = "";
		private static MorphData charData = null;
		private static string lastCharDir = "";
		private static DateTime lastDT = new DateTime();

		internal MorphControls controls = new MorphControls();
		//internal static readonly MorphTarget morphTarget = new MorphTarget();
		private static bool m_faceBonemodTgl = true, m_bodyBonemodTgl = true;
		internal static bool faceBonemodTgl
		{
			get { if(MakerAPI.InsideMaker) return m_faceBonemodTgl; else return true; }
			set { m_faceBonemodTgl = value; }
		}
		internal static bool bodyBonemodTgl
		{
			get { if(MakerAPI.InsideMaker) return m_bodyBonemodTgl; else return true; }
			set { m_bodyBonemodTgl = value; }
		}

		public readonly MorphData m_data1 = new MorphData(), m_data2 = new MorphData();


		/// <summary>
		/// Called after the model has finished being loaded for the first time
		/// </summary>
		public bool initLoadFinished { get; private set; } = false;

		/// <summary>
		/// In the process of reloading. set to false after complete
		/// </summary>
		public bool reloading { get; internal set; } = true;

		/// <summary>
		/// makes sure most main functins don't run when creating template character
		/// </summary>
		public bool dummy { get; internal set; } = false;


#if KOI_API
		public static readonly List<KeyValuePair<string, string>> boneDatabaseCatagories = new List<KeyValuePair<string, string>>()
#else

		//this is a tuple list btw (of bones found in abmx mod and online... somewhere)
		public static readonly List<(string, string)> boneDatabaseCatagories = new List<(string, string)>()
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
		    new KeyValuePair<string, string>("cf_j_foot_L"       , "feet"),
			new KeyValuePair<string, string>("cf_j_leg03_L"      , "feet"),
			new KeyValuePair<string, string>("cf_s_leg03_L"      , "feet"),
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
		/*
		Coroutine coResetHeight = null;
		private IEnumerator CoResetHeight(int delayFrames = 5)
		{

			if(coResetHeight != null) StopCoroutine(coResetHeight);

			IEnumerator CoResetHeight(int delayrs)
			{
				for(int a = 0; a < delayrs; ++a) yield return null;

				if(reloading) yield return new WaitWhile(() => reloading);

				ResetHeight();

				yield break;
			}
			coResetHeight = StartCoroutine(CoResetHeight(delayFrames));
			yield break;
		}
		*/

		public IEnumerator CoABMXFullRefresh(int delayFrames = 5)
		{
			for(int a = 0; a < delayFrames; ++a)
				yield return null;


			var boneCtrl = GetComponent<BoneController>();
			if((boneCtrl?.NeedsFullRefresh ?? false) || (boneCtrl?.NeedsBaselineUpdate ?? false))
				yield return new WaitWhile(() => (boneCtrl?.NeedsFullRefresh ?? false) || (boneCtrl?.NeedsBaselineUpdate ?? false));

			if(reloading) yield break;
			//	boneCtrl = GetComponent<BoneController>();
			//	if(boneCtrl != null) boneCtrl.NeedsFullRefresh = true;

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

			for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
				MorphChangeUpdate(updateValues: updateValues, initReset: initReset);


			yield break;
		}

		//Coroutine coFullRefresh;
		public IEnumerator CoMorphChangeUpdate(int delay = 6, bool forceReset = false, bool initReset = false, bool forceChange = false)
		{
			for(int a = 0; a < delay; ++a)
				yield return null;

			if(!reloading || forceChange)
			{
				MorphChangeUpdate(forceReset: forceReset, initReset: initReset);
			}
			else
			{
				yield return new WaitWhile(() => reloading);

				MorphChangeUpdate(forceReset: forceReset, initReset: initReset);
			}


			yield break;
		}

		public IEnumerator CoMorphAfterABMX(int delay = 5, bool forcereset = false, bool forceChange = false)
		{
			var boneCtrl = GetComponent<BoneController>();

			yield return new WaitWhile(() => boneCtrl.NeedsFullRefresh || boneCtrl.NeedsBaselineUpdate);

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Updating morph values after ABMX");

			yield return StartCoroutine(CoMorphChangeUpdate(delay, forcereset, forceChange: forceChange));

			//	yield return StartCoroutine(CoResetHeight((int)cfg.multiUpdateSliderTest.Value));

			yield break;
		}

		//bool forcedReload = false;
		Coroutine coForceReload;
		/// <summary>
		/// This is jank and may not work
		/// </summary>
		internal void ForceCardReload()
		{

			MorphChangeUpdate(forceReset: true);
			IEnumerator CoRestore(int delay)
			{
				//		forcedReload = true;
				for(int count = 0; count < delay; ++count)
					yield return null;
				//copy the current status  
				m_data1.main.CopyStatus(ChaControl.fileStatus);

				//Reset original character data
				ChaControl.chaFile.CopyAll(m_data1.main);

				ChaControl.chaFile.SetCustomBytes(m_data1.main.GetCustomBytes(), ChaFileDefine.ChaFileCustomVersion);

				//	ChaControl.Reload(noChangeClothes: false);
				ChaControl.Reload();
				//ChaControl.chaFile.CopyStatus(m_data1.main.status.);




				//	forcedReload = false;
				CharaMorpher_Core.Logger.LogDebug("restored backup");
				yield break;
			}

			if(coForceReload != null)
				StopCoroutine(coForceReload);
			coForceReload = StartCoroutine(CoRestore(12));
		}

		public CharaMorpherController()
		{

			//	CharacterApi.CharacterReloaded += (s, e) => { reloading = false; if(!MakerAPI.InsideMaker) OnCharaReload(KoikatuAPI.GetCurrentGameMode()); };
		}
		protected override void Awake()
		{
			base.Awake();

			if(dummy) return;

			var core = Instance;

			foreach(var ctrl in core.controlCategories)
				controls.all[ctrl.Value] = Tuple.Create(cfg.defaults[ctrl.Key].Value * .01f, (MorphCalcType)cfg.defaultModes[ctrl.Key].Value);

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("dictionary has default values");



			//IEnumerator tmpFunc()
			//{
			//	yield return null;
			//	OnReload(KoikatuAPI.GetCurrentGameMode(), false);
			//	yield break;
			//}

			//StartCoroutine(tmpFunc());
		}

		public void LateUpdate()
		{
			if(dummy) return;

			if((!m_data1.abmx.isSplit || !m_data2.abmx.isSplit)
				&& initLoadFinished && boneSplitCheck())
				MorphChangeUpdate();
		}

		bool boneSplitCheck(bool onlycheck = false)
		{

			if(!onlycheck && (!m_data1.abmx.isSplit || !m_data2.abmx.isSplit))
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


		/// <summary>
		/// Called whenever base character data needs to be updated for calculations
		/// </summary>
		/// <param name="currentGameMode">game mode state</param>
		/// <param name="abmxOnly">Only change ABMX data for current character (base character data is not changed)</param>
		public void OnCharaReload(GameMode currentGameMode)
		{
			if(reloading || dummy) return;

			reloading = true;
			var boneCtrl = GetComponent<BoneController>();
			int val = (int)cfg.reloadTest.Value;

			{
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("clear data");
				m_data1.Clear();
				m_data2.Clear();
			}

			{

				//store picked character data
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("replace data 1");


				m_data1.Copy(this); //get all character data!!!

				//store png data
				m_data1.main.pngData = ChaFileControl.pngData;
#if KOI_API
				m_data1.main.facePngData = ChaFileControl.facePngData;
#endif

				if((MakerAPI.InsideMaker && initLoadFinished) || !MakerAPI.InsideMaker)//for the initial character in maker
				{
					MorphTargetUpdate();

					//for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
					MorphChangeUpdate(initReset: true, updateValues: true, abmx: false);
					//	MorphChangeUpdate(initReset: false, updateValues: true, abmx: false);
				}




				//if(MakerAPI.InsideMaker && !initLoadFinished)//for the initial character in maker
				//	ChaFileControl.CopyAll(m_data1.main);

				//	ChaControl.LateUpdateForce();
			}


			ResetHeight();



			//post update 
			IEnumerator CoReloadComplete(int delayFrames, BoneController _boneCtrl)
			{
				reloading = true;
				for(int a = 0; a < delayFrames; ++a)
					yield return null;


				MorphTargetUpdate();


				initLoadFinished = true;
				reloading = false;
				for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
					StartCoroutine(CoMorphChangeUpdate(a + 1));





				//MorphChangeUpdate();
				//StartCoroutine(CoResetFace((int)cfg.multiUpdateEnableTest.Value + 1));
				//StartCoroutine(CoResetHeight((int)cfg.multiUpdateEnableTest.Value + 1));
				//StartCoroutine(CoABMXFullRefresh((int)cfg.multiUpdateEnableTest.Value + 2));



				//	ChaControl.LateUpdateForce();
				//	boneCtrl.NeedsFullRefresh = true;

				yield break;
			}
			StartCoroutine(CoReloadComplete(val, boneCtrl));//I just need to do this stuff later
		}


		bool singleReset = true;
		/// <inheritdoc/>
		protected override void OnReload(GameMode currentGameMode, bool keepState)
		{
			if(keepState) return;


			if(singleReset && !initLoadFinished)
				singleReset = reloading = false;

			OnCharaReload(currentGameMode);

		}

		/// <summary>
		/// updates the morphtarget to a specified target if path has changed or card has been updated
		/// </summary>
		/// <param name="ctrl"></param>
		public void MorphTargetUpdate()
		{
			if(dummy) return;

			//create path to morph target
			string path = Path.Combine(MorphUtil.MakeDirPath(cfg.charDir.Value), MorphUtil.MakeDirPath(cfg.imageName.Value));


			//Get referenced character data (only needs to be loaded once)
			if(File.Exists(path))

				if(/*charData == null ||*/
					!MorphTarget.initalize ||
					lastCharDir != path ||
					File.GetLastWriteTime(path).Ticks != lastDT.Ticks)
				{


					CharaMorpher_Core.Logger.LogDebug("Initializing secondary character");
					(this).MorphTargetLoc = path;//TODO: get this in working order 

					lastDT = File.GetLastWriteTime(path);
					lastCharDir = path;
					charData = new MorphData();

					//initialize secondary model
					MorphTarget.initalize = true;

					MorphTarget.extraCharacter?.gameObject?.SetActive(false);

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("load morph target");
					MorphTarget.chaFile.LoadCharaFile(path, noLoadPng: true);

					charData.Copy(this, true);
				}


			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("replace data 2");
			(this).m_data2.Copy(charData);

		}

		/// <inheritdoc/>
		protected override void OnCardBeingSaved(GameMode currentGameMode)
		{
			//reset values to normal after saving
			if(cfg.enable.Value && !cfg.saveWithMorph.Value)
				for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
					StartCoroutine(CoMorphChangeUpdate(delay: (int)cfg.multiUpdateEnableTest.Value + a + 1));//turn the card back after(do not change)
																											 //StartCoroutine(CoResetFace((int)cfg.multiUpdateEnableTest.Value));
																											 //	StartCoroutine(CoResetHeight((int)cfg.multiUpdateEnableTest.Value));
		}

		/// <inheritdoc/> 
		protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate) { }

		/// <summary>
		/// Taken from ABMX to get the data from card more easily 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
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
						if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("[KKABMX] Loading legacy embedded ABM data");
						return ABMXOldDataConverter.MigrateOldExtData(data);
#endif

					default:
						throw new NotSupportedException($"[KKABMX] Save version {data.version} is not supported");
					}
				}
				catch(Exception ex)
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogError("[KKABMX] Failed to load extended data - " + ex);
				}
			}
			return new List<BoneModifier>();
		}

		internal bool ResetCheck()
		{
			bool reset = !cfg.enable.Value && !reloading;
			return KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame ?
					(reset || !cfg.enableInGame.Value) : reset;

		}

		/// <summary>
		/// Update bones/shapes whenever a change is made to the sliders
		/// </summary>
		/// <param name="forceReset: ">reset regardless of other perimeters</param>
		public void MorphChangeUpdate(bool forceReset = false, bool initReset = false, bool updateValues = true, bool abmx = true)
		{
			if(dummy) return;

			var currGameMode = KoikatuAPI.GetCurrentGameMode();

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"is data copied check?");
			if(m_data1?.main == null) return;

			var charaCtrl = ChaControl;
			var boneCtrl = charaCtrl.GetComponent<BoneController>();

			#region Merge results

			//add non-existent bones to other lists
			if(boneSplitCheck(true))
			{

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("balancing bone lists...");

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

#if KOI_API
				charaCtrl.LateUpdateForce();
#endif
			}

			#endregion

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("update values check?");
			if(!updateValues) return;


			MorphValuesUpdate(forceReset || ResetCheck(), initReset: initReset, abmx: abmx);
			//if(!reloading)
			//	ResetFace();//may work ¯\_(ツ)_/¯
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="contain"></param>
		/// <param name="abmx"></param>
		/// <returns></returns>
		private KeyValuePair<string, Tuple<float, MorphCalcType>> GetControlValue(string contain, bool abmx = false, bool overall = false, bool fullVal = false)
		{
			var tmp = controls.all.ToList();
			if(fullVal)
				tmp = controls.fullVal.ToList();



			return (abmx ?
				tmp.Find(m => m.Key.ToLower().Contains("abmx") && Regex.IsMatch(m.Key, contain, RegexOptions.IgnoreCase)) :
				tmp.Find(m => !m.Key.ToLower().Contains("abmx") && Regex.IsMatch(m.Key, contain, RegexOptions.IgnoreCase)))
				;

		}

		//MotionIK motion = null;
		private void MorphValuesUpdate(bool reset, bool initReset = false, bool abmx = true)
		{
			var currGameMode = KoikatuAPI.GetCurrentGameMode();



			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("not male in main game check?");
			if(!MakerAPI.InsideMaker && ChaControl.sex != 1/*(allowed in maker as of now)*/)
				return;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("not male in maker check?");
			if(MakerAPI.InsideMaker && ChaControl.sex != 1
				&& !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker


			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("All Checks passed?");



			reset = initReset || reset;

			//var cfg = CharaMorpher_Core.cfg;
			var charaCtrl = ChaControl;
			var boneCtrl = charaCtrl.GetComponent<BoneController>();
			float enable = (reset ? 0 : 1);






			//charaCtrl.LateUpdateForce();


			//update obscure values//
			{

				//not sure how to update this :\ (well it works so don't question it)
				charaCtrl.fileBody.areolaSize = Mathf.LerpUnclamped(m_data1.main.custom.body.areolaSize, m_data2.main.custom.body.areolaSize,
					enable * GetControlValue("body").Value.Item1 * GetControlValue("Boobs").Value.Item1);

				charaCtrl.fileBody.bustSoftness = Mathf.LerpUnclamped(m_data1.main.custom.body.bustSoftness, m_data2.main.custom.body.bustSoftness,
					enable * GetControlValue("body").Value.Item1 * GetControlValue("Boob Phys.").Value.Item1);

				charaCtrl.fileBody.bustWeight = Mathf.LerpUnclamped(m_data1.main.custom.body.bustWeight, m_data2.main.custom.body.bustWeight,
					enable * GetControlValue("body").Value.Item1 * GetControlValue("Boob Phys.").Value.Item1);

				//ChaControl.updateBustSize =
				//ChaControl.resetDynamicBoneAll =
				//ChaControl.reSetupDynamicBoneBust = true;

				//Skin Colour
				bool newcol = false;
				var col1 = Color.LerpUnclamped(
#if KOI_API
					m_data1.main.custom.body.skinMainColor, m_data2.main.custom.body.skinMainColor,
#elif HONEY_API
					m_data1.main.custom.body.skinColor, m_data2.main.custom.body.skinColor,
#endif
									enable * GetControlValue("skin").Value.Item1 * GetControlValue("base skin").Value.Item1);


#if KOI_API
				newcol |= charaCtrl.fileBody.skinMainColor != col1;
				charaCtrl.fileBody.skinMainColor = col1;
#elif HONEY_API
				newcol |= charaCtrl.fileBody.skinColor != col1;
				charaCtrl.fileBody.skinColor = col1;
#endif

				var col2 = Color.LerpUnclamped(m_data1.main.custom.body.sunburnColor, m_data2.main.custom.body.sunburnColor,
									enable * GetControlValue("skin").Value.Item1 * GetControlValue("sunburn").Value.Item1);

				newcol |= charaCtrl.fileBody.sunburnColor != col2;
				charaCtrl.fileBody.sunburnColor = col2;

				//colour update
				if(initLoadFinished && newcol)
				{
					charaCtrl.AddUpdateCMBodyColorFlags
#if HONEY_API
						(true, true, true, true);
#elif KOI_API
					(true, true, true, true, true, true);
#endif

					charaCtrl.AddUpdateCMFaceColorFlags
						(true, true, true, true, true, true, true);

					if(!MakerAPI.InsideMaker)
					{

						charaCtrl.AddUpdateCMBodyTexFlags
#if HONEY_API
							(true, true, true, true);
#elif KOI_API
						(true, true, true, true, true);
#endif
						charaCtrl.AddUpdateCMFaceTexFlags
							(true, true, true, true, true, true, true);
					}


					//reset the textures in game
					charaCtrl.CreateBodyTexture();
					charaCtrl.CreateFaceTexture();
				}


				//Voice
#if HS2
				charaCtrl.fileParam2.voiceRate = Mathf.Lerp(m_data1.main.parameter2.voiceRate, m_data2.main.parameter2.voiceRate,
					enable * GetControlValue("voice").Value.Item1);
#endif

				charaCtrl.fileParam.voiceRate = Mathf.Lerp(m_data1.main.parameter.voiceRate, m_data2.main.parameter.voiceRate,
					enable * GetControlValue("voice").Value.Item1);

				if(cfg.debug.Value)
				{

					CharaMorpher_Core.Logger.LogDebug($"data1   voice rate: {m_data1.main.parameter.voiceRate}");
					CharaMorpher_Core.Logger.LogDebug($"data2   voice rate: {m_data2.main.parameter.voiceRate}");
					CharaMorpher_Core.Logger.LogDebug($"current voice rate: {charaCtrl.fileParam.voiceRate}");

#if HS2
					CharaMorpher_Core.Logger.LogDebug($"data1   voice rate2: {m_data1.main.parameter2.voiceRate}");
					CharaMorpher_Core.Logger.LogDebug($"data2   voice rate2: {m_data2.main.parameter2.voiceRate}");
					CharaMorpher_Core.Logger.LogDebug($"current voice rate2: {charaCtrl.fileParam2.voiceRate}");
#endif
				}


			}

			if(cfg.debug.Value)
			{

				CharaMorpher_Core.Logger.LogDebug($"data 1 body bones: {m_data1.abmx.body.Count}");
				CharaMorpher_Core.Logger.LogDebug($"data 2 body bones: {m_data2.abmx.body.Count}");
				CharaMorpher_Core.Logger.LogDebug($"data 1 face bones: {m_data1.abmx.face.Count}");
				CharaMorpher_Core.Logger.LogDebug($"data 2 face bones: {m_data2.abmx.face.Count}");
				CharaMorpher_Core.Logger.LogDebug($"chara bones: {boneCtrl.GetAllModifiers().ToList().Count}");
				CharaMorpher_Core.Logger.LogDebug($"body parts: {m_data1.main.custom.body.shapeValueBody.Length}");
				CharaMorpher_Core.Logger.LogDebug($"face parts: {m_data1.main.custom.face.shapeValueFace.Length}");
			}


			//value update loops//

			//Main
			for(int a = 0; a < Mathf.Max(new float[]
			{
				m_data1.main.custom.body.shapeValueBody.Length,
				m_data1.main.custom.face.shapeValueFace.Length,
			});
			++a)
			{
				float result = 0;




				enable = (reset ? (initReset ? cfg.initialMorphBodyTest.Value : 0) : 1);
				//Body Shape
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"updating body Shape");
				if(a < m_data1.main.custom.body.shapeValueBody.Length)
				{
					//Value Update
					{
						float
							d1 = m_data1.main.custom.body.shapeValueBody[a],
							d2 = m_data2.main.custom.body.shapeValueBody[a];

						if(cfg.headIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("head", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("head", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						if(cfg.torsoIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("torso", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("torso", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						if(cfg.buttIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("butt", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("butt", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						if(cfg.legIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("legs", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("legs", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						if(cfg.armIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("arms", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("arms", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						if(cfg.brestIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("boobs", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("boobs", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						{
							var val = GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("body other", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("body other", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
					}

					//load values to character
					//	charaCtrl.fileCustom.body.shapeValueBody[a] = result;
					if(result != charaCtrl.GetShapeBodyValue(a))
						charaCtrl.SetShapeBodyValue(a, result);
				}

				enable = (reset ? (initReset ? cfg.initialMorphFaceTest.Value : 0) : 1);
				//Face Shape
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"updating face Shape");
				if(a < m_data1.main.custom.face.shapeValueFace.Length)
				{
					//Value Update
					{
						float
							d1 = m_data1.main.custom.face.shapeValueFace[a],
							d2 = m_data2.main.custom.face.shapeValueFace[a];

						if(cfg.eyeIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("face", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("eyes", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("eyes", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						 if(cfg.mouthIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("face", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("mouth", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("mouth", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						  if(cfg.earIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("face", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("ears", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("ears", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						 if(cfg.noseIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							var val = GetControlValue("face", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("nose", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("nose", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
						else
						{
							var val = GetControlValue("face", fullVal: initReset, overall: true).Value.Item1 * GetControlValue("face other", fullVal: initReset).Value.Item1;
							result = Mathf.LerpUnclamped(d1, d2,
								enable * val * (GetControlValue("face other", fullVal: initReset).Value.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(val) : 1));
						}
					}

					//load values to character

					//charaCtrl.fileCustom.face.shapeValueFace[a] = result;
					if(result != charaCtrl.GetShapeBodyValue(a))
						charaCtrl.SetShapeFaceValue(a, result);
				}
			}

			//ABMX
			if(abmx)
				AbmxSettings(reset, initReset, boneCtrl);

			charaCtrl.updateShape = true;//this should update the model better

			//Slider Defaults set
			if(MakerAPI.InsideMaker)
				SetDefaultSliders();


			//charaCtrl.LateUpdateForce();


			//ResetHeight();


		}

		/// <summary>
		/// Don't ask me why this works it just does
		/// </summary>
		internal void ResetHeight()
		{
			//reset the height using shoes

#if KOI_API
			var tmpstate1 = ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.shoes_inner];
			var tmpstate2 = ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.shoes_outer];
			ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes_inner, tmpstate1);
			ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes_outer, tmpstate2);
#else
			var tmpstate = ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.shoes];
			//	ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes, (byte)(tmpstate ? 1 : 0));
#endif

			//	ChaControl.LateUpdateForce();
#if KOI_API
			void heightReset(byte shoestate1, byte shoestate2)
#else
			void heightReset(byte shoestate)
#endif
			{
				//for(int a = 0; a < 1; ++a)
				//	yield return null;


#if KOI_API
				ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes_inner, shoestate1);
				ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes_outer, shoestate2);
#else
				ChaControl.SetClothesState((int)ChaFileDefine.ClothesKind.shoes, shoestate);
#endif

				//ChaControl.LateUpdateForce();
				//	yield break;
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
				for(int a = -1; a < (int)cfg.reloadTest.Value; ++a)
					yield return null;
#if KOI_API
				heightReset(state1, state2);
#else
				heightReset(state);
#endif
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
				//	ChaControl.SetShapeBodyValue(a, ChaControl.GetShapeBodyValue(a) + valu);
				//ChaControl.LateUpdateForce();




				//Use if prior don't work
				var
				tmp = GetControlValue("eyes");
				controls.all[tmp.Key] = Tuple.Create(tmp.Value.Item1 + valu, tmp.Value.Item2);
				tmp = GetControlValue("mouth");
				controls.all[tmp.Key] = Tuple.Create(tmp.Value.Item1 + valu, tmp.Value.Item2);
				tmp = GetControlValue("ears");
				controls.all[tmp.Key] = Tuple.Create(tmp.Value.Item1 + valu, tmp.Value.Item2);
				tmp = GetControlValue("nose");
				controls.all[tmp.Key] = Tuple.Create(tmp.Value.Item1 + valu, tmp.Value.Item2);
				tmp = GetControlValue("face other");
				controls.all[tmp.Key] = Tuple.Create(tmp.Value.Item1 + valu, tmp.Value.Item2);



				MorphValuesUpdate(false, abmx: false);
				ChaControl.LateUpdateForce();
			}

			Reset(-val);
			Reset(val * 2);
			Reset(-val);
			if(ResetCheck() || forceReset)
				MorphValuesUpdate(true, abmx: false);

		}

		public IEnumerator CoResetFace(int delayFrames, bool forceReset = false)
		{
			for(int a = 0; a < delayFrames; ++a)
				yield return null;

			ResetFace(forceReset);

			yield break;
		}

		public void AbmxSettings(bool reset, bool initReset, BoneController boneCtrl)
		{
			if(!m_data1.abmx.isSplit || !m_data2.abmx.isSplit) return;

			float enable;
			for(int a = 0; a < Mathf.Max(new float[]
				{
				m_data1.abmx.body.Count, m_data1.abmx.face.Count
				});
				++a)
			{
				//float result = 0;


				#region ABMX

				enable = ((reset || !cfg.enableABMX.Value) ? (initReset ? cfg.initialMorphBodyTest.Value : 0) : 1);
				//Body
				if(a < m_data1.abmx.body.Count)
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"looking for body values");

					var bone1 = m_data1.abmx.body[a];
					var bone2 = m_data2.abmx.body[a];
					var current = boneCtrl.GetAllModifiers().First((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));

					var modVal = Tuple.Create(0f, MorphCalcType.LINEAR);

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

					if(Array.FindIndex(fingerNames, (k) => content.Contains(k.Trim().ToLower())) >= 0)
						modVal = GetControlValue("hands", true, fullVal: initReset).Value;
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



						if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
							content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

#if KOI_API
						switch(boneDatabaseCatagories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
						switch(boneDatabaseCatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
#endif
						{
						case "torso":
							modVal = GetControlValue("Torso", true, fullVal: initReset).Value;
							break;
						case "boobs":
							modVal = GetControlValue("Boobs", true, fullVal: initReset).Value;
							break;
						case "butt":
							modVal = GetControlValue("Butt", true, fullVal: initReset).Value;
							break;
						case "arms":
							modVal = GetControlValue("Arms", true, fullVal: initReset).Value;
							break;
						case "hands":
							modVal = GetControlValue("Hands", true, fullVal: initReset).Value;
							break;
						case "genitals":
							modVal = GetControlValue("Genitals", true, fullVal: initReset).Value;
							break;
						case "legs":
							modVal = GetControlValue("Legs", true, fullVal: initReset).Value;
							break;
						case "feet":
							modVal = GetControlValue("Feet", true, fullVal: initReset).Value;
							break;

						default:
							modVal = GetControlValue("body other", true, fullVal: initReset).Value;
							break;
						}
					}

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
					UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
						sectVal: (cfg.linkOverallABMXSliders.Value ?
						GetControlValue("body", fullVal: initReset, overall: true).Value.Item1 : 1) *
						GetControlValue("Body", abmx: true, fullVal: initReset).Value.Item1,
						enable: enable);
				}

				enable = ((reset || !cfg.enableABMX.Value) ? (initReset ? cfg.initialMorphFaceTest.Value : 0) : 1);
				//face
				if(a < m_data1.abmx.face.Count)
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"looking for face values");

					var bone1 = m_data1.abmx.face[a];
					var bone2 = m_data2.abmx.face[a];
					var current = boneCtrl.GetAllModifiers().First((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));

					var modVal = Tuple.Create(0f, MorphCalcType.LINEAR);

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
					switch(boneDatabaseCatagories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
					switch(boneDatabaseCatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
#endif
					{

					case "eyes":
						modVal = GetControlValue("Eyes", true, fullVal: initReset).Value;
						break;
					case "nose":
						modVal = GetControlValue("Nose", true, fullVal: initReset).Value;
						break;
					case "mouth":
						modVal = GetControlValue("Mouth", true, fullVal: initReset).Value;
						break;
					case "ears":
						modVal = GetControlValue("Ears", true, fullVal: initReset).Value;
						break;
					case "hair":
						modVal = GetControlValue("Hair", true, fullVal: initReset).Value;
						break;


					default:
						modVal = GetControlValue("head other", true, fullVal: initReset).Value;
						break;
					}

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
					UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
						sectVal: (cfg.linkOverallABMXSliders.Value ?
						GetControlValue("face", fullVal: initReset).Value.Item1 : 1) *
						GetControlValue("head", true, fullVal: initReset).Value.Item1,
						enable: enable);
				}
				#endregion

			}
			//	boneCtrl.NeedsFullRefresh = true;

		}


		private void SetDefaultSliders()
		{
			var mkBase = MakerAPI.GetMakerBase();
			var bodycustum = CharaMorpherGUI.bodyCustom;
			var facecustum = CharaMorpherGUI.faceCustom;
			var boobcustum = CharaMorpherGUI.boobCustom;

			if(mkBase && !reloading)
			{


				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Resetting CVS Sliders");
				bodycustum?.CalculateUI();
				facecustum?.CalculateUI();
				boobcustum?.CalculateUI();

				mkBase.updateCvsChara = true;
#if HONEY_API
				mkBase.updateCvsBodyShapeBreast = true;
#endif


				KKABMX.GUI.KKABMX_GUI.SpawnedSliders.Last().Sliders.Last().Value = KKABMX.GUI.KKABMX_GUI.SpawnedSliders.Last().Sliders.Last().DefaultValue;
			}
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
					BoneModifier nBone;

					bone1.Add(nBone = new BoneModifier(name, BoneLocation.Unknown));
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

			foreach(var bone in bone2)
			{
				string content = bone.BoneName.Trim().ToLower();

				if((bone1.GetAllModifiers().Count((k) => k.BoneName.Trim().ToLower() == content) - 1) < 0)
				{
					string name = bone.BoneName;
					BoneModifier nBone;
					bone1.AddModifier(nBone = new BoneModifier(name, BoneLocation.Unknown));
					if(bone.IsCoordinateSpecific())
						nBone.MakeCoordinateSpecific(bone.CoordinateModifiers.Length);
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
		private void UpdateBoneModifier(ref BoneModifier current, BoneModifier bone1, BoneModifier bone2, Tuple<float, MorphCalcType> modVal, float sectVal = 1, float enable = 1, int index = 0)
		{
			try
			{
				var lerpVal = Mathf.Clamp(enable, 0, 1) *
					sectVal * modVal.Item1 *
					(modVal.Item2 == MorphCalcType.QUADRATIC && cfg.enableCalcTypes.Value ? Mathf.Abs(modVal.Item1) : 1f);

				int count = 0;//may use this in other mods
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
						//   CharaMorpher_Core.Logger.LogDebug($"updated values");
						if(count == 0)
						{

							CharaMorpher_Core.Logger.LogDebug($"lerp Value {index}: {enable * modVal.Item1}");
							CharaMorpher_Core.Logger.LogDebug($"{current.BoneName} modifiers!!");
							CharaMorpher_Core.Logger.LogDebug($"Body Bone 1 scale {index}: {bone1.CoordinateModifiers[count].ScaleModifier}");
							CharaMorpher_Core.Logger.LogDebug($"Body Bone 2 scale {index}: {bone2.CoordinateModifiers[count].ScaleModifier}");
							CharaMorpher_Core.Logger.LogDebug($"Result scale {index}: {mod.ScaleModifier}");
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
					boneCtrl.NeedsBaselineUpdate = true;//may be needed to update abmx sliders
				}

			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.LogError($"Error: {e.TargetSite} went boom... {e.Message}");
			}

		}

		protected override void OnDestroy()
		{
			if(!dummy)
				MorphTarget.initalize = false;
			base.OnDestroy();
		}

	}

	public enum MorphCalcType : int
	{
		LINEAR,
		QUADRATIC
	}

	internal class MorphTarget
	{
		private static ChaControl _extraCharacter = null;
		private static BoneController _bonectrl = null;

		public static bool initalize
		{
			set
			{
				if(value)
				{
					if(_extraCharacter == null)
					{

						Transform parent = null;
						parent = MorphUtil.GetFuncCtrlOfType<CharaMorpherController>().First()?.transform.parent ?? null;
						_extraCharacter = new ChaControl();

						_extraCharacter =

#if HONEY_API
							Character.Instance.CreateChara(1, parent?.gameObject, -10);
#elif KK
							Character.Instance.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#elif KKS
							Character.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#endif

						if(!_extraCharacter.gameObject) { _extraCharacter = null; return; }

						//remove character from internal list
#if KKS
						Character.DeleteChara(_extraCharacter, entryOnly: true);
#else
						Character.Instance?.DeleteChara(_extraCharacter, entryOnly: true);
#endif

						_bonectrl = _extraCharacter?.GetComponent<BoneController>();

						//This is needed so extracharacter is not imidiately destroyed
						var ctrler = _extraCharacter?.GetComponent<CharaMorpherController>();
						if(ctrler)
						{

							CharaMorpher_Core.Logger.LogDebug("Destroying dummy chara controller");
							ctrler.dummy = true;
							ctrler.enabled = false;
							GameObject.DestroyImmediate(ctrler);//change back to destroy if issues arise
						}

						_extraCharacter.gameObject.SetActive(false);
						CharaMorpher_Core.Logger.LogDebug("created new Morph character instance");
					}

					if(_bonectrl) _bonectrl.hideFlags = HideFlags.HideAndDontSave;

					return;
				}


				if(_bonectrl) _bonectrl.hideFlags = HideFlags.None;
				if(_extraCharacter) GameObject.Destroy(_extraCharacter?.gameObject);

				_extraCharacter = null;
			}
			get { return _extraCharacter != null; }
		}
		public static ChaControl extraCharacter { get => _extraCharacter; }

		public static ChaFileControl chaFile { get { return extraCharacter?.chaFile; } }
	}

	public class MorphData
	{
		public class AMBXSections
		{
			public List<BoneModifier> body = new List<BoneModifier>();
			public List<BoneModifier> face = new List<BoneModifier>();


			public bool isLoaded { get; private set; } = false;
			public bool isSplit { get; private set; } = false;


			public void Populate(CharaMorpherController morphControl, bool morph = false)
			{

				var boneCtrl = morph ? MorphTarget.extraCharacter.GetComponent<BoneController>() : morphControl.GetComponent<BoneController>();
				var charaCtrl = morphControl.ChaControl;

				if(isLoaded) return;
				//Store Bonemod Extended Data
				{//helps get rid of data sooner

					if(!boneCtrl) CharaMorpher_Core.Logger.LogDebug("Bone controller don't exist");
					if(!morphControl.ChaControl) CharaMorpher_Core.Logger.LogDebug("Character controller don't exist");

					//This is the second dumbest fix
					//(I was changing the player character's bones when this was true ¯\_(ツ)_/¯)
					var data = boneCtrl?.GetExtendedData(!morph);

					var newModifiers = ReadBoneModifiers(data);
					if(morph || bodyBonemodTgl)
						body = new List<BoneModifier>(newModifiers);
					if(morph || faceBonemodTgl)
						face = new List<BoneModifier>(newModifiers);
					isLoaded = true;
				}

				if(cfg.debug.Value)
				{
					if(morph) CharaMorpher_Core.Logger.LogDebug("Character 2:");
					else CharaMorpher_Core.Logger.LogDebug("Character 1:");
					foreach(var part in body) CharaMorpher_Core.Logger.LogDebug("Bone: " + part.BoneName);
				}


				BoneSplit(morphControl, charaCtrl, morph);

			}

			//split up body & head bones
			public void BoneSplit(CharaMorpherController charaControl, ChaControl charaCtrl, bool morph = false)
			{
				var ChaControl = charaControl.GetComponent<ChaControl>();
				var ChaFileControl = ChaControl.chaFile;

				if(!charaCtrl.objHeadBone) return;
				if(isSplit || !isLoaded) return;


				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Splitting bones apart");

				var headRoot = charaCtrl.objHeadBone.transform.parent.parent;

				var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name)) { /*Additional*/headRoot.name };

				//Load Body
				if(morph || bodyBonemodTgl)
					body.RemoveAll(x => headBones.Contains(x.BoneName));

				//Load face
				if(morph || faceBonemodTgl)
				{
					var bodyBones = new HashSet<string>(charaCtrl.objTop.transform.
						GetComponentsInChildren<Transform>().Select(x => x.name).Except(headBones));
					face.RemoveAll(x => bodyBones.Contains(x.BoneName));
				}

				isSplit = true;
			}

			public void ResetSplitStatus() { isSplit = false; isLoaded = false; }


			public void Clear()
			{

				if(bodyBonemodTgl)
					body?.Clear();
				if(faceBonemodTgl)
					face?.Clear();



				isLoaded = false;
				isSplit = false;
			}

			public AMBXSections Copy()
			{
				return new AMBXSections()
				{
					body = new List<BoneModifier>(body ?? new List<BoneModifier>()),
					face = new List<BoneModifier>(face ?? new List<BoneModifier>()),

					isSplit = isSplit,
					isLoaded = isLoaded,
				};
			}
		}

		public ChaFile main = new ChaFile();
		public AMBXSections abmx = new AMBXSections();


		public void Clear()
		{

			main = new ChaFile();
			abmx.Clear();
		}

		public MorphData Clone()
		{
			var tmp = new ChaFile();
			try
			{
				tmp.CopyAll(main);
			}
			catch { }
#if HONEY_API
			//CopyAll will not copy this data in hs2
			tmp.dataID = main.dataID;
#endif

			return new MorphData() { main = tmp, abmx = abmx.Copy() };
		}

		public void Copy(MorphData data)
		{
			if(data == null) return;

			try
			{
				main.CopyAll(data.main);
			}
			catch { }
			abmx = data.abmx.Copy();

		}

		public void Copy(CharaMorpherController data, bool morph = false)
		{

#if HONEY_API
			//CopyAll will not copy this data in hs2
			main.dataID = morph ? MorphTarget.chaFile.dataID : data.ChaControl.chaFile.dataID;
#endif

			try
			{
				main.CopyAll(morph ? MorphTarget.chaFile : data.ChaFileControl);
			}
			catch { CharaMorpher_Core.Logger.LogDebug("Could not copy character data"); }

			abmx.Populate(data, morph);
		}
	}

	internal class MorphControls
	{
		Dictionary<string, Tuple<float, MorphCalcType>> _all, _lastAll;

		Coroutine post;
		public Dictionary<string, Tuple<float, MorphCalcType>> all
		{
			get
			{
				if(_all == null)
				{
					_all = new Dictionary<string, Tuple<float, MorphCalcType>>();
					_lastAll = new Dictionary<string, Tuple<float, MorphCalcType>>();
				}

				//var ctrl = this;
				IEnumerator CoPost()
				{
					for(int a = -1; a < cfg.multiUpdateEnableTest.Value; ++a)
						yield return null;


					bool Check()
					{
						if(_all.Count != _lastAll.Count)
							return false;

						for(int a = 0; a < _all.Count; ++a)
							if(_all[_all.Keys.ElementAt(a)].Item1 != _lastAll[_lastAll.Keys.ElementAt(a)].Item1)
								return true;


						return false;
					}

					if(Check())
						OnSliderValueChange.Invoke();

					_lastAll = new Dictionary<string, Tuple<float, MorphCalcType>>(_all);
				}

				if(post != null)
					Instance.StopCoroutine(post);

				post = Instance.StartCoroutine(CoPost());
				return _all;
			}
			set { _all = value; }
		}

		/// <summary>
		/// each value is set to one
		/// </summary>
		public Dictionary<string, Tuple<float, MorphCalcType>> fullVal
		{
			get
			{
				var tmp = all.ToDictionary(curr => curr.Key, curr => curr.Value);
				for(int a = 0; a < tmp.Count; ++a)
					tmp[tmp.Keys.ElementAt(a)] = Tuple.Create(1f, tmp[tmp.Keys.ElementAt(a)].Item2);
				return tmp;
			}
		}

		/// <summary>
		/// list of every control with an "overall" name
		/// </summary>
		public IEnumerable<KeyValuePair<string, Tuple<float, MorphCalcType>>> overall
		{
			get
			=> all.Where((p) => Regex.IsMatch(p.Key, "overall", RegexOptions.IgnoreCase));
		}

		/// <summary>
		/// list of every control w/o an "overall" name
		/// </summary>
		public IEnumerable<KeyValuePair<string, Tuple<float, MorphCalcType>>> notOverall
		{
			get
			=> all.Where((p) => !Regex.IsMatch(p.Key, "overall", RegexOptions.IgnoreCase));
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
						CharaMorpher_Core.Logger.LogError($"ABMX: Failed to load legacy line \"{string.Join(",", singleEntry)}\" - {ex.Message}");
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
}
