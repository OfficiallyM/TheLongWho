using TheLongWho.Tardis.Materialisation;
using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.System;
using UnityEngine;

namespace TheLongWho.Tardis.PerceptionFilter
{
	internal class PerceptionFilterSystem : TardisSystem
	{
		public override string Name => "Perception Filter";
		public override bool IsScreenControllable => true;

		private ShellController _shell;
		private float _swapThreshold = 0.95f;
		private float _minDistance = 25f;
		private float _maxDistance = 100f;
		private float _fadeSpeed = 1.5f;
		private float _currentAlpha = 1f;
		private float _targetAlpha = 1f;
		private bool _hasOverlayControl = false;

		private void Awake()
		{
			_shell = GetComponent<ShellController>();
		}

		public override void Activate()
		{
			base.Activate();
			// Reset to visible when turning on.
			_currentAlpha = 1f;
			_targetAlpha = 1f;

			// Release overlay control.
			if (_hasOverlayControl)
			{
				_shell.SetOverlayActive(false);
				_shell.SetShellRendered(true);
				_hasOverlayControl = false;
			}

			_shell.SetOverlayFade(1f);
		}

		public override void Deactivate()
		{
			base.Deactivate();
			// When disabled, make sure real shell is visible.
			if (_hasOverlayControl)
			{
				_shell.SetOverlayActive(false);
				_hasOverlayControl = false;
			}
			_shell.SetShellRendered(true);
			_shell.SetOverlayFade(1f);
		}

		public override void Tick()
		{
			// If materialisation is in progress, don't interfere.
			if (_shell.Materialisation.CurrentState != MaterialisationSystem.State.Idle || _shell.IsInside()) return;

			fpscontroller player = mainscript.M.player;

			float distance = Vector3.Distance(player.transform.position, _shell.transform.position);

			if (distance <= _minDistance)
				_targetAlpha = 1f;
			else if (distance >= _maxDistance)
				_targetAlpha = 0.25f;
			else
			{
				float t = (distance - _minDistance) / (_maxDistance - _minDistance);
				_targetAlpha = Mathf.Lerp(1f, 0.25f, t);
			}

			// Smoothly move current alpha toward target.
			_currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, _fadeSpeed * Time.deltaTime);

			// If alpha is below threshold and we don't already control the overlay, take it over.
			if (_currentAlpha <= _swapThreshold)
			{
				if (!_hasOverlayControl)
				{
					_hasOverlayControl = true;
					_shell.SetOverlayActive(true);
					_shell.SetShellRendered(false);
				}

				_shell.SetOverlayFade(_currentAlpha);
			}
			else
			{
				// If we control the overlay and alpha is near fully visible, swap back to real shell.
				if (_hasOverlayControl && _currentAlpha >= _swapThreshold)
				{
					_hasOverlayControl = false;
					_shell.SetOverlayActive(false);
					_shell.SetShellRendered(true);
				}
			}
		}
	}
}
