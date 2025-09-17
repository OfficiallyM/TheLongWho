using UnityEngine;

namespace TheLongWho.Sonic.Modes
{
	internal class Break : SonicMode
	{
		public override string Name => "Break";
		public override float EngageTime => 1f;

		public override void OnEngage()
		{
			fpscontroller player = mainscript.M.player;
			RaycastHit hitInfo;
			if (Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out hitInfo, mainscript.M.player.FrayRange * 3f, (int)mainscript.M.player.useLayer))
			{
				breakablescript breakable = hitInfo.collider.GetComponentInParent<breakablescript>();
				// Don't break anything with an AI.
				if (breakable != null && breakable.AI == null && breakable.newAI == null)
					breakable.TryBreak(breakable.health);

				attachablescript attach = hitInfo.collider.GetComponentInParent<attachablescript>();
				if (attach != null && attach.attached)
					attach.Detach();

				partscript part = hitInfo.collider.GetComponentInParent<partscript>();
				if (part != null && part.slot != null)
					part.FallOFf();
			}
		}
	}
}
