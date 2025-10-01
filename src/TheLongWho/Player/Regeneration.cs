using Newtonsoft.Json.Linq;
using System.Collections;
using TheLongWho.Save;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Player
{
	internal class Regeneration : MonoBehaviour, ISaveable
	{
		public static Regeneration I;

		public string SaveKey => "Regeneration";
		public SaveController Save;

		private RegenerationSave _data = new RegenerationSave();
		private bool _isRegenerating = false;

		private void Awake()
		{
			I = this;
			Save = gameObject.AddComponent<SaveController>();
			Save.RequiresInstantiation = false;
			SaveManager.RegisterPrefab(gameObject);
		}

		private void Start()
		{
			// Set default regeneration count.
			if (!_data.Regenerations.HasValue)
				// 12 regenerations including 0.
				_data.Regenerations = 11;
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Comma))
				TryTriggerRegeneration();
		}

		public int GetRegenerations() => _data.Regenerations ?? 0;

		public int GetRegenerationsPretty() => GetRegenerations() + 1;

		public bool TryTriggerRegeneration()
		{
			if (_isRegenerating) return false;
			if (GetRegenerations() <= 0) return false;
			fpscontroller player = mainscript.M.player;
			_isRegenerating = true;
			
			// Unkill the player.
			player.died = false;
			survivalscript survival = player.survival;
			while (survival.wounds.Count > 0)
			{
				Destroy(survival.wounds[0].T.gameObject);
				survival.wounds.RemoveAt(0);
			}
			survival.hp = survival.maxHp;
			survival.toHp = 0;
			survival.UpdBlood();

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

			_isRegenerating = false;
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
