using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLongWho.Save
{
	public class SaveController : MonoBehaviour
	{
		public string ObjectID;

		private List<ISaveable> _saveables;

		private void Awake()
		{
			if (string.IsNullOrEmpty(ObjectID))
				ObjectID = Guid.NewGuid().ToString();

			_saveables = new List<ISaveable>(GetComponentsInChildren<ISaveable>());
			SaveManager.Register(this);
		}

		public void RefetchSaveables()
		{
			_saveables = new List<ISaveable>(GetComponentsInChildren<ISaveable>());
		}

		public SaveEntry GetSaveEntry()
		{
			var entry = new SaveEntry
			{
				Name = gameObject.name.Replace("(Clone)", "").Trim(),
				ObjectID = ObjectID,
				Position = transform.position,
				Rotation = transform.rotation,
				Data = new Dictionary<string, object>()
			};

			foreach (ISaveable saveable in _saveables)
			{
				entry.Data[saveable.SaveKey] = saveable.GetSaveData();
			}

			return entry;
		}

		public void LoadSaveEntry(SaveEntry entry)
		{
			transform.position = entry.Position;
			transform.rotation = entry.Rotation;

			foreach (ISaveable saveable in _saveables)
			{
				if (entry.Data.TryGetValue(saveable.SaveKey, out object data))
					saveable.LoadSaveData(data);
			}
		}
	}
}
