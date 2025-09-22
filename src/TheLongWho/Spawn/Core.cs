using System.Collections.Generic;
using UnityEngine;

namespace TheLongWho.Spawn.Core
{
	public class SpawnRule
	{
		public GameObject Prefab;
		public List<SpawnLocationRule> Locations;

		public SpawnRule(GameObject prefab, List<SpawnLocationRule> locations)
		{
			Prefab = prefab;
			Locations = locations;
		}
	}

	public class SpawnLocationRule
	{
		public SpawnLocationType LocationType;
		public float Chance;
		public List<string> AllowedProviderIds;

		public SpawnLocationRule(SpawnLocationType locationType, float chance)
		{
			LocationType = locationType;
			Chance = chance;
			AllowedProviderIds = new List<string>();
		}

		public SpawnLocationRule(SpawnLocationType locationType, float chance, List<string> allowedProviderIds)
		{
			LocationType = locationType;
			Chance = chance;
			AllowedProviderIds = allowedProviderIds;
		}
	}
	
	public enum SpawnLocationType
	{
		Building,
		//Road, // Potential future addition, spawning enemies per road bone.
	}
}
