cmake_minimum_required(VERSION 3.22)

list(APPEND CMAKE_MODULE_PATH ${CMAKE_CURRENT_SOURCE_DIR}/cmake)

if ( WIN32 AND NOT CYGWIN AND NOT ( CMAKE_SYSTEM_NAME STREQUAL "WindowsStore" ) )
    set(CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE} /MT" CACHE STRING "")
    set(CMAKE_C_FLAGS_DEBUG "${CMAKE_C_FLAGS_DEBUG} /MTd" CACHE STRING "")
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT" CACHE STRING "")
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MTd" CACHE STRING "")
endif ()

if ( IOS )
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fembed-bitcode")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fembed-bitcode")
endif ()


project(NavMeshExport)

find_path(NAVMESHEXPORT_PROJECT_DIR NAMES SConstruct
    PATHS
    ${CMAKE_SOURCE_DIR}
    NO_DEFAULT_PATH
)

MARK_AS_ADVANCED(NAVMESHEXPORT_PROJECT_DIR)

# 配置cpp文件
file(GLOB NAVMESHEXPORT_SOURCES 
	Source/*.cpp
	json/*.hpp
	../../Detour/Source/*.cpp 
	../../DetourCrowd/Source/*.cpp 
	../../DetourTileCache/Source/*.cpp 
	../../Recast/Source/*.cpp
	../../RecastDemo/Source/*.cpp
	../../DebugUtils/Source/*.cpp
	../../RecastDemo/Contrib/fastlz/fastlz.c
)

# 排除 main.cpp 文件
list(REMOVE_ITEM NAVMESHEXPORT_SOURCES "${CMAKE_CURRENT_SOURCE_DIR}/../../RecastDemo/Source/main.cpp")

if(NOT SDL2_ROOT_DIR)
  if(WIN32)
    set(SDL2_ROOT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../RecastDemo/Contrib/SDL")
  elseif(APPLE)
    set(SDL2_ROOT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../RecastDemo/Bin")
  endif()
endif()

find_package(OpenGL REQUIRED)
if(APPLE)
  find_library(SDL2_LIBRARY 
    NAMES SDL2
    PATHS ${SDL2_ROOT_DIR}
    REQUIRED)
elseif(WIN32)
  find_package(SDL2 REQUIRED)
else()
  find_package(SDL2 REQUIRED)
endif()


include_directories(SYSTEM ${OPENGL_INCLUDE_DIR})
include_directories(SYSTEM ../../RecastDemo/Contrib/fastlz)
include_directories(SYSTEM ../../RecastDemo/Contrib)
include_directories(../../DebugUtils/Include)
include_directories(../../Detour/Include)
include_directories(../../DetourCrowd/Include)
include_directories(../../DetourTileCache/Include)
include_directories(../../Recast/Include)
include_directories(../../RecastDemo/Include)
include_directories(../../RecastDemo/Contrib/SDL/include)
include_directories(Include)
include_directories(json)
if(APPLE)
  include_directories(${SDL2_LIBRARY}/Headers)
endif()

macro(source_group_by_dir proj_dir source_files)
    if(MSVC OR APPLE)
        get_filename_component(sgbd_cur_dir ${proj_dir} ABSOLUTE)
        foreach(sgbd_file ${${source_files}})
            get_filename_component(sgbd_abs_file ${sgbd_file} ABSOLUTE)
            file(RELATIVE_PATH sgbd_fpath ${sgbd_cur_dir} ${sgbd_abs_file})
            string(REGEX REPLACE "\(.*\)/.*" \\1 sgbd_group_name ${sgbd_fpath})
            string(COMPARE EQUAL ${sgbd_fpath} ${sgbd_group_name} sgbd_nogroup)
            string(REPLACE "/" "\\" sgbd_group_name ${sgbd_group_name})
            if(sgbd_nogroup)
                set(sgbd_group_name "\\")
            endif(sgbd_nogroup)
            source_group(${sgbd_group_name} FILES ${sgbd_file})
        endforeach(sgbd_file)
    endif(MSVC OR APPLE)
endmacro(source_group_by_dir)

source_group_by_dir(${CMAKE_CURRENT_SOURCE_DIR} NAVMESHEXPORT_SOURCES)

if (APPLE)
    if (IOS)
        set(CMAKE_OSX_ARCHITECTURES "$(ARCHS_STANDARD)")
        add_library(NavMeshExport STATIC ${NAVMESHEXPORT_SOURCES})
    else ()
        set(CMAKE_OSX_ARCHITECTURES "$(ARCHS_STANDARD_64_BIT)")
        add_library(NavMeshExport MODULE ${NAVMESHEXPORT_SOURCES})
        set_target_properties ( NavMeshExport PROPERTIES BUNDLE TRUE )
    endif ()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -Os -Wall")
    target_link_libraries(NavMeshExport ${OPENGL_LIBRARIES} ${SDL2_LIBRARY})
else()
    add_library(NavMeshExport SHARED ${NAVMESHEXPORT_SOURCES})
    target_link_libraries(NavMeshExport ${OPENGL_LIBRARIES} SDL2::SDL2main )
endif ( )

# 精简so文件大小
if (ANDROID)
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=c++11 -Os -Wall")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -Os -Wall")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -s")
endif ( )

if ( WIN32 AND NOT CYGWIN )
    target_compile_definitions (NavMeshExport PRIVATE DLL_EXPORTS)
endif ( )

