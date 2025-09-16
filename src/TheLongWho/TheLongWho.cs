using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheLongWho.Save;
using TheLongWho.Sonic;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.Shell;
using TheLongWho.Utilities;
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
		internal GameObject OverlayShell;
		internal GameObject Interior;
		internal Texture ScreenImage;
		internal AudioClip MaterialiseClip;
		internal AudioClip DematerialiseClip;
		internal AudioClip FlightClip;
		internal GameObject UIButton;
		internal GameObject UIText;
		internal GameObject UIImage;
		internal GameObject Sonic;
		internal AudioClip SonicClip;

		public event Action OnCacheRebuild;
		private float _nextCacheUpdate = 2f;

		private GameObject[] _toLoad = new GameObject[0];

		private bool _hasUIControl = false;
		public event Action OnForceReleaseUIControl;
		public int ScreenWidth;
		public int ScreenHeight;

		internal SonicHelper SonicHelper;

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
			ScreenImage = bundle.LoadAsset<Texture>("screenimage.jpg");

			UIButton = bundle.LoadAsset<GameObject>("uibutton.prefab");
			UIText = bundle.LoadAsset<GameObject>("uitext.prefab");
			UIImage = bundle.LoadAsset<GameObject>("uiimage.prefab");

			MaterialiseClip = bundle.LoadAsset<AudioClip>("mat.wav");
			DematerialiseClip = bundle.LoadAsset<AudioClip>("demat.wav");
			FlightClip = bundle.LoadAsset<AudioClip>("flight.wav");

			Sonic = bundle.LoadAsset<GameObject>("sonicclosed.prefab");
			SonicClip = bundle.LoadAsset<AudioClip>("sonic.wav");

			bundle.Unload(false);
			_areAssetsLoaded = true;

			if (itemdatabase.d.glegycsapo.GetComponent<SonicController>() == null)
			{
				itemdatabase.d.glegycsapo.AddComponent<SonicController>();
				itemdatabase.d.glegycsapo.name += " or Sonic Screwdriver";
			}

			// Create placeholders to show in M-ultiTool mod items category.
			try
			{
				GameObject sonicPlaceholder = new GameObject("SonicPlaceholder");
				sonicPlaceholder.transform.SetParent(mainscript.M.transform);
				sonicPlaceholder.SetActive(false);
				GameObject sonic = new GameObject("Sonic Screwdriver");
				sonic.transform.SetParent(sonicPlaceholder.transform, false);
				sonic.AddComponent<SonicSpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, sonic).ToArray();
			}
			catch (Exception ex)
			{
				Logging.Log($"Failed to create placeholders. Details: {ex}", TLDLoader.Logger.LogLevel.Error);
			}

			GameObject sonicHelperObj = new GameObject("SonicHelper");
			SonicHelper = sonicHelperObj.AddComponent<SonicHelper>();

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
					{
						interior.Shell.Exit();
						return;
					}
				}
				else if (shell != null && shell.CanEnter())
				{
					player.E = "Enter TARDIS";
					player.BcanE = true;

					if (Input.GetKeyDown(KeyCode.E))
					{
						shell.Enter();
						return;
					}

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
				StateManager.LastTardis.Materialisation.Materialise(WorldUtilities.GetGlobalObjectPosition(position), rotation);
			}

			if (_hasUIControl && !mainscript.M.menu.Menu.activeSelf && Input.GetButtonDown("Cancel"))
				ToggleUIControl(false);
		}

		public override void OnGUI()
		{
			// Find screen resolution.
			ScreenWidth = Screen.width;
			ScreenHeight = Screen.height;
			int resX = settingsscript.s.S.IResolutionX;
			int resY = settingsscript.s.S.IResolutionY;
			if (resX != ScreenWidth)
			{
				ScreenWidth = resX;
				ScreenHeight = resY;
			}
		}

		public void ToggleUIControl(bool? force = null)
		{
			if (force.HasValue)
			{
				_hasUIControl = force.Value;
				if (!_hasUIControl)
					OnForceReleaseUIControl?.Invoke();
			}
			else
				_hasUIControl = !_hasUIControl;

			mainscript.M.crsrLocked = !_hasUIControl;
			mainscript.M.SetCursorVisible(_hasUIControl);
			mainscript.M.menu.gameObject.SetActive(!_hasUIControl);
		}
	}
}
