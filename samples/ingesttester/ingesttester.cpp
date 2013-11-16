// ingesttester.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <twitchsdk.h>
#include <malloc.h>

#pragma warning (push,0)
#include "boost/program_options.hpp"
#include "boost/thread.hpp"
#include "boost/chrono.hpp"
#pragma warning (pop)
#include <fstream>

namespace po = boost::program_options;
std::string gClientId = "<client id here>";
std::string gClientSecret = "<client secret here>";
bool gWaitingForCallback = false;
TTV_ErrorCode gLastTaskResult = TTV_EC_SUCCESS; 
uint64_t gTotalSent;
uint64_t gRTMPState;

boost::chrono::time_point<boost::chrono::steady_clock> gStartTime;

void* AllocCallback (size_t size, size_t alignment)
{
	return _aligned_malloc(size, alignment);
}

void FreeCallback (void* ptr)
{
	return _aligned_free(ptr);
}

void StatsCallback(TTV_StatType type, uint64_t value)
{
	switch (type)
	{
	case TTV_ST_RTMPSTATE:
		gRTMPState = value;
		break;
	case TTV_ST_RTMPDATASENT:
		gTotalSent = value;
		break;
	}
}

//////////////////////////////////////////////////////////////////////////
// Authentication
//////////////////////////////////////////////////////////////////////////
void AuthDoneCallback(TTV_ErrorCode result, void* /*userData*/)
{
	if (TTV_SUCCEEDED(result))
	{
		std::cout << "... Success!\n";
	}
	else
	{
		std::cout << "... Failure! (" << result << ")\n";
	}
	gLastTaskResult = result;
	gWaitingForCallback = false;
}

TTV_ErrorCode Authenticate(TTV_AuthToken* authToken, const po::variables_map& variableMap)
{
	gWaitingForCallback = true;

	TTV_AuthParams authParams;
	authParams.size = sizeof(TTV_AuthParams);
	authParams.userName = variableMap["username"].as<std::string>().c_str();
	authParams.password = variableMap["password"].as<std::string>().c_str();
	authParams.clientSecret = gClientSecret.c_str();

	std::cout << "- Requesting Authentication";

	TTV_ErrorCode ret = TTV_RequestAuthToken(&authParams, AuthDoneCallback, NULL, authToken);
	ASSERT_ON_ERROR(ret);
	
	while (gWaitingForCallback && TTV_SUCCEEDED(ret))
	{
		ret = TTV_PollTasks();
		ASSERT_ON_ERROR(ret);
		boost::thread::yield();
	} 

	return gLastTaskResult;
}

//////////////////////////////////////////////////////////////////////////
// Ingest List Retrieval
//////////////////////////////////////////////////////////////////////////
void IngestListCallback(TTV_ErrorCode result, void* /*userData*/)
{
	if (TTV_SUCCEEDED(result))
	{
		std::cout << "... Success!\n";
	}
	else
	{
		std::cout << "... Failure! (" << result << ")\n";
	}

	gLastTaskResult = result;
	gWaitingForCallback = false;
}


TTV_ErrorCode GetIngestList(const TTV_AuthToken* authToken, TTV_IngestList* ingestList)
{
	gWaitingForCallback = true;

	std::cout << "- Requesting Ingest List";
	TTV_ErrorCode ret = TTV_GetIngestServers(authToken, IngestListCallback, 0, ingestList);
	ASSERT_ON_ERROR(ret);

	while (gWaitingForCallback && TTV_SUCCEEDED(ret))
	{
		ret = TTV_PollTasks();
		ASSERT_ON_ERROR(ret);
		boost::thread::yield();
	} 

	return gLastTaskResult;
}

//////////////////////////////////////////////////////////////////////////
// Logging in
//////////////////////////////////////////////////////////////////////////
void LoginCallback(TTV_ErrorCode result, void* /*userData*/)
{
	if (TTV_SUCCEEDED(result))
	{
		std::cout << "... Success!\n";
	}
	else
	{
		std::cout << "... Failure! (" << result << ")\n";
	}

	gLastTaskResult = result;
	gWaitingForCallback = false;
}


TTV_ErrorCode Login(const TTV_AuthToken* authToken, TTV_ChannelInfo* channelInfo)
{
	gWaitingForCallback = true;

	std::cout << "- Logging in";
	TTV_ErrorCode ret = TTV_Login(authToken, LoginCallback, nullptr, channelInfo);
	ASSERT_ON_ERROR(ret);

	while (gWaitingForCallback &&  TTV_SUCCEEDED(ret))
	{
		ret = TTV_PollTasks();
		ASSERT_ON_ERROR(ret);
		boost::thread::yield();
	}

	return gLastTaskResult;
}


//////////////////////////////////////////////////////////////////////////
// Normally you would dealllocate buffers sent to the encoder in this
// callback. Specifically in the ingest tester case we are just using 
// a dummy buffer
//////////////////////////////////////////////////////////////////////////
void BufferUnlockCallback (const uint8_t* /*buffer*/, void* /*userData*/)
{

}

int _tmain(int argc, _TCHAR* argv[])
{
	po::options_description desc("Allowed options");
	po::variables_map variableMap;

	desc.add_options()
		("help", "produce help message")
		("username", po::value<std::string>(), "user name")
		("password", po::value<std::string>(), "password")		
		("tracing_file", po::value<std::string>(), "tracing filename")
		("trace_level", po::value<unsigned int>()->default_value(TTV_ML_NONE), "tracing level")
		("duration", po::value<unsigned int>()->default_value(5), "duration of test")
		;

	po::store(po::parse_command_line(argc, argv, desc), variableMap);

	std::ifstream configFile("config.cfg");
	if (configFile.is_open())
	{
		po::store(po::parse_config_file(configFile, desc), variableMap);
		configFile.close();
	}

	po::notify(variableMap);

	//////////////////////////////////////////////////////////////////////////
	// Initialize the SDK
	//////////////////////////////////////////////////////////////////////////
	TTV_MemCallbacks memCallbacks;
	memCallbacks.size = sizeof (TTV_MemCallbacks);
	memCallbacks.allocCallback = AllocCallback;
	memCallbacks.freeCallback = FreeCallback;	

	TTV_ErrorCode ret = TTV_Init(
		&memCallbacks, 
		gClientId.c_str(),		
		TTV_VID_ENC_INTEL, L"");		
	ASSERT_ON_ERROR(ret);

	TTV_RegisterStatsCallback(StatsCallback);

	//////////////////////////////////////////////////////////////////////////
	// Setup tracing
	//////////////////////////////////////////////////////////////////////////
	if (variableMap.count("tracing_file"))
	{
		std::string traceFile = variableMap["tracing_file"].as<std::string>();
		TTV_SetTraceOutput(std::wstring(traceFile.begin(), traceFile.end()).c_str());
	}
	if (variableMap.count("trace_level"))
	{
		TTV_SetTraceLevel (static_cast<TTV_MessageLevel> (variableMap["trace_level"].as<unsigned int>()));
	}

	TTV_AuthToken authToken;
	TTV_IngestList ingestList;	

	ret = Authenticate(&authToken, variableMap);
	ASSERT_ON_ERROR(ret);

	TTV_ChannelInfo foo;
	foo.size = sizeof(foo);

	ret = Login(&authToken, &foo);
	ASSERT_ON_ERROR(ret);
	
	ret = GetIngestList(&authToken, &ingestList);
	ASSERT_ON_ERROR(ret);

	const uint width = 1280;
	const uint height = 720;

	TTV_VideoParams videoParams;
	videoParams.size = sizeof (TTV_VideoParams);
	videoParams.targetFps = TTV_MAX_FPS;
	videoParams.maxKbps = TTV_MAX_BITRATE;
	videoParams.outputWidth = width;
	videoParams.outputHeight = height;
	videoParams.pixelFormat = TTV_PF_BGRA;

	TTV_GetDefaultParams(&videoParams);

	std::vector<uint32_t> whiteBuffer(width*height, 0xFFFFFFFF);
	std::vector<uint32_t> blackBuffer(width*height, 0x000000FF);

	TTV_AudioParams audioParams;
	audioParams.size = sizeof(TTV_AudioParams);
	audioParams.audioEnabled = false;

	auto testDuration = boost::chrono::seconds(variableMap["duration"].as<unsigned int>());

	for (auto i = 0U;TTV_SUCCEEDED(ret) && i<ingestList.ingestCount; i++)
	{
		std::cout << "- Testing " << ingestList.ingestList[i].serverName << '\n';		

		gTotalSent = 0;
		gRTMPState = 0;

		gStartTime = boost::chrono::steady_clock::now();

		ret = TTV_Start(&videoParams, &audioParams, &ingestList.ingestList[i], 0, nullptr, nullptr);
		ASSERT_ON_ERROR(ret);

		auto elapsedTime = boost::chrono::steady_clock::now() - gStartTime;

		auto conectionTime = boost::chrono::duration_cast<boost::chrono::milliseconds>(elapsedTime);
		gStartTime = boost::chrono::steady_clock::now();

		auto lastTotalSent = gTotalSent;

		bool twiddle = true;
		do
		{
			TTV_SubmitVideoFrame(reinterpret_cast<uint8_t*>(twiddle ? &whiteBuffer[0] : &blackBuffer[0]), BufferUnlockCallback, 0);
			twiddle = !twiddle;
			
			TTV_PollStats();
			elapsedTime = boost::chrono::steady_clock::now() - gStartTime;

			if (lastTotalSent != gTotalSent)
			{
				auto elapsedMilliseconds = boost::chrono::duration_cast<boost::chrono::milliseconds>(elapsedTime);
				float bitrate = static_cast<float>(gTotalSent * 8) / static_cast<float>(elapsedMilliseconds.count());
				std::cout << "\t- RTMP Connected (" << conectionTime.count() << "ms) " << bitrate << "Kbps   \n";
				lastTotalSent = gTotalSent;
			}
		} while (elapsedTime < testDuration);

		if (TTV_SUCCEEDED(ret))
		{
			ret = TTV_Stop(nullptr, nullptr);
			ASSERT_ON_ERROR(ret);
		}

		TTV_PollStats();
		std::cout << std::endl;
	}

	if (TTV_SUCCEEDED(ret))
	{
		ret = TTV_FreeIngestList(&ingestList);
		ASSERT_ON_ERROR(ret);
	}

	TTV_RemoveStatsCallback(StatsCallback);

	//////////////////////////////////////////////////////////////////////////
	// Shutdown the SDK
	//////////////////////////////////////////////////////////////////////////
	ret = TTV_Shutdown();
	ASSERT_ON_ERROR(ret);

	return 0;
}

