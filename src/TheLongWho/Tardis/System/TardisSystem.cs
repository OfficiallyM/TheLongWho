using UnityEngine;

namespace TheLongWho.Tardis.System
{
	internal abstract class TardisSystem : MonoBehaviour
	{
		public abstract string Name { get; }
		public virtual float EnergyUsage => 0f;
		public bool IsActive { get; set; }
		public virtual bool IsScreenControllable { get; set; } = false;
		public virtual bool IsActiveByDefault { get; set; } = true;
		public SystemController Systems { get; set; }

		public virtual void Activate()
		{
			IsActive = true;
			Systems.UpdateSaveState(this, true);
		}

		public virtual void Deactivate()
		{
			IsActive = false;
			Systems.UpdateSaveState(this, false);
		}

		public virtual void Tick() { }
		public virtual void FixedTick() { }
	}
}
