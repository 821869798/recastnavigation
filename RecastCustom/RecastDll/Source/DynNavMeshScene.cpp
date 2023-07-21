#include "DynNavMeshScene.h"
#include "DetourNavMeshBuilder.h"
#include "DetourTileCacheBuilder.h"
#include "fastlz.h"

struct FastLZCompressor : public dtTileCacheCompressor
{
	virtual ~FastLZCompressor();

	virtual int maxCompressedSize(const int bufferSize)
	{
		return (int)(bufferSize * 1.05f);
	}

	virtual dtStatus compress(const unsigned char* buffer, const int bufferSize,
		unsigned char* compressed, const int /*maxCompressedSize*/, int* compressedSize)
	{
		*compressedSize = fastlz_compress((const void*)buffer, bufferSize, compressed);
		return DT_SUCCESS;
	}

	virtual dtStatus decompress(const unsigned char* compressed, const int compressedSize,
		unsigned char* buffer, const int maxBufferSize, int* bufferSize)
	{
		*bufferSize = fastlz_decompress(compressed, compressedSize, buffer, maxBufferSize);
		return *bufferSize < 0 ? DT_FAILURE : DT_SUCCESS;
	}
};

FastLZCompressor::~FastLZCompressor()
{
	// Defined out of line to fix the weak v-tables warning
}

struct LinearAllocator : public dtTileCacheAlloc
{
	unsigned char* buffer;
	size_t capacity;
	size_t top;
	size_t high;

	LinearAllocator(const size_t cap) : buffer(0), capacity(0), top(0), high(0)
	{
		resize(cap);
	}

	virtual ~LinearAllocator();

	void resize(const size_t cap)
	{
		if (buffer) dtFree(buffer);
		buffer = (unsigned char*)dtAlloc(cap, DT_ALLOC_PERM);
		capacity = cap;
	}

	virtual void reset()
	{
		high = dtMax(high, top);
		top = 0;
	}

	virtual void* alloc(const size_t size)
	{
		if (!buffer)
			return 0;
		if (top + size > capacity)
			return 0;
		unsigned char* mem = &buffer[top];
		top += size;
		return mem;
	}

	virtual void free(void* /*ptr*/)
	{
		// Empty
	}
};

LinearAllocator::~LinearAllocator()
{
	// Defined out of line to fix the weak v-tables warning
	dtFree(buffer);
}


struct MeshProcess : public dtTileCacheMeshProcess
{
	//InputGeom* m_geom;

	inline MeshProcess() // : m_geom(0)
	{
	}

	virtual ~MeshProcess();

	//inline void init(InputGeom* geom)
	//{
	//	m_geom = geom;
	//}

	virtual void process(struct dtNavMeshCreateParams* params,
		unsigned char* polyAreas, unsigned short* polyFlags)
	{
		// Update poly flags from areas.
		for (int i = 0; i < params->polyCount; ++i)
		{
			if (polyAreas[i] == DT_TILECACHE_WALKABLE_AREA)
				polyAreas[i] = SAMPLE_POLYAREA_GROUND;

			if (polyAreas[i] == SAMPLE_POLYAREA_GROUND ||
				polyAreas[i] == SAMPLE_POLYAREA_GRASS ||
				polyAreas[i] == SAMPLE_POLYAREA_ROAD)
			{
				polyFlags[i] = SAMPLE_POLYFLAGS_WALK;
			}
			else if (polyAreas[i] == SAMPLE_POLYAREA_WATER)
			{
				polyFlags[i] = SAMPLE_POLYFLAGS_SWIM;
			}
			else if (polyAreas[i] == SAMPLE_POLYAREA_DOOR)
			{
				polyFlags[i] = SAMPLE_POLYFLAGS_WALK | SAMPLE_POLYFLAGS_DOOR;
			}
		}

		// Pass in off-mesh connections.
		//if (m_geom)
		//{
		//	params->offMeshConVerts = m_geom->getOffMeshConnectionVerts();
		//	params->offMeshConRad = m_geom->getOffMeshConnectionRads();
		//	params->offMeshConDir = m_geom->getOffMeshConnectionDirs();
		//	params->offMeshConAreas = m_geom->getOffMeshConnectionAreas();
		//	params->offMeshConFlags = m_geom->getOffMeshConnectionFlags();
		//	params->offMeshConUserID = m_geom->getOffMeshConnectionId();
		//	params->offMeshConCount = m_geom->getOffMeshConnectionCount();
		//}
	}
};

MeshProcess::~MeshProcess()
{
	// Defined out of line to fix the weak v-tables warning
}


int32_t DynNavMeshScene::initTileCache(const char* buffer, int32_t n)
{
	int index = 0;
	// Read header.
	TileCacheSetHeader header;

	int count = sizeof(TileCacheSetHeader);
	if (index + count > n)
	{
		return -1;
	}
	memcpy(&header, buffer + index, count);
	index += count;

	if (header.magic != TILECACHESET_MAGIC)
	{
		return -2;
	}
	if (header.version != TILECACHESET_VERSION)
	{
		return -3;
	}

	navMesh = dtAllocNavMesh();
	if (!navMesh)
	{
		return -4;
	}
	dtStatus status = navMesh->init(&header.meshParams);
	if (dtStatusFailed(status))
	{
		return -5;
	}

	tileCache = dtAllocTileCache();
	if (!tileCache)
	{
		return -6;
	}


	status = tileCache->init(&header.cacheParams, mTalloc, mTcomp, mTmproc);
	if (dtStatusFailed(status))
	{
		return -7;
	}


	// Read tiles.
	for (int i = 0; i < header.numTiles; ++i)
	{
		TileCacheTileHeader tileHeader;

		count = sizeof(TileCacheTileHeader);
		if (index + count > n)
		{
			return -11;
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
			dtFree(data);
			return -12;
		}
		memcpy(data, buffer + index, count);
		index += count;

		dtCompressedTileRef tile = 0;
		dtStatus addTileStatus = tileCache->addTile(data, tileHeader.dataSize, DT_COMPRESSEDTILE_FREE_DATA, &tile);
		if (dtStatusFailed(addTileStatus))
		{
			dtFree(data);
		}

		if (tile)
			tileCache->buildNavMeshTile(tile, navMesh);
	}
	return 0;
}


int32_t DynNavMeshScene::init(const char* buffer, int32_t n)
{
	if (inited)
	{
		return -100;
	}

	inited = true;
	int32_t ret = initTileCache(buffer, n);

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



int32_t DynNavMeshScene::addObstacle(dtObstacleRef* obstacleId, const float* pos, const float radius, const float height)
{
	if (!tileCache)
	{
		return -1;
	}
	dtObstacleRef result;
	dtStatus status = tileCache->addObstacle(pos, radius, height, &result);
	if (!dtStatusSucceed(status))
	{
		return -2;
	}
	*obstacleId = result;
	return 0;
}

int32_t DynNavMeshScene::addBoxObstacle(dtObstacleRef* obstacleId, const float* bmin, const float* bmax)
{
	if (!tileCache)
	{
		return -1;
	}
	dtObstacleRef result;
	dtStatus status = tileCache->addBoxObstacle(bmin, bmax, &result);
	if (!dtStatusSucceed(status))
	{
		return -2;
	}
	*obstacleId = result;
	return 0;
}

int32_t DynNavMeshScene::addBoxObstacle(dtObstacleRef* obstacleId, const float* center, const float* halfExtents, const float yRadians)
{
	if (!tileCache)
	{
		return -1;
	}
	dtObstacleRef result;
	dtStatus status = tileCache->addBoxObstacle(center, halfExtents, yRadians, &result);
	if (!dtStatusSucceed(status))
	{
		return -2;
	}
	*obstacleId = result;
	return 0;
}

int32_t DynNavMeshScene::removeObstacle(const dtObstacleRef ref)
{
	if (!tileCache)
	{
		return -1;
	}
	dtStatus status = tileCache->removeObstacle(ref);
	if (!dtStatusSucceed(status))
	{
		return -2;
	}
	return 0;
}

int32_t DynNavMeshScene::updateObstacles(bool isEveryFrame)
{
	if (!tileCache)
	{
		return -1;
	}
	if (isEveryFrame)
	{
		//如果是每帧刷新，就不需要一口气算完
		dtStatus status = tileCache->update(0, navMesh);
		if (!dtStatusSucceed(status))
		{
			return -2;
		}
		return 0;
	}

	//一口气算法，直到upToDate为true
	bool upToDate = false;
	while (!upToDate)
	{
		dtStatus status = tileCache->update(0, navMesh, &upToDate);
		if (!dtStatusSucceed(status))
		{
			return -2;
		}
	}
	return 0;
}

DynNavMeshScene::DynNavMeshScene(int32_t id) : NavMeshScene(id)
{
	mTalloc = new LinearAllocator(32 * 1024);
	mTcomp = new FastLZCompressor();
	mTmproc = new MeshProcess();
}

DynNavMeshScene::~DynNavMeshScene()
{
	if (tileCache)
	{
		dtFreeTileCache(tileCache);
		tileCache = NULL;
	}
	if (mTalloc)
	{
		delete mTalloc;
		mTalloc = NULL;
	}
	if (mTcomp)
	{
		delete mTcomp;
		mTcomp = NULL;
	}
	if (mTmproc)
	{
		delete mTmproc;
		mTmproc = NULL;
	}
}


