using System.Collections.Generic;
using UnityEngine;

namespace TheLongWho.Save
{
	public class SaveEntry
	{
		public string Name { get; set; }
		public string ObjectID;
		public Vector3 Position;
		public Quaternion Rotation;
		public Dictionary<string, object> Data;
	}

	public class SaveFile
	{
		public List<SaveEntry> Entries = new List<SaveEntry>();
	}
}
