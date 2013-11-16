//////////////////////////////////////////////////////////////////////////////
// This file contains the interface to the chat rendering module.
//////////////////////////////////////////////////////////////////////////////

#ifndef CHATRENDERER_H
#define CHATRENDERER_H

#include "twitchchat.h"

void InitializeChatRenderer(unsigned int windowWidth, unsigned int windowHeight);
void DeinitChatRenderer();

void ProcessEmoticonData(); // Triggers the download of emoticon data from Twitch.

void AddChatUser(const TTV_ChatUserInfo* user);
void RemoveChatUser(const TTV_ChatUserInfo* user);
void UpdateChatUser(const TTV_ChatUserInfo* user);

void AddChatMessages(const TTV_ChatTokenizedMessageList* messageList);
void ClearChatMessages();

void RenderChatText(); // Renders the chat messages to the screen.

void BeginChatInput();
void AppendChatInput(char ch);
void EndChatInput(bool submit);
bool AcceptingChatInput();

#endif
