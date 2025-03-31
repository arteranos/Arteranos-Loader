# CMakeList.txt: CMake-Projekt für "Setup". Schließen Sie die Quelle ein, und definieren Sie
# projektspezifische Logik hier.
#
cmake_minimum_required (VERSION 3.8)

# Aktivieren Sie Hot Reload für MSVC-Compiler, sofern unterstützt.
if (POLICY CMP0141)
  cmake_policy(SET CMP0141 NEW)
  set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("Setup")

set(SolutionRoot ${CMAKE_SOURCE_DIR})

message(STATUS "Source dir   : ${CMAKE_SOURCE_DIR}")
message(STATUS "Binary dir   : ${CMAKE_BINARY_DIR}")
message(STATUS "OS           : ${CMAKE_SYSTEM_NAME}")
message(STATUS "Architecture : ${CMAKE_SYSTEM_PROCESSOR}")
message(STATUS "Version      : ${CPACK_PACKAGE_VERSION}")


set(LoaderExeDir ${SolutionRoot}/Arteranos-Loader/bin/Release)
set(LoaderExeFile ${LoaderExeDir}/Arteranos-Loader.exe)

set(APIDllDir ${SolutionRoot}/ipfs-rpc-api/bin/Release)
set(APIDllFile ${APIDllDir}/ipfs-rpc-api.dll)

if(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
	if(NOT EXISTS ${LoaderExeFile})
		message(FATAL_ERROR "Need to build Loader executable with the Windows toolchain first")
	endif()
else()
	add_custom_target(LoaderExe DEPENDS ${LoaderExeFile})
	add_custom_command(
		OUTPUT ${LoaderExeFile}
		COMMAND "msbuild" Arteranos-Loader\\Arteranos-Loader.csproj /t:Restore,Build -p:Configuration=Release -p:Platform=AnyCPU
		WORKING_DIRECTORY ${SolutionRoot}
		VERBATIM
	)


# No need, dependent projects are managed in the .csproj
#
#	add_custom_target(ipfs-rpc-api DEPENDS ${APIDllFile})
#	add_custom_command(
#		OUTPUT ${APIDllFile}
#		COMMAND "msbuild" ipfs-rpc-api\\ipfs-rpc-api.csproj /t:Restore,Build -p:Configuration=Release -p:Platform=AnyCPU
#		WORKING_DIRECTORY ${SolutionRoot}
#		VERBATIM
#	)
endif()



################## Install section #######################################

set(CMAKE_PROJECT_HOMEPAGE_URL "https://github.com/arteranos/Arteranos-Loader")

set(CPACK_PACKAGE_NAME "Arteranos")
set(CPACK_PACKAGE_VENDOR "arteranos")
set(CPACK_PACKAGE_CONTACT "arteranos")
set(CPACK_PACKAGE_DESCRIPTION "This is the Arteranos Loader, to download, update and start Arteranos.")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "Arteranos Loader")

set(SetupDataDir ${CMAKE_SOURCE_DIR}/Setup)

set(CPACK_DEBIAN_PACKAGE_SECTION "games")
#set(CPACK_DEBIAN_PACKAGE_CONTROL_EXTRA ${CMAKE_BINARY_DIR}/postinst;${CMAKE_BINARY_DIR}/prerm)

set(CPACK_WIX_UPGRADE_GUID "FB5A017C-2AFC-4C8B-BC97-47787B42B6CB")
set(CPACK_WIX_PROGRAM_MENU_FOLDER "Arteranos")
set(CPACK_WIX_TEMPLATE ${SetupDataDir}/WIX.template.in)
set(CPACK_WIX_INSTALL_SCOPE "perMachine")

set(CPACK_WIX_PRODUCT_ICON ${SetupDataDir}/Arteranos.ico)
set(CPACK_WIX_LICENSE_RTF ${SetupDataDir}/SetupLicense.rtf)
set(CPACK_WIX_UI_BANNER ${SetupDataDir}/banner.bmp)
set(CPACK_WIX_UI_DIALOG ${SetupDataDir}/Dialog.bmp)
# set(CPACK_WIX_CUSTOM_XMLNS fw=http://schemas.microsoft.com/wix/FirewallExtension)
set(CPACK_WIX_EXTRA_SOURCES ${SetupDataDir}/WIX.fwrules.wxs)
set(CPACK_WIX_EXTENSIONS WixFirewallExtension)
#set()

if(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
	set(CPACK_PACKAGING_INSTALL_PREFIX "/usr/")
	set(CPACK_GENERATOR "DEB")
else()
	set(CPACK_PACKAGING_INSTALL_PREFIX "/")
	set(CPACK_GENERATOR "WIX")
endif()

include(CPack)
include(GNUInstallDirs)

install(PROGRAMS
	${LoaderExeFile}
	DESTINATION ${CMAKE_INSTALL_BINDIR}
)

install(DIRECTORY
	${LoaderExeDir}/
	DESTINATION ${CMAKE_INSTALL_BINDIR}
)

set_property(INSTALL ${CMAKE_INSTALL_BINDIR}/Arteranos-Loader.exe
    PROPERTY CPACK_START_MENU_SHORTCUTS "Arteranos"
)

set_property(INSTALL ${CMAKE_INSTALL_BINDIR}/Arteranos-Loader.exe
    PROPERTY CPACK_DESKTOP_SHORTCUTS "Arteranos"
)
