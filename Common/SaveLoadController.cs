using System;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.Chara;
using KKAPI.Utilities;
using MessagePack;
using MessagePack.Unity;
using MessagePack.Resolvers;
using UniRx;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using static BepInEx.Logging.LogLevel;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Character_Morpher
{
	public abstract class SaveLoadController
	{
		public SaveLoadController()
		{

			CompositeResolver.Register(
				UnityResolver.Instance,
				BuiltinResolver.Instance,
				StandardResolver.Instance,

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


		public abstract int Version { get; }
		public abstract string DataKey { get; }

		public abstract PluginData Save(CharaCustomFunctionController ctrler);
		public abstract PluginData Load(CharaCustomFunctionController ctrler, PluginData data);
		protected abstract PluginData UpdateVersionFromPrev(CharaCustomFunctionController ctrler, PluginData data);

	}

	/// <summary>
	/// saves controls from current data. make a new one if variables change
	/// </summary>
	internal class CurrentSaveLoadController : SaveLoadControllerV1
	{
		public override int Version => base.Version + 1;

		public override string DataKey => "MorphData";


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

		/// <summary>
		/// creates an updated version 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override PluginData UpdateVersionFromPrev(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;

			
			if(data?.version != Version)
			{
				data = base.Load(ctrler, data).Copy();


				//last version
				var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKey + "_values"], CompositeResolver.Instance);
				//var target = LZ4MessagePackSerializer.Deserialize<MorphData>((byte[])data.data[DataKey + "_targetCard"], CompositeResolver.Instance);
				//var png = ObjectToByteArray(data.data[DataKey + "_targetPng"]);


				var newValues = new Dictionary<string, Dictionary<string, Tuple<float, MorphCalcType>>>() { { "(Default)", values } };
				data.data.Add(DataKey + "_values", LZ4MessagePackSerializer.Serialize(newValues, CompositeResolver.Instance));
				//			ctrl.m_data2.Copy(target);

				data.version= Version;
			}

			if(data == null)
				data = ctrler?.GetExtendedData(ctrl.reloading);

			return data;
		}

		public override PluginData Load(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;

			data = UpdateVersionFromPrev(ctrler, data);// use if version goes up (i.e. 1->2)

			if(data == null) return null;

			try
			{
				if(data.version != Version) throw new Exception($"Target card data was incorrect version: expected [V{Version}] instead of [V{data.version}]");


				var values = LZ4MessagePackSerializer.Deserialize
					<Dictionary<string, Dictionary<string, Tuple<float, MorphCalcType>>>>
					((byte[])data.data[DataKey + "_values"], CompositeResolver.Instance);
				var target = LZ4MessagePackSerializer.Deserialize<MorphData>
					((byte[])data.data[DataKey + "_targetCard"], CompositeResolver.Instance);
				var png = ObjectToByteArray(data.data[DataKey + "_targetPng"]);

				if(png == null) throw new Exception("png data does not exist...");

				target.abmx.ForceSplitStatus();//needed since split is not saved 😥

				ctrl.controls.all = values;
				ctrl.m_data2.Copy(target);

			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.Log(Error | Message, $"Could not load PluginData: \n {e} ");
				return null;
			}

			return data;
		}

		public override PluginData Save(CharaCustomFunctionController ctrler)
		{
			if(!CharaMorpher_Core.cfg.saveAsMorphData.Value) return null;
			PluginData data = new PluginData() { version = Version, };
			try
			{
				var ctrl = (CharaMorpherController)ctrler;
				if(!ctrl.m_data2.abmx.isSplit) throw new Exception("Target card data was not fully initialized");

				data.data.Add(DataKey + "_values", LZ4MessagePackSerializer.Serialize(ctrl.controls.all, CompositeResolver.Instance));
				data.data.Add(DataKey + "_targetCard", LZ4MessagePackSerializer.Serialize(ctrl.m_data2, CompositeResolver.Instance));


				if(ctrl.m_data2.main.pngData.IsNullOrEmpty()) throw new Exception("png data does not exist...");
				data.data.Add(DataKey + "_targetPng", ctrl.m_data2.main.pngData);
			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.Log(Error | Message, $"Could not save PluginData: \n {e} ");
				return null;
			}
			ctrler.SetExtendedData(data);

			return data;
		}

	}


	internal class SaveLoadControllerV1 : SaveLoadController
	{
		public override int Version =>/*base.Version + */1;

		public override string DataKey => "MorphData";

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

		/// <summary>
		/// creates an updated version 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override PluginData UpdateVersionFromPrev(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;

			if(data == null)
				data = ctrler?.GetExtendedData(ctrl.reloading);

			return data;
		}

		public override PluginData Load(CharaCustomFunctionController ctrler, PluginData data)
		{
			var ctrl = (CharaMorpherController)ctrler;

			data = UpdateVersionFromPrev(ctrler, data);// use if version goes up (i.e. 1->2)

			if(data == null) return null;

			try
			{
				if(data.version != Version) throw new Exception($"Target card data was incorrect version: expected [V{Version}] instead of [V{data.version}]");


				var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKey + "_values"], CompositeResolver.Instance);
				var target = LZ4MessagePackSerializer.Deserialize<MorphData>((byte[])data.data[DataKey + "_targetCard"], CompositeResolver.Instance);
				var png = ObjectToByteArray(data.data[DataKey + "_targetPng"]);

				if(png == null) throw new Exception("png data does not exist...");

				//	target.abmx.ForceSplitStatus();//needed since split is not saved 😥
				//
				//	ctrl.controls.all = values;
				//	ctrl.m_data2.Copy(target);

			}
			catch(Exception e)
			{
				//CharaMorpher_Core.Logger.Log(Error | Message, $"Could not load PluginData: \n {e} ");
				return null;
			}

			return data;
		}

		public override PluginData Save(CharaCustomFunctionController ctrler)
		{
			if(!CharaMorpher_Core.cfg.saveAsMorphData.Value) return null;
			PluginData data = new PluginData() { version = Version, };
			try
			{
				var ctrl = (CharaMorpherController)ctrler;
				if(!ctrl.m_data2.abmx.isSplit) throw new Exception("Target card data was not fully initialized");

				data.data.Add(DataKey + "_values", LZ4MessagePackSerializer.Serialize(ctrl.controls.all, CompositeResolver.Instance));
				data.data.Add(DataKey + "_targetCard", LZ4MessagePackSerializer.Serialize(ctrl.m_data2, CompositeResolver.Instance));


				if(ctrl.m_data2.main.pngData.IsNullOrEmpty()) throw new Exception("png data does not exist...");
				data.data.Add(DataKey + "_targetPng", ctrl.m_data2.main.pngData);
			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.Log(Error | Message, $"Could not save PluginData: \n {e} ");
				return null;
			}
			ctrler.SetExtendedData(data);

			return data;
		}

	}


}
