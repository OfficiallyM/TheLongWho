using System;
using System.Collections.Generic;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Save
{
	public class SaveController : MonoBehaviour
	{
		public string ObjectID;
		public SaveEntry InitialEntry;

		private List<ISaveable> _saveables;

		private void Awake()
		{
			if (string.IsNullOrEmpty(ObjectID))
				ObjectID = Guid.NewGuid().ToString();

			_saveables = new List<ISaveable>(GetComponentsInChildren<ISaveable>());
			SaveManager.Register(this);
		}

		private void OnDestroy()
		{
			SaveManager.Delete(ObjectID);
		}

		public void RefetchSaveables()
		{
			_saveables = new List<ISaveable>(GetComponentsInChildren<ISaveable>());

			// Re-trigger a save load with new saveables.
			if (InitialEntry == null) return;
			foreach (ISaveable saveable in _saveables)
			{
				if (InitialEntry.Data.TryGetValue(saveable.SaveKey, out object data))
					saveable.LoadSaveData(data);
			}
			InitialEntry = null;
		}

		public SaveEntry GetSaveEntry()
		{
			try
			{
				var entry = new SaveEntry
				{
					Name = gameObject.name.Replace("(Clone)", "").Trim(),
					ObjectID = ObjectID,
					Position = WorldUtilities.GetGlobalObjectPosition(transform.position),
					Rotation = transform.rotation,
					Data = new Dictionary<string, object>()
				};

				foreach (ISaveable saveable in _saveables)
				{
					entry.Data[saveable.SaveKey] = saveable.GetSaveData();
				}

				return entry;
			}
			catch { }
			return new SaveEntry() { ObjectID = ObjectID };
		}

		public void LoadSaveEntry(SaveEntry entry)
		{
			transform.position = WorldUtilities.GetLocalObjectPosition(entry.Position);
			transform.rotation = entry.Rotation;

			foreach (ISaveable saveable in _saveables)
			{
				if (entry.Data.TryGetValue(saveable.SaveKey, out object data))
					saveable.LoadSaveData(data);
			}
		}
	}
}
