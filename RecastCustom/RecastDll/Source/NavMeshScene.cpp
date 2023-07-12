#include <cstring>
#include <string>
#include "NavMeshScene.h"
#include "RecastUtility.h"
#include "DetourCommon.h"

struct NavMeshSetHeader
{
	int magic;
	int version;
	int numTiles;
	dtNavMeshParams params;
};

struct NavMeshTileHeader
{
	dtTileRef tileRef;
	int dataSize;
};

int32_t initNav(const char* buffer, int32_t n, dtNavMesh*& navMesh)
{
	int index = 0;
	// Read header.
	NavMeshSetHeader header;

	int count = sizeof(NavMeshSetHeader);
	if (index + count > n)
	{
		return -1;
	}
	memcpy(&header, buffer + index, count);
	index += count;

	if (header.magic != NAVMESHSET_MAGIC)
	{
		return -2;
	}
	if (header.version != NAVMESHSET_VERSION)
	{
		return -3;
	}

	dtNavMesh* mesh = dtAllocNavMesh();
	if (!mesh)
	{
		return -4;
	}
	dtStatus status = mesh->init(&header.params);
	if (dtStatusFailed(status))
	{
		return -5;
	}

	// Read tiles.
	for (int i = 0; i < header.numTiles; ++i)
	{
		NavMeshTileHeader tileHeader;

		count = sizeof(NavMeshTileHeader);
		if (index + count > n)
		{
			return -6;
		}
		memcpy(&tileHeader, buffer + index, count);
		index += count;

		if (!tileHeader.tileRef || !tileHeader.dataSize)
			break;

		unsigned char* data = (unsigned char*)dtAlloc(tileHeader.dataSize, DT_ALLOC_PERM);
		if (!data) break;
		memset(data, 0, tileHeader.dataSize);

		count = tileHeader.dataSize;
		if (index + count > n)
		{
			return -7;
		}
		memcpy(data, buffer + index, count);
		index += count;

		mesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
	}
	navMesh = mesh;
	return 0;
}

int32_t NavMeshScene::getId()
{
	return this->id;
}

int32_t NavMeshScene::init(const char* buffer, int32_t n)
{
	if (inited)
	{
		return -100;
	}

	inited = true;
	int32_t ret = initNav(buffer, n, navMesh);
	std::string s;

	if (ret != 0)
	{
		return ret;
	}

	navQuery = new dtNavMeshQuery();
	navQuery->init(navMesh, 4096);

	crowd = dtAllocCrowd();
	crowd->init(500, 0.6f, navMesh);

	return 0;
}

int32_t NavMeshScene::pathfindFollow(float* extents, float* m_spos, float* m_epos, float* m_smoothPath)
{


	dtPolyRef startRef = 0;
	dtPolyRef endRef = 0;
	float startNearestPt[3];
	float endNearestPt[3];

	navQuery->findNearestPoly(m_spos, extents, &navFilter, &startRef, startNearestPt);
	navQuery->findNearestPoly(m_epos, extents, &navFilter, &endRef, endNearestPt);

	dtPolyRef polys[MAX_POLYS];
	int npolys;
	unsigned char straightPathFlags[MAX_POLYS];
	dtPolyRef straightPathPolys[MAX_POLYS];
	int nstraightPath = 0;

	navQuery->findPath(startRef, endRef, startNearestPt, endNearestPt, &navFilter, polys, &npolys, MAX_POLYS);

	if (npolys)
	{
		float epos1[3];
		dtVcopy(epos1, endNearestPt);

		if (polys[npolys - 1] != endRef)
		{
			navQuery->closestPointOnPoly(polys[npolys - 1], endNearestPt, epos1, 0);
		}

		navQuery->findStraightPath(startNearestPt, endNearestPt, polys, npolys, m_smoothPath, straightPathFlags, straightPathPolys, &nstraightPath, MAX_POLYS, DT_STRAIGHTPATH_ALL_CROSSINGS);
	}

	return nstraightPath;

	/*
	dtPolyRef m_startRef;
	dtPolyRef m_endRef;

	float realStartPos[3];
	float realEndPos[3];


	navQuery->findNearestPoly(m_spos, extents, &navFilter, &m_startRef, realStartPos);
	navQuery->findNearestPoly(m_epos, extents, &navFilter, &m_endRef, realEndPos);

	int m_npolys = 0;
	int m_nsmoothPath = 0; //path point count
	if (m_startRef && m_endRef)
	{
		navQuery->findPath(m_startRef, m_endRef, realStartPos, realEndPos, &navFilter, m_polys, &m_npolys, MAX_POLYS);

		if (m_npolys)
		{
			// Iterate over the path to find smooth path on the detail mesh surface.
			dtPolyRef polys[MAX_POLYS];
			memcpy(polys, m_polys, sizeof(dtPolyRef) * m_npolys);
			int npolys = m_npolys;

			float iterPos[3], targetPos[3];
			navQuery->closestPointOnPoly(m_startRef, realStartPos, iterPos, 0);
			navQuery->closestPointOnPoly(polys[npolys - 1], realEndPos, targetPos, 0);

			static const float STEP_SIZE = 0.3f;
			static const float SLOP = 0.01f;

			m_nsmoothPath = 0;

			dtVcopy(&m_smoothPath[m_nsmoothPath * 3], iterPos);
			m_nsmoothPath++;

			// Move towards target a small advancement at a time until target reached or
			// when ran out of memory to store the path.
			while (npolys && m_nsmoothPath < MAX_SMOOTH)
			{
				// Find location to steer towards.
				float steerPos[3];
				unsigned char steerPosFlag;
				dtPolyRef steerPosRef;

				if (!getSteerTarget(navQuery, iterPos, targetPos, SLOP,
					polys, npolys, steerPos, steerPosFlag, steerPosRef))
					break;

				bool endOfPath = (steerPosFlag & DT_STRAIGHTPATH_END) ? true : false;
				bool offMeshConnection = (steerPosFlag & DT_STRAIGHTPATH_OFFMESH_CONNECTION) ? true : false;

				// Find movement delta.
				float delta[3], len;
				dtVsub(delta, steerPos, iterPos);
				len = dtMathSqrtf(dtVdot(delta, delta));
				// If the steer target is end of path or off-mesh link, do not move past the location.
				if ((endOfPath || offMeshConnection) && len < STEP_SIZE)
					len = 1;
				else
					len = STEP_SIZE / len;
				float moveTgt[3];
				dtVmad(moveTgt, iterPos, delta, len);

				// Move
				float result[3];
				dtPolyRef visited[16];
				int nvisited = 0;
				navQuery->moveAlongSurface(polys[0], iterPos, moveTgt, &navFilter,
					result, visited, &nvisited, 16);

				npolys = fixupCorridor(polys, npolys, MAX_POLYS, visited, nvisited);
				npolys = fixupShortcuts(polys, npolys, navQuery);

				float h = 0;
				navQuery->getPolyHeight(polys[0], result, &h);
				result[1] = h;
				dtVcopy(iterPos, result);

				// Handle end of path and off-mesh links when close enough.
				if (endOfPath && inRange(iterPos, steerPos, SLOP, 1.0f))
				{
					// Reached end of path.
					dtVcopy(iterPos, targetPos);
					if (m_nsmoothPath < MAX_SMOOTH)
					{
						dtVcopy(&m_smoothPath[m_nsmoothPath * 3], iterPos);
						m_nsmoothPath++;
					}
					break;
				}
				else if (offMeshConnection && inRange(iterPos, steerPos, SLOP, 1.0f))
				{
					// Reached off-mesh connection.
					float startPos[3], endPos[3];

					// Advance the path up to and over the off-mesh connection.
					dtPolyRef prevRef = 0, polyRef = polys[0];
					int npos = 0;
					while (npos < npolys && polyRef != steerPosRef)
					{
						prevRef = polyRef;
						polyRef = polys[npos];
						npos++;
					}
					for (int i = npos; i < npolys; ++i)
						polys[i - npos] = polys[i];
					npolys -= npos;

					// Handle the connection.
					dtStatus status = navMesh->getOffMeshConnectionPolyEndPoints(prevRef, polyRef, startPos, endPos);
					if (dtStatusSucceed(status))
					{
						if (m_nsmoothPath < MAX_SMOOTH)
						{
							dtVcopy(&m_smoothPath[m_nsmoothPath * 3], startPos);
							m_nsmoothPath++;
							// Hack to make the dotted path not visible during off-mesh connection.
							if (m_nsmoothPath & 1)
							{
								dtVcopy(&m_smoothPath[m_nsmoothPath * 3], startPos);
								m_nsmoothPath++;
							}
						}
						// Move position at the other side of the off-mesh link.
						dtVcopy(iterPos, endPos);
						float eh = 0.0f;
						navQuery->getPolyHeight(polys[0], iterPos, &eh);
						iterPos[1] = eh;
					}
				}

				// Store results.
				if (m_nsmoothPath < MAX_SMOOTH)
				{
					dtVcopy(&m_smoothPath[m_nsmoothPath * 3], iterPos);
					m_nsmoothPath++;
				}
			}

		}
	}
	else
	{
		m_npolys = 0;
		m_nsmoothPath = 0;
	}

	return m_nsmoothPath;
	*/

}

int32_t NavMeshScene::tryMove(float* extents, float* startPos, float* endPos, float* realEndPos)
{
	// Find the start polygon
	dtPolyRef startPolyRef;
	navQuery->findNearestPoly(startPos, extents, &navFilter, &startPolyRef, 0);

	// If we couldn't find a start polygon, return failure
	if (!startPolyRef)
	{
		return -100;
	}

	// Try to move from the start to the end position
	dtPolyRef visited[16];
	int nvisited = 0;
	dtStatus status = navQuery->moveAlongSurface(startPolyRef, startPos, endPos, &navFilter, realEndPos, visited, &nvisited, 16);

	if (!dtStatusSucceed(status)) {
		// Moving along the surface failed
		return -101;
	}

	// If we couldn't find a path, return failure
	if (nvisited == 0)
	{
		//return -102;
		return 0;
	}

	// Height Fix Solution 1: The effect is not good, there will be a high degree of ghost animals
	// Find the poly height at the position
	//dtPolyRef realEndPolyRef = visited[nvisited - 1];
	//float h = 0;
	//status = navQuery->getPolyHeight(realEndPolyRef, realEndPos, &h);
	//if (dtStatusSucceed(status)) {
	//	realEndPos[1] = h;
	//}

	// Height Fix Solution 2: Some unnecessary operations
	//dtPolyRef nearestPoly;
	//float m_tmpPos[3];
	//navQuery->findNearestPoly(realEndPos, extents, &navFilter, &nearestPoly, m_tmpPos);
	//if (nearestPoly)
	//{
	//	float h = 0;
	//	dtStatus status = navQuery->getPolyHeight(nearestPoly, m_tmpPos, &h);
	//	if (dtStatusSucceed(status)) {
	//		realEndPos[0] = m_tmpPos[0];
	//		realEndPos[1] = h;
	//		realEndPos[2] = m_tmpPos[2];
	//	}
	//}

	// Height Fix Solution 3
	dtPolyRef nearestPoly;
	navQuery->findNearestPoly(realEndPos, extents, &navFilter, &nearestPoly, m_tmpPos);
	if (nearestPoly)
	{
		realEndPos[0] = m_tmpPos[0];
		realEndPos[1] = m_tmpPos[1];
		realEndPos[2] = m_tmpPos[2];
		float h = 0;
		dtStatus status = navQuery->getPolyHeight(nearestPoly, m_tmpPos, &h);
		if (dtStatusSucceed(status)) {
			realEndPos[1] = h;
		}
	}

	// Otherwise, return success
	return 0;
}

int NavMeshScene::addAgent(float* pos, float radius, float height, float maxSpeed, float maxAcceleration)
{
	dtCrowdAgentParams ap;
	memset(&ap, 0, sizeof(ap));
	ap.radius = radius;
	ap.height = height;
	ap.maxAcceleration = maxAcceleration;
	ap.maxSpeed = maxSpeed;
	ap.collisionQueryRange = ap.radius * 12.0f;
	ap.pathOptimizationRange = ap.radius * 30.0f;
	//TODO modify
	ap.updateFlags = DT_CROWD_ANTICIPATE_TURNS | DT_CROWD_OPTIMIZE_VIS | DT_CROWD_OPTIMIZE_TOPO | DT_CROWD_OBSTACLE_AVOIDANCE;
	ap.obstacleAvoidanceType = 3;
	ap.separationWeight = 2.0f;

	int agentId = crowd->addAgent(pos, &ap);

	return agentId;
}

void NavMeshScene::removeAgent(int agentId)
{
	crowd->removeAgent(agentId);
}

void NavMeshScene::clearAgent()
{
	for (int i = 0; i < crowd->getAgentCount(); ++i) {
		if (crowd->getAgent(i)->active) {
			crowd->removeAgent(i);
		}
	}
}

int32_t NavMeshScene::getAgentPos(int agentId, float* pos)
{
	const dtCrowdAgent* agent = crowd->getAgent(agentId);

	if (agent && agent->active)
	{
		dtVcopy(pos, agent->npos);
		return 0;
	}
	return -1;
}

int32_t NavMeshScene::getAgentPosWithState(int agentId, float* pos, int32_t* targetState)
{
	const dtCrowdAgent* agent = crowd->getAgent(agentId);

	if (agent && agent->active)
	{
		*targetState = agent->targetState;
		dtVcopy(pos, agent->npos);
		return 0;
	}
	return -1;
}

int32_t NavMeshScene::setAgentPos(int agentId, const float* pos)
{
	dtCrowdAgent* ag = crowd->getEditableAgent(agentId);

	if (!(ag && ag->active))
	{
		return -1;
	}

	float nearest[3];
	dtPolyRef ref = 0;
	dtVcopy(nearest, pos);
	dtStatus status = navQuery->findNearestPoly(pos, crowd->getQueryHalfExtents(), &navFilter, &ref, nearest);
	if (dtStatusFailed(status))
	{
		return -2;
	}

	ag->corridor.reset(ref, nearest);
	ag->boundary.reset();
	ag->partial = false;

	dtVcopy(ag->npos, nearest);

	return 0;
}

int32_t NavMeshScene::setAgentMoveTarget(int agentId, const float* pos, bool adjust)
{
	const dtCrowdAgent* agent = crowd->getAgent(agentId);

	if (!(agent && agent->active))
	{
		return -1;
	}

	if (adjust)
	{
		float vel[3];

		calcVel(vel, agent->npos, pos, agent->params.maxSpeed);
		crowd->requestMoveVelocity(agentId, vel);
	}
	else
	{
		dtPolyRef targetRef;
		navQuery->findNearestPoly(pos, crowd->getQueryHalfExtents(), &navFilter, &targetRef, m_tmpPos);

		crowd->requestMoveTarget(agentId, targetRef, m_tmpPos);
	}
	return 0;
}

void NavMeshScene::update(float deltaTime)
{
	if (crowd)
	{
		crowd->update(deltaTime, NULL);
	}
}


NavMeshScene::NavMeshScene(int32_t id)
{
	this->id = id;
	inited = false;
	navFilter.setIncludeFlags(SAMPLE_POLYFLAGS_ALL ^ SAMPLE_POLYFLAGS_DISABLED);
	navFilter.setExcludeFlags(0);
	navMesh = nullptr;
	navQuery = nullptr;
}

NavMeshScene::~NavMeshScene()
{
	if (navQuery)
	{
		dtFreeNavMeshQuery(navQuery);
		navQuery = NULL;
	}
	if (navMesh)
	{
		dtFreeNavMesh(navMesh);
		navMesh = NULL;
	}
	if (crowd)
	{
		dtFreeCrowd(crowd);
		crowd = NULL;
	}
}


