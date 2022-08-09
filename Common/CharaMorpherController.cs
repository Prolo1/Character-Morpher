
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Text;
using System.Text.RegularExpressions;

using IllusionUtility.GetUtility;
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


using MessagePack;

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

				//public List<BoneModifier> newModifiers = new List<BoneModifier>();
				public bool isLoaded { get; private set; } = false;
				public bool isSplit { get; private set; } = false;
				//public List<BoneModifier> other = new List<BoneModifier>();

				public void Populate(CharaMorpherController morphControl, bool morph = false)
				{

					var boneCtrl = morph ? morphTarget.extraCharacter.GetComponent<BoneController>() : morphControl.GetComponent<BoneController>();
					var charaCtrl = morphControl.ChaControl;

					if(isLoaded) return;
					//Store Bonemod Extended Data
					{//helps get rid of data sooner



						if(!boneCtrl) CharaMorpher_Core.Logger.LogDebug("Bone controller don't exist");
						if(!morphControl.ChaControl) CharaMorpher_Core.Logger.LogDebug("Character controller don't exist");

						//This is the second dumbest fix
						//(I was changing the the first character's bones when this was true ¯\_(ツ)_/¯)
						//#if KKS || AI
						var data = boneCtrl?.GetExtendedData(!morph);
						//#else
						//						var data = boneCtrl?.GetExtendedData(false);
						//#endif

						var newModifiers = ReadBoneModifiers(data);
						body = new List<BoneModifier>(newModifiers);
						face = new List<BoneModifier>(newModifiers);
						isLoaded = true;
					}

					if(morph) CharaMorpher_Core.Logger.LogDebug("Character 2:");
					else CharaMorpher_Core.Logger.LogDebug("Character 1:");
					foreach(var part in body) CharaMorpher_Core.Logger.LogDebug("Bone: " + part.BoneName);



					BoneSplit(morphControl, charaCtrl);

				}

				//split up body & head bones
				public void BoneSplit(CharaMorpherController charaControl, ChaControl charaCtrl)
				{
					var ChaControl = charaControl.GetComponent<ChaControl>();
					var ChaFileControl = ChaControl.chaFile;

					//charaCtrl = morph ? morphTarget.extraCharacter : charaCtrl;

					if(!charaCtrl.objHeadBone) return;
					if(isSplit || !isLoaded) return;


					CharaMorpher_Core.Logger.LogDebug("Splitting bones apart");
					//if(!charaCtrl.objHeadBone) await Task.Run(() => { while(!charaCtrl.objHeadBone) ; });
					//#if HONEY_API
					//                    var headRoot = charaCtrl.transform.FindLoop("cf_J_Head");
					//#else
					//					var headRoot = charaCtrl.transform.FindLoop("cf_j_head");
					//#endif
					var headRoot = charaCtrl.objHeadBone.transform.parent.parent;

					var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name)) { /*Additional*/headRoot.name };

					//Load Body
					body.RemoveAll(x => headBones.Contains(x.BoneName));
					//body.AddRange(newModifiers.Where(x => headBones.Contains(x.BoneName)));

					//Load face
					var bodyBones = new HashSet<string>(charaCtrl.objTop.transform.
						GetComponentsInChildren<Transform>().Select(x => x.name).Except(headBones));
					face.RemoveAll(x => bodyBones.Contains(x.BoneName));
					//face.AddRange(newModifiers.Where(x => bodyBones.Contains(x.BoneName)));

					isSplit = true;


					//	charaControl.MorphChangeUpdate(updateValues: false);
					//	yield break;
				}

				public void ResetSplitStatus() => isSplit = false;


				public void Clear()
				{
					body?.Clear();
					face?.Clear();
					//other?.Clear();
					isLoaded = false;
					isSplit = false;
				}

				public AMBXSections Copy()
				{
					return new AMBXSections()
					{
						body = new List<BoneModifier>(body ?? new List<BoneModifier>()),
						face = new List<BoneModifier>(face ?? new List<BoneModifier>()),
						//other = new List<BoneModifier>(other ?? new List<BoneModifier>()),
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
				main.dataID = morph ? morphTarget.chaFile.dataID : data.ChaControl.chaFile.dataID;
#endif

				try
				{
					main.CopyAll(morph ? morphTarget.chaFile : data.ChaFileControl);
				}
				catch { }

				//IEnumerator CoPopulate(CharaMorpherController _data, bool _morph, int delay = 5)
				//{
				//	for(int a = 0; a < delay; ++a)
				//		yield return null;
				//	abmx.Populate(_data, _morph);
				//}
				//
				//data.StartCoroutine(CoPopulate(data, morph));

				abmx.Populate(data, morph);
			}
		}

		internal class MorphControls
		{
			Dictionary<string, float> _all, _lastAll;
			Coroutine post;
			public Dictionary<string, float> all
			{
				get
				{
					if(_all == null)
					{
						_all = new Dictionary<string, float>();
						_lastAll = new Dictionary<string, float>();
					}

					//var ctrl = this;
					IEnumerator CoPost()
					{
						for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
							yield return null;
						// CharaMorpher_Core.Logger.LogDebug("post called in controls");

						bool Check()
						{
							if(_all.Count != _lastAll.Count)
								return false;

							for(int a = 0; a < _all.Count; ++a)
								if(_all[_all.Keys.ElementAt(a)] != _lastAll[_lastAll.Keys.ElementAt(a)])
									return true;

							//CharaMorpher_Core.Logger.LogDebug("All values the same ");
							return false;
						}

						if(Check())
							OnSliderValueChange.Invoke();

						_lastAll = new Dictionary<string, float>(_all);
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
			public Dictionary<string, float> full
			{
				get
				{
					var tmp = all.ToDictionary(curr => curr.Key, curr => curr.Value);
					for(int a = 0; a < tmp.Count; ++a)
						tmp[tmp.Keys.ElementAt(a)] = 1;
					return tmp;
				}
			}
			/// <summary>
			/// list of every control with an "overall" name
			/// </summary>
			public IEnumerable<KeyValuePair<string, float>> overall
			{
				get
				=> all.Where((p) => Regex.IsMatch(p.Key, "overall", RegexOptions.IgnoreCase));
			}

		}
		internal MorphControls controls = new MorphControls();

		private static MorphData charData = null;
		private static string lastCharDir = "";
		private static DateTime lastDT = new DateTime();


		internal readonly MorphData m_data1 = new MorphData(), m_data2 = new MorphData();


		/// <summary>
		/// Called after the model has finished being loaded for the first time
		/// </summary>
		public bool initLoadFinished { get; private set; } = false;

		/// <summary>
		/// In the process of reloading. set to false after complete
		/// </summary>
		public bool reloading { get; internal set; } = false;

		internal static readonly MorphTarget morphTarget = new MorphTarget();
		internal class MorphTarget
		{
			private static ChaControl _extraCharacter = null;

			public static bool initalize
			{
				set
				{
					if(value)
					{
						if(_extraCharacter == null)
						{

							Transform parent = null;
							foreach(var hnd in CharacterApi.RegisteredHandlers)
								if(hnd.ControllerType == typeof(CharaMorpherController))
									if(hnd.Instances.Count() > 0)
										parent = hnd.Instances.First().transform.parent;


							//_extraCharacter = new ChaControl();//this is needed!!!
							_extraCharacter =

#if HONEY_API
							Character.Instance.CreateChara(1, parent?.gameObject, -10);
#elif KK
							Character.Instance.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#elif KKS
							Character.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#endif

							Destroy(morphTarget.extraCharacter.GetComponent<CharaMorpherController>());
							_extraCharacter.gameObject.SetActive(false);
							CharaMorpher_Core.Logger.LogDebug("created new character instance");
						}

						return;
					}
#if KKS
					Character.DeleteChara(_extraCharacter);
#else
					Character.Instance?.DeleteChara(_extraCharacter);
#endif
					_extraCharacter = null;
				}
				get { return _extraCharacter != null; }
			}
			public ChaControl extraCharacter { get => _extraCharacter; }

			public ChaFileControl chaFile { get { return extraCharacter?.chaFile; } }
		}



		//this is a tuple list btw (of bones found in abmx mod and online... somewhere)
#if KOI_API
		public static readonly List<KeyValuePair<string, string>> boneDatabaseCatagories = new List<KeyValuePair<string, string>>()
#else
		public static readonly List<(string, string)> bonecatagories = new List<(string, string)>()
#endif

#if KOI_API
		#region KKBones
		{
             //ABMX
        
                 //other head
	new KeyValuePair<string, string>("cf_J_megane_rx_ear", ""    ),

   //Head;
	new KeyValuePair<string, string>("cf_j_head",     "" ),
	new KeyValuePair<string, string>("cf_s_neck"         , ""  ),
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
	new KeyValuePair<string, string>("cf_hit_waist_L"    , "torso"  ),
	new KeyValuePair<string, string>("cf_j_spine01"      , "torso"  ),
	new KeyValuePair<string, string>("cf_j_spine02"      , "torso"  ),
	new KeyValuePair<string, string>("cf_j_spine03"      , "torso"  ),
	new KeyValuePair<string, string>("cf_s_waist01"      , "torso"  ),
	new KeyValuePair<string, string>("cf_s_waist02"      , "torso"  ),
   
    //Butt;
	new KeyValuePair<string, string>("cf_t_hips"         , "butt" ),
	new KeyValuePair<string, string>("cf_s_siri_L"       , "butt"),
	new KeyValuePair<string, string>("cf_hit_siri_L"     , "butt"),
 
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
	new KeyValuePair<string, string>("k_f_legupL_00"     , "legs" ),
	new KeyValuePair<string, string>("k_f_legupR_00"     , "legs" ),
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
	new KeyValuePair<string, string>("cf_j_toes_L"       , "feet"),

  //Genitals;
    new KeyValuePair<string, string>("cf_d_kokan"        , "genitals"),
	new KeyValuePair<string, string>("cf_j_kokan"        , "genitals"),
	new KeyValuePair<string, string>("cm_J_dan100_00"    , "genitals"),
	new KeyValuePair<string, string>("cm_J_dan109_00"    , "genitals"),
	new KeyValuePair<string, string>("cm_J_dan_f_L"      , "genitals"),
	new KeyValuePair<string, string>("cf_j_ana"          , "genitals"),


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
				("cm_J_dan101_00															 ","genitals"),
				("cm_J_dan109_00															 ","genitals"),
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
				("cf_J_Legsk_01_00",            "legs"),
				("cf_J_Legsk_01_01",            "legs"),
				("cf_J_Legsk_01_02",            "legs"),
				("cf_J_Legsk_01_03",            "legs"),
				("cf_J_Legsk_01_04",            "legs"),
				("cf_J_Legsk_01_05",            "legs"),
				("cf_J_Legsk_02_00",            "legs"),
				("cf_J_Legsk_02_01",            "legs"),
				("cf_J_Legsk_02_02",            "legs"),
				("cf_J_Legsk_02_03",            "legs"),
				("cf_J_Legsk_02_04",            "legs"),
				("cf_J_Legsk_02_05",            "legs"),
				("cf_J_Legsk_03_00",            "legs"),
				("cf_J_Legsk_03_01",            "legs"),
				("cf_J_Legsk_03_02",            "legs"),
				("cf_J_Legsk_03_03",            "legs"),
				("cf_J_Legsk_03_04",            "legs"),
				("cf_J_Legsk_03_05",            "legs"),
				("cf_J_Legsk_05_00",            "legs"),
				("cf_J_Legsk_05_01",            "legs"),
				("cf_J_Legsk_05_02",            "legs"),
				("cf_J_Legsk_05_03",            "legs"),
				("cf_J_Legsk_05_04",            "legs"),
				("cf_J_Legsk_05_05",            "legs"),
				("cf_J_Legsk_06_00",            "legs"),
				("cf_J_Legsk_06_01",            "legs"),
				("cf_J_Legsk_06_02",            "legs"),
				("cf_J_Legsk_06_03",            "legs"),
				("cf_J_Legsk_06_04",            "legs"),
				("cf_J_Legsk_06_05",            "legs"),
				("cf_J_Legsk_07_00",            "legs"),
				("cf_J_Legsk_07_01",            "legs"),
				("cf_J_Legsk_07_02",            "legs"),
				("cf_J_Legsk_07_03",            "legs"),
				("cf_J_Legsk_07_04",            "legs"),
				("cf_J_Legsk_07_05",            "legs"),

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

		private IEnumerator CoReloadChara()
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

			MorphTargetUpdate(this);

			yield return null;

			for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
				MorphChangeUpdate(updateValues: updateValues, initReset: initReset);


			yield break;
		}

		//Coroutine coFullRefresh;
		public IEnumerator CoMorphUpdate(int delay = 6, bool forceReset = false, bool initReset = false, bool forceChange = false)
		{
			//var tmp = reloading;

			for(int a = 0; a < delay; ++a)
				yield return null;

			//CharaMorpher_Core.Logger.LogDebug("Updating morph values after card save/load");
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

		public IEnumerator CoMorphAfterABMX(int delayExtra = 5, bool forcereset = false, bool forceChange = false)
		{
			var boneCtrl = ChaControl.GetComponent<BoneController>();

			yield return new WaitWhile(() => boneCtrl.NeedsFullRefresh || boneCtrl.NeedsBaselineUpdate);

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Updating morph values after ABMX");
			//MorphChangeUpdate(forcereset);

			yield return StartCoroutine(CoMorphUpdate(delayExtra, forcereset, forceChange: forceChange));


			yield break;
		}
		//bool forcedReload = false;
		Coroutine coForceReload;
		public void ForceCardReload()
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

		private static string MakeDirPath(string path) => CharaMorpher_Core.MakeDirPath(path);


		protected override void Awake()
		{
			base.Awake();

			var core = Instance;

			foreach(var ctrl in core.controlCategories)
				controls.all[ctrl.Value] = cfg.defaults[ctrl.Key].Value * .01f;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("dictionary has default values");

			MorphTarget.initalize = false;//just in-case
		}

		void LateUpdate()
		{
			if((!m_data1.abmx.isSplit || !m_data2.abmx.isSplit)
				&& initLoadFinished && boneSplitCheck())
				MorphChangeUpdate();
		}



		void HoneyReload(object m, CharaReloadEventArgs n) =>
			StartCoroutine(CoReloadChara());



		bool boneSplitCheck(bool onlycheck = false)
		{

			if(!onlycheck && (!m_data1.abmx.isLoaded || !m_data2.abmx.isLoaded) && (!m_data1.abmx.isSplit || !m_data2.abmx.isSplit))
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
					m_data2.abmx.BoneSplit(this, ChaControl);

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
			if(reloading) return;

			reloading = true;

			var boneCtrl = GetComponent<BoneController>();


			{
				//clear original data
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("clear data");
				m_data1.Clear();
				m_data2.Clear();
			}

			//store picked character data
			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("replace data 1");


			m_data1.Copy(this); //get all character data!!!

			//store png data
			m_data1.main.pngData = ChaFileControl.pngData;
#if KOI_API
			m_data1.main.facePngData = ChaFileControl.facePngData;
#endif

			//StartCoroutine(CoMorphTargetUpdate(4));
			MorphTargetUpdate(this);


			//if(initLoadFinished)
			{
				//	if(MakerAPI.InsideMaker)
				for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
					MorphChangeUpdate(initReset: true);

			}


			for(int a = -1; a < cfg.multiUpdateTest.Value + 6; ++a)
				StartCoroutine(CoMorphAfterABMX(delayExtra: 20 + a + 1, forceChange: true));

			if(!initLoadFinished)
				ChaFileControl.CopyAll(m_data1.main);

			//post update
			IEnumerator CoLaterStatus(int delayFrames, BoneController _boneCtrl)
			{
				reloading = true;
				for(int a = 0; a < delayFrames; ++a)
					yield return null;


				reloading = false;
				initLoadFinished = true;

				_boneCtrl.NeedsFullRefresh = true;
				yield break;
			}
			StartCoroutine(CoLaterStatus(11, boneCtrl));//I just need to do this stuff later
		}

		public static void MorphTargetUpdate(CharaMorpherController ctrl)
		{

			//create path to morph target
			string path = Path.Combine(MakeDirPath(cfg.charDir.Value), MakeDirPath(cfg.imageName.Value));


			//Get referenced character data (only needs to be loaded once)
			if(File.Exists(path))
				if(/*charData == null ||*/
					!MorphTarget.initalize ||
					lastCharDir != path ||
					File.GetLastWriteTime(path).Ticks != lastDT.Ticks)
				{

					//reloading = true;
					CharaMorpher_Core.Logger.LogDebug("Initializing secondary character");

					lastDT = File.GetLastWriteTime(path);
					lastCharDir = path;
					charData = new MorphData();

					//initialize secondary model
					MorphTarget.initalize = true;

					var bonectrl = morphTarget.extraCharacter.GetComponent<BoneController>();
					//this is the stupidest fix I've ever done (this allows the original ABMX sliders to work)
					if(bonectrl) bonectrl.hideFlags = HideFlags.DontSave;

					morphTarget.extraCharacter?.gameObject?.SetActive(false);

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("load morph target");
					morphTarget.chaFile.LoadCharaFile(path, noLoadPng: true);

					charData.Copy(ctrl, true);
				}


			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("replace data 2");
			ctrl.m_data2.Copy(charData);
		}

		/// <inheritdoc/>
		protected override void OnReload(GameMode currentGameMode, bool keepState)
		{
			if(keepState || reloading) return;

			reloading = false;
			CharaMorpher_Core.Logger.LogDebug("new Chara loaded");
			OnCharaReload(currentGameMode);
		}

		/// <inheritdoc/>
		protected override void OnCardBeingSaved(GameMode currentGameMode)
		{
			//reset values to normal after saving
			if(cfg.enable.Value && !cfg.saveWithMorph.Value)
				for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
					StartCoroutine(CoMorphUpdate(delay: 10));//turn the card back after
		}

		/// <inheritdoc/> 
		protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
		{

			//	for(int a = -1; a < cfg.multiUpdateTest.Value; ++a)
			//		StartCoroutine(CoMorphUpdate(delay: 10));
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


		/// <summary>
		/// Update bones/shapes whenever a change is made to the sliders
		/// </summary>
		/// <param name="forceReset: ">reset regardless of other perimeters</param>
		public void MorphChangeUpdate(bool forceReset = false, bool initReset = false, bool updateValues = true)
		{

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
			}

			#endregion

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("update values check?");
			if(!updateValues) return;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("not male in main game check?");
			if(currGameMode == GameMode.MainGame && ChaControl.sex != 1/*(allowed in maker as of now)*/)
				return;

			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("not male in maker check?");
			if(currGameMode == GameMode.Maker && ChaControl.sex == 0
				&& !cfg.enableInMaleMaker.Value) return;//lets try it out in male maker


			if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("All Checks passed?");

			bool reset = !cfg.enable.Value;
			reset = currGameMode == GameMode.MainGame ? (reset || !cfg.enableInGame.Value) : reset;

			MorphValuesUpdate(forceReset || reset, initReset: initReset);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="contain"></param>
		/// <param name="abmx"></param>
		/// <returns></returns>
		private float GetControlValue(string contain, bool abmx = false, bool fullVal = false)
		{
			var tmp = controls.all.ToList();
			if(fullVal)
				tmp = controls.full.ToList();

			return abmx ? tmp.Find(m => m.Key.ToLower().Contains("abmx") && m.Key.ToLower().Contains(contain.ToLower())).Value :
				tmp.Find(m => !m.Key.ToLower().Contains("abmx") && m.Key.ToLower().Contains(contain.ToLower())).Value;
		}

		private void MorphValuesUpdate(bool reset, bool initReset = false, bool abmx = true)
		{

			reset = initReset || reset;

			var cfg = CharaMorpher_Core.cfg;
			var charaCtrl = ChaControl;
			var boneCtrl = charaCtrl.GetComponent<BoneController>();

			float enable = (reset ? 0 : 1);


			//update obscure values
			{

				//not sure how to update this :\ (well it works so don't question it)
				charaCtrl.fileBody.areolaSize = Mathf.LerpUnclamped(m_data1.main.custom.body.areolaSize, m_data2.main.custom.body.areolaSize,
					enable * GetControlValue("body") * GetControlValue("Boobs"));

				charaCtrl.fileBody.bustSoftness = Mathf.LerpUnclamped(m_data1.main.custom.body.bustSoftness, m_data2.main.custom.body.bustSoftness,
					enable * GetControlValue("body") * GetControlValue("Boobs"));

				charaCtrl.fileBody.bustWeight = Mathf.LerpUnclamped(m_data1.main.custom.body.bustWeight, m_data2.main.custom.body.bustWeight,
					enable * GetControlValue("body") * GetControlValue("Boobs"));

				//Skin Colour
#if KOI_API
				charaCtrl.fileBody.skinMainColor = Color.LerpUnclamped(m_data1.main.custom.body.skinMainColor, m_data2.main.custom.body.skinMainColor,
									enable * GetControlValue("skin") * GetControlValue("base skin"));
				//	charaCtrl.fileBody.skinSubColor = Color.LerpUnclamped(m_data1.main.custom.body.skinSubColor, m_data2.main.custom.body.skinSubColor,
				//						enable * GetControlValue("skin") * GetControlValue("base skin"));
#elif HONEY_API
				charaCtrl.fileBody.skinColor = Color.LerpUnclamped(m_data1.main.custom.body.skinColor, m_data2.main.custom.body.skinColor,
									enable * GetControlValue("skin") * GetControlValue("base skin"));

#endif
				charaCtrl.fileBody.sunburnColor = Color.LerpUnclamped(m_data1.main.custom.body.sunburnColor, m_data2.main.custom.body.sunburnColor,
									enable * GetControlValue("skin") * GetControlValue("sunburn"));

				//Voice
#if HS2
				charaCtrl.fileParam2.voiceRate = Mathf.Lerp(m_data1.main.parameter2.voiceRate, m_data2.main.parameter2.voiceRate,
					enable * GetControlValue("voice"));
#endif

				charaCtrl.fileParam.voiceRate = Mathf.Lerp(m_data1.main.parameter.voiceRate, m_data2.main.parameter.voiceRate,
					enable * GetControlValue("voice"));

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
				CharaMorpher_Core.Logger.LogDebug($"chara bones: {boneCtrl.Modifiers.Count}");
				CharaMorpher_Core.Logger.LogDebug($"body parts: {m_data1.main.custom.body.shapeValueBody.Length}");
				CharaMorpher_Core.Logger.LogDebug($"face parts: {m_data1.main.custom.face.shapeValueFace.Length}");
			}



			//float MyLerp(float a, float b, float t) => a + t * (b - a);//this is not right but needed it for testing

			//value update loop

			//Main
			for(int a = 0; a < Mathf.Max(new float[]
			{
				m_data1.main.custom.body.shapeValueBody.Length,
				m_data1.main.custom.face.shapeValueFace.Length,
			});
			++a)
			{
				float result = 0;


				enable = (reset ? (initReset ? cfg.initialMorphTest.Value : 0) : 1);



				//Body Shape
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"updating body");
				if(a < m_data1.main.custom.body.shapeValueBody.Length)
				{
					//Value Update
					{
						float
							d1 = m_data1.main.custom.body.shapeValueBody[a],
							d2 = m_data2.main.custom.body.shapeValueBody[a];

						if(cfg.headIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset) * GetControlValue("head", fullVal: initReset));
						//result = MyLerp(d1, d2,
						//	  enable * controls.body * controls.head);//lerp, may change it later
						else
						if(cfg.torsoIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset) * GetControlValue("torso", fullVal: initReset));
						//result = MyLerp(d1, d2,
						//  enable * controls.body * controls.torso);//lerp, may change it later
						else
						if(cfg.buttIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset) * GetControlValue("butt", fullVal: initReset));
						//result = MyLerp(d1, d2,
						//enable * controls.body * controls.butt);//lerp, may change it later
						else
						if(cfg.legIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset) * GetControlValue("legs", fullVal: initReset));
						//result = MyLerp(d1, d2,
						// enable * controls.body * controls.legs);//lerp, may change it later
						else
						if(cfg.armIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset) * GetControlValue("arms", fullVal: initReset));
						//result = MyLerp(d1, d2,
						// enable * controls.body * controls.arms);//lerp, may change it later
						else
						if(cfg.brestIndex.FindIndex(find => (find.Value == a)) >= 0)
						{
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset) * GetControlValue("boobs", fullVal: initReset));

							//	charaCtrl.fileBody.shapeValueBody[a] = result;

							//result = MyLerp(d1, d2,
							//   enable * controls.body * controls.boobs);//lerp, may change it later
						}

						else
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("body", fullVal: initReset));
						//result = MyLerp(d1, d2,
						//enable * controls.body);//lerp, may change it later
					}

					// CharaMorpher_Core.Logger.LogDebug($"Loaded Body Part 1: {m_data1.main.custom.body.shapeValueBody[a]} at index {a}");
					//CharaMorpher.Logger.LogDebug($"Loaded Body Part 2: {m_data2.main.custom.body.shapeValueBody[a]} at index {a}");

					//load values to character
					charaCtrl.SetShapeBodyValue(a, result);
				}

				//Face Shape
				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"updating face");
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
								enable * GetControlValue("face", fullVal: initReset) * GetControlValue("eyes", fullVal: initReset));
						//result = MyLerp(d1, d2,
						//	enable * controls.face * controls.eyes);
						else
						 if(cfg.mouthIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("face", fullVal: initReset) * GetControlValue("mouth", fullVal: initReset));
						//result = MyLerp(d1, d2,
						// enable * controls.face * controls.mouth);
						else
						  if(cfg.earIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("face", fullVal: initReset) * GetControlValue("ears", fullVal: initReset));
						//result = MyLerp(d1, d2,
						// enable * controls.face * controls.ears);
						else
						 if(cfg.noseIndex.FindIndex(find => (find.Value == a)) >= 0)
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("face", fullVal: initReset) * GetControlValue("nose", fullVal: initReset));
						else
							result = Mathf.LerpUnclamped(d1, d2,
								enable * GetControlValue("face", fullVal: initReset));
						//result = MyLerp(d1, d2,
						//  enable * controls.face);
					}

					// CharaMorpher_Core.Logger.LogDebug($"Loaded Face Part 1: {m_data1.main.custom.face.shapeValueFace[a]} at index {a}");
					//CharaMorpher.Logger.LogDebug($"Loaded Face Part 2: {m_data1.main.custom.face.shapeValueFace[a]}");


					//load values to character
					//charaCtrl.chaFile.custom.face.shapeValueFace[a] = result;
					charaCtrl.SetShapeFaceValue(a, result);
				}
				//#endregion

				//  CharaMorpher_Core.Logger.LogDebug("");
			}

			//ABMX
			if(abmx)
				AbmxSettings(reset, initReset, boneCtrl);


			//Slider Defaults set
			if(MakerAPI.InsideMaker)
				SetDefaultSliders();



			//colour update
			if(initLoadFinished)
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

#if HONEY_API
			//  charaCtrl.ChangeNipColor();
			//  charaCtrl.ChangeNipGloss();
			//  charaCtrl.ChangeNipKind();
			//  charaCtrl.ChangeNipScale();


			//boneCtrl.NeedsFullRefresh = true;//may need to be called after slider movement
			//boneCtrl.NeedsBaselineUpdate = true;

#elif KOI_API
			//	charaCtrl.ChangeSettingBodyDetail();
			//	charaCtrl.ChangeSettingFaceDetail();
			//	charaCtrl.ChangeSettingAreolaSize();
			//	charaCtrl.ChangeSettingNipColor();
			//	charaCtrl.ChangeSettingNipGlossPower();
			//	charaCtrl.ChangeSettingNip();
			//	charaCtrl.ChangeSettingEyeTilt();

#endif

			//charaCtrl.UpdateBustSoftnessAndGravity();
			//charaCtrl.UpdateForce();//will update voice?
			//charaCtrl.LateUpdateForce();//will update everything else?

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

				enable = ((reset || !cfg.enableABMX.Value) ? (initReset ? cfg.initialMorphTest.Value : 0) : 1);

				#region ABMX

				//Body
				if(a < m_data1.abmx.body.Count)
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"looking for body values");

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
							ending1 = content.Substring(content.LastIndexOf("_"));
							end2 = content.Substring(0, end).LastIndexOf("_");
						}
						if(end2 >= 0)
							ending2 = content.Substring(end - (end - (end2)));

						// CharaMorpher_Core.Logger.LogDebug($"the result of ending 2 = {ending2}");

						if(ending1 == "_l" || ending1 == "_r" || ending2 == "_l_00" || ending2 == "_r_00")
							content = content.Substring(0, content.LastIndexOf(((ending1 == "_l" || ending1 == "_r") ? ending1 : ending2)));

						// CharaMorpher_Core.Logger.LogDebug($"content of bone = {content ?? "... this is null"}");
#if KOI_API
						switch(boneDatabaseCatagories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
						switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
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
							modVal = 1;
							break;
						}
					}

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
					UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
						sectVal: (cfg.linkOverallABMXSliders.Value ?
						GetControlValue("body", fullVal: initReset) : 1) *
						GetControlValue("Body", true, fullVal: initReset),
						enable: enable);
				}

				//face
				if(a < m_data1.abmx.face.Count)
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"looking for face values");

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


#if KOI_API
					switch(boneDatabaseCatagories.Find((k) => k.Key.Trim().ToLower().Contains(content)).Value)
#else
					switch(bonecatagories.Find((k) => k.Item1.Trim().ToLower().Contains(content)).Item2)
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
						modVal = 1;
						break;
					}

					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"Morphing Bone...");
					UpdateBoneModifier(ref current, bone1, bone2, modVal, index: a,
						sectVal: (cfg.linkOverallABMXSliders.Value ?
						GetControlValue("face", fullVal: initReset) : 1) *
						GetControlValue("head", true, fullVal: initReset),
						enable: enable);
				}
				#endregion

				//  CharaMorpher_Core.Logger.LogDebug("");
			}
		}


		private void SetDefaultSliders(int delay = 3)
		{
			//for(int a = 0; a < delay; ++a)
			//	yield return null;

			var mkBase = MakerAPI.GetMakerBase();
			var bodycustum = CharaMorpherGUI.bodyCustom;
			var facecustum = CharaMorpherGUI.faceCustom;
			var boobcustum = CharaMorpherGUI.boobCustom;

			if(mkBase && !reloading)
			{
				//boobcustum?.sldBustWeight?.Set(ChaControl.fileBody.bustWeight);
				//boobcustum?.sldBustSoftness?.Set(ChaControl.fileBody.bustSoftness);
				//boobcustum?.sldAreolaSize?.Set(ChaControl.fileBody.areolaSize);

				if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug("Resetting CVS Sliders");
				bodycustum?.CalculateUI();
				facecustum?.CalculateUI();
				boobcustum?.CalculateUI();

				mkBase.updateCvsChara = true;
#if HONEY_API
				mkBase.updateCvsBodyShapeBreast = true;
#endif

			}

			//yield break;
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
		private void UpdateBoneModifier(ref BoneModifier current, BoneModifier bone1, BoneModifier bone2, float modVal, float sectVal = 1, float enable = 1, int index = 0)
		{
			try
			{

				int count = 0;//may use this in other mods
				foreach(var mod in current?.CoordinateModifiers)
				{
					if(cfg.debug.Value) CharaMorpher_Core.Logger.LogDebug($"in for loop");

					var inRange = count < bone2.CoordinateModifiers.Length;


					mod.PositionModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].PositionModifier, bone2.CoordinateModifiers[inRange ? count : 0].PositionModifier,
						Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

					mod.RotationModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].RotationModifier, bone2.CoordinateModifiers[inRange ? count : 0].RotationModifier,
						Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

					mod.ScaleModifier = Vector3.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].ScaleModifier, bone2.CoordinateModifiers[inRange ? count : 0].ScaleModifier,
						Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

					mod.LengthModifier = Mathf.LerpUnclamped(bone1.CoordinateModifiers[inRange ? count : 0].LengthModifier, bone2.CoordinateModifiers[inRange ? count : 0].LengthModifier,
						Mathf.Clamp(enable, 0, 1) * sectVal * modVal);

					if(cfg.debug.Value)
					{
						//   CharaMorpher_Core.Logger.LogDebug($"updated values");
						if(count == 0)
						{
							//if(cfg.debug.Value)
							//{
							CharaMorpher_Core.Logger.LogDebug($"lerp Value {index}: {enable * modVal}");
							CharaMorpher_Core.Logger.LogDebug($"{current.BoneName} modifiers!!");
							CharaMorpher_Core.Logger.LogDebug($"Body Bone 1 scale {index}: {bone1.CoordinateModifiers[count].ScaleModifier}");
							CharaMorpher_Core.Logger.LogDebug($"Body Bone 2 scale {index}: {bone2.CoordinateModifiers[count].ScaleModifier}");
							CharaMorpher_Core.Logger.LogDebug($"Result scale {index}: {mod.ScaleModifier}");
							//}
						}
					}

					++count;
				}

				var boneCtrl = GetComponent<BoneController>();
				//   CharaMorpher_Core.Logger.LogDebug($"applying values");
#if HS2
				current.Apply(boneCtrl.CurrentCoordinate.Value, null, true);
#else
				current.Apply(boneCtrl.CurrentCoordinate.Value, null, true);
#endif

				boneCtrl.NeedsBaselineUpdate = true;
			}
			catch
			{
			}

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
