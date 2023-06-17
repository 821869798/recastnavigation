using System;
using UnityEngine;
using UnityEngine.AI;

public class GameMainRecast : MonoBehaviour
{
	public Transform character;
	public Joystick joystick;
	public TextAsset navMeshText;
	public float speed = 5f;

	private IntPtr navMeshScene;
	private RecastAgent recastAgent;

	private void Awake()
	{
		var bytes = navMeshText.bytes;
		navMeshScene = RecastDll.RecastLoad(1, bytes, bytes.Length);
		if (navMeshScene == IntPtr.Zero)
		{
			Debug.LogError("Load Recast Data failed!");
			return;
		}
		recastAgent = RecastAgent.Create(character, navMeshScene);
	}

	private void OnDestroy()
	{
		if (navMeshScene != IntPtr.Zero)
		{
			RecastDll.RecastClear(navMeshScene);
			navMeshScene = IntPtr.Zero;
		}
	}

	private void OnEnable()
	{
		//设置到正确的起始位置
		if (recastAgent.FindNearestPoint(recastAgent.transform.position, out var realEndPos))
		{
			recastAgent.transform.position = realEndPos;
		}

		joystick.onTouchMove += OnJoystickMove;
		joystick.onTouchUp += OnJoystickUp;
	}

	private void OnDisable()
	{
		joystick.onTouchMove -= OnJoystickMove;
		joystick.onTouchUp -= OnJoystickUp;
	}

	private void OnJoystickMove(JoystickData joystickData)
	{
		recastAgent.ResetPath();

		var forward = GameUtil.mainCamera.transform.forward;
		forward = Vector3.ProjectOnPlane(forward, Vector3.up);
		recastAgent.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0, -joystickData.angle + 90, 0);
		var offset = recastAgent.transform.forward * speed * joystickData.power * Time.deltaTime;
		recastAgent.Move(offset);

	}

	private void OnJoystickUp()
	{

	}

	private void Update()
	{
		this.recastAgent.Update();

		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = GameUtil.mainCamera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out var hit))
			{
				recastAgent.SetDestination(hit.point);
			}
		}
	}
}
