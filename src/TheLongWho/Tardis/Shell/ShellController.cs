using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheLongWho.Tardis.Flight;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Shell
{
	internal class ShellController : MonoBehaviour
	{
		public static GameObject InteriorPrefab;
		public InteriorController Interior;
		internal Transform exitPoint { get; private set; }
		internal seatscript fakeSeat { get; private set; }

		private void Start()
		{
			SpawnInterior();
			exitPoint = transform.Find("ExitPoint");
			Transform seat = transform.Find("Seat");
			fakeSeat = seat.gameObject.AddComponent<seatscript>();
			fakeSeat.sitPos = seat;

			// This is required to keep the TARDIS in sync with the world when it moves.
			visszarako visszarako = gameObject.AddComponent<visszarako>();
			visszarako.importantUnderMapLook = true;
			visszarako.RB = GetComponent<Rigidbody>();
			visszarako.rb = true;

			// Set up all systems.
			gameObject.AddComponent<FlightSystem>();
			
			// System controller is added last so it automatically registers all of the systems.
			gameObject.AddComponent<SystemController>().RegisterAllSystems();
		}

		private void SpawnInterior()
		{
			if (Interior != null) return;

			GameObject interior = Instantiate(InteriorPrefab);
			interior.transform.parent = transform;
			interior.transform.localPosition = new Vector3 (0, -100, 0);
			Interior = interior.GetComponent<InteriorController>();
			Interior.shell = this;
		}

		public void Enter()
		{
			if (!CanEnter()) return;
			WorldUtilities.TeleportPlayer(Interior.enterPoint.position + Vector3.up * 2f);
		}

		public void Exit()
		{
			if (!CanExit()) return;
			WorldUtilities.TeleportPlayer(exitPoint.position);
		}

		public bool CanEnter()
		{
			if (!Interior.gameObject.activeSelf) return false;
			return true;
		}

		public bool CanExit()
		{
			return true;
		}
	}
}
