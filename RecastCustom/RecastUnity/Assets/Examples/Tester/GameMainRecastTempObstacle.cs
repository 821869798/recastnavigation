using RecastUnity;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可添加动态障碍物的寻路测试
/// </summary>
public class GameMainRecastTempObstacle : GameMainRecast
{
	public override bool isDynamicScene => true;

	public BoxCollider boxObstacle;
	public Dictionary<BoxCollider, uint> obstacleIdMaping = new Dictionary<BoxCollider, uint>();

	private float[] targetPos = new float[3];
	private float[] halfExtents = new float[3];

	private void AddBoxObstacle(Vector3 pos)
	{
		if (boxObstacle == null)
		{
			Debug.LogError("boxObstacle prefab is null");
			return;
		}
		var obj = Object.Instantiate(boxObstacle.gameObject, boxObstacle.transform.parent);
		obj.SetActive(true);
		var collider = obj.GetComponent<BoxCollider>();
		collider.transform.position = pos;

		RecastUtility.UnityPos2RecastPos(pos, targetPos);
		var size = Vector3.Scale(collider.size, collider.transform.localScale);
		//需要加上主角半径不能移动
		halfExtents[0] = size.x / 2 + CharcterRadius;
		halfExtents[1] = size.y / 2 + CharcterRadius;
		halfExtents[2] = size.z / 2 + CharcterRadius;

		uint obstacleId = 0;
		int result = RecastDll.RecastAddBoxCenterObstacle(navMeshScene, ref obstacleId, targetPos, halfExtents, 0);
		if (result != 0)
		{
			Debug.LogError("add box obstacle failed,result:" + result);
			Object.Destroy(obj);
			return;
		}
		obj.name = "obstacle-" + obstacleId;
		obstacleIdMaping.Add(collider, obstacleId);

		//一次完整的重建mesh，才能刷新动态障碍物
		result = RecastDll.RecastUpdateObstacles(navMeshScene);
		if (result != 0)
		{
			Debug.LogError("update obstacle rebuild mesh failed,result:" + result);
			return;
		}
#if UNITY_EDITOR
		if (enableDebug)
		{
			if (RecastUtility.RecastTryGetNavMesh(navMeshScene, out var mesh))
			{
				RecastNavMeshDebugInfo.Instance.SetMeshDraw(mesh);
			}
		}
#endif
	}

	private void RemoveBoxObstacle(BoxCollider box)
	{
		if (box == null)
		{
			return;
		}
		if (!obstacleIdMaping.TryGetValue(box, out var obstacleId))
		{
			return;
		}
		int result = RecastDll.RecastRemoveObstacle(navMeshScene, obstacleId);
		if (result != 0)
		{
			Debug.LogError("remove box obstacle failed,result:" + result);
			return;
		}

		obstacleIdMaping.Remove(box);
		Object.Destroy(box.gameObject);

		//一次完整的重建mesh，才能刷新动态障碍物
		result = RecastDll.RecastUpdateObstacles(navMeshScene);
		if (result != 0)
		{
			Debug.LogError("update obstacle rebuild mesh failed,result:" + result);
			return;
		}

#if UNITY_EDITOR
		if (enableDebug)
		{
			if (RecastUtility.RecastTryGetNavMesh(navMeshScene, out var mesh))
			{
				RecastNavMeshDebugInfo.Instance.SetMeshDraw(mesh);
			}
		}
#endif
	}


	protected override void Update()
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
		else if (Input.GetMouseButtonDown(1))
		{
			Ray ray = GameUtil.mainCamera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out var hit))
			{
				AddBoxObstacle(hit.point);
			}
		}
		else if (Input.GetMouseButtonDown(2))
		{
			Ray ray = GameUtil.mainCamera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out var hit))
			{
				RemoveBoxObstacle(hit.collider as BoxCollider);
			}
		}

		//RecastDll.RecastUpdateObstacles(navMeshScene,true);
	}
}
