using UnityEngine;
using System.Collections.Generic;
using System;
using Twitch.Chat;
using ErrorCode = Twitch.ErrorCode;


public class ChatGUI : MonoBehaviour
{
	[SerializeField]
	protected string m_UserName = "";
	[SerializeField]
	protected string m_Password = "";
	[SerializeField]
	protected string m_Channel = "";
	[SerializeField]
    protected Twitch.Broadcast.BroadcastController m_BroadcastController = null; //!< We currently depend on getting the auth token from the BroadcastController
	[SerializeField]
	protected UnityChatController m_ChatController = null;
	
	[SerializeField]
	protected String m_Message = string.Empty; //!< Just a hack to allow entering messages for testing quickly
	
	
	public string UserName
	{
		get { return m_UserName; }
		set { m_UserName = value; }
	}
	
	public string Password
	{
		get { return m_Password; }
		set { m_Password = value; }
	}
	
	public string Channel
	{
		get { return m_Channel; }
		set { m_Channel = value; }
	}
	
	void Start()
	{
		DebugOverlay.CreateInstance();
		
		m_ChatController.MessagesCleared += this.HandleClearMessages;
		m_ChatController.Connected += this.HandleConnected;
		m_ChatController.Disconnected += this.HandleDisconnected;
		m_ChatController.RawMessagesReceived += this.HandleRawMessagesReceived;
		m_ChatController.UsersChanged += this.HandleUsersChanged;
	}
	
	void OnGUI()
	{
		int left = 200;
		int width = 150;
		int height = 30;
		int top = 70;
		int i = 0;
		
		bool connect = false;
		bool connectAnonymous = false;
		bool disconnect = false;
		bool sendMessage = false;
		
		if (m_ChatController.IsInitialized)
		{
			if (m_ChatController.IsConnected)
			{
				if (!m_ChatController.IsAnonymous)
				{
					sendMessage = GUI.Button(new Rect(left,top+height*i++,width,height), "Send Message");
				}
				disconnect = GUI.Button(new Rect(left,top+height*i++,width,height), "Disconnect");
			}
		}
		else
		{
			connect = GUI.Button(new Rect(left,top+height*i++,width,height), "Connect");
			connectAnonymous = GUI.Button(new Rect(left,top+height*i++,width,height), "Connect Anonymously");
		}
		
        if (connect)
		{
			if (string.IsNullOrEmpty(m_Channel.Trim()))
			{
				m_Channel = m_UserName;
			}
			
            m_ChatController.AuthToken = m_BroadcastController.AuthToken;
            m_ChatController.UserName = m_UserName;

			m_ChatController.Connect(m_Channel);
		}
        else if (connectAnonymous)
		{
			if (string.IsNullOrEmpty(m_Channel.Trim()))
			{
				m_Channel = m_UserName;
			}
			
            m_ChatController.AuthToken = m_BroadcastController.AuthToken;

			m_ChatController.ConnectAnonymous(m_Channel);
		}
		else if (disconnect)
		{
            m_ChatController.Disconnect();
		}
		else if (sendMessage)
		{
            m_ChatController.SendChatMessage(m_Message);
			m_Message = string.Empty;
		}
    }
	
	void Update()
	{
		if (DebugOverlay.InstanceExists)
		{
			DebugOverlay.Instance.AddViewportText("Chat: " + m_ChatController.CurrentState.ToString(), 0);
		}
	}
	
	void OnDestroy()
	{
		if (m_ChatController != null)
		{
			m_ChatController.Disconnect();
		}

		m_ChatController.MessagesCleared -= this.HandleClearMessages;
		m_ChatController.Connected -= this.HandleConnected;
		m_ChatController.Disconnected -= this.HandleDisconnected;
		m_ChatController.RawMessagesReceived -= this.HandleRawMessagesReceived;
		m_ChatController.UsersChanged -= this.HandleUsersChanged;
	}
	
    #region Callbacks

    protected void HandleRawMessagesReceived(ChatMessage[] messages)
    {
        for (int i = 0; i < messages.Length; ++i)
        {
            string line = "[" + messages[i].UserName + "] " + messages[i].Message;
            DebugOverlay.Instance.AddViewportText(line);
        }
    }

    protected void HandleUsersChanged(ChatUserInfo[] joins, ChatUserInfo[] leaves, ChatUserInfo[] infoChanges)
    {
        for (int i = 0; i < leaves.Length; ++i)
        {
			DebugOverlay.Instance.AddViewportText(leaves[i].DisplayName + " left");
        }

        for (int i = 0; i < infoChanges.Length; ++i)
        {
			// TODO: if we were displaying user attributes we would update them here
        }

        for (int i = 0; i < joins.Length; ++i)
        {
            DebugOverlay.Instance.AddViewportText(leaves[i].DisplayName + " joined");
        }
    }

    protected void HandleConnected()
    {
		DebugOverlay.Instance.AddViewportText("Connected");
    }

    protected void HandleDisconnected()
    {
        DebugOverlay.Instance.AddViewportText("Disconnected");
    }

    protected void HandleClearMessages()
    {
        DebugOverlay.Instance.AddViewportText("Messages cleared");
    }

    #endregion
}
