using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLDLoader;

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
