using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheLongWho.Utilities;
using UnityEngine;

namespace TheLongWho.Tardis.Audio
{
	internal class AudioController : MonoBehaviour
	{
		private Dictionary<string, AudioSource> _sources = new Dictionary<string, AudioSource>();
		private Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
		private bool _isPaused = false;

		private void Update()
		{
			// Handle pausing audio when game is paused.
			bool paused = !mainscript.M.crsrLocked;

			if (paused && !_isPaused)
			{
				PauseAll();
				_isPaused = true;
			}
			else if (!paused && _isPaused)
			{
				UnpauseAll();
				_isPaused = false;
			}
		}

		public void RegisterSource(string name, GameObject obj)
		{
			_sources[name] = obj.AddComponent<AudioSource>();
		}

		public void RegisterClip(string name, AudioClip clip)
		{
			_clips[name] = clip;
		}

		public void Play(string sourceName, string clipName, bool loop = false, float volume = 1f, float fadeTime = 0f)
		{
			if (!_sources.TryGetValue(sourceName, out AudioSource source))
			{
				Logging.Log($"Audio source {sourceName} not found.", TLDLoader.Logger.LogLevel.Error);
				return;
			}

			if (!_clips.TryGetValue(clipName, out AudioClip clip))
			{
				Logging.Log($"Clip {clipName} not found.", TLDLoader.Logger.LogLevel.Error);
				return;
			}

			source.clip = clip;
			source.loop = loop;

			if (Mathf.Abs(fadeTime) > 0)
			{
				StartCoroutine(FadeIn(source, fadeTime, volume));
			}
			else
			{
				source.volume = volume;
				source.Play();
			}
		}

		public void Stop(string sourceName, float fadeTime = 0f)
		{
			if (!_sources.TryGetValue(sourceName, out AudioSource source)) return;
			if (Mathf.Abs(fadeTime) > 0)
				StartCoroutine(FadeOut(source, fadeTime));
			else
				source.Stop();
		}

		public void StopAll(float fadeTime = 0f)
		{
			foreach (AudioSource source in _sources.Values)
			{
				Stop(source.name, fadeTime);
			}
		}

		private IEnumerator FadeIn(AudioSource source, float duration, float targetVolume)
		{
			source.volume = 0f;
			source.Play();
			float timer = 0f;
			while (timer < duration)
			{
				source.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
				timer += Time.deltaTime;
				yield return null;
			}
			source.volume = targetVolume;
		}

		private IEnumerator FadeOut(AudioSource source, float duration)
		{
			float startVolume = source.volume;
			float timer = 0f;
			while (timer < duration)
			{
				source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
				timer += Time.deltaTime;
				yield return null;
			}
			source.volume = 0f;
			source.Stop();
		}

		private void PauseAll()
		{
			foreach (var s in _sources.Values)
				if (s.isPlaying) s.Pause();
		}

		private void UnpauseAll()
		{
			foreach (var s in _sources.Values)
				s.UnPause();
		}
	}
}
