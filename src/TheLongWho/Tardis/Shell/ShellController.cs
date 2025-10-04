using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using TheLongWho.Audio;
using TheLongWho.Common;
using TheLongWho.Save;
using TheLongWho.Tardis.Flight;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.Materialisation;
using TheLongWho.Tardis.PerceptionFilter;
using TheLongWho.Tardis.Screen;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Shell
{
	internal class ShellController : MonoBehaviour, ISaveable
	{
		public string SaveKey => "Shell";
		public static GameObject InteriorPrefab;
		public static event Action<ShellController> OnTardisEnter;
		public static event Action<ShellController> OnTardisExit;
		public InteriorController Interior;
		public AudioController Audio;
		public SaveController SaveController;
		public MaterialisationSystem Materialisation;
		public Transform ExitPoint { get; private set; }
		public seatscript FakeSeat { get; private set; }
		public event Action<RaycastHit> OnLookAt;
		public Collider[] Colliders;
		public Renderer[] Renderers;
		public GameObject OverlayShell;
		public WorldspaceDisplay Display;

		private Material _lampMaterial;
		private Material _overlayLampMaterial;
		private Color _lampStartColor;
		private Coroutine _lampFlashRoutine;
		private Coroutine _overlayLampFlashRoutine;
		private ShellSave _shellSave = new ShellSave();

		private void Awake()
		{
			SaveController = gameObject.AddComponent<SaveController>();
		}

		private void Start()
		{
			// Find colliders and renderers before interior is created to avoid
			// finding anything interior-related.
			Colliders = GetComponentsInChildren<Collider>();
			Renderers = GetComponentsInChildren<Renderer>();
			OverlayShell = Instantiate(TheLongWho.I.OverlayShell, transform);
			OverlayShell.SetActive(false);

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
			Audio.RegisterSource("interior", Interior.Console.gameObject);

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

			// Set up overlay lamp.
			Transform overlayLamp = OverlayShell.transform.Find("Base/Pillars/Ceiling/Roof/Lamp Base/Lamp Bottom/Lamp Lens");
			Renderer overlayLampRenderer = overlayLamp.GetComponent<Renderer>();
			_overlayLampMaterial = overlayLampRenderer.material;
			_overlayLampMaterial.SetFloat("_EMISSION", 1f);

			// Set up all systems.
			gameObject.AddComponent<FlightSystem>();
			Materialisation = gameObject.AddComponent<MaterialisationSystem>();
			gameObject.AddComponent<ScreenSystem>();
			gameObject.AddComponent<PerceptionFilterSystem>();

			// System controller is added last so it automatically registers all of the systems.
			gameObject.AddComponent<SystemController>().RegisterAllSystems();
			SaveController.RefetchSaveables();

			// Player saved inside, spawn them outside.
			if (_shellSave.IsInside)
			{
				WorldUtilities.TeleportPlayerSafe(GetSafeExitPoint());
				_shellSave.IsInside = false;
			}

			// Set up help display.
			Display = gameObject.AddComponent<WorldspaceDisplay>();
			Display.SetPosition(new Vector3(0, 2.9f, 0));
			Display.SetMaxWidth(500);
			Display.SetFontSize(40);

			// Trigger a manual save.
			SaveManager.Save(SaveController, true);

			StateManager.LastTardis = this;
		}

		private void OnDestroy()
		{
			// Destroy interior with shell.
			if (Interior != null)
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
			Interior.SyncPositionToShell();
			WorldUtilities.TeleportPlayer(Interior.EnterPoint.position + Vector3.up * 2f);
			StateManager.LastTardis = this;
			_shellSave.IsInside = true;
			SaveManager.Save(SaveController, true);

			OnTardisEnter?.Invoke(this);
		}

		public void Exit()
		{
			if (!CanExit()) return;
			WorldUtilities.TeleportPlayer(GetSafeExitPoint());
			_shellSave.IsInside = false;
			SaveManager.Save(SaveController, true);
			OnTardisExit?.Invoke(this);
		}

		public bool CanEnter()
		{
			if (!Interior.gameObject.activeSelf) return false;
			if (Materialisation.CurrentState == MaterialisationSystem.State.Dematerialised) return false;
			return true;
		}

		public bool CanExit()
		{
			if (Materialisation.CurrentState == MaterialisationSystem.State.Dematerialised) return false;
			return true;
		}

		public bool IsInside() => _shellSave.IsInside;

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

		public void StartOverlayLampFlash(float speed = 2f)
		{
			if (_overlayLampFlashRoutine != null)
				StopCoroutine(_overlayLampFlashRoutine);
			_overlayLampFlashRoutine = StartCoroutine(OverlayLampFlashRoutine(speed));
		}

		public void StopOverlayLampFlash()
		{
			if (_overlayLampFlashRoutine != null)
			{
				StopCoroutine(_overlayLampFlashRoutine);
				_overlayLampFlashRoutine = null;
			}

			_overlayLampMaterial.SetColor("_EmissionColor", Color.black);
			_overlayLampMaterial.color = _lampStartColor;
		}

		public void SetCollidersEnabled(bool state)
		{
			foreach (Collider collider in Colliders)
			{
				collider.enabled = state;
			}
		}

		public void SetRenderersEnabled(bool state)
		{
			foreach (Renderer renderer in Renderers)
			{
				renderer.enabled = state;
			}
		}

		public void SetShellRendered(bool state)
		{
			foreach (Renderer renderer in Renderers)
				renderer.enabled = state;
		}

		public void SetOverlayActive(bool state, float? alpha = null)
		{
			OverlayShell.SetActive(state);
			if (alpha.HasValue)
				SetOverlayFade(alpha.Value);

			if (!state)
				StopOverlayLampFlash();
		}

		public void SetOverlayFade(float alpha)
		{
			foreach (Renderer renderer in OverlayShell.GetComponentsInChildren<Renderer>())
			{
				foreach (Material material in renderer.materials)
				{
					Color c = material.GetColor("_Color");
					c.a = alpha;
					material.SetColor("_Color", c);
				}
			}
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

		private IEnumerator OverlayLampFlashRoutine(float flashSpeed)
		{
			float t = 0f;

			while (true)
			{
				t += Time.deltaTime * flashSpeed;
				float lerp = Mathf.PingPong(t, 1f);

				Color current = Color.Lerp(Color.grey, Color.white, lerp);
				_overlayLampMaterial.SetColor("_EmissionColor", current);
				current = Color.Lerp(_lampStartColor, Color.white, lerp);
				_overlayLampMaterial.color = current;

				yield return null;
			}
		}
	}
}
