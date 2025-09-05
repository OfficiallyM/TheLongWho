using System.Collections;
using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.System;
using UnityEngine;

namespace TheLongWho.Tardis.Materialisation
{
	internal class MaterialisationSystem : TardisSystem
	{
		public override string Name => "Materialisation";

		private ShellController _shell;
		private Rigidbody _rb;
		private Coroutine _currentRoutine;

		public State CurrentState = State.Idle;

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
		}

		public void Dematerialise(Speed speed = Speed.Standard)
		{
			if (CurrentState == State.Dematerialised) return;
			if (_currentRoutine != null) StopCoroutine(_currentRoutine);
			_currentRoutine = StartCoroutine(DematerialiseRoutine(speed));
		}

		public void Materialise(Vector3 position, Quaternion rotation, Speed speed = Speed.Standard)
		{
			if (_currentRoutine != null) StopCoroutine(_currentRoutine);

			// Ensure TARDIS is dematerialised.
			if (CurrentState == State.Idle)
				_currentRoutine = StartCoroutine(FullRoutine(position, rotation, speed));
			// Already dematerialised, just materialise.
			else
				_currentRoutine = StartCoroutine(MaterialiseRoutine(position, rotation, speed));
		}

		private IEnumerator DematerialiseRoutine(Speed speed)
		{
			float duration = speed == Speed.Standard ? 18f : 9f;
			float fadeDuration = speed == Speed.Standard ? 14f : 9f;
			string clip = speed == Speed.Standard ? "dematerialise" : string.Empty;

			if (!string.IsNullOrEmpty(clip)) _shell.Audio.Play(_shell.IsInside() ? "interior" : "shell", clip);

			// Set up overlay shell.
			_shell.SetOverlayActive(true);
			_shell.StartOverlayLampFlash(2f);

			// Hide the real shell.
			_shell.SetShellRendered(false);

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
			CurrentState = State.Dematerialised;
		}

		private IEnumerator MaterialiseRoutine(Vector3 position, Quaternion rotation, Speed speed)
		{
			float duration = speed == Speed.Standard ? 18f : 9f;
			float fadeDuration = speed == Speed.Standard ? 16f : 9f;
			string clip = speed == Speed.Standard ? "materialise" : string.Empty;

			_shell.transform.position = position;
			_shell.transform.rotation = rotation;
			_shell.Interior.SyncPositionToShell();

			// Re-enable colliders and gravity to ensure it appears on the floor.
			_shell.SetCollidersEnabled(true);
			_rb.useGravity = true;

			if (!string.IsNullOrEmpty(clip)) _shell.Audio.Play(_shell.IsInside() ? "interior" : "shell", clip);

			// Set up overlay shell.
			_shell.SetOverlayActive(true);
			_shell.StartOverlayLampFlash(2f);

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

		private IEnumerator FullRoutine(Vector3 position, Quaternion rotation, Speed speed)
		{
			yield return StartCoroutine(DematerialiseRoutine(speed));
			yield return StartCoroutine(MaterialiseRoutine(position, rotation, speed));
		}
	}
}
