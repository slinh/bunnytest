using UnityEngine;
using System.Collections.Generic;
using System;
using Twitch;
using Twitch.Broadcast;
using ErrorCode = Twitch.ErrorCode;


public class BroadcastGUI : MonoBehaviour
{
	[SerializeField]
	protected string m_UserName = "";
	[SerializeField]
	protected string m_Password = "";
    [SerializeField]
    protected bool m_CalculateParamsFromBitrate = true;
    [SerializeField]
	protected int m_BroadcastWidth = 512;
	[SerializeField]
	protected int m_BroadcastHeight = 512;
	[SerializeField]
	protected int m_BroadcastFramesPerSecond = 480;
	[SerializeField]
    protected int m_TargetBitrate = 2000; //!< kbps
	[SerializeField]
	protected float m_BroadcastAspectRatio = 1024f/768f;
	[SerializeField]
    protected float m_BroadcastBitsPerPixel = 0.1f;

    protected UnityBroadcastController m_BroadcastController = null;
    protected IngestTester m_IngestTester = null;
    protected bool m_GamePaused = false;


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

    public bool CalculateParamsFromBitrate
    {
        get { return m_CalculateParamsFromBitrate; }
        set { m_CalculateParamsFromBitrate = value; }
    }

    public int BroadcastWidth
    {
        get { return m_BroadcastWidth; }
        set { m_BroadcastWidth = value; }
    }

    public int BroadcastHeight
    {
        get { return m_BroadcastHeight; }
        set { m_BroadcastHeight = value; }
    }

    public int BroadcastFramesPerSecond
    {
        get { return m_BroadcastFramesPerSecond; }
        set { m_BroadcastFramesPerSecond = value; }
    }

    public int TargetBitrate
    {
        get { return m_TargetBitrate; }
        set { m_TargetBitrate = value; }
    }

    public float BroadcastAspectRatio
    {
        get { return m_BroadcastAspectRatio; }
        set { m_BroadcastAspectRatio = value; }
    }

    public float BroadcastBitsPerPixel
    {
        get { return m_BroadcastBitsPerPixel; }
        set { m_BroadcastBitsPerPixel = value; }
    }
	
	
	void Start()
	{
		Application.runInBackground = true;
		
		DebugOverlay.CreateInstance();

        m_BroadcastController = this.gameObject.GetComponent<UnityBroadcastController>();

		m_BroadcastController.AuthTokenRequestComplete += this.HandleAuthTokenRequestComplete;
		m_BroadcastController.LoginAttemptComplete += this.HandleLoginAttemptComplete;
        m_BroadcastController.GameNameListReceived += this.HandleGameNameListReceived;
        m_BroadcastController.BroadcastStateChanged += this.HandleBroadcastStateChanged;
        m_BroadcastController.LoggedOut += this.HandleLoggedOut;
        m_BroadcastController.StreamInfoUpdated += this.HandleStreamInfoUpdated;
        m_BroadcastController.FrameSubmissionIssue += this.HandleFrameSubmissionIssue;
        m_BroadcastController.IngestListReceived += this.HandleIngestListReceived;

        if (!m_BroadcastController.IsPlatformSupported)
        {
            DebugOverlay.Instance.AddViewportText("Operating system does not support broadcasting: " + System.Environment.OSVersion.ToString(), -1);
        }
    }
	
	void OnGUI()
	{
		int width = 150;
		int height = 30;
		int top = 70;
		int i = 0;
		
		bool init = false;
		bool shutdown = false;
		bool requestAuthToken = false;
		bool setAuthToken = false;
		bool logOut = false;
		bool start = false;
		bool stop = false;
		bool pause = false;
		bool resume = false;
        bool runCommercial = false;
        bool startIngestTest = false;
        bool skipIngestServer = false;
        bool cancelIngestTest = false;
        bool gamePause = false;
        bool gameResume = false;
        bool loadNextScene = false;
        bool enterFullScreen = false;
        bool exitFullScreen = false;
		
		if (m_BroadcastController.IsInitialized)
		{
            if (m_BroadcastController.IsBroadcasting)
			{
				runCommercial = GUI.Button(new Rect(10,top+height*i++,width,height), "Run Commercial");
				
				if (m_BroadcastController.IsPaused)
				{
					resume = GUI.Button(new Rect(10,top+height*i++,width,height), "Resume");
				}
				else
				{
					pause = GUI.Button(new Rect(10,top+height*i++,width,height), "Pause");
				}
				
				stop = GUI.Button(new Rect(10,top+height*i++,width,height), "Stop");
			}
			else
			{
				if (m_BroadcastController.IsLoggedIn)
				{
					if (m_BroadcastController.IsReadyToBroadcast)
					{
                        start = GUI.Button(new Rect(10, top + height * i++, width, height), "Start");
                        startIngestTest = GUI.Button(new Rect(10, top + height * i++, width, height), "Start Ingest Test");
						logOut = GUI.Button(new Rect(10,top+height*i++,width,height), "Log Out");
					}
                    else if (m_BroadcastController.IsIngestTesting)
                    {
                        skipIngestServer = GUI.Button(new Rect(10, top + height * i++, width, height), "Skip Server");
                        cancelIngestTest = GUI.Button(new Rect(10, top + height * i++, width, height), "Cancel Ingest Test");
                    }
				}
				else
				{
					requestAuthToken = GUI.Button(new Rect(10,top+height*i++,width,height), "Request Auth Token");
					setAuthToken = GUI.Button(new Rect(10,top+height*i++,width,height), "Use Existing Auth Token");
				}

                if (!m_BroadcastController.IsIngestTesting)
                {
                    shutdown = GUI.Button(new Rect(10, top + height * i++, width, height), "Shutdown");
                }
			}
		}
		else
		{
			init = GUI.Button(new Rect(10,top+height*i++,width,height), "Init");
		}

        if (m_GamePaused)
        {
            gameResume = GUI.Button(new Rect(10, top + height * i++, width, height), "Resume Game");
        }
        else
        {
            gamePause = GUI.Button(new Rect(10, top + height * i++, width, height), "Pause Game");
        }

        loadNextScene = GUI.Button(new Rect(10, top + height * i++, width, height), "Load Next Scene");

        if (Screen.fullScreen)
        {
            exitFullScreen = GUI.Button(new Rect(10, top + height * i++, width, height), "Exit Full Screen");
        }
        else
        {
            enterFullScreen = GUI.Button(new Rect(10, top + height * i++, width, height), "Go Full Screen");
        }

        if (init)
		{
            if (!m_BroadcastController.Initialize())
            {
                DebugOverlay.Instance.AddViewportText("Error initializing Twitch", 2);
            }
		}
        else if (shutdown)
		{
            m_BroadcastController.Shutdown();
		}
        else if (start)
		{
			VideoParams videoParams = null;

            if (m_CalculateParamsFromBitrate)
			{
                videoParams = m_BroadcastController.GetRecommendedVideoParams((uint)m_TargetBitrate, (uint)m_BroadcastFramesPerSecond, m_BroadcastBitsPerPixel, m_BroadcastAspectRatio);
			}
			else
			{
                videoParams = m_BroadcastController.GetRecommendedVideoParams((uint)m_BroadcastWidth, (uint)m_BroadcastHeight, (uint)m_BroadcastFramesPerSecond);
            }

            if (!m_BroadcastController.StartBroadcasting(videoParams))
			{
				return;
			}
		}
        else if (requestAuthToken)
		{
            m_BroadcastController.RequestAuthToken(m_UserName, m_Password);
		}
        else if (setAuthToken)
		{
			string token = PlayerPrefs.GetString(s_TwitchAuthTokenKey, null);
			string username = PlayerPrefs.GetString(s_TwitchUserNameKey, null);
			
			if (token != null && username != null)
			{
            	m_BroadcastController.SetAuthToken(username, new AuthToken(token));
			}
		}
        else if (logOut)
		{
            m_BroadcastController.Logout();
		}
        else if (stop)
		{
            m_BroadcastController.StopBroadcasting();
		}
        else if (pause)
		{
            m_BroadcastController.PauseBroadcasting();
		}
        else if (resume)
		{
            m_BroadcastController.ResumeBroadcasting();
		}
        else if (runCommercial)
        {
            m_BroadcastController.RunCommercial();
        }
        else if (startIngestTest)
        {
            m_IngestTester = m_BroadcastController.StartIngestTest();
            if (m_IngestTester != null)
            {
                m_IngestTester.OnTestStateChanged += OnIngestTesterStateChanged;
            }
        }
        else if (skipIngestServer)
        {
            m_BroadcastController.IngestTester.SkipCurrentServer();
        }
        else if (cancelIngestTest)
        {
            m_BroadcastController.IngestTester.Cancel();
        }

        if (gamePause)
        {
            m_GamePaused = true;
            Time.timeScale = 0;
        }
        else if (gameResume)
        {
            m_GamePaused = false;
            Time.timeScale = 1;
        }
        else if (loadNextScene)
        {
            string scene = "SampleScene";
            if (Application.loadedLevelName == "SampleScene")
            {
                scene = "SampleScene2";
            }
            Application.LoadLevel(scene);
        }
        else if (enterFullScreen)
        {
            Screen.SetResolution(1080, 1024, true);
        }
        else if (exitFullScreen)
        {
            Screen.SetResolution(1080, 1024, false);
        }
    }
	
	void Update()
	{
		if (DebugOverlay.InstanceExists)
		{
            DebugOverlay.Instance.AddViewportText("Broadcast: " + m_BroadcastController.CurrentState.ToString(), 0);

            if (m_BroadcastController.IngestTester != null)
            {
                IngestTester tester = m_BroadcastController.IngestTester;
                string str = "[" + (int)(tester.TotalProgress * 100) + "%] " + tester.State.ToString();
                if (tester.CurrentServer != null)
                {
                    str += ": " + tester.CurrentServer.ServerName + "... " + tester.CurrentServer.BitrateKbps + " kbps [" + (int)(tester.ServerProgress * 100) + "%]";
                }
                DebugOverlay.Instance.AddViewportText(str, 0);
            }
		}
	}
	
	void OnDestroy()
	{
		if (m_BroadcastController.IsInitialized)
		{
			m_BroadcastController.Shutdown();
		}

		m_BroadcastController.AuthTokenRequestComplete -= HandleAuthTokenRequestComplete;
		m_BroadcastController.LoginAttemptComplete -= HandleLoginAttemptComplete;
        m_BroadcastController.GameNameListReceived -= this.HandleGameNameListReceived;
        m_BroadcastController.BroadcastStateChanged -= this.HandleBroadcastStateChanged;
        m_BroadcastController.LoggedOut -= this.HandleLoggedOut;
        m_BroadcastController.StreamInfoUpdated -= this.HandleStreamInfoUpdated;
        m_BroadcastController.FrameSubmissionIssue -= this.HandleFrameSubmissionIssue;
        m_BroadcastController.IngestListReceived -= this.HandleIngestListReceived;
    }
	
	#region Callbacks
	
	protected static readonly string s_TwitchAuthTokenKey = "Twitch.AuthToken";
	protected static readonly string s_TwitchUserNameKey = "Twitch.UserName";
	
	protected void HandleAuthTokenRequestComplete(Twitch.ErrorCode result, AuthToken authToken)
	{
		if (Twitch.Error.Succeeded(result))
		{
			PlayerPrefs.SetString(s_TwitchAuthTokenKey, authToken.Data);
			PlayerPrefs.SetString(s_TwitchUserNameKey, m_BroadcastController.UserName);
		
			DebugOverlay.Instance.AddViewportText("User authenticated", 2);
		}
		else
		{
			PlayerPrefs.DeleteKey(s_TwitchAuthTokenKey);
			PlayerPrefs.DeleteKey(s_TwitchUserNameKey);
		
			DebugOverlay.Instance.AddViewportText("Failed to authenticate", 2);
		}
		
		PlayerPrefs.Save();
	}
	
	protected void HandleLoginAttemptComplete(Twitch.ErrorCode result)
	{
		if (Twitch.Error.Succeeded(result))
		{
			DebugOverlay.Instance.AddViewportText("Logged in, ready to stream", 2);
		}
		else
		{
			PlayerPrefs.DeleteKey(s_TwitchAuthTokenKey);
			PlayerPrefs.Save();
		
			DebugOverlay.Instance.AddViewportText("AuthToken invalid, please enter your username and password again", 2);
		}
	}

    protected void HandleGameNameListReceived(Twitch.ErrorCode result, GameInfo[] list)
    {
        // TODO: handle the list of games
    }

    protected void HandleBroadcastStateChanged(BroadcastController.BroadcastState state)
    {
    }

    protected void HandleLoggedOut()
    {
        if (DebugOverlay.Instance != null)
        {
            DebugOverlay.Instance.AddViewportText("Logged out", 2);
        }
    }

    protected void HandleStreamInfoUpdated(StreamInfo streamInfo)
    {
        // TODO: update your UI based on the current number of viewers
    }

    protected void HandleIngestListReceived(IngestList list)
    {
        // TODO: populate your list box with them
    }

    protected void HandleFrameSubmissionIssue(ErrorCode result)
    {
        // if you are receiving TTV_WRN_QUEUELENGTH then it's possible the bitrate is too high for the user's internet connection
        DebugOverlay.Instance.AddViewportText("FrameSubmissionIssue: " + result.ToString(), 1);
    }

    protected void OnIngestTesterStateChanged(IngestTester source, IngestTester.TestState state)
    {
        switch (state)
        {
            case IngestTester.TestState.Finished:
            case IngestTester.TestState.Cancelled:
            {
                m_IngestTester.OnTestStateChanged -= OnIngestTesterStateChanged;

                // use the best server based on kbps
                m_BroadcastController.IngestServer = m_BroadcastController.IngestList.BestServer;

                if (m_BroadcastController.IngestServer != null)
                {
                    DebugOverlay.Instance.AddViewportText("Selected Best Ingest Server: " + m_BroadcastController.IngestServer.ServerName, 2);
                }

                break;
            }
        }
    }
	#endregion
}
