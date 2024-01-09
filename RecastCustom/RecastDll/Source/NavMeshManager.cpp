#include "NavMeshManager.h"

NavMeshManager* NavMeshManager::instance = nullptr;

NavMeshManager* NavMeshManager::getInstance()
{
	if (instance == nullptr)
	{
		instance = new NavMeshManager();
	}
	return instance;
}

NavMeshScene* NavMeshManager::newScene(int32_t id, const char* buffer, int32_t n, bool dynamic)
{
	std::unique_lock<std::mutex> lock(this->lockScenes);

	const auto it = navMeshScenes.find(id);
	if (it != navMeshScenes.end()) {
		delete it->second;
		navMeshScenes.erase(it);
	}

	NavMeshScene* navMeshScene;
	if (dynamic)
	{
		navMeshScene = new DynNavMeshScene(id);
	}
	else
	{
		navMeshScene = new NavMeshScene(id);
	}

	int32_t ret = navMeshScene->init(buffer, n);

	if (ret != 0)
	{
		delete navMeshScene;
		return nullptr;
	}

	navMeshScenes[id] = navMeshScene;
	return navMeshScene;
}

NavMeshScene* NavMeshManager::getScene(int32_t id)
{
	std::unique_lock<std::mutex> lock(this->lockScenes);
	const auto it = navMeshScenes.find(id);
	if (it != navMeshScenes.end())
	{
		return it->second;
	}
	return nullptr;
}

bool NavMeshManager::clearScene(int32_t id)
{
	std::unique_lock<std::mutex> lock(this->lockScenes);
	const auto it = navMeshScenes.find(id);
	if (it != navMeshScenes.end())
	{
		delete it->second;
		navMeshScenes.erase(it);
		return true;
	}

	return false;
}

void NavMeshManager::clearAll()
{
	std::unique_lock<std::mutex> lock(this->lockScenes);
	for (auto kv : navMeshScenes)
	{
		delete kv.second;
	}
	navMeshScenes.clear();
}

