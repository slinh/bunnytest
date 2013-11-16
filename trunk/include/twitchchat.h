/********************************************************************************************
* Twitch Broadcasting SDK
*
* This software is supplied under the terms of a license agreement with Justin.tv Inc. and
* may not be copied or used except in accordance with the terms of that agreement
* Copyright (c) 2012-2013 Justin.tv Inc.
*********************************************************************************************/

#ifndef TTVSDK_TWITCH_CHAT_H
#define TTVSDK_TWITCH_CHAT_H

#include "twitchchat/chattypes.h"

#ifdef __cplusplus
extern "C"
{
#endif

/**
 * TTV_ChatInit - Sets the callbacks for receiving chat events.  This may be NULL to stop receiving chat events but this is not recommended
 *                since you may miss connection and disconnection events.
 *
 * @param[in] channelName - The UTF-8 encoded name of the channel to connect to. See #kMaxChatChannelNameLength for details.
 * @param[in] chatCallbacks - The set of callbacks for receiving chat events.
 * @return - TTV_EC_SUCCESS.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_Init(const utf8char* channelName, const TTV_ChatCallbacks* chatCallbacks);

/**
 * TTV_Chat_Shutdown - Tear down the chat subsystem. Be sure to have freed all outstanding lists before calling this.
 *
 * @return - TTV_EC_SUCCESS.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_Shutdown();

/**
 * TTV_ChatConnect - Connects to the chat service.  This is an asynchronous call and notification of 
 *					 connection success or fail will come in the callback.   TTV_Chat_Init should be called first with 
 *                   valid callbacks for receiving connection and disconnection events.  The actual result of the connection attempt will
 *					 come in the statusCallback.
 *
 * @param[in] username - The UTF-8 encoded account username to use for logging in to chat.  See #kMaxChatUserNameLength for details.
 * @param[in] authToken - The auth token for the account.
 * @return - TTV_EC_SUCCESS if the request to connect is valid (does not guarantee connection, wait for a response from statusCallback).
 *			 TTV_EC_CHAT_NOT_INITIALIZED if system not initialized.
 *			 TTV_EC_CHAT_ALREADY_IN_CHANNEL if already in channel.
 *			 TTV_EC_CHAT_LEAVING_CHANNEL if still leaving a channel.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_Connect(const utf8char* username, const char* authToken);

/**
 * TTV_Chat_ConnectAnonymous - Connects to the chat service anonymously allowing chat messages to be received but not sent.  This is an asynchronous 
 *					call and notification of connection success or fail will come in the callback.   TTV_Chat_Init should be called first with 
 *                   valid callbacks for receiving connection and disconnection events.  The actual result of the connection attempt will
 *					 come in the statusCallback.
 *
 * @return - TTV_EC_SUCCESS if the request to connect is valid (does not guarantee connection, wait for a response from statusCallback).
 *			 TTV_EC_CHAT_NOT_INITIALIZED if system not initialized.
 *			 TTV_EC_CHAT_ALREADY_IN_CHANNEL if already in channel.
 *			 TTV_EC_CHAT_LEAVING_CHANNEL if still leaving a channel.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_ConnectAnonymous();

/**
 * TTV_ChatDisconnect - Disconnects from the chat server.  This will automatically remove the user from
 *						all channels that the user is in.  A notification will come in statusCallback to indicate
 *						that the disconnection was successful but you can safely assume this will succeed.
 *
 * @return - TTV_EC_SUCCESS if disconnection successful.
 *			 TTV_EC_CHAT_NOT_INITIALIZED if system not initialized.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_Disconnect();

/**
 * TTV_ChatGetChannelUsers - Retrieves the current users for the named channel.  This is used by both broadcasters and viewers.
 *							 The list returned in the callback must be freed by calling TTV_Chat_FreeUserList when the application is done with it.
 *
 * @param[in] callback - The callback to call with the result of the query.
 * @return - TTV_EC_SUCCESS if function succeeds.
 *			 TTV_EC_CHAT_NOT_IN_CHANNEL if not in the channel.
 *			 TTV_EC_CHAT_NOT_INITIALIZED if system not initialized.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_GetChannelUsers(TTV_ChatQueryChannelUsersCallback callback);

/**
 * TTV_Chat_SendMessage - Sends the given message to the channel.  The user must have joined the channel first.  
 *						  This is used by both broadcasters and viewers.
 *
 *						  The game/user may also send some commands using a message to take some action in the channel.  The valid commands are:
 * 
 *						  "/disconnect":			Disconnects from the chat channel.
 *						  "/commercial":			Make all viewers who normally watch commercials see a commercial right now.
 *						  "/mods":					Get a list of all of the moderators in the current channel.
 *						  "/mod <username>":		Grant moderator status to the given user.
 *						  "/unmod <username>":		Revoke moderator status from the given user.
 *						  "/ban: <username>":		Ban the given user from your channel.
 *						  "/unban <username>":		Lift a ban or a time-out that has been given to the given user.
 *						  "/clear":					Clear the current chat history.  This clears the chat room for all viewers.
 *						  "/timeout <username>":	Give a time-out to the given user.
 *						  "/subscribers":			Turn on subscribers-only mode, which keeps people who have not purchased channel subscriptions to this channel from talking in chat.
 *						  "/subscribersoff":		Turn off subscribers-only mode.
 *						  "/slow <interval>":		Require that chatters wait <interval> seconds between lines of chat.
 *						  "/slowoff":				Don't require that chatters wait between lines of chat anymore.
 *						  "/fastsubs <on|off>":		Makes subscribers exempt from /slow.
 *						  "/me":					Speak in the third person. Ex: /me want smash -> <my_username> want smash.  The entire message should also be colored with the user color.
 *						  "/color":					Change the color of your username.
 *						  "/ignore <username>":		Ignores the named user.
 *						  "/unignore <username>":	Unignores the named user.
 *
 * @param[in] message - The UTF-8 encoded message to send to the channel.
 * @return - TTV_EC_SUCCESS if function succeeds.
 *			 TTV_EC_CHAT_NOT_INITIALIZED if system not initialized.
 *			 TTV_EC_CHAT_ANON_DENIED if connected anonymously.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_SendMessage(const utf8char* message);

/**
 * TTV_ChatFlushEvents - Calls callbacks for all events which has accumulated since the last flush.  This include connects, disconnects,  
 *						 user changes and received messages.
 *
 * @return - TTV_EC_SUCCESS if function succeeds.
 *			 TTV_EC_CHAT_NOT_INITIALIZED if system not initialized.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_FlushEvents();

/**
 * TTV_Chat_FreeUserList - Frees the memory for the given user list which was passed to the application during a callback.
 *
 * @param[in] userList - The user list to free.
 * @return - TTV_EC_SUCCESS if function succeeds.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_FreeUserList(const TTV_ChatUserList* userList);

/**
 * TTV_Chat_FreeTokenizedMessageList - Frees the memory allocated from by TTV_ChatChannelTokenizedMessageCallback.
 *
 * @param[in] tokenizedMessageList - The list to free.
 * @return - TTV_EC_SUCCESS if function succeeds, TTV_EC_INVALID_ARG if the list is not expected.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_FreeTokenizedMessageList(const TTV_ChatTokenizedMessageList* tokenizedMessageList);

/**
 * TTV_Chat_DownloadEmoticonData - Initiates a download of the emoticon data.  This will trigger a redownload if called a second time.  The callback will be called
 *								   to indicate the success of the download.  Call TTV_Chat_GetEmoticonDatato retrieve the data after it has 
 *								   been downloaded.
 *
 * @param[in] createTextureAtlas - Whether or not to download images and create a texture atlas.
 * @param[in] callback - The callback to call when the emoticon data has finished downloading and is prepared for use.
 * @param[in] userdata - The userdata to pass back in the callback.
 * @return - TTV_EC_SUCCESS if function succeeds
 *			 TTV_EC_CHAT_EMOTICON_DATA_DOWNLOADING if the data is still downloading.
 *			 TTV_EC_CHAT_EMOTICON_DATA_LOCKED if the data has been locked by a call to TTV_Chat_GetEmoticonData and has not yet been freed by TTV_Chat_FreeEmoticonData.
 *			 TTV_EC_INVALID_ARG if an invalid callback.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_DownloadEmoticonData(bool createTextureAtlas, TTV_EmoticonDataDownloadCallback callback, void* userdata);

/**
 * TTV_Chat_GetEmoticonData - Retrieves the texture information and badge info after it has been downloaded and prepared. When done with this data be sure 
 *							  to call TTV_Chat_FreeEmoticonData to free the memory.  Initiate the download by calling TTV_Chat_DownloadEmoticonData.
 *
 * @param[out] data - The emoticon information that will be returned.
 * @return - TTV_EC_SUCCESS if function succeeds.
 *			 TTV_EC_CHAT_EMOTICON_DATA_DOWNLOADING if the data is still downloading.
 *			 TTV_EC_CHAT_EMOTICON_DATA_NOT_READY if the data is not yet ready to be retrieved.  
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_GetEmoticonData(TTV_ChatEmoticonData** data);

/**
 * TTV_Chat_FreeEmoticonData - Frees the data previously obtained from TTV_Chat_GetEmoticonData.
 *
 * @param[in] data - The data to be freed.
 * @return - TTV_EC_SUCCESS if function succeeds.
 *			 TTV_EC_INVALID_ARG if not valid data.
 */
TTVSDK_API TTV_ErrorCode TTV_Chat_FreeEmoticonData(TTV_ChatEmoticonData* data);


#ifdef __cplusplus
}
#endif

#endif	/* TTVSDK_TWITCH_CHAT_H */
