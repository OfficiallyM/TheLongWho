using System.Collections.Generic;
using System.Reflection;
using TheLongWho.Audio;
using TheLongWho.Common;
using TheLongWho.Sonic.Modes;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Sonic
{
	[DisallowMultipleComponent]
	internal class SonicController : MonoBehaviour
	{
		public AudioController Audio;
		public WorldspaceDisplay Display;

		private GameObject _sonic;
		private Material _tipMaterial;
		private GameObject _tipOverlay;
		private pickupable _pickup;
		private weaponscript _weapon;
		private bool _wasPulling = false;
		private bool _positionFixed = false;

		private List<SonicMode> _modes = new List<SonicMode>();
		private int _currentModeIndex = 0;
		private SonicMode _currentMode;
		private float _holdTimer;
		private bool _modeEngaged;

		private static FieldInfo _pullingField;

		private void Start()
		{
			try
			{
				tosaveitemscript save = GetComponent<tosaveitemscript>();
				if (save == null || !ShouldReplace(save.idInSave))
				{
					enabled = false;
					return;
				}

				// Disable default meshes and colliders.
				foreach (MeshRenderer mesh in gameObject.GetComponentsInChildren<MeshRenderer>())
					mesh.enabled = false;
				foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
					collider.enabled = false;

				_sonic = Instantiate(TheLongWho.I.Sonic);
				_sonic.transform.SetParent(transform, false);
				_sonic.transform.position = Vector3.zero;
				_sonic.transform.localPosition = Vector3.zero + Vector3.down * 0.04f;
				_sonic.transform.localEulerAngles = Vector3.zero;

				Transform tip = _sonic.transform.Find("Model/tip");
				_tipOverlay = _sonic.transform.Find("Model/tip_refr").gameObject;
				Renderer tipRenderer = tip.GetComponent<Renderer>();
				_tipMaterial = tipRenderer.material;
				_tipMaterial.SetFloat("_EMISSION", 1f);
				_tipMaterial.SetColor("_EmissionColor", Color.black);

				_pullingField = typeof(weaponscript).GetField("pulling", BindingFlags.NonPublic | BindingFlags.Instance);

				Audio = gameObject.AddComponent<AudioController>();
				Audio.RegisterClip("buzz", TheLongWho.I.SonicClip);
				Audio.RegisterSource("sonic", _sonic);

				_pickup = GetComponent<pickupable>();

				// Fix pickupable colliders.
				List<layerScript> layerScripts = new List<layerScript>();
				foreach (Collider col in _sonic.GetComponentsInChildren<Collider>())
					layerScripts.Add(col.gameObject.AddComponent<layerScript>());
				_pickup.cols = layerScripts.ToArray();
				_pickup.cols2 = layerScripts.ToArray();

				// Set hold position and rotation.
				_pickup.RHP = new Vector3(0, 0, 0.1f);
				_pickup.RHR = new Vector3(40, 0, 0);

				// Treat as a weapon.
				Destroy(GetComponent<meleeweaponscript>());
				_weapon = gameObject.AddComponent<weaponscript>();
				_weapon.infinite = true;
				_weapon.automatic = true;
				_weapon.Triggers = new weaponscript.OnTriggerPull[0];
				ammoscript ammo = gameObject.AddComponent<ammoscript>();
				ammo.w = _weapon;
				ammo.SetIntAmmo(-1);
				_weapon.ammo = ammo;
				_pickup.weapon = _weapon;

				// Set up display canvas.
				Display = _sonic.AddComponent<WorldspaceDisplay>();

				// Set up sonic modes.
				gameObject.AddComponent<SummonTardis>();
				gameObject.AddComponent<None>();
				gameObject.AddComponent<Scanner>();
				gameObject.AddComponent<DisruptElectrics>();
				gameObject.AddComponent<Break>();
				_modes.AddRange(GetComponents<SonicMode>());
				if (_modes.Count > 0)
					SetMode(0);

				foreach (SonicMode mode in _modes)
					mode.Sonic = this;
			}
			catch (System.Exception ex)
			{
				Logging.Log($"Start exception. Details {ex}");
			}
		}

		private void Update()
		{
			if (_pullingField == null) return;

			fpscontroller player = mainscript.M.player;

			bool isPulling = (bool)_pullingField.GetValue(_weapon);
			_tipMaterial.SetColor("_EmissionColor", isPulling ? Color.white * 2f : Color.black);
			_tipOverlay.SetActive(!isPulling);

			// Only properly engage if equipped. Just picking up the sonic
			// will only light the tip and play the sound.
			bool isHolding = player.inHandP == _pickup;
			bool isEngaged = isPulling && isHolding;

			// Ensure correct hold position when taking out of inventory after a save load.
			if (!_positionFixed && isHolding && _pickup.transform.position != player.RH.TransformPoint(_pickup.RHP))
			{
				player.InvSwitchTo(player.selectedInv);
				_positionFixed = true;
			}

			if (_wasPulling != isPulling)
			{
				if (!isPulling)
					Disengage();
				else
					Audio.Play("sonic", "buzz", true);
			}

			// Track hold duration
			if (isEngaged && _currentMode != null)
			{
				_holdTimer += Time.deltaTime;

				if (!_modeEngaged && _holdTimer >= _currentMode.EngageTime)
				{
					// Only call once per hold.
					_currentMode.OnEngage();
					_modeEngaged = true;
				}

				// If already engaged, let the mode tick.
				if (_modeEngaged)
					_currentMode.Tick();
			}

			if (isHolding)
			{
				if (player.input.reloadDown)
					NextMode();
			}

			_wasPulling = isPulling;
		}

		private void LateUpdate()
		{
			// Force game to use standard crosshair rather
			// than weapon crosshair.
			fpscontroller player = mainscript.M.player;
			if (player.inHandP == _pickup)
				player.kWeapon = false;
		}

		// Prevent sound persisting on inventory switch.
		private void OnDisable()
		{
			Disengage();
		}
		private void OnDestroy()
		{
			Disengage();
		}
		private void OnEnable()
		{
			Disengage();
		}

		private void Disengage()
		{
			try
			{
				// Reset hold state on release.
				_holdTimer = 0f;

				if (_modeEngaged)
				{
					_currentMode?.OnDisengage();
					_modeEngaged = false;
				}
				Audio.Stop("sonic");
			}
			catch { }
		}

		private void OnGUI()
		{
			fpscontroller player = mainscript.M.player;

			if (_currentMode == null || player.inHandP != _pickup)
				return;

			int screenWidth = TheLongWho.I.ScreenWidth;
			int screenHeight = TheLongWho.I.ScreenHeight;

			GUI.Button(new Rect((screenWidth / 2) - 150f, screenHeight - 40f, 300f, 30f), $"[{settingsscript.s.S.inputs[16].GetName()}] Mode: {_currentMode.Name}");
		}

		private void NextMode()
		{
			int next = (_currentModeIndex + 1) % _modes.Count;
			SetMode(next);
		}

		private void SetMode(int index)
		{
			_currentModeIndex = index;
			_currentMode = _modes[_currentModeIndex];
		}

		// 1 in 3 chance to replace.
		public static bool ShouldReplace(int id) => new System.Random(id).Next(3) == 0;
	}
}
