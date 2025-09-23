using System.Collections.Generic;
using TheLongWho.Common;
using TheLongWho.Extensions;
using UnityEngine;

namespace TheLongWho.Sonic.Modes
{
	internal class Scanner : SonicMode
	{
		public override string Name => "Scanner";

		public override void OnEngage()
		{
			fpscontroller player = mainscript.M.player;
			RaycastHit hitInfo;
			if (Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out hitInfo, float.MaxValue, (int)mainscript.M.player.useLayer))
			{
				List<string> text = new List<string>();
				Transform root = hitInfo.transform.root.GetComponentInChildren<Rigidbody>()?.transform ?? hitInfo.transform;

				string name = root.name.Prettify();
				if (name.StartsWith("G_") || name == "GameObject") return;

				text.Add($"Object: {name}");
				text.Add($"Distance: {Vector3.Distance(root.position, mainscript.M.player.transform.position).ToString("F2")}m");

				breakablescript breakable = root.GetComponentInParent<breakablescript>();
				if (breakable != null)
					text.Add($"Health: {breakable.health}");

				partconditionscript[] parts = root.GetComponentsInChildren<partconditionscript>();
				if (parts.Length > 0)
				{
					float total = 0f;
					foreach (partconditionscript part in parts)
					{
						total += part.state;
					}

					float average = total / parts.Length;
					float conditionPercent = (1f - (average / 4f)) * 100f;
					int conditionDisplay = Mathf.RoundToInt(conditionPercent);

					text.Add($"Condition: {conditionDisplay}%");
				}

				Sonic.Display.RenderMessage(new WorldspaceDisplay.Message(text));
			}
		}
	}
}
