#include <ctime>
#include "RecastDll.h"
#include "NavMeshManager.h"


NavMeshScene* RecastLoad(int32_t id, const char* buffer, int32_t n, bool isTileCache)
{
	srand((int)time(0));
	return NavMeshManager::getInstance()->newScene(id, buffer, n, isTileCache);
}

NavMeshScene* RecastGet(int32_t id)
{
	return NavMeshManager::getInstance()->getScene(id);
}

bool RecastClearById(int32_t id)
{
	return NavMeshManager::getInstance()->clearScene(id);
}

bool RecastClear(NavMeshScene* navMeshScene)
{
	if (navMeshScene == nullptr)
	{
		return false;
	}
	return NavMeshManager::getInstance()->clearScene(navMeshScene->getId());
}

void RecastClearAll()
{
	NavMeshManager::getInstance()->clearAll();
}

int32_t RecastGetBounds(NavMeshScene* navMeshScene, float* bmin, float* bmax)
{
	if (navMeshScene == nullptr)
	{
		return -1;
	}
	if (bmin == nullptr)
	{
		return -2;
	}
	if (bmax == nullptr)
	{
		return -3;
	}
	return navMeshScene->getBounds(bmin, bmax);
}

EXPORT_API int32_t RecastGenNavMeshInfo(NavMeshScene* navMeshScene, const char* exportJsonPath)
{
	if (navMeshScene == nullptr)
	{
		return -1;
	}
	return navMeshScene->GenNavMeshInfo(exportJsonPath);
}

bool RecastFindRandomPoint(NavMeshScene* navMeshScene, float* pos)
{
	if (navMeshScene == nullptr || pos == nullptr)
	{
		return false;
	}
	dtPolyRef startRef = 0;
	dtStatus status = navMeshScene->navQuery->findRandomPoint(&navMeshScene->navFilter, frand, &startRef, pos);
	return dtStatusSucceed(status);
}

int32_t RecastFindNearestPoint(NavMeshScene* navMeshScene, float* extents, float* startPos, float* nearestPos)
{
	if (navMeshScene == nullptr)
	{
		return -1;
	}
	if (extents == nullptr)
	{
		return -2;
	}
	if (startPos == nullptr)
	{
		return -3;
	}
	if (nearestPos == nullptr)
	{
		return -4;
	}

	dtPolyRef startRef = 0;

	auto result = navMeshScene->navQuery->findNearestPoly(startPos, extents, &navMeshScene->navFilter, &startRef, nearestPos);
	if (!dtStatusSucceed(result))
	{
		return -100;
	}
	if (!startRef)
	{
		return -10;
	}

	return 0;
}

int32_t RecastFindRandomPointAroundCircle(NavMeshScene* navMeshScene, float* extents, const float* centerPos, const float maxRadius, float* pos)
{
	if (navMeshScene == nullptr)
	{
		return -1;
	}
	if (extents == nullptr)
	{
		return -2;
	}
	if (centerPos == nullptr)
	{
		return -3;
	}
	if (pos == nullptr)
	{
		return -5;
	}

	dtPolyRef startRef = 0;
	dtPolyRef randomRef = 0;
	auto result = navMeshScene->navQuery->findNearestPoly(centerPos, extents, &navMeshScene->navFilter, &startRef, 0);
	if (!dtStatusSucceed(result))
	{
		return result;
	}
	result = navMeshScene->navQuery->findRandomPointAroundCircle(startRef, centerPos, maxRadius, &navMeshScene->navFilter, frand, &randomRef, pos);
	if (!dtStatusSucceed(result))
	{
		return result;
	}
	return 0;
}

int32_t RecastFindFollow(NavMeshScene* navMeshScene, float* extents, float* startPos, float* endPos, float* smoothPath)
{
	if (navMeshScene == nullptr)
	{
		return -1;
	}
	if (extents == nullptr)
	{
		return -2;
	}
	if (startPos == nullptr)
	{
		return -3;
	}
	if (endPos == nullptr)
	{
		return -4;
	}
	if (smoothPath == nullptr)
	{
		return -5;
	}

	return navMeshScene->pathfindFollow(extents, startPos, endPos, smoothPath);
}

int32_t RecastTryMove(NavMeshScene* navMeshScene, float* extents, float* startPos, float* endPos, float* realEndPos)
{
	if (navMeshScene == nullptr)
	{
		return -1;
	}
	if (extents == nullptr)
	{
		return -2;
	}
	if (startPos == nullptr)
	{
		return -3;
	}
	if (endPos == nullptr)
	{
		return -4;
	}
	if (realEndPos == nullptr)
	{
		return -5;
	}

	return navMeshScene->tryMove(extents, startPos, endPos, realEndPos);
}

int32_t RecastAddAgent(NavMeshScene* navMeshScene, float* pos, float radius, float height, float maxSpeed, float maxAcceleration)
{
	if (navMeshScene == nullptr)
	{
		return -2;
	}
	if (pos == nullptr)
	{
		return -3;
	}

	return navMeshScene->addAgent(pos, radius, height, maxSpeed, maxAcceleration);
}

void RecastRemoveAgent(NavMeshScene* navMeshScene, int32_t agentId)
{
	if (navMeshScene == nullptr)
	{
		return;
	}
	navMeshScene->removeAgent(agentId);
}

void RecastClearAgent(NavMeshScene* navMeshScene)
{
	if (navMeshScene == nullptr)
	{
		return;
	}
	navMeshScene->clearAgent();
}

int32_t RecastGetAgentPos(NavMeshScene* navMeshScene, int32_t agentId, float* pos)
{
	if (navMeshScene == nullptr)
	{
		return -2;
	}
	if (pos == nullptr)
	{
		return -3;
	}
	return navMeshScene->getAgentPos(agentId, pos);
}

int32_t RecastGetAgentPosWithState(NavMeshScene* navMeshScene, int32_t agentId, float* pos, int32_t* targetState)
{
	if (navMeshScene == nullptr)
	{
		return -2;
	}
	if (pos == nullptr)
	{
		return -3;
	}
	return navMeshScene->getAgentPosWithState(agentId, pos, targetState);
}

int32_t RecastSetAgentPos(NavMeshScene* navMeshScene, int32_t agentId, const float* pos)
{
	if (navMeshScene == nullptr)
	{
		return -2;
	}
	if (pos == nullptr)
	{
		return -3;
	}
	return navMeshScene->setAgentPos(agentId, pos);
}

int32_t RecastSetAgentMoveTarget(NavMeshScene* navMeshScene, int32_t agentId, const float* pos, bool adjust)
{
	if (navMeshScene == nullptr)
	{
		return -2;
	}
	if (pos == nullptr)
	{
		return -3;
	}
	return navMeshScene->setAgentMoveTarget(agentId, pos, adjust);
}

EXPORT_API int32_t RecastSetAgentSpeed(NavMeshScene* navMeshScene, int32_t agentId, float maxSpeed, float maxAcceleration)
{
	if (navMeshScene == nullptr)
	{
		return -2;
	}
	return navMeshScene->setAgentSpeed(agentId, maxSpeed, maxAcceleration);
}

void RecastUpdate(NavMeshScene* navMeshScene, float deltaTime)
{
	if (navMeshScene == nullptr)
	{
		return;
	}
	navMeshScene->update(deltaTime);
}

int32_t RecastAddObstacle(NavMeshScene* navMeshScene, uint32_t* obstacleId, const float* pos, const float radius, const float height)
{
	if (navMeshScene == nullptr)
	{
		return -3;
	}
	if (pos == nullptr)
	{
		return -4;
	}

	return navMeshScene->addObstacle(obstacleId, pos, radius, height);
}

int32_t RecastAddBoxObstacle(NavMeshScene* navMeshScene, uint32_t* obstacleId, const float* bmin, const float* bmax)
{
	if (navMeshScene == nullptr)
	{
		return -3;
	}
	if (bmin == nullptr)
	{
		return -4;
	}
	if (bmax == nullptr)
	{
		return -5;
	}

	return navMeshScene->addBoxObstacle(obstacleId, bmin, bmax);
}

int32_t RecastAddBoxCenterObstacle(NavMeshScene* navMeshScene, uint32_t* obstacleId, const float* center, const float* halfExtents, const float yRadians)
{
	if (navMeshScene == nullptr)
	{
		return -3;
	}
	if (center == nullptr)
	{
		return -4;
	}
	if (halfExtents == nullptr)
	{
		return -5;
	}

	return navMeshScene->addBoxObstacle(obstacleId, center, halfExtents, yRadians);
}

int32_t RecastRemoveObstacle(NavMeshScene* navMeshScene, const uint32_t obstacleId)
{
	if (navMeshScene == nullptr)
	{
		return -3;
	}

	return navMeshScene->removeObstacle(obstacleId);
}

int32_t RecastUpdateObstacles(NavMeshScene* navMeshScene, bool isEveryFrame)
{
	if (navMeshScene == nullptr)
	{
		return -3;
	}
	return navMeshScene->updateObstacles(isEveryFrame);
}
