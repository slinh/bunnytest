#Twitch Broadcasting SDK - Change history

#### October 03, 2013
- Added TTV_VID_ENC_DISABLE to disable video (and audio) capture/encoding/streaming. 
  This is being done as part of an ongoing effort to break up all functionality into modules that can be used independently.  

#### Sept 24, 2013  
Added a new API TTV_SubmitAudioSamples to allow the client to submit audio if desired (Currently only tested on iOS). New flags added to TTV_AudioParams to enable/disable capturing mic and/or system audio by the SDK. At least one of these flags MUST be set if audio is enabled.  

#### Sept 19, 2013  
- Changed the way shutdown works, so that data is dumped if there is a long queue of data. If the network is very
  stalled, shutdown can still take a while as we have to wait for the OS to complete older send()s

#### Sept 5, 2013  
- Improved detection of QuickSync hardware encoding capability. In some cases, QuickSync wouldn't be detected and software encoding used when in fact hardware encoding was available.  

#### August 19, 2013  
- Added callbacks to metadata submission API.  
- Added callback to TTV_GetStreamInfo.   
- Changed TTV_EC_WEBAPI_RESULT_NO_STREAMINFO error to TTV_WRN_STREAMINFO_PENDING warning for TTV_GetStreamInfo.  
- Added TTV_EC_SOUNDFLOWER_NOT_INSTALLED error for attempting TTV_Start with audio enabled on Mac when SoundFlower is not installed.  

#### August 16, 2013  
- Added userdata to all chat callbacks.  
- Perform resolution validation synchronously when TTV_Starting asynchronously.   

#### August 15, 2013  
- Added a callback to TTV_SetStreamInfo.  

#### August 5, 2013  
- Moved all the chat code into a new twitchchat project. This won't impact any public API's, but you'll need to add twitchsdk/twitchchat/include/ to your include paths. Also, if building twitchsdk in your solution, you'll need to add this project.  

####July 25, 2013  
- Fixed a bug where in absence of hardware encoding, if a dll path wasn't specified (because dll's are in same folder as exe), the encoder initialization would fail.  

####July 18, 2013  
- Updated the Intel Media sdk to version 1.7 and improved the code to detect hardware QSV support. *IMPORTANT* Update the libmfx*.dll binaries you distribute.  

####July 17, 2013  
- Removed libcurl. We now use platform-specific implementations for making HTTP requests (e.g. WinInet on Windows). This means you no longer need the CA Cert bundle file and libcurl.dll, libeay32.dll and ssleay32.dll.  

####July 16, 2013  
- Fixed a crash in chat when receiving extremely long messages from the server.  

####July 15, 2013
- Changed the code to ensure keyframes every 2 seconds to be compatible with future video tech at twitch.tv
- A side effect of the change is that if no data is submitted, the last frame is auto resubmitted and this will keep the stream alive

####June 27, 2013  
- Moved some common services into the new twitchcore project and you'll need to add twitchsdk/twitchcore/include/ to your include paths. This doesn't impact the public API, however if you were building twitchsdk as part of your solution, you'll also need to add this new project.  

####June 26, 2013  
- Added the ability to filter the trace output based on the channel/area of the trace. This will make it easier for us to use a high verbosity trace when tracking down bugs with you.
- Changed the logging to use OutputDebugString instead of printf. This means that you will now see our trace messages from our DLL if you use TTV_SetTraceLevel, TTV_SetTraceChannelLevel without setting a file with TTV_SetTraceOutput

####June 25, 2013  
- Changed the metadata API functions so that they don't require the stream id.  This enables games to send metadata immediately and allows the SDK to do caching internally and flush the batch to the server when the stream id is available.  Affected API functions are TTV_SendActionMetaData, TTV_SendStartSpanMetaData and TTV_SendEndSpanMetaData.  
- Added a new API function called TTV_GetStreamTime which returns the current stream time in milliseconds for use in submitting metadata.  

####June 10, 2013  
- Updated the metadata API. Please see comments in twitchsdk.h.  

####June 7, 2013  
- Implement bitrate throttling to try to cope with low bandwidth situations. If packets continue to queue up despite bitrate throttling TTV_EC_FRAME_QUEUE_TOO_LONG is returned in TTV_SubmitVideoFrame and you should stop streaming (or risk running out of memory eventually).  

####May 30, 2013  
- Added callbacks to TTV_Start and TTV_Stop so that they can optionally be asynchronous.  If null is supplied as the callback then the function will behave synchronously as before.  
- Fixed a minor issue when shutting down that callbacks for outstanding async requests would not be called.  Now they will be called with TTV_EC_REQUEST_ABORTED during TTV_Shutdown.  

####May 29, 2013  
- Added TTV_GetGameLiveStreams which returns the list of up to 25 live streams for a given game  

###May 23, 2013  
- Added TTV_EC_STREAM_NOT_STARTED and TTV_EC_STREAM_ALREADY_STARTED error codes for returning in some cases when a call to TTV_Start, TTV_Pause, TTV_SubmitVideoFrame or TTV_Login is made under inappropriate circumstances.  

####May 16, 2013  
- Added optimizations for RGB->YUV color conversion. Width of the submitted frames must now be a multiple of 32 (height still only needs to be a multiple of 16)  

####April 19, 2013  
- Added TTV_GetMaxResolution. Instead of the game supplying the resolution and the SDK recommending a bitrate, the game must now supply a maximum bitrate and the SDK will recommend a max resolution which would result in acceptable video quality.  

####April 16, 2013  
- Added new API to download emoticon data for chat and provides a texture atlas of images.  See TTV_Chat_DownloadEmoticonData, TTV_Chat_GetEmoticonData and TTV_Chat_TokenizeMessage.  

####April 3, 2013  
- Renamed TTV_WebCam_GetNextFrame to TTV_WebCam_GetFrame and simplified the webcam sample.  

####April 2, 2013  
- Add support for selecting the best available video encoder by initializing the SDK with TTV_VID_ENC_DEFAULT.  

####April 1, 2013  
- Added support for microphone audio capture and AAC encoding on iOS.  

####March 15, 2013 
- Added a new API call which will retrieve a list of games with names matching the query string you provide.  This is useful in selecting a valid game name for broadcasting.  See TTV_GetGameNameList for details.

####March 12, 2013 
- Added an alternate chat connection API which allows connecting anonymously to a channel for listening only.  See TTV_Chat_ConnectAnonymous for details.

####February 27, 2013 
- The call to TTV_PauseVideo will now display an animation (rather than a solid color) to help indicate that the stream has not died.

####February 25, 2013  
- Added support for iOS streaming (currently video only; no audio).
- TTV_GetStreamInfo now returns TTV_EC_WEBAPI_RESULT_NO_STREAMINFO. If this is returned you should maintain the last valid stream info obtained.  
- Ensuring that submitted frame buffer has 16-byte alignment if needed (added error code TTV_EC_ALIGN16_REQUIRED).
- Added new webcam API available though twitchwebcam.h and accompanying webcam sample.  Hasn't yet been tested on a wide variety of devices.
- Updated API in general to consistently use TTV_AuthToken for passing authentication tokens around.  This struct should now be serialized as binary when persisted by the application.
- The signature of TTV_AllocCallback in TTV_MemCallbacks now takes an alignment parameter. The callback must return memory meeting the alignment requirement unless it's 0.  

####February 4, 2013  
- Added TTV_RunCommercial to trigger a commercial on the channel

####January 24, 2013  
- Added TTV_GetArchivingState to determine whether archiving is enabled for the channel  
- Removed TTV_PrepareStream and added TTV_Login which returns channel info  
- Added ability to toggle the broadcast resolution in the streaming sample with F1 key   
- Fixed bug in the streaming sample which occurred when changing broadcast resolution  
- Added support for user modes, subscriptions, clearing the chat window, user color, ignore and user change even  

####January 15, 2013  
- Added Windows Direct3D streaming sample
- Added error to string conversion function to API: TTV_ErrorToString

####January 14, 2013  
- Added support for Mac audio capture  
- Added support for chat  
- Added ingest tester and chat samples  

####December 13, 2012  
- Changing file paths to take a wchar_t-based string instead of a char-based string.

####December 12, 2012  
- Added support for pausing the stream  

####December 11, 2012
- Fixed bug with upsampling audio, low sample rate devices are being initialized again.
- jsoncpp now uses 64bit
- Updated tasks to use 64bit  

####November 20, 2012  
- Not initializing audio device if its sample rate is below 44100  

####November 16, 2012
- Added mac support! Audio does not work yet and it uses the libx264 encoder.
- Added a sample of doing Ingest testing.
- Returning a warning when submitting frames if the delay in the internal send queue is longer than 5 seconds
- Added code to get performance stats from the SDK. Limited to rtmp connection and rtmp bitrate for now.

####November 14, 2012  
- Fixed race condition in taskrunner when shutting down  

####November 12, 2012  
- Fixed minor memory leak
- Ensuring Intel SDK always runs in software mode  

####November 1, 2012
- Fixed TTV_SetGameInfo

####October 31, 2012
- Fixed accumulating roundoff error causing audio/video drift

####October 25, 2012  
- Renamed ffmpeg lib/dll's to add a -ttv suffix  

####October 19, 2012  
- Fixed a bug in RTMP where the stream would not show any video and go offline after a few seconds
- Replaced encoding quality setting with "Encoding CPU usage"
- Fixed the bug where corrupt video would be generated when width wasn't multiple of 32  

####October 18, 2012
- Switched to CBR encoding mode
- Changed how we caluclate default bitrate
- Fixed a bug in the muxer
- Added API calls for getting the user info and stream info
- Fixed bug where no video was generated when audio was disabled

####October 16, 2012
- Fixed crash due to race condition when trying to shut down the SDK while getting ingest lists is in progress
- Added timeouts for web API requests to avoid potential for blocking forever
- Refactored code for the web API tasks
- Passing errors from flv/rtmp back up the chain

####October 11, 2012
- Added our own rtmp implmentation
- Removed lots of deps on ffmpeg

####October 8, 2012
- Switched to using a versioned API so that web API changes won't break the SDK
- Requiring a client_secret for authentication

####October 4, 2012  
- Returning the default ingest server to use  
- Added all required  license files  
- Fixed uninitialized variable bug when getting list of ingest servers  
- No longer linking with libx264  
- Handling audio configuration changes (e.g. plugging/unplugging headphones)  
- Fix for bug when game calls CoInitialize on the same thread
- Updated the sample code: https://github.com/justintv/twitchsdk/wiki/Basic-Sample

####September 27, 2012
- Fixed FFMPEG RTMP bandwidth bug that limited possible output bitrate  
- Poperly getting list of ingest servers  

####September 18, 2012
- Added support for selecting the video encoder
- Removed requirement for VS SP1 from the API headers
- Removed references to functions that don't exist on Windows XP

####September 14, 2012
- Added tracing support
- Fixed bugs with starting and stopping streaming multiple times
- Changed all our busy loops to a suspend/resume model. Has a noticable impact on cpu usage.

####September 11, 2012
- Added 64-bit support to the SDK

####September 4, 2012
- Added support for getting list of available ingest servers

####Auguest 29, 2012
- Using LAME MP3 encoder directly (Using new build of ffmpeg with MP3 encoder removed)

####August 24, 2012
- New minimal build of ffmpeg libraries
- Fix crash during shutdown

####August 21, 2012
- Added support for supplying an optional path of where the Intel encoder DLL should be loaded from
- Removed option for writing to local file from the API
- Fixed bug in Init where failed return values were not properly returned back

####August 17, 2012
- Added a CA Cert bundle file and now TTV_Init requires the path to a cert bundle file

####August 15, 2012
- Tuned encoder settings

####August 14, 2012
- Requiring Visual Studio 2010 SP1 to build
- Fixed shutdown crashes/leaks: The SDK now shuts down cleanly.
- Fixed frame dropping algorithm: If you submit frames faster than the target output FPS, the SDK drops some frames to maintain the target FPS.

####August 9, 2012
- Fixed issues with audio quality (particularly noticable with high pitch sounds)
- Fixed bug where the libx264 encoder was being loaded although it's not actually used
- Added support for using DirectX Video Acceleration for color conversion and encoding.  
  NOTE: This *may* improve performance but is very experimental at this point.

####August 6, 2012
- Support for authentication using Twitch username/password (no need to supply a stream key anymore)
- Support for sending metadata (currently the server will ignore any data sent)
- Fixed audio/video sync issues
- Added volume control for each device
- A callback function is called when the SDK is done with the submitted frame
- Support for setting more video encoding parameters (e.g. bitrate) and an API function that supplies default values for them
- Routing all Intel encoder allocations to the SDK, and in turn, the game-supplied allocation functions
- Removed boost
