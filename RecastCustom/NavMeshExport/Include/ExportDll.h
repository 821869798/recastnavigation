#pragma once
#include <cstdint>
#include "NavMeshExport.h"
#include "ExportUtility.h"

#if !RECASTNAVIGATION_STATIC && WIN32
#define EXPORT_API _declspec(dllexport)
#else
#define EXPORT_API
#endif


#ifdef __cplusplus
extern "C" {
#endif

	EXPORT_API bool NavMeshExport(NavMeshBuildArgs* buildArgs, const float* vertices, const int* triangles, const int* area, const char* exportPath);

	EXPORT_API int32_t TempObstaclesExport(RecastNavMeshBuildArgs* buildArgs, const char* objPath, const char* exportPath);

	EXPORT_API int32_t SoleMeshExport(RecastNavMeshBuildArgs* buildArgs, const char* objPath, const char* exportPath);

	EXPORT_API int32_t TempObstaclesGenMeshInfo(const char* binPath, const char* exportJsonPath);

	EXPORT_API int32_t SoleMeshGenMeshInfo(const char* binPath, const char* exportJsonPath);

	EXPORT_API int32_t AutoGenMeshInfo(const char* binPath, const char* exportJsonPath);

#ifdef __cplusplus
}
#endif