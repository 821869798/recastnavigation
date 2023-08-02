#include "RecastDll.h"
#include <fstream>
#include <vector>
#include <iostream>

int readFileToBuffer(const std::string& filename, std::vector<char>& buffer) {
	std::ifstream file(filename, std::ios::binary | std::ios::ate);
	if (!file) {
		std::cout << "failed to open file\n";
		return 1;
	}

	std::streamsize size = file.tellg();
	file.seekg(0, std::ios::beg);

	buffer.resize(static_cast<size_t>(size));
	if (!file.read(buffer.data(), size)) {
		std::cout << "failed to read file\n";
		return 2;
	}
	return 0;
}

void test_PrintBounds(NavMeshScene* navMeshScene)
{
	float bmin[3];
	float bmax[3];
	auto result = RecastGetBounds(navMeshScene, bmin, bmax);
	if (result < 0) {
		printf("RecastGetBounds failed\n");
		return;
	}

	printf("bound min:%f %f %f\nbound max:%f %f %f\n", bmin[0], bmin[1], bmin[2], bmax[0], bmax[1], bmax[2]);
}


void test_RecastFindRandomPoint(NavMeshScene* navMeshScene)
{
	float randPos[3];
	auto ok = RecastFindRandomPoint(navMeshScene, randPos);
	if (!ok) {
		printf("RecastFindRandomPoint failed\n");
		return;
	}

	printf("randPos:%f %f %f\n", randPos[0], randPos[1], randPos[2]);
}

void test_RecastFindNearestPoint(NavMeshScene* navMeshScene)
{
	float extents[3];
	extents[0] = 2;
	extents[1] = 4;
	extents[2] = 2;

	float startPos[3];
	startPos[0] = 0;
	startPos[1] = 0;
	startPos[2] = 0;

	float endPos[3];
	int result = RecastFindNearestPoint(navMeshScene, extents, startPos, endPos);
	if (result < 0) {
		printf("RecastFindNearestPoint failed:%d\n", result);
		return;
	}

	printf("RecastFindNearestPoint endPos:%f %f %f\n", endPos[0], endPos[1], endPos[2]);

}

void testSoleNavMesh()
{
	printf("start testSoleNavMesh\n");

	std::vector<char> buffer;
	std::string path = R"(../../Bin/solo_navmesh.bin)";
	auto result = readFileToBuffer(path.c_str(), buffer);
	if (result != 0) {
		return;
	}

	const char* data = buffer.data();
	int32_t n = static_cast<int32_t>(buffer.size());

	auto recast = RecastLoad(1, data, n, false);
	if (recast == nullptr) {
		printf("load sole navmesh recast binary failed");
		return;
	}

	test_PrintBounds(recast);

	test_RecastFindRandomPoint(recast);

	test_RecastFindNearestPoint(recast);
}

void test_RecastAddBoxCenterObstacle(NavMeshScene* navMeshScene)
{
	float targetPos[3];
	targetPos[0] = -0.26572;
	targetPos[1] = -0.74188;
	targetPos[2] = -3.63819;

	float extents[3];
	extents[0] = 1;
	extents[1] = 1;
	extents[2] = 0.5;

	uint32_t obstacleId;
	int result = RecastAddBoxCenterObstacle(navMeshScene, &obstacleId, targetPos, extents, 0);
	if (result != 0) {
		printf("RecastAddBoxCenterObstacle failed:%d\n", result);
		return;
	}

	result = RecastUpdateObstacles(navMeshScene, false);
	if (result != 0) {
		printf("UpdateObstacles failed:%d\n", result);
		return;
	}

	printf("RecastAddBoxCenterObstacle result:%d\n", obstacleId);

}

void testTileCache()
{
	printf("start testTileCache\n");

	std::vector<char> buffer;
	std::string path = R"(../../Bin/all_tiles_tilecache.bin)";
	auto result = readFileToBuffer(path.c_str(), buffer);
	if (result != 0) {
		return;
	}

	const char* data = buffer.data();
	int32_t n = static_cast<int32_t>(buffer.size());

	auto recast = RecastLoad(1, data, n, true);
	if (recast == nullptr) {
		printf("load tilecache recast binary failed");
		return;
	}

	test_PrintBounds(recast);

	test_RecastFindRandomPoint(recast);

	test_RecastFindNearestPoint(recast);

	test_RecastAddBoxCenterObstacle(recast);
}

int main(int /*argc*/, char** /*argv*/) {

	testSoleNavMesh();

	testTileCache();

	return 0;
}

