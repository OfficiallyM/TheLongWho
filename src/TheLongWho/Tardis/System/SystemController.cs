using System.Collections.Generic;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.System
{
	internal class SystemController : MonoBehaviour
	{
		//private float _energy = 100f;
		private List<ISystem> _systems = new List<ISystem>();

		public void RegisterSystem(ISystem system)
		{
			_systems.Add(system);
			Logging.Log($"Registered system {system.Name}");
		}

		private void Update()
		{
			foreach (ISystem system in _systems)
			{
				if (!system.IsActive) continue;
				system.Tick();
			}
		}

		private void FixedUpdate()
		{
			foreach (ISystem system in _systems)
			{
				if (!system.IsActive) continue;
				system.FixedTick();
			}
		}

		public void RegisterAllSystems()
		{
			foreach (var sys in GetComponentsInChildren<ISystem>())
			{
				RegisterSystem(sys);
			}
		}
	}
}
