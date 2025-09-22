using System.Collections.Generic;
using TheLongWho.Spawn.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheLongWho.Spawn
{
	public class SpawnManager : MonoBehaviour
	{
		private List<SpawnRule> _spawnRules = new List<SpawnRule>();

		public void RegisterSpawn(SpawnRule spawn) => _spawnRules.Add(spawn);

		public void RegisterProvider(ISpawnProvider provider)
		{
			provider.OnReadyToSpawn += HandleProviderReady;
		}

		public void DeregisterProvider(ISpawnProvider provider)
		{
			provider.OnReadyToSpawn -= HandleProviderReady;
		}

		private void HandleProviderReady(ISpawnProvider provider)
		{
			foreach (var rule in _spawnRules)
			{
				foreach (var location in rule.Locations)
				{
					if (location.LocationType != provider.LocationType)
						continue;

					if (location.AllowedProviderIds != null && location.AllowedProviderIds.Count > 0 && !location.AllowedProviderIds.Contains(provider.ProviderId))
						continue;

					foreach (var pos in provider.GetSpawnPositions())
					{
						if (Random.value <= location.Chance)
						{
							SpawnAtParent(provider.Origin, rule.Prefab, pos, Random.rotation);
						}
					}
				}
			}
		}

		private GameObject SpawnAtParent(Transform parent, GameObject prefab, Vector3 offset, Quaternion rotation)
		{
			var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
			obj.transform.SetParent(parent);
			obj.transform.localPosition = offset;
			obj.transform.localRotation = rotation;
			obj.transform.parent = null;
			return obj;
		}
	}
}
