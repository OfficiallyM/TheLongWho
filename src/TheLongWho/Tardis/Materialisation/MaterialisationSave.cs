using System.Collections.Generic;

namespace TheLongWho.Tardis.Materialisation
{
	public class MaterialisationSave
	{
		public Location LastLocation { get; set; }
		public Dictionary<string, Location> CustomDestinations { get; set; } = new Dictionary<string, Location>();
	}
}
