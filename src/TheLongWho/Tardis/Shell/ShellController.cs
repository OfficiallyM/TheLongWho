using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using TheLongWho.Audio;
using TheLongWho.Save;
using TheLongWho.Tardis.Flight;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Shell
{
	internal class ShellController : MonoBehaviour, ISaveable
	{
		public string SaveKey => "Shell";
		public static GameObject InteriorPrefab;
		public InteriorController Interior;
		public AudioController Audio;
		public SaveController SaveController;
		public Transform ExitPoint { get; private set; }
		public seatscript FakeSeat { get; private set; }
		public event Action<RaycastHit> OnLookAt;

		private Material _lampMaterial;
		private Color _lampStartColor;
		private Coroutine _lampFlashRoutine;

		private ShellSave _shellSave = new ShellSave();

		private void Awake()
		{
			SaveController = gameObject.AddComponent<SaveController>();
		}

		private void Start()
		{
			SpawnInterior();
			ExitPoint = transform.Find("ExitPoint");
			Transform seat = transform.Find("Seat");
			FakeSeat = seat.gameObject.AddComponent<seatscript>();
			FakeSeat.sitPos = seat;

			// Register audio.
			Audio = gameObject.AddComponent<AudioController>();
			Audio.RegisterClip("flight", TheLongWho.I.FlightClip);
			Audio.RegisterClip("dematerialise", TheLongWho.I.DematerialiseClip);
			Audio.RegisterClip("materialise", TheLongWho.I.MaterialiseClip);
			Audio.RegisterSource("shell", gameObject);

			// This is required to keep the TARDIS in sync with the world when it moves.
			visszarako visszarako = gameObject.AddComponent<visszarako>();
			visszarako.importantUnderMapLook = true;
			visszarako.RB = GetComponent<Rigidbody>();
			visszarako.rb = true;

			// Add a dummy tosaveitemscript to allow M-ultiTool to delete the TARDIS.
			gameObject.AddComponent<tosaveitemscript>();

			// Set up lamp.
			Transform lamp = transform.Find("Base/Pillars/Ceiling/Roof/Lamp Base/Lamp Bottom/Lamp Lens");
			Renderer lampRenderer = lamp.GetComponent<Renderer>();
			_lampMaterial = lampRenderer.material;
			_lampMaterial.SetFloat("_EMISSION", 1f);
			_lampStartColor = _lampMaterial.color;

			// Set up all systems.
			gameObject.AddComponent<FlightSystem>();
			
			// System controller is added last so it automatically registers all of the systems.
			gameObject.AddComponent<SystemController>().RegisterAllSystems();

			SaveController.RefetchSaveables();

			// Player saved inside, spawn them outside.
			if (_shellSave.IsInside)
				WorldUtilities.TeleportPlayerSafe(GetSafeExitPoint());

			// Trigger a manual save.
			SaveManager.Save(SaveController, true);
		}

		private void OnDestroy()
		{
			// Destroy interior with shell.
			Destroy(Interior.gameObject);
		}

		public object GetSaveData() => _shellSave;
		
		public void LoadSaveData(object data)
		{
			ShellSave shellSave = (data as JObject)?.ToObject<ShellSave>();
			if (shellSave == null) return;
			_shellSave = shellSave;
		}

		public void OnLook(RaycastHit hit)
		{
			OnLookAt?.Invoke(hit);
		}

		public void Enter()
		{
			if (!CanEnter()) return;
			_shellSave.IsInside = true;
			SaveManager.Save(SaveController, true);
			Interior.SyncPositionToShell();
			WorldUtilities.TeleportPlayer(Interior.EnterPoint.position + Vector3.up * 2f);
		}

		public void Exit()
		{
			if (!CanExit()) return;
			_shellSave.IsInside = false;
			SaveManager.Save(SaveController, true);
			WorldUtilities.TeleportPlayer(GetSafeExitPoint());
		}

		public bool CanEnter()
		{
			if (!Interior.gameObject.activeSelf) return false;
			return true;
		}

		public bool CanExit()
		{
			return true;
		}

		public void StartLampFlash(float speed = 2f)
		{
			if (_lampFlashRoutine != null)
				StopCoroutine(_lampFlashRoutine);
			_lampFlashRoutine = StartCoroutine(LampFlashRoutine(speed));
		}

		public void StopLampFlash()
		{
			if (_lampFlashRoutine != null)
			{
				StopCoroutine(_lampFlashRoutine);
				_lampFlashRoutine = null;
			}

			_lampMaterial.SetColor("_EmissionColor", Color.black);
			_lampMaterial.color = _lampStartColor;
		}

		private void SpawnInterior()
		{
			if (Interior != null) return;

			GameObject interior = Instantiate(InteriorPrefab);
			Interior = interior.GetComponent<InteriorController>();
			Interior.Shell = this;
			Interior.SyncPositionToShell();
		}

		private Vector3 GetSafeExitPoint()
		{
			Vector3 exitPos = ExitPoint.position;

			if (Vector3.Dot(ExitPoint.up, Vector3.up) < 0.1f)
				exitPos += Vector3.up * 3f;

			return exitPos;
		}

		private IEnumerator LampFlashRoutine(float flashSpeed)
		{
			float t = 0f;

			while (true)
			{
				t += Time.deltaTime * flashSpeed;
				float lerp = Mathf.PingPong(t, 1f);

				Color current = Color.Lerp(Color.grey, Color.white, lerp);
				_lampMaterial.SetColor("_EmissionColor", current);
				current = Color.Lerp(_lampStartColor, Color.white, lerp);
				_lampMaterial.color = current;

				yield return null;
			}
		}
	}
}
