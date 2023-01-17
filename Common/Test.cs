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
		public static UnityEvent<string> myStrEvent;
		public static UnityEvent myEvent;
		Test()
		{

			myStrEvent = new StringEvent();
			myEvent = new UnityEvent();

			myStrEvent.AddListener((s) => { task(); });
			myEvent.AddListener(() => { task(); });
		}

		private void task()
		{
			//This is a task
		}

	}

	public class Tester2
	{

		void InvokeTestEvent()
		{

			Test.myStrEvent.Invoke("Hi");
			Test.myEvent.Invoke();

		}

	}
}
