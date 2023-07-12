using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameMainRecastCrowd : MonoBehaviour
{
	public Transform mainCharacter;
	public Joystick joystick;
	public float speed = 5f;

	public Transform character1;
	public Transform character2;

	public TextAsset navMeshText;

	private IntPtr navMeshScene;
	private RecastAgent recastAgent;

	private int agentId1;
	private int agentId2;
	private float[] targetPos1 = new float[3];
	private float[] targetPos2 = new float[3];

	private float[] tmpPos = new float[3];


	private void Awake()
	{
		var bytes = navMeshText.bytes;
		navMeshScene = RecastDll.RecastLoad(1, bytes, bytes.Length);
		if (navMeshScene == IntPtr.Zero)
		{
			Debug.LogError("Load Recast Data failed!");
			return;
		}
		recastAgent = RecastAgent.Create(mainCharacter, navMeshScene);

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

		recastAgent.SetCrowdAgent();

		//添加巡逻npc
		RecastDll.RecastFindNearestPoint(navMeshScene, RecastUtility.DefaultHalfExtents, RecastUtility.UnityPos2RecastPosNew(character1.position), targetPos1);
		character1.position = RecastUtility.RecastPos2UnityPos(targetPos1);
		agentId1 = RecastDll.RecastAddAgent(navMeshScene, targetPos1, 0.5f, 2, 3.5f, 8);

		RecastDll.RecastFindNearestPoint(navMeshScene, RecastUtility.DefaultHalfExtents, RecastUtility.UnityPos2RecastPosNew(character2.position), targetPos2);
		character2.position = RecastUtility.RecastPos2UnityPos(targetPos2);
		agentId2 = RecastDll.RecastAddAgent(navMeshScene, targetPos2, 0.5f, 2, 3.5f, 8);

		//交互位置，设置为目标位置
		RecastUtility.SwapRecastPos(ref targetPos1, ref targetPos2);

		//相互对冲
		RecastDll.RecastSetAgentMoveTarget(navMeshScene, agentId1, targetPos1);
		RecastDll.RecastSetAgentMoveTarget(navMeshScene, agentId2, targetPos2);
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

	private void UpdateRecastCrowd()
	{
		//更新人群
		RecastDll.RecastUpdate(navMeshScene, Time.deltaTime);

		//从recast中取位置更新表现
		RecastDll.RecastGetAgentPos(navMeshScene, agentId1, tmpPos);
		character1.position = RecastUtility.RecastPos2UnityPos(tmpPos);

		RecastDll.RecastGetAgentPos(navMeshScene, agentId2, tmpPos);
		character2.position = RecastUtility.RecastPos2UnityPos(tmpPos);


		//无限相互对冲
		if (Vector3.Distance(character1.position, RecastUtility.RecastPos2UnityPos(targetPos1)) < 0.1f
			&& Vector3.Distance(character2.position, RecastUtility.RecastPos2UnityPos(targetPos2)) < 0.1f)
		{
			RecastUtility.SwapRecastPos(ref targetPos1, ref targetPos2);

			RecastDll.RecastSetAgentMoveTarget(navMeshScene, agentId1, targetPos1);
			RecastDll.RecastSetAgentMoveTarget(navMeshScene, agentId2, targetPos2);
		}

	}

	private void Update()
	{
		this.recastAgent.Update();
		UpdateRecastCrowd();

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
