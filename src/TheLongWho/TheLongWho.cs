using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheLongWho.Enemies.Angel;
using TheLongWho.Save;
using TheLongWho.Sonic;
using TheLongWho.Spawn;
using TheLongWho.Spawn.Core;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.Shell;
using TheLongWho.Utilities;
using TLDLoader;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

namespace TheLongWho
{
	public class TheLongWho : Mod
	{
		// Mod meta stuff.
		public override string ID => "M_TheLongWho";
		public override string Name => "The Long Who";
		public override string Author => "M-";
		public override string Version => "0.0.2";
		public override bool LoadInDB => true;
		public override bool EchoToGameLog => true;
		public override bool UseLogger => true;
		public override bool UseHarmony => true;

		internal static TheLongWho I;

		// Shell assets.
		internal GameObject Shell;
		internal GameObject OverlayShell;

		// Interior assets.
		internal GameObject Interior;
		internal Texture ScreenImage;

		// TARDIS audio assets.
		internal AudioClip MaterialiseClip;
		internal AudioClip DematerialiseClip;
		internal AudioClip FlightClip;

		// UI assets.
		internal GameObject UIButton;
		internal GameObject UIText;
		internal GameObject UIImage;

		// Sonic assets.
		internal GameObject Sonic;
		internal AudioClip SonicClip;

		// Enemy assets.
		internal GameObject Angel;

		public event Action OnCacheRebuild;
		private float _nextCacheUpdate = 2f;

		private bool _hasUIControl = false;
		public event Action OnForceReleaseUIControl;
		public int ScreenWidth;
		public int ScreenHeight;

		internal SonicHelper SonicHelper;
		public SpawnManager SpawnManager;

		public static event Action<buildingscript> OnBuildingItemSpawn;

		public TheLongWho()
		{
			I = this;
		}

		public override void DbLoad()
		{
			// Set up any global helpers.
			GameObject helperObj = new GameObject("TheLongWho");
			SonicHelper = helperObj.AddComponent<SonicHelper>();
			SpawnManager = helperObj.AddComponent<SpawnManager>();

			AssetBundle bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(TheLongWho)}.thelongwho"));
			Shell = bundle.LoadAsset<GameObject>("tardis.prefab");
			Shell.AddComponent<ShellController>();
			SaveManager.RegisterPrefab(Shell);
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

			Angel = bundle.LoadAsset<GameObject>("angel.prefab");
			Angel.AddComponent<AngelController>();
			SaveManager.RegisterPrefab(Angel);
			SpawnManager.RegisterSpawn(new SpawnRule(Angel, new List<SpawnLocationRule>()
			{
				new SpawnLocationRule(SpawnLocationType.Building, 0.33f, new List<string>()
				{
					"mansion",
					"yellowfoodkut",
					"nagybenzinkut",
				}),
			}));

			bundle.Unload(false);

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
				UnityEngine.Object.Instantiate(Sonic, sonic.transform, false).transform.Rotate(0f, 180f, 0f);
				sonic.AddComponent<SonicSpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, sonic).ToArray();
				sonic.GetComponentInChildren<Collider>().enabled = false;

				GameObject angelPlaceholder = new GameObject("AngelPlaceholder");
				angelPlaceholder.transform.SetParent(mainscript.M.transform);
				angelPlaceholder.SetActive(false);
				GameObject angel = new GameObject("Weeping Angel");
				angel.transform.SetParent(angelPlaceholder.transform, false);
				UnityEngine.Object.Instantiate(Angel, angel.transform, false);
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, angel).ToArray();
			}
			catch (Exception ex)
			{
				Logging.Log($"Failed to create placeholders. Details: {ex}", TLDLoader.Logger.LogLevel.Error);
			}

			foreach (var building in itemdatabase.d.buildings)
			{
				if (building.GetComponent<BuildingSpawnProvider>() == null)
					building.AddComponent<BuildingSpawnProvider>();
			}

			SaveManager.Init();
			SaveManager.ResetLoadedState();
		}

		public override void OnLoad()
		{
			SaveManager.LoadAll();

			if (mainscript.M.load)
				return;
			// Add sonic component to the correct starter house object.
			foreach (KeyValuePair<int, tosaveitemscript> keyValuePair in savedatascript.d.toSaveStuff)
			{
				if (keyValuePair.Value != null && keyValuePair.Value.id == itemdatabase.d.glegycsapo.GetComponent<tosaveitemscript>().id && keyValuePair.Value.gameObject.GetComponent<SonicController>() == null)
					keyValuePair.Value.gameObject.AddComponent<SonicController>();
			}
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

		internal static void RaiseOnBuildingItemSpawn(buildingscript building)
			=> OnBuildingItemSpawn?.Invoke(building);
	}
}
