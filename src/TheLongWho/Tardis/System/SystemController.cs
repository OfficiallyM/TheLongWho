using System.Collections.Generic;
using System.Linq;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.System
{
	internal class SystemController : MonoBehaviour
	{
		//private float _energy = 100f;
		private List<TardisSystem> _systems = new List<TardisSystem>();

		public void RegisterSystem(TardisSystem system)
		{
			system.Systems = this;
			_systems.Add(system);
			Logging.Log($"Registered system {system.Name}");
		}

		public void RegisterAllSystems()
		{
			foreach (var sys in GetComponentsInChildren<TardisSystem>())
			{
				RegisterSystem(sys);
			}
		}

		public void EnableSystem<T>() where T : TardisSystem
		{
			TardisSystem system = _systems.OfType<T>().FirstOrDefault();
			system?.Activate();
		}

		public void DisableSystem<T>() where T : TardisSystem
		{
			TardisSystem system = _systems.OfType<T>().FirstOrDefault();
			system?.Deactivate();
		}

		private void Update()
		{
			foreach (TardisSystem system in _systems)
			{
				if (!system.IsActive) continue;
				system.Tick();
			}
		}

		private void FixedUpdate()
		{
			foreach (TardisSystem system in _systems)
			{
				if (!system.IsActive) continue;
				system.FixedTick();
			}
		}
	}
}
