using System.Collections.Generic;
using TheLongWho.Common;
using TheLongWho.Extensions;
using TheLongWho.Player;
using UnityEngine;

namespace TheLongWho.Sonic.Modes
{
	internal class SelfScanner : SonicMode
	{
		public override string Name => "Scan Self";

		public override void OnEngage()
		{
			fpscontroller player = mainscript.M.player;
			List<string> text = new List<string>();

			text.Add($"Species: {(Regeneration.I.CanRegenerate() ? "Time Lord" : "Human")}");
			if (Regeneration.I.CanRegenerate())
			{
				text.Add($"Current regeneration: {Regeneration.I.GetCurrentRegeneration()}");
				text.Add($"Remaining regenerations: {Regeneration.I.GetRegenerations()}");
			}

			survivalscript survival = player.survival;
			float hpPercent = survival.hp / survival.maxHp * 100;
			string status = "WARNING: Life signs failing";
			if (hpPercent > 90)
				status = "Stable";
			else if (hpPercent > 75)
				status = "Minor injuries";
			else if (hpPercent > 50)
				status = "Injured";
			else if (hpPercent > 25)
				status = "Severe injuries";
			else if (hpPercent > 10)
				status = "Critical injuries";
			text.Add($"Vital signs: {status}");

			Sonic.Display.RenderMessage(new WorldspaceDisplay.Message(text));
		}
	}
}
