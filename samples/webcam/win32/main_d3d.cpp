//////////////////////////////////////////////////////////////////////////////
// This module contains the main entry point and code specific to Direct3D9.
//////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "resource.h"
#include "../webcam.h"
#include "twitchsdk.h"
#include "twitchwebcam.h"

#include <vector>
#include <d3d9.h>
#include <d3dx9math.h>
#include <assert.h>


#define WINDOW_STYLE  (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX)
#define SAFE_RELEASE(x) if (x) { x->Release(); x = nullptr; } 

#define SCREEN_QUAD_VERTEX_FVF (D3DFVF_XYZ | D3DFVF_TEX2)

struct ScreenQuadVertex
{
	D3DVECTOR v; 
	FLOAT tx, ty; 
};


struct D3DDeviceCaptureData : DeviceCaptureData
{
	IDirect3DTexture9* texture[2];
	uint8_t* conversionBuffer;
	int captureIndex;
	int renderIndex;
};


namespace SampleState
{
	enum Enum
	{
		Uninitialized,
		Initialized,
		Shutdown
	};
}


#pragma region Global Variables

HINSTANCE gInstanceHandle = 0;								// The current application instance
HWND gWindowHandle = 0;										// The main window handle.
TCHAR gWindowTitle[128];									// The title bar text
TCHAR gWindowClass[128];									// the main window class name

IDirect3D9* gDirect3D = nullptr;							// The Direct3D instance.
IDirect3DDevice9* gGraphicsDevice = nullptr;				// The graphics device.

D3DXMATRIX gScreenProjectionMatrix;							// The orthographic screen projection matrix.
D3DXMATRIX gOrthoViewMatrix;								// The screen tranlsation matrix.

IDirect3DVertexBuffer9* gScreenQuadVertexBuffer = nullptr;	// The vertex buffer containing the screen quad.

unsigned int gWindowWidth = 960;								//!< The starting window width.
unsigned int gWindowHeight = 720;								//!< The starting window height.
SampleState::Enum gSampleState = SampleState::Uninitialized;	//!< The state of the webcam system.

std::vector< std::shared_ptr<DeviceCaptureData> > gCaptureDevices;			// The devices which are currently reported by the system.
std::vector<ScreenRegion> gScreenRegions;		//!< The ordered list of region in which to render the captured images.
int gNumCapturing = 0;							//!< The number of devices currently capturing
int gActiveDeviceSlot = -1;						//!< The device slot in which to apply operations.

#pragma endregion


#pragma region Forward Declarations

ATOM				RegisterWindowClass(HINSTANCE hInstance);
BOOL				InitInstance(HINSTANCE, int);
LRESULT CALLBACK	WndProc(HWND, UINT, WPARAM, LPARAM);
void HandleDeviceStarted(int deviceIndex, const TTV_WebCamDeviceCapability* capability);
void HandleDeviceStopped(int deviceIndex);
void StartDevice(int deviceIndex, unsigned int capabilityIndex);
void StopDevice(int deviceIndex);
void SelectFirstValidCapability(int deviceSlot);

#pragma endregion


#pragma region Callbacks

void WebCamInitializationCallback(TTV_ErrorCode error, void* userdata)
{
	gSampleState = SampleState::Initialized;

	if (TTV_SUCCEEDED(error))
	{
		printf("Webcam system initialized\n");
	}
	else
	{
		const char* e = TTV_ErrorToString(error);
		printf("Error initializing webcam system: %s\n", e);
	}
}


void WebCamShutdownCallback(TTV_ErrorCode error, void* userdata)
{
	gSampleState = SampleState::Shutdown;

	if (TTV_SUCCEEDED(error))
	{
		printf("Webcam system shutdown\n");
	}
	else
	{
		const char* e = TTV_ErrorToString(error);
		printf("Error shutting down webcam system: %s\n", e);
	}
}


void WebCamDeviceStatusCallback(const TTV_WebCamDevice* device, const TTV_WebCamDeviceCapability* capability, TTV_ErrorCode error, void* userdata)
{
	const char* st = nullptr;
	switch (device->status)
	{
	case TTV_WEBCAM_DEVICE_STARTED:
		st = "TTV_WEBCAM_DEVICE_STARTED";
		break;
	case TTV_WEBCAM_DEVICE_STOPPED:
		st = "TTV_WEBCAM_DEVICE_STOPPED";
		break;
	case TTV_WEBCAM_DEVICE_UNINITIALIZED:
	default:
		st = "TTV_WEBCAM_DEVICE_UNINITIALIZED";
		assert(false);
		break;
	}

	if (TTV_SUCCEEDED(error))
	{
		switch (device->status)
		{
			case TTV_WEBCAM_DEVICE_STARTED:
			{
				HandleDeviceStarted(device->deviceIndex, capability);
				break;
			}
			case TTV_WEBCAM_DEVICE_STOPPED:
			{
				HandleDeviceStopped(device->deviceIndex);
				break;
			}
		}		

		printf("Device status: %s - %s\n", device->uniqueId, st);
	}
	else
	{
		const char* e = TTV_ErrorToString(error);
		printf("Error with device status: %s %s - %s\n", e, device->uniqueId, st);
	}
}


void WebCamDeviceChangeCallback(TTV_WebCamDeviceChange change, const TTV_WebCamDevice* device, TTV_ErrorCode error, void* userdata)
{
	const char* ch = change == TTV_WEBCAM_DEVICE_FOUND ? "TTV_WEBCAM_DEVICE_FOUND" : "TTV_WEBCAM_DEVICE_LOST";

	if (TTV_SUCCEEDED(error))
	{
		switch (change)
		{
			// add the device
			case TTV_WEBCAM_DEVICE_FOUND:
			{
				std::shared_ptr<DeviceCaptureData> data( new D3DDeviceCaptureData());
				data->capturing = false;
				data->flipped = false;
				data->device = CopyDevice(device);
				gCaptureDevices.push_back(data);

				static_cast<D3DDeviceCaptureData*>(data.get())->conversionBuffer = nullptr;

				if (gActiveDeviceSlot < 0)
				{
					gActiveDeviceSlot = 0;
				}

				data->capabilityIndex = -1;
				SelectFirstValidCapability(static_cast<unsigned int>(gCaptureDevices.size())-1);

				break;
			}
			// remove the device
			case TTV_WEBCAM_DEVICE_LOST:
			{
				auto iter = FindDeviceData(device->uniqueId);
				auto data = *iter;

				// stop the capturing
				if (data->capturing)
				{
					HandleDeviceStopped(data->device->deviceIndex);
				}

				FreeDevice(data->device);
				data->device = nullptr;
				gCaptureDevices.erase(iter);

				if (gActiveDeviceSlot >= (int)gCaptureDevices.size())
				{
					gActiveDeviceSlot = (int)gCaptureDevices.size()-1;
				}

				break;
			}
		}

		printf("Device change: %s - %s\n", ch, device->uniqueId);
	}
	else
	{
		const char* e = TTV_ErrorToString(error);
		printf("Error in Device change: %s %s - %s\n", e, ch, device->uniqueId);
	}
}

#pragma endregion


TTV_WebcamFormat sSupportedFormats[] = 
{
	TTV_WEBCAM_FORMAT_ARGB32
};


void RestoreCaptureState(std::shared_ptr<DeviceCaptureData> data, int capabilityIndex)
{
	if (data->capturing)
	{
		// The start follows the stop for convenience in the sample but does not necessarily represent the way you should do this
		// in your game.  You should likely have a Stop button which stops the broadcast, then enable a dropdown for the resolution.
		// The Start button should be pressed by the user to restart the broadcast.  You should be enabling the resolution dropdown 
		// in the callback to WebCamDeviceChangeCallback which will indicate when the device starts or stops.
		// However, the commands are processed in order so StartDevice won't be serviced until StopDevice has finished being processed.
		StopDevice(data->device->deviceIndex);
		StartDevice(data->device->deviceIndex, capabilityIndex);
	}
}


void SelectFirstValidCapability(int deviceSlot)
{
	auto data = gCaptureDevices[deviceSlot];
	auto device = data->device;

	bool found = false;
	for (size_t i=0; i<device->capabilityList.count; ++i)
	{
		for (size_t j=0; j<sizeof(sSupportedFormats)/sizeof(sSupportedFormats[0]); ++j)
		{
			if (device->capabilityList.list[i].format == sSupportedFormats[j])
			{
				data->capabilityIndex = device->capabilityList.list[i].capabilityIndex;
				found = true;
				break;
			}
		}

		if (found)
		{
			break;
		}
	}

	// no valid format which shouldn't happen in practice
	if (!found)
	{
		assert(false);
		data->capabilityIndex = -1;
	}

	RestoreCaptureState(data, data->capabilityIndex);
}


void SelectNextValidCapability(int deviceSlot)
{
	auto data = gCaptureDevices[deviceSlot];
	auto device = data->device;

	int i=0;

	// find the current capability
	while (data->capabilityIndex != data->device->capabilityList.list[i].capabilityIndex)
	{
		++i;
	}

	// find the next one, wrapping if needed
	bool found = false;
	while (!found)
	{
		++i;
		i = i % data->device->capabilityList.count;

		for (size_t j=0; j<sizeof(sSupportedFormats)/sizeof(sSupportedFormats[0]); ++j)
		{
			if (device->capabilityList.list[i].format == sSupportedFormats[j])
			{
				data->capabilityIndex = device->capabilityList.list[i].capabilityIndex;
				found = true;
				break;
			}
		}

		if (found)
		{
			break;
		}
	}

	RestoreCaptureState(data, data->capabilityIndex);
}


void SelectPreviousValidCapability(int deviceSlot)
{
	auto data = gCaptureDevices[deviceSlot];
	auto device = data->device;

	int i=0;

	// find the current capability
	while (data->capabilityIndex != data->device->capabilityList.list[i].capabilityIndex)
	{
		++i;
	}

	// find the next one, wrapping if needed
	bool found = false;
	while (!found)
	{
		i += data->device->capabilityList.count-1;
		i = i % data->device->capabilityList.count;

		for (size_t j=0; j<sizeof(sSupportedFormats)/sizeof(sSupportedFormats[0]); ++j)
		{
			if (device->capabilityList.list[i].format == sSupportedFormats[j])
			{
				data->capabilityIndex = device->capabilityList.list[i].capabilityIndex;
				found = true;
				break;
			}
		}

		if (found)
		{
			break;
		}
	}

	RestoreCaptureState(data, data->capabilityIndex);
}


void HandleDeviceStarted(int deviceIndex, const TTV_WebCamDeviceCapability* capability)
{
	auto iter = FindDeviceData(deviceIndex);
	if (iter == gCaptureDevices.end())
	{
		// TODO: error
		return;
	}

	auto data = *iter;
	assert(!data->capturing);

	D3DDeviceCaptureData* d3ddata = static_cast<D3DDeviceCaptureData*>(data.get());

	D3DFORMAT d3dFormat = D3DFMT_A8R8G8B8;

	d3ddata->capabilityIndex = capability->capabilityIndex;
	data->flipped = capability->isTopToBottom;
	d3ddata->captureIndex = 0;
	d3ddata->renderIndex = -1;

	// setup the capture textures
	for (int i=0; i<std::extent<decltype(d3ddata->texture),0>::value; ++i)
	{
		if ( FAILED(gGraphicsDevice->CreateTexture(capability->resolution.width, capability->resolution.height, 1, D3DUSAGE_DYNAMIC, d3dFormat, D3DPOOL_DEFAULT, &d3ddata->texture[i], nullptr)) )
		{
			ReportError("Error creating texture");
			return;
		}
	}

	assert(capability->format == TTV_WEBCAM_FORMAT_ARGB32);

	data->capturing = true;
	gNumCapturing++;
}


void HandleDeviceStopped(int deviceIndex)
{
	auto iter = FindDeviceData(deviceIndex);
	if (iter == gCaptureDevices.end())
	{
		// TODO: error
		return;
	}

	auto data = *iter;
	if (!data->capturing)
	{
		return;
	}
				
	D3DDeviceCaptureData* d3ddata = static_cast<D3DDeviceCaptureData*>(data.get());

	if (d3ddata->conversionBuffer != nullptr)
	{
		delete [] d3ddata->conversionBuffer;
		d3ddata->conversionBuffer = nullptr;
	}

	data->capturing = false;
	gNumCapturing--;

	// clean up the textures
	for (int i=0; i<std::extent<decltype(d3ddata->texture),0>::value; ++i)
	{
		SAFE_RELEASE(d3ddata->texture[i]);
	}
}


void StartDevice(int deviceIndex, unsigned int capabilityIndex)
{
	auto iter = FindDeviceData(deviceIndex);
	if (iter == gCaptureDevices.end())
	{
		// TODO: error
		return;
	}

	auto data = *iter;

	TTV_ErrorCode err = TTV_WebCam_Start(deviceIndex, capabilityIndex, &WebCamDeviceStatusCallback, nullptr);
	if (TTV_FAILED(err))
	{
		const char* msg = TTV_ErrorToString(err);
		ReportError("Error starting device: %s", msg);
	}
}


void StopDevice(int deviceIndex)
{
	auto iter = FindDeviceData(deviceIndex);
	if (iter == gCaptureDevices.end())
	{
		// TODO: error
		return;
	}

	auto data = *iter;

	TTV_ErrorCode err = TTV_WebCam_Stop(deviceIndex, &WebCamDeviceStatusCallback, nullptr);
	if (TTV_FAILED(err))
	{
		const char* msg = TTV_ErrorToString(err);
		ReportError("Error stopping device: %s", msg);
	}
}


void ToggleCapture(int slot)
{
	auto data = gCaptureDevices[slot];
	auto device = data->device;

	if (device == nullptr)
	{
		return;
	}

	if (data->capturing)
	{
		StopDevice(device->deviceIndex);
	}
	else
	{
		StartDevice(device->deviceIndex, data->capabilityIndex);
	}
}


/**
 * Prints the error to the console and shows a message box.
 */
void ReportError(const char* format, ...)
{
	char buffer[256];
	va_list args;
	va_start(args, format);
	vsprintf_s(buffer, sizeof(buffer), format, args);
	perror(buffer);
	va_end(args);
 
	OutputDebugStringA(buffer);
	printf("%s", buffer);
	MessageBoxA(gWindowHandle, buffer, "Error", MB_OK);
}


/**
 * Determines the directory that the intel encoder DLL is located.
 */
std::wstring GetIntelDllPath()
{
	return std::wstring(L".\\");
}


void DetermineWindowSize(unsigned int& width, unsigned int& height)
{
	// Calculate the size of the window which will guarantee the client size we want
	RECT rect;
	rect.top = 0;
	rect.left = 0;
	rect.right = gWindowWidth;
	rect.bottom = gWindowHeight;
	AdjustWindowRect(&rect, WINDOW_STYLE, false);
	width = rect.right - rect.left;
	height = rect.bottom - rect.top;
}


/**
 * Initializes the rendering using the appropriate rendering method.
 */
bool InitializeRendering()
{
	// Set the viewport
	D3DVIEWPORT9 vp;
	vp.X = 0;
	vp.Y = 0;
	vp.MinZ = 0.0f;
	vp.MaxZ = 1.0f;
	vp.Width = gWindowWidth;
	vp.Height = gWindowHeight;
	gGraphicsDevice->SetViewport(&vp);

	// Disable lighting
	gGraphicsDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	// create the screen quad
	if ( FAILED(gGraphicsDevice->CreateVertexBuffer(4*sizeof(ScreenQuadVertex), 0, SCREEN_QUAD_VERTEX_FVF, D3DPOOL_MANAGED, &gScreenQuadVertexBuffer, nullptr)) )
	{
		ReportError("Error creating vertex buffer");
		return false;
	}

	ScreenQuadVertex* pScreenQuad = nullptr;
	if ( FAILED(gScreenQuadVertexBuffer->Lock(0, 0, reinterpret_cast<void**>(&pScreenQuad), 0)) )
	{
		ReportError("Vertex buffer lock failed");
		return false;
	}

	// we will scale and translate the quad to place it on the screen
	pScreenQuad[0].v.x = 0;
	pScreenQuad[0].v.y = 0;
	pScreenQuad[0].v.z = 1;
	pScreenQuad[0].tx = 0;
	pScreenQuad[0].ty = 0;

	pScreenQuad[1].v.x = 0;
	pScreenQuad[1].v.y = 1;
	pScreenQuad[1].v.z = 1;
	pScreenQuad[1].tx = 0;
	pScreenQuad[1].ty = 1;

	pScreenQuad[2].v.x = 1;
	pScreenQuad[2].v.y = 0;
	pScreenQuad[2].v.z = 1;
	pScreenQuad[2].tx = 1;
	pScreenQuad[2].ty = 0;

	pScreenQuad[3].v.x = 1;
	pScreenQuad[3].v.y = 1;
	pScreenQuad[3].v.z = 1;
	pScreenQuad[3].tx = 1;
	pScreenQuad[3].ty = 1;

	gScreenQuadVertexBuffer->Unlock();

	// Setup the ortho projection for the render to screen
	D3DXMatrixOrthoOffCenterLH(&gScreenProjectionMatrix, 0, (FLOAT)gWindowWidth, 0, (FLOAT)gWindowHeight, 1, 100);

	// Setup the screen translation
	D3DXMatrixIdentity(&gOrthoViewMatrix);

	return true;
}


void ShutdownRendering()
{
	SAFE_RELEASE(gScreenQuadVertexBuffer);
	SAFE_RELEASE(gGraphicsDevice);
	SAFE_RELEASE(gDirect3D)
}


/**
 * See if new frames are available for capturing devices.
 */
void UpdateWebcamFrames()
{
	for (size_t i=0; i<gCaptureDevices.size(); ++i)
	{
		auto data = gCaptureDevices[i];

		if (!data->capturing)
		{
			continue;
		}

		auto device = data->device;

		bool available = false;
		TTV_WebCam_IsFrameAvailable(device->deviceIndex, &available);

		if (!available)
		{
			continue;
		}

		// find the capability
		int listIndex = 0;
		while (device->capabilityList.list[listIndex].capabilityIndex != data->capabilityIndex)
		{
			listIndex++;
		}

		const TTV_WebCamDeviceCapability& capability = device->capabilityList.list[listIndex];

		D3DDeviceCaptureData* d3ddata = static_cast<D3DDeviceCaptureData*>(data.get());

		// Update the texture data
		D3DLOCKED_RECT rect;
	
		if ( FAILED(d3ddata->texture[d3ddata->captureIndex]->LockRect(0, &rect, nullptr, D3DLOCK_DISCARD)) )
		{
			ReportError("Error locking texture");
			continue;
		}

		assert(capability.format == TTV_WEBCAM_FORMAT_ARGB32);

		// The capability format matches the format of the texture
		if (capability.format == TTV_WEBCAM_FORMAT_ARGB32)
		{
			// grab the frame directly into the texture memory
			TTV_WebCam_GetFrame(device->deviceIndex, rect.pBits, (unsigned int)rect.Pitch);
		}

		d3ddata->texture[d3ddata->captureIndex]->UnlockRect(0);

		// swap the textures
		d3ddata->renderIndex = d3ddata->captureIndex;
		d3ddata->captureIndex = (~d3ddata->captureIndex) & 0x1; // 0 -> 1, 1 -> 0
	}
}


/**
 * Render the image from the camera to the screen.
 */
void RenderScene()
{
	// Disable depth buffering
	gGraphicsDevice->SetRenderState(D3DRS_ZENABLE, FALSE);
	gGraphicsDevice->SetRenderState(D3DRS_ZWRITEENABLE, FALSE);
	gGraphicsDevice->SetRenderState(D3DRS_ALPHATESTENABLE, FALSE);
	gGraphicsDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
	gGraphicsDevice->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
	gGraphicsDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

	// Enable texture coordinate transformations
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_TEXTURETRANSFORMFLAGS, D3DTTFF_COUNT2);

	// View transformation
	gGraphicsDevice->SetTransform(D3DTS_VIEW, &gOrthoViewMatrix);
	
	// Perspective transformation
	gGraphicsDevice->SetTransform(D3DTS_PROJECTION, &gScreenProjectionMatrix);

	// Clear the back buffer
	gGraphicsDevice->Clear(0, nullptr, D3DCLEAR_TARGET, D3DCOLOR_ARGB(255, 0, 0, 0), 1.0f, 0);

	// Render the texture to the screen
	gGraphicsDevice->BeginScene();
	{
		gGraphicsDevice->SetFVF(SCREEN_QUAD_VERTEX_FVF);
		gGraphicsDevice->SetStreamSource(0, gScreenQuadVertexBuffer, 0, sizeof(ScreenQuadVertex));

		for (size_t i=0; i<gCaptureDevices.size(); ++i)
		{
			auto data = gCaptureDevices[i];
			D3DDeviceCaptureData* d3ddata = static_cast<D3DDeviceCaptureData*>(data.get());
			
			if (!data->capturing || d3ddata->renderIndex == -1)
			{
				continue;
			}

			// Setup the region to render to
			D3DXMATRIX translate;
			D3DXMATRIX scale;

			// If only one device is active then fill the entire window
			if (gNumCapturing == 1)
			{
				D3DXMatrixTranslation(&translate, 0, 0, 20);
				D3DXMatrixScaling(&scale, (FLOAT)gWindowWidth, (FLOAT)gWindowHeight, 1);
			}
			// Use the assigned region
			else
			{
				ScreenRegion region = gScreenRegions[i];
				D3DXMatrixTranslation(&translate, (FLOAT)region.left, (FLOAT)region.bottom, 20);
				D3DXMatrixScaling(&scale, (FLOAT)(region.right-region.left), (FLOAT)(region.top-region.bottom), 1);
			}

			D3DXMATRIX tx = scale * translate;
			gGraphicsDevice->SetTransform(D3DTS_VIEW, &tx);

			// Flip the image if upside-down
			if (data->flipped)
			{
				D3DXMATRIX tex;
				D3DXMatrixScaling(&tex, 1, -1, 1);
				gGraphicsDevice->SetTransform(D3DTS_TEXTURE0, &tex);
			}

			// Set the texture and render the quad
			gGraphicsDevice->SetTexture(0, d3ddata->texture[d3ddata->renderIndex]);
			gGraphicsDevice->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 2);

			// Clear the flip
			if (data->flipped)
			{
				D3DXMATRIX tex;
				D3DXMatrixIdentity(&tex);
				gGraphicsDevice->SetTransform(D3DTS_TEXTURE0, &tex);
			}
		}

		// Clear device state
		gGraphicsDevice->SetStreamSource(0, nullptr, 0, 0);
		gGraphicsDevice->SetTexture(0, nullptr);
	}
	gGraphicsDevice->EndScene();

	gGraphicsDevice->Present(nullptr, nullptr, nullptr, nullptr);
}


void InitializeRegions()
{
	gScreenRegions.clear();

	ScreenRegion screenRegion;

	screenRegion.left = 0;
	screenRegion.top = gWindowHeight;
	screenRegion.bottom = (gWindowHeight/2);
	screenRegion.right = (gWindowWidth/2);
	gScreenRegions.push_back(screenRegion);

	screenRegion.left = (gWindowWidth/2);
	screenRegion.top = gWindowHeight;
	screenRegion.bottom = (gWindowHeight/2);
	screenRegion.right = gWindowWidth;
	gScreenRegions.push_back(screenRegion);

	screenRegion.left = 0;
	screenRegion.top = (gWindowHeight/2);
	screenRegion.bottom = 0;
	screenRegion.right = (gWindowWidth/2);
	gScreenRegions.push_back(screenRegion);

	screenRegion.left = (gWindowWidth/2);
	screenRegion.top = (gWindowHeight/2);
	screenRegion.bottom = 0;
	screenRegion.right = gWindowWidth;
	gScreenRegions.push_back(screenRegion);
}


/**
 * The main entry point for the application.
 */
int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
{
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

	// Initialize global strings
	const int maxLoadString = 100;
	LoadString(hInstance, IDS_APP_TITLE, gWindowTitle, sizeof(gWindowTitle));
	LoadString(hInstance, IDC_WEBCAM, gWindowClass, sizeof(gWindowClass));

	// Register the window class
	RegisterWindowClass(hInstance);

	// Perform application initialization:
	if ( !InitInstance(hInstance, nCmdShow) )
	{
		return FALSE;
	}

	InitializeRendering();

	InitializeRegions();

	InitializeWebcamSystem("<clientId>", "<clientSecret>", GetIntelDllPath());

	// Main message loop
	MSG msg;
	while (true)
	{
		// Check to see if any messages are waiting in the queue
		while (PeekMessage(&msg, nullptr, 0, 0, PM_REMOVE))
		{
			// Process window messages
			TranslateMessage(&msg);
			DispatchMessage(&msg);

			// Received a quit message
			if (msg.message == WM_QUIT)
			{
				break;
			}
		}

		// Received a quit message so exit the app
		if (msg.message == WM_QUIT)
		{
			break;
		}

		// The SDK may generate events that need to be handled by the main thread so we should handle them
		FlushWebcamEvents();

		// Grab the latest frame from capturing webcams if they're available
		UpdateWebcamFrames();

		// Draw the scene
		RenderScene();

		// Update the window title
		std::string title = "Twitch Direct3D Webcam Sample - ";
		
		switch (gSampleState)
		{
		case SampleState::Uninitialized:
			title += "Uninitialized ";
			break;
		case SampleState::Initialized:
			title += "Initialized ";
			break;
		case SampleState::Shutdown:
			title += "Shutdown ";
			break;
		}

		// Post info about each connected device
		for (size_t i=0; i<gCaptureDevices.size(); ++i)
		{
			char buffer[256];
			auto data = gCaptureDevices[i];
			auto device = data->device;

			const TTV_WebCamDeviceCapability* capability = nullptr;

			if (data->capabilityIndex >= 0)
			{
				int index = 0;
				while (device->capabilityList.list[index].capabilityIndex != data->capabilityIndex)
				{
					index++;
				}
				capability = &data->device->capabilityList.list[index];

				sprintf_s(buffer, sizeof(buffer), "%s%u: [%u %u %s %ux%u] %s ", 
					i == gActiveDeviceSlot ? "*" : "", 
					device->deviceIndex, 
					data->capabilityIndex, 
					capability->format, 
					capability->isNative ? "Native" : "Converted", 
					capability->resolution.width, 
					capability->resolution.height, 
					gCaptureDevices[i]->capturing ? "Live" : "Ready");
			}
			else
			{
				sprintf_s(buffer, sizeof(buffer), "%s%u: %s ", 
					i == gActiveDeviceSlot ? "*" : "", 
					device->deviceIndex, 
					"No capabilities");
			}

			title += buffer;
		}

		SetWindowTextA(gWindowHandle, title.c_str());
	}

	ShutdownWebcamSystem();

	// Shutdown the app
	ShutdownRendering();

	return (int)msg.wParam;
}


/**
 * Register the window class.
 */
ATOM RegisterWindowClass(HINSTANCE hInstance)
{
	WNDCLASSEX wcex;

	wcex.cbSize = sizeof(WNDCLASSEX);

	wcex.style			= CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc	= WndProc;
	wcex.cbClsExtra		= 0;
	wcex.cbWndExtra		= 0;
	wcex.hInstance		= hInstance;
	wcex.hIcon			= LoadIcon(hInstance, MAKEINTRESOURCE(IDI_WEBCAM));
	wcex.hCursor		= LoadCursor(nullptr, IDC_ARROW);
	wcex.hbrBackground	= (HBRUSH)(COLOR_WINDOW+1);
	wcex.lpszMenuName	= MAKEINTRESOURCE(IDC_WEBCAM);
	wcex.lpszClassName	= gWindowClass;
	wcex.hIconSm		= LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_SMALL));

	return RegisterClassEx(&wcex);
}


/**
 * Create the window and initialize the graphics device.
 */
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
	gInstanceHandle = hInstance; // Store instance handle in our global variable

	// Calculate the size of the window which will guarantee the client size we want
	unsigned int width, height;
	DetermineWindowSize(width, height);

	// Create the window
	gWindowHandle = CreateWindow(gWindowClass, gWindowTitle, WINDOW_STYLE, CW_USEDEFAULT, CW_USEDEFAULT, width, height,  nullptr, nullptr, hInstance, nullptr);
	if (!gWindowHandle)
	{
		return FALSE;
	}

	// Initialize Direct3D
	gDirect3D = Direct3DCreate9(D3D_SDK_VERSION);
	if (gDirect3D == nullptr)
	{
		ReportError("Failed to initialize Direct3D");
		return FALSE;
	}

	D3DPRESENT_PARAMETERS params;
	memset(&params, 0, sizeof(D3DPRESENT_PARAMETERS));
	params.Windowed = TRUE;
	params.SwapEffect = D3DSWAPEFFECT_DISCARD;
	params.hDeviceWindow = gWindowHandle;
	params.BackBufferCount = 2;

	HRESULT result = gDirect3D->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, gWindowHandle, D3DCREATE_HARDWARE_VERTEXPROCESSING, &params, &gGraphicsDevice);
	if (FAILED(result))
	{
		ReportError("Failed to create Direct3D device");
		return FALSE;
	}

	// Display the window
	ShowWindow(gWindowHandle, nCmdShow);
	UpdateWindow(gWindowHandle);

	return TRUE;
}


/**
 * The window procedure which handles the application events.
 */
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message)
	{
		case WM_SETFOCUS:
		{
			break;
		}
		case WM_KILLFOCUS:
		{
		  break;
		}
		// An explicit paint message
		case WM_PAINT:
		{
			// This is only really called when something is dragged over top of the window
			RenderScene();

			ValidateRect(hWnd, nullptr);
			break;
		}
		// Handle window size changes and pause streaming when minimized since the back buffer might not be available
		case WM_SIZE:
		{
			break;
		}
		case WM_CHAR:
		{
			if (wParam == '[')
			{
				if (gCaptureDevices.size() > 0)
				{
					gActiveDeviceSlot = gActiveDeviceSlot + (int)gCaptureDevices.size() - 1;
					gActiveDeviceSlot = gActiveDeviceSlot % (int)gCaptureDevices.size();
				}
			}
			else if (wParam == ']')
			{
				if (gCaptureDevices.size() > 0)
				{
					gActiveDeviceSlot++;
					gActiveDeviceSlot = gActiveDeviceSlot % gCaptureDevices.size();
				}
			}
			else
			{
				int slot = (int)(wParam - '1');
				if (slot >= 0 && slot < (int)gCaptureDevices.size())
				{
					ToggleCapture(slot);
				}
			}

			break;
		}
		// Handle key presses
		case WM_KEYDOWN:
		{
			int slot = (int)wParam - (int)VK_F1;
			if (wParam == VK_RIGHT)
			{
				if (gCaptureDevices.size() > 0)
				{
					SelectNextValidCapability(gActiveDeviceSlot);
				}
			}
			else if (wParam == VK_LEFT)
			{
				if (gCaptureDevices.size() > 0)
				{
					SelectPreviousValidCapability(gActiveDeviceSlot);
				}
			}
			break;
		}
		// Close the application
		case WM_DESTROY:
		{
			PostQuitMessage(0);
			break;
		}
		default:
		{
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
	}
	return 0;
}
