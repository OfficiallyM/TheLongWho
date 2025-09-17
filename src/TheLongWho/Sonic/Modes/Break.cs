using UnityEngine;

namespace TheLongWho.Sonic.Modes
{
	internal class Break : SonicMode
	{
		public override string Name => "Break";
		public override float EngageTime => 1f;

		private float _nextBreakTime = 0f;

		public override void OnDisengage()
		{
			_nextBreakTime = 0f;
		}

		public override void Tick()
		{
			fpscontroller player = mainscript.M.player;

			bool hasBroken = false;
			if (_nextBreakTime > 0)
			{
				_nextBreakTime -= Time.deltaTime;
				return;
			}

			RaycastHit hitInfo;
			if (Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out hitInfo, mainscript.M.player.FrayRange * 3f, (int)mainscript.M.player.useLayer))
			{
				breakablescript breakable = hitInfo.collider.GetComponentInParent<breakablescript>();
				// Don't break anything with an AI.
				if (breakable != null && breakable.AI == null && breakable.newAI == null)
				{
					breakable.TryBreak(breakable.health);
					hasBroken = true;
				}

				attachablescript attach = hitInfo.collider.GetComponentInParent<attachablescript>();
				if (attach != null && attach.attached)
				{
					attach.Detach();
					hasBroken = true;
				}

				partscript part = hitInfo.collider.GetComponentInParent<partscript>();
				if (part != null && part.slot != null)
				{
					part.FallOFf();
					hasBroken = true;
				}
			}

			if (hasBroken)
				_nextBreakTime = 0.5f;
		}
	}
}
