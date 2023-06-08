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


int main(int /*argc*/, char** /*argv*/) {

	std::vector<char> buffer;
	auto result = readFileToBuffer("../../Bin/solo_navmesh.bin", buffer);
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

	float randPos[3];
	auto ok = RecastFindRandomPoint(recast, randPos);
	if (!ok) {
		printf("RecastFindRandomPoint failed");
		return 1;
	}

	printf("randPos:%f %f %f", randPos[0], randPos[1], randPos[2]);

	return 0;
}