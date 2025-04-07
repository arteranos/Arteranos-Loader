cmake_minimum_required (VERSION 3.29)

if (POLICY CMP0141)
  cmake_policy(SET CMP0141 NEW)
  set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("Setup")

set(CPACK_PACKAGE_VERSION 0.9.0)

set(SolutionRoot ${CMAKE_SOURCE_DIR})
set(SetupDataDir ${CMAKE_SOURCE_DIR}/Setup)
set(GeneratedFiles ${CMAKE_BINARY_DIR}/Generated)

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
	add_custom_command(
		OUTPUT ${LoaderExeFile}
		COMMAND "msbuild" Arteranos-Loader\\Arteranos-Loader.csproj /t:Restore,Build -p:Configuration=Release -p:Platform=AnyCPU
		WORKING_DIRECTORY ${SolutionRoot}
		VERBATIM
	)

	add_custom_target(LoaderExe DEPENDS ${LoaderExeFile})

endif()

################## Install configuration #################################

set(CMAKE_PROJECT_HOMEPAGE_URL "https://github.com/arteranos/Arteranos-Loader")

set(CPACK_PACKAGE_VENDOR "arteranos")
set(CPACK_PACKAGE_CONTACT "arteranos")
set(CPACK_PACKAGE_DESCRIPTION "This is the Arteranos Loader, to download, update and start Arteranos.")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "Arteranos Loader")

set(CPACK_PACKAGING_INSTALL_PREFIX "/")

set(CPACK_DEBIAN_PACKAGE_SECTION "games")
set(CPACK_DEBIAN_PACKAGE_CONTROL_EXTRA ${GeneratedFiles}/postinst;${GeneratedFiles}/prerm)

set(CPACK_WIX_UPGRADE_GUID "FB5A017C-2AFC-4C8B-BC97-47787B42B6CB")
set(CPACK_WIX_PROGRAM_MENU_FOLDER "Arteranos")
set(CPACK_WIX_TEMPLATE ${SetupDataDir}/Windows/WIX.template.in)
set(CPACK_WIX_INSTALL_SCOPE "perMachine")

set(CPACK_WIX_PRODUCT_ICON ${SetupDataDir}/Windows/Arteranos.ico)
set(CPACK_WIX_LICENSE_RTF ${SetupDataDir}/Windows/SetupLicense.rtf)
set(CPACK_WIX_UI_BANNER ${SetupDataDir}/Windows/banner.bmp)
set(CPACK_WIX_UI_DIALOG ${SetupDataDir}/Windows/Dialog.bmp)
# set(CPACK_WIX_CUSTOM_XMLNS fw=http://schemas.microsoft.com/wix/FirewallExtension)
set(CPACK_WIX_EXTRA_SOURCES 
	${SetupDataDir}/Windows/WIX.fwrules.wxs
	${SetupDataDir}/Windows/WIX.moreShortcuts.wxs
)
set(CPACK_WIX_EXTENSIONS WixFirewallExtension)
#set()

if(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
	set(CPACK_PACKAGE_NAME "arteranos-server")
	set(CPACK_GENERATOR "DEB")

	add_custom_target(Installer DEPENDS package)
else()
set(CPACK_PACKAGE_NAME "Arteranos")
	set(CPACK_GENERATOR "WIX")

	add_custom_command(
		OUTPUT ${CPACK_PACKAGE_NAME}-${CPACK_PACKAGE_VERSION}-Win64.msi
		COMMAND "ninja" "package"
		DEPENDS ${LoaderExeFile}
		WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
	)

	add_custom_target(Installer DEPENDS ${CPACK_PACKAGE_NAME}-${CPACK_PACKAGE_VERSION}-Win64.msi)
endif()

include(CPack)
include(GNUInstallDirs)

################## Generated files #######################################

set(TargetLibexecDir	${CMAKE_INSTALL_LIBEXECDIR}/arteranos)
set(MainExePath /${TargetLibexecDir}/Arteranos-Loader.exe)

file(MAKE_DIRECTORY ${GeneratedFiles})

configure_file(
	${SetupDataDir}/Linux/Arteranos-Server-Start.in
	${GeneratedFiles}/Arteranos-Server-Start
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

configure_file(
	${SetupDataDir}/Linux/init_d_arteranos.in
	${GeneratedFiles}/arteranos
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

configure_file(
	${SetupDataDir}/Linux/postinst.in
	${GeneratedFiles}/postinst
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

configure_file(
	${SetupDataDir}/Linux/prerm.in
	${GeneratedFiles}/prerm
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

################## Packaging #############################################

if(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
	# Linux keeps this in a dedicated libexec directory, maybe with a sofdt link in /bin
	set(TargetInstallBindir ${TargetLibexecDir})
else()
	# Windows keeps these in the Program Files directory
	set(TargetInstallBindir ${CMAKE_INSTALL_BINDIR})
endif()

install(DIRECTORY
	${LoaderExeDir}/
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	DESTINATION ${TargetInstallBindir}
)

install(DIRECTORY
	${SetupDataDir}/Common/_bin/
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	DESTINATION ${TargetInstallBindir}
)

if(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
	install(PROGRAMS
		${GeneratedFiles}/Arteranos-Server-Start
		DESTINATION ${CMAKE_INSTALL_BINDIR}
	)

	install(PROGRAMS
		${GeneratedFiles}/arteranos
		DESTINATION ${CMAKE_INSTALL_SYSCONFDIR}/init.d
	)
else()
	set_property(INSTALL ${TargetInstallBindir}/Arteranos-Loader.exe
		PROPERTY CPACK_START_MENU_SHORTCUTS "Arteranos"
	)

	set_property(INSTALL ${TargetInstallBindir}/Arteranos-Loader.exe
		PROPERTY CPACK_DESKTOP_SHORTCUTS "Arteranos"
	)
endif()
