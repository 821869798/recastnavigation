#pragma once
#include <cstdint>
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"
#include "DetourCrowd.h"
#include "RecastUtility.h"
#include "DetourTileCache.h"


static const int NAVMESHSET_MAGIC = 'M' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;


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


class NavMeshScene
{

protected:
	//dtPolyRef m_polys[MAX_POLYS];
	bool inited;
	int32_t id;
	float m_tmpPos[3];

private:
	int32_t initNavmesh(const char* buffer, int32_t n);

public:
	dtNavMesh* navMesh;
	dtNavMeshQuery* navQuery;
	dtQueryFilter navFilter;
	dtCrowd* crowd;

	NavMeshScene(int32_t id);
	virtual ~NavMeshScene();

	int32_t getId();

	virtual int32_t init(const char* buffer, int32_t n);

	int32_t pathfindFollow(float* extents, float* m_spos, float* m_epos, float* m_smoothPath);

	int32_t tryMove(float* extents, float* startPos, float* endPos, float* realEndPos);

	int32_t addAgent(float* pos, float radius, float height, float maxSpeed, float maxAcceleration);

	void removeAgent(int agentId);

	void clearAgent();

	int32_t getAgentPos(int32_t agentId, float* pos);

	int32_t getAgentPosWithState(int32_t agentId, float* pos, int32_t* targetState);

	int32_t setAgentPos(int32_t agentId, const float* pos);

	int32_t setAgentMoveTarget(int32_t agentId, const float* pos, bool adjust);

	void update(float deltaTime);

	inline virtual int32_t addObstacle(dtObstacleRef* /*obstacleId*/, const float* /*pos*/, const float /*radius*/, const float /*height*/) { return -100; }
	inline virtual int32_t addBoxObstacle(dtObstacleRef* /*obstacleId*/, const float* /*bmin*/, const float* /*bmax*/) { return -100; };
	inline virtual int32_t addBoxObstacle(dtObstacleRef* /*obstacleId*/, const float* /*center*/, const float* /*halfExtents*/, const float /*yRadians*/) { return -100; };
	inline virtual int32_t removeObstacle(const dtObstacleRef /*ref*/) { return -100; };
	inline virtual int32_t updateObstacles(bool /*isEveryFrame*/) { return -100; }
};