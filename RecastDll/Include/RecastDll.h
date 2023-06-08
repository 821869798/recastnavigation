#pragma once
#include <cstdint>
#include "NavMeshScene.h"

#if !RECASTNAVIGATION_STATIC && WIN32
#define RECAST_DLL _declspec(dllexport)
#else
#define RECAST_DLL
#endif


#ifdef __cplusplus
extern "C" {
#endif

	RECAST_DLL NavMeshScene* RecastGet(int32_t id);
	RECAST_DLL NavMeshScene* RecastLoad(int32_t id, const char* buffer, int32_t n);
	RECAST_DLL bool RecastClear(int32_t id);
	RECAST_DLL void RecastClearAll();
	RECAST_DLL bool RecastFindRandomPoint(NavMeshScene* navMeshScene, float* pos);
	RECAST_DLL int32_t RecastFindNearestPoint(NavMeshScene* navMeshScene, float* extents, float* startPos, float* nearestPos);
	RECAST_DLL int32_t RecastFindRandomPointAroundCircle(NavMeshScene* navMeshScene, float* extents, const float* centerPos, const float maxRadius, float* pos);
	RECAST_DLL int32_t RecastFindFollow(NavMeshScene* navMeshScene, float* extents, float* startPos, float* endPos, float* smoothPath);

#ifdef __cplusplus
}
#endif