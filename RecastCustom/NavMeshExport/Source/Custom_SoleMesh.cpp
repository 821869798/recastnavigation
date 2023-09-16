#include "Custom_SoleMesh.h"

Custom_SoloMesh::Custom_SoloMesh()
{
}

Custom_SoloMesh::~Custom_SoloMesh()
{
}

void Custom_SoloMesh::setBuildArg(RecastNavMeshBuildArgs* buildArg)
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
}


bool Custom_SoloMesh::handleSave(const char* savePath)
{
	saveAll(savePath, m_navMesh);
	return true;
}

dtNavMesh* Custom_SoloMesh::loadToNavMesh(const char* path)
{
	dtFreeNavMesh(m_navMesh);
	m_navMesh = Sample::loadAll(path);
	return m_navMesh;
}
