using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLongWho.Common
{
	internal class WorldspaceDisplay : MonoBehaviour
	{
		public class Message
		{
			public List<string> Rows { get; set; }
			public float DisplayTime { get; set; }
			public float TypewriterSpeed { get; set; }

			public Message(List<string> rows, float displayTime = 5f, float typewriterSpeed = 0.03f)
			{
				Rows = rows;
				DisplayTime = displayTime;
				TypewriterSpeed = typewriterSpeed;
			}
		}

		private Canvas _canvas;
		private TextMeshProUGUI _text;
		private RectTransform _textRect;
		private RectTransform _canvasRect;
		private Camera _cam;
		private Image _background;

		private Color _bgVisible = new Color(0f, 0f, 0f, 0.6f);
		private Color _bgHidden = new Color(0f, 0f, 0f, 0f);

		private Vector3 _position = new Vector3(0, 0.13f, 0);
		private Vector2 _pivot = new Vector2(0.5f, 0f);
		private float _fontSize = 12;
		private float _maxWidth = 250;

		private bool _isReady = false;
		private Message _queuedMessage;

		private Coroutine _displayRoutine;
		private Coroutine _textRenderRoutine;
		private Coroutine _fadeRoutine;

		private void Init()
		{
			_cam = mainscript.M.player.Cam;

			// Create canvas.
			GameObject canvasObj = new GameObject("WorldspaceDisplay");
			canvasObj.transform.SetParent(transform, false);

			_canvas = canvasObj.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.worldCamera = _cam;
			canvasObj.AddComponent<CanvasScaler>();
			canvasObj.AddComponent<GraphicRaycaster>();

			_canvasRect = _canvas.GetComponent<RectTransform>();
			_canvasRect.sizeDelta = new Vector2(150, 100);
			_canvasRect.localPosition = _position;
			_canvasRect.localRotation = Quaternion.identity;
			_canvasRect.localScale = Vector3.one * 0.0025f;
			_canvasRect.anchorMin = _canvasRect.anchorMax = _canvasRect.pivot = _pivot;

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

			_text.fontSize = _fontSize;
			_text.alignment = TextAlignmentOptions.TopLeft;
			_text.text = string.Empty;
			// Change text material shader to not be affected by light.
			_text.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			_text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");

			_isReady = true;
		}

		private void Update()
		{
			if (_queuedMessage == null) return;
			if (!_isReady) return;
			RenderMessage(_queuedMessage);
			_queuedMessage = null;
		}

		private void LateUpdate()
		{
			if (!_isReady) return;

			// Always face camera.
			_canvas.transform.rotation = Quaternion.LookRotation(_canvas.transform.position - _cam.transform.position);
		}

		public void SetPosition(Vector3 pos) => _position = pos;
		public void SetPivot(Vector2 pos) => _pivot = pos;
		public void SetFontSize(float size) => _fontSize = size;
		public void SetMaxWidth(float maxWidth) => _maxWidth = maxWidth;

		public void RenderMessage(Message message)
		{
			if (!_isReady)
			{
				Init();
				_queuedMessage = message;
				return;
			}

			if (_displayRoutine != null)
				StopCoroutine(_displayRoutine);

			ResizeCanvasForContent(message.Rows);
			_displayRoutine = StartCoroutine(DisplayRoutine(message.Rows, message.DisplayTime, message.TypewriterSpeed));
		}

		public void ClearMessage()
		{
			// No text to clear, return early.
			if (string.IsNullOrEmpty(_text?.text))
				return;

			if (_displayRoutine != null)
				StopCoroutine(_displayRoutine);

			_displayRoutine = StartCoroutine(ClearRoutine());
		}

		private void ResizeCanvasForContent(List<string> rows, float padding = 20f)
		{
			// Calculate canvas width and height from text.
			float lineHeight = _text.fontSize * 1.2f;
			float totalHeight = rows.Count * lineHeight + padding;

			float widest = 0f;
			foreach (string row in rows)
			{
				Vector2 size = _text.GetPreferredValues(row, Mathf.Infinity, Mathf.Infinity);
				if (size.x > widest)
					widest = size.x;
			}

			float totalWidth = Mathf.Min(widest + padding, _maxWidth);

			RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
			canvasRect.sizeDelta = new Vector2(totalWidth, totalHeight);
		}

		private IEnumerator DisplayRoutine(List<string> rows, float displayTime, float typewriterSpeed)
		{
			_text.text = string.Empty;
			StartFade(_bgVisible, 0.3f);

			foreach (string row in rows)
			{
				yield return _textRenderRoutine = StartCoroutine(TypeLine(row, typewriterSpeed));
				_text.text += "\n";
				yield return new WaitForSeconds(0.5f);
			}

			yield return new WaitForSeconds(displayTime);
			yield return StartCoroutine(ClearRoutine());
		}

		private IEnumerator ClearRoutine()
		{
			if (_textRenderRoutine != null)
				StopCoroutine(_textRenderRoutine);

			_text.text = string.Empty;
			StartFade(_bgHidden, 0.3f);
			_displayRoutine = null;
			yield return null;
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
