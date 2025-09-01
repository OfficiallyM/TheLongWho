using HarmonyLib;

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
}
