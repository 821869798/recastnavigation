mkdir build32 & pushd build32
cmake -G "Visual Studio 17 2022" ..
popd
cmake --build build32 --config Release
md Plugins\x86
copy /Y build32\Release\NavMeshExport.dll Plugins\x86\NavMeshExport.dll
rmdir /S /Q build32
pause