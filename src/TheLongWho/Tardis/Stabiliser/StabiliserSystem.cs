using TheLongWho.Tardis.System;
using UnityEngine;

namespace TheLongWho.Tardis.Stabiliser
{
	internal class StabiliserSystem : TardisSystem
	{
		public override string Name => "Stabiliser";
		public override float EnergyUsage => 0.1f;

		private Rigidbody _rb;

		private void Start()
		{
			_rb = GetComponent<Rigidbody>();
			IsActive = true;
		}

		public override void FixedTick()
		{
			float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

			if (tiltAngle > 15f)
			{
				Quaternion targetRotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);
				Quaternion stabilised = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 1f);
				_rb.MoveRotation(stabilised);
			}
		}
	}
}
