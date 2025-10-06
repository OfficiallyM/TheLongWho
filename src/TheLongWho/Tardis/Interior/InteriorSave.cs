using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace TheLongWho.Tardis.Interior
{
	public class Item
	{
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		[JsonIgnore]
		public Transform Transform { get; set; }

		public Item(Vector3 position, Quaternion rotation, Transform transform = null)
		{
			Position = position;
			Rotation = rotation;
			Transform = transform;
		}
	}

	public class InteriorSave
	{
		public Dictionary<int, Item> Items = new Dictionary<int, Item>();
	}
}
