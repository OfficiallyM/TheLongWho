using System.Collections.Generic;
using TheLongWho.Save;
using UnityEngine;

namespace TheLongWho.Enemies.Angel
{
	public class AngelController : MonoBehaviour
	{
		public SaveController SaveController;

		private GameObject _angelIdle;
		private GameObject _angelAttack;
		private GameObject _angelPoint;
		private AngelState _angelState;
		private bool _isObserved = false;
		private bool _justMoved = false;
		private GameObject _target;

		private const float LockRadius = 20f;
		private const float UnlockRadius = 100f;
		private const float AttackRadius = 1.5f;
		private const float KillRadius = 1f;
		private const float MoveSpeed = 2f;

		private static List<AngelController> _angels = new List<AngelController>();
		
		public enum AngelState
		{
			Idle,
			Attack,
			Point,
		}

		private void Awake()
		{
			_angelIdle = transform.Find("AngelIdle").gameObject;
			_angelAttack = transform.Find("AngelAttack").gameObject;
			_angelPoint = transform.Find("AngelPoint").gameObject;
			SaveController = gameObject.AddComponent<SaveController>();
		}

		private void Start()
		{
			SetState(AngelState.Idle);

			// Add a dummy tosaveitemscript to allow M-ultiTool to delete.
			gameObject.AddComponent<tosaveitemscript>();

			// Trigger a manual save.
			SaveManager.Save(SaveController, true);
		}

		private void OnEnable() => _angels.Add(this);
		private void OnDisable() => _angels.Remove(this);

		private void Update()
		{
			_isObserved = IsObserved();
			fpscontroller player = mainscript.M.player;

			// Find target.
			if (_target == null && !IsDead(player.gameObject) && Vector3.Distance(transform.position, player.transform.position) < LockRadius)
			{
				_target = player.gameObject;
			}

			// Follow target.
			if (_target != null && !_isObserved)
			{
				float distance = Vector3.Distance(transform.position, player.transform.position);

				if (distance >= UnlockRadius)
				{
					// Release if out of range.
					_target = null;
				}
				else
				{
					// In range, move towards.
					MoveTowardTarget();
					_justMoved = true;

					if (distance <= AttackRadius)
					{
						SetState(AngelState.Attack);

						if (distance <= KillRadius)
						{
							KillTarget();
							_target = null;
						}
					}
					else
					{
						SetState(AngelState.Idle);
					}
				}
			}
			else if (_isObserved && _angelState != AngelState.Attack && _justMoved)
			{
				// 1% chance of pointing instead of idling.
				if (Random.value < 0.01f) 
					SetState(AngelState.Point);
				else
					SetState(AngelState.Idle);

				_justMoved = false;
			}

			// Keep on ground.
			if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 3f))
				transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
		}

		private bool IsObserved()
		{
			foreach (camscript camscript in mainscript.M.Cams)
			{
				if (camscript.distantCamera || camscript.skyCamera) continue;

				// Camera is a mirror, continue if player is not looking at the mirror.
				if (camscript.mirror && !IsVisibleFrom(camscript.transform.root.gameObject, mainscript.M.player.Cam)) continue;

				if (IsVisibleFrom(camscript.cam)) return true;
			}

			// Check for angels looking at this angel.
			foreach (var angel in _angels)
			{
				int index = _angels.IndexOf(angel);

				if (angel == this) continue;
				if (angel._angelState == AngelState.Idle) continue;

				// Ignore angels too far away.
				float distance = Vector3.Distance(transform.position, angel.transform.position);
				if (distance > 100f) continue;

				Vector3 origin = angel.transform.position + Vector3.up * 1.5f + angel.transform.forward * 2f;

				// Target this angel’s center.
				Renderer rend = GetComponentInChildren<Renderer>();
				if (rend == null) continue;
				Vector3 target = rend.bounds.center;
				Vector3 dir = target - origin;

				if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, distance))
				{
					if (hit.collider.transform.IsChildOf(transform))
						return true;
				}
			}

			return false;
		}

		private bool IsVisibleFrom(Camera cam)
		{
			return IsVisibleFrom(gameObject, cam);
		}

		private bool IsVisibleFrom(GameObject obj, Camera cam)
		{
			if (cam == null) return false;

			Renderer renderer = obj.GetComponentInChildren<Renderer>();
			if (renderer == null) return false;

			Vector3 targetPos = renderer.bounds.center;
			Vector3 viewportPos = cam.WorldToViewportPoint(targetPos);

			// Check if within the viewport rectangle and in front of the camera.
			if (viewportPos.z <= 0f ||
				viewportPos.x < 0f || viewportPos.x > 1f ||
				viewportPos.y < 0f || viewportPos.y > 1f)
			{
				return false;
			}

			// Line of sight check.
			Vector3 camPos = cam.transform.position;
			Vector3 dir = targetPos - camPos;
			foreach (RaycastHit hit in Physics.RaycastAll(camPos, dir.normalized, dir.magnitude))
			{
				if (hit.collider.transform.IsChildOf(obj.transform)) 
					return true;
			}

			return false;
		}

		private void MoveTowardTarget()
		{
			if (_target == null) return;
			Vector3 targetPos = _target.transform.position;
			targetPos.y = transform.position.y;
			transform.position = Vector3.MoveTowards(transform.position, targetPos, MoveSpeed * Time.deltaTime);
			LookAtFlat(_target.transform);
		}

		private void LookAtFlat(Transform target)
		{
			Vector3 direction = target.position - transform.position;
			// Ignore vertical.
			direction.y = 0f; 
			if (direction.sqrMagnitude > 0.001f)
				transform.rotation = Quaternion.LookRotation(direction);
		}

		private bool IsDead(GameObject obj)
		{
			fpscontroller player = obj.GetComponent<fpscontroller>();
			if (player != null && player.died) return true;

			return false;
		}

		private void KillTarget()
		{
			if (_target == null) return;

			fpscontroller player = _target.GetComponent<fpscontroller>();
			if (player != null)
				player.Death();
		}

		private void SetState(AngelState state)
		{
			switch (state)
			{
				case AngelState.Idle:
					_angelIdle.SetActive(true);
					_angelAttack.SetActive(false);
					_angelPoint.SetActive(false);
					break;
				case AngelState.Attack:
					_angelIdle.SetActive(false);
					_angelAttack.SetActive(true);
					_angelPoint.SetActive(false);
					break;
				case AngelState.Point:
					_angelIdle.SetActive(false);
					_angelAttack.SetActive(false);
					_angelPoint.SetActive(true);
					break;
			}

			_angelState = state;
		}
	}
}
