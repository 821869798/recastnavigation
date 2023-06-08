#include <ctime>
#include "RecastDll.h"
#include "NavMeshManager.h"
#include "RecastUtility.h"



NavMeshScene* RecastLoad(int32_t id, const char* buffer, int32_t n)
{
	srand((int)time(0));
	return NavMeshManager::getInstance()->newScene(id, buffer, n);
}

NavMeshScene* RecastGet(int32_t id)
{
	return NavMeshManager::getInstance()->getScene(id);
}

bool RecastClear(int32_t id)
{
	return NavMeshManager::getInstance()->clearScene(id);
}

void RecastClearAll()
{
	NavMeshManager::getInstance()->clearAll();
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

