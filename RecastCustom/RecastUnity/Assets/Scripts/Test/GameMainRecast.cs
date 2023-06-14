using System;
using UnityEngine;

public class GameMainRecast : MonoBehaviour
{
	public Transform character;
	public Joystick joystick;
	public TextAsset navMeshText;
	public float speed = 5f;

	private IntPtr navMeshScene;
	private float[] halfExtents = new float[3] { 2, 4, 2 };
	private float[] sharedStartPos = new float[3];
	private float[] sharedEndPos = new float[3];
	private float[] sharedRealEndPos = new float[3];

	private void Awake()
	{
		var bytes = navMeshText.bytes;
		navMeshScene = RecastDll.RecastLoad(1, bytes, bytes.Length);
		if (navMeshScene == IntPtr.Zero)
		{
			Debug.LogError("Load Recast Data failed!");
		}
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
		if (FindNearestPoint(character.transform.position, out var realEndPos))
		{
			character.transform.position = realEndPos;
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
		var forward = GameUtil.mainCamera.transform.forward;
		forward = Vector3.ProjectOnPlane(forward, Vector3.up);
		var rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0, -joystickData.angle + 90, 0);
		var offset = rotation * Vector3.forward * speed * joystickData.power * Time.deltaTime;
		var tryEndPos = character.transform.position + offset;


		if (TryMove(character.transform.position, tryEndPos, out var realEndPos))
		{
			character.transform.rotation = rotation;
			character.transform.position = realEndPos;
		}

	}

	private bool TryMove(Vector3 startPos, Vector3 endPos, out Vector3 realEndPos)
	{
		sharedStartPos[0] = -startPos.x;
		sharedStartPos[1] = startPos.y;
		sharedStartPos[2] = startPos.z;

		sharedEndPos[0] = -endPos.x;
		sharedEndPos[1] = endPos.y;
		sharedEndPos[2] = endPos.z;


		int result = RecastDll.RecastTryMove(navMeshScene, halfExtents, sharedStartPos, sharedEndPos, sharedRealEndPos);

		realEndPos = new Vector3(-sharedRealEndPos[0], sharedRealEndPos[1], sharedRealEndPos[2]);

		if (result < 0)
		{
			Debug.LogError("Recast TryMove failed,result:" + result);
			return false;
		}
		return true;

	}

	private bool FindNearestPoint(Vector3 startPos, out Vector3 realEndPos)
	{
		sharedStartPos[0] = -startPos.x;
		sharedStartPos[1] = startPos.y;
		sharedStartPos[2] = startPos.z;

		int result = RecastDll.RecastFindNearestPoint(navMeshScene, halfExtents, sharedStartPos, sharedRealEndPos);

		realEndPos = new Vector3(-sharedRealEndPos[0], sharedRealEndPos[1], sharedRealEndPos[2]);

		if (result < 0)
		{
			Debug.LogError("Recast FindNearestPoint failed,result:" + result);
			return false;
		}
		return true;
	}

	private void OnJoystickUp()
	{

	}
}
