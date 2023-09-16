#include "Custom_TempObstacles.h"
#include "DetourTileCache.h"
#include "Recast.h"
#include "DetourCommon.h"
#include "InputGeom.h"


Custom_TempObstacles::Custom_TempObstacles()
{
}

Custom_TempObstacles::~Custom_TempObstacles()
{
}

void Custom_TempObstacles::setBuildArg(RecastNavMeshBuildArgs* buildArg)
{
	this->m_cellSize = buildArg->cellSize;
	this->m_cellHeight = buildArg->cellHeight;
	this->m_agentHeight = buildArg->agentHeight;
	this->m_agentRadius = buildArg->agentRadius;
	this->m_agentMaxClimb = buildArg->agentMaxClimb;
	this->m_agentMaxSlope = buildArg->agentMaxSlope;
	this->m_regionMinSize = buildArg->regionMinSize;
	this->m_regionMergeSize = buildArg->regionMergeSize;
	this->m_edgeMaxLen = buildArg->edgeMaxLen;
	this->m_edgeMaxError = buildArg->edgeMaxError;
	this->m_vertsPerPoly = buildArg->vertsPerPoly;
	this->m_detailSampleDist = buildArg->detailSampleDist;
	this->m_detailSampleMaxError = buildArg->detailSampleMaxError;
	this->m_partitionType = buildArg->partitionType;
	this->m_tileSize = buildArg->tileSize;
}

static const int EXPECTED_LAYERS_PER_TILE = 4;

void Custom_TempObstacles::handleSettings()
{
	if (m_geom)
	{
		const float* bmin = m_geom->getNavMeshBoundsMin();
		const float* bmax = m_geom->getNavMeshBoundsMax();
		char text[64];
		int gw = 0, gh = 0;
		rcCalcGridSize(bmin, bmax, m_cellSize, &gw, &gh);
		const int ts = (int)m_tileSize;
		const int tw = (gw + ts - 1) / ts;
		const int th = (gh + ts - 1) / ts;
		snprintf(text, 64, "Tiles  %d x %d", tw, th);

		// Max tiles and max polys affect how the tile IDs are caculated.
		// There are 22 bits available for identifying a tile and a polygon.
		int tileBits = rcMin((int)dtIlog2(dtNextPow2(tw * th * EXPECTED_LAYERS_PER_TILE)), 14);
		if (tileBits > 14) tileBits = 14;
		int polyBits = 22 - tileBits;
		m_maxTiles = 1 << tileBits;
		m_maxPolysPerTile = 1 << polyBits;
	}
	else
	{
		m_maxTiles = 0;
		m_maxPolysPerTile = 0;
	}

}

bool Custom_TempObstacles::handleSave(const char* savePath)
{
	saveAll(savePath);
	return true;
}

dtNavMesh* Custom_TempObstacles::loadToNavMesh(const char* path)
{
	dtFreeNavMesh(m_navMesh);
	dtFreeTileCache(m_tileCache);
	loadAll(path);
	return m_navMesh;
}
