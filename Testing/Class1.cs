using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Testing
{
	internal class Class1
	{
		[Obsolete("use InterpolableModel.InterpolableDelegate() instead")]
		public delegate void InterpolableDelegate();

		public delegate void InterpolableDelegate<Data, Peram>();

		public static void Main(string[] args)
		{
			var str = "this is args test";
			var ptrn = "hiat";
			int i = -1;

			Action<object, object> t1=(o,p)=> {
				Console.WriteLine("Thing 1");
			};
			Action<int, float> t2=(o,p)=>{ 
				Console.WriteLine("Thing 2");
			};
			InterpolableDelegate t3=()=> { };
			InterpolableDelegate<int,int> t4=()=> { };


			((InterpolableDelegate)(object)t4).Invoke();
			((InterpolableDelegate<int, int>)(object)t3).Invoke( );

			Console.ReadLine();
		}
	}
}
