using System;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI.Chara;
using KKAPI.Utilities;
using MessagePack;
using MessagePack.Resolvers;
using UniRx;
using static BepInEx.Logging.LogLevel;
namespace Character_Morpher
{
	internal abstract class SaveLoadController
	{

		public abstract int Version { get; }
		public abstract string DataKey { get; }

		public abstract PluginData Save(CharaCustomFunctionController ctrler);
		public abstract PluginData Load(CharaCustomFunctionController ctrler);

	}

	/// <summary>
	/// saves controls from current data. make a new one if variables change
	/// </summary>
	internal class CurrentSaveLoadController : SaveLoadController
	{
		public override int Version => 1;

		public override string DataKey => "MorphData";


		public override PluginData Load(CharaCustomFunctionController ctrler)
		{
			//base.Load(ctrler);// use if version goes up (i.e. 1->2)

			var data = ctrler?.GetExtendedData();
			if(data == null) return null;

			try
			{
				if(data.version != Version) return null;

				var ctrl = (CharaMorpherController)ctrler;
				var values = LZ4MessagePackSerializer.Deserialize<Dictionary<string, Tuple<float, MorphCalcType>>>((byte[])data.data[DataKey + "_values"], ContractlessStandardResolver.Instance);
				var target = LZ4MessagePackSerializer.Deserialize<MorphData>((byte[])data.data[DataKey + "_targetCard"], ContractlessStandardResolver.Instance);
				//var abmx = LZ4MessagePackSerializer.Deserialize<MorphData.AMBXSections>((byte[])data.data[DataKey + "_targetCardABMX"], ContractlessStandardResolver.Instance);

				target.abmx.ForceSplitStatus();//needed since split is not saved

				ctrl.controls.all = values;
				ctrl.m_data2.Copy(target);
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

				data.data.Add(DataKey + "_values", LZ4MessagePackSerializer.Serialize(ctrl.controls.all, ContractlessStandardResolver.Instance));
				data.data.Add(DataKey + "_targetCard", LZ4MessagePackSerializer.Serialize(ctrl.m_data2, ContractlessStandardResolver.Instance));
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
