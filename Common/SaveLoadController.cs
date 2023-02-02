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

namespace Character_Morpher
{
	internal abstract class SaveLoadController
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


		public override PluginData Load(CharaCustomFunctionController ctrler, PluginData data)
		{
			//base.Load(ctrler);// use if version goes up (i.e. 1->2)
			if(data == null)
				data = ctrler?.GetExtendedData(true);
			if(data == null)
				data = ctrler?.GetExtendedData(false);

			if(data == null) return null;

			try
			{
				if(data.version != Version) return null;

				var ctrl = (CharaMorpherController)ctrler;
				var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKey + "_values"], CompositeResolver.Instance);
				var target = LZ4MessagePackSerializer.Deserialize<MorphData>((byte[])data.data[DataKey + "_targetCard"], CompositeResolver.Instance);
				//	var png = LZ4MessagePackSerializer.Deserialize<byte[]>((byte[])data.data[DataKey + "_targetPng"], CompositeResolver.Instance);

				target.abmx.ForceSplitStatus();//needed since split is not saved 😥

				ctrl.controls.all = values;
				ctrl.m_data2.Copy(target);
				//	ctrl.m_data2.main.pngData = png;
				//	ctrl.m_data2.main.GetChaControl().Load();

			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.Log(Error | Message, "Could not load PluginData: " + e);
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
				data.data.Add(DataKey + "_targetPng", LZ4MessagePackSerializer.Serialize(ctrl.m_data2.main.pngData, CompositeResolver.Instance));
				//	data.data.Add(DataKey + "_targetCardABMX", LZ4MessagePackSerializer.Serialize(ctrler.m_data2.abmx, ContractlessStandardResolver.Instance));

				//ContractlessStandardResolver.Instance;
			}
			catch(Exception e)
			{
				CharaMorpher_Core.Logger.Log(Error | Message, "Could not save PluginData: " + e);
				return null;
			}
			ctrler.SetExtendedData(data);

			return data;
		}
	}


}
