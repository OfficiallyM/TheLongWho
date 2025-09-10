using System.Collections.Generic;
using TheLongWho.Extensions;
using TheLongWho.Tardis.Shell;
using TheLongWho.Tardis.System;
using TheLongWho.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLongWho.Tardis.Screen
{
	internal class ScreenSystem : TardisSystem
	{
		public override string Name => "Screen";

		private ShellController _shell;
		private Canvas _canvas;
		private PointerEventData _eventData;
		private List<RaycastResult> _results = new List<RaycastResult>();
		private Dictionary<string, GameObject> _menus = new Dictionary<string, GameObject>();
		private GameObject _currentMenu;
		private Button _backButton;
		private RawImage _screensaver;
		private List<Button> _buttons = new List<Button>();
		private bool _initialisedSystems = false;
		private Dictionary<string, TardisSystem> _systems = new Dictionary<string, TardisSystem>();

		private void Start()
		{
			_shell = GetComponent<ShellController>();
			_canvas = _shell.Interior.ScreenCanvas;
			_canvas.worldCamera = mainscript.M.player.Cam;

			GameObject screensaver = Instantiate(TheLongWho.I.UIImage, _canvas.transform);
			_screensaver = screensaver.GetComponent<RawImage>();
			_screensaver.texture = TheLongWho.I.ScreenImage;

			// Set up UI.
			_backButton = CreateButton("<", new Rect(0, 149, 25, 25));

			CreateMenu("main");
			ShowMenu("main");
			CreateButton("Systems", new Rect(61, 40, 200, 25), "main", "systems");
			CreateButton("Destinations", new Rect(61, 60, 200, 25), "main", "destinations");
			CreateButton("Rotation", new Rect(61, 80, 200, 25), "main", "rotation");
			CreateButton("Fast return", new Rect(61, 100, 200, 25), "main");

			CreateMenu("systems");
			// Can't populate systems menu here, they haven't registered yet.

			CreateMenu("destinations");
			CreateButton("Starter house", new Rect(65, 10, 200, 25), "destinations");

			CreateMenu("rotation");
			CreateButton("180 degrees", new Rect(65, 10, 200, 25), "rotation");
			CreateButton("-90 degrees", new Rect(65, 30, 200, 25), "rotation");
			CreateButton("90 degrees", new Rect(65, 50, 200, 25), "rotation");
		}

		private void Update()
		{
			// Cache screen systems once all systems are registered.
			if (!_initialisedSystems && Systems.HasRegisterFinished)
			{
				float y = 10f;
				foreach (TardisSystem system in Systems.GetScreenControlSystems())
				{
					_systems.Add(system.Name.ToMachineName(), system);
					CreateButton(system.Name, new Rect(65, y, 200, 25), "systems");
					y += 20f;
				}
				_initialisedSystems = true;
			}

			if (!_shell.IsInside()) return;

			fpscontroller player = mainscript.M.player;
			float distance = Vector3.Distance(player.transform.position, _canvas.transform.position);

			if (distance > 5f)
			{
				_currentMenu.SetActive(false);
				_screensaver.gameObject.SetActive(true);
				return;
			}

			if (!_currentMenu.activeSelf)
				ShowMenu(_currentMenu.name);
			if (_screensaver.gameObject.activeSelf)
				_screensaver.gameObject.SetActive(false);

			// Set button colours.
			foreach (Button btn in _buttons)
			{
				btn.image.color = btn.colors.normalColor;

				if (btn.name == "fast_return" && _shell.Materialisation.LastLocation == null)
					btn.image.color = btn.colors.disabledColor;

				string menu = btn.transform.parent.name;
				if (menu == "systems")
				{
					TardisSystem system = _systems[btn.name];
					if (!system.IsActive)
						btn.image.color = btn.colors.selectedColor;
				}
			}

			_eventData = new PointerEventData(EventSystem.current)
			{
				position = new Vector2(UnityEngine.Screen.width / 2, UnityEngine.Screen.height / 2)
			};

			_results.Clear();
			_canvas.GetComponent<GraphicRaycaster>().Raycast(_eventData, _results);

			if (_results.Count > 0)
			{
				var button = _results[0].gameObject.GetComponent<Button>();
				if (button != null && button.image.color != button.colors.disabledColor)
				{
					string interactText = "Select";
					if (button == _backButton)
						interactText = "Back";
					else if (button.name.Contains("trigger_"))
						interactText = "Open menu";
					else if (_currentMenu.name == "systems")
						interactText = "Toggle system";
					player.E = interactText;
					player.BcanE = true;
					if (Input.GetKeyDown(KeyCode.E))
						button.onClick.Invoke();
				}
			}
		}

		private Button CreateButton(string name, Rect position, string menu = null, string triggerMenu = null)
		{
			Button button = Instantiate(TheLongWho.I.UIButton, menu != null ? _menus[menu].transform : _canvas.transform).GetComponent<Button>();
			SetButtonRect(button.GetComponent<RectTransform>(), position);
			button.onClick.AddListener(() => OnButtonClick(button));
			button.name = triggerMenu == null ? name.ToMachineName() : "trigger_" + triggerMenu;
			button.GetComponentInChildren<TextMeshProUGUI>().text = name;

			// We're not using the colours for their named purposes.
			// selectedColor for disabled.
			ColorBlock colors = button.colors;
			colors.selectedColor = new Color(252 / 255f, 159 / 255f, 159 / 255f);
			button.colors = colors;
			_buttons.Add(button);
			return button;
		}

		private void SetButtonRect(RectTransform rt, Rect rect)
		{
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);

			rt.sizeDelta = new Vector2(rect.width, rect.height);
			rt.anchoredPosition = new Vector2(rect.x / 100f, -(rect.y / 100f));
		}

		private void CreateMenu(string name)
		{
			GameObject menu = new GameObject(name);
			menu.transform.SetParent(_canvas.transform, false);
			menu.SetActive(false);
			_menus.Add(name, menu);
		}

		private void ShowMenu(string menu)
		{
			if (_currentMenu != null)
				_currentMenu.SetActive(false);

			_currentMenu = _menus[menu];
			_currentMenu.SetActive(true);

			_backButton.gameObject.SetActive(true);
			if (menu == "main")
				_backButton.gameObject.SetActive(false);
		}

		private void OnButtonClick(Button button)
		{
			if (button == _backButton)
			{
				ShowMenu("main");
				return;
			}

			if (button.name.Contains("trigger_"))
			{
				string menu = button.name.Replace("trigger_", "");
				ShowMenu(menu);
				return;
			}

			switch (_currentMenu.name)
			{
				case "main":
					switch (button.name)
					{
						case "fast_return":
							_shell.Materialisation.Materialise(_shell.Materialisation.LastLocation.Position, _shell.Materialisation.LastLocation.Rotation);
							break;
					}
					break;

				case "systems":
					TardisSystem system = _systems[button.name];
					if (system.IsActive)
						system.Deactivate();
					else
						system.Activate();
					break;

				case "destinations":
					switch (button.name)
					{
						case "starter_house":
							foreach (buildingscript building in savedatascript.d.buildings)
							{
								if (building.name.ToLower().Contains("haz02"))
								{
									Transform transform = building.transform.Find("interiorKitchen");
									if (transform != null)
									{
										Vector3 pos = transform.position + Vector3.back * 10f + Vector3.up * 5f;
										_shell.Materialisation.Materialise(WorldUtilities.GetGlobalObjectPosition(pos), transform.rotation);
									}
								}
							}
							break;
					}
					break;

				case "rotation":
					string rotationString = button.name.Replace(" degrees", "");
					if (float.TryParse(rotationString, out float rotation))
					{
						Quaternion currentRot = _shell.transform.rotation;
						Quaternion extraRot = Quaternion.Euler(0f, rotation, 0f);
						Quaternion targetRot = currentRot * extraRot;
						_shell.Materialisation.Materialise(_shell.transform.position, targetRot, shouldSaveLocation: false);
					}
					break;
			}

			// Return back to main menu on selection.
			if (_currentMenu.name != "systems")
				ShowMenu("main");
		}
	}
}
