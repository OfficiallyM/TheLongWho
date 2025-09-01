using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using UnityEngine;
using static snTempPlayerSync;

namespace TheLongWho.Tardis.Flight
{
	internal class FlightSystem : MonoBehaviour, ISystem
	{
		public string Name => "Flight";
		public bool IsActive { get; private set; }
		public float EnergyUsage => 2f;

		private ShellController _shell;
		private Rigidbody _rb;
		private Vector3 _lastPosition;
		private bool _justToggled = false;

		private float thrustForce = 40f;
		private float verticalForce = 20f;
		private float drag = 0.98f;
		private float boostModifier = 2f;
		private bool spin = true;
		private bool boost = false;

		private Vector3 velocity;
		private Vector3 angularVelocity;
		private Quaternion _currentTilt = Quaternion.identity;

		private void Start()
		{
			_shell = GetComponent<ShellController>();
			_rb = GetComponent<Rigidbody>();
			TheLongWho.I.onLookAt += OnLook;
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

		public void Activate()
		{
			fpscontroller player = mainscript.M.player;
			_lastPosition = player.transform.position;
			player.transform.position = transform.position + Vector3.up * 1f;
			_shell.fakeSeat.RB = null;
			player.GetIn(_shell.fakeSeat);
			_shell.Interior.gameObject.SetActive(false);
			StateManager.InFlight = true;

			IsActive = true;
			Logging.Log("Flight activated");
		}

		public void Deactivate()
		{
			_shell.Interior.gameObject.SetActive(true);
			_rb.useGravity = true;
			IsActive = false;

			fpscontroller player = mainscript.M.player;
			player.camView = false;
			player.GetOut(_lastPosition, true);
			StateManager.InFlight = false;

			Logging.Log("Flight deactivated");
		}

		public void Tick()
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
				return;
			}


			if (!_rb.useGravity)
			{
				if (Input.GetKeyDown(KeyCode.R))
				{
					spin = !spin;
				}
			}
		}

		public void FixedTick()
		{
			// Disable movement if gravity is enabled.
			if (_rb.useGravity) return;

			fpscontroller player = mainscript.M.player;
			Transform cam = player.CamParent;

			// Flatten camera direction to just XZ plane.
			Vector3 camForward = cam.forward;
			camForward.y = 0;
			camForward.Normalize();

			Vector3 camRight = cam.right;
			camRight.y = 0;
			camRight.Normalize();

			// Movement input.
			boost = Input.GetKey(KeyCode.LeftShift);
			float boostForce = boost ? boostModifier : 1f;

			bool shouldTilt = false;
			if (Input.GetKey(KeyCode.W))
			{
				velocity += camForward * thrustForce * boostForce * Time.deltaTime;
				shouldTilt = true;
			}
			if (Input.GetKey(KeyCode.S))
			{
				velocity -= camForward * thrustForce * boostForce * Time.deltaTime;
				shouldTilt = true;
			}
			if (Input.GetKey(KeyCode.A))
			{
				velocity -= camRight * thrustForce * boostForce * Time.deltaTime;
				shouldTilt = true;
			}
			if (Input.GetKey(KeyCode.D))
			{
				velocity += camRight * thrustForce * boostForce * Time.deltaTime;
				shouldTilt = true;
			}
			if (Input.GetKey(KeyCode.Space))
				velocity += Vector3.up * verticalForce * boostForce * Time.deltaTime;
			if (Input.GetKey(KeyCode.LeftControl))
				velocity += Vector3.down * verticalForce * boostForce * Time.deltaTime;

			// Apply drag and velocity.
			velocity *= drag;
			_rb.velocity = velocity;

			// Spin yaw.
			float spinTarget = spin ? (boost ? 50f : 25f) : 0f;
			angularVelocity.y = Mathf.Lerp(angularVelocity.y, spinTarget, Time.fixedDeltaTime * 10f);
			float spinAngle = angularVelocity.y * Time.fixedDeltaTime;
			Quaternion spinRotation = Quaternion.Euler(0f, spinAngle, 0f);

			// Base upright rotation (yaw only).
			Quaternion baseRotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);

			// Tilt from velocity.
			Vector3 localVel = Quaternion.Inverse(baseRotation) * _rb.velocity;

			Quaternion targetTilt;
			float tiltSpeed;

			if (shouldTilt)
			{
				float tiltMax = boost ? 60f : 50f;

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
