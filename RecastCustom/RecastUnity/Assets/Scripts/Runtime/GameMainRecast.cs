using System;
using UnityEngine;

/// <summary>
/// 基础recast寻路测试
/// </summary>
public class GameMainRecast : MonoBehaviour
{
	public Transform mainCharacter;
	public Joystick joystick;
	public TextAsset navMeshText;
	public float speed = 5f;
	/// <summary>
	/// 主角半径
	/// </summary>
	public const float CharcterRadius = 0.5f;

	protected IntPtr navMeshScene;
	protected RecastAgent recastAgent;

	public virtual bool isDynamicScene => false;

	protected virtual void Awake()
	{
		var bytes = navMeshText.bytes;
		navMeshScene = RecastDll.RecastLoad(1, bytes, bytes.Length, isDynamicScene);
		if (navMeshScene == IntPtr.Zero)
		{
			Debug.LogError("Load Recast Data failed!");
			return;
		}
		if (RecastUtility.RecastTryGetBounds(navMeshScene, out var bounds))
		{
			Debug.Log($"Recast Bounds min:{bounds.min} max: {bounds.max}");
		}

		recastAgent = RecastAgent.Create(mainCharacter, navMeshScene);
	}

	protected virtual void OnDestroy()
	{
		if (navMeshScene != IntPtr.Zero)
		{
			RecastDll.RecastClear(navMeshScene);
			navMeshScene = IntPtr.Zero;
		}
	}


	protected virtual void OnEnable()
	{
		//设置到正确的起始位置
		if (recastAgent.FindNearestPoint(recastAgent.transform.position, out var realEndPos))
		{
			recastAgent.transform.position = realEndPos;
		}

		joystick.onTouchMove += OnJoystickMove;
		joystick.onTouchUp += OnJoystickUp;
	}

	protected virtual void OnDisable()
	{
		joystick.onTouchMove -= OnJoystickMove;
		joystick.onTouchUp -= OnJoystickUp;
	}

	protected virtual void OnJoystickMove(JoystickData joystickData)
	{
		recastAgent.ResetPath();

		var forward = GameUtil.mainCamera.transform.forward;
		forward = Vector3.ProjectOnPlane(forward, Vector3.up);
		recastAgent.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0, -joystickData.angle + 90, 0);
		var offset = recastAgent.transform.forward * speed * joystickData.power * Time.deltaTime;
		recastAgent.Move(offset);

	}

	protected virtual void OnJoystickUp()
	{

	}

	protected virtual void Update()
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
