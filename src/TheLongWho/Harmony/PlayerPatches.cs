using HarmonyLib;
using TheLongWho.Player;

namespace TheLongWho.Harmony
{
	[HarmonyPatch(typeof(fpscontroller), nameof(fpscontroller.UpdHandPos))]
	internal static class Patch_Player_UpdHandPos
	{
		private static bool Prefix()
		{
			if (Utilities.StateManager.InFlight)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(fpscontroller), nameof(fpscontroller.Death))]
	internal static class Patch_Player_Death
	{
		private static bool Prefix()
		{
			return !Regeneration.I.TryTriggerRegeneration();
		}
	}
}
