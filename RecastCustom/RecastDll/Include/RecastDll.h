#pragma once
#include <cstdint>
#include "NavMeshScene.h"

#if !RECASTNAVIGATION_STATIC && WIN32
#define EXPORT_API _declspec(dllexport)
#else
#define EXPORT_API
#endif


#ifdef __cplusplus
extern "C" {
#endif

	EXPORT_API NavMeshScene* RecastGet(int32_t id);
	EXPORT_API NavMeshScene* RecastLoad(int32_t id, const char* buffer, int32_t n);
	EXPORT_API bool RecastClearById(int32_t id);
	EXPORT_API bool RecastClear(NavMeshScene* navMeshScene);
	EXPORT_API void RecastClearAll();
	EXPORT_API bool RecastFindRandomPoint(NavMeshScene* navMeshScene, float* pos);
	EXPORT_API int32_t RecastFindNearestPoint(NavMeshScene* navMeshScene, float* extents, float* startPos, float* nearestPos);
	EXPORT_API int32_t RecastFindRandomPointAroundCircle(NavMeshScene* navMeshScene, float* extents, const float* centerPos, const float maxRadius, float* pos);
	EXPORT_API int32_t RecastFindFollow(NavMeshScene* navMeshScene, float* extents, float* startPos, float* endPos, float* smoothPath);
	EXPORT_API int32_t RecastTryMove(NavMeshScene* navMeshScene, float* extents, float* startPos, float* endPos, float* realEndPos);
	EXPORT_API int RecastAddAgent(NavMeshScene* navMeshScene, float* pos, float radius, float height, float maxSpeed, float maxAcceleration);
	EXPORT_API void RecastRemoveAgent(NavMeshScene* navMeshScene, int agentId);
	EXPORT_API void RecastClearAgent(NavMeshScene* navMeshScene);
	EXPORT_API int32_t RecastGetAgentPos(NavMeshScene* navMeshScene, int agentId, float* pos);
	EXPORT_API int32_t RecastGetAgentPosWithState(NavMeshScene* navMeshScene, int agentId, float* pos, int32_t* targetState);
	EXPORT_API int32_t RecastSetAgentPos(NavMeshScene* navMeshScene, int agentId, const float* pos);
	EXPORT_API int32_t RecastSetAgentMoveTarget(NavMeshScene* navMeshScene, int agentId, const float* pos, bool adjust);
	EXPORT_API void RecastUpdate(NavMeshScene* navMeshScene, float deltaTime);
#ifdef __cplusplus
}
#endif