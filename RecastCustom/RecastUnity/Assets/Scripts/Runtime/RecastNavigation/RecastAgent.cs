using System;
using Unity.VisualScripting;
using UnityEngine;

public class RecastAgent
{
	private IntPtr navMeshScene;
	public Transform transform { private set; get; }

	public virtual float radius { protected set; get; } = 0.5f;

	public virtual float height { protected set; get; } = 2f;

	public float speed { get; set; } = 5;

	/// <summary>
	/// 人群的id，用于动态避障
	/// </summary>
	public int crowdAgentId { private set; get; } = -1;

	public float[] halfExtents = new float[3] { 2, 4, 2 };

	protected float[] sharedStartPos = new float[3];
	protected float[] sharedEndPos = new float[3];
	protected float[] sharedRealEndPos = new float[3];
	protected float[] smoothPath = new float[256 * 3];


	protected int nextPosIndex;
	protected int smoothPathCount;
	protected Vector3 nextPos;

	/// <summary>
	/// 是否移动中
	/// </summary>
	public bool pathPending { protected get; set; }

	private RecastAgent()
	{

	}

	public static RecastAgent Create(Transform transform, IntPtr navMeshScene)
	{
		RecastAgent recastAgent = new RecastAgent();
		recastAgent.transform = transform;
		recastAgent.navMeshScene = navMeshScene;
		return recastAgent;
	}

	public void SetCrowdAgent()
	{
		RecastUtility.UnityPos2RecastPos(transform.position, sharedEndPos);
		crowdAgentId = RecastDll.RecastAddAgent(navMeshScene, sharedRealEndPos, radius, height, speed, 0f);
	}

	public virtual void UpdateCrowdAgent()
	{
		if (crowdAgentId > -1)
		{
			RecastUtility.UnityPos2RecastPos(transform.position, sharedEndPos);
			int result = RecastDll.RecastSetAgentPos(navMeshScene, crowdAgentId, sharedEndPos);
			if (result < 0)
			{
				Debug.LogError("Recast UpdateCrowdAgent failed:" + result);
			}
		}

	}

	public virtual void Move(Vector3 offset)
	{
		var tryEndPos = this.transform.position + offset;
		if (TryMove(this.transform.position, tryEndPos, out var realEndPos))
		{
			TurnLook(realEndPos);
			this.transform.position = realEndPos;
			UpdateCrowdAgent();
		}
	}


	protected virtual void TurnLook(Vector3 target)
	{
		Vector3 targetDirection = target - transform.position;
		targetDirection.y = 0; // this makes the direction strictly horizontal
		this.transform.rotation = Quaternion.LookRotation(targetDirection);
	}

	protected virtual bool TryMove(Vector3 startPos, Vector3 endPos, out Vector3 realEndPos)
	{
		RecastUtility.UnityPos2RecastPos(startPos, sharedStartPos);
		RecastUtility.UnityPos2RecastPos(endPos, sharedEndPos);

		int result = RecastDll.RecastTryMove(navMeshScene, halfExtents, sharedStartPos, sharedEndPos, sharedRealEndPos);

		realEndPos = RecastUtility.RecastPos2UnityPos(sharedRealEndPos);

		if (result < 0)
		{
			Debug.LogError("Recast TryMove failed,result:" + result);
			return false;
		}
		return true;

	}

	public virtual bool FindNearestPoint(Vector3 startPos, out Vector3 realEndPos)
	{
		RecastUtility.UnityPos2RecastPos(startPos, sharedStartPos);

		int result = RecastDll.RecastFindNearestPoint(navMeshScene, halfExtents, sharedStartPos, sharedRealEndPos);

		realEndPos = RecastUtility.RecastPos2UnityPos(sharedRealEndPos);

		if (result < 0)
		{
			Debug.LogError("Recast FindNearestPoint failed,result:" + result);
			return false;
		}
		return true;
	}

	public virtual bool SetDestination(Vector3 target)
	{
		var startPos = transform.position;

		RecastUtility.UnityPos2RecastPos(startPos, sharedStartPos);
		RecastUtility.UnityPos2RecastPos(target, sharedEndPos);

		smoothPathCount = RecastDll.RecastFindFollow(navMeshScene, halfExtents, sharedStartPos, sharedEndPos, smoothPath);
		if (smoothPathCount <= 0)
		{
			return false;
		}

		pathPending = true;

		nextPosIndex = 0;
		PathNextPosition();
		TurnLook(nextPos);

		return true;
	}

	public virtual void ResetPath()
	{
		pathPending = false;
	}

	private void PathNextPosition()
	{
		nextPosIndex++;
		RecastUtility.SetIndexPathPos2Unity(smoothPath, nextPosIndex, ref nextPos);
	}

	public virtual void Update()
	{
		if (!pathPending)
		{
			return;
		}

		// Calculate the movement this frame
		Vector3 dir = (nextPos - transform.position).normalized;
		Vector3 displacement = dir * speed * Time.deltaTime;

		// If the displacement is beyond the next position, move to the next position
		if (displacement.sqrMagnitude >= (nextPos - transform.position).sqrMagnitude)
		{
			transform.position = nextPos;
			UpdateCrowdAgent();

			if (smoothPathCount == nextPosIndex + 1)
			{
				pathPending = false;
				return;
			}
			PathNextPosition();
			TurnLook(nextPos);
		}
		else
		{
			// Otherwise, move in the direction of the next position
			transform.position += displacement;
			UpdateCrowdAgent();
		}
	}

	//public virtual void Update()
	//{
	//	if (!pathPending)
	//	{
	//		return;
	//	}
	//	//目标点

	//	float d = speed * Time.deltaTime;

	//	Vector3 dir = nextPos - transform.position;
	//	dir.Normalize();
	//	dir.y = 0;

	//	float d_x = dir.x * d;
	//	float d_z = dir.z * d;

	//	//本次计算到达的点
	//	Vector3 f = new Vector3(d_x, 0, d_z);
	//	Vector3 xxx = transform.position + f;
	//	float d1 = Vector3.Distance(transform.position, xxx);
	//	float d2 = Vector3.Distance(transform.position, nextPos);

	//	if (d1 >= d2)
	//	{
	//		transform.position = nextPos;

	//		if (smoothPathCount == nextPosIndex + 1)
	//		{
	//			pathPending = false;
	//			return;
	//		}

	//		PathNextPosition();
	//		TurnLook(nextPos);

	//	}
	//	else
	//	{
	//		transform.position = xxx;
	//	}

	//}
}
