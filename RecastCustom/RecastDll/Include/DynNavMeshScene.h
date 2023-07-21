#pragma once
#include <cstdint>
#include "NavMeshScene.h"
#include "DetourTileCache.h"

static const int TILECACHESET_MAGIC = 'T' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'TSET';
static const int TILECACHESET_VERSION = 1;

struct TileCacheSetHeader
{
	int magic;
	int version;
	int numTiles;
	dtNavMeshParams meshParams;
	dtTileCacheParams cacheParams;
};

struct TileCacheTileHeader
{
	dtCompressedTileRef tileRef;
	int dataSize;
};

class DynNavMeshScene : public NavMeshScene
{
public:
	DynNavMeshScene(int32_t id);
	virtual ~DynNavMeshScene();

	class dtTileCache* tileCache;

	int32_t init(const char* buffer, int32_t n) override;
	int32_t addObstacle(dtObstacleRef* obstacleId, const float* pos, const float radius, const float height) override;
	int32_t addBoxObstacle(dtObstacleRef* obstacleId, const float* bmin, const float* bmax) override;
	int32_t addBoxObstacle(dtObstacleRef* obstacleId, const float* center, const float* halfExtents, const float yRadians) override;
	int32_t removeObstacle(const dtObstacleRef ref) override;
	int32_t updateObstacles(bool isEveryFrame) override;
private:
	struct LinearAllocator* mTalloc;
	struct FastLZCompressor* mTcomp;
	struct MeshProcess* mTmproc;

	int32_t initTileCache(const char* buffer, int32_t n);
};