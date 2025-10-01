using System;
using System.Collections.Generic;
using TheLongWho.Extensions;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Save
{
	public class SaveController : MonoBehaviour
	{
		public string ObjectID;
		public SaveEntry InitialEntry;
		public bool RequiresInstantiation { get; set; } = true;

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
					Name = gameObject.name.Prettify(),
					ObjectID = ObjectID,
					RequiresInstantiation = RequiresInstantiation,
					Data = new Dictionary<string, object>()
				};

				if (RequiresInstantiation)
				{
					entry.Position = WorldUtilities.GetGlobalObjectPosition(transform.position);
					entry.Rotation = transform.rotation;
				}

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
			if (entry.Position.HasValue)
				transform.position = WorldUtilities.GetLocalObjectPosition(entry.Position.Value);
			if (entry.Rotation.HasValue)
				transform.rotation = entry.Rotation.Value;

			foreach (ISaveable saveable in _saveables)
			{
				if (entry.Data.TryGetValue(saveable.SaveKey, out object data))
					saveable.LoadSaveData(data);
			}
		}
	}
}
