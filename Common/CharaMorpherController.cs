
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

        private static MorphData charData = null;
        private static string lastCharDir = "";
        private static DateTime lastDT = new DateTime();
        private readonly MorphData m_data1 = new MorphData(), m_data2 = new MorphData();

        //this is a tuple list btw (of all the bones I found online https://betterpaste.me/?14286297a731ab43#4LAzfYnuymh5Eq2ce6v5zj4gGbQhFxwK6KZp1dM9LKGb)
        public static readonly List<(string, string)> bonecatagories =
             new List<(string, string)>()

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

            public float abmxHead;
            public float abmxEyes;
            public float abmxMouth;
            public float abmxEars;
            public float abmxHair;
        }

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
        //internal static bool initialLoad = false;

        public bool reloading = false;
        void CharaReloaded(object m, CharaReloadEventArgs n)
        {
            CharaMorpherController ctrl = n.ReloadedCharacter.GetComponent<CharaMorpherController>();

            //initialLoad = true;


            CharaMorpher_Core.Logger.LogDebug("Reloading Character");
            StartCoroutine(ctrl.CoMorphAsync());

            // initialLoad = true;
            // ctrl.UpdateMorphValues(false);

        }

        protected override void Awake()
        {

            KKAPI.Chara.CharacterApi.CharacterReloaded += CharaReloaded;

            //Make sure to call base version
            base.Awake();
        }

        IEnumerator CoMorphAsync()
        {
            for(int a = 0; a < 5; ++a)
                yield return new WaitForEndOfFrame();

            CharaMorpher_Core.Logger.LogDebug("Reloading After character loaded");
            reloading = true;
            OnCharaReload(KoikatuAPI.GetCurrentGameMode());

            for(int a = 0; a < 1; ++a)
                yield return new WaitForEndOfFrame();

            CharaMorpher_Core.Logger.LogDebug("Morphing model...");
            MorphChangeUpdate();
            reloading = false;
        }



        protected override void OnDestroy()
        {
            KKAPI.Chara.CharacterApi.CharacterReloaded -= CharaReloaded;
            base.OnDestroy();
        }

        /// <inheritdoc />
        void OnCharaReload(GameMode currentGameMode)
        {
            var cfg = CharaMorpher_Core.Instance.cfg;
            if(!cfg.enableInGame.Value && currentGameMode == GameMode.MainGame) return;
            if(!reloading || ChaControl.sex != 1/*could allow it with both genders later*/)
                return;


            var charaCtrl = ChaControl;
            var boneCtrl = charaCtrl.GetComponent<BoneController>();

            //TODO: Enter logic here...
            CharaMorpher_Core.Logger.LogDebug("clear data");
            m_data1.Clear();
            m_data2.Clear();

            //remove current modifiers
            CharaMorpher_Core.Logger.LogDebug("remove existing bone mods");
            foreach(var mod in boneCtrl.Modifiers)
                mod.Reset();
            boneCtrl.Modifiers.Clear();

            //Get picked character data
            CharaMorpher_Core.Logger.LogDebug("replace data 1");
            m_data1.main.CopyAll(ChaControl.chaFile);//get all character data!!!          
            m_data1.abmx.Populate(this);


            //Get referenced character data (only needs to be loaded once)
            if(charData == null ||
                lastCharDir != cfg.mergeCharDir.Value ||
                System.IO.File.GetLastWriteTime(cfg.mergeCharDir.Value).Ticks != lastDT.Ticks)
            {
                lastDT = System.IO.File.GetLastWriteTime(cfg.mergeCharDir.Value);
                lastCharDir = cfg.mergeCharDir.Value;
                charData = new MorphData();

                CharaMorpher_Core.Logger.LogDebug("load morph target");
                ChaFileControl.LoadCharaFile(cfg.mergeCharDir.Value, 255/*female; 0 male*/);

                CharaMorpher_Core.Logger.LogDebug("copy morph target");
                charData.main.CopyAll(ChaControl.chaFile);
                charData.abmx.Populate(this);//Store Bonemod Extended Data

                CharaMorpher_Core.Logger.LogDebug("reset original data");
                //Reset original character data
                ChaControl.chaFile.CopyAll(m_data1.main);
                //  initialLoad = true;
            }

            CharaMorpher_Core.Logger.LogDebug("replace data 2");
            m_data2.Copy(charData);






            //CharaMorpher_Core.Logger.LogDebug("Morphing model...");
            ////Update the model
            //MorphChangeUpdate();
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
        public void MorphChangeUpdate()
        {
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

            UpdateMorphValues(!cfg.enable.Value);
        }

        private void UpdateMorphValues(bool reset)
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
                charaCtrl.ChangeSettingNip();
                charaCtrl.ChangeSettingAreolaSize();
                charaCtrl.ChangeSettingNipColor();
                charaCtrl.ChangeSettingNipGlossPower();
#endif

                charaCtrl.UpdateBustSoftnessAndGravity();
            }

            CharaMorpher_Core.Logger.LogDebug($"data 1 body bones: {m_data1.abmx.body.Count}");
            CharaMorpher_Core.Logger.LogDebug($"data 2 body bones: {m_data2.abmx.body.Count}");
            CharaMorpher_Core.Logger.LogDebug($"data 1 face bones: {m_data1.abmx.face.Count}");
            CharaMorpher_Core.Logger.LogDebug($"data 2 face bones: {m_data2.abmx.face.Count}");
            CharaMorpher_Core.Logger.LogDebug($"chara bones: {boneCtrl.Modifiers.Count}");



            CharaMorpher_Core.Logger.LogDebug($"body parts: {m_data1.main.custom.body.shapeValueBody.Length}");
            CharaMorpher_Core.Logger.LogDebug($"face parts: {m_data1.main.custom.face.shapeValueFace.Length}");

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
                        CharaMorpher_Core.Logger.LogDebug($"current = {current.BoneName}");

                        float modVal = 0;

                        //remove L/R from bone name
                        string content = bone1.BoneName.Trim().ToLower();

                        string[] fingerNames = new[]
                        {
                            "cf_j_thumb",
                            "cf_j_index",
                            "cf_j_middle",
                            "cf_j_ring",
                            "cf_j_little"
                        };
                        if(fingerNames.ToList().FindIndex((k) => content.Contains(k)) >= 0)
                            modVal = controls.abmxHands;
                        else
                        {

                            string ending1 = "";
                            string ending2 = "";
                            int end = content.LastIndexOf("_");
                            int end2 = content.Substring(0, end).LastIndexOf("_");
                            if(end >= 0)
                                ending1 = content.Substring(content.LastIndexOf("_"));
                            if(end2 >= 0)
                                ending2 = content.Substring(end - (end - (end2)));

                            CharaMorpher_Core.Logger.LogDebug($"the result of ending 2 = {ending2}");

                            if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
                                content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

                            CharaMorpher_Core.Logger.LogDebug($"content of bone = {content ?? "... this is null"}");

                            switch(bonecatagories.Find((k) => k.Item1.Contains(content)).Item2)
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

                        CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
                        foreach(var mod in current.CoordinateModifiers)
                        {

                            CharaMorpher_Core.Logger.LogDebug($"in for loop");
                            var inRange = count < bone2.CoordinateModifiers.Length;
                            mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].PositionModifier, bone2.CoordinateModifiers[inRange ? count : 0].PositionModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].RotationModifier, bone2.CoordinateModifiers[inRange ? count : 0].RotationModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[count].ScaleModifier, bone2.CoordinateModifiers[inRange ? count : 0].ScaleModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[count].LengthModifier, bone2.CoordinateModifiers[inRange ? count : 0].LengthModifier,
                                enable * controls.body * controls.abmxBody * modVal);

                            CharaMorpher_Core.Logger.LogDebug($"updated values");
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

                        CharaMorpher_Core.Logger.LogDebug($"applying values");
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
                        int end2 = content.Substring(0, end).LastIndexOf("_");
                        if(end >= 0)
                            ending1 = content.Substring(content.LastIndexOf("_"));
                        if(end2 >= 0)
                            ending2 = content.Substring(end - (end - (end2)));

                        CharaMorpher_Core.Logger.LogDebug($"the result of ending 2 = {ending2}");

                        if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
                            content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

                        CharaMorpher_Core.Logger.LogDebug($"content of bone = {content ?? "... this is null"}");


                        switch(bonecatagories.Find((k) => k.Item1.Contains(content)).Item2)
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

                CharaMorpher_Core.Logger.LogDebug($"updating body");
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

                    CharaMorpher_Core.Logger.LogDebug($"Loaded Body Part 1: {m_data1.main.custom.body.shapeValueBody[a]} at index {a}");
                    //CharaMorpher.Logger.LogDebug($"Loaded Body Part 2: {m_data2.main.custom.body.shapeValueBody[a]} at index {a}");

                    //load values to character
                    charaCtrl.SetShapeBodyValue(a, result);
                }

                CharaMorpher_Core.Logger.LogDebug($"updating face");
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

                    CharaMorpher_Core.Logger.LogDebug($"Loaded Face Part 1: {m_data1.main.custom.face.shapeValueFace[a]} at index {a}");
                    //CharaMorpher.Logger.LogDebug($"Loaded Face Part 2: {m_data1.main.custom.face.shapeValueFace[a]}");


                    //load values to character
                    charaCtrl.SetShapeFaceValue(a, result);
                }
                CharaMorpher_Core.Logger.LogDebug("");
            }

            charaCtrl.updateShape = true;
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

        /// <inheritdoc />
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            //Nothing to implement :<
            //  throw new NotImplementedException();
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
