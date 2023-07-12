mkdir build64 & pushd build64
cmake -G "Visual Studio 17 2022" -A x64 ..
popd
cmake --build build64 --config Release
md Plugins\x86_64
copy /Y build64\Release\NavMeshExport.dll Plugins\x86_64\NavMeshExport.dll
rmdir /S /Q build64
pause