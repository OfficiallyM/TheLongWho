using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Materialisation
{
	public class Location
	{
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }

		public Location(Vector3 position, Quaternion rotation)
		{
			Position = position;
			Rotation = rotation;
		}

		public static Location FromTransform(Transform t)
		{
			return new Location(WorldUtilities.GetGlobalObjectPosition(t.position), t.rotation);
		}
	}
}
