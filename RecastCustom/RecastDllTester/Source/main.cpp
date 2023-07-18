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
		printf("RecastFindNearestPoint failed\n");
		return;
	}

	printf("RecastFindNearestPoint endPos:%f %f %f\n", endPos[0], endPos[1], endPos[2]);

}

int main(int /*argc*/, char** /*argv*/) {

	std::vector<char> buffer;
	//std::string path = R"(E:\work\lhcx2\Assets\Res\12_ModuleCfg\NavMesh\level_test1.navmesh.bytes)";
	//std::string path = R"(../../Bin/bad.navmesh.bytes)";
	std::string path = R"(../../Bin/solo_navmesh.bin)";
	auto result = readFileToBuffer(path.c_str(), buffer);
	if (result != 0) {
		return 1;
	}

	const char* data = buffer.data();
	int32_t n = static_cast<int32_t>(buffer.size());

	auto recast = RecastLoad(1, data, n);
	if (recast == nullptr) {
		printf("load recast binary failed");
		return 1;
	}

	test_RecastFindRandomPoint(recast);

	test_RecastFindNearestPoint(recast);

	return 0;
}

