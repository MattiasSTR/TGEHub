workspace "TGEHub"
    configurations { "Debug", "Release" }
    platforms { "Any CPU" }
    startproject "TGEHub"

project "TGEHub"
    kind "WindowedApp"
    language "C#"
    flags {"WPF"}
    targetdir ("TGEHub/bin/%{cfg.buildcfg}/")
    objdir ("TGEHub/obj/%{cfg.buildcfg}/")
	location "TGEHub"
	dotnetframework "net8.0-windows"
	clr "On"

    files { "**.cs", "**.xaml.cs", "TGEHub/TGAIcon.ico"}

    filter "platforms:AnyCpu"
        architecture "x86_64"

    filter "configurations:Debug"
        defines { "DEBUG" }
        symbols "On"

    filter "configurations:Release"
        defines { "NDEBUG" }
        optimize "On"
		
		