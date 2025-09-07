using TheLongWho.Tardis.Shell;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Interior
{
	internal class InteriorController : MonoBehaviour
	{
		internal ShellController Shell;
		internal Transform EnterPoint { get; private set; }
		internal Transform Console { get; private set; }
		internal Transform Rotor { get; private set; }
		internal Canvas ScreenCanvas { get; private set; }

		private void Awake()
		{
			EnterPoint = transform.Find("EnterPoint");
			Console = transform.Find("Console");
			Rotor = transform.Find("Rotor");
			ScreenCanvas = transform.Find("ScreenCanvas")?.GetComponent<Canvas>();
		}

		public void SyncPositionToShell()
		{
			// Offset directly below shell.
			Vector3 offset = Vector3.down * 1000f;

			// Use yaw-only rotation so interior doesn't tilt with shell.
			Quaternion yawOnly = Quaternion.Euler(0f, Shell.transform.eulerAngles.y, 0f);

			// Record player's local position relative to interior.
			fpscontroller player = mainscript.M.player;
			Vector3 localPos = transform.InverseTransformPoint(player.transform.position);
			Quaternion localRot = Quaternion.Inverse(transform.rotation) * player.transform.rotation;

			transform.position = Shell.transform.position + yawOnly * offset;
			transform.rotation = yawOnly;

			// Restore player inside relative to interior.
			if (Shell.IsInside() && !StateManager.InFlight)
				WorldUtilities.TeleportPlayerSafe(transform.TransformPoint(localPos), (transform.rotation * localRot).eulerAngles);
		}
	}
}
