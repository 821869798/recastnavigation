#pragma once

#if !RECASTNAVIGATION_STATIC && WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif

#include "Recast.h"
#include "DetourNavMesh.h"
#include <map>
#include <unordered_map>


// NavMesh filter flags.
enum UnityNavMeshPolyFlags
{
	UNM_POLYFLAGS_WALK = 0x01,      // Ability to walk (ground, grass, road)
	UNM_POLYFLAGS_SWIM = 0x02,      // Ability to swim (water).
	UNM_POLYFLAGS_DOOR = 0x04,      // Ability to move through doors.
	UNM_POLYFLAGS_JUMP = 0x08,      // Ability to jump.
	UNM_POLYFLAGS_DISABLED = 0x10,  // Disabled polygon
	UNM_POLYFLAGS_ALL = 0xffff      // All abilities.
};


//
// Save NavMesh to file, From recast demo.
//

// NavMesh mesh file format header.
static const int UNITY_NAVMESH_SET_MAGIC = ('M' << 24) | ('S' << 16) | ('E' << 8) | 'T';

// NavMesh mesh version.
static const int UNITY_NAVMESH_SET_VERSION = 1;

// Unity NavMesh set header info.
struct UnityNavMeshSetHeader
{
	// The file format header.
	int magic;

	// Data version.
	int version;

	// Number of tiles.
	int numTiles;

	// The min point of aabb bound.
	//float boundBoxMin[3];

	// The max point of aabb bound.
	//float boundBoxMax[3];

	// NavMesh create parameters.
	dtNavMeshParams params;

	// Default constructor.
	UnityNavMeshSetHeader() : magic(0), version(0), numTiles(0) {}
};


// Unity NavMesh tile header.
struct UnityNavMeshTileHeader
{
	// Tile reference.
	dtTileRef tileRef;

	// Tile data size.
	int dataSize;

	// Default constructor.
	UnityNavMeshTileHeader() : tileRef(0), dataSize(0) {}
};


// edge index - (1st tri of the shared edge / 2nd tri of the shared edge).
// Define poly mesh edge shared info.
// key - edge hash, value - the two triangles that share this edge, if only one triangle then, the other value will be -1.
typedef std::map<int, std::pair<int, int> > PolyMeshEdgeSharedMap;

// The vertex position in vector space.
struct VertexPosition
{
	float position[3];

	VertexPosition()
	{
		position[0] = 0.0f;
		position[1] = 0.0f;
		position[2] = 0.0f;
	}

	VertexPosition(float x, float y, float z)
	{
		position[0] = x;
		position[1] = y;
		position[2] = z;
	}
};

// Hash function for vertex position map.
struct VertexPositionHashFunc
{
	size_t operator()(const VertexPosition& o) const
	{
		std::hash<float> hasher = std::hash<float>();
		return hasher(o.position[0]) + hasher(o.position[1]) + hasher(o.position[2]);
	}
};

// Compare function for vertex position map.
struct VertexPositionCompareFunc
{
	bool operator()(const VertexPosition& l, const VertexPosition& r) const
	{
		return (rcAbs(l.position[0] - r.position[0]) < 0.000001f) && (rcAbs(l.position[1] - r.position[1]) < 0.000001f) && (rcAbs(l.position[2] - r.position[2]) < 0.000001f);
	}
};


// Vertex position and index map.
// key - vertex in vector space, value - the vertex index in merged vertices buffer.
typedef std::unordered_map<VertexPosition, int, VertexPositionHashFunc, VertexPositionCompareFunc> VertexIndexMap;

struct NavMeshBuildArgs
{
	int vertexCount;
	int triangleCount;
	unsigned short regionId;
	float cellSize;
	float cellHeight;
	float walkableHeight;
	float walkableRadius;
	float walkableClimb;
	float boundMin[3];
	float boundMax[3];
};

// The core part of building NavMesh from exported unity NavMesh.
dtNavMesh* rcBuildNavMesh(rcContext* ctx, NavMeshBuildArgs* buildArgs, const float* vertices, const int* triangles, const int* area, int maxVertexPerPolygon);

#ifdef __cplusplus
extern "C" {
#endif

	// The api used for importing NavMesh exported by NavMeshExporter_Unity from unity.
	//EXPORT_API dtNavMesh* NavMeshImporter_Unity(const char* importPath, float* boundBoxMin, float* boundBoxMax);

	EXPORT_API bool NavMeshExport(NavMeshBuildArgs* buildArgs, const float* vertices, const int* triangles, const int* area, const char* exportPath);

#ifdef __cplusplus
}
#endif