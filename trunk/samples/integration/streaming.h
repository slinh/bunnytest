//////////////////////////////////////////////////////////////////////////////
// This file contains the interface to rendering the demo scene.
//////////////////////////////////////////////////////////////////////////////

#ifndef STREAMING_H
#define STREAMING_H

#include <string>

/**
 * Used to keep track of the current state.
 */
#define STREAM_STATE_LIST\
	STREAM_STATE(Uninitialized)\
	STREAM_STATE(Initialized)\
	STREAM_STATE(Authenticating)\
	STREAM_STATE(Authenticated)\
	STREAM_STATE(LoggingIn)\
	STREAM_STATE(LoggedIn)\
	STREAM_STATE(FindingIngestServer)\
	STREAM_STATE(FoundIngestServer)\
	STREAM_STATE(ReadyToStream)\
	STREAM_STATE(Streaming)\
	STREAM_STATE(Paused)


#undef STREAM_STATE
#define STREAM_STATE(__state__) SS_##__state__,
enum StreamState
{
	STREAM_STATE_LIST
};
#undef STREAM_STATE

void InitializeStreaming(const std::string& username, const std::string& password, const std::string& clientId, const std::string& clientSecret, const std::wstring& dllLoadPath);
void StartStreaming(unsigned int outputWidth, unsigned int outputHeight, unsigned int targetFps);
const std::string& GetUsername();
unsigned char* GetNextFreeBuffer();
void SubmitFrame(unsigned char* pBgraFrame);
void Pause();
StreamState GetStreamState();
bool IsStreaming();
bool IsReadyToStream();
void FlushStreamingEvents();
void StopStreaming();
void ShutdownStreaming();

#endif
