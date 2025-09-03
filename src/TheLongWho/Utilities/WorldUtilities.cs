using UnityEngine;

namespace TheLongWho.Utilities
{
	internal static class WorldUtilities
	{
		/// <summary>
		/// Get global position of an object.
		/// </summary>
		/// <param name="objPos">Object to get global position of</param>
		/// <returns>Vector3 global object position</returns>
		public static Vector3 GetGlobalObjectPosition(Vector3 objPos)
		{
			return new Vector3((float)(-mainscript.M.mainWorld.coord.x + objPos.x), (float)(-mainscript.M.mainWorld.coord.y + objPos.y), (float)(-mainscript.M.mainWorld.coord.z + objPos.z));
		}

		/// <summary>
		/// Get object local position from global.
		/// </summary>
		/// <param name="globalPos">Current global position</param>
		/// <returns>Vector3 local object position</returns>
		public static Vector3 GetLocalObjectPosition(Vector3 globalPos)
		{
			return new Vector3((float)-(-mainscript.M.mainWorld.coord.x - globalPos.x), (float)-(-mainscript.M.mainWorld.coord.y - globalPos.y), (float)-(-mainscript.M.mainWorld.coord.z - globalPos.z));
		}

		// The following teleport methods are shamelessly stolen from beta v2024.11.26b_test.
		public static void TeleportPlayer(Vector3 _upos) => TeleportPlayer(_upos, mainscript.M.player.Tb.eulerAngles);
		public static void TeleportPlayer(Vector3 _upos, Vector3 _rot)
		{
			mainscript.M.player.transform.position = _upos;
			mainscript.M.player.Tb.eulerAngles = new Vector3(0.0f, _rot.y, 0.0f);
		}
		public static void TeleportPlayerWithParent(Vector3 _upos) => mainscript.M.player.transform.root.position += _upos - mainscript.M.player.transform.position;

		public static void TeleportPlayerSafe(Vector3 _upos) => TeleportPlayerSafe(_upos, mainscript.M.player.Tb.eulerAngles);
		public static void TeleportPlayerSafe(Vector3 _upos, Vector3 _rot)
		{
			mainscript.M.player.RB.velocity = Vector3.zero;
			mainscript.M.player.lastVelocity = 0;
			TeleportPlayer(_upos, _rot);
		}
	}
}
