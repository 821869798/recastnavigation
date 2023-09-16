#pragma once
#include "Sample.h"
#include <fstream>

struct RecastNavMeshBuildArgs
{
	float cellSize;
	float cellHeight;
	float agentHeight;
	float agentRadius;
	float agentMaxClimb;
	float agentMaxSlope;
	float regionMinSize;
	float regionMergeSize;
	float edgeMaxLen;
	float edgeMaxError;
	float vertsPerPoly;
	float detailSampleDist;
	float detailSampleMaxError;
	float tileSize;
	int partitionType;
};

inline RecastNavMeshBuildArgs RecastNavMeshBuildArgsDefault()
{
	RecastNavMeshBuildArgs buildArgs;
	buildArgs.cellSize = 0.3f;
	buildArgs.cellHeight = 0.2f;
	buildArgs.agentHeight = 2.0f;
	buildArgs.agentRadius = 0.6f;
	buildArgs.agentMaxClimb = 0.9f;
	buildArgs.agentMaxSlope = 45.0f;
	buildArgs.regionMinSize = 8.0f;
	buildArgs.regionMergeSize = 20.0f;
	buildArgs.edgeMaxLen = 12.0f;
	buildArgs.edgeMaxError = 1.3f;
	buildArgs.vertsPerPoly = 6.0f;
	buildArgs.detailSampleDist = 6.0f;
	buildArgs.detailSampleMaxError = 1.0f;
	buildArgs.partitionType = SAMPLE_PARTITION_WATERSHED;
	buildArgs.tileSize = 48;
	return buildArgs;
}


static const int NAVMESHSET_MAGIC = 'M' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;

static const int TILECACHESET_MAGIC = 'T' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'TSET';
static const int TILECACHESET_VERSION = 1;

struct NavMeshSetCommonHeader
{
	int magic;
	int version;
};



inline bool isFileExists(const char* filePath) {
	std::ifstream f(filePath);
	return f.good();
}
