using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheLongWho.Sonic
{
	internal sealed class SonicSpawner : MonoBehaviour
	{
		public void Start()
		{
			try
			{
				int num = 0;
				while (!SonicController.ShouldReplace(num) || savedatascript.d.toSaveStuff.ContainsKey(num) || savedatascript.d.data.farStuff.ContainsKey(num) || savedatascript.d.data.nearStuff.ContainsKey(num))
					++num;

				GameObject g = Instantiate(itemdatabase.d.glegycsapo, transform.position, transform.rotation);
				g.GetComponent<tosaveitemscript>().FStart(num);
				mainscript.M.PostSpawn(g);
			}
			catch { }
			Destroy(gameObject, 0.0f);
			gameObject.SetActive(false);
		}
	}
}
