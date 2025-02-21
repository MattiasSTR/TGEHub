project "CommonUtilities"
location (dirs.projectfiles)
language "C++"
cppdialect "C++17"
targetdir (dirs.lib)
targetname("%{prj.name}_%{cfg.buildcfg}")
objdir ("%{dirs.temp}/%{prj.name}/%{cfg.buildcfg}")
files {
"CommonUtilities/**.h",
"CommonUtilities/**.hpp",
"CommonUtilities/**.cpp",
}
libdirs { dirs.lib, dirs.dependencies }
verify_or_create_settings("Game")
filter "configurations:Debug"
defines {"_DEBUG"}
runtime "Debug"
symbols "on"
filter "configurations:Release"
defines "_RELEASE"
runtime "Release"
optimize "on"
filter "configurations:Retail"
defines "_RETAIL"
runtime "Release"
optimize "on"
filter "system:windows"
kind "StaticLib"
staticruntime "off"
symbols "On"
systemversion "latest"
warnings "Extra"
sdlchecks "true"
flags {
"FatalCompileWarnings",
"MultiProcessorCompile"
}
defines {
"WIN32",
"_CRT_SECURE_NO_WARNINGS",
"_LIB",
"_WIN32_WINNT=0x0601",
"TGE_SYSTEM_WINDOWS"
}