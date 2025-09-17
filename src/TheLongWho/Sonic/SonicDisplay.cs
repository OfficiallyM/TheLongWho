using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLongWho.Sonic
{
	internal class SonicDisplay : MonoBehaviour
	{
		private Canvas _canvas;
		private TextMeshProUGUI _text;
		private RectTransform _textRect;
		private Camera _cam;
		private Image _background;
		private Color _bgVisible = new Color(0f, 0f, 0f, 0.6f);
		private Color _bgHidden = new Color(0f, 0f, 0f, 0f);

		private Coroutine _displayRoutine;
		private Coroutine _fadeRoutine;

		private void Start()
		{
			_cam = mainscript.M.player.Cam;

			// Create canvas.
			GameObject canvasObj = new GameObject("SonicCanvas");
			canvasObj.transform.SetParent(transform, false);

			_canvas = canvasObj.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.worldCamera = _cam;
			canvasObj.AddComponent<CanvasScaler>();
			canvasObj.AddComponent<GraphicRaycaster>();

			RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
			canvasRect.sizeDelta = new Vector2(150, 100);
			canvasRect.localPosition = new Vector3(0, 0.13f, 0);
			canvasRect.localRotation = Quaternion.identity;
			canvasRect.localScale = Vector3.one * 0.0025f;
			canvasRect.anchorMin = new Vector2(0.5f, 0f);
			canvasRect.anchorMax = new Vector2(0.5f, 0f);
			canvasRect.pivot = new Vector2(0.5f, 0f);

			// Add background panel.
			GameObject bg = new GameObject("Background");
			bg.transform.SetParent(canvasObj.transform, false);
			RectTransform bgRect = bg.AddComponent<RectTransform>();
			bgRect.anchorMin = Vector2.zero;
			bgRect.anchorMax = Vector2.one;
			bgRect.offsetMin = Vector2.zero;
			bgRect.offsetMax = Vector2.zero;

			_background = bg.AddComponent<Image>();
			_background.color = _bgHidden;

			// Add text.
			GameObject textObj = new GameObject("DisplayText");
			textObj.transform.SetParent(canvasObj.transform, false);

			_text = textObj.AddComponent<TextMeshProUGUI>();
			_textRect = _text.GetComponent<RectTransform>();
			_textRect.anchorMin = Vector2.zero;
			_textRect.anchorMax = Vector2.one;
			_textRect.offsetMin = new Vector2(10, 10);
			_textRect.offsetMax = new Vector2(-10, -10);

			_text.fontSize = 12;
			_text.alignment = TextAlignmentOptions.TopLeft;
			_text.text = string.Empty;
			// Change text material shader to not be affected by light.
			_text.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			_text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
		}

		private void LateUpdate()
		{
			// Always face camera.
			_canvas.transform.rotation = Quaternion.LookRotation(_canvas.transform.position - _cam.transform.position);
		}

		public void ShowMessages(List<string> rows, float displayTime = 5f, float typewriterSpeed = 0.03f)
		{
			if (_displayRoutine != null)
				StopCoroutine(_displayRoutine);

			ResizeCanvasForContent(rows, _text.fontSize);
			_displayRoutine = StartCoroutine(DisplayRoutine(rows, displayTime, typewriterSpeed));
		}

		private void ResizeCanvasForContent(List<string> rows, float fontSize, float padding = 20f, float maxWidth = 250f)
		{
			// Calculate canvas width and height from text.
			float lineHeight = fontSize * 1.2f;
			float totalHeight = (rows.Count * lineHeight) + padding;

			float widest = 0f;
			foreach (string row in rows)
			{
				Vector2 size = _text.GetPreferredValues(row, Mathf.Infinity, Mathf.Infinity);
				if (size.x > widest)
					widest = size.x;
			}

			float totalWidth = Mathf.Min(widest + padding, maxWidth);

			RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
			canvasRect.sizeDelta = new Vector2(totalWidth, totalHeight);
		}

		private IEnumerator DisplayRoutine(List<string> rows, float displayTime, float typewriterSpeed)
		{
			_text.text = string.Empty;
			StartFade(_bgVisible, 0.3f);

			foreach (string row in rows)
			{
				yield return StartCoroutine(TypeLine(row, typewriterSpeed));
				_text.text += "\n";
				yield return new WaitForSeconds(0.5f);
			}

			yield return new WaitForSeconds(displayTime);
			_text.text = string.Empty;
			StartFade(_bgHidden, 0.3f);
			_displayRoutine = null;
		}

		private IEnumerator TypeLine(string line, float typewriterSpeed)
		{
			for (int i = 0; i < line.Length; i++)
			{
				_text.text += line[i];
				yield return new WaitForSeconds(typewriterSpeed);
			}
		}

		private void StartFade(Color target, float duration)
		{
			if (_fadeRoutine != null)
				StopCoroutine(_fadeRoutine);

			_fadeRoutine = StartCoroutine(FadeBackground(target, duration));
		}

		private IEnumerator FadeBackground(Color target, float duration)
		{
			Color start = _background.color;
			float t = 0f;

			while (t < 1f)
			{
				t += Time.deltaTime / duration;
				_background.color = Color.Lerp(start, target, t);
				yield return null;
			}

			_background.color = target;
			_fadeRoutine = null;
		}
	}
}
