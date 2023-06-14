mkdir -p build_osx && cd build_osx
cmake -GXcode ../
cd ..
cmake --build build_osx --config Release
mkdir -p Plugins/NavMeshExport.bundle/Contents/MacOS/
cp build_osx/Release/NavMeshExport.bundle/Contents/MacOS/NavMeshExport Plugins/NavMeshExport.bundle/Contents/MacOS/NavMeshExport
rm -rf build_osx
