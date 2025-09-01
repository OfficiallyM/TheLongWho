using TheLongWho.Tardis.Shell;

namespace TheLongWho.Tardis.System
{
	internal interface ISystem
	{
		string Name { get; }
		bool IsActive { get; }
		float EnergyUsage { get; }

		void Activate();
		void Deactivate();
		void Tick();
		void FixedTick();
	}
}
