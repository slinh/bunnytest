using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twitch;
using Twitch;
using ErrorCode = Twitch.ErrorCode;

namespace Twitch.Chat
{
    /// <summary>
    /// The state machine which manages the chat state.  It provides a high level interface to the SDK libraries.  The ChatController (CC) performs many operations
    /// asynchronously and hides the details.  This can be tweaked if needed but should handle all your chat needs (other than emoticons which may be provided in the future).
    /// 
    /// The typical order of operations a client of CC will take is:
    /// 
    /// - Subscribe for events via delegates on ChatController
    /// - Call CC.Connect() / call CC.ConnectAnonymous()
    /// - Wait for the connection callback 
    /// - Call CC.SendChatMessage() to send messages (if not connected anonymously)
    /// - Receive message callbacks
    /// - Call CC.Disconnect() when done
    /// 
    /// Events will fired during the call to CC.Update().  When chat messages are received RawMessagesReceived will be fired.
    /// 
    /// NOTE: The implementation of texture emoticon data is not yet complete and currently not available.
    /// </summary>
    public abstract partial class ChatController : IChatCallbacks
    {
        #region Types

        /// <summary>
        /// The possible states the ChatController can be in.
        /// </summary>
        public enum ChatState
        {
            Uninitialized,  //!< Chat is not yet initialized.
            Initialized,    //!< The component is initialized.
            Connecting,     //!< Currently attempting to connect to the channel.
            Connected,      //!< Connected to the channel.
            Disconnected    //!< Initialized but not connected.
        }

        /// <summary>
        /// The emoticon parsing mode for chat messages.
        /// </summary>
        public enum EmoticonMode
        {
            None,			//!< Do not parse out emoticons in messages.
            Url, 			//!< Parse out emoticons and return urls only for images.
            TextureAtlas 	//!< Parse out emoticons and return texture atlas coordinates.
        }

        /// <summary>
        /// The callback signature for the event fired when a tokenized set of messages has been received.
        /// </summary>
        /// <param name="messages">The list of messages</param>
        public delegate void TokenizedMessagesReceivedDelegate(ChatTokenizedMessage[] messages);

        /// <summary>
        /// The callback signature for the event fired when a set of text-only messages has been received.
        /// </summary>
        /// <param name="messages">The list of messages</param>
        public delegate void RawMessagesReceivedDelegate(ChatMessage[] messages);

        /// <summary>
        /// The callback signature for the event fired when users join, leave or changes their status in the channel.
        /// </summary>
        /// <param name="joinList">The list of users who have joined the room.</param>
        /// <param name="leaveList">The list of useres who have left the room.</param>
        /// <param name="userInfoList">The list of users who have changed their status.</param>
        public delegate void UsersChangedDelegate(ChatUserInfo[] joinList, ChatUserInfo[] leaveList, ChatUserInfo[] userInfoList);

        /// <summary>
        /// The callback signature for the event fired when the local user has been connected to the channel.
        /// </summary>
        public delegate void ConnectedDelegate();

        /// <summary>
        /// The callback signature for the event fired when the local user has been disconnected from the channel.
        /// </summary>
        public delegate void DisconnectedDelegate();

        /// <summary>
        /// The callback signature for the event fired when the messages in the room should be cleared.  The UI should be cleared of any previous messages.
        /// </summary>
        public delegate void ClearMessagesDelegate();

        /// <summary>
        /// The callback signature for the event fired when the emoticon data has been made available.
        /// </summary>
        public delegate void EmoticonDataAvailableDelegate();

        /// <summary>
        /// The callback signature for the event fired when the emoticon data is no longer valid.
        /// </summary>
        public delegate void EmoticonDataExpiredDelegate();

        #endregion

        #region Memeber Variables
        
        public event TokenizedMessagesReceivedDelegate TokenizedMessagesReceived;
        public event RawMessagesReceivedDelegate RawMessagesReceived;
        public event UsersChangedDelegate UsersChanged;
        public event ConnectedDelegate Connected;
        public event DisconnectedDelegate Disconnected;
        public event ClearMessagesDelegate MessagesCleared;
        public event EmoticonDataAvailableDelegate EmoticonDataAvailable;
        public event EmoticonDataExpiredDelegate EmoticonDataExpired;

        protected Twitch.Core m_Core = null;
        protected Twitch.Chat.Chat m_Chat = null;

        protected string m_UserName = "";
        protected string m_ChannelName = "";

        protected bool m_ChatInitialized = false;
        protected bool m_Anonymous = false;
        protected ChatState m_ChatState = ChatState.Uninitialized;
        protected AuthToken m_AuthToken = new AuthToken();

        protected List<ChatUserInfo> m_ChannelUsers = new List<ChatUserInfo>();
        protected LinkedList<ChatMessage> m_RawMessages = new LinkedList<ChatMessage>();
        protected LinkedList<ChatTokenizedMessage> m_TokenizedMessages = new LinkedList<ChatTokenizedMessage>();
        protected uint m_MessageHistorySize = 128;

        protected EmoticonMode m_EmoticonMode = EmoticonMode.None;
        protected EmoticonMode m_ActiveEmoticonMode = EmoticonMode.None;
        protected ChatEmoticonData m_EmoticonData = null;

        #endregion


        #region IChatCallbacks

        void IChatCallbacks.ChatStatusCallback(ErrorCode result)
        {
            if (Error.Succeeded(result))
            {
                return;
            }

            m_ChatState = ChatState.Disconnected;
        }

        void IChatCallbacks.ChatChannelMembershipCallback(TTV_ChatEvent evt, ChatChannelInfo channelInfo)
        {
            switch (evt)
            {
                case TTV_ChatEvent.TTV_CHAT_JOINED_CHANNEL:
                {
                    m_ChatState = ChatState.Connected;
                    FireConnected();
                    break;
                }
                case TTV_ChatEvent.TTV_CHAT_LEFT_CHANNEL:
                {
                    m_ChatState = ChatState.Disconnected;
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        void IChatCallbacks.ChatChannelUserChangeCallback(ChatUserList joinList, ChatUserList leaveList, ChatUserList userInfoList)
        {
            for (int i=0; i<leaveList.List.Length; ++i)
            {
                int index = m_ChannelUsers.IndexOf(leaveList.List[i]);
                if (index >= 0)
                {
                    m_ChannelUsers.RemoveAt(index);
                }
            }

            for (int i=0; i<userInfoList.List.Length; ++i)
            {
                // this will find the existing user with the same name
                int index = m_ChannelUsers.IndexOf(userInfoList.List[i]);
                if (index >= 0)
                {
                    m_ChannelUsers.RemoveAt(index);
                }

                m_ChannelUsers.Add(userInfoList.List[i]);
            }

            for (int i=0; i<joinList.List.Length; ++i)
            {
                m_ChannelUsers.Add(joinList.List[i]);
            }

            try
            {
                if (UsersChanged != null)
                {
                    this.UsersChanged(joinList.List, leaveList.List, userInfoList.List);
                }
            }
            catch
            {
            }
        }

        void IChatCallbacks.ChatQueryChannelUsersCallback(ChatUserList userList)
        {
            // listening for incremental changes so no need for full query
        }

        void IChatCallbacks.ChatChannelMessageCallback(ChatMessageList messageList)
        {
            for (int i = 0; i < messageList.Messages.Length; ++i)
            {
                m_RawMessages.AddLast(messageList.Messages[i]);
            }

            try
            {
                if (RawMessagesReceived != null)
                {
                    this.RawMessagesReceived(messageList.Messages);
                }
            }
            catch (Exception x)
            {
                ReportError(string.Format("Error in ChatChannelMessageCallback: {0}", x.ToString()));
            }

            // cap the number of messages cached
            while (m_RawMessages.Count > m_MessageHistorySize)
            {
                m_RawMessages.RemoveFirst();
            }
        }

        void IChatCallbacks.ChatChannelTokenizedMessageCallback(ChatTokenizedMessage[] messageList)
        {
            for (int i = 0; i < messageList.Length; ++i)
            {
                m_TokenizedMessages.AddLast(messageList[i]);
            }

            try
            {
                if (TokenizedMessagesReceived != null)
                {
                    this.TokenizedMessagesReceived(messageList);
                }
            }
            catch (Exception x)
            {
                ReportError(string.Format("Error in ChatChannelTokenizedMessageCallback: {0}", x.ToString()));
            }

            // cap the number of messages cached
            while (m_TokenizedMessages.Count > m_MessageHistorySize)
            {
                m_TokenizedMessages.RemoveFirst();
            }
        }

        void IChatCallbacks.ChatClearCallback(string channelName)
        {
	        ClearMessages();
        }

        void IChatCallbacks.EmoticonDataDownloadCallback(ErrorCode error)
        {
            // grab the texture and badge data
            if (Error.Succeeded(error))
            {
                SetupEmoticonData();
            }
        }

        #endregion


        #region Properties

        /// <summary>
        /// Whether or not the controller has been initialized.
        /// </summary>
        public bool IsInitialized
        {
            get { return m_ChatInitialized; }
        }
        
        /// <summary>
        /// Whether or not currently connected to the channel.
        /// </summary>
        public bool IsConnected
        {
            get { return m_ChatState == ChatState.Connected; }
        }

        /// <summary>
        /// Whether or not connected anonymously (listen only).
        /// </summary>
        public bool IsAnonymous
        {
            get { return m_Anonymous; }
        }

        /// <summary>
        /// The AuthToken obtained from using the BroadcastController or some other means.
        /// </summary>
        public AuthToken AuthToken
        {
            get { return m_AuthToken; }
            set { m_AuthToken = value; }
        }

        /// <summary>
        /// The Twitch client ID assigned to your application.
        /// </summary>
        public abstract string ClientId
        {
            get;
            set;
        }

        /// <summary>
        /// The secret code gotten from the Twitch site for the client id.
        /// </summary>
        public abstract string ClientSecret
        {
            get;
            set;
        }

        /// <summary>
        /// The username to log in with.
        /// </summary>
        public string UserName
        {
            get { return m_UserName; }
            set { m_UserName = value; }
        }

        /// <summary>
        /// The maximum number of messages to be kept in the chat history.
        /// </summary>
        public uint MessageHistorySize
        {
            get { return m_MessageHistorySize; }
            set { m_MessageHistorySize = value; }
        }

        /// <summary>
        /// The current state of the ChatController.
        /// </summary>
        public ChatState CurrentState
        {
            get { return m_ChatState; }
        }

        /// <summary>
        /// An iterator for the raw chat messages from oldest to newest.
        /// </summary>
        public LinkedList<ChatMessage>.Enumerator RawMessages
        {
            get { return m_RawMessages.GetEnumerator(); }
        }

        /// <summary>
        /// An iterator for the tokenized chat messages from oldest to newest.
        /// </summary>
        public LinkedList<ChatTokenizedMessage>.Enumerator TokenizedMessages
        {
            get { return m_TokenizedMessages.GetEnumerator(); }
        }

        /// <summary>
        /// The emoticon parsing mode for chat messages.  This must be set before connecting to the channel to set the preference until disconnecting.  
        /// If a texture atlas is selected this will trigger a download of emoticon images to create the atlas.
        /// </summary>
        public EmoticonMode EmoticonParsingMode
        {
            get { return m_EmoticonMode; }
            set { m_EmoticonMode = value; }
        }

        /// <summary>
        /// Retrieves the emoticon data that can be used to render icons.
        /// </summary>
        public ChatEmoticonData EmoticonData
        {
            get { return m_EmoticonData;}
        }

        #endregion

        /// <summary>
        /// Connects to the given channel.  The actual result of the connection attempt will be returned in the Connected / Disconnected event.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <returns>Whether or not the request was successful.</returns>
        public virtual bool Connect(string channel)
        {
            Disconnect();

            m_Anonymous = false;
            m_ChannelName = channel;

            return Initialize(channel);
        }

        /// <summary>
        /// Connects to the given channel anonymously.  The actual result of the connection attempt will be returned in the Connected / Disconnected event.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <returns>Whether or not the request was valid.</returns>
        public virtual bool ConnectAnonymous(string channel)
        {
            Disconnect();

            m_Anonymous = true;
            m_ChannelName = channel;
        
            return Initialize(channel);
        }

        /// <summary>
        /// Disconnects from the channel.  The result of the attempt will be returned in a Disconnected event.
        /// </summary>
        /// <returns>Whether or not the disconnect attempt was valid.</returns>
        public virtual bool Disconnect()
        {
            if (m_ChatState == ChatState.Connected || 
                m_ChatState == ChatState.Connecting)
            {
                ErrorCode ret = m_Chat.Disconnect();
                if (Error.Failed(ret))
                {
                    string err = Error.GetString(ret);
                    ReportError(string.Format("Error disconnecting: {0}", err));
                }

                FireDisconnected();
            }
            else if (m_ChatState == ChatState.Disconnected)
            {
                FireDisconnected();
            }
            else
            {
                return false;
            }

            return Shutdown();
        }

        protected virtual bool Initialize(string channel)
        {
            if (m_ChatInitialized)
            {
                return false;
            }

            ErrorCode ret = m_Core.Initialize(this.ClientId, VideoEncoder.TTV_VID_ENC_DISABLE, null);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error initializing core: {0}", err));

                FireDisconnected();

                return false;
            }

            m_ActiveEmoticonMode = m_EmoticonMode;
            ret = m_Chat.Initialize(channel, m_ActiveEmoticonMode != EmoticonMode.None);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error initializing chat: {0}", err));

                FireDisconnected();

                return false;
            }
            else
            {
                m_Chat.ChatCallbacks = this;
                m_ChatInitialized = true;
                m_ChatState = ChatState.Initialized;

                return true;
            }
        }

        protected virtual bool Shutdown()
        {
            if (m_ChatInitialized)
            {
                ErrorCode ret = m_Chat.Shutdown();
                if (Error.Failed(ret))
                {
                    string err = Error.GetString(ret);
                    ReportError(string.Format("Error shutting down chat: {0}", err));

                    return false;
                }

                ret = m_Core.Shutdown();
                if (Error.Failed(ret))
                {
                    string err = Error.GetString(ret);
                    ReportError(string.Format("Error shutting down core: {0}", err));

                    return false;
                }
            }

            m_ChatState = ChatState.Uninitialized;
            m_ChatInitialized = false;

            CleanupEmoticonData();

            m_Chat.ChatCallbacks = null;

            return true;
        }

        /// <summary>
        /// Periodically updates the internal state of the controller.
        /// </summary>
        public virtual void Update()
        {
            // for stress testing to make sure memory is being passed around properly
            //GC.Collect(); 
        
            if (!m_ChatInitialized)
            {
                return;
            }

	        ErrorCode ret = m_Chat.FlushEvents();
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error flushing chat events: {0}", err));
            }

	        switch (m_ChatState)
	        {
		        case ChatState.Uninitialized:
		        {
			        break;
		        }
		        case ChatState.Initialized:
		        {
                    // connect to the channel
                    if (m_Anonymous)
                    {
                        ret = m_Chat.ConnectAnonymous();
                    }
                    else
                    {
                        ret = m_Chat.Connect(m_UserName, m_AuthToken.Data);
                    }

                    if (Error.Failed(ret))
                    {
                        string err = Error.GetString(ret);
                        ReportError(string.Format("Error connecting: {0}", err));

                        Shutdown();

                        FireDisconnected();
                    }
                    else
                    {
                        m_ChatState = ChatState.Connecting;
                        DownloadEmoticonData();
                    }

			        break;
		        }
                case ChatState.Connecting:
                {
                    break;
                }
                case ChatState.Connected:
                {
                    break;
                }
                case ChatState.Disconnected:
                {
                    Disconnect();
                    break;
                }
	        }
        }

        /// <summary>
        /// Sends a chat message to the channel.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>Whether or not the attempt was valid.</returns>
        public virtual bool SendChatMessage(string message)
        {
            if (m_ChatState != ChatState.Connected)
            {
                return false;
            }

            ErrorCode ret = m_Chat.SendMessage(message);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error sending chat message: {0}", err));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Clears the chat message history.
        /// </summary>
        public virtual void ClearMessages()
        {
            m_RawMessages.Clear();

            try
            {
                if (MessagesCleared != null)
                {
                    this.MessagesCleared();
                }
            }
            catch (Exception x)
            {
                ReportError(string.Format("Error clearing chat messages: {0}", x.ToString()));
            }
        }

        #region Event Helpers

        protected void FireConnected()
        {
            try
            {
                if (Connected != null)
                {
                    this.Connected();
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        protected void FireDisconnected()
        {
            try
            {
                if (Disconnected != null)
                {
                    this.Disconnected();
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        #endregion

        #region Emoticon Handling

        protected virtual void DownloadEmoticonData()
        {
            // don't download emoticons
            if (m_ActiveEmoticonMode == EmoticonMode.None)
            {
                return;
            }

            if (m_EmoticonData == null &&
                m_ChatInitialized)
            {
                ErrorCode ret = m_Chat.DownloadEmoticonData(m_ActiveEmoticonMode == EmoticonMode.TextureAtlas);
                if (Error.Failed(ret))
                {
                    string err = Error.GetString(ret);
                    ReportError(string.Format("Error trying to download emoticon data: {0}", err));
                }
            }
        }

        protected virtual void SetupEmoticonData()
        {
            if (m_EmoticonData != null)
            {
                return;
            }

            ErrorCode ec = m_Chat.GetEmoticonData(out m_EmoticonData);
            if (Error.Succeeded(ec))
            {
                try
                {
                    if (EmoticonDataAvailable != null)
                    {
                        EmoticonDataAvailable();
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                }
            }
            else
            {
                ReportError("Error preparing emoticon data: " + Error.GetString(ec));
            }
        }

        protected virtual void CleanupEmoticonData()
        {
            if (m_EmoticonData == null)
            {
                return;
            }

            m_EmoticonData = null;

            try
            {
                if (EmoticonDataExpired != null)
                {
                    EmoticonDataExpired();
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        #endregion

        #region Error Handling

        protected virtual void CheckError(ErrorCode err)
        {
        }

        protected virtual void ReportError(string err)
        {
        }

        protected virtual void ReportWarning(string err)
        {
        }

        #endregion
    }
}
