using System.Collections;
using UnityEngine;

namespace TheLongWho.Player
{
	internal class RegenerationEffect : MonoBehaviour
	{
		private ParticleSystem _particles;
		private Light _light;

		public IEnumerator Play(float durationSeconds = 5f)
		{
			CreateParticles(durationSeconds);
			CreateLight();

			float timer = 0f;

			while (timer < durationSeconds)
			{
				timer += Time.deltaTime;

				// Light intensity pulse
				if (_light != null)
				{
					float pulse = Mathf.Sin(Time.time * 10f) * 0.25f + 0.75f;
					_light.intensity = Mathf.Lerp(0, 4f * pulse, 1f - (timer / durationSeconds));
				}

				yield return null;
			}

			Cleanup();
		}

		private void CreateParticles(float duration)
		{
			GameObject psObj = Instantiate(TheLongWho.I.RegenerationParticles);
			psObj.transform.SetParent(mainscript.M.player.Tb, false);
			psObj.transform.localPosition = Vector3.zero;

			_particles = psObj.GetComponent<ParticleSystem>();
			_particles.Play();
		}

		private void CreateLight()
		{
			GameObject lightObj = new GameObject("RegenerationLight");
			lightObj.transform.SetParent(transform, false);
			lightObj.transform.localPosition = Vector3.zero;

			_light = lightObj.AddComponent<Light>();
			_light.type = LightType.Point;
			_light.range = 5f;
			_light.color = new Color(1f, 0.75f, 0.3f);
			_light.intensity = 0f;
		}

		private void Cleanup()
		{
			if (_particles != null) Destroy(_particles.gameObject);
			if (_light != null) Destroy(_light.gameObject);
			Destroy(this);
		}
	}
}
