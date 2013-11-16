//////////////////////////////////////////////////////////////////////////////
// This module contains the platform-independent code demonstrating how to 
// set up and use the Twitch SDK for basic streaming.
//////////////////////////////////////////////////////////////////////////////

#include "twitchsdk.h"
#include "twitchwebcam.h"
#include "webcam.h"
#include <vector>
#include <algorithm>

extern std::vector< std::shared_ptr<DeviceCaptureData> > gCaptureDevices;


bool gSdkInitialized = false;			// Whether or not TTV_Init has been called.

std::string gClientId = "";				// The cached client id.
std::string gClientSecret = "";			// The cached client secret.

// Forward declarations
void ReportError(const char* format, ...);


#pragma region Callbacks

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


#pragma endregion


void InitializeWebcamSystem(const std::string& clientId, const std::string& clientSecret, const std::wstring& dllLoadPath)
{
	gClientId = clientId;
	gClientSecret = clientSecret;

	// Setup the memory allocation callbacks needed by the SDK
	TTV_MemCallbacks memCallbacks;
	memCallbacks.size = sizeof(TTV_MemCallbacks);
	memCallbacks.allocCallback = AllocCallback;
	memCallbacks.freeCallback = FreeCallback;

	// Initialize the SDK
	TTV_ErrorCode ret = TTV_Init(&memCallbacks, clientId.c_str(), dllLoadPath.c_str());
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while initializing the Twitch SDK: %s\n", err);
		return;
	}

	// Initialize the webcam system
	TTV_WebCamCallbacks webcamCallbacks;
	webcamCallbacks.deviceChangeCallback = &WebCamDeviceChangeCallback;
	webcamCallbacks.deviceChangeUserData = nullptr;

	ret = TTV_WebCam_Init(&webcamCallbacks, WebCamInitializationCallback, nullptr);
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while initializing the webcam system: %s\n", err);
		return;
	}

	// SDK now initialized
	gSdkInitialized = true;
}


/**
 * Allows the callback functions to be called on the current thread.  This should be called periodically.
 */
void FlushWebcamEvents()
{
	TTV_WebCam_FlushEvents();
}


/**
 * Cleans up the SDK after being initialized.
 */
void ShutdownWebcamSystem()
{
	if (!gSdkInitialized)
	{
		return;
	}

	gSdkInitialized = false;

	TTV_ErrorCode ret = TTV_WebCam_Shutdown(WebCamShutdownCallback, nullptr);
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while shutting down the webcam system: %s\n", err);
		return;
	}

	ret = TTV_Shutdown();
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while shutting down the Twitch SDK: %s\n", err);
		return;
	}
}


std::vector< std::shared_ptr<DeviceCaptureData> >::iterator FindDeviceData(int deviceIndex)
{
	for (auto iter = gCaptureDevices.begin(); iter != gCaptureDevices.end(); ++iter)
	{
		std::shared_ptr<DeviceCaptureData> data = *iter;
		if (data->device->deviceIndex == deviceIndex)
		{
			return iter;
		}
	}

	return gCaptureDevices.end();
}


std::vector< std::shared_ptr<DeviceCaptureData> >::iterator FindDeviceData(const utf8char* uniqueId)
{
	for (auto iter = gCaptureDevices.begin(); iter != gCaptureDevices.end(); ++iter)
	{
		std::shared_ptr<DeviceCaptureData> data = *iter;
		if ( strcmp(data->device->uniqueId, uniqueId) == 0 )
		{
			return iter;
		}
	}

	return gCaptureDevices.end();
}


TTV_WebCamDevice* CopyDevice(const TTV_WebCamDevice* device)
{
	TTV_WebCamDevice* copy = new TTV_WebCamDevice();
	*copy = *device;
	copy->capabilityList.list = new TTV_WebCamDeviceCapability[device->capabilityList.count];
	memcpy(copy->capabilityList.list, device->capabilityList.list, sizeof(TTV_WebCamDeviceCapability)*device->capabilityList.count);

	return copy;
}


void FreeDevice(TTV_WebCamDevice* device)
{
	delete [] device->capabilityList.list;
	delete device;
}
