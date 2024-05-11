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
//using UniRx;

using static BepInEx.Logging.LogLevel;
using KKABMX.Core;
using static Character_Morpher.CharaMorpher_Core;
using static Character_Morpher.CharaMorpher_Controller;

#if HONEY_API
using AIChara;
//using CharaCustom;
//using AIProject;
#elif KK
//using UniRx;
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
	using static Character_Morpher.Morph_Util;//leave it here
	using static Character_Morpher.CurrentSaveLoadManager.LoadDataType;

	/// <summary>
	/// saves controls from current data. 
	/// Note: make a new one if variables change
	/// </summary> 
	public abstract class SaveLoadManager<TCtrler, TData>
	{
		public abstract int Version { get; }
		public abstract string[] DataKeys { get; }
		public enum LoadDataType : int { }

		public SaveLoadManager()
		{
			CompositeResolver.Register(
				UnityResolver.Instance,
				StandardResolver.Instance,
				BuiltinResolver.Instance,
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

		public static T1 ByteArrayToObject<T1>(byte[] arr)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using(var ms = new MemoryStream())
			{
				ms.Write(arr, 0, arr.Length);
				T1 obj = (T1)bf.Deserialize(ms);
				return obj;
			}
		}

		public abstract TData Save(TCtrler ctrler, TData data);
		public abstract TData Load(TCtrler ctrler, TData data);
		protected abstract TData UpdateVersionFromPrev(TCtrler ctrler, TData data);
	}

	/// <inheritdoc/>
	public class CurrentSaveLoadManager : SaveLoadManagerV2
	{
		public new int Version => base.Version + 1;

		public new string[] DataKeys => new[]
		{
			"MorphData_values",
			"MorphData_targetCard",
			"MorphData_targetPng",
			"MorphData_ogSize",
			"MorphData_isCurrenntData",
			"MorphData_enable",
			"MorphData_enableABMX"
		};

		public new enum LoadDataType : int
		{
			Values,
			TargetCard,
			TargetPng,
			OrigSize,
			HoldsFigureData,
			Enable,
			EnableABMX,
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
		/// <summary>
		/// creates an updated version 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected new PluginData UpdateVersionFromPrev(CharaMorpher_Controller ctrl, PluginData data)
		{

			if(data == null)
				data = ctrl?.GetExtendedData(ctrl.IsReloading);

			if(data == null || data.version != Version)
			{

				data = base.UpdateVersionFromPrev(ctrl, data)?.Copy();

				if(cfg.debug.Value) Logger.LogDebug($"Old version: {data?.version.ToString() ?? "Don't exist..."}");
				if(data != null && data.version == base.Version)
				{
					data.data[DataKeys[((int)LoadDataType.Enable)]] = LZ4MessagePackSerializer.Serialize(true, CompositeResolver.Instance);
					data.data[DataKeys[((int)LoadDataType.EnableABMX)]] = LZ4MessagePackSerializer.Serialize(true, CompositeResolver.Instance);


					data.version = Version;
				}
			}

			if(data == null)
				data = ctrl?.GetExtendedData(ctrl.IsReloading);


			return data;
		}

		public new PluginData Load(CharaMorpher_Controller ctrl, PluginData data = null)
		{

			data = UpdateVersionFromPrev(ctrl, data);// use if version goes up (i.e. 1->2)

			if(data == null) return null;

			try
			{

				if(data.version != Version) throw new Exception($"Target card data was incorrect version: expected [V{Version}] instead of [V{data.version}]");

				var png = ObjectToByteArray(data.data[DataKeys[((int)LoadDataType.TargetPng)]]);

				if(png == null) throw new Exception("png data does not exist...");

				ctrl.morphEnable = LZ4MessagePackSerializer.Deserialize<bool>
					((byte[])data.data[DataKeys[((int)LoadDataType.Enable)]], CompositeResolver.Instance);

				ctrl.morphEnableABMX = LZ4MessagePackSerializer.Deserialize<bool>
					((byte[])data.data[DataKeys[((int)LoadDataType.EnableABMX)]], CompositeResolver.Instance);


				var values = LZ4MessagePackSerializer.Deserialize<MorphControls>
					((byte[])data.data[DataKeys[((int)LoadDataType.Values)]], CompositeResolver.Instance);

				var data2 = LZ4MessagePackSerializer.Deserialize<MorphData>
					((byte[])data.data[DataKeys[((int)LoadDataType.TargetCard)]], CompositeResolver.Instance);

				var data1 = LZ4MessagePackSerializer.Deserialize<MorphData>
					((byte[])data.data[DataKeys[((int)LoadDataType.OrigSize)]], CompositeResolver.Instance);

				var isCurData = LZ4MessagePackSerializer.Deserialize<bool>
					((byte[])data.data[DataKeys[((int)LoadDataType.HoldsFigureData)]], CompositeResolver.Instance);



				data2.abmx.ForceSplitStatus();//needed since split is not saved 😥

				//	var newValues = values.all.ToDictionary(k => k.Key, v => v.Value.ToDictionary(k => k.Key, v2 => v2.Value.Clone()));

				if(ctrl.IsReloading)//can only be done when reloading 
					ctrl.SoftSaveControls(CanUseCardMorphData);//keep this here

				//	Morph_Util.Logger.LogDebug("DATA 2");
				ctrl.m_data2.Copy(data2);

				values.CorrectAbmxStates();
				ctrl.ctrls2 = values.Clone();

				if(CanUseCardMorphData)
					ctrl.controls.Copy(ctrl.ctrls2);

				//get original 
				data1.abmx.ForceSplitStatus();
				//	Morph_Util.Logger.LogDebug("DATA 1");
				ctrl.m_data1.Copy(data1);
			}
			catch(Exception e)
			{
				Morph_Util.Logger.Log(Error | Message, $"Could not load PluginData:\n{e}\n");
				return null;
			}

			return data;
		}

		public new PluginData Save(CharaMorpher_Controller ctrl, PluginData data = null)
		{
			if(!CharaMorpher_Core.cfg.saveExtData.Value) return null;
			if(data == null)
				data = new PluginData() { version = Version, };

			try
			{
				if(!ctrl.m_data2.abmx.isSplit) throw new Exception("Target card data was not fully initialized");

				data.data[DataKeys[((int)LoadDataType.Enable)]] = LZ4MessagePackSerializer.Serialize(ctrl.morphEnable, CompositeResolver.Instance);
				data.data[DataKeys[((int)LoadDataType.EnableABMX)]] = LZ4MessagePackSerializer.Serialize(ctrl.morphEnableABMX, CompositeResolver.Instance);
				data.data[DataKeys[((int)LoadDataType.HoldsFigureData)]] = LZ4MessagePackSerializer.Serialize(true, CompositeResolver.Instance);

				data.data[DataKeys[((int)LoadDataType.Values)]] = LZ4MessagePackSerializer.Serialize(ctrl.controls, CompositeResolver.Instance);
				data.data[DataKeys[((int)LoadDataType.TargetCard)]] = LZ4MessagePackSerializer.Serialize(ctrl.m_data2, CompositeResolver.Instance);

				if(ctrl.m_data2.main.pngData.IsNullOrEmpty()) throw new Exception("png data does not exist...");
				data.data[DataKeys[((int)LoadDataType.TargetPng)]] = ctrl.m_data2.main.pngData;
				data.data[DataKeys[((int)LoadDataType.OrigSize)]] = LZ4MessagePackSerializer.Serialize(ctrl.m_data1, CompositeResolver.Instance);

			}
			catch(Exception e)
			{
				Morph_Util.Logger.Log(Error | Message, $"Could not save PluginData: \n {e} ");
				return null;
			}

			ctrl.SetExtendedData(data);

			return data;
		}

	}

	/// <inheritdoc/>
	public class SaveLoadManagerV2 : SaveLoadManagerV1
	{
		public new int Version => base.Version + 1;

		public new string[] DataKeys => new[] { "MorphData_values", "MorphData_targetCard", "MorphData_targetPng", "MorphData_ogSize", "MorphData_isCurrenntData" };

		public new enum LoadDataType : int
		{
			Values,
			TargetCard,
			TargetPng,
			OrigSize,
			HoldsFigureData,
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
		protected new PluginData UpdateVersionFromPrev(CharaMorpher_Controller ctrl, PluginData data)
		{

			if(data == null)
				data = ctrl?.GetExtendedData(ctrl.IsReloading);

			if(data == null || data.version != Version)
			{

				data = base.UpdateVersionFromPrev(ctrl, data)?.Copy();

				if(cfg.debug.Value) Logger.LogDebug($"Old version: {data?.version.ToString() ?? "Don't exist..."}");
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
					data.data[DataKeys[((int)LoadDataType.OrigSize)]] = LZ4MessagePackSerializer.Serialize(ctrl.m_data1, CompositeResolver.Instance);
					//Todo: next line can be removed from newer versions (V3+)
					data.data[DataKeys[((int)LoadDataType.HoldsFigureData)]] = LZ4MessagePackSerializer.Serialize(false, CompositeResolver.Instance);

					data.version = Version;
				}
			}

			if(data == null)
				data = ctrl?.GetExtendedData(ctrl.IsReloading);

			//Todo: next 3 lines can be removed from newer versions (V3+)
			if(data != null && !data.data.Keys.Contains(DataKeys[((int)LoadDataType.HoldsFigureData)]))
				data.data[DataKeys[((int)LoadDataType.HoldsFigureData)]] =
					LZ4MessagePackSerializer.Serialize(true, CompositeResolver.Instance);

			return data;
		}

	}

	/// <inheritdoc/>
	public class SaveLoadManagerV1 : SaveLoadManager<CharaMorpher_Controller, PluginData>
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


				public void Populate(CharaMorpher_Controller morphControl, bool morph = false)
				{
					if(!ABMXDependency.IsInTargetVersionRange) return;

					var boneCtrl = morph ? MorphTarget.extraCharacter?.GetComponent<BoneController>() : morphControl?.GetComponent<BoneController>();
					var charaCtrl = morphControl?.ChaControl;

					if(isLoaded) return;
					//Store Bonemod Extended Data
					{//helps get rid of data sooner

						if(!boneCtrl) Morph_Util.Logger.LogDebug("Bone controller doesn't exist");
						if(!charaCtrl) Morph_Util.Logger.LogDebug("Character controller doesn't exist");

						//This is the second dumbest fix
						//(I was changing the player character's bones when this was true ¯\_(ツ)_/¯)
						var data = boneCtrl?.GetExtendedData(!morph);

						var newModifiers = data.ReadBoneModifiers();
						//body bonemods on
						if(morph || BodyBonemodTgl)
							body = new List<BoneModifier>(newModifiers);
						//face bonemods on
						if(morph || FaceBonemodTgl)
							face = new List<BoneModifier>(newModifiers);

						isLoaded = !!boneCtrl;//it can be shortened to just "boneCtrl" if I want
					}

					if(cfg.debug.Value)
					{
						if(morph) Morph_Util.Logger.LogDebug("Character 2:");
						else Morph_Util.Logger.LogDebug("Character 1:");
						foreach(var part in body) Morph_Util.Logger.LogDebug("Bone: " + part.BoneName);
					}

					BoneSplit(morphControl, charaCtrl, morph);
				}

				//split up body & head bones
				public void BoneSplit(CharaMorpher_Controller charaControl, ChaControl bodyCharaCtrl, bool morph = false)
				{
					if(!ABMXDependency.IsInTargetVersionRange) return;

					var ChaControl = charaControl?.GetComponent<ChaControl>();
					var ChaFileControl = ChaControl?.chaFile;

					if(!bodyCharaCtrl?.objHeadBone) return;
					if(isSplit || !isLoaded) return;

					if(cfg.debug.Value) Morph_Util.Logger.LogDebug("Splitting bones apart (this is gonna hurt)");


					var headRoot = bodyCharaCtrl.objHeadBone.transform.parent.parent;

					var headBones = new HashSet<string>(headRoot.GetComponentsInChildren<Transform>().Select(x => x.name)) { /*Additional*/headRoot.name };

					//Load Body
					if(morph || BodyBonemodTgl)
						body.RemoveAll(x => headBones.Contains(x.BoneName));

					//Load face
					if(morph || FaceBonemodTgl)
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

					if(BodyBonemodTgl)
						body?.Clear();
					if(FaceBonemodTgl)
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
				catch(Exception e) { Morph_Util.Logger.LogError("Could not copy character data:\n" + e); }
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

			public void Copy(CharaMorpher_Controller data, bool morph = false)
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
				catch(Exception e) { Morph_Util.Logger.LogError("Could not copy character data:\n" + e); }

				abmx.Populate(data, morph);
			}
		}

		/// <summary>
		/// creates an updated version (can NOT be called on base version)
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override PluginData UpdateVersionFromPrev(CharaMorpher_Controller ctrl, PluginData data)
		{

			if(data == null)
				data = ctrl?.GetExtendedData(ctrl.IsReloading);

			return data;
		}

		public override PluginData Load(CharaMorpher_Controller ctrl, PluginData data = null)
		{ throw new NotImplementedException(); }

		public override PluginData Save(CharaMorpher_Controller ctrl, PluginData data = null) { throw new NotImplementedException(); }

	}
}
