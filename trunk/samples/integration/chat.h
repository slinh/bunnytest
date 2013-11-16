////////////////////////////////////////////////////////////////////////////////////
// This file contains the interface of the chat system.
////////////////////////////////////////////////////////////////////////////////////

#pragma once

#include "twitchchat.h"

/**
 * Used to keep track of the current state.
 */
#define CHAT_STATE_LIST\
	CHAT_STATE(Uninitialized)\
	CHAT_STATE(Initialized)\
	CHAT_STATE(Connecting)\
	CHAT_STATE(Connected)\
	CHAT_STATE(Disconnected)

#undef CHAT_STATE
#define CHAT_STATE(__state__) CS_##__state__,
enum ChatState
{
	CHAT_STATE_LIST
};
#undef CHAT_STATE


void InitializeChat(const utf8char* channel);
void ShutdownChat();
void FlushChatEvents();
ChatState GetChatState();