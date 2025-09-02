using TheLongWho.Tardis.Shell;
using UnityEngine;

namespace TheLongWho.Tardis.Interior
{
	internal class InteriorController : MonoBehaviour
	{
		internal ShellController Shell;
		internal Transform EnterPoint { get; private set; }

		private void Start()
		{
			EnterPoint = transform.Find("EnterPoint");
		}

		public void SyncPositionToShell()
		{
			// Offset directly below shell.
			Vector3 offset = Vector3.down * 1000f;

			// Use yaw-only rotation so interior doesn't tilt with shell.
			Quaternion yawOnly = Quaternion.Euler(0f, Shell.transform.eulerAngles.y, 0f);

			transform.position = Shell.transform.position + yawOnly * offset;
			transform.rotation = yawOnly;
		}
	}
}
