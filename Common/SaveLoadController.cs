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
using static BepInEx.Logging.LogLevel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
		public static byte[] ObjectToByteArray(Object obj)
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

	}

	/// <summary>
	/// saves controls from current data. make a new one if variables change
	/// </summary>
	internal class CurrentSaveLoadController : SaveLoadController
	{
		public override int Version => 1;

		public override string DataKey => "MorphData";

		/*
		 Data that can affect save:
		* enum MorphCalcType
		* class MorphData
		* class MorphData.AMBXSections
		* class MorphConfig 
		* var CharaMorpher_Core.cfg.defaults
		* var CharaMorpher_Core.cfg.controlCategories
		* all I can think of for now
		 */

		public override PluginData Load(CharaCustomFunctionController ctrler, PluginData data)
		{
			//data = base.Load(ctrler,data);// use if version goes up (i.e. 1->2)
			var ctrl = (CharaMorpherController)ctrler;
			if(data == null)
				data = ctrler?.GetExtendedData(ctrl.reloading);

			if(data == null) return null;

			try
			{
				if(data.version != Version) throw new Exception($"Target card data was incorrect version: expected V{Version} instead of V{data.version}");


				var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKey + "_values"], CompositeResolver.Instance);
				var target = LZ4MessagePackSerializer.Deserialize<MorphData>((byte[])data.data[DataKey + "_targetCard"], CompositeResolver.Instance);
				var png = ObjectToByteArray(data.data[DataKey + "_targetPng"]);

				if(png == null) throw new Exception("png data does not exist...");

				target.abmx.ForceSplitStatus();//needed since split is not saved 😥

				ctrl.controls.all = values;
				ctrl.m_data2.Copy(target);
			
			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.Log(Error | Message, $"Could not load PluginData: \n {e} \n {e.StackTrace}");
				return null;
			}

			return data;
		}

		public override PluginData Save(CharaCustomFunctionController ctrler)
		{
			if(CharaMorpher_Core.cfg.saveWithMorph.Value) return null;
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
				CharaMorpher_Core.Logger.Log(Error | Message, $"Could not save PluginData: \n {e} \n {e.StackTrace}");
				return null;
			}
			ctrler.SetExtendedData(data);

			return data;
		}
	}


}
