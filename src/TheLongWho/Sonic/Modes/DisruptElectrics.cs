using UnityEngine;

namespace TheLongWho.Sonic.Modes
{
	internal class DisruptElectrics : SonicMode
	{
		public override string Name => "Disrupt Electrics";

		public override void OnEngage()
		{
			fpscontroller player = mainscript.M.player;
			RaycastHit hitInfo;
			if (Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out hitInfo, mainscript.M.player.FrayRange * 3f, (int)mainscript.M.player.useLayer))
			{
				SonicHelper helper = TheLongWho.I.SonicHelper;
				foreach (electronicsscript electronic in hitInfo.transform.GetComponentsInParent<electronicsscript>())
				{
					if (helper.Electronics.Contains(electronic))
						helper.Electronics.Remove(electronic);
					else
						helper.Electronics.Add(electronic);

					// Small explosion to show something happened.
					explosionscript explosion = Instantiate(mainscript.M.ExplosionBlue).GetComponent<explosionscript>();
					explosion.transform.position = hitInfo.point;
					explosion.explosionForce = 1f;
					explosion.multiplier = 0.5f;
					explosion.Explode();
				}
			}
		}
	}
}
