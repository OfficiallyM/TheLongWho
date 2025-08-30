using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheLongWho.Tardis.Interior;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Shell
{
	internal class ShellController : MonoBehaviour
	{
		public static GameObject InteriorPrefab;
		private InteriorController _interior;
		internal Transform exitPoint { get; private set; }

		private void Start()
		{
			SpawnInterior();
			exitPoint = transform.Find("ExitPoint");
		}

		private void SpawnInterior()
		{
			if (_interior != null) return;

			GameObject interior = Instantiate(InteriorPrefab);
			interior.transform.parent = transform;
			interior.transform.localPosition = new Vector3 (0, -100, 0);
			_interior = interior.GetComponent<InteriorController>();
			_interior.shell = this;
		}

		public void Enter()
		{
			WorldUtilities.TeleportPlayerWithParent(_interior.enterPoint.position + Vector3.up * 2f);
		}

		public void Exit()
		{
			WorldUtilities.TeleportPlayerWithParent(exitPoint.position);
		}
	}
}
