using Newtonsoft.Json.Linq;
using System.Collections;
using TheLongWho.Enemies.Angel;
using TheLongWho.Save;
using TheLongWho.Tardis.Shell;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Player
{
	internal class Regeneration : MonoBehaviour, ISaveable
	{
		public static Regeneration I;

		public string SaveKey => "Regeneration";
		public SaveController SaveController;

		private RegenerationSave _data = new RegenerationSave();
		private bool _isRegenerating = false;

		private void Awake()
		{
			I = this;
			SaveController = gameObject.AddComponent<SaveController>();
			SaveController.RequiresInstantiation = false;
			SaveManager.RegisterPrefab(gameObject);
		}

		private void Start()
		{
			ShellController.OnTardisEnter += OnTardisEnter;
		}

		public bool CanRegenerate() => _data.CanRegenerate;
		public int GetRegenerations() => _data.Regenerations ?? 0;
		public int GetCurrentRegeneration() => 12 - GetRegenerations();

		public void Enable()
		{
			_data.CanRegenerate = true;
			SaveController.Save();

			var effect = gameObject.AddComponent<RegenerationEffect>();
			StartCoroutine(effect.Play(1.5f));
		}

		public bool TryTriggerRegeneration()
		{
			if (!CanRegenerate() || _isRegenerating || GetRegenerations() <= 0) return false;

			fpscontroller player = mainscript.M.player;
			_isRegenerating = true;
			
			// Unkill the player.
			player.died = false;
			player.survival.Restart();
			
			// Interact with surroundings.
			foreach (var collider in Physics.OverlapSphere(transform.position, 10f))
			{
				// Skip self.
				if (collider.transform.IsChildOf(transform)) continue;

				// Break / kill anything nearby.
				breakablescript breakable = collider.GetComponentInParent<breakablescript>();
				if (breakable != null)
				{
					try
					{
						breakable.TryBreak(breakable.health);
						continue;
					}
					catch { }
				}

				// Destroy any angels.
				AngelController angel = collider.GetComponentInParent<AngelController>();
				if (angel != null)
				{ 
					Destroy(angel.gameObject);
					continue;
				}

				// Push any rigidbodies away.
				Rigidbody rb = collider.attachedRigidbody;
				if (rb != null)
				{
					Vector3 direction = (collider.transform.position - transform.position).normalized;
					rb.AddForce(direction * 500f, ForceMode.Impulse);
				}
			}

			StartCoroutine(Regenerate());
			
			return true;
		}

		private IEnumerator Regenerate()
		{
			fpscontroller player = mainscript.M.player;
			var effect = gameObject.AddComponent<RegenerationEffect>();
			yield return StartCoroutine(effect.Play(5f));

			_data.Regenerations -= 1;

			playermodeloutfitscript outfit = player.outfit;
			int max = outfit.characters.Length - 1;
			int newChar = outfit.selectedCharacter + 1;
			if (newChar > max)
				newChar = 0;
			player.outfit.selectedCharacter = newChar;
			outfit.refresh = true;
			outfit.SetRandom(false);

			SaveController.Save();

			_isRegenerating = false;
		}

		private void OnTardisEnter(ShellController tardis)
		{
			if (CanRegenerate()) return;
			Enable();
		}

		public object GetSaveData() => _data;

		public void LoadSaveData(object data)
		{
			RegenerationSave save = (data as JObject)?.ToObject<RegenerationSave>();
			if (save == null) return;
			_data = save;
		}
	}
}
