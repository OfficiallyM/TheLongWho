using Newtonsoft.Json.Linq;
using System.Collections;
using TheLongWho.Save;
using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Materialisation
{
	internal class MaterialisationSystem : TardisSystem, ISaveable
	{
		public override string Name => "Materialisation";
		public string SaveKey => nameof(MaterialisationSystem);

		private ShellController _shell;
		private Transform _rotor;
		private Rigidbody _rb;
		private Coroutine _currentRoutine;
		private MaterialisationSave _materialisationSave = new MaterialisationSave();

		public State CurrentState = State.Idle;
		public Location LastLocation {
			get { return _materialisationSave.LastLocation; }
			private set { _materialisationSave.LastLocation = value; }
		}

		public enum Speed
		{
			Standard,
			Fast,
			Instant
		}

		public enum State
		{
			Idle,
			Dematerialised,
		}

		private void Start()
		{
			_shell = GetComponent<ShellController>();
			_rb = GetComponent<Rigidbody>();
			_rotor = _shell.Interior.Rotor;
		}

		public object GetSaveData() => _materialisationSave;

		public void LoadSaveData(object data)
		{
			MaterialisationSave materialisationSave = (data as JObject)?.ToObject<MaterialisationSave>();
			if (materialisationSave == null) return;
			_materialisationSave = materialisationSave;
		}

		public void Dematerialise(Speed speed = Speed.Standard, bool shouldSaveLocation = true)
		{
			if (CurrentState == State.Dematerialised) return;
			if (_currentRoutine != null) StopCoroutine(_currentRoutine);
			_currentRoutine = StartCoroutine(DematerialiseRoutine(speed, shouldSaveLocation));
		}

		public void Materialise(Vector3 position, Quaternion rotation, Speed speed = Speed.Standard, bool shouldSaveLocation = true)
		{
			if (_currentRoutine != null) StopCoroutine(_currentRoutine);

			// Ensure TARDIS is dematerialised.
			if (CurrentState == State.Idle)
				_currentRoutine = StartCoroutine(FullRoutine(position, rotation, speed, shouldSaveLocation));
			// Already dematerialised, just materialise.
			else
				_currentRoutine = StartCoroutine(MaterialiseRoutine(position, rotation, speed));
		}

		private IEnumerator DematerialiseRoutine(Speed speed, bool shouldSaveLocation)
		{
			if (shouldSaveLocation)
				LastLocation = Location.FromTransform(_shell.transform);

			CurrentState = State.Dematerialised;
			float duration = speed == Speed.Standard ? 18f : 9f;
			float fadeDuration = speed == Speed.Standard ? 14f : 9f;
			string clip = speed == Speed.Standard ? "dematerialise" : string.Empty;

			if (!string.IsNullOrEmpty(clip)) _shell.Audio.Play(_shell.IsInside() ? "interior" : "shell", clip);

			// Set up overlay shell.
			_shell.SetOverlayActive(true);
			_shell.StartOverlayLampFlash(2f);

			// Hide the real shell.
			_shell.SetShellRendered(false);

			StartCoroutine(AnimateRotor(duration, false));

			float timer = 0f;
			while (timer < fadeDuration)
			{
				float t = timer / fadeDuration;

				// Base fade out.
				float alpha = Mathf.Lerp(1f, 0f, t);

				// Add breathing pulse.
				alpha *= 0.8f + 0.2f * Mathf.Sin(timer * 6f);
				_shell.SetOverlayFade(alpha);

				timer += Time.deltaTime;
				yield return null;
			}

			_shell.SetOverlayFade(0f);
			_shell.StopOverlayLampFlash();

			// Disable colliders and gravity after fade ends.
			_rb.useGravity = false;
			_shell.SetCollidersEnabled(false);

			// Keep overlay active but invisible until sound ends.
			yield return new WaitForSeconds(duration - fadeDuration);

			_shell.SetOverlayActive(false);

			_currentRoutine = null;
		}

		private IEnumerator MaterialiseRoutine(Vector3 position, Quaternion rotation, Speed speed)
		{
			float duration = speed == Speed.Standard ? 18f : 9f;
			float fadeDuration = speed == Speed.Standard ? 16f : 9f;
			string clip = speed == Speed.Standard ? "materialise" : string.Empty;

			_shell.transform.position = WorldUtilities.GetLocalObjectPosition(position);
			_shell.transform.rotation = rotation;
			_shell.Interior.SyncPositionToShell();
			// Wait for the position sync to happen, otherwise the audio doesn't play correctly.
			yield return new WaitForSeconds(2f);

			// Re-enable colliders and gravity to ensure it appears on the floor.
			_shell.SetCollidersEnabled(true);
			_rb.useGravity = true;

			if (!string.IsNullOrEmpty(clip)) _shell.Audio.Play(_shell.IsInside() ? "interior" : "shell", clip);

			// Set up overlay shell.
			_shell.SetOverlayActive(true);
			_shell.StartOverlayLampFlash(2f);

			StartCoroutine(AnimateRotor(duration, true));

			float timer = 0f;
			while (timer < fadeDuration)
			{
				float t = timer / fadeDuration;

				// Base fade in.
				float alpha = Mathf.Lerp(0f, 1f, t);

				// Add breathing pulse.
				alpha *= 0.8f + 0.2f * Mathf.Sin(timer * 6f);
				_shell.SetOverlayFade(alpha);

				timer += Time.deltaTime;
				yield return null;
			}

			// Fully visible.
			_shell.SetOverlayFade(1f);

			// Wait for the rest of the sound before finishing.
			yield return new WaitForSeconds(duration - fadeDuration);

			// Swap overlay for the real shell.
			_shell.SetShellRendered(true);
			_shell.SetOverlayActive(false);

			_currentRoutine = null;
			CurrentState = State.Idle;
		}

		private IEnumerator FullRoutine(Vector3 position, Quaternion rotation, Speed speed, bool shouldSaveLocation)
		{
			yield return StartCoroutine(DematerialiseRoutine(speed, shouldSaveLocation));
			yield return StartCoroutine(MaterialiseRoutine(position, rotation, speed));
		}

		private IEnumerator AnimateRotor(float duration, bool isMaterialising)
		{
			float timer = 0f;
			Vector3 basePos = _rotor.localPosition;

			float amplitude = 0.6f;
			float cycles = 4f;

			while (timer < duration)
			{
				float t = timer / duration;

				// Compute slow sine-wave movement.
				float sine = Mathf.Sin(t * Mathf.PI * cycles);

				// Gradually ramp the motion in or out.
				float ramp = isMaterialising ? t : (1f - t);

				// Apply ramp to the entire sine output, not the sine itself.
				float offset = sine * amplitude * ramp;

				_rotor.transform.localPosition = basePos + Vector3.up * offset;

				timer += Time.deltaTime;
				yield return null;
			}

			// Snap back to base smoothly.
			_rotor.localPosition = basePos;
		}
	}
}
