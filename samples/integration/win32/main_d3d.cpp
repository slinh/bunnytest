//////////////////////////////////////////////////////////////////////////////
// This module contains the main entry point and code specific to Direct3D9.
//////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "resource.h"
#include "../wavemesh.h"
#include "../streaming.h"
#include "../chat.h"
#include "capture_d3d.h"
#include "../chatrenderer.h"

#include <d3d9.h>
#include <d3dx9math.h>

#define WINDOW_STYLE  (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX)


#pragma region Global Variables

HINSTANCE gInstanceHandle = 0;								// The current application instance
HWND gWindowHandle = 0;										// The main window handle.
TCHAR gWindowTitle[128];									// The title bar text
TCHAR gWindowClass[128];									// the main window class name

IDirect3D9* gDirect3D = nullptr;							// The Direct3D instance.
IDirect3DDevice9* gGraphicsDevice = nullptr;				// The graphics device.

float gRenderFramesPerSecond = 60;							// The number of frames per second to render, 0 if no throttling.
float gLastFrameTime = 0;									// The last time a frame was rendered.
float gLastCaptureTime = 0;									// The timestamp of the last frame capture.

// 360p widescreen is 640x360
// 480p widescreen is about 853x480
unsigned int gBroadcastFramesPerSecond = 30;				// The broadcast frames per second.
unsigned int gBroadcastWidth = 640;							// The broadcast width in pixels.
unsigned int gBroadcastHeight = 368;						// The broadcast height in pixels.

unsigned int gWindowWidth = 1280;							// The width of the window.
unsigned int gWindowHeight = 1024;							// The height of the window.
unsigned int gFullscreen = false;							// Whether or not the app should be fullscreen.

bool gStreamingDesired = false;								// Whether or not the app wants to stream.
bool gPaused = false;										// Whether or not the streaming is paused.
bool gFocused = false;										// Whether the window has focus.
bool gReinitializeRequired = true;							// Whether the device requires reinitialization.

FLOAT gCameraFlySpeed = 100.0f;								// The number of units per second to move the camera.
FLOAT gCameraRotateSpeed = 90.0f;							// The number of degrees to rotate per second.
POINT gLastMousePos;										// Cached mouse position for calculating deltas.

D3DXMATRIX gViewMatrix;										// The camera view matrix.
D3DXMATRIX gProjectionMatrix;								// The scene projection matrix.

#pragma endregion


#pragma region Forward Declarations

ATOM				RegisterWindowClass(HINSTANCE hInstance);
BOOL				InitInstance(HINSTANCE, int);
LRESULT CALLBACK	WndProc(HWND, UINT, WPARAM, LPARAM);

#pragma endregion


#pragma region Timer Functions

float GetSystemClockFrequency()
{	
	static float frequency = 0;
	if (frequency == 0)
	{
		unsigned __int64 freq;
		QueryPerformanceFrequency( reinterpret_cast<LARGE_INTEGER*>(&freq) );
		frequency = (float)freq;
	}

	return frequency;
}

float GetSystemClockTime()
{
	unsigned __int64 counter;
	QueryPerformanceCounter( reinterpret_cast<LARGE_INTEGER*>(&counter) );
	return (float)counter;
}

float SystemTimeToMs(float sysTime)
{
	return (float)sysTime * 1000 / GetSystemClockFrequency();
}

/**
 * Determines the current system time in milliseconds.
 */
float GetSystemTimeMs()
{
	return SystemTimeToMs( GetSystemClockTime() );
}

#pragma endregion


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
	printf("%s\n", buffer);
	MessageBoxA(gWindowHandle, buffer, "Error", MB_OK);
}


/**
 * Determines the directory that the intel encoder DLL is located.
 */
std::wstring GetIntelDllPath()
{
	return std::wstring(L".\\");
}


/**
 * Retrieves the size of the screen.
 */
void GetScreenSize(unsigned int& width, unsigned int& height)
{
	HWND hwnd = GetDesktopWindow();
	RECT rcClient;
	GetClientRect(hwnd, &rcClient);
	POINT topLeft = {rcClient.left, rcClient.top};
	POINT bottomRight = {rcClient.right, rcClient.bottom};
	ClientToScreen(hwnd, &topLeft);
	ClientToScreen(hwnd, &bottomRight);
	
	height = bottomRight.y - topLeft.y;
	width = bottomRight.x - topLeft.x;
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
 * Resets the view to the default.
 */
void ResetView()
{
	// Set the view to the default position
	D3DXMatrixIdentity(&gViewMatrix);
}


/**
 * Handles user input.
 */
void HandleInput()
{
	float timeDelta = (float)(GetSystemTimeMs() - gLastFrameTime) / 1000.0f;

	D3DXMATRIX tx;

	// handle camera rotation
	if (GetAsyncKeyState(VK_RBUTTON))
	{
		POINT last = gLastMousePos;
		GetCursorPos(&gLastMousePos);

		const float dampening = 10.0f;
		float dx = (gLastMousePos.x - last.x) / dampening;
		float dy = (gLastMousePos.y - last.y) / dampening;

		if (dx != 0)
		{
			D3DXMatrixRotationY(&tx, -D3DXToRadian(dx) * timeDelta * gCameraRotateSpeed);
			gViewMatrix = gViewMatrix * tx;
		}

		if (dy != 0)
		{
			D3DXMatrixRotationX(&tx, -D3DXToRadian(dy) * timeDelta * gCameraRotateSpeed);
			gViewMatrix = gViewMatrix * tx;
		}
	}

	// handle camera fly through
	FLOAT x = 0;
	FLOAT y = 0;
	FLOAT z = 0;
	if (GetAsyncKeyState('A'))
	{
		x += gCameraFlySpeed * timeDelta;
	}
	if (GetAsyncKeyState('D'))
	{
		x -= gCameraFlySpeed * timeDelta;
	}
	if (GetAsyncKeyState('E'))
	{
		y -= gCameraFlySpeed * timeDelta;
	}
	if (GetAsyncKeyState('Q'))
	{
		y += gCameraFlySpeed * timeDelta;
	}
	if (GetAsyncKeyState('W'))
	{
		z -= gCameraFlySpeed * timeDelta;
	}
	if (GetAsyncKeyState('S'))
	{
		z += gCameraFlySpeed * timeDelta;
	}

	D3DXMatrixTranslation(&tx, x, y, z);
	gViewMatrix = gViewMatrix * tx;

	// Reset the view
	if (GetAsyncKeyState('R'))
	{
		ResetView();
	}

	// Get the latest mouse position
	GetCursorPos(&gLastMousePos);
}


/**
 * Initializes the rendering using the appropriate rendering method.
 */
bool InitializeRendering()
{
	DestroyWaveMesh();

	DeinitRendering();

	D3DPRESENT_PARAMETERS params;
	memset(&params, 0, sizeof(D3DPRESENT_PARAMETERS));
	params.Windowed = !gFullscreen;
	params.SwapEffect = D3DSWAPEFFECT_DISCARD;
	params.hDeviceWindow = gWindowHandle;
	params.BackBufferCount = 2;
	if (gFullscreen)
	{
		GetScreenSize(gWindowWidth, gWindowHeight);

		params.BackBufferFormat = D3DFMT_A8R8G8B8;
		params.BackBufferWidth = gWindowWidth;
		params.BackBufferHeight = gWindowHeight;
	}
	else
	{
		gWindowWidth = 1280;
		gWindowHeight = 1024;

		unsigned int width, height;
		DetermineWindowSize(width, height);

		SetWindowPos(gWindowHandle, HWND_TOP, 0, 0, width, height, SWP_NOMOVE);
	}

	HRESULT hr = gGraphicsDevice->Reset(&params);
	if (FAILED(hr))
	{
		ReportError("Failed to initialize device");
		return false;
	}

	InitRendering(gWindowWidth, gWindowHeight, gBroadcastWidth, gBroadcastHeight);
	
	// Set the viewport
	D3DVIEWPORT9 vp;
	vp.X = 0;
	vp.Y = 0;
	vp.MinZ = 0.0f;
	vp.MaxZ = 1.0f;
	vp.Width = gWindowWidth;
	vp.Height = gWindowHeight;
	gGraphicsDevice->SetViewport(&vp);

	// Setup the projection matrix
	D3DXMatrixPerspectiveFovLH(&gProjectionMatrix, D3DXToRadian(60), (FLOAT)gWindowWidth/(FLOAT)gWindowHeight, 1, 1000);

	// Create the mesh that will be rendered
	CreateWaveMesh(64, 20);

	InitializeChatRenderer(gWindowWidth, gWindowHeight);

	return true;
}


/**
 * Render the scene using the appropriate rendering method.
 */
void Render()
{
	// Disable lighting
	gGraphicsDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	RenderScene();

	RenderChatText();

	gGraphicsDevice->Present(nullptr, nullptr, nullptr, nullptr);
}


/**
 * The main entry point for the application.
 */
int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
{
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

	// Initialize global strings
	LoadString(hInstance, IDS_APP_TITLE, gWindowTitle, sizeof(gWindowTitle));
	LoadString(hInstance, IDC_INTEGRATION, gWindowClass, sizeof(gWindowClass));

	// Register the window class
	RegisterWindowClass(hInstance);

	// Perform application initialization:
	if ( !InitInstance(hInstance, nCmdShow) )
	{
		return FALSE;
	}

	// Set the view to the default position
	ResetView();

	// Cache the last mouse position
	GetCursorPos(&gLastMousePos);

	// Initialize the Twitch SDK
	std::string channelName = "<username>";
	InitializeStreaming("<username>", "<password>", "<clientId>", "<clientSecret>", GetIntelDllPath());

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

		if (gReinitializeRequired)
		{
			gReinitializeRequired = false;
			InitializeRendering();
		}

		// Draw the scene
		Render();

		UpdateWaveMesh();

		// Process user input independent of the event queue
		if (gFocused && !AcceptingChatInput())
		{
			HandleInput();
		}

		// Record the frame time
		float curTime = GetSystemTimeMs();

		// Begin streaming when ready
		if (gStreamingDesired && 
			!IsStreaming() &&
			IsReadyToStream())
		{
			StartStreaming(gBroadcastWidth, gBroadcastHeight, gBroadcastFramesPerSecond);

			gLastCaptureTime = 0;
		}

		// If you send frames too quickly to the SDK (based on the broadcast FPS you configured) it will not be able 
		// to make use of them all.  In that case, it will simply release buffers without using them which means the
		// game wasted time doing the capture.  To mitigate this, the app should pace the captures to the broadcast FPS.
		float captureDelta = curTime - gLastCaptureTime;
		bool isTimeForNextCapture = (captureDelta / 1000.0f) >= (1.0f / gBroadcastFramesPerSecond);

		// streaming is in progress so try and capture a frame
		if (IsStreaming() && 
			!gPaused &&
			isTimeForNextCapture)
		{
			// capture a snapshot of the back buffer
			unsigned char* pBgraFrame = nullptr;
			int width = 0;
			int height = 0;
			bool gotFrame = false;

			gotFrame = CaptureFrame(gBroadcastWidth, gBroadcastHeight, pBgraFrame, width, height);

			// send a frame to the stream
			if (gotFrame)
			{
				SubmitFrame(pBgraFrame);
			}
		}

		// The SDK may generate events that need to be handled by the main thread so we should handle them
		FlushStreamingEvents();

		#undef CHAT_STATE
		#undef STREAM_STATE
		#define CHAT_STATE(__state__) CS_##__state__
		#define STREAM_STATE(__state__) SS_##__state__

		// initialize chat after we have authenticated
		if (GetChatState() == CHAT_STATE(Uninitialized) && 
			GetStreamState() >= STREAM_STATE(Authenticated))
		{
			InitializeChat(channelName.c_str());
		}

		if (GetChatState() != CHAT_STATE(Uninitialized))
		{
			FlushChatEvents();
		}

		#undef CHAT_STATE
		#undef STREAM_STATE

		gLastFrameTime = curTime;

		// Update the window title to show the state
		#undef STREAM_STATE
		#define STREAM_STATE(__state__) #__state__,
		const char* streamStates[] = 
		{
			STREAM_STATE_LIST
		};
		#undef STREAM_STATE

		#undef CHAT_STATE
		#define CHAT_STATE(__state__) #__state__,
		const char* chatStates[] = 
		{
			CHAT_STATE_LIST
		};
		#undef CHAT_STATE

		char buffer[256];
		sprintf_s(buffer, sizeof(buffer), "Twitch Direct3D Integration Sample - %s - Stream:%s Chat:%s", GetUsername().c_str(), streamStates[GetStreamState()], chatStates[GetChatState()]);
		SetWindowTextA(gWindowHandle, buffer);
	}

	StopStreaming();

	DeinitChatRenderer();

	// Shutdown the Twitch SDK
	ShutdownChat();
	ShutdownStreaming();

	DeinitRendering();

	// Shutdown the app
	gGraphicsDevice->Release();
	gDirect3D->Release();

	// Cleanup the mesh
	DestroyWaveMesh();

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
	wcex.hIcon			= LoadIcon(hInstance, MAKEINTRESOURCE(IDI_INTEGRATION));
	wcex.hCursor		= LoadCursor(nullptr, IDC_ARROW);
	wcex.hbrBackground	= (HBRUSH)(COLOR_WINDOW+1);
	wcex.lpszMenuName	= MAKEINTRESOURCE(IDC_INTEGRATION);
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

	D3DPRESENT_PARAMETERS params;
	memset(&params, 0, sizeof(D3DPRESENT_PARAMETERS));
	params.Windowed = TRUE;
	params.SwapEffect = D3DSWAPEFFECT_DISCARD;
	params.hDeviceWindow = gWindowHandle;
	params.BackBufferCount = 2;

	gDirect3D->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, gWindowHandle, D3DCREATE_HARDWARE_VERTEXPROCESSING, &params, &gGraphicsDevice);

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
			GetCursorPos(&gLastMousePos);
			gFocused = true;
			break;
		}
		case WM_KILLFOCUS:
		{
		  gFocused = false;
		  break;
		}
		// An explicit paint message
		case WM_PAINT:
		{
			if (!gReinitializeRequired)
			{
				// This is only really called when something is dragged over top of the window
				RenderScene();
			}

			ValidateRect(hWnd, nullptr);
			break;
		}
		// Handle window size changes and pause streaming when minimized since the back buffer might not be available
		case WM_SIZE:
		{
			// Update the pause state
			int wmEvent = LOWORD(wParam);
			if (wmEvent == SIZE_MINIMIZED)
			{
				gPaused = true;
				Pause();
			}
			else if (wmEvent == SIZE_RESTORED)
			{
				gPaused = false;
			}

			break;
		}
		case WM_CHAR:
		{
			if ( AcceptingChatInput() )
			{
				unsigned char ch = static_cast<unsigned char>(wParam);

				// backspace or visible character
				if ( ch == 8 || (ch >= 32 && ch < 127) )
				{
					AppendChatInput(ch);
				}
			}
			break;
		}
		// Handle key presses
		case WM_KEYDOWN:
		{
			if ( AcceptingChatInput() )
			{
				switch (wParam)
				{
					case VK_RETURN:
					{
						EndChatInput(true);
						break;
					}
					case VK_ESCAPE:
					{
						EndChatInput(false);
						break;
					}
				}
			}
			else
			{
				switch (wParam)
				{
					// begin chat input
					case VK_RETURN:
					{
						BeginChatInput();
						break;
					}
					// Toggle streaming
					case VK_F5:
					{
						if (IsStreaming())
						{
							gStreamingDesired = false;
							StopStreaming();
						}
						else
						{
							gStreamingDesired = true;
							StartStreaming(gBroadcastWidth, gBroadcastHeight, gBroadcastFramesPerSecond);
						}
						break;
					}
					// Toggle fullscreen
					case VK_F12:
					{
						gFullscreen = !gFullscreen;
						gReinitializeRequired = true;
						break;
					}
					// Toggle broadcast resolution
					case VK_F1:
					{
						bool streaming = IsStreaming();
						if (streaming)
						{
							StopStreaming();
						}

						if (gBroadcastWidth == 640)
						{
							gBroadcastWidth = 1024;
							gBroadcastHeight = 768;
						}
						else
						{
							gBroadcastWidth = 640;
							gBroadcastHeight = 368;
						}

						if (streaming)
						{
							StartStreaming(gBroadcastWidth, gBroadcastHeight, gBroadcastFramesPerSecond);
						}

						break;
					}
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
