#include <fstream>
#include <vector>
#include <iostream>
#include <ExportDll.h>

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

void testTempObstaclesExport()
{
	std::string objPath = R"(../../Bin/Meshes/nav_test.obj)";
	std::string exportPath = R"(../../Bin/all_tiles_tilecache.bin)";
	RecastNavMeshBuildArgs buildArgs = RecastNavMeshBuildArgsDefault();
	int32_t result = TempObstaclesExport(&buildArgs, objPath.c_str(), exportPath.c_str());
	if (result != 0)
	{
		printf("TempObstaclesExport failed:%d\n", result);
		return;
	}
	printf("TempObstaclesExport success\n");
}

void testSoleMeshExport()
{
	std::string objPath = R"(../../Bin/Meshes/nav_test.obj)";
	std::string exportPath = R"(../../Bin/solo_navmesh.bin)";
	RecastNavMeshBuildArgs buildArgs = RecastNavMeshBuildArgsDefault();
	int32_t result = SoleMeshExport(&buildArgs, objPath.c_str(), exportPath.c_str());
	if (result != 0)
	{
		printf("SoleMeshExport failed:%d\n", result);
		return;
	}
	printf("SoleMeshExport success\n");
}

void testTempObstaclesGenMeshInfo()
{
	std::string binPath = R"(../../Bin/all_tiles_tilecache.bin)";
	std::string exportJsonPath = R"(../../Bin/all_tiles_tilecache.bin.json)";

	int32_t result = AutoGenMeshInfo(binPath.c_str(), exportJsonPath.c_str());
	if (result != 0)
	{
		printf("TempObstaclesGenMeshInfo failed:%d\n", result);
		return;
	}
	printf("TempObstaclesGenMeshInfo success\n");
}

void testSoleMeshGenMeshInfo()
{
	std::string binPath = R"(../../Bin/solo_navmesh.bin)";
	std::string exportJsonPath = R"(../../Bin/solo_navmesh.bin.json)";

	int32_t result = AutoGenMeshInfo(binPath.c_str(), exportJsonPath.c_str());
	if (result != 0)
	{
		printf("SoleMeshGenMeshInfo failed:%d\n", result);
		return;
	}
	printf("SoleMeshGenMeshInfo success\n");
}

int main(int /*argc*/, char** /*argv*/) {

	testTempObstaclesExport();

	testSoleMeshExport();

	testTempObstaclesGenMeshInfo();

	testSoleMeshGenMeshInfo();

	return 0;
}

