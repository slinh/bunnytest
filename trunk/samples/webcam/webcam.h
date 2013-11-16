//////////////////////////////////////////////////////////////////////////////
// This file contains the interface to managing the capture devices.
//////////////////////////////////////////////////////////////////////////////

#ifndef WEBCAM_H
#define WEBCAM_H

#include "twitchsdk.h"
#include "twitchwebcam.h"
#include <string>
#include <vector>
#include <memory>


struct ScreenRegion
{
	int left;
	int top;
	int right;
	int bottom;
};


/**
 * Information used to render the output from the given device.
 */
struct DeviceCaptureData
{
	TTV_WebCamDevice* device;
	int capabilityIndex;
	bool flipped;
	bool capturing;
};


// These callbacks will be implemented by the platform-specific code
void WebCamInitializationCallback(TTV_ErrorCode error, void* userdata);
void WebCamShutdownCallback(TTV_ErrorCode error, void* userdata);
void WebCamDeviceStatusCallback(const TTV_WebCamDevice* device, TTV_ErrorCode error, void* userdata);
void WebCamDeviceChangeCallback(TTV_WebCamDeviceChange change, const TTV_WebCamDevice* device, TTV_ErrorCode error, void* userdata);


void InitializeWebcamSystem(const std::string& clientId, const std::string& clientSecret, const std::wstring& dllLoadPath);
void FlushWebcamEvents();
void ShutdownWebcamSystem();

std::vector< std::shared_ptr<DeviceCaptureData> >::iterator FindDeviceData(int deviceIndex);
std::vector< std::shared_ptr<DeviceCaptureData> >::iterator FindDeviceData(const utf8char* uniqueId);
TTV_WebCamDevice* CopyDevice(const TTV_WebCamDevice* device);
void FreeDevice(TTV_WebCamDevice* device);


#endif
