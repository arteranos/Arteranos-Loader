﻿cmake_minimum_required (VERSION 3.28)

project ("Setup")

set(CPACK_PACKAGE_VERSION 0.11.0)

set(SolutionRoot ${CMAKE_SOURCE_DIR})
set(SetupDataDir ${CMAKE_SOURCE_DIR}/Setup)
set(GeneratedFiles ${CMAKE_BINARY_DIR}/Generated)

file(MAKE_DIRECTORY ${GeneratedFiles})

message(STATUS "Source dir   : ${CMAKE_SOURCE_DIR}")
message(STATUS "Binary dir   : ${CMAKE_BINARY_DIR}")
message(STATUS "OS           : ${CMAKE_SYSTEM_NAME}")
message(STATUS "Architecture : ${CMAKE_SYSTEM_PROCESSOR}")
message(STATUS "Version      : ${CPACK_PACKAGE_VERSION}")

################## Multiarch building ####################################

macro(buildsubtarget subtarget)
	set(subbindir ${CMAKE_BINARY_DIR}/${subtarget})
	add_custom_target(${subtarget}
		COMMAND ${CMAKE_COMMAND} -E make_directory ${subbindir}
		COMMAND cd ${subbindir} 
			&& ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE} -G ${CMAKE_GENERATOR} -DTARGET_SYSTEM=${subtarget} -S${CMAKE_SOURCE_DIR} 
			&& ${CMAKE_MAKE_PROGRAM} Installer
	)

	add_custom_target(single_${subtarget}
		COMMAND ${CMAKE_COMMAND} -E make_directory ${subbindir}
		COMMAND cd ${subbindir} 
			&& ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE} -G ${CMAKE_GENERATOR} -DTARGET_SYSTEM=${subtarget} -S${CMAKE_SOURCE_DIR} 
			&& ${CMAKE_MAKE_PROGRAM} Installer
	)

	add_dependencies(allarchs ${subtarget})

	# Chain individual architectures to prevent race conditions -- dotnet separates output directories, but NOT intermediates
	if(DEFINED lastarch)
		add_dependencies(${subtarget} ${lastarch})
	endif()

	set(lastarch ${subtarget})
endmacro()

if(NOT DEFINED TARGET_SYSTEM)
	add_custom_target(allarchs)

	buildsubtarget("linux-x64")
	buildsubtarget("win-x64")

	return()
endif()

################## Building ##############################################

message(STATUS "Target system: ${TARGET_SYSTEM}")

set(LoaderExeDir ${SolutionRoot}/Arteranos-Loader/bin/Release/net8.0/${TARGET_SYSTEM}/publish)
set(LoaderExeName ArteranosLoader${CMAKE_EXECUTABLE_SUFFIX})
set(LoaderExeFile ${LoaderExeDir}/ArteranosLoader)

add_custom_target(LoaderExe
	COMMAND dotnet publish Arteranos-Loader.sln -r ${TARGET_SYSTEM} -c Release 
	# --sc
	# /p:PublishSingleFile=true /p:PublishTrimmed=true
	# No Self contained because of package/installer dependencies to .net8.0
	# SingleFile leaves the .so files outside, and Trimmed needs indepth testing
	WORKING_DIRECTORY ${SolutionRoot}
)


################## Install configuration #################################

set(CPACK_PACKAGING_INSTALL_PREFIX "/")

set(CMAKE_PROJECT_HOMEPAGE_URL "https://github.com/arteranos/Arteranos-Loader")

set(CPACK_PACKAGE_VENDOR "arteranos")
set(CPACK_PACKAGE_CONTACT "arteranos")
set(CPACK_PACKAGE_DESCRIPTION "This is the Arteranos Loader, to download, update and start Arteranos.")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "Arteranos Loader")

set(CPACK_PACKAGE_INSTALL_DIRECTORY "Arteranos")

set(CPACK_RESOURCE_FILE_LICENSE ${SetupDataDir}/Windows/SetupLicense.rtf)

# Override package file name to denote the _target_ architecture, not the build system
if(${TARGET_SYSTEM} STREQUAL "linux-x64")
	set(CPACK_PACKAGE_NAME "arteranos-server")
	set(CPACK_GENERATOR "DEB")

	set(CPACK_PACKAGE_FILE_NAME ${CPACK_PACKAGE_NAME}-${CPACK_PACKAGE_VERSION}-Linux64)

	set(CPACK_DEBIAN_PACKAGE_SECTION "games")
	set(CPACK_DEBIAN_PACKAGE_CONTROL_EXTRA ${GeneratedFiles}/postinst;${GeneratedFiles}/prerm)
	set(CPACK_DEBIAN_PACKAGE_DEPENDS
		dotnet-runtime-8.0
	)
elseif(${TARGET_SYSTEM} STREQUAL "win-x64")
	set(CPACK_PACKAGE_NAME "Arteranos")
	set(CPACK_GENERATOR "NSIS")

	set(CPACK_PACKAGE_FILE_NAME ${CPACK_PACKAGE_NAME}-${CPACK_PACKAGE_VERSION}-Win64)

#	set(CPACK_NSIS_MUI_HEADERIMAGE ${SetupDataDir}/Windows/banner.bmp)
	set(CPACK_NSIS_MUI_WELCOMEFINISHPAGE_BITMAP ${SetupDataDir}/Windows/Dialog.bmp)
	set(CPACK_NSIS_MUI_ICON ${SetupDataDir}/Windows/Arteranos.ico)
	set(CPACK_NSIS_INSTALLED_ICON_NAME ${SetupDataDir}/Windows/Arteranos.ico)

	# CPack-NSIS-generator disregards the CPACK_* target properties, so we have to add the extra lines directly into the generated .nsi script.
	string(JOIN "\n" CPACK_NSIS_CREATE_ICONS_EXTRA
		"CreateShortCut '$SMPROGRAMS\\\\$STARTMENU_FOLDER\\\\Arteranos.lnk' '$INSTDIR\\\\bin\\\\${LoaderExeName}'"
		"CreateShortCut '$SMPROGRAMS\\\\$STARTMENU_FOLDER\\\\Arteranos Dedicated Server.lnk' '$INSTDIR\\\\bin\\\\${LoaderExeName}' '--server'"
	)

	string(JOIN "\n" CPACK_NSIS_DELETE_ICONS_EXTRA
		"Delete '$SMPROGRAMS\\\\$START_MENU\\\\Arteranos.lnk'"
		"Delete '$SMPROGRAMS\\\\$START_MENU\\\\Arteranos Dedicated Server.lnk'"
	)

	configure_file(
		${SetupDataDir}/Windows/NSIS.addFwRules.nsi.in
		${GeneratedFiles}/NSIS.addFwRules.nsi
		FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
		@ONLY
		NEWLINE_STYLE LF
	)
	file(READ ${GeneratedFiles}/NSIS.addFwRules.nsi CPACK_NSIS_EXTRA_INSTALL_COMMANDS)

	configure_file(
		${SetupDataDir}/Windows/NSIS.rmFwRules.nsi.in
		${GeneratedFiles}/NSIS.rmFwRules.nsi
		FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
		@ONLY
		NEWLINE_STYLE LF
	)
	file(READ ${GeneratedFiles}/NSIS.rmFwRules.nsi CPACK_NSIS_EXTRA_UNINSTALL_COMMANDS)

else()
	message(FATAL_ERROR "Unsupported installer generator for target architecture")
endif()

add_custom_target(Installer
	COMMAND ${CMAKE_MAKE_PROGRAM} "package"
	DEPENDS LoaderExe
	WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
)

include(CPack)
include(GNUInstallDirs)

################## Generated files #######################################

set(TargetLibexecDir	${CMAKE_INSTALL_LIBEXECDIR}/arteranos)
set(MainExeName ${LoaderExeName})
set(MainExePath /${TargetLibexecDir}/${MainExeName})

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

if(${TARGET_SYSTEM} STREQUAL "linux-x64")
	# Linux keeps this in a dedicated libexec directory, maybe with a soft link in /bin
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

if(${TARGET_SYSTEM} STREQUAL "linux-x64")
	install(PROGRAMS
		${GeneratedFiles}/Arteranos-Server-Start
		DESTINATION ${CMAKE_INSTALL_BINDIR}
	)

	install(PROGRAMS
		${GeneratedFiles}/arteranos
		DESTINATION ${CMAKE_INSTALL_SYSCONFDIR}/init.d
	)
else()
	# set_property(INSTALL ${TargetInstallBindir}/${MainExeName}
	# 	PROPERTY CPACK_START_MENU_SHORTCUTS "Arteranos"
	# )

	# set_property(INSTALL ${TargetInstallBindir}/${MainExeName}
	# 	PROPERTY CPACK_DESKTOP_SHORTCUTS "Arteranos"
	# )
endif()
