using TheLongWho.Tardis.Shell;

namespace TheLongWho.Utilities
{
	internal static class StateManager
	{
		public static bool InFlight { get; set; }
		public static ShellController LastTardis { get; set; }
	}
}
