#pragma once
#include <unordered_map>
#include "NavMeshScene.h"
#include "DynNavMeshScene.h"

class NavMeshManager
{
public:
	static NavMeshManager* getInstance();
	NavMeshScene* newScene(int32_t id, const char* buffer, int32_t n, bool dynamic);
	NavMeshScene* getScene(int32_t id);
	bool clearScene(int32_t id);
	void clearAll();

private:
	static NavMeshManager* instance;
	NavMeshManager() {}
	~NavMeshManager() {}
	NavMeshManager(const NavMeshManager&) = delete;
	NavMeshManager& operator=(const NavMeshManager&) = delete;

	std::unordered_map<int32_t, NavMeshScene*> navMeshScenes;

};
