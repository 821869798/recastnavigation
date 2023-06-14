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
	int32_t ret = initNav(buffer, n, navMesh);
	std::string s;

	if (ret != 0)
	{
		return -1;
	}

	navQuery = new dtNavMeshQuery();
	navQuery->init(navMesh, 4096);
	return 0;
}

int32_t NavMeshScene::pathfindFollow(float* extents, float* m_spos, float* m_epos, float* m_smoothPath)
{
	dtPolyRef m_startRef;
	dtPolyRef m_endRef;

	navQuery->findNearestPoly(m_spos, extents, &navFilter, &m_startRef, 0);
	navQuery->findNearestPoly(m_epos, extents, &navFilter, &m_endRef, 0);

	int m_npolys = 0;
	int m_nsmoothPath = 0; //path point count
	if (m_startRef && m_endRef)
	{
		navQuery->findPath(m_startRef, m_endRef, m_spos, m_epos, &navFilter, m_polys, &m_npolys, MAX_POLYS);

		if (m_npolys)
		{
			// Iterate over the path to find smooth path on the detail mesh surface.
			dtPolyRef polys[MAX_POLYS];
			memcpy(polys, m_polys, sizeof(dtPolyRef) * m_npolys);
			int npolys = m_npolys;

			float iterPos[3], targetPos[3];
			navQuery->closestPointOnPoly(m_startRef, m_spos, iterPos, 0);
			navQuery->closestPointOnPoly(polys[npolys - 1], m_epos, targetPos, 0);

			static const float STEP_SIZE = 0.5f;
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
		return -102;
	}

	// Find the poly height at the position
	float h = 0;
	status = navQuery->getPolyHeight(startPolyRef, realEndPos, &h);
	if (dtStatusSucceed(status)) {
		realEndPos[1] = h;
	}

	// Otherwise, return success
	return 0;
}


NavMeshScene::NavMeshScene(int32_t id)
{
	this->id = id;
	navFilter.setIncludeFlags(SAMPLE_POLYFLAGS_ALL ^ SAMPLE_POLYFLAGS_DISABLED);
	navFilter.setExcludeFlags(0);
	navMesh = nullptr;
	navQuery = nullptr;
}

NavMeshScene::~NavMeshScene()
{
	if (navQuery != nullptr)
	{
		dtFreeNavMeshQuery(navQuery);
		navQuery = nullptr;
	}
	if (navMesh != nullptr)
	{
		dtFreeNavMesh(navMesh);
		navMesh = nullptr;
	}
}


