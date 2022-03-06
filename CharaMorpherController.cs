using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IllusionUtility.GetUtility;
using HS2;
using AIChara;

using KKAPI;
using KKAPI.Utilities;
using KKAPI.Maker;
using KKAPI.Chara;
using KKAPI.Studio;

using KKABMX.Core;
using ExtensibleSaveFormat;

using UnityEngine;
using UniRx;

namespace HS2_CharaMorpher
{
    public class CharaMorpherController : CharaCustomFunctionController
    {
        internal class MorphData
        {
            internal class ABMXSections
            {
                public List<BoneModifier> body = new List<BoneModifier>();
                public List<BoneModifier> face = new List<BoneModifier>();
                public List<BoneModifier> other = new List<BoneModifier>();

                //gets extended data from last loaded character
                public void Populate(BoneController boneCtrl, Transform modbody)
                {
                    var charaCtrl = boneCtrl.GetComponent<ChaControl>();


                    //Store Bonemod Extended Data
                    {//helps get rid of data sooner
                        var data = boneCtrl.GetExtendedData(true);
                        var newModifiers = ReadBoneModifiers(data);
                        body = new List<BoneModifier>(newModifiers);
                        face = new List<BoneModifier>(newModifiers);
                    }

                    //split up body & head bones
                    {
                        //#if AI || HS2
                        var headRoot = charaCtrl.objHeadBone.transform.parent.parent;
                        CharaMorpher.Logger.LogDebug(charaCtrl.objHeadBone.transform.parent.parent.name);
                        CharaMorpher.Logger.LogDebug(charaCtrl.objBodyBone.name);
                        //#else
                        //                        var headRoot = charaCtrl.transform.FindLoop("cf_j_head");
                        //#endif

                        //charaCtrl.objHeadBone;
                        //charaCtrl.objHead;
                        //charaCtrl.objBody;
                        //charaCtrl.objBodyBone;



                        var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name));
                        headBones.Add(headRoot.name);
                        body.RemoveAll(x => headBones.Contains(x.BoneName));

                        var bodyBones = new HashSet<string>(modbody.transform.FindLoop("BodyTop").GetComponentsInChildren<Transform>().Select(x => x.name).Except(headBones));

                        face.RemoveAll(x => bodyBones.Contains(x.BoneName));
                    }
                }

                public ABMXSections Copy()
                {
                    return new ABMXSections()
                    {
                        body = new List<BoneModifier>(body),
                        face = new List<BoneModifier>(face),
                        other = new List<BoneModifier>(other)
                    };
                }
                public void Clear()
                {
                    body.Clear();
                    face.Clear();
                    other.Clear();
                }
            }
            public ChaFile main { get; set; } = new ChaFile();
            public ABMXSections abmx { get; set; } = new ABMXSections();
            public void Clear()
            {
                main = new ChaFile();
                abmx.Clear();
            }
            public MorphData Copy()
            {
                var tmp = new MorphData() { abmx = abmx.Copy() };
                tmp.main.CopyCustom(main.custom);
                return tmp;
            }
        }

        private static MorphData charData = null;
        private static string lastCharDir = "";
        private static DateTime lastDT = new DateTime();
        private MorphData m_data1 = new MorphData(), m_data2 = new MorphData();

        //this is a tuple list btw (of all the AI/HS2 bones I found online https://betterpaste.me/?14286297a731ab43#4LAzfYnuymh5Eq2ce6v5zj4gGbQhFxwK6KZp1dM9LKGb)
        public static readonly List<(string, string)> bonecatagories =
             new List<(string, string)>()
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

             };

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

            public float abmxFace;
            public float abmxEyes;
            public float abmxMouth;
            public float abmxEars;
            public float abmxHair;
        }

        private static int index = 0;//used exclusively for the controls
        public static MorphControls controls = new MorphControls()
        {
            //Main
            body = CharaMorpher.Instance.cfg.defaults[index++].Value,
            head = CharaMorpher.Instance.cfg.defaults[index++].Value,
            boob = CharaMorpher.Instance.cfg.defaults[index++].Value,
            butt = CharaMorpher.Instance.cfg.defaults[index++].Value,
            torso = CharaMorpher.Instance.cfg.defaults[index++].Value,
            arm = CharaMorpher.Instance.cfg.defaults[index++].Value,
            leg = CharaMorpher.Instance.cfg.defaults[index++].Value,

            face = CharaMorpher.Instance.cfg.defaults[index++].Value,
            ear = CharaMorpher.Instance.cfg.defaults[index++].Value,
            eyes = CharaMorpher.Instance.cfg.defaults[index++].Value,
            mouth = CharaMorpher.Instance.cfg.defaults[index++].Value,

            //ABMX
            abmxBody = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxBoobs = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxButt  = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxTorso = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxArms  = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxHands = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxLegs = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxFeet = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxGenitals = CharaMorpher.Instance.cfg.defaults[index++].Value,

            abmxFace = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxEars = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxEyes = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxMouth = CharaMorpher.Instance.cfg.defaults[index++].Value,
            abmxHair = CharaMorpher.Instance.cfg.defaults[index++].Value,
        };
        internal bool initialLoad = true, loadingSecondary = false;

        /// <inheritdoc />
        protected override void OnReload(GameMode currentGameMode)
        {

            initialLoad = true;

            var cfg = CharaMorpher.Instance.cfg;
            if(currentGameMode != GameMode.Maker || !cfg.enable.Value || MakerAPI.GetMakerSex() != 1/*could just allow it in both makers later*/|| loadingSecondary)
            {
                KKAPI.Maker.MakerAPI.MakerExiting += (a, b) => { CharaMorpher.Logger.LogDebug($"Morpher Has exited!"); };
                if(loadingSecondary)
                {
                    CharaMorpher.Logger.LogDebug($"this is a secondarry load");
                    ChaControl.chaFile.CopyCustom(m_data1.main.custom);

                    //Update the model
                    MorphChangeUpdate();
                    loadingSecondary = false;
                }

                return;
            }


            //TODO: Enter logic here...
            m_data1.Clear();
            m_data2.Clear();

            //Get picked character data
            m_data1.main.CopyCustom(ChaControl.fileCustom);//get all character data!!!
            var boneCtrl = ChaControl.GetComponent<BoneController>();

            //Store Bonemod Extended Data
            m_data1.abmx.Populate(boneCtrl, this.transform);



            //Get referenced character data (only needs to be loaded once)
            if(charData == null ||
                lastCharDir != cfg.morphCharDir.Value ||
                System.IO.File.GetLastWriteTime(cfg.morphCharDir.Value).Ticks != lastDT.Ticks)
            {
                lastDT = System.IO.File.GetLastWriteTime(cfg.morphCharDir.Value);
                lastCharDir = cfg.morphCharDir.Value;
                charData = new MorphData();

                //I have no clue if this works
                //GameObject go = Instantiate(ChaControl.gameObject, ChaControl.gameObject.transform.parent);
                try
                {
                    loadingSecondary = true;
                    CharaMorpher.Logger.LogDebug($"Start loading chara file");
                    ChaControl.chaFile.LoadCharaFile(cfg.morphCharDir.Value);

                    CharaMorpher.Logger.LogDebug($"End loading chara file");

                    CharaMorpher.Logger.LogDebug($"found file data");
                    charData.main.CopyCustom(ChaControl.fileCustom);

                    CharaMorpher.Logger.LogDebug($"Got File data");
                    //Store Bonemod Extended Data
                    charData.abmx.Populate(GetComponent<BoneController>(), this.transform);

                    CharaMorpher.Logger.LogDebug($"Got Extended File data");


                    ChaControl.chaFile.LoadFromBytes(m_data1.main.GetCustomBytes());
                    CharaMorpher.Logger.LogDebug($"Loaded Original data");

                    m_data2 = charData.Copy();

                    //ChaControl.SetBodyBaseMaterial();//probobly not what I need
                }
                catch(Exception e)
                {
                    CharaMorpher.Logger.LogDebug(e);
                    // Destroy(go);
                }

            }
            else
            {
                m_data2.main.CopyCustom(charData.main.custom);
                m_data2.abmx.body = new List<BoneModifier>(charData.abmx.body);
                m_data2.abmx.face = new List<BoneModifier>(charData.abmx.face);
            }

            //Update the model
            MorphChangeUpdate();
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
#if KK || EC || KKS
                      case 1:
                          CharaMorpher.Logger.LogDebug("[KKABMX] Loading legacy embedded ABM data");
                          return OldDataConverter.MigrateOldExtData(data);
#endif

                    default:
                        throw new NotSupportedException($"[KKABMX] Save version {data.version} is not supported");
                    }
                }
                catch(Exception ex)
                {
                    CharaMorpher.Logger.LogError("[KKABMX] Failed to load extended data - " + ex);
                }
            }
            return new List<BoneModifier>();
        }

        public void MorphChangeUpdate()
        {
            var cfg = CharaMorpher.Instance.cfg;

            // if(!cfg.enable.Value) { UpdateMorphValues(true); return; }


            //Merge results

            var charaCtrl = MakerAPI.GetCharacterControl();
            var boneCtrl = charaCtrl.GetComponent<BoneController>();
            if(cfg.enableABMX.Value)
            {
                //add non-existent bones to other lists
                //Body
                BoneModifierMatching(ref m_data1.abmx.body, m_data2.abmx.body);
                BoneModifierMatching(ref m_data2.abmx.body, m_data1.abmx.body);
                BoneModifierMatching(ref boneCtrl, m_data1.abmx.body);

                //Face
                BoneModifierMatching(ref m_data1.abmx.face, m_data2.abmx.face);
                BoneModifierMatching(ref m_data2.abmx.face, m_data1.abmx.face);
                BoneModifierMatching(ref boneCtrl, m_data1.abmx.face);

                //sort for less computation later (can't sort boneCtrl since it has both)
                m_data1.abmx.body.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
                m_data2.abmx.body.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
                m_data1.abmx.face.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
                m_data2.abmx.face.Sort((a, b) => a.BoneName.CompareTo(b.BoneName));
            }

            UpdateMorphValues(!cfg.enable.Value);

        }

        public void UpdateMorphValues(bool reset)
        {
            var cfg = CharaMorpher.Instance.cfg;
            var charaCtrl = MakerAPI.GetCharacterControl();
            var boneCtrl = charaCtrl.GetComponent<BoneController>();

            float enable = reset ? 0 : 1;
            //update obscure values
            {
                //not sure how to update this one specifically :\
                charaCtrl.fileBody.areolaSize = (m_data1.main.custom.body.areolaSize +
                              enable * controls.body * controls.boob * (m_data2.main.custom.body.areolaSize - m_data1.main.custom.body.areolaSize));

                charaCtrl.fileBody.bustSoftness = (m_data1.main.custom.body.bustSoftness +
                               enable * controls.body * controls.boob * (m_data2.main.custom.body.bustSoftness - m_data1.main.custom.body.bustSoftness));

                charaCtrl.fileBody.bustWeight = (m_data1.main.custom.body.bustWeight +
                               enable * controls.body * controls.boob * (m_data2.main.custom.body.bustWeight - m_data1.main.custom.body.bustWeight));


                charaCtrl.UpdateBustSoftnessAndGravity();
                //one of these may work (if they are what I think they do: and they did!)
                charaCtrl.ChangeNipColor();
                charaCtrl.ChangeNipGloss();
                charaCtrl.ChangeNipKind();
                charaCtrl.ChangeNipScale();

            }

            //value update loop
            for(int a = 0; a < Mathf.Max(new float[] { m_data1.main.custom.body.shapeValueBody.Length, m_data1.main.custom.face.shapeValueFace.Length, m_data1.abmx.body.Count, m_data1.abmx.face.Count }); ++a)
            {
                float result = 0;

                //ABMX
                if(cfg.enableABMX.Value || reset)
                {
                    //Body
                    if(a < m_data1.abmx.body.Count)
                    {
                        var bone1 = m_data1.abmx.body[a];
                        var bone2 = m_data2.abmx.body[a];
                        var current = boneCtrl.Modifiers.Find((k) => k.BoneName == bone1.BoneName);
                        int count = 0;//may use this in other mods


                        float modVal = 0;
                        switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower())).Item2)
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

                        //CharaMorpher.Logger.LogDebug($"Morphing Bone...");
                        foreach(var mod in current.CoordinateModifiers)
                        {
                            mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].PositionModifier, bone2.CoordinateModifiers[count].PositionModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].RotationModifier, bone2.CoordinateModifiers[count].RotationModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].ScaleModifier, bone2.CoordinateModifiers[count].ScaleModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[count].LengthModifier, bone2.CoordinateModifiers[count].LengthModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            //CharaMorpher.Logger.LogDebug($"lerp Value {a}: {modVal}");
                            //CharaMorpher.Logger.LogDebug($"{current.BoneName} modifiers!!");
                            //CharaMorpher.Logger.LogDebug($"Body Bone 1 scale {a}: {bone1.CoordinateModifiers[count].ScaleModifier}");
                            //CharaMorpher.Logger.LogDebug($"Body Bone 2 scale {a}: {bone2.CoordinateModifiers[count].ScaleModifier}");
                            //CharaMorpher.Logger.LogDebug($"Result scale {a}: {mod.ScaleModifier}");
                            //CharaMorpher.Logger.LogDebug($"Body Bone has {count+1} modifiers!!");

                            ++count;
                        }

                        current.Apply(boneCtrl.CurrentCoordinate.Value, null, false);
                    }

                    //face
                    if(a < m_data1.abmx.face.Count)
                    {
                        var bone1 = m_data1.abmx.face[a];
                        var bone2 = m_data2.abmx.face[a];
                        var current = boneCtrl.Modifiers.Find((k) => k.BoneName == bone1.BoneName);
                        int count = 0;

                        float modVal = 0;
                        switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(bone1.BoneName.Trim().ToLower())).Item2)
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

                            mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].PositionModifier, bone2.CoordinateModifiers[count].PositionModifier,
                                enable * controls.face * controls.abmxFace * modVal);
                            mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].RotationModifier, bone2.CoordinateModifiers[count].RotationModifier,
                                enable * controls.face * controls.abmxFace * modVal);
                            mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].ScaleModifier, bone2.CoordinateModifiers[count].ScaleModifier,
                                enable * controls.face * controls.abmxFace * modVal);
                            mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[count].LengthModifier, bone2.CoordinateModifiers[count].LengthModifier,
                                enable * controls.face * controls.abmxFace * modVal);


                            //CharaMorpher.Logger.LogDebug($"lerp Value: {modVal}");
                            //CharaMorpher.Logger.LogDebug($"{current.BoneName} modifiers!!");
                            //CharaMorpher.Logger.LogDebug($"Face Bone 1 scale {a}: {bone1.CoordinateModifiers[count].ScaleModifier}");
                            //CharaMorpher.Logger.LogDebug($"Face Bone 2 scale {a}: {bone2.CoordinateModifiers[count].ScaleModifier}");
                            //CharaMorpher.Logger.LogDebug($"Result scale {a}: {mod.ScaleModifier}");
                            //CharaMorpher.Logger.LogDebug($"Face Bone has {count+1} modifiers!!");

                            ++count;
                        }

                        current.Apply(boneCtrl.CurrentCoordinate.Value, null, false);
                    }
                }

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

                    //CharaMorpher.Logger.LogDebug($"Loaded Body Part 1: {m_data1.main.custom.body.shapeValueBody[a]} at index {a}");
                    //   CharMerger.Logger.LogDebug($"Loaded Body Part 2: {m_data2.custom.body.shapeValueBody[a]} at index {a}");

                    //load values to character
                    charaCtrl.SetShapeBodyValue(a, result);
                }

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

                    CharaMorpher.Logger.LogDebug($"Loaded Face [{a}]: {m_data1.main.custom.face.shapeValueFace[a]}");
                    //CharMerger.Logger.LogDebug($"Loaded Face Part 2: {data2.main.custom.face.shapeValueFace[a]}");


                    //load values to character
                    charaCtrl.SetShapeFaceValue(a, result);
                }
            }

            charaCtrl.updateShape = true;


            if(initialLoad)
                boneCtrl.NeedsFullRefresh = true;
            else
                boneCtrl.NeedsBaselineUpdate = true;

            initialLoad = false;
        }

        /// <summary>
        /// Adds all bones from bone2 to bone1
        /// </summary>
        /// <param name="bone1"></param>
        /// <param name="bone2"></param>
        private static void BoneModifierMatching(ref List<BoneModifier> bone1, List<BoneModifier> bone2)
        {
            foreach(var bone in bone2)
            {
                if(bone1.FindIndex((k) => k.BoneName == bone.BoneName) < 0)
                {
                    bone1.Add(new BoneModifier(bone.BoneName));
                    //     CharaMorpher.Logger.LogDebug($"adding bone: {bone.BoneName} to [{bone1}]");
                }
            }
        }

        /// <summary>
        /// Adds all bones from bone2 to bone1
        /// </summary>
        /// <param name="bone1"></param>
        /// <param name="bone2"></param>
        private static void BoneModifierMatching(ref BoneController bone1, List<BoneModifier> bone2)
        {
            foreach(var bone in bone2)
            {
                if(bone1.Modifiers.FindIndex((k) => k.BoneName == bone.BoneName) < 0)
                {
                    bone1.AddModifier(new BoneModifier(bone.BoneName));
                    //    CharaMorpher.Logger.LogDebug($"adding bone: {bone.BoneName} to [{bone1}]");
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            //Nothing to implement :<
            //  throw new NotImplementedException();
        }
    }
}
