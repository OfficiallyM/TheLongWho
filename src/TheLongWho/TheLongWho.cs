using System.Reflection;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.Shell;
using TLDLoader;
using UnityEngine;

namespace TheLongWho
{
	public class TheLongWho : Mod
	{
		// Mod meta stuff.
		public override string ID => "M_TheLongWho";
		public override string Name => "The Long Who";
		public override string Author => "M-";
		public override string Version => "0.0.1";
		public override bool LoadInDB => true;
		public override bool EchoToGameLog => true;
		public override bool UseLogger => true;
		public override bool UseHarmony => true;

		internal static TheLongWho I;

		private static bool _areAssetsLoaded = false;
		internal GameObject Shell;
		internal GameObject Interior;

		public TheLongWho()
		{
			I = this;
		}

		public override void DbLoad()
		{
			if (_areAssetsLoaded) return;
			AssetBundle bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(TheLongWho)}.thelongwho"));
			Shell = bundle.LoadAsset<GameObject>("tardis.prefab");
			Shell.AddComponent<ShellController>();
			Interior = bundle.LoadAsset<GameObject>("type30.prefab");
			Interior.AddComponent<InteriorController>();
			ShellController.InteriorPrefab = Interior;
			
			_areAssetsLoaded = true;
		}

		public override void Update()
		{
			fpscontroller player = mainscript.M.player;
			RaycastHit hitInfo;
			if (Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out hitInfo, mainscript.M.player.FrayRange, (int)mainscript.M.player.useLayer))
			{
				InteriorController interior = hitInfo.transform.GetComponentInParent<InteriorController>();
				ShellController shell = hitInfo.transform.GetComponent<ShellController>();
				if (interior != null && hitInfo.collider.name == "TardisExit" && interior.Shell.CanExit())
				{
					player.E = "Exit TARDIS";
					player.BcanE = true;

					if (Input.GetKeyDown(KeyCode.E))
						interior.Shell.Exit();
				}
				else if (shell != null && shell.CanEnter())
				{
					player.E = "Enter TARDIS";
					player.BcanE = true;

					if (Input.GetKeyDown(KeyCode.E))
						shell.Enter();
				}
				else
				{
					shell = hitInfo.transform.root.GetComponent<InteriorController>()?.Shell;
					shell?.OnLook(hitInfo);
				}
			}

			if (Input.GetKeyDown(KeyCode.Comma))
			{
				Vector3 position = mainscript.M.player.lookPoint + Vector3.up * 0.75f;
				Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, -mainscript.M.player.mainCam.transform.right);

				GameObject.Instantiate(Shell, position, rotation);
			}
		}

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);
		}
	}
}
