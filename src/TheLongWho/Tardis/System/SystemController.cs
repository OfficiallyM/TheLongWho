using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TheLongWho.Extensions;
using TheLongWho.Save;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.System
{
	internal class SystemController : MonoBehaviour, ISaveable
	{
		public string SaveKey => "Systems";
		public bool HasRegisterFinished = false;

		private bool _hasLoadFinished = false;

		//private float _energy = 100f;
		private List<TardisSystem> _systems = new List<TardisSystem>();
		private SystemSave _systemSave = new SystemSave();

		public object GetSaveData() => _systemSave;

		public void LoadSaveData(object data)
		{
			SystemSave systemSave = (data as JObject)?.ToObject<SystemSave>();
			if (systemSave == null)
			{
				_hasLoadFinished = true;
				return;
			}
			_systemSave = systemSave;

			foreach (KeyValuePair<string, bool> state in _systemSave.States)
			{
				TardisSystem system = GetByName(state.Key);
				if (system == null) continue;
				if (state.Value)
					system.Activate();
				else
					system.Deactivate();
			}

			_hasLoadFinished = true;
		}

		public void RegisterSystem(TardisSystem system)
		{
			system.Systems = this;
			_systems.Add(system);

			if (system.IsActiveByDefault)
				system.Activate();
			Logging.Log($"Registered system {system.Name}");
		}

		public void RegisterAllSystems()
		{
			foreach (var sys in GetComponentsInChildren<TardisSystem>())
			{
				RegisterSystem(sys);
			}

			HasRegisterFinished = true;
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

		public List<TardisSystem> GetScreenControlSystems()
		{
			List<TardisSystem> output = new List<TardisSystem>();
			foreach (TardisSystem system in _systems)
			{
				if (system.IsScreenControllable)
					output.Add(system);
			}
			return output;
		}

		public TardisSystem GetByName(string name)
		{
			name = name.ToMachineName();

			foreach (TardisSystem system in _systems)
			{
				if (system.Name.ToMachineName() == name)
					return system;
			}
			return null;
		}

		public void UpdateSaveState(TardisSystem system, bool isActive)
		{
			if (!_hasLoadFinished || !system.IsScreenControllable) return;
			string name = system.Name.ToMachineName();
			if (_systemSave.States.ContainsKey(name))
				_systemSave.States[name] = isActive;
			else
				_systemSave.States.Add(name, isActive);
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
