mkdir -p Plugins/x86
mkdir -p build_linux32 && cd build_linux32
cmake -DCMAKE_C_FLAGS=-m32 -DCMAKE_CXX_FLAGS=-m32 -DCMAKE_SHARED_LINKER_FLAGS=-m32 ../
cd ..
cmake --build build_linux32 --config Release
cp build_linux32/libNavMeshExport.so Plugins/x86/libNavMeshExport.so
rm -rf build_linux32
