using UnityEngine;

namespace TheLongWho.Tardis.System
{
	internal abstract class TardisSystem : MonoBehaviour
	{
		public abstract string Name { get; }
		public virtual float EnergyUsage => 0f;
		public bool IsActive { get; set; }
		public SystemController Systems { get; set; }

		public virtual void Activate()
		{
			IsActive = true;
		}

		public virtual void Deactivate()
		{
			IsActive = false;
		}

		public virtual void Tick() { }
		public virtual void FixedTick() { }
	}
}
