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
		 
		public static void Main(string[] args)
		{
			var str = "this is args test";
			var ptrn = "hiat";
			int i = -1;

			double a1 = 60.312f;
			float a2 = (float)60;

			Console.WriteLine($"{a1}:{a2}");
			
			Console.ReadLine();
		}
	}
}
