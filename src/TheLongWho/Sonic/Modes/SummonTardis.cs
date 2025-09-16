using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.Materialisation;
using TheLongWho.Utilities;
using UnityEngine;
using System.Collections;

namespace TheLongWho.Sonic.Modes
{
	internal class SummonTardis : SonicMode
	{
		public override string Name => "Summon TARDIS";

		public override void OnEngage()
		{
			StartCoroutine(SpawnSummon());
		}

		private IEnumerator SpawnSummon()
		{
			Vector3 position = mainscript.M.player.lookPoint + Vector3.up * 0.75f;
			Vector3 directionToPlayer = (mainscript.M.player.transform.position - position).normalized;
			directionToPlayer.y = 0;
			Quaternion rotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);

			if (StateManager.LastTardis == null)
			{
				ShellController shell = GameObject.Instantiate(TheLongWho.I.Shell, position + Vector3.down * 20f, rotation).GetComponent<ShellController>();

				while (shell.Materialisation == null) yield return null;
				shell.Materialisation.Dematerialise(MaterialisationSystem.Speed.Instant, false);
			}

			StateManager.LastTardis.Materialisation.Materialise(WorldUtilities.GetGlobalObjectPosition(position), rotation);
		}
	}
}
