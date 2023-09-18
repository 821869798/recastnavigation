using RecastUnity;
using UnityEngine;


/// <summary>
/// 人群动态避障寻路测试
/// </summary>
public class GameMainRecastCrowd : GameMainRecast
{
	public Transform character1;
	public Transform character2;

	private int agentId1;
	private int agentId2;
	private float[] targetPos1 = new float[3];
	private float[] targetPos2 = new float[3];

	private float[] tmpPos = new float[3];


	protected override void OnEnable()
	{
		base.OnEnable();
		//设置为自动更新人群位置的agent，适用于主角
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

	protected override void Update()
	{
		this.recastAgent.Update();

		//更新人群数据
		this.UpdateRecastCrowd();

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
