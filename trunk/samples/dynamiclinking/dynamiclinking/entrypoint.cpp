#include <Windows.h>
#include "twitchsdk.h"
#include <cassert>




/**
 * The callback that will be called by the SDK to allocate memory.
 */
void* AllocCallback(size_t size, size_t alignment)
{
	return _aligned_malloc(size, alignment);
}

/**
 * The callback that will be called by the SDK to free memory.
 */
void FreeCallback(void* ptr)
{
	_aligned_free(ptr);
}




int main()
{
	// Load the DLL
	#if defined(_M_X64) // A x64 build
		#if defined(_DEBUG)
			wchar_t* dllFileName = L"..\\..\\bin\\x64\\twitchsdk_64_debug.dll";
		#else
			wchar_t* dllFileName = L"..\\..\\bin\\x64\\twitchsdk_64_release.dll";
		#endif
	#else
		#if defined(_DEBUG)
			wchar_t* dllFileName = L"..\\..\\bin\\Win32\\twitchsdk_32_debug.dll";
		#else
			wchar_t* dllFileName = L"..\\..\\bin\\Win32\\twitchsdk_32_release.dll";
		#endif
	#endif
	
	HMODULE twitchDLL = LoadLibrary(dllFileName);
	if (twitchDLL == NULL)
	{
		DWORD errorCode = GetLastError();
		assert(false);
		exit(-1);
	}
	



	// Get the function pointers into that DLL

	FARPROC farprocTTV_Init = GetProcAddress(twitchDLL, "TTV_Init");
	if (farprocTTV_Init == NULL)
	{
		DWORD errorCode = GetLastError();
		assert(false);
		exit(-1);
	}

	FARPROC farprocTTV_Shutdown = GetProcAddress(twitchDLL, "TTV_Shutdown");
	if (farprocTTV_Shutdown == NULL)
	{
		DWORD errorCode = GetLastError();
		assert(false);
		exit(-1);
	}




	// Those functions have these forward declarations:

	//TTVSDK_API TTV_ErrorCode TTV_Init(const TTV_MemCallbacks* memCallbacks, 
	//                                  const char* clientID,	
	//                                  TTV_VideoEncoder vidEncoder,
	//                                  const wchar_t* dllPath);
	//TTVSDK_API TTV_ErrorCode TTV_Shutdown();

	// Lets make function pointer typedefs to match

	typedef TTV_ErrorCode (*TTV_InitFunctionPointer)(const TTV_MemCallbacks* memCallbacks, 
	                                                 const char* clientID,	                                                 
	                                                 TTV_VideoEncoder vidEncoder,
	                                                 const wchar_t* dllPath);
	typedef TTV_ErrorCode (*TTV_ShutdownFunctionPointer)(void);




	// Now lets convert the previous FARPROCs into these more useful function pointers

	TTV_InitFunctionPointer ourTTV_Init = (TTV_InitFunctionPointer)(farprocTTV_Init);
	TTV_ShutdownFunctionPointer ourTTV_Shutdown = (TTV_ShutdownFunctionPointer)(farprocTTV_Shutdown);




	// Now we can call those functions

	TTV_MemCallbacks memCallbacks;
	memCallbacks.size = sizeof(TTV_MemCallbacks);
	memCallbacks.allocCallback = AllocCallback;
	memCallbacks.freeCallback = FreeCallback;

	// The intel encoder is used on Windows
	TTV_VideoEncoder vidEncoder = TTV_VID_ENC_INTEL;

	// Initialize the SDK
#error Don't forget to fill in the strings below
	TTV_ErrorCode ret = ourTTV_Init(&memCallbacks, "client ID here", vidEncoder, L".\\");
	if ( TTV_FAILED(ret) )
	{
		assert(false);
		exit(-1);
	}

	ret = ourTTV_Shutdown();
	if ( TTV_FAILED(ret) )
	{
		assert(false);
		exit(-1);
	}

	return 0;
}
