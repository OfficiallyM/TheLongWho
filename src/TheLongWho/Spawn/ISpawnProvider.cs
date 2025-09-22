using System;
using System.Collections.Generic;
using TheLongWho.Spawn.Core;
using UnityEngine;

namespace TheLongWho.Spawn
{
	public interface ISpawnProvider
	{
		event Action<ISpawnProvider> OnReadyToSpawn;
		IEnumerable<Vector3> GetSpawnPositions();
		SpawnLocationType LocationType { get; }
		string ProviderId { get; }
		Transform Origin { get; }
	}
}
