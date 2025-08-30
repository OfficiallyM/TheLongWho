using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheLongWho.Tardis.Shell;
using UnityEngine;

namespace TheLongWho.Tardis.Interior
{
	internal class InteriorController : MonoBehaviour
	{
		internal ShellController shell;
		internal Transform enterPoint { get; private set; }

		private void Start()
		{
			enterPoint = transform.Find("EnterPoint");
		}
	}
}
