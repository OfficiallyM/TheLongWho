using System.Collections;
using TheLongWho.Tardis.Flight;
using TheLongWho.Tardis.Interior;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Shell
{
	internal class ShellController : MonoBehaviour
	{
		public static GameObject InteriorPrefab;
		public InteriorController Interior;
		internal Transform ExitPoint { get; private set; }
		internal seatscript FakeSeat { get; private set; }

		private Material _lampMaterial;
		private Color _lampStartColor;
		private Coroutine _lampFlashRoutine;

		private void Start()
		{
			SpawnInterior();
			ExitPoint = transform.Find("ExitPoint");
			Transform seat = transform.Find("Seat");
			FakeSeat = seat.gameObject.AddComponent<seatscript>();
			FakeSeat.sitPos = seat;

			// This is required to keep the TARDIS in sync with the world when it moves.
			visszarako visszarako = gameObject.AddComponent<visszarako>();
			visszarako.importantUnderMapLook = true;
			visszarako.RB = GetComponent<Rigidbody>();
			visszarako.rb = true;

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
		}

		private void SpawnInterior()
		{
			if (Interior != null) return;

			GameObject interior = Instantiate(InteriorPrefab);
			Interior = interior.GetComponent<InteriorController>();
			Interior.Shell = this;
			Interior.SyncPositionToShell();
		}

		public void Enter()
		{
			if (!CanEnter()) return;
			Interior.SyncPositionToShell();
			WorldUtilities.TeleportPlayer(Interior.EnterPoint.position + Vector3.up * 2f);
		}

		public void Exit()
		{
			if (!CanExit()) return;
			WorldUtilities.TeleportPlayer(GetSafeExitPoint());
		}

		private Vector3 GetSafeExitPoint()
		{
			Vector3 exitPos = ExitPoint.position;

			if (Vector3.Dot(ExitPoint.up, Vector3.up) < 0.1f)
				exitPos += Vector3.up * 3f;

			return exitPos;
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
