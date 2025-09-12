using System.Collections.Generic;
using System.Linq;
using TheLongWho.Extensions;
using TheLongWho.Tardis.Materialisation;
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

		private Vector2 _canvasSize;
		private Dictionary<string, GameObject> _menus = new Dictionary<string, GameObject>();
		private List<Button> _buttons = new List<Button>();
		private RawImage _screensaver;
		private GameObject _currentMenu;
		private Button _backButton;
		private Button _createDestinationButton;
		private bool _isCreatingDestination = false;
		private string _destinationName;

		private bool _initialisedSystems = false;
		private Dictionary<string, TardisSystem> _systems = new Dictionary<string, TardisSystem>();

		private void Start()
		{
			_shell = GetComponent<ShellController>();
			_canvas = _shell.Interior.ScreenCanvas;
			_canvas.worldCamera = mainscript.M.player.Cam;
			_canvasSize = _canvas.GetComponent<RectTransform>().rect.size;

			TheLongWho.I.OnForceReleaseUIControl += OnForceReleaseUIControl;

			GameObject screensaver = Instantiate(TheLongWho.I.UIImage, _canvas.transform);
			_screensaver = screensaver.GetComponent<RawImage>();
			_screensaver.texture = TheLongWho.I.ScreenImage;

			// Set up UI.
			_backButton = CreateButton("<", new Rect(0, 145, 15, 15));
			_createDestinationButton = CreateButton("+", new Rect(0, 0, 15, 15));

			CreateMenu("main");
			ShowMenu("main");
			CreateButton("Systems", new RectPercent(50, 25, 75, 10), "main", "systems");
			CreateButton("Destinations", new RectPercent(50, 41.67f, 75, 10), "main", "destinations");
			CreateButton("Rotation", new RectPercent(50, 58.33f, 75, 10), "main", "rotation");
			CreateButton("Fast return", new RectPercent(50, 75, 75, 10), "main");

			CreateMenu("systems");
			// Can't populate systems menu here, they haven't registered yet.

			CreateMenu("destinations");
			CreateButton("Custom destinations", new RectPercent(50, 25, 75, 10), "destinations", "custom");
			BuildCustomDestinationMenus();
			CreateButton("Starter house", new RectPercent(50, 75, 75, 10), "destinations");

			CreateMenu("rotation");
			CreateButton("180 degrees", new RectPercent(50, 25, 75, 10), "rotation");
			CreateButton("-90 degrees", new RectPercent(50, 50, 75, 10), "rotation");
			CreateButton("90 degrees", new RectPercent(50, 75, 75, 10), "rotation");
		}

		private void Update()
		{
			// Cache screen systems once all systems are registered.
			if (!_initialisedSystems && Systems.HasRegisterFinished)
			{
				float y = 25f;
				foreach (TardisSystem system in Systems.GetScreenControlSystems())
				{
					_systems.Add(system.Name.ToMachineName(), system);
					CreateButton(system.Name, new RectPercent(50, y, 75, 10), "systems");
					y += 10f;
				}
				_initialisedSystems = true;
			}

			if (!_shell.IsInside()) return;

			fpscontroller player = mainscript.M.player;
			float distance = Vector3.Distance(player.transform.position, _canvas.transform.position);

			if (distance > 5f)
			{
				_currentMenu.SetActive(false);
				_backButton.gameObject.SetActive(false);
				_createDestinationButton.gameObject.SetActive(false);
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
						interactText = "Back to main menu";
					else if (_currentMenu.name.Contains("custom"))
					{
						if (button.name == "+")
							interactText = "Add new custom destination";
						else if (button.name.Contains("trigger_"))
							interactText = "Change page";
						else if (button.name.Contains("delete_"))
							interactText = "Delete destination";
						else
							interactText = "Materialise to";
					}
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

		private Button CreateButton(string name, Rect position, string menu = null, string triggerMenu = null, bool useMiddleAnchor = false, string overrideLabel = null)
		{
			Button button = Instantiate(TheLongWho.I.UIButton, menu != null ? _menus[menu].transform : _canvas.transform).GetComponent<Button>();
			SetButtonRect(button.GetComponent<RectTransform>(), position, useMiddleAnchor);
			button.onClick.AddListener(() => OnButtonClick(button));
			button.name = triggerMenu == null ? name.ToMachineName() : "trigger_" + triggerMenu;
			var label = button.GetComponentInChildren<TextMeshProUGUI>();
			label.text = overrideLabel != null ? overrideLabel : name;
			label.fontSize = 15;

			// We're not using the colours for their named purposes.
			// selectedColor for disabled.
			ColorBlock colors = button.colors;
			colors.selectedColor = new Color(252 / 255f, 159 / 255f, 159 / 255f);
			button.colors = colors;
			_buttons.Add(button);
			return button;
		}

		private Button CreateButton(string name, RectPercent rectPercent, string menu = null, string triggerMenu = null, string overrideLabel = null)
		{
			return CreateButton(name, rectPercent.ToRect(_canvasSize), menu, triggerMenu, true, overrideLabel);
		}

		private void SetButtonRect(RectTransform rt, Rect rect, bool useMiddleAnchor)
		{
			// Prefab scale is 0.01, so we need to multiply the size by 100.
			Vector2 size = new Vector2(rect.width, rect.height);
			if (useMiddleAnchor)
				size *= 100f;
			Vector2 pos = new Vector2(rect.x, rect.y);

			if (useMiddleAnchor)
			{
				// Center-anchored, pivot in the middle.
				rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
			}
			else
			{
				// Top-left anchor, pivot in top-left.
				rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
			}
			// Unity UI y+ is up, but rects are y+ is down.
			pos.y = -pos.y;

			// Scale down for absolute positioned elements to make positioning use nicer numbers.
			if (!useMiddleAnchor)
				pos /= 100f;

			rt.sizeDelta = size;
			rt.anchoredPosition = pos;
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

			// Only show the create destination button on a custom destination menu.
			_createDestinationButton.gameObject.SetActive(false);
			if (menu.Contains("custom"))
				_createDestinationButton.gameObject.SetActive(true);
		}

		private void OnButtonClick(Button button)
		{
			// Handle back button.
			if (button == _backButton)
			{
				ShowMenu("main");
				return;
			}

			// Handle menu switch buttons.
			if (button.name.Contains("trigger_"))
			{
				string menu = button.name.Replace("trigger_", "");
				ShowMenu(menu);
				return;
			}

			// Handle add custom location button.
			if (button == _createDestinationButton)
			{
				TheLongWho.I.ToggleUIControl(true);
				_isCreatingDestination = true;
				return;
			}

			// Handle generic buttons.
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

			if (_currentMenu.name.Contains("custom"))
			{
				if (button.name.Contains("delete_"))
				{
					string destination = button.name.Replace("delete_", "");
					_shell.Materialisation.DeleteCustomDestination(destination);
					BuildCustomDestinationMenus();
					ShowMenu("custom");
				}
				else
				{
					Location destination = _shell.Materialisation.GetCustomDestination(button.name);
					if (destination != null)
						_shell.Materialisation.Materialise(destination.Position, destination.Rotation);
				}
				return;
			}

			// Return back to main menu on selection.
			if (_currentMenu.name != "systems")
				ShowMenu("main");
		}

		private void OnForceReleaseUIControl()
		{
			_isCreatingDestination = false;
			_destinationName = string.Empty;
		}

		private void OnGUI()
		{
			if (!_isCreatingDestination) return;

			int screenWidth = TheLongWho.I.ScreenWidth;
			int screenHeight = TheLongWho.I.ScreenHeight;
			float boxWidth = screenWidth / 5;
			GUILayout.BeginArea(new Rect(0, 0, screenWidth, screenHeight));
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical("box", GUILayout.Width(boxWidth));
			GUILayout.Label("Destination name");
			_destinationName = GUILayout.TextField(_destinationName);
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Create destination at current location", GUILayout.ExpandWidth(false)))
			{
				_shell.Materialisation.AddCustomDestination(_destinationName, Location.FromTransform(_shell.transform));
				BuildCustomDestinationMenus();
				ShowMenu(GetLastCustomMenuName());
				TheLongWho.I.ToggleUIControl(false);
				_isCreatingDestination = false;
				_destinationName = string.Empty;
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Exit", GUILayout.ExpandWidth(false)))
			{
				TheLongWho.I.ToggleUIControl(false);
				_isCreatingDestination = false;
				_destinationName = string.Empty;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();
		}

		private void BuildCustomDestinationMenus()
		{
			// Remove any existing custom destination menus.
			List<string> oldCustomMenus = _menus.Keys.Where(k => k.StartsWith("custom")).ToList();
			foreach (string menu in oldCustomMenus)
			{
				foreach (Button button in _menus[menu].GetComponentsInChildren<Button>())
					_buttons.Remove(button);
				Destroy(_menus[menu]);
				_menus.Remove(menu);
			}

			var destinations = _shell.Materialisation.CustomDestinations.ToList();

			// No destinations, ensure a placeholder menu is still created.
			if (destinations.Count == 0)
			{
				CreateMenu("custom");
				return;
			}

			int perPage = 4;
			int totalPages = Mathf.CeilToInt(destinations.Count / (float)perPage);

			float[] yPositions = { 25f, 41.67f, 58.33f, 75f };

			for (int page = 0; page < totalPages; page++)
			{
				string menuName = page == 0 ? "custom" : $"custom_{page}";
				CreateMenu(menuName);

				// Create destination buttons
				for (int i = 0; i < perPage; i++)
				{
					int index = page * perPage + i;
					if (index >= destinations.Count) break;

					var dest = destinations[index];
					CreateButton(dest.Key, new RectPercent(45, yPositions[i], 75, 10), menuName);
					CreateButton($"delete_{dest.Key}", new RectPercent(90, yPositions[i], 7, 10), menuName, overrideLabel: "X");
				}

				// Add navigation.
				if (page > 0)
				{
					CreateButton("Prev", new RectPercent(25, 92, 20, 8), menuName, page - 1 == 0 ? "custom" : $"custom_{page - 1}");
				}

				if (page < totalPages - 1)
				{
					CreateButton("Next", new RectPercent(75, 92, 20, 8), menuName, $"custom_{page + 1}");
				}
			}
		}

		private string GetLastCustomMenuName()
		{
			int perPage = 4;
			int total = _shell.Materialisation.CustomDestinations.Count;
			int totalPages = Mathf.CeilToInt(total / (float)perPage);
			return totalPages <= 1 ? "custom" : $"custom_{totalPages - 1}";
		}
	}
}
