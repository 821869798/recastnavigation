#pragma once
#include <cstdint>
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"
#include "DetourCrowd.h"
#include "RecastUtility.h"


static const int NAVMESHSET_MAGIC = 'M' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;

enum SamplePolyFlags
{
	SAMPLE_POLYFLAGS_WALK = 0x01,		// Ability to walk (ground, grass, road)
	SAMPLE_POLYFLAGS_SWIM = 0x02,		// Ability to swim (water).
	SAMPLE_POLYFLAGS_DOOR = 0x04,		// Ability to move through doors.
	SAMPLE_POLYFLAGS_JUMP = 0x08,		// Ability to jump.
	SAMPLE_POLYFLAGS_DISABLED = 0x10,		// Disabled polygon
	SAMPLE_POLYFLAGS_ALL = 0xffff	// All abilities.
};


class NavMeshScene
{

private:
	//dtPolyRef m_polys[MAX_POLYS];
	bool inited;
	int32_t id;
	float m_tmpPos[3];

public:
	dtNavMesh* navMesh;
	dtNavMeshQuery* navQuery;
	dtQueryFilter navFilter;
	dtCrowd* crowd;

	NavMeshScene(int32_t id);
	~NavMeshScene();

	int32_t getId();

	int32_t init(const char* buffer, int32_t n);

	int32_t pathfindFollow(float* extents, float* m_spos, float* m_epos, float* m_smoothPath);

	int32_t tryMove(float* extents, float* startPos, float* endPos, float* realEndPos);

	int addAgent(float* pos, float radius, float height, float maxSpeed, float maxAcceleration);

	void removeAgent(int agentId);

	void clearAgent();

	int32_t getAgentPos(int agentId, float* pos);

	int32_t getAgentPosWithState(int agentId, float* pos, int32_t* targetState);

	int32_t setAgentPos(int agentId, const float* pos);

	int32_t setAgentMoveTarget(int agentId, const float* pos, bool adjust);

	void update(float deltaTime);
};