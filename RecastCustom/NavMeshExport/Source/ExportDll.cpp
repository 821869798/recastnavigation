#include "ExportDll.h"
#include "DetourCommon.h"
#include "ExportUtility.h"
#include "Sample_TileMesh.h"
#include "InputGeom.h"
#include "Custom_TempObstacles.h"
#include "Custom_SoleMesh.h"
#include "json.hpp"

#include <cstring>
#include <iostream>
#include <fstream>
#include <string>
#include <vector>
using json = nlohmann::json;

bool NavMeshExport(NavMeshBuildArgs* buildArgs, const float* vertices, const int* triangles, const int* area, const char* exportPath)
{
	if (NULL == buildArgs)
	{
		return false;
	}

	// Build nav mesh.
	rcContext buildContext;
	dtNavMesh* navMesh = rcBuildNavMesh(&buildContext, buildArgs, vertices, triangles, area, DT_VERTS_PER_POLYGON);
	if (NULL == navMesh)
	{
		return false;
	}

	// Calculate the AABB bound.
	float boundMinPosition[3] = { 0.0f, 0.0f, 0.0f };
	float boundMaxPosition[3] = { 0.0f, 0.0f, 0.0f };
	rcCalcBounds(vertices, buildArgs->vertexCount, boundMinPosition, boundMaxPosition);

	// Save nav mesh to file.
	FILE* file = fopen(exportPath, "wb");
	if (file == NULL)
	{
		return false;
	}

	// Store header.
	UnityNavMeshSetHeader fileHeader;
	fileHeader.magic = UNITY_NAVMESH_SET_MAGIC;
	fileHeader.version = UNITY_NAVMESH_SET_VERSION;

	// Store the aabb bound.
	//dtVcopy(fileHeader.boundBoxMin, boundMinPosition);
	//dtVcopy(fileHeader.boundBoxMax, boundMaxPosition);

	// Copy to the out result.
	dtVcopy(buildArgs->boundMin, boundMinPosition);
	dtVcopy(buildArgs->boundMax, boundMaxPosition);

	// Get tile count.
	const dtNavMesh* saveMesh = navMesh;
	fileHeader.numTiles = 0;
	for (int i = 0; i < saveMesh->getMaxTiles(); i++)
	{
		const dtMeshTile* meshTile = saveMesh->getTile(i);
		if ((NULL == meshTile) || (NULL == meshTile->header) || (meshTile->dataSize <= 0))
		{
			continue;
		}

		fileHeader.numTiles++;
	}

	// Store header params.
	memcpy(&fileHeader.params, saveMesh->getParams(), sizeof(dtNavMeshParams));
	fwrite(&fileHeader, sizeof(fileHeader), 1, file);

	// Store tiles.
	for (int i = 0; i < saveMesh->getMaxTiles(); i++)
	{
		const dtMeshTile* meshTile = saveMesh->getTile(i);
		if ((NULL == meshTile) || (NULL == meshTile->header) || (meshTile->dataSize <= 0))
		{
			continue;
		}

		// Store tile header.
		UnityNavMeshTileHeader tileHeader;
		tileHeader.tileRef = saveMesh->getTileRef(meshTile);
		tileHeader.dataSize = meshTile->dataSize;

		fwrite(&tileHeader, sizeof(tileHeader), 1, file);

		// Store tile data.
		fwrite(meshTile->data, meshTile->dataSize, 1, file);
	}

	fclose(file);
	file = NULL;

	// Free memory.
	dtFreeNavMesh(navMesh);

	return true;
}


int32_t TempObstaclesExport(RecastNavMeshBuildArgs* buildArgs, const char* objPath, const char* exportPath)
{

	if (buildArgs == NULL)
	{
		return 1;
	}
	if (!isFileExists(objPath))
	{
		return 2;
	}

	// set params
	BuildContext* ctx = new BuildContext();

	Custom_TempObstacles* sample = new Custom_TempObstacles();
	sample->setContext(ctx);

	// load obj
	InputGeom* geom = new InputGeom();
	if (!geom->load(ctx, objPath))
	{
		// clean up
		delete sample;
		delete geom;
		delete ctx;
		return 10;
	}
	sample->handleMeshChanged(geom);


	// build temp obstacles
	sample->setBuildArg(buildArgs);

	// Temp Obstacles special settings
	sample->handleSettings();

	if (!sample->handleBuild())
	{
		// clean up
		delete sample;
		delete geom;
		delete ctx;
		return 20;
	}

	// export temp obstacles
	if (!sample->handleSave(exportPath))
	{
		// clean up
		delete sample;
		delete geom;
		delete ctx;
		return 30;
	}

	// clean up
	delete sample;
	delete geom;
	delete ctx;

	return 0;
}

int32_t SoleMeshExport(RecastNavMeshBuildArgs* buildArgs, const char* objPath, const char* exportPath)
{
	if (buildArgs == NULL)
	{
		return 1;
	}
	if (!isFileExists(objPath))
	{
		return 2;
	}

	// set params
	BuildContext* ctx = new BuildContext();

	Custom_SoloMesh* sample = new Custom_SoloMesh();
	sample->setContext(ctx);

	// load obj
	InputGeom* geom = new InputGeom();
	if (!geom->load(ctx, objPath))
	{
		// clean up
		delete sample;
		delete geom;
		delete ctx;
		return 10;
	}
	sample->handleMeshChanged(geom);

	// build temp obstacles
	sample->setBuildArg(buildArgs);
	//sample->handleSettings();
	if (!sample->handleBuild())
	{
		// clean up
		delete sample;
		delete geom;
		delete ctx;
		return 20;
	}

	// export temp obstacles
	if (!sample->handleSave(exportPath))
	{
		// clean up
		delete sample;
		delete geom;
		delete ctx;
		return 30;
	}

	// clean up
	delete sample;
	delete geom;
	delete ctx;

	return 0;
}

int32_t navmeshInfoToJson(dtNavMesh* navMesh, const char* exportJsonPath)
{
	if (!navMesh)
	{
		return 1;
	}
	const dtNavMesh* constNavMesh = navMesh;
	// get tile count.
	std::vector<std::vector<float>> vertices;
	std::vector<std::vector<int>> triangles;
	int tileCount = navMesh->getMaxTiles();
	// for each tile
	for (int i = 0; i < tileCount; ++i) {
		const dtMeshTile* tile = constNavMesh->getTile(i);
		if (!tile) {
			continue;
		}
		if (!tile->header) {
			continue;
		}
		// for each polygon in tile
		for (int j = 0; j < tile->header->polyCount; ++j) {
			const dtPoly* poly = &tile->polys[j];

			std::vector<int> triangle;
			// for each vertex in polygon
			for (int k = 0; k < poly->vertCount; ++k) {
				// get vertex position
				float* v = &tile->verts[poly->verts[k] * 3];
				// push vertex to vertices
				vertices.push_back({ v[0], v[1], v[2] });
				// push vertex index to triangle
				int triangleIndex = static_cast<int>(vertices.size());
				triangle.push_back(triangleIndex - 1);
			}
			// push triangle to triangles
			triangles.push_back(triangle);
		}
	}

	json result = {
		{ "vertices",vertices },
		{ "triangles" ,triangles },
	};

	std::ofstream outFile(exportJsonPath);
	if (outFile.is_open()) {
		outFile << std::setw(4) << result << std::endl;
		outFile.close();
		return 0;
	}

	return 100;
}

int32_t TempObstaclesGenMeshInfo(const char* binPath, const char* exportJsonPath)
{
	Custom_TempObstacles* sample = new Custom_TempObstacles();
	dtNavMesh* navMehsh = sample->loadToNavMesh(binPath);
	if (navMehsh == NULL)
	{
		delete sample;
		return 1;
	}
	int32_t code = navmeshInfoToJson(navMehsh, exportJsonPath);
	delete sample;
	return code;
}

int32_t SoleMeshGenMeshInfo(const char* binPath, const char* exportJsonPath)
{
	Custom_SoloMesh* sample = new Custom_SoloMesh();
	dtNavMesh* navMehsh = sample->loadToNavMesh(binPath);
	if (navMehsh == NULL)
	{
		delete sample;
		return 1;
	}
	int32_t code = navmeshInfoToJson(navMehsh, exportJsonPath);
	delete sample;
	return code;
}

int32_t AutoGenMeshInfo(const char* binPath, const char* exportJsonPath)
{
	FILE* fp = fopen(binPath, "rb");
	if (!fp) return 101;

	// Read header.
	NavMeshSetCommonHeader header;
	size_t readLen = fread(&header, sizeof(NavMeshSetCommonHeader), 1, fp);
	fclose(fp);
	if (readLen != 1)
	{
		return 102;
	}
	if (header.magic == NAVMESHSET_MAGIC)
	{
		return SoleMeshGenMeshInfo(binPath, exportJsonPath);
	}
	else if (header.magic == TILECACHESET_MAGIC)
	{
		return TempObstaclesGenMeshInfo(binPath, exportJsonPath);
	}
	return 110;
}
