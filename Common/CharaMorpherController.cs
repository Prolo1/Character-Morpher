
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using KKAPI;
using KKAPI.Chara;
#if HS2
using AIChara;
#endif


using KKABMX.Core;
using ExtensibleSaveFormat;

using UnityEngine;
using UniRx;

namespace CharaMorpher
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
                        var data = boneCtrl.GetExtendedData();
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


                        var bodyBones = new HashSet<string>(charaCtrl.objBodyBone.transform.parent.parent.GetComponentsInChildren<Transform>().Select(x => x.name).Except(headBones));
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
                    body.Clear();
                    face.Clear();
                    other.Clear();
                }
                public AMBXSections Copy()
                {
                    return new AMBXSections()
                    {
                        body = new List<BoneModifier>(body),
                        face = new List<BoneModifier>(face),
                        other = new List<BoneModifier>(other)
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

            public MorphData Copy()
            {
                var tmp = new ChaFile();
                tmp.CopyAll(main);
                return new MorphData() { main = tmp, abmx = abmx.Copy() };
            }

            public void Copy(MorphData data)
            {
                main.CopyAll(data.main);
                abmx = data.abmx.Copy();
            }
        }
        public struct MorphControls
        {
            //Main
            public float body;
            public float head;
            public float boob;
            public float torso;
            public float arm;
            public float butt;
            public float leg;

            public float face;
            public float eyes;
            public float mouth;
            public float ear;

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

        private static MorphData charData = null;
        private static string lastCharDir = "";
        private static DateTime lastDT = new DateTime();

        private readonly MorphData m_data1 = new MorphData(), m_data2 = new MorphData();
        static int morphindex = 0;//get defaults from config
        public static MorphControls controls = new MorphControls()
        {

            //Main
            body = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            head = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            boob = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            butt = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            torso = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            arm = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            leg = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,

            face = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
            ear = CharaMorpher_Core.Instance.cfg.defaults[morphindex++].Value * .01f,
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

        /// <summary>
        /// Called after the model has been updated for the first time
        /// </summary>
        public bool initLoadFinished { get; private set; } = false;

        /// <summary>
        /// In the process of reloading. set to false after complete
        /// </summary>
        public bool reloading { get; private set; } = false;

        //this is a tuple list btw (of bones found in abmx)
        public static readonly List<(string, string)> bonecatagories =
             new List<(string, string)>()
#if KKSS || KK
        #region KKBones
             {
             //ABMX
        
                 //other head
                ("cf_J_NoseBase_rx"  , "nose"    ),
                ("cf_J_Nose_tip"     , "nose"    ),
                ("cf_J_NoseBridge_rx", "nose"    ),
                ("cf_J_NoseBridge_ty", "nose"    ),
                ("cf_J_megane_rx_ear", ""    ),

        //Head;
                ("cf_J_FaceBase"     , ""     ),
                ("cf_s_head"         , ""     ),
                ("cf_J_FaceUp_ty"    , ""     ),
                ("cf_J_FaceUp_tz"    , ""     ),
                ("cf_J_FaceLow_sx"   , ""     ),
                ("cf_J_FaceLow_tz"   , ""     ),
                ("cf_hit_head"       , ""     ),
                ("cf_J_Chin_Base"    , ""     ),
                ("cf_J_ChinLow"      , ""     ),
                ("cf_J_ChinTip_Base" , ""     ),
                ("cf_J_CheekUpBase"  , ""     ),
                ("cf_J_CheekUp2_L"   , ""     ),
                ("cf_J_CheekUp_s_L"  , ""     ),
                ("cf_J_CheekLow_s_L" , ""     ),
                                                                
        //Mouth;
                ("cf_J_MouthBase_rx" ,"mouth" ),
                ("cf_J_MouthBase_ty" ,"mouth" ),
                ("cf_J_Mouth_L"      ,"mouth" ),
                ("cf_J_Mouthup"      ,"mouth" ),
                ("cf_J_MouthLow"     ,"mouth" ),
                ("cf_J_MouthCavity"  ,"mouth" ),

                                                                
        //Ears;
                ("cf_J_EarBase_ry_L" , "ears" ),
                ("cf_J_EarUp_L"      , "ears" ),
                ("cf_J_EarLow_L"     , "ears" ),

        //Eyes;
                ("cf_J_Mayu_L"       , "eyes"),
                ("cf_J_MayuMid_s_L"  , "eyes"),
                ("cf_J_MayuTip_s_L"  , "eyes"),
                ("cf_J_Eye_tz"       , "eyes"),
                ("cf_J_Eye_rz_L"     , "eyes"),
                ("cf_J_Eye_tx_L"     , "eyes"),
                ("cf_J_Eye01_s_L"    , "eyes"),
                ("cf_J_Eye02_s_L"    , "eyes"),
                ("cf_J_Eye03_s_L"    , "eyes"),
                ("cf_J_Eye04_s_L"    , "eyes"),
                ("cf_J_Eye05_s_L"    , "eyes"),
                ("cf_J_Eye06_s_L"    , "eyes"),
                ("cf_J_Eye07_s_L"    , "eyes"),
                ("cf_J_Eye08_s_L"    , "eyes"),
                                                                
                                                                




                                                           

                //other body
        //Body;
                ("cf_n_height"       , "Body"                   ),
                ("cf_s_neck"         , "Neck"                   ),
                ("cf_d_sk_top"       , "Whole Skirt"            ),
                ("cf_d_sk_00_00"     , "Skirt Front"            ),
                ("cf_d_sk_07_00"     , "Skirt Front Sides"      ),
                ("cf_d_sk_06_00"     , "Skirt Sides"            ),
                ("cf_d_sk_05_00"     , "Skirt Back Sides"       ),
                ("cf_d_sk_04_00"     , "Skirt Back"             ),
                                                                
        //Boobs;
                ("cf_d_bust01_L"     , "boobs"   ),
                ("cf_d_bust02_L"     , "boobs"   ),
                ("cf_d_bust03_L"     , "boobs"   ),
                ("cf_s_bust00_L"     , "boobs"   ),
                ("cf_s_bust01_L"     , "boobs"   ),
                ("cf_s_bust02_L"     , "boobs"   ),
                ("cf_s_bust03_L"     , "boobs"   ),
                ("cf_hit_bust02_L"   , "boobs"   ),
                ("cf_s_bnip01_L"     , "boobs"   ),
                ("cf_s_bnip025_L"    , "boobs"   ),
                ("cf_d_bnip01_L"     , "boobs"   ),
                ("cf_s_bnip02_L"     , "boobs"   ),
                ("cf_s_bnipacc_L"    , "boobs"   ),
                                                                
                                                                
        //Torso;
                ("cf_s_spine03"      , "torso"  ),
                ("cf_s_spine02"      , "torso"  ),
                ("cf_s_spine01"      , "torso"  ),
                ("cf_hit_spine01"    , "torso"  ),
                ("cf_hit_spine02_L"  , "torso"  ),
                ("cf_hit_berry"      , "torso"  ),
                ("cf_hit_waist_L"    , "torso"  ),
                ("cf_j_spine01"      , "torso"  ),
                ("cf_j_spine02"      , "torso"  ),
                ("cf_j_spine03"      , "torso"  ),
                ("cf_s_waist01"      , "torso"  ),
                ("cf_s_waist02"      , "torso"  ),
                                                                
        //Butt;
                ("cf_s_siri_L"       , "butt"          ),
                ("cf_hit_siri_L"     , "butt"          ),
                                                                
                                                                
        //Arms;
                ("cf_s_shoulder02_L" , "arms"  ),
                ("cf_hit_shoulder_L" , "arms"  ),
                ("cf_j_shoulder_L"   , "arms"  ),
                ("cf_j_arm00_L"      , "arms"  ),
                ("cf_s_arm01_L"      , "arms"  ),
                ("cf_s_arm02_L"      , "arms"  ),
                ("cf_s_arm03_L"      , "arms"  ),
                ("cf_j_forearm01_L"  , "arms"  ),
                ("cf_s_forearm01_L"  , "arms"  ),
                ("cf_s_forearm02_L"  , "arms"  ),
                ("cf_s_wrist_L"      , "arms"  ),

        //Hands;
                ("cf_j_hand_L"       , "hands"        ),
                ("cf_s_hand_L"       , "hands"        ),
                ("cf_hit_arm_L"      , "hands"        ),
                                                               
        //Legs;
                ("cf_j_waist01"      , "legs"  ),
                ("cf_j_waist02"      , "legs"  ),
                ("cf_j_thigh00_L"    , "legs"  ),
                ("cf_s_thigh01_L"    , "legs"  ),
                ("cf_s_thigh02_L"    , "legs"  ),
                ("cf_s_thigh03_L"    , "legs"  ),
                ("cf_hit_thigh01_L"  , "legs"  ),
                ("cf_hit_thigh02_L"  , "legs"  ),
                ("cf_j_leg01_L"      , "legs"  ),
                ("cf_s_leg01_L"      , "legs"  ),
                ("cf_s_leg02_L"      , "legs"  ),
                ("cf_s_leg03_L"      , "legs"  ),

        //Feet;
                ("cf_j_foot_L"       , "feet"          ),
                ("cf_j_leg03_L"      , "feet"          ),
                ("cf_j_toes_L"       , "feet"          ),
                                                                
                                                                
        //Genitals;
                ("cf_d_kokan"        , "genitals"           ),
                ("cf_j_kokan"        , "genitals"           ),
                ("cm_J_dan100_00"    , "genitals"           ),
                ("cm_J_dan109_00"    , "genitals"           ),
                ("cm_J_dan_f_L"      , "genitals"           ),
                ("cf_j_ana"          , "genitals"           ),
             }
             #endregion
#elif HS2 || AI
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



        void CharaReloaded(object o, CharaReloadEventArgs a)
        {
            CharaMorpherController ctrl = a.ReloadedCharacter.GetComponent<CharaMorpherController>();

            //initialLoad = true;


#if KKSS

            if(!initLoadFinished)
                ctrl.CurrentCoordinate.Subscribe((type) => { StartCoroutine(CoMorphUpdate()); });
#endif
            reloading = true;
            CharaMorpher_Core.Logger.LogDebug("Reloading Character");
            StartCoroutine(ctrl.CoMorphReload());

            // initialLoad = true;
            // ctrl.UpdateMorphValues(false);

        }

        /// <summary>
        /// made for internal use
        /// </summary>
        public void resetLastCharacter()
        {

        }

        protected override void Awake()
        {

            KKAPI.Chara.CharacterApi.CharacterReloaded += CharaReloaded;

            //Make sure to call base version
            base.Awake();
        }


        IEnumerator CoMorphReload()
        {
            for(int a = 0; a < 6; ++a)
                yield return new WaitForEndOfFrame();

            reloading = true;
            CharaMorpher_Core.Logger.LogDebug("Reloading After character loaded");
            OnCharaReload(KoikatuAPI.GetCurrentGameMode());

            CharaMorpher_Core.Logger.LogDebug("Morphing model...");
            MorphChangeUpdate();


            reloading = false;
            initLoadFinished = true;
        }

        IEnumerator CoMorphUpdate()
        {
            for(int a = 0; a < 6; ++a)
                yield return new WaitForEndOfFrame();

            MorphChangeUpdate();

        }

        protected override void OnDestroy()
        {
            KKAPI.Chara.CharacterApi.CharacterReloaded -= CharaReloaded;
            base.OnDestroy();
        }

        string MakeDirPath(string dir)
        {
            dir = dir.Replace('\\', '/');
            dir = dir.Replace("//", "/");
            if((dir.LastIndexOf('.') <= dir.LastIndexOf('/'))
                && dir.Last() != '/')
                dir += '/';

            return dir;
        }

        ///<inheritdoc/>
        public void OnCharaReload(GameMode currentGameMode)
        {

            var cfg = CharaMorpher_Core.Instance.cfg;
            var boneCtrl = ChaControl.GetComponent<BoneController>();

            //clear original data
            CharaMorpher_Core.Logger.LogDebug("clear data");
            m_data1.Clear();
            m_data2.Clear();

            //store picked character data
            CharaMorpher_Core.Logger.LogDebug("replace data 1");
            m_data1.main.CopyAll(ChaControl.chaFile);//get all character data!!!

#if HS2 || AI
            //CopyAll will not copy this data in hs2
            m_data1.main.dataID = ChaControl.chaFile.dataID;
#endif

            m_data1.abmx.Populate(this);


            string path = MakeDirPath(cfg.charDir.Value) + cfg.imageName.Value;
            // CharaMorpher_Core.Logger.LogDebug($"image path: {path}");

            //Get referenced character data (only needs to be loaded once)
            if(charData == null ||
                lastCharDir != path ||
                System.IO.File.GetLastWriteTime(path).Ticks != lastDT.Ticks)
            {
                lastDT = System.IO.File.GetLastWriteTime(path);
                lastCharDir = path;
                charData = new MorphData();

                CharaMorpher_Core.Logger.LogDebug("load morph target");
                ChaFileControl.LoadCharaFile(path, 255/*female; 0 male*/);

                CharaMorpher_Core.Logger.LogDebug("copy morph target");
                charData.main.CopyAll(ChaControl.chaFile);
                charData.abmx.Populate(this);//Store Bonemod Extended Data

                CharaMorpher_Core.Logger.LogDebug("reset original data");
                //Reset original character data
                ChaControl.chaFile.CopyAll(m_data1.main);

#if HS2 || AI
                //CopyAll will not copy this data in hs2
                ChaControl.chaFile.dataID = m_data1.main.dataID;
#endif
            }

            if(ChaControl.sex != 1/*could allow it with both genders later*/)
                return;

            CharaMorpher_Core.Logger.LogDebug("replace data 2");
            m_data2.Copy(charData);


            //CharaMorpher_Core.Logger.LogDebug("Morphing model...");
            ////Update the model
            MorphChangeUpdate(true);
        }

        ///<inheritdoc/>
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            StartCoroutine(CoMorphUpdate());
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
        {
            base.OnCoordinateBeingLoaded(coordinate);

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
#if KK || EC || KKSS
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
            if(m_data1.main != null)
            {
#if HS2
                string storedID = m_data1.main.dataID, cardID = ChaControl.chaFile.dataID;
#else
                string storedID = m_data1.main.about.dataID, cardID = ChaControl.chaFile.about.dataID;
#endif
                CharaMorpher_Core.Logger.LogDebug($"file is: {cardID}");
                CharaMorpher_Core.Logger.LogDebug($"stored file is: {storedID}");


                if(cardID == null || cardID != storedID) return;
            }
            else return;

            if(ChaControl.sex != 1/*could allow it with both genders later*/)
                return;

            var cfg = CharaMorpher_Core.Instance.cfg;
            var charaCtrl = ChaControl;
            var boneCtrl = charaCtrl.GetComponent<BoneController>();


            //Merge results
            {

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
            }

            bool reset = !cfg.enable.Value;
            reset = KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame ? reset || !cfg.enableInGame.Value : reset;
            UpdateMorphValues(forceReset ? true : reset);
        }

        public void UpdateMorphValues(bool reset)
        {
            var cfg = CharaMorpher_Core.Instance.cfg;
            var charaCtrl = ChaControl;
            var boneCtrl = charaCtrl.GetComponent<BoneController>();

            float enable = reset ? 0 : 1;

            //update obscure values
            {
                //not sure how to update this :\
                charaCtrl.fileBody.areolaSize = (m_data1.main.custom.body.areolaSize +
                            enable * controls.body * controls.boob * (m_data2.main.custom.body.areolaSize - m_data1.main.custom.body.areolaSize));

                charaCtrl.fileBody.bustSoftness = (m_data1.main.custom.body.bustSoftness +
                            enable * controls.body * controls.boob * (m_data2.main.custom.body.bustSoftness - m_data1.main.custom.body.bustSoftness));

                charaCtrl.fileBody.bustWeight = (m_data1.main.custom.body.bustWeight +
                            enable * controls.body * controls.boob * (m_data2.main.custom.body.bustWeight - m_data1.main.custom.body.bustWeight));

#if HS2
                charaCtrl.ChangeNipColor();
                charaCtrl.ChangeNipGloss();
                charaCtrl.ChangeNipKind();
                charaCtrl.ChangeNipScale();
#elif KKSS
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

            //value update loop
            for(int a = 0; a < Mathf.Max(new float[]
            { m_data1.main.custom.body.shapeValueBody.Length,
                m_data1.main.custom.face.shapeValueFace.Length,
                m_data1.abmx.body.Count, m_data1.abmx.face.Count }); ++a)
            {
                float result = 0;

                enable = reset || !cfg.enableABMX.Value ? 0 : 1;

                //ABMX
                {
                    //Body
                    if(a < m_data1.abmx.body.Count)
                    {
                        //  CharaMorpher.Logger.LogDebug($"looking for values");
                        var bone1 = m_data1.abmx.body[a];
                        var bone2 = m_data2.abmx.body[a];
                        var current = boneCtrl.Modifiers.Find((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));
                        int count = 0;//may use this in other mods

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

                            switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
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

                        // CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
                        foreach(var mod in current.CoordinateModifiers)
                        {

                            //  CharaMorpher_Core.Logger.LogDebug($"in for loop");
                            var inRange = count < bone2.CoordinateModifiers.Length;
                            mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].PositionModifier, bone2.CoordinateModifiers[inRange ? count : 0].PositionModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].RotationModifier, bone2.CoordinateModifiers[inRange ? count : 0].RotationModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].ScaleModifier, bone2.CoordinateModifiers[inRange ? count : 0].ScaleModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[count].LengthModifier, bone2.CoordinateModifiers[inRange ? count : 0].LengthModifier,
                                enable * controls.body * controls.abmxBody * modVal);

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

                        // CharaMorpher_Core.Logger.LogDebug($"applying values");
                        current.Apply(boneCtrl.CurrentCoordinate.Value, null, KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame);
                    }

                    //face
                    if(a < m_data1.abmx.face.Count)
                    {
                        var bone1 = m_data1.abmx.face[a];
                        var bone2 = m_data2.abmx.face[a];
                        var current = boneCtrl.Modifiers.Find((k) => k.BoneName.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower()));
                        int count = 0;

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


                        switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
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

                        //CharaMorpher.Logger.LogDebug($"Morphing Bone...");
                        foreach(var mod in current.CoordinateModifiers)
                        {

                            var inRange = count < bone2.CoordinateModifiers.Length;

                            mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].PositionModifier, bone2.CoordinateModifiers[inRange ? count : 0].PositionModifier,
                                enable * controls.face * controls.abmxHead * modVal);

                            mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].RotationModifier, bone2.CoordinateModifiers[inRange ? count : 0].RotationModifier,
                                enable * controls.face * controls.abmxHead * modVal);

                            mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].ScaleModifier, bone2.CoordinateModifiers[inRange ? count : 0].ScaleModifier,
                                enable * controls.face * controls.abmxHead * modVal);

                            mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[count].LengthModifier, bone2.CoordinateModifiers[inRange ? count : 0].LengthModifier,
                                enable * controls.face * controls.abmxHead * modVal);

                            if(count == 0)
                            {
                                //CharaMorpher.Logger.LogDebug($"lerp Value: {enable * modVal}");
                                //CharaMorpher.Logger.LogDebug($"{current.BoneName} modifiers!!");
                                //CharaMorpher.Logger.LogDebug($"Face Bone 1 scale {a}: {bone1.CoordinateModifiers[count].ScaleModifier}");
                                //CharaMorpher.Logger.LogDebug($"Face Bone 2 scale {a}: {bone2.CoordinateModifiers[count].ScaleModifier}");
                                //CharaMorpher.Logger.LogDebug($"Result scale {a}: {mod.ScaleModifier}");
                                //CharaMorpher.Logger.LogDebug($"Face Bone has {count+1} modifiers!!");
                            }
                            ++count;
                        }

                        current.Apply(boneCtrl.CurrentCoordinate.Value, null, KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame);
                    }
                }

                enable = reset ? 0 : 1;

                // CharaMorpher_Core.Logger.LogDebug($"updating body");
                //Body Shape
                if(a < m_data1.main.custom.body.shapeValueBody.Length)
                {

                    if(cfg.headIndex.Value == a)
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                          enable * controls.body * controls.head * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later
                    else
                    if(cfg.torsoIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                          enable * controls.body * controls.torso * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later
                    else
                    if(cfg.buttIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                        enable * controls.body * controls.butt * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later
                    else
                    if(cfg.legIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                         enable * controls.body * controls.leg * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later
                    else
                    if(cfg.armIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                         enable * controls.body * controls.arm * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later
                    else
                    if(cfg.brestIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                       enable * controls.body * controls.boob * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later

                    else
                        result = (m_data1.main.custom.body.shapeValueBody[a] +
                        enable * controls.body * (m_data2.main.custom.body.shapeValueBody[a] - m_data1.main.custom.body.shapeValueBody[a]));//lerp, may change it later

                    // CharaMorpher_Core.Logger.LogDebug($"Loaded Body Part 1: {m_data1.main.custom.body.shapeValueBody[a]} at index {a}");
                    //CharaMorpher.Logger.LogDebug($"Loaded Body Part 2: {m_data2.main.custom.body.shapeValueBody[a]} at index {a}");

                    //load values to character
                    charaCtrl.SetShapeBodyValue(a, result);
                }

                // CharaMorpher_Core.Logger.LogDebug($"updating face");
                //Face Shape
                if(a < m_data1.main.custom.face.shapeValueFace.Length)
                {
                    if(cfg.eyeIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.face.shapeValueFace[a] +
                         enable * controls.face * controls.eyes * (m_data2.main.custom.face.shapeValueFace[a] - m_data1.main.custom.face.shapeValueFace[a]));
                    else
                     if(cfg.mouthIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.face.shapeValueFace[a] +
                         enable * controls.face * controls.mouth * (m_data2.main.custom.face.shapeValueFace[a] - m_data1.main.custom.face.shapeValueFace[a]));
                    else
                      if(cfg.earIndex.FindIndex(find => (find.Value == a)) >= 0)
                        result = (m_data1.main.custom.face.shapeValueFace[a] +
                         enable * controls.face * controls.ear * (m_data2.main.custom.face.shapeValueFace[a] - m_data1.main.custom.face.shapeValueFace[a]));
                    else
                        result = (m_data1.main.custom.face.shapeValueFace[a] +
                          enable * controls.face * (m_data2.main.custom.face.shapeValueFace[a] - m_data1.main.custom.face.shapeValueFace[a]));

                    // CharaMorpher_Core.Logger.LogDebug($"Loaded Face Part 1: {m_data1.main.custom.face.shapeValueFace[a]} at index {a}");
                    //CharaMorpher.Logger.LogDebug($"Loaded Face Part 2: {m_data1.main.custom.face.shapeValueFace[a]}");


                    //load values to character
                    charaCtrl.SetShapeFaceValue(a, result);
                }
                //  CharaMorpher_Core.Logger.LogDebug("");
            }

            charaCtrl.updateShape = true;
            charaCtrl.updateBustSize = true;
            //   if(initialLoad || reset || !cfg.enableABMX.Value)
            //       boneCtrl.NeedsFullRefresh = true;

            // boneCtrl.NeedsBaselineUpdate = true;

#if KKSS
            charaCtrl.ChangeSettingBodyDetail();
            charaCtrl.ChangeSettingFaceDetail();
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

                }
            }
        }




    }

    /// <summary>
    /// Needed to copy this class from ABMX in case old card is loaded
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
