#pragma once 
#include "Sample_SoloMesh.h"
#include "ExportUtility.h"

class Custom_SoloMesh : public Sample_SoloMesh
{
protected:

public:
	Custom_SoloMesh();
	virtual ~Custom_SoloMesh();

	void setBuildArg(RecastNavMeshBuildArgs* buildArg);
	virtual bool handleSave(const char* savePath);
	dtNavMesh* loadToNavMesh(const char* path);

};

