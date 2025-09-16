using System.Collections.Generic;
using UnityEngine;

namespace TheLongWho.Sonic
{
	internal class SonicHelper : MonoBehaviour
	{
		public List<electronicsscript> Electronics = new List<electronicsscript>();

		private void Update()
		{
			foreach (electronicsscript electronic in Electronics)
			{
				if (electronic == null)
				{
					Electronics.Remove(electronic);
					return;
				}

				electronic.jamValue = mainscript.maxJamValue;
			}
		}
	}
}
