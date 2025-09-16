using UnityEngine;

namespace TheLongWho.Sonic
{
	internal abstract class SonicMode : MonoBehaviour
	{
		public abstract string Name { get; }
		public virtual float EngageTime => 1.5f;
		public SonicController Sonic { get; set; }

		public virtual void OnEngage() { }
		public virtual void OnDisengage() { }
		public virtual void Tick() { }
	}
}
