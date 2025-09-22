using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Save
{
	internal static class SaveManager
	{
		private static SaveFile _saveFile = new SaveFile();
		private static List<SaveController> _saves = new List<SaveController>();
		private static bool _isLoaded = false;
		private static List<GameObject> _prefabs = new List<GameObject>();

		public static void Init()
		{
			TheLongWho.I.OnCacheRebuild += OnCacheRebuild;
		}

		private static void OnCacheRebuild()
		{
			if (!_isLoaded) return;
			SaveAll();
		}

		public static void Register(SaveController save) => _saves.Add(save);

		public static void RegisterPrefab(GameObject obj) => _prefabs.Add(obj);

		public static void SaveAll()
		{
			foreach (SaveController save in _saves)
				Save(save, false);

			SerializeSaveData();
		}

		public static void LoadAll()
		{
			_isLoaded = true;
			UnserializeSaveData();
			foreach (GameObject prefab in _prefabs)
				Load(prefab);
		}

		public static void Save(SaveController save, bool commit = false)
		{
			SaveEntry entry = save.GetSaveEntry();
			_saveFile.Entries.RemoveAll(e => e.ObjectID == entry.ObjectID);
			if (entry.Name != null)
				_saveFile.Entries.Add(entry);

			if (commit) SerializeSaveData();
		}

		public static void Load(GameObject prefab)
		{
			foreach (SaveEntry entry in _saveFile.Entries)
			{
				if (entry.Name != prefab.name) continue;

				GameObject obj = GameObject.Instantiate(prefab, WorldUtilities.GetLocalObjectPosition(entry.Position), entry.Rotation);
				SaveController saveable = obj.GetComponent<SaveController>();
				saveable.ObjectID = entry.ObjectID;
				saveable.InitialEntry = entry;
				saveable.LoadSaveEntry(entry);
			}
		}

		public static void Delete(string objectID)
		{
			_saveFile.Entries.RemoveAll(e => e.ObjectID == objectID);
			SerializeSaveData();
		}

		public static void ResetLoadedState()
		{
			_isLoaded = false;
		}

		/// <summary>
		/// Read/write data to game save
		/// <para>Originally from RundensWheelPositionEditor</para>
		/// </summary>
		/// <param name="input">The string to write to the save</param>
		/// <returns>The read/written string</returns>
		private static string ReadWriteToGameSave(string input = null)
		{
			try
			{
				save_rendszam saveRendszam = null;
				save_prefab savePrefab1;

				// Attempt to find existing plate.
				if ((savedatascript.d.data.farStuff.TryGetValue(Mathf.Abs(TheLongWho.I.ID.GetHashCode()), out savePrefab1) || savedatascript.d.data.nearStuff.TryGetValue(Mathf.Abs(TheLongWho.I.ID.GetHashCode()), out savePrefab1)) && savePrefab1.rendszam != null)
					saveRendszam = savePrefab1.rendszam;

				// Plate doesn't exist.
				if (saveRendszam == null)
				{
					// Create a new plate to store the input string in.
					tosaveitemscript component = itemdatabase.d.gplate.GetComponent<tosaveitemscript>();
					save_prefab savePrefab2 = new save_prefab(component.category, component.id, double.MaxValue, double.MaxValue, double.MaxValue, 0.0f, 0.0f, 0.0f);
					savePrefab2.rendszam = new save_rendszam();
					saveRendszam = savePrefab2.rendszam;
					saveRendszam.S = string.Empty;
					savedatascript.d.data.farStuff.Add(Mathf.Abs(TheLongWho.I.ID.GetHashCode()), savePrefab2);
				}

				// Write the input to the plate.
				if (input != null && input != string.Empty)
					saveRendszam.S = input;

				return saveRendszam.S;
			}
			catch (Exception ex)
			{
				Logging.Log($"Save read/write error - {ex}", TLDLoader.Logger.LogLevel.Error);
			}

			return string.Empty;
		}

		/// <summary>
		/// Unserialize existing save data
		/// </summary>
		private static void UnserializeSaveData()
		{
			if (_saveFile.Entries.Count > 0) return;

			string existingString = ReadWriteToGameSave();
			if (string.IsNullOrEmpty(existingString)) return;

			var settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			_saveFile = JsonConvert.DeserializeObject<SaveFile>(existingString, settings);
		}

		/// <summary>
		/// Serialize save data and write to save
		/// </summary>
		private static void SerializeSaveData()
		{
			var settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			string json = JsonConvert.SerializeObject(_saveFile, Formatting.None, settings);
			ReadWriteToGameSave(json);
		}
	}
}
