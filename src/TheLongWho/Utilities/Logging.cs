namespace TheLongWho.Utilities
{
	internal static class Logging
	{
		public static void Log(string message, TLDLoader.Logger.LogLevel logLevel = TLDLoader.Logger.LogLevel.Info)
		{
			TheLongWho.I.Logger.Log(message, logLevel);
		}
	}
}
