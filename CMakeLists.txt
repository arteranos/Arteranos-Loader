cmake_minimum_required (VERSION 3.28)

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

################## Multiarch building ####################################

function(buildsubtarget subtarget)
	set(subbindir ${CMAKE_BINARY_DIR}/${subtarget})
	add_custom_target(${subtarget}
		COMMAND ${CMAKE_COMMAND} -E make_directory ${subbindir}
		COMMAND cd ${subbindir} 
			&& ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE} -G ${CMAKE_GENERATOR} -DTARGET_SYSTEM=${subtarget} -S${CMAKE_SOURCE_DIR} 
			&& ${CMAKE_MAKE_PROGRAM} Installer
	)

	add_dependencies(allarchs ${subtarget})
endfunction()

if(NOT DEFINED TARGET_SYSTEM)
	add_custom_target(allarchs)

	buildsubtarget("linux-x64")
#	Currently unavailable, because WiX is iffy to handle in Linux itself, and CMake doen't support WiX out-of-the-box.
#	buildsubtarget("win-x64")

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

set(CMAKE_PROJECT_HOMEPAGE_URL "https://github.com/arteranos/Arteranos-Loader")

set(CPACK_PACKAGE_VENDOR "arteranos")
set(CPACK_PACKAGE_CONTACT "arteranos")
set(CPACK_PACKAGE_DESCRIPTION "This is the Arteranos Loader, to download, update and start Arteranos.")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "Arteranos Loader")

set(CPACK_PACKAGING_INSTALL_PREFIX "/")

set(CPACK_DEBIAN_PACKAGE_SECTION "games")
set(CPACK_DEBIAN_PACKAGE_CONTROL_EXTRA ${GeneratedFiles}/postinst;${GeneratedFiles}/prerm)
set(CPACK_DEBIAN_PACKAGE_DEPENDS
	dotnet8
)

set(CPACK_WIX_UPGRADE_GUID "FB5A017C-2AFC-4C8B-BC97-47787B42B6CB")
set(CPACK_WIX_PROGRAM_MENU_FOLDER "Arteranos")
set(CPACK_WIX_TEMPLATE ${SetupDataDir}/Windows/WIX.template.in)
set(CPACK_WIX_INSTALL_SCOPE "perMachine")

set(CPACK_WIX_PRODUCT_ICON ${SetupDataDir}/Windows/Arteranos.ico)
set(CPACK_WIX_LICENSE_RTF ${SetupDataDir}/Windows/SetupLicense.rtf)
set(CPACK_WIX_UI_BANNER ${SetupDataDir}/Windows/banner.bmp)
set(CPACK_WIX_UI_DIALOG ${SetupDataDir}/Windows/Dialog.bmp)
set(CPACK_WIX_EXTRA_SOURCES 
	${SetupDataDir}/Windows/WIX.fwrules.wxs
	${SetupDataDir}/Windows/WIX.moreShortcuts.wxs
)
set(CPACK_WIX_EXTENSIONS WixFirewallExtension)


if(${TARGET_SYSTEM} STREQUAL "linux-x64")
	set(CPACK_PACKAGE_NAME "arteranos-server")
	set(CPACK_GENERATOR "DEB")

	set(PackageFile ${CPACK_PACKAGE_NAME}-${CPACK_PACKAGE_VERSION}-Linux.deb)
else()
	set(CPACK_PACKAGE_NAME "Arteranos")
	set(CPACK_GENERATOR "WIX")

	set(PackageFile ${CPACK_PACKAGE_NAME}-${CPACK_PACKAGE_VERSION}-Win64.msi)
endif()

add_custom_command(
	OUTPUT ${PackageFile}
	COMMAND ${CMAKE_MAKE_PROGRAM} "package"
	DEPENDS LoaderExe
	WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
)

add_custom_target(Installer DEPENDS ${PackageFile})

include(CPack)
include(GNUInstallDirs)

################## Generated files #######################################

set(TargetLibexecDir	${CMAKE_INSTALL_LIBEXECDIR}/arteranos)
set(MainExeName ${LoaderExeName})
set(MainExePath /${TargetLibexecDir}/${MainExeName})

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

# install(DIRECTORY
# 	${SetupDataDir}/Common/_bin/
# 	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
# 	DESTINATION ${TargetInstallBindir}
# )

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
	set_property(INSTALL ${TargetInstallBindir}/${MainExeName}
		PROPERTY CPACK_START_MENU_SHORTCUTS "Arteranos"
	)

	set_property(INSTALL ${TargetInstallBindir}/${MainExeName}
		PROPERTY CPACK_DESKTOP_SHORTCUTS "Arteranos"
	)
endif()
