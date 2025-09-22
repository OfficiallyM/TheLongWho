using HarmonyLib;

namespace TheLongWho.Harmony
{
	[HarmonyPatch(typeof(buildingscript), nameof(buildingscript.SpawnStuff))]
	internal static class Patch_Building_SpawnStuff
	{
		private static void Prefix(buildingscript __instance)
		{
			TheLongWho.RaiseOnBuildingItemSpawn(__instance);
		}
	}
}
