using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.Stabiliser;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Flight
{
	internal class FlightSystem : TardisSystem
	{
		public override string Name => "Flight";
		public override float EnergyUsage => 2f;

		private ShellController _shell;
		private Rigidbody _rb;
		private Vector3 _lastPosition;
		private bool _justToggled = false;

		private float _thrustForce = 40f;
		private float _verticalForce = 20f;
		private float _drag = 0.98f;
		private float _boostModifier = 2f;
		private bool _spin = true;
		private bool _boost = false;

		private Vector3 _velocity;
		private Vector3 _angularVelocity;
		private Quaternion _currentTilt = Quaternion.identity;

		private void Start()
		{
			_shell = GetComponent<ShellController>();
			_rb = GetComponent<Rigidbody>();
			_shell.OnLookAt += OnLook;
		}
		
		private void OnLook(RaycastHit hitInfo)
		{
			fpscontroller player = mainscript.M.player;
			if (hitInfo.collider.name == "Console")
			{
				player.E = "Engage flight";
				player.BcanE = true;

				if (Input.GetKeyUp(KeyCode.E))
				{
					Activate();
					_justToggled = true;
					return;
				}
			}
		}

		public override void Activate()
		{
			fpscontroller player = mainscript.M.player;
			_lastPosition = _shell.Interior.transform.InverseTransformPoint(player.transform.position);
			player.transform.position = transform.position + Vector3.up * 1f;
			_shell.FakeSeat.RB = null;
			player.GetIn(_shell.FakeSeat);
			_shell.Interior.gameObject.SetActive(false);
			StateManager.InFlight = true;
			Systems.DisableSystem<StabiliserSystem>();
			IsActive = true;
		}

		public override void Deactivate()
		{
			_shell.Interior.gameObject.SetActive(true);
			_shell.Interior.SyncPositionToShell();
			_rb.useGravity = true;
			IsActive = false;
			Systems.EnableSystem<StabiliserSystem>();
			fpscontroller player = mainscript.M.player;
			player.camView = false;
			player.GetOut(_shell.Interior.transform.TransformPoint(_lastPosition), true);
			_shell.StopLampFlash();
			StateManager.InFlight = false;
		}

		public override void Tick()
		{
			fpscontroller player = mainscript.M.player;

			// Prevent infinite loop of entering and exiting flight mode.
			if (_justToggled)
			{
				_justToggled = false;
				return;
			}

			if (Input.GetKeyDown(KeyCode.E))
			{
				Deactivate();
				return;
			}

			// Force third person view.
			player.camView = true;

			if (Input.GetKeyDown(KeyCode.F))
			{
				_rb.useGravity = !_rb.useGravity;
				if (!_rb.useGravity)
				{
					_velocity = _rb.velocity;
					_angularVelocity = _rb.angularVelocity;
					_shell.StartLampFlash();
				}
				else
				{
					_shell.StopLampFlash();
				}
			}

			if (!_rb.useGravity)
			{
				if (Input.GetKeyDown(KeyCode.R))
				{
					_spin = !_spin;
				}
			}
		}

		public override void FixedTick()
		{
			fpscontroller player = mainscript.M.player;
			Transform cam = player.CamParent;
			bool shouldTilt = false;
			bool shouldSpin = _spin && !_rb.useGravity;

			// Disable movement if gravity is enabled.
			if (!_rb.useGravity)
			{
				// Flatten camera direction to just XZ plane.
				Vector3 camForward = cam.forward;
				camForward.y = 0;
				camForward.Normalize();

				Vector3 camRight = cam.right;
				camRight.y = 0;
				camRight.Normalize();

				// Movement input.
				_boost = Input.GetKey(KeyCode.LeftShift);
				float boostForce = _boost ? _boostModifier : 1f;

				if (Input.GetKey(KeyCode.W))
				{
					_velocity += camForward * _thrustForce * boostForce * Time.fixedDeltaTime;
					shouldTilt = true;
				}
				if (Input.GetKey(KeyCode.S))
				{
					_velocity -= camForward * _thrustForce * boostForce * Time.fixedDeltaTime;
					shouldTilt = true;
				}
				if (Input.GetKey(KeyCode.A))
				{
					_velocity -= camRight * _thrustForce * boostForce * Time.fixedDeltaTime;
					shouldTilt = true;
				}
				if (Input.GetKey(KeyCode.D))
				{
					_velocity += camRight * _thrustForce * boostForce * Time.fixedDeltaTime;
					shouldTilt = true;
				}
				if (Input.GetKey(KeyCode.Space))
					_velocity += Vector3.up * _verticalForce * boostForce * Time.fixedDeltaTime;
				if (Input.GetKey(KeyCode.LeftControl))
					_velocity += Vector3.down * _verticalForce * boostForce * Time.fixedDeltaTime;

				// Apply drag and velocity.
				_velocity *= _drag;
				_rb.velocity = _velocity;
			}

			// Spin yaw.
			float spinTarget = shouldSpin ? (_boost ? 50f : 25f) : 0f;
			_angularVelocity.y = Mathf.Lerp(_angularVelocity.y, spinTarget, Time.fixedDeltaTime * 10f);
			float spinAngle = _angularVelocity.y * Time.fixedDeltaTime;
			Quaternion spinRotation = Quaternion.Euler(0f, spinAngle, 0f);

			// Base upright rotation (yaw only).
			Quaternion baseRotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);

			// Tilt from velocity.
			Vector3 localVel = Quaternion.Inverse(baseRotation) * _rb.velocity;

			Quaternion targetTilt;
			float tiltSpeed;

			if (shouldTilt)
			{
				float tiltMax = _boost ? 50f : 40f;

				// Local velocity in XZ.
				Vector3 flatVel = new Vector3(localVel.x, 0f, localVel.z);

				// Direction of tilt, normalized.
				Vector3 tiltDir = flatVel.normalized;

				// Scale tilt amount by speed, capped at 1.
				float speedFactor = Mathf.Clamp01(flatVel.magnitude / 3f);

				float tiltForward = tiltDir.z * tiltMax * speedFactor;
				float tiltSide = tiltDir.x * tiltMax * speedFactor;
				targetTilt = Quaternion.Euler(tiltForward, 0f, -tiltSide);
				tiltSpeed = 2f;
			}
			else
			{
				targetTilt = Quaternion.identity;
				tiltSpeed = 1.25f;
			}

			_currentTilt = Quaternion.Slerp(_currentTilt, targetTilt, Time.fixedDeltaTime * tiltSpeed);

			// Combine everything.
			Quaternion targetRotation = baseRotation * spinRotation * _currentTilt;
			_rb.MoveRotation(targetRotation);
		}
	}
}
