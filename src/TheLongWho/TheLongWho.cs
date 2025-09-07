using System;
using System.Collections.Generic;
using System.Reflection;
using TheLongWho.Save;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.Shell;
using TheLongWho.Utilities;
using TLDLoader;
using UnityEngine;
using static settingsscript;

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
		internal GameObject OverlayShell;
		internal GameObject Interior;
		internal AudioClip MaterialiseClip;
		internal AudioClip DematerialiseClip;
		internal AudioClip FlightClip;
		internal GameObject UIButton;

		public event Action OnCacheRebuild;
		private float _nextCacheUpdate = 2f;

		private GameObject[] _toLoad = new GameObject[0];

		public TheLongWho()
		{
			I = this;
		}

		public override void DbLoad()
		{
			if (_areAssetsLoaded) return;
			List<GameObject> toLoad = new List<GameObject>();
			AssetBundle bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(TheLongWho)}.thelongwho"));
			foreach (string asset in bundle.GetAllAssetNames())
				Logging.Log(asset);
			Shell = bundle.LoadAsset<GameObject>("tardis.prefab");
			Shell.AddComponent<ShellController>();
			toLoad.Add(Shell);
			OverlayShell = bundle.LoadAsset<GameObject>("tardis overlay.prefab");

			Interior = bundle.LoadAsset<GameObject>("type30.prefab");
			Interior.AddComponent<InteriorController>();
			ShellController.InteriorPrefab = Interior;

			UIButton = bundle.LoadAsset<GameObject>("uibutton.prefab");

			MaterialiseClip = bundle.LoadAsset<AudioClip>("mat.wav");
			DematerialiseClip = bundle.LoadAsset<AudioClip>("demat.wav");
			FlightClip = bundle.LoadAsset<AudioClip>("flight.wav");

			bundle.Unload(false);
			_areAssetsLoaded = true;

			_toLoad = toLoad.ToArray();
			SaveManager.Init();
		}

		public override void OnLoad()
		{
			SaveManager.LoadAll(_toLoad);
		}

		public override void Update()
		{
			// Handle cache rebuild event.
			_nextCacheUpdate -= Time.unscaledDeltaTime;
			if (_nextCacheUpdate <= 0)
			{
				OnCacheRebuild?.Invoke();
				_nextCacheUpdate = 2f;
			}

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
				Vector3 directionToPlayer = (mainscript.M.player.transform.position - position).normalized;
				directionToPlayer.y = 0;
				Quaternion rotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);

				GameObject.Instantiate(Shell, position, rotation);
			}

			if (Input.GetKeyDown(KeyCode.Period) && StateManager.LastTardis != null)
			{
				Vector3 position = mainscript.M.player.lookPoint;
				Vector3 directionToPlayer = (mainscript.M.player.transform.position - position).normalized;
				directionToPlayer.y = 0;
				Quaternion rotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
				StateManager.LastTardis.Materialisation.Materialise(position, rotation);
			}
		}

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);
		}
	}
}
