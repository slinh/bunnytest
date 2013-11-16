//////////////////////////////////////////////////////////////////////////////
// This module contains the platform-independent code demonstrating how to 
// set up and use the Twitch SDK for basic streaming.
//////////////////////////////////////////////////////////////////////////////

#include "twitchsdk.h"
#include "streaming.h"
#include <vector>
#include <algorithm>

bool gSdkInitialized = false;			// Whether or not TTV_Init has been called.
StreamState gStreamState = SS_Uninitialized;	// The current state of streaming.

std::string gUserName = "";				// The cached username.
std::string gPassword = "";				// The cached password.
std::string gClientId = "";				// The cached client id.
std::string gClientSecret = "";			// The cached client secret.

TTV_AuthToken gAuthToken;				// The unique key that allows the client to stream.
TTV_ChannelInfo gChannelInfo;			// The information about the channel associated with the auth token
TTV_IngestList gIngestList;				// Will contain valid data the callback triggered due to TTV_GetIngestServers is called.
TTV_UserInfo gUserInfo;					// Profile information about the local user.
TTV_StreamInfo gStreamInfo;				// Information about the stream the user is streaming on.
TTV_IngestServer gIngestServer;			// The ingest server to use.

std::vector<unsigned char*> gFreeBufferList;	// The list of free buffers.  The app needs to allocate exactly 3.
std::vector<unsigned char*> gCaptureBuffers;	// The list of all buffers.

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


/**
 * Callback from the SDK to return the result of login as well the channel info
 */
void LoginCallback(TTV_ErrorCode result, void* userData)
{
	if ( TTV_SUCCEEDED(result) )
	{
		gStreamState = SS_LoggedIn;
	}
	else
	{
		const char* err = TTV_ErrorToString(result);
		ReportError("LoginCallback got failure: %s\n", err);
	}
}

/**
 * Callback from the SDK to provide the list of ingest servers available.
 */
void IngestListCallback(TTV_ErrorCode result, void* userData)
{
	if ( TTV_SUCCEEDED(result) )
	{
		// Find the ingest server to use
		uint serverIndex = 0;
		for (uint i = 0; i < gIngestList.ingestCount; ++i)
		{
			// Use the default server for now
			if (gIngestList.ingestList[i].defaultServer)
			{
				serverIndex = i;
				break;
			}
		}
		
		gIngestServer = gIngestList.ingestList[serverIndex];
		gStreamState = SS_FoundIngestServer;
	}
	else
	{
		const char* err = TTV_ErrorToString(result);
		ReportError("IngestListCallback got failure: %s\n", err);
	}

	// It is the app's responsibility to free the ingest list when done with it
	TTV_FreeIngestList(&gIngestList);
}

/**
 * Callback from the SDK which provides profile information about the local user.
 */
void UserInfoDoneCallback(TTV_ErrorCode result, void* /*userData*/)
{
	if ( TTV_FAILED(result) )
	{
		const char* err = TTV_ErrorToString(result);
		ReportError("UserInfoDoneCallback got failure: %s\n", err);
	}
}

/**
 * Callback from the SDK which provides information about the stream.
 */
void StreamInfoDoneCallback(TTV_ErrorCode result, void* /*userData*/)
{
	if ( TTV_FAILED(result) && result != TTV_EC_WEBAPI_RESULT_NO_STREAMINFO )
	{
		const char* err = TTV_ErrorToString(result);
		ReportError("StreamInfoDoneCallback got failure: %s\n", err);
	}
}

/**
 * The callback that is called when the SDK has authenticated the user.
 */
void AuthDoneCallback(TTV_ErrorCode result, void* userData)
{
	if ( TTV_SUCCEEDED(result) )
	{
		// Now that the user is authorized the information can be requested about which server to stream to
		gStreamState = SS_Authenticated;
	}
	else
	{
		const char* err = TTV_ErrorToString(result);
		ReportError("AuthDoneCallback got failure: %s\n", err);
	}
}

/**
 * The callback that will be called when the SDK is finished encoding a frame the application has passed to it.
 */
void FrameUnlockCallback(const uint8_t* buffer, void* /*userData*/)
{
	unsigned char* p = const_cast<unsigned char*>(buffer);

	// Put back on the free list
	gFreeBufferList.push_back(p);
}

#pragma endregion


/**
 * Initializes the Twitch SDK and begins authentication of the user.  The authentication is asynchronous and FlushStreamingEvents() needs to be 
 * called to call callback functions.  When IsReadyToStream() returns true then the SDK is ready to begin streaming which can be 
 * done with a call to StartStreaming().
 */
void InitializeStreaming(const std::string& username, const std::string& password, const std::string& clientId, 
						 const std::string& clientSecret, const std::wstring& dllLoadPath)
{
	switch (gStreamState)
	{
		// SDK not initialized
		case SS_Uninitialized:
			break;

		// Already trying to stream
		case SS_Initialized:
		case SS_Authenticating:
		case SS_Authenticated:
		case SS_LoggingIn:
		case SS_LoggedIn:
		case SS_FindingIngestServer:
		case SS_FoundIngestServer:		
		case SS_Streaming:
		case SS_Paused:		
			return;
		default:
			break;
	}

	gUserName = username;
	gPassword = password;
	gClientId = clientId;
	gClientSecret = clientSecret;

	// Setup the memory allocation callbacks needed by the SDK
	TTV_MemCallbacks memCallbacks;
	memCallbacks.size = sizeof(TTV_MemCallbacks);
	memCallbacks.allocCallback = AllocCallback;
	memCallbacks.freeCallback = FreeCallback;

	// The intel encoder is used on Windows
	TTV_VideoEncoder vidEncoder = TTV_VID_ENC_INTEL;

	// Initialize the SDK
	TTV_ErrorCode ret = TTV_Init(&memCallbacks, clientId.c_str(), vidEncoder, dllLoadPath.c_str());
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while initializing the Twitch SDK: %s\n", err);
		return;
	}

	// SDK now initialized
	gSdkInitialized = true;

	// Obtain the AuthToken which will allow the user to stream
	TTV_AuthParams authParams;
	authParams.size = sizeof(TTV_AuthParams);
	authParams.userName = username.c_str();
	authParams.password = password.c_str();
	authParams.clientSecret = clientSecret.c_str();
	
	gStreamState = SS_Authenticating;

	ret = TTV_RequestAuthToken(&authParams, AuthDoneCallback, nullptr, &gAuthToken);
	if ( TTV_FAILED(ret) )
	{
		gStreamState = SS_Initialized;
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while requesting auth token: %s\n", err);
		return;
	}

	// Now we need to wait for the callback to be called for TTV_RequestAuthToken
}


/**
 * Once InitializeStreaming() has called all callback functions and IsReadyToStream() returns true then this function
 * can be called which will initiate streaming.  Once streaming begins, the app must call SubmitFrame() to submit frames
 * to the stream.
 */
void StartStreaming(unsigned int outputWidth, unsigned int outputHeight, unsigned int targetFps)
{
	switch (gStreamState)
	{
		// SDK not initialized
		case SS_Uninitialized:
			return;

		// Already trying to stream
		case SS_Initialized:
		case SS_Authenticating:
		case SS_Authenticated:
		case SS_FindingIngestServer:
		case SS_FoundIngestServer:		
		case SS_Streaming:
		case SS_Paused:
			return;

		// Ready to stream
		case SS_ReadyToStream:
			break;
	}

	// Setup the video parameters
	TTV_VideoParams videoParams;
	memset(&videoParams, 0, sizeof(videoParams));
	videoParams.size = sizeof(TTV_VideoParams);
	videoParams.outputWidth = outputWidth;
	videoParams.outputHeight = outputHeight;
	videoParams.targetFps = targetFps;

	// Compute the rest of the fields based on the given parameters
	TTV_GetDefaultParams(&videoParams);
	videoParams.pixelFormat = TTV_PF_BGRA;

	// Setup the audio parameters
	TTV_AudioParams audioParams;
	audioParams.size = sizeof(TTV_AudioParams);
	audioParams.audioEnabled = true;

	TTV_ErrorCode ret = TTV_Start(&videoParams, &audioParams, &gIngestServer, 0, nullptr, nullptr);
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while starting to stream: %s\n", err);
		return;
	}

	// Now streaming
	gStreamState = SS_Streaming;

	// Allocate exactly 3 buffers to use as the capture destination while streaming.
	// These buffers are passed to the SDK.
	for (unsigned int i=0; i<3; ++i)
	{
		unsigned char* pBuffer = new unsigned char[outputWidth*outputHeight*4];
		gCaptureBuffers.push_back(pBuffer);
		gFreeBufferList.push_back(pBuffer);
	}
}


/**
 * Retrieves the username.
 */
const std::string& GetUsername()
{
	return gUserName;
}


/**
 * Grabs the next available buffer from the free list.  There should always be one available.
 */
unsigned char* GetNextFreeBuffer()
{
	if (gFreeBufferList.size() == 0)
	{
		ReportError("Out of free buffers, this should never happen\n");
		return nullptr;
	}

	unsigned char* pBuffer = gFreeBufferList.back();
	gFreeBufferList.pop_back();

	return pBuffer;
}


/**
 * Submits a frame to the stream.  The size of the buffer must be outputWidth*outputHeight*4 which was specified in the call to StartStreaming().
 */
void SubmitFrame(unsigned char* pBgraFrame)
{	
	if (!IsStreaming())
	{
		return;
	}

	TTV_ErrorCode ret = TTV_SubmitVideoFrame(pBgraFrame, FrameUnlockCallback, 0);
	if ( TTV_FAILED(ret) )
	{
		// not streaming anymore
		gStreamState = SS_Initialized;
		StopStreaming();

		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while submitting frame to stream: %s\n", err);
	}
}


/**
 * Pauses the stream which will display a default image on the Twitch site.  To unpause the stream simply submit another frame.
 */
void Pause()
{
	if (!IsStreaming())
	{
		return;
	}

	TTV_ErrorCode ret = TTV_PauseVideo();
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error pausing video: %s\n", err);
	}
}


/**
 * Retrieves the state of the module.
 */
StreamState GetStreamState()
{
	return gStreamState;
}


/**
 * Determines whether or not streaming is currently in progress.
 */
bool IsStreaming()
{
	return gStreamState == SS_Streaming || gStreamState == SS_Paused;
}


/**
 * Determines whether or not the SDK has been initialized properly and is ready to have StartStreaming() called.
 */
bool IsReadyToStream()
{
	return gStreamState == SS_ReadyToStream;
}


/**
 * Allows the callback functions to be called on the current thread.  This should be called periodically.
 */
void FlushStreamingEvents()
{
	TTV_PollTasks();

	switch (gStreamState)
	{
		// Kick off an authentication request
		case SS_Authenticated:
		{
			gStreamState = SS_LoggingIn;
			gChannelInfo.size = sizeof(TTV_ChannelInfo);
			TTV_Login(&gAuthToken, LoginCallback, nullptr, &gChannelInfo);
			break;
		}
		// Login
		case SS_LoggedIn:
		{
			gStreamState = SS_FindingIngestServer;
			TTV_GetIngestServers(&gAuthToken, IngestListCallback, nullptr, &gIngestList);
			break;
		}
		// Ready to stream
		case SS_FoundIngestServer:
		{
			gStreamState = SS_ReadyToStream;

			// Kick off requests for the user and stream information that aren't 100% essential to be ready before streaming starts
			gUserInfo.size = sizeof(TTV_UserInfo);
			TTV_GetUserInfo(&gAuthToken, UserInfoDoneCallback, nullptr, &gUserInfo);

			gStreamInfo.size = sizeof(TTV_StreamInfo);
			TTV_GetStreamInfo(&gAuthToken, StreamInfoDoneCallback, nullptr, gUserName.c_str(), &gStreamInfo);
			break;
		}
		// No action required
		case SS_FindingIngestServer:
		case SS_Authenticating:
		case SS_Initialized:
		case SS_Uninitialized:		
		case SS_Streaming:
		case SS_Paused:
		{
			break;
		}
		default:
		{
			break;
		}
	}
}


/**
 * After StartStreaming() is called streaming will continue until this function is called.
 */
void StopStreaming()
{
	if (!IsStreaming())
	{
		return;
	}

	// No longer streaming
	gStreamState = SS_ReadyToStream;

	TTV_ErrorCode ret = TTV_Stop(nullptr, nullptr);
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while stopping the stream: %s\n", err);
	}

	// Delete the capture buffers
	for (unsigned int i=0; i<gCaptureBuffers.size(); ++i)
	{
		delete [] gCaptureBuffers[i];
	}
	gFreeBufferList.clear();
	gCaptureBuffers.clear();
}


/**
 * Cleans up the SDK after being initialized.
 */
void ShutdownStreaming()
{
	if (!gSdkInitialized)
	{
		return;
	}

	StopStreaming();

	gSdkInitialized = false;
	gStreamState = SS_Uninitialized;

	TTV_ErrorCode ret = TTV_Shutdown();
	if ( TTV_FAILED(ret) )
	{
		const char* err = TTV_ErrorToString(ret);
		ReportError("Error while shutting down the Twitch SDK: %s\n", err);
		return;
	}
}
