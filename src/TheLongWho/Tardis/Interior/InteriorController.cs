using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TheLongWho.Save;
using TheLongWho.Tardis.Shell;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Interior
{
	internal class InteriorController : MonoBehaviour, ISaveable
	{
		public string SaveKey => "Interior";
		public SaveController SaveController;

		internal ShellController Shell;
		internal Transform EnterPoint { get; private set; }
		internal Transform Console { get; private set; }
		internal Transform Rotor { get; private set; }
		internal Canvas ScreenCanvas { get; private set; }

		private InteriorSave _saveData = new InteriorSave();
		private List<Transform> _cachedItems = new List<Transform>();
		private bool _isSyncingPosition = false;
		private bool _isFirstSync = true;

		private void Awake()
		{
			SaveController = gameObject.AddComponent<SaveController>();
			SaveController.RequiresInstantiation = false;
			SaveManager.RegisterPrefab(gameObject);

			EnterPoint = transform.Find("EnterPoint");
			Console = transform.Find("Console");
			Rotor = transform.Find("Rotor");
			ScreenCanvas = transform.Find("ScreenCanvas")?.GetComponent<Canvas>();
		}

		private void Start()
		{
			// This is required to keep the interior in sync with the world when the shell moves.
			visszarako visszarako = gameObject.AddComponent<visszarako>();
			visszarako.importantUnderMapLook = true;
			visszarako.RB = GetComponent<Rigidbody>();
			visszarako.rb = true;

			TheLongWho.I.OnCacheRebuild += CacheItems;
		}

		public void SyncPositionToShell()
		{
			_isSyncingPosition = true;
			// Offset directly below shell.
			Vector3 offset = Vector3.down * 1000f;

			// Use yaw-only rotation so interior doesn't tilt with shell.
			Quaternion yawOnly = Quaternion.Euler(0f, Shell.transform.eulerAngles.y, 0f);

			// Record player's local position relative to interior.
			fpscontroller player = mainscript.M.player;
			Vector3 localPos = transform.InverseTransformPoint(player.transform.position);
			Quaternion localRot = Quaternion.Inverse(transform.rotation) * player.transform.rotation;

			// Cache items inside.
			if (!_isFirstSync)
				CacheItems();

			// Sync interior to shell position.
			transform.position = Shell.transform.position + offset;
			transform.rotation = yawOnly * Quaternion.Euler(0f, 180f, 0f);

			// Restore cached item positions.
			RestoreItems();

			// Restore player inside relative to interior.
			if (Shell.IsInside() && !StateManager.InFlight)
				WorldUtilities.TeleportPlayerSafe(transform.TransformPoint(localPos), (transform.rotation * localRot).eulerAngles);
			_isSyncingPosition = false;
			_isFirstSync = false;
		}

		private void CacheItems()
		{
			if (_isSyncingPosition) return;
			if (!Shell.IsInside()) return;

			_cachedItems.Clear();
			
			Collider[] colliders = Physics.OverlapBox(transform.position, new Vector3(10, 10, 10), transform.rotation);

			foreach (Collider col in colliders)
			{
				Transform root = col.transform.GetComponentInParent<tosaveitemscript>()?.transform;

				// Skip nulls, already cached items, player and self.
				if (root == null) continue;
				if (_cachedItems.Contains(root)) continue;
				if (root.IsChildOf(mainscript.M.player.transform)) continue;
				if (root.IsChildOf(transform)) continue;
				_cachedItems.Add(root);
			}

			RecordItemOffsets();
		}

		private void RecordItemOffsets()
		{
			_saveData.Items.Clear();

			foreach (var item in _cachedItems)
			{
				var save = item.GetComponent<tosaveitemscript>();
				Vector3 localPos = transform.InverseTransformPoint(item.position);
				Quaternion localRot = Quaternion.Inverse(transform.rotation) * item.rotation;

				_saveData.Items[save.idInSave] = new Item(localPos, localRot, item);
			}
		}

		private void RestoreItems()
		{
			foreach (var kvp in _saveData.Items)
			{
				int id = kvp.Key;
				Item item = kvp.Value;
				if (item.Transform == null)
					item.Transform = GetTransformFromSaveId(id);

				if (item.Transform == null)
				{
					Logging.Log($"Id {id} unable to determine transform.", TLDLoader.Logger.LogLevel.Error);
					continue;
				}

				item.Transform.position = transform.TransformPoint(item.Position);
				item.Transform.rotation = transform.rotation * item.Rotation;
			}
		}

		private Transform GetTransformFromSaveId(int id)
		{
			foreach (var save in savedatascript.d.toSaveStuff.Values)
			{
				if (save.idInSave == id) return save.transform;
			}

			return null;
		}

		public object GetSaveData() => _saveData;

		public void LoadSaveData(object data)
		{
			InteriorSave saveData = (data as JObject)?.ToObject<InteriorSave>();
			if (saveData == null) return;
			_saveData = saveData;
			RestoreItems();
		}
	}
}
