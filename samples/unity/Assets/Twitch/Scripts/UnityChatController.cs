using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twitch;
using UnityEngine;
using ErrorCode = Twitch.ErrorCode;

namespace Twitch.Chat
{
	public class UnityChatController : ChatController
	{
	    #region Memeber Variables
	
	    [SerializeField]
		protected string m_ClientId = "";
		[SerializeField]
		protected string m_ClientSecret = "";
	
		#endregion
			
		#region Properties

        public override string ClientId
        {
            get { return m_ClientId; }
            set { m_ClientId = value; }
        }

        public override string ClientSecret
        {
            get { return m_ClientSecret; }
            set { m_ClientSecret = value; }
        }
		
		#endregion
        
		#region Unity Overrides
		
        protected void Awake()
	    {
            // force the twitch libraries to be loaded
            Twitch.Broadcast.UnityBroadcastController.LoadTwitchLibraries();

            m_Core = new Core(new StandardCoreAPI());
            m_Chat = new Chat(new StandardChatAPI());
	    }

        protected void OnDestroy()
        {
            Disconnect();

            // force a low-level shutdown
            if (m_Chat != null)
            {
                m_Chat.Shutdown();
            }
        }
		
		public override void Update()
		{
			// for awareness that the Unity Update hook is actually being called
			base.Update();
		}
		
		#endregion
		
		#region Error Handling
		
	    protected override void CheckError(Twitch.ErrorCode err)
	    {
	        if (err != Twitch.ErrorCode.TTV_EC_SUCCESS)
	        {
	            Debug.LogError(err.ToString());
	        }
	    }
	
	    protected override void ReportError(string err)
	    {
	        Debug.LogError(err);
	    }
	
	    protected override void ReportWarning(string err)
	    {
	        Debug.LogError(err);
	    }
		
		#endregion
	}
}
