using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;


namespace Character_Morpher
{
	public class StringEvent: UnityEvent<string> { }

	public class Test
	{
		public static UnityEvent<string> myEvent;
		Test()
		{

			myEvent = new StringEvent();
		}

	}

	public class Tester2
	{

		void InvokeTestEvent()
		{

			Test.myEvent.Invoke("Hi");

		}

	}
}
