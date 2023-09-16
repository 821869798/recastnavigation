#pragma once
#include "Sample_TempObstacles.h"
#include "ExportUtility.h"

class Custom_TempObstacles : public Sample_TempObstacles
{
protected:

public:
	Custom_TempObstacles();
	virtual ~Custom_TempObstacles();

	void setBuildArg(RecastNavMeshBuildArgs* buildArg);
	virtual void handleSettings();
	virtual bool handleSave(const char* savePath);
	dtNavMesh* loadToNavMesh(const char* path);
};

