#How To setup the sample projects

- Every sample uses the same bin directory of the following hierarchy:
    samples/bin/win32/
    samples/bin/x64/


- Copy all of the external dependencies (dlls) for win32 and x64 respectively into those directories.

- Some of the samples require boost 1.52.0. In the external/boost directory, there are two scripts: get_boost.bat and build_boost.bat. Run get_boost.bat to download boost 1.52.0 and build_boost.bat to build it.

- Copy twitchsdk_32_release.dll, twitchsdk_32_debug.dll to the win32 bin sudirectory and copy twitchsdk_64_release.dll, twitchsdl_64_debug.dll to the x64 bin subdirectory.
