using System;
using System.Collections.Generic;
using TheLongWho.Extensions;
using TheLongWho.Spawn.Core;
using UnityEngine;

namespace TheLongWho.Spawn
{
	internal class BuildingSpawnProvider : MonoBehaviour, ISpawnProvider
	{
		private buildingscript _building;
		private string _buildingName;
		public event Action<ISpawnProvider> OnReadyToSpawn;
		public string ProviderId
		{
			get { return _buildingName; }
			private set { _buildingName = value; }
		}
		public SpawnLocationType LocationType => SpawnLocationType.Building;
		public Transform Origin => _building?.transform;

		private void Awake()
		{
			_building = GetComponent<buildingscript>();
			_buildingName = _building.name.Prettify().ToLowerInvariant();
		}

		private void Start()
		{
			if (_building == null)
			{
				Destroy(this);
				return;
			}

			TheLongWho.I.SpawnManager.RegisterProvider(this);

			TheLongWho.OnBuildingItemSpawn += SpawnStuff;
		}

		private void OnDisable()
		{
			TheLongWho.I.SpawnManager.DeregisterProvider(this);
		}

		public void SpawnStuff(buildingscript building)
		{
			if (building != _building) return;
			OnReadyToSpawn?.Invoke(this);
		}

		public IEnumerable<Vector3> GetSpawnPositions()
		{
			int posCount = 1;
			switch (_buildingName)
			{
				case "mansion":
					posCount = 3; 
					break;
			}

			List<Vector3> positions = new List<Vector3>();
			for (int i = 0; i < posCount; i++)
			{
				positions.Add(new Vector3
				(
					UnityEngine.Random.Range(-10, 10),
					UnityEngine.Random.Range(1, 10),
					UnityEngine.Random.Range(-10, 10)
				));
			}

			return positions;
		}
	}
}
