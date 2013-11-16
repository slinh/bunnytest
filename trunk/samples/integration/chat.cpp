////////////////////////////////////////////////////////////////////////////////////
// This file contains the implementation of the chat system.
////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "chat.h"
#include "twitchsdk.h"
#include "twitchchat.h"
#include "chatrenderer.h"

#include <assert.h>
#include <string>

#define USE_TEXTURE_ATLAS 1

extern TTV_AuthToken gAuthToken;
extern std::string gUserName;

#define CHAT_STATE(__state__) CS_##__state__

static ChatState gChatState = CHAT_STATE(Uninitialized);


void ChatStatusCallback(TTV_ErrorCode result, void* /*userdata*/)
{
	if (TTV_SUCCEEDED(result))
	{
	}
	else if (result == TTV_EC_CHAT_LOST_CONNECTION)
	{
		gChatState = CHAT_STATE(Disconnected);
	}
	else
	{
		gChatState = CHAT_STATE(Disconnected);
		ASSERT_ON_ERROR(result);
	}
}

void ChatMembershipCallback(TTV_ChatEvent evt, const TTV_ChatChannelInfo* channelInfo, void* /*userdata*/)
{
	switch (evt)
	{
	case TTV_CHAT_JOINED_CHANNEL:
		gChatState = CHAT_STATE(Connected);
		break;
	case TTV_CHAT_LEFT_CHANNEL:
		gChatState = CHAT_STATE(Disconnected);
		break;
	default:
		break;
	}
}

void ChatUserCallback (const TTV_ChatUserList* joinList, const TTV_ChatUserList* leaveList, const TTV_ChatUserList* userInfoList, void* /*userdata*/)
{
	for (uint i = 0; i < leaveList->userCount; ++i)
	{
		RemoveChatUser(&leaveList->userList[i]);
	}

	for (uint i = 0; i < joinList->userCount; ++i)
	{
		AddChatUser(&joinList->userList[i]);
	}

	for (uint i = 0; i < userInfoList->userCount; ++i)
	{
		UpdateChatUser(&userInfoList->userList[i]);
	}

	//////////////////////////////////////////////////////////////////////////
	// Important to free user lists when we are done with them
	//////////////////////////////////////////////////////////////////////////
	TTV_Chat_FreeUserList(joinList);
	TTV_Chat_FreeUserList(leaveList);
	TTV_Chat_FreeUserList(userInfoList);
}

void ChatTokenizedMessageCallback(const TTV_ChatTokenizedMessageList* messageList, void* /*userdata*/)
{
	assert (messageList);

	AddChatMessages(messageList);
}

void ChatClearCallback(const utf8char* channelName, void* /*userdata*/)
{
	ClearChatMessages();
}


void EmoticonDataDownloadCallback(TTV_ErrorCode error, void* /*userdata*/)
{
	assert( TTV_SUCCEEDED(error) );

	// grab the texture and badge data
	if (TTV_SUCCEEDED(error))
	{
		ProcessEmoticonData();
	}
}


void InitializeChat(const utf8char* channel)
{
	TTV_ChatCallbacks chatCallbacks;
	memset(&chatCallbacks, 0, sizeof(chatCallbacks));
	chatCallbacks.statusCallback = ChatStatusCallback;
	chatCallbacks.membershipCallback = ChatMembershipCallback;
	chatCallbacks.userCallback = ChatUserCallback;
	chatCallbacks.messageCallback = nullptr;
	chatCallbacks.tokenizedMessageCallback = ChatTokenizedMessageCallback;
	chatCallbacks.clearCallback = ChatClearCallback;
	chatCallbacks.unsolicitedUserData = nullptr;

	TTV_ErrorCode ret = TTV_Chat_Init(
		channel,		
		&chatCallbacks);
	ASSERT_ON_ERROR(ret);

	TTV_SetTraceLevel(TTV_ML_CHAT);

	gChatState = CHAT_STATE(Initialized);
}


void ShutdownChat()
{
	TTV_Chat_Shutdown();
}


ChatState GetChatState()
{
	return gChatState;
}


void FlushChatEvents()
{
	TTV_Chat_FlushEvents();

	switch (gChatState)
	{
		case CHAT_STATE(Uninitialized):
		{
			break;
		}
		case CHAT_STATE(Initialized):
		{
			gChatState = CHAT_STATE(Connecting);

			// connect to the channel
			TTV_ErrorCode ret = TTV_Chat_Connect(gUserName.c_str(), gAuthToken.data);
			ASSERT_ON_ERROR(ret);

			// start downloading the emoticon data
			ret = TTV_Chat_DownloadEmoticonData(USE_TEXTURE_ATLAS != 0, EmoticonDataDownloadCallback, nullptr);
			ASSERT_ON_ERROR(ret);

			break;
		}
		case CHAT_STATE(Connecting):
			break;
		case CHAT_STATE(Connected):
			break;
		case CHAT_STATE(Disconnected):
			break;
	}
}
