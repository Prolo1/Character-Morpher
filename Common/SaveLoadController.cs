using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using ExtensibleSaveFormat;
using KKAPI.Chara;
using MessagePack;
using MessagePack.Unity;
using MessagePack.Resolvers;
using UniRx;

using static BepInEx.Logging.LogLevel;
using KKABMX.Core;
using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.CharaMorpherController;
using static Character_Morpher.MorphUtil;

#if HONEY_API
using AIChara;
//using CharaCustom;
//using AIProject;
#else
//using ChaCustom;
//using static ChaFileDefine;
//using StrayTech;
#endif

/*
 Data that can (potentially) affect the save:
* enum MorphCalcType
* class MorphControls
* class MorphData
* class MorphData.AMBXSections
* class MorphConfig 
* var CharaMorpher_Core.cfg.defaults
* var CharaMorpher_Core.cfg.controlCategories

 all I can think of for now
 */

namespace Character_Morpher
{
	public abstract class SaveLoadController
	{
		public SaveLoadController()
		{

			CompositeResolver.Register(
				BuiltinResolver.Instance,
				StandardResolver.Instance,
				UnityResolver.Instance,
				//default resolver
				ContractlessStandardResolver.Instance
				);
		}

		// Convert an object to a byte array
		public static byte[] ObjectToByteArray(object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using(var ms = new MemoryStream())
			{
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		public static T ByteArrayToObject<T>(byte[] arr)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using(var ms = new MemoryStream())
			{
				ms.Write(arr, 0, arr.Length);
				T obj = (T)bf.Deserialize(ms);
				return obj;
			}
		}


		public abstract int Version { get; }
		public abstract string[] DataKeys { get; }

		public abstract PluginData Save(CharaCustomFunctionController ctrler);
		public abstract PluginData Load(CharaCustomFunctionController ctrler, PluginData data);
		protected abstract PluginData UpdateVersionFromPrev(CharaCustomFunctionController ctrler, PluginData data);

		public enum LoadDataType : int { }
	}

	/// <summary>
	/// saves controls from current data. make a new one if variables change
	/// </summary>
	public class CurrentSaveLoadController : SaveLoadControllerV1
	{
		public new int Version => base.Version + 1;

		public new string[] DataKeys => new[] { "MorphData_values", "MorphData_targetCard", "MorphData_targetPng", "MorphData_ogSize", "MorphData_isCurrenntData" };

		public new enum LoadDataType : int
		{
			Values,
			TargetCard,
			TargetPng,
			OgSize,
			IsCurrentData,
		}

		/*
		 Data that can (potentially) affect the save:
		* enum MorphCalcType
		* class MorphControls
		* class MorphData
		* class MorphData.AMBXSections
		* class MorphConfig 
		* var CharaMorpher_Core.cfg.defaults
		* var CharaMorpher_Core.cfg.controlCategories
		* string CharaMorpher_Core.strDivider
		* string CharaMorpher_Core.defaultStr
		 all I can think of for now
		 */

		/// <summary>
		/// creates an updated version 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected new PluginData UpdateVersionFromPrev(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;


			if(data == null || data.version != Version)
			{

				data = base.Load(ctrler, data)?.Copy();
				//MorphUtil.Logger.LogDebug($"Old version: {data?.version.ToString() ?? "Don't exist..."}");
				if(data != null && data.version == base.Version)
				{
					//last version
					var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKeys[0]], CompositeResolver.Instance);
					var tmpVals = values.ToDictionary((k) => k.Key.Trim(),
						(v) => new MorphSliderData(v.Key.Trim() /*just in case*/,
						data: v.Value.Item1, calc: v.Value.Item2,
						isABMX: bool.Parse(oldConversionList.FirstOrNull((p) => v.Key.Trim() == p[0].Trim())?[2] ?? bool.FalseString)));

					var newValues = new MorphControls() { currentSet = defaultStr, all = { { defaultStr, tmpVals } } };
					data.data[DataKeys[((int)LoadDataType.Values)]] = LZ4MessagePackSerializer.Serialize(newValues, CompositeResolver.Instance);
					data.data[DataKeys[((int)LoadDataType.OgSize)]] = LZ4MessagePackSerializer.Serialize(ctrl.m_data1, CompositeResolver.Instance);
					data.data[DataKeys[((int)LoadDataType.IsCurrentData)]] = LZ4MessagePackSerializer.Serialize(false, CompositeResolver.Instance);

					data.version = Version;
				}
				else
					data = null;
			}

			if(data == null)
				data = ctrler?.GetExtendedData(ctrl.isReloading);

			if((!data?.data?.Keys.Contains(DataKeys[((int)LoadDataType.IsCurrentData)])) ?? false)
				data.data[DataKeys[((int)LoadDataType.IsCurrentData)]] = LZ4MessagePackSerializer.Serialize(true, CompositeResolver.Instance);

			return data;
		}

		public new PluginData Load(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;

			data = UpdateVersionFromPrev(ctrler, data);// use if version goes up (i.e. 1->2)

			if(data == null) return null;

			try
			{

				if(data.version != Version) throw new Exception($"Target card data was incorrect version: expected [V{Version}] instead of [V{data.version}]");

				var png = ObjectToByteArray(data.data[DataKeys[((int)LoadDataType.TargetPng)]]);

				if(png == null) throw new Exception("png data does not exist...");

				var values = LZ4MessagePackSerializer.Deserialize
					<MorphControls>
					((byte[])data.data[DataKeys[((int)LoadDataType.Values)]], CompositeResolver.Instance);

				var data2 = LZ4MessagePackSerializer.Deserialize<MorphData>
					((byte[])data.data[DataKeys[((int)LoadDataType.TargetCard)]], CompositeResolver.Instance);

				var data1 = LZ4MessagePackSerializer.Deserialize<MorphData>
					((byte[])data.data[DataKeys[((int)LoadDataType.OgSize)]], CompositeResolver.Instance);

				var isCurData = LZ4MessagePackSerializer.Deserialize<bool>
					((byte[])data.data[DataKeys[((int)LoadDataType.IsCurrentData)]], CompositeResolver.Instance);



				data2.abmx.ForceSplitStatus();//needed since split is not saved 😥

				var newValues = values.all.ToDictionary(k => k.Key, v => v.Value.ToDictionary(k => k.Key, v2 => v2.Value.Clone()));

				if(ctrl.isReloading)//can only be done when reloading 
					ctrl.SoftSaveControls(ctrl.canUseCardMorphData);//keep this here

				//	MorphUtil.Logger.LogDebug("DATA 2");
				ctrl.m_data2.Copy(data2);

				ctrl.ctrls2 = new MorphControls { all = newValues };

				if(ctrl.canUseCardMorphData)
					ctrl.controls.Copy(ctrl.ctrls2);

				//get original 
				data1.abmx.ForceSplitStatus();
				//	MorphUtil.Logger.LogDebug("DATA 1");
				ctrl.m_data1.Copy(data1);
			}
			catch(Exception e)
			{
				MorphUtil.Logger.Log(Error | Message, $"Could not load PluginData:\n{e}\n");
				return null;
			}

			return data;
		}

		public new PluginData Save(CharaCustomFunctionController ctrler)
		{
			if(!CharaMorpher_Core.cfg.saveExtData.Value) return null;
			PluginData data = new PluginData() { version = Version, };
			try
			{
				var ctrl = (CharaMorpherController)ctrler;
				if(!ctrl.m_data2.abmx.isSplit) throw new Exception("Target card data was not fully initialized");

				data.data.Add(DataKeys[0], LZ4MessagePackSerializer.Serialize(ctrl.controls, CompositeResolver.Instance));
				data.data.Add(DataKeys[1], LZ4MessagePackSerializer.Serialize(ctrl.m_data2, CompositeResolver.Instance));


				if(ctrl.m_data2.main.pngData.IsNullOrEmpty()) throw new Exception("png data does not exist...");
				data.data.Add(DataKeys[2], ctrl.m_data2.main.pngData);
				data.data.Add(DataKeys[3], LZ4MessagePackSerializer.Serialize(ctrl.m_data1, CompositeResolver.Instance));

			}
			catch(Exception e)
			{
				MorphUtil.Logger.Log(Error | Message, $"Could not save PluginData: \n {e} ");
				return null;
			}
			ctrler.SetExtendedData(data);

			return data;
		}

	}

	public class SaveLoadControllerV1 : SaveLoadController
	{
		public override int Version => 1;

		public override string[] DataKeys => new[] { "MorphData_values", "MorphData_targetCard", "MorphData_targetPng", };

		public new enum LoadDataType : int
		{
			Values,
			TargetCard,
			TargetPng,
		}

		internal class OldMorphControls
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
			/// each value is set to zero
			/// </summary>
			public Dictionary<string, Tuple<float, MorphCalcType>> noVal
			{
				get
				{
					var tmp = all.ToDictionary(curr => curr.Key, curr => curr.Value);
					for(int a = 0; a < tmp.Count; ++a)
						tmp[tmp.Keys.ElementAt(a)] = Tuple.Create(0f, tmp[tmp.Keys.ElementAt(a)].Item2);
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
		[Serializable]
		public class OldMorphData
		{
			[Serializable]
			public class OldAMBXSections
			{
				public List<BoneModifier> body = new List<BoneModifier>();
				public List<BoneModifier> face = new List<BoneModifier>();
				private bool m_isLoaded = false;
				private bool m_isSplit = false;

				public bool isLoaded { get => m_isLoaded; private set => m_isLoaded = value; }
				public bool isSplit { get => m_isSplit; private set => m_isSplit = value; }


				public void Populate(CharaMorpherController morphControl, bool morph = false)
				{

					var boneCtrl = morph ? MorphTarget.extraCharacter?.GetComponent<BoneController>() : morphControl?.GetComponent<BoneController>();
					var charaCtrl = morphControl?.ChaControl;

					if(isLoaded) return;
					//Store Bonemod Extended Data
					{//helps get rid of data sooner

						if(!boneCtrl) MorphUtil.Logger.LogDebug("Bone controller doesn't exist");
						if(!charaCtrl) MorphUtil.Logger.LogDebug("Character controller doesn't exist");

						//This is the second dumbest fix
						//(I was changing the player character's bones when this was true ¯\_(ツ)_/¯)
						var data = boneCtrl?.GetExtendedData(!morph);

						var newModifiers = data.ReadBoneModifiers();
						//body bonemods on
						if(morph || bodyBonemodTgl)
							body = new List<BoneModifier>(newModifiers);
						//face bonemods on
						if(morph || faceBonemodTgl)
							face = new List<BoneModifier>(newModifiers);

						isLoaded = !!boneCtrl;//it can be shortened to just "boneCtrl" if I want
					}

					if(cfg.debug.Value)
					{
						if(morph) MorphUtil.Logger.LogDebug("Character 2:");
						else MorphUtil.Logger.LogDebug("Character 1:");
						foreach(var part in body) MorphUtil.Logger.LogDebug("Bone: " + part.BoneName);
					}

					BoneSplit(morphControl, charaCtrl, morph);
				}

				//split up body & head bones
				public void BoneSplit(CharaMorpherController charaControl, ChaControl bodyCharaCtrl, bool morph = false)
				{
					var ChaControl = charaControl?.GetComponent<ChaControl>();
					var ChaFileControl = ChaControl?.chaFile;

					if(!bodyCharaCtrl?.objHeadBone) return;
					if(isSplit || !isLoaded) return;

					if(cfg.debug.Value) MorphUtil.Logger.LogDebug("Splitting bones apart (this is gonna hurt)");


					var headRoot = bodyCharaCtrl.objHeadBone.transform.parent.parent;

					var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name)) { /*Additional*/headRoot.name };

					//Load Body
					if(morph || bodyBonemodTgl)
						body.RemoveAll(x => headBones.Contains(x.BoneName));

					//Load face
					if(morph || faceBonemodTgl)
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

				public OldAMBXSections Copy()
				{
					return new OldAMBXSections()
					{
						body = new List<BoneModifier>(body ?? new List<BoneModifier>()),
						face = new List<BoneModifier>(face ?? new List<BoneModifier>()),

						m_isSplit = m_isSplit,
						m_isLoaded = m_isLoaded,
					};
				}
			}

			public ChaFileControl main = new ChaFileControl();
			public OldAMBXSections abmx = new OldAMBXSections();


			public void Clear()
			{
				main = new ChaFileControl();
				abmx.Clear();
			}

			public OldMorphData Clone()
			{
				var tmp = new ChaFileControl();
				try
				{
					tmp.CopyAll(main);
					tmp.pngData = main.pngData.ToArray();//copy
#if KOI_API
					tmp.facePngData = main.facePngData.ToArray();//copy
#endif
				}
				catch(Exception e) { MorphUtil.Logger.LogError("Could not copy character data:\n" + e); }
#if HONEY_API
				//CopyAll will not copy this data in hs2
				tmp.dataID = main.dataID;
#endif

				return new OldMorphData() { main = tmp, abmx = abmx.Copy() };
			}

			public void Copy(OldMorphData data)
			{
				if(data == null) return;

				var tmp = data.Clone();
				this.main = tmp.main;
				this.abmx = tmp.abmx;
			}

			public void Copy(CharaMorpherController data, bool morph = false)
			{

#if HONEY_API
				//CopyAll will not copy this data in hs2/AI
				main.dataID = morph ? MorphTarget.chaFile.dataID : data.ChaControl.chaFile.dataID;
#endif

				try
				{
					main.CopyAll(morph ? MorphTarget.chaFile : data.ChaFileControl);
					main.pngData = (morph ? MorphTarget.chaFile.pngData :
						data.ChaFileControl.pngData)?.ToArray();
#if KOI_API
					main.facePngData = (morph ? MorphTarget.chaFile.facePngData :
						data.ChaFileControl.facePngData)?.ToArray();
#endif
				}
				catch(Exception e) { MorphUtil.Logger.LogError("Could not copy character data:\n" + e); }

				abmx.Populate(data, morph);
			}
		}

		/// <summary>
		/// creates an updated version (can NOT be called on base version)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override PluginData UpdateVersionFromPrev(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;

			if(data == null)
				data = ctrler?.GetExtendedData(ctrl.isReloading);

			return data;
		}

		public override PluginData Load(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;


			data = UpdateVersionFromPrev(ctrler, data);

			if(data == null) return null;

			try
			{
				if(data.version != Version) return data;


				var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKeys[((int)LoadDataType.Values)]], CompositeResolver.Instance);
				var target = LZ4MessagePackSerializer.Deserialize<OldMorphData>((byte[])data.data[DataKeys[((int)LoadDataType.TargetCard)]], CompositeResolver.Instance);
				var png = ObjectToByteArray(data.data[DataKeys[((int)LoadDataType.TargetPng)]]);

				if(png == null) throw new Exception("png data does not exist...");


			}
			catch(Exception e)
			{
				MorphUtil.Logger.Log(Error | Message, $"Could not load PluginData:\n{e} ");
				return null;
			}

			return data;
		}

		public override PluginData Save(CharaCustomFunctionController ctrler)
		{
			if(!CharaMorpher_Core.cfg.saveExtData.Value) return null;
			PluginData data = new PluginData() { version = Version, };
			try
			{
				var ctrl = (CharaMorpherController)ctrler;
				if(!ctrl.m_data2.abmx.isSplit) throw new Exception("Target card data was not fully initialized");

				data.data.Add(DataKeys[0], LZ4MessagePackSerializer.Serialize(ctrl.controls.all, CompositeResolver.Instance));
				data.data.Add(DataKeys[1], LZ4MessagePackSerializer.Serialize(ctrl.m_data2, CompositeResolver.Instance));


				if(ctrl.m_data2.main.pngData.IsNullOrEmpty()) throw new Exception("png data does not exist...");
				data.data.Add(DataKeys[2], ctrl.m_data2.main.pngData);
			}
			catch(Exception e)
			{
				MorphUtil.Logger.Log(Error | Message, $"Could not save PluginData: \n {e} ");
				return null;
			}

			ctrler.SetExtendedData(data);
			return data;
		}

	}

}
