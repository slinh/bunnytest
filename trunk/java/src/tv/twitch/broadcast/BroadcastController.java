package tv.twitch.broadcast;

import java.util.*;

import tv.twitch.*;


/**
 * The state machine which manages the broadcasting state.  It provides a high level interface to the SDK libraries.  The BroadcastController (BC) performs many operations
 * asynchronously and hides the details.  
 * 
 * Events will be fired during the call to BC.Update().
 * 
 * The typical order of operations a client of BC will take is:
 * 
 * - Call BC.InitializeTwitch()
 * - Call BC.RequestAuthToken() / Call BC.SetAuthToken()
 * - Determine the server to use for broadcasting
 * - Call BC.StartBroadcasting()
 * - Submit frames (this is done differently depending on platform), see platform-specific documentation for details
 * - Call BC.StopBroadcasting()
 * - Call BC.ShutdownTwitch()
 * 
 * When setting up the VideoParams for the broadcast you should use the bitrate version of GetRecommendedVideoParams.  This will setup the resolution and other parameters
 * based on your connection to the server.  The other version is more explicit and may be useful for advanced users and testing but is generally more confusing and produces
 * poorer results for the typical user who doesn't understand the settings.  We've found that many users just crank up the settings and don't understand why their 
 * broadcast doesn't look that great or the stream drops (network backup).  Simply giving the users a slider for maximum bitrate generally produces better visual quality.
 * In a more advanced integration the bitrate can actually be determined by ingest testing (see below).
 * 
 * The ingest server to use for broadcasting can be configured via the IngestServer property.  The list of servers can be retrieved from the IngestServers property.
 * Normally, the default server will be adequate and sufficiently close for decent broadcasting.  However, to be sure, you can perform ingest testing which will determine 
 * the connection speeds to all the Twitch servers.  This will help both in determining the best server to use (the server with the highest connection) and the actual bitrate 
 * to use when calculating the optimal VideoParams for that server. 
 * 
 * Ingest testing.  This can be done by using the IngestTester class.  After logging into the BC, ingest testing can be performed by calling BC.StartIngestTest() which is only 
 * available in the ReadyForBroadcasting state.  This returns an instance of IngestTester which is a single-use instance.  This class will perform a test which will measure 
 * the connection speed to each of the Twitch ingest servers.  See the documentation of the class for details.  While the test is underway the BC is unavailable for any operations.
 */
public class BroadcastController implements IStreamCallbacks, IStatCallbacks
{
    //region Types

    public enum BroadcastState
    {
        Uninitialized,          //!< InitializeTwitch not called.
        Initialized,            //!< InitializeTwitch has been called.
        Authenticating,         //!< Requesting an AuthToken.
        Authenticated,          //!< Have an AuthToken (not necessarily a valid one).
        LoggingIn,              //!< Waiting to see if the AuthToken is valid).
        LoggedIn,               //!< AuthToken is valid.
        FindingIngestServer,    //!< Determining which server we can broadcast to.
        ReceivedIngestServers,  //!< Received the list of ingest servers.
        ReadyToBroadcast,       //!< Idle and ready to broadcast.
        Starting,               //!< Processing a request to start broadcasting.
        Broadcasting,           //!< Currently broadcasting.
        Stopping,               //!< Processing a request to stop broadcasting.
        Paused,                 //!< Broadcasting but paused.
        IngestTesting           //!< Running the ingest tester.
    }
	
    public interface Listener
    {
    	/**
    	 * The callback signature for the event fired when a request for an auth token is complete.
    	 * @param result
    	 * @param authToken
    	 */
		void onAuthTokenRequestComplete(ErrorCode result, AuthToken authToken);
		
		/**
		 * The callback signature for the event which is fired when an attempt to login is complete.
		 * @param result
		 */
	    void onLoginAttemptComplete(ErrorCode result);
	    
	    /**
	     * The callback signature for the event which is fired when a game name list request is complete.
	     * @param result
	     * @param list
	     */
	    void onGameNameListReceived(ErrorCode result, GameInfo[] list);
	    
	    /**
	     * The callback signature for the event which is fired when the BroadcastController changes state.
	     * @param state
	     */
	    void onBroadcastStateChanged(BroadcastState state);
	    
	    /**
	     * The callback signature for the event which is fired when the user is logged out.
	     */
	    void onLoggedOut();
	    
	    /**
	     * The callback signature for the event which is fired when the stream info is updated.
	     * @param info
	     */
        void onStreamInfoUpdated(StreamInfo info);
        
        /**
         * The callback signature for the event which is fired when the ingest list is updated.
         * @param list
         */
        void onIngestListReceived(IngestList list);
        
        /**
         * The callback signature for the event which is fired when there was an issue submitting frames to the SDK for encoding.
         * @param result
         */
        void onframeSubmissionIssue(ErrorCode result);
        
        /**
         * The callback signature for the event which is fired when broadcasting begins.
         */
        void onBroadcastStarted();
        
        /**
         * The callback signature for the event which is fired when broadcasting ends.
         */
        void onBroadcastStopped();
    }
    
    //endregion


    //region Constants
    
    protected final int s_StreamInfoUpdateInterval = 30; 	//!< Update the stream info every 30 seconds.
    protected final int s_NumSdkBuffers = 3; 				//!< The number of buffers required to submit to the SDK.
    
    //endregion
    
    //region Member Variables

    protected Listener m_Listener = null;
	
    protected String m_ClientId = "";
    protected String m_ClientSecret = "";
    protected String m_DllPath = "";
    protected boolean m_EnableAudio = true;

    protected Core m_Core = null;
    protected Stream m_Stream = null;
    protected List<FrameBuffer> m_CaptureBuffers = new ArrayList<FrameBuffer>();
    protected List<FrameBuffer> m_FreeBufferList = new ArrayList<FrameBuffer>();

    protected boolean m_SdkInitialized = false;    //!< Has Stream.Initialize() been called?
    protected boolean m_LoggedIn = false;          //!< The AuthToken as been validated and can be used for calls to the server.
    protected boolean m_ShuttingDown = false;      //!< The controller is currently shutting down.

    protected BroadcastState m_BroadcastState = BroadcastState.Uninitialized;

    protected String m_UserName = null;
    protected VideoParams m_VideoParams = null;         //!< The VideoParams currently in use.
    protected AudioParams m_AudioParams = null;         //!< The AudioParams currently in use.

    protected IngestList m_IngestList = new IngestList( new IngestServer[0] );
    protected IngestServer m_IngestServer = null;
    protected AuthToken m_AuthToken = new AuthToken();
    protected ChannelInfo m_ChannelInfo = new ChannelInfo();
    protected UserInfo m_UserInfo = new UserInfo();
    protected StreamInfo m_StreamInfo = new StreamInfo();
    protected ArchivingState m_ArchivingState = new ArchivingState();

    protected long m_LastStreamInfoUpdateTime = 0;
    protected IngestTester m_IngestTester = null;
    
    //endregion


    //region IStreamCallbacks
    
    public void requestAuthTokenCallback(ErrorCode result, AuthToken authToken)
    {
        if (ErrorCode.succeeded(result))
        {
            // Now that the user is authorized the information can be requested about which server to stream to
            m_AuthToken = authToken;
            setBroadcastState(BroadcastState.Authenticated);
        }
        else
        {
            m_AuthToken.data = "";
            setBroadcastState(BroadcastState.Initialized);

            String err = ErrorCode.getString(result);
            reportError(String.format("RequestAuthTokenDoneCallback got failure: %s", err));
        }
		
        try
        {
            if (m_Listener != null)
            {
            	m_Listener.onAuthTokenRequestComplete(result, authToken);
            }
        }
        catch (Exception x)
        {
            reportError(x.toString());
        }
    }

    public void loginCallback(ErrorCode result, ChannelInfo channelInfo)
    {
        if (ErrorCode.succeeded(result))
        {
            m_ChannelInfo = channelInfo;
            setBroadcastState(BroadcastState.LoggedIn);
            m_LoggedIn = true;
        }
        else
        {
            setBroadcastState(BroadcastState.Initialized);
            m_LoggedIn = false;

            String err = ErrorCode.getString(result);
            reportError(String.format("LoginCallback got failure: %s", err));
        }
		
        try
        {
            if (m_Listener != null)
            {
            	m_Listener.onLoginAttemptComplete(result);
            }
        }
        catch (Exception x)
        {
            reportError(x.toString());
        }
    }

    public void getIngestServersCallback(ErrorCode result, IngestList ingestList)
    {
        if (ErrorCode.succeeded(result))
        {
            m_IngestList = ingestList;

            // assume we're going to use the default ingest server unless overridden by the client
            m_IngestServer = m_IngestList.getDefaultServer();

            setBroadcastState(BroadcastState.ReceivedIngestServers);
            
            try
            {
                if (m_Listener != null)
                {
                	m_Listener.onIngestListReceived(ingestList);
                }
            }
            catch (Exception x)
            {
                reportError(x.toString());
            }
        }
        else
        {
            String err = ErrorCode.getString(result);
            reportError(String.format("IngestListCallback got failure: %s", err));

            // try again
            setBroadcastState(BroadcastState.LoggingIn);
        }
    }

    public void getUserInfoCallback(ErrorCode result, UserInfo userInfo)
    {
        m_UserInfo = userInfo;

        if (ErrorCode.failed(result))
        {
            String err = ErrorCode.getString(result);
            reportError(String.format("UserInfoDoneCallback got failure: %s", err));
        }
    }

    public void getStreamInfoCallback(ErrorCode result, StreamInfo streamInfo)
    {
        if (ErrorCode.succeeded(result))
        {
            m_StreamInfo = streamInfo;

            try
            {
                if (m_Listener != null)
                {
                	m_Listener.onStreamInfoUpdated(streamInfo);
                }
            }
            catch (Exception x)
            {
                reportError(x.toString());
            }
        }
        else
        {
            //String err = ErrorCode.getString(result);
            //reportWarning(String.Format("StreamInfoDoneCallback got failure: {0}", err));
        }
    }

    public void getArchivingStateCallback(ErrorCode result, ArchivingState state)
    {
        m_ArchivingState = state;

        if (ErrorCode.failed(result))
        {
            //String err = ErrorCode.getString(result);
            //reportWarning(String.Format("ArchivingStateDoneCallback got failure: {0}", err));
        }
    }
    
    public void runCommercialCallback(ErrorCode result)
    {
        if (ErrorCode.failed(result))
        {
            String err = ErrorCode.getString(result);
            reportWarning(String.format("RunCommercialCallback got failure: %s", err));
        }
    }

    public void setStreamInfoCallback(ErrorCode result)
    {
        if (ErrorCode.failed(result))
        {
            String err = ErrorCode.getString(result);
            reportWarning(String.format("SetStreamInfoCallback got failure: %s", err));
        }
    }

    public void getGameNameListCallback(ErrorCode result, GameInfoList list)
    {
        if (ErrorCode.failed(result))
        {
            String err = ErrorCode.getString(result);
            reportError(String.format("GameNameListCallback got failure: %s", err));
        } 
        
        try
        {
            if (m_Listener != null)
            {
            	m_Listener.onGameNameListReceived(result, list == null ? new GameInfo[0] : list.list);
            }
        }
        catch (Exception x)
        {
            reportError(x.toString());
        }
    }

    public void bufferUnlockCallback(long address)
    {
    	FrameBuffer buffer = FrameBuffer.lookupBuffer(address);
    	
        // Put back on the free list
        m_FreeBufferList.add(buffer);
    }

    public void startCallback(ErrorCode ret)
    {
        if (ErrorCode.succeeded(ret))
        {
            try
            {
                if (m_Listener != null)
                {
                    m_Listener.onBroadcastStarted();
                }
            }
            catch (Exception x)
            {
                reportError(x.toString());
            } 
            
            setBroadcastState(BroadcastState.Broadcasting);
        }
        else
        {
            m_VideoParams = null;
            m_AudioParams = null;
            
            setBroadcastState(BroadcastState.ReadyToBroadcast);

            String err = ErrorCode.getString(ret);
            reportError(String.format("startCallback got failure: %s", err));
        }
    }

    public void stopCallback(ErrorCode ret)
    {
        if (ErrorCode.succeeded(ret))
        {
            m_VideoParams = null;
            m_AudioParams = null;

            cleanupBuffers();

            try
            {
                if (m_Listener != null)
                {
                	m_Listener.onBroadcastStopped();
                }
            }
            catch (Exception x)
            {
                reportError(x.toString());
            }

            if (m_LoggedIn)
            {
                setBroadcastState(BroadcastState.ReadyToBroadcast);
            }
            else
            {
                setBroadcastState(BroadcastState.Initialized);
            }
        }
        else
        {
        	// there's not really a good state to go into here
        	setBroadcastState(BroadcastState.ReadyToBroadcast);
        	
            String err = ErrorCode.getString(ret);
            reportError(String.format("stopCallback got failure: %s", err));        	
        }
    }
    
    public void sendActionMetaDataCallback(ErrorCode ret)
    {
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("sendActionMetaDataCallback got failure: %s", err));
        }
    }
    
    public void sendStartSpanMetaDataCallback(ErrorCode ret)
    {
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("sendStartSpanMetaDataCallback got failure: %s", err));
        }
    }
    
    public void sendEndSpanMetaDataCallback(ErrorCode ret)
    {
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("sendEndSpanMetaDataCallback got failure: %s", err));
        }
    }

    //endregion

    
    //region IStatCallbacks
    
    public void statCallback(StatType type, long data)
    {
    }
    
    //endregion

    //region Properties

    public Listener getListener()
    {
    	return m_Listener;
    }
    public void setListener(Listener listener)
    {
    	m_Listener = listener;
    }

    public boolean getIsInitialized()
    {
        return m_SdkInitialized;
    }

    /**
     * The Twitch client ID assigned to your application.
     */
    public String getClientId()
    {
        return m_ClientId;
    }
    /**
     * The Twitch client ID assigned to your application.
     */
    public void setClientId(String value)
    {
     	m_ClientId = value;
    }

    public String getClientSecret()
    {
        return m_ClientSecret;
    }
    public void setClientSecret(String value)
    {
        m_ClientSecret = value;
    }

    public String getUserName()
    {
        return m_UserName;
    }

    public boolean getEnableAudio()
    {
        return m_EnableAudio;
    }
    public void setEnableAudio(boolean value)
    {
        m_EnableAudio = value;
    }

    public BroadcastState getCurrentState()
    {
        return m_BroadcastState;
    }

    public ArchivingState getArchivingState()
    {
        return m_ArchivingState;
    }

    public AuthToken getAuthToken()
    {
        return m_AuthToken;
    }

    public StreamInfo getStreamInfo()
    {
        return m_StreamInfo;
    }

    public UserInfo getUserInfo()
    {
        return m_UserInfo;
    }

    public ChannelInfo getChannelInfo()
    {
        return m_ChannelInfo;
    }

    public boolean getIsBroadcasting()
    {
        return m_BroadcastState == BroadcastState.Broadcasting || m_BroadcastState == BroadcastState.Paused;
    }

    public boolean getIsReadyToBroadcast()
    {
        return m_BroadcastState == BroadcastState.ReadyToBroadcast;
    }

    public boolean getIsIngestTesting()
    {
        return m_BroadcastState == BroadcastState.IngestTesting;
    }

    public boolean getIsPaused()
    {
        return m_BroadcastState == BroadcastState.Paused;
    }

    public boolean getIsLoggedIn()
    {
        return m_LoggedIn;
    }

    public boolean getHaveAuthToken()
    {
        return m_AuthToken != null && m_AuthToken.getIsValid();
    }

    /**
     * The currently configured ingest server to broadcast to.  This can only be set before beginning a broadcast and must be from the IngestList.
     * @return
     */
    public IngestServer getIngestServer()
    {
        return m_IngestServer;
    }
    /**
     * The currently configured ingest server to broadcast to.  This can only be set before beginning a broadcast and must be from the IngestList.
     * @param value
     */
    public void setIngestServer(IngestServer value)
    {
        m_IngestServer = value;
    }

    /**
     * The list of ingest servers available for broadcasting to.
     * @return
     */
    public IngestList getIngestList()
    {
        return m_IngestList;
    }

    /**
     * Currently can only be read / set while broadcasting.
     */
    public float getMicrophoneVolume()
    {
        return m_Stream.getVolume(AudioDeviceType.TTV_RECORDER_DEVICE);
    }
    public void setMicrophoneVolume(float value)
    {
    	m_Stream.setVolume(AudioDeviceType.TTV_RECORDER_DEVICE, value);
    }
    
    /**
     * Currently can only be read / set while broadcasting.
     */
    public float getSystemVolume()
    {
        return m_Stream.getVolume(AudioDeviceType.TTV_PLAYBACK_DEVICE);
    }
    public void setSystemVolume(float value)
    {
    	m_Stream.setVolume(AudioDeviceType.TTV_PLAYBACK_DEVICE, value);
    }
    
    /**
     * The IngestTester instance currently being used to run the ingest test. This will only be non-null while the state is IngestTesting.
     */
    public IngestTester getIngestTester()
    {
    	return m_IngestTester;
    }
    
    /**
     * Retrieves the current broadcast time in milliseconds since the start of the broadcast.  Pausing the stream does not stop this timer.
     */
    public long getCurrentBroadcastTime()
    {
    	return m_Stream.getStreamTime();
    }
    
    /**
     * 
     * @return
     */
    protected boolean getIsAudioSupported()
    {
    	// TODO: implement OS detection 
    	return true;
    }
    
    //endregion

    public BroadcastController()
    {
    	m_Core = Core.getInstance();
    	
    	if (Core.getInstance() == null)
    	{
    		m_Core = new Core( new StandardCoreAPI() );
    	}
    	
    	m_Stream = new Stream(new DesktopStreamAPI());
    }
    
    protected PixelFormat determinePixelFormat()
    {
        return PixelFormat.TTV_PF_RGBA;
    }
    
    /**
     * Initializes the SDK and the controller.
     */
    public boolean initialize()
    {
		if (m_SdkInitialized)
		{
			return false;
		}

        String dllPath = m_DllPath;
        if (dllPath == "")
        {
            dllPath = "./";
        }
        
        m_Stream.setStreamCallbacks(this);
        
        ErrorCode err = m_Core.initialize(m_ClientId, VideoEncoder.TTV_VID_ENC_DEFAULT, dllPath);
        if (!checkError(err))
        {
        	m_Stream.setStreamCallbacks(null);
        	return false;
        }
		
        err = m_Core.setTraceLevel(MessageLevel.TTV_ML_ERROR);
        if (!checkError(err))
        {
        	m_Stream.setStreamCallbacks(null);
        	return false;
        }

        if (ErrorCode.succeeded(err))
        {
            m_SdkInitialized = true;
            setBroadcastState(BroadcastState.Initialized);
            return true;
        }
        
        return false;
    }

    /**
     * Cleans up and shuts down the SDK and the controller.  This will force broadcasting to terminate and the user to be logged out.
     */
    public boolean shutdown()
    {
		if (!m_SdkInitialized)
		{
			return true;
		}
		else if (getIsIngestTesting())
		{
			return false;
		}
		
		m_ShuttingDown = true;
		
        logout();

        m_Stream.setStreamCallbacks(null);
        m_Stream.setStatCallbacks(null);
        
        ErrorCode err = m_Stream.shutdown();
        checkError(err);

        err = m_Core.shutdown();
        checkError(err);
        
        m_SdkInitialized = false;
        m_ShuttingDown = false;
		setBroadcastState(BroadcastState.Uninitialized);
		
		return true;
    }

    /**
     * Asynchronously request an authentication key based on the provided username and password.  When the request completes 
     * onAuthTokenRequestComplete will be fired.  This does not need to be called every time the user wishes to stream.  A valid 
     * auth token can be saved locally between sessions and restored by calling SetAuthToken.  If a request for a new auth token is made
     * it will invalidate the previous valid auth token.  If successful, this will proceed to log the user in and will fire OnLoginAttemptComplete
     * with the result.
     * @param username The account username
     * @param password The account password
     * @return Whether or not the request was made
     */
    public boolean requestAuthToken(String username, String password)
    {
        if (getIsIngestTesting() || !m_SdkInitialized)
        {
            return false;
        }

        logout();

        m_UserName = username;

        AuthParams authParams = new AuthParams();
        authParams.userName = username;
        authParams.password = password;
        authParams.clientSecret = m_ClientSecret;

        ErrorCode err = m_Stream.requestAuthToken(authParams);
        checkError(err);

        if (ErrorCode.succeeded(err))
        {
            setBroadcastState(BroadcastState.Authenticating);
            return true;
        }
        
        return false;
    }
	
    /**
     * Sets the auth token to use if it has been saved from a previous session.  If successful, this will proceed to log the user in 
     * and will fire OnLoginAttemptComplete with the result.
     * @param username The username
     * @param token Whether or not the auth token was set
     */
	public boolean setAuthToken(String username, AuthToken token)
	{
        if (getIsIngestTesting())
        {
            return false;
        }

        logout();

        if (username == null || username.isEmpty())
        {
            reportError("Username must be valid");
            return false;
        }
        else if (token == null || token.data == null || token.data.isEmpty())
        {
            reportError("Auth token must be valid");
            return false;
        }
        
        m_UserName = username;
		m_AuthToken = token;
		
		if (this.getIsInitialized())
		{
			setBroadcastState(BroadcastState.Authenticated);
		}
		
		return true;
	}
	
	/**
	 * Logs the current user out and clear the username and auth token.  This will terminate the broadcast if necessary.
	 */
    public boolean logout()
    {
        if (getIsIngestTesting())
        {
            return false;
        }

        // stop synchronously
        if (this.getIsBroadcasting())
        {
            m_Stream.stop(false);
        }

        m_UserName = "";
        m_AuthToken = new AuthToken();

        if (!m_LoggedIn)
        {
            return false;
        }

        m_LoggedIn = false;

        // only fire the event if the logout was explicitly requested
        if (!m_ShuttingDown)
        {
	        try
	        {
	            if (m_Listener != null)
	            {
	            	m_Listener.onLoggedOut();
	            }
	        }
	        catch (Exception x)
	        {
	            reportError(x.toString());
	        }
        }
        
        setBroadcastState(BroadcastState.Initialized);
        
        return true;
    }

    /**
     * Sets the visible data about the channel of the currently logged in user.
     * @param channel The name of the channel.
     * @param game The name of the game.  If the empty string or null then this parameter is ignored.
     * @param title The title of the channel.  If the empty string or null then this parameter is ignored.
     * @return Whether or not the request was made
     */
    public boolean setStreamInfo(String channel, String game, String title)
    {
        if (!m_LoggedIn)
        {
            return false;
        }

        if (channel == null || channel == "")
        {
        	channel = m_UserName;
        }

        if (game == null)
        {
        	game = "";
        }

        if (title == null)
        {
        	title = "";
        }
        
        StreamInfoForSetting info = new StreamInfoForSetting();
        info.streamTitle = title;
        info.gameName = game;

        ErrorCode err = m_Stream.setStreamInfo(m_AuthToken, channel, info);
        checkError(err);
        
        return ErrorCode.succeeded(err);
    }

    /**
     * Runs a commercial on the channel.  Must be broadcasting.
     * @return Whether or not successful
     */
    public boolean runCommercial()
    {
        if (!this.getIsBroadcasting())
        {
            return false;
        }

        ErrorCode err = m_Stream.runCommercial(m_AuthToken);
        checkError(err);
        
        return ErrorCode.succeeded(err);
    }
    
    /**
     * Determines the recommended streaming parameters based on the maximum bandwidth of the user's internet connection.
     * This is the recommended method if the broadcast resolution can be independent of the game window resolution and 
     * will produce the best visual quality.  The game must submit buffers at the resolution returned in the VideoParams.
     * 
     * @param maxKbps Maximum bitrate supported (this should be determined by running the ingest tester for a given ingest server).
     * @param frameRate The desired frame rate. For a given bitrate and motion factor, a higher framerate will mean a lower resolution.
     * @param bitsPerPixel The bits per pixel used in the final encoded video. A fast motion game (e.g. first person
							shooter) required more bits per pixel of encoded video avoid compression artifacting. Use 0.1 for an 
							average motion game. For games without too many fast changes in the scene, you could use a value below
							0.1 but not much. For fast moving games with lots of scene changes a value as high as  0.2 would be appropriate.
     * @param aspectRatio - The aspect ratio of the video which we'll use for calculating width and height.
     * @return The filled in VideoParams.
     */
    public VideoParams getRecommendedVideoParams(int maxKbps, int frameRate, float bitsPerPixel, float aspectRatio)
    {
    	int[] resolution = m_Stream.getMaxResolution(maxKbps, frameRate, bitsPerPixel, aspectRatio);
    	
    	VideoParams videoParams = new VideoParams();
    	videoParams.maxKbps = maxKbps;
    	videoParams.encodingCpuUsage = EncodingCpuUsage.TTV_ECU_HIGH;
    	videoParams.pixelFormat = determinePixelFormat();
    	videoParams.targetFps = frameRate;
    	videoParams.outputWidth = resolution[0];
    	videoParams.outputHeight = resolution[1];
    	videoParams.disableAdaptiveBitrate = false;
    	videoParams.verticalFlip = false;
    	
    	return videoParams;
    }
    
    /**
     * Returns a fully populated VideoParams struct based on the given width, height and frame rate.  
     * This function is not the advised way to setup VideoParams because it has no information about the user's maximum bitrate. 
     * Use the other version of GetRecommendedVideoParams instead if possible.
     * @param width The broadcast width
     * @param height The broadcast height
     * @param frameRate The broadcast frames per second
     * @return The VideoParams
     */
    public VideoParams getRecommendedVideoParams(int width, int height, int frameRate)
    {
        VideoParams videoParams = new VideoParams();
        
        videoParams.outputWidth = width;
        videoParams.outputHeight = height;
        videoParams.targetFps = frameRate;
        videoParams.pixelFormat = determinePixelFormat();
        videoParams.encodingCpuUsage = EncodingCpuUsage.TTV_ECU_HIGH;
    	videoParams.disableAdaptiveBitrate = false;
    	videoParams.verticalFlip = false;

        // Compute the rest of the fields based on the given parameters
        ErrorCode ret = m_Stream.getDefaultParams(videoParams);
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error in GetDefaultParams: %s", err));
            return null;
        }
        
    	return videoParams;
    }
    
    /**
     * Begins broadcast using the given VideoParams.
     * @param videoParams The video params
     * @return Whether or not successfully broadcasting
     */
    public boolean startBroadcasting(VideoParams videoParams)
    {
        if (videoParams == null || !this.getIsReadyToBroadcast())
        {
            return false;
        }

        m_VideoParams = videoParams.clone();
        
        // Setup the audio parameters
        m_AudioParams = new AudioParams();
        m_AudioParams.audioEnabled = m_EnableAudio && getIsAudioSupported(); // // only enable audio if possible
        m_AudioParams.enableMicCapture = m_AudioParams.audioEnabled;
        m_AudioParams.enablePlaybackCapture = m_AudioParams.audioEnabled;
		m_AudioParams.enablePassthroughAudio = false;

		if (!allocateBuffers())
		{
        	m_VideoParams = null;
        	m_AudioParams = null;
        	return false;
		}

        ErrorCode ret = m_Stream.start(videoParams, m_AudioParams, m_IngestServer, StartFlags.None, true);
        if (ErrorCode.failed(ret))
        {
        	cleanupBuffers();
        	        	
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error while starting to broadcast: %s", err));
			
        	m_VideoParams = null;
        	m_AudioParams = null;
            
            return false;
        }

        setBroadcastState(BroadcastState.Starting);

        return true;
    }

    /**
     * Terminates the broadcast.
     * @return Whether or not successfully stopped
     */
    public boolean stopBroadcasting()
    {
        if (!this.getIsBroadcasting())
        {
            return false;
        }

        ErrorCode ret = m_Stream.stop(true);
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error while stopping the broadcast: %s", err));
            return false;
        }
		
        setBroadcastState(BroadcastState.Stopping);
        
		return ErrorCode.succeeded(ret);
    }
	
    /**
     * Pauses the current broadcast and displays the default pause screen.
     * @return Whether or not successfully paused
     */
	public boolean pauseBroadcasting()
	{
	    if (!this.getIsBroadcasting())
	    {
		    return false;
	    }

        ErrorCode ret = m_Stream.pauseVideo();
	    if ( ErrorCode.failed(ret) )
	    {
		    // not streaming anymore
		    stopBroadcasting();

		    String err = ErrorCode.getString(ret);
		    reportError(String.format("Error pausing stream: %s\n", err));
	    }
	    else
	    {
	    	setBroadcastState(BroadcastState.Paused);
	    }

	    return ErrorCode.succeeded(ret);
	}
	
	/**
	 * Resumes broadcasting after being paused.
	 * @return Whether or not successfully resumed
	 */
	public boolean resumeBroadcasting()
	{
	    if (!this.getIsPaused())
	    {
		    return false;
	    }
		
		setBroadcastState(BroadcastState.Broadcasting);
		
		return true;
	}
	
	/**
	 * Send a singular action metadata point to Twitch's metadata service.
	 * @param name A specific name for an event meant to be queryable
	 * @param streamTime Number of milliseconds into the broadcast for when event occurs
	 * @param humanDescription Long form string to describe the meaning of an event. Maximum length is 1000 characters
	 * @param data A valid JSON object that is the payload of an event. Values in this JSON object have to be strings. Maximum of 50 keys are allowed. Maximum length for values are 255 characters.
	 * @return True if submitted and no error, false otherwise.
	 */
    public boolean sendActionMetaData(String name, long streamTime, String humanDescription, String data)
    {
        ErrorCode ret = m_Stream.sendActionMetaData(m_AuthToken, name, streamTime, humanDescription, data);
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error while sending meta data: %s\n", err));
            
            return false;
        }
        
        return true;
    }

    /**
     * Send the beginning datapoint of an event that has a beginning and end.
     * @param name A specific name for an event meant to be queryable
     * @param streamTime Number of milliseconds into the broadcast for when event occurs
     * @param humanDescription Long form string to describe the meaning of an event. Maximum length is 1000 characters
     * @param data A valid JSON object that is the payload of an event. Values in this JSON object have to be strings. Maximum of 50 keys are allowed. Maximum length for values are 255 characters.
     * @return A positive, unique sequenceId returned that associates a start and end event together.  This will be -1 if failed.
     */
    public long startSpanMetaData(String name, long streamTime, String humanDescription, String data)
    {
    	long ret = m_Stream.sendStartSpanMetaData(m_AuthToken, name, streamTime, humanDescription, data);
        if (ret == -1)
        {
            reportError(String.format("Error in SendStartSpanMetaData\n"));
        }
        
        return ret;
    }

    /**
     * Send the ending datapoint of an event that has a beginning and end. 
     * @param name A specific name for an event meant to be queryable
     * @param streamTime Number of milliseconds into the broadcast for when event occurs
     * @param sequenceId Associates a start and end event together. Use the corresponding sequenceId returned in TTV_SendStartSpanMetaData
     * @param humanDescription Long form string to describe the meaning of an event. Maximum length is 1000 characters
     * @param data A valid JSON object that is the payload of an event. Values in this JSON object have to be strings. Maximum of 50 keys are allowed. Maximum length for values are 255 characters.
     * @return True if submitted and no error, false otherwise.
     */
    public boolean endSpanMetaData(String name, long streamTime, long sequenceId, String humanDescription, String data)
    {
    	if (sequenceId == -1)
    	{
            reportError(String.format("Invalid sequence id: %d\n", sequenceId));
    		return false;
    	}
    	
        ErrorCode ret = m_Stream.sendEndSpanMetaData(m_AuthToken, name, streamTime, sequenceId, humanDescription, data);
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error in SendStopSpanMetaData: %s\n", err));
            
            return false;
        }
        
        return true;
    }

    /**
     * Requests a list of all games matching the given search string.  The result will be returned asynchronously via OnGameNameListReceived.
     * @param str Whether or not the request was made
     */
    public void requestGameNameList(String str)
    {
        ErrorCode ret = m_Stream.getGameNameList(str);
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error in GetGameNameList: %s\n", err));
        }
    }

    protected void setBroadcastState(BroadcastState state)
    {
        if (state == m_BroadcastState)
        {
            return;
        }

        m_BroadcastState = state;

        try
        {
            if (m_Listener != null)
            {
            	m_Listener.onBroadcastStateChanged(state);
            }
        }
        catch (Exception x)
        {
            reportError(x.toString());
        }
    }

    /**
     * Updates the internals of the controller.  This should be called at least as frequently as the desired broadcast framerate.
     * Asynchronous results will be fired from inside this function.
     */
    public void update()
    {
    	if (m_Stream == null || !m_SdkInitialized)
    	{
    		return;
    	}
    	
        ErrorCode ret = m_Stream.pollTasks();
        checkError(ret);

        // update the ingest tester
        if (getIsIngestTesting())
        {
            m_IngestTester.update();

            // all done testing
            if (m_IngestTester.getIsDone())
            {
                m_IngestTester = null;
                setBroadcastState(BroadcastState.ReadyToBroadcast);
            }
        }        
        
	    switch (m_BroadcastState)
	    {
		    // Kick off an authentication request
		    case Authenticated:
		    {
			    setBroadcastState(BroadcastState.LoggingIn);

                ret = m_Stream.login(m_AuthToken);
			    if (ErrorCode.failed(ret))
			    {
				    String err = ErrorCode.getString(ret);
				    reportError(String.format("Error in TTV_Login: %s\n", err));
			    }
			    break;
		    }
		    // Login
		    case LoggedIn:
		    {
			    setBroadcastState(BroadcastState.FindingIngestServer);

                ret = m_Stream.getIngestServers(m_AuthToken);
                if (ErrorCode.failed(ret))
			    {
                    setBroadcastState(BroadcastState.LoggedIn);

				    String err = ErrorCode.getString(ret);
                    reportError(String.format("Error in TTV_GetIngestServers: %s\n", err));
			    }
			    break;
		    }
		    // Ready to stream
		    case ReceivedIngestServers:
		    {
			    setBroadcastState(BroadcastState.ReadyToBroadcast);

                // Kick off requests for the user and stream information that aren't 100% essential to be ready before streaming starts
                ret = m_Stream.getUserInfo(m_AuthToken);
                if (ErrorCode.failed(ret))
                {
                    String err = ErrorCode.getString(ret);
                    reportError(String.format("Error in TTV_GetUserInfo: %s\n", err));
                }

                updateStreamInfo();

                ret = m_Stream.getArchivingState(m_AuthToken);
                if (ErrorCode.failed(ret))
                {
                    String err = ErrorCode.getString(ret);
                    reportError(String.format("Error in TTV_GetArchivingState: %s\n", err));
                }

			    break;
		    }
            // Waiting for the start/stop callback
            case Starting:
            case Stopping:
            {
                break;
            }
            // No action required
		    case FindingIngestServer:
		    case Authenticating:
		    case Initialized:
		    case Uninitialized:		
		    case IngestTesting:		
		    {
			    break;
		    }
		    case Paused:
		    case Broadcasting:
			{
				updateStreamInfo();
				break;
			}
		    default:
		    {
			    break;
		    }
	    }
    }

    protected void updateStreamInfo()
    {
    	long now = System.nanoTime();
    	long delta = (now - m_LastStreamInfoUpdateTime) / 1000000000;

        // only check every so often
        if (delta < s_StreamInfoUpdateInterval)
        {
            return;
        }

        m_LastStreamInfoUpdateTime = now;

        ErrorCode ret = m_Stream.getStreamInfo(m_AuthToken, m_UserName);
        if (ErrorCode.failed(ret))
        {
            String err = ErrorCode.getString(ret);
            reportError(String.format("Error in TTV_GetStreamInfo: %s", err));
        }
    }    

    //#region Ingest Testing

    /**
     * If the user is logged in and ready to broadcast this will kick off an asynchronous test of bandwidth to all ingest servers.
     * This will in turn fill in the bitrate fields in the ingest list.
     * @return The IngestTester instance that is valid during the test.
     */
    public IngestTester startIngestTest()
    {
        if (!getIsReadyToBroadcast() || m_IngestList == null)
        {
            return null;
        }

        if (getIsIngestTesting())
        {
            return null;
        }

        m_IngestTester = new IngestTester(m_Stream, m_IngestList);
        m_IngestTester.Start();

        setBroadcastState(BroadcastState.IngestTesting);

        return m_IngestTester;
    }

    /**
     * Asynchronously cancels a currently underway ingest test.
     */
    public void cancelIngestTest()
    {
        if (getIsIngestTesting())
        {
            m_IngestTester.Cancel();
        }
    }

    //#endregion    
    
    protected boolean allocateBuffers()
    {
        // Allocate exactly 3 buffers to use as the capture destination while streaming.
        // These buffers are passed to the SDK.
        for (int i = 0; i < s_NumSdkBuffers; ++i)
        {
        	FrameBuffer buffer = m_Stream.allocateFrameBuffer(m_VideoParams.outputWidth * m_VideoParams.outputHeight * 4);
            if (!buffer.getIsValid())
            {
                reportError(String.format("Error while allocating frame buffer"));
                return false;
            }

            m_CaptureBuffers.add(buffer);
            m_FreeBufferList.add(buffer);
        }

        return true;
    }
    
    protected void cleanupBuffers()
    {
        // Delete the capture buffers
        for (int i = 0; i < m_CaptureBuffers.size(); ++i)
        {
        	FrameBuffer buffer = m_CaptureBuffers.get(i);
            buffer.free();
        }

        m_FreeBufferList.clear();
        m_CaptureBuffers.clear();
    }

    public FrameBuffer getNextFreeBuffer()
    {
        if (m_FreeBufferList.size() == 0)
        {
            reportError(String.format("Out of free buffers, this should never happen"));
            return null;
        }

        FrameBuffer buffer = m_FreeBufferList.get(m_FreeBufferList.size() - 1);
        m_FreeBufferList.remove(m_FreeBufferList.size() - 1);

        return buffer;
    }
    
    public void captureFrameBuffer_ReadPixels(FrameBuffer buffer)
    {
    	m_Stream.captureFrameBuffer_ReadPixels(buffer);
    }
    
    public ErrorCode submitFrame(FrameBuffer bgraFrame)
    {
        if (this.getIsPaused())
        {
            resumeBroadcasting();
        }
        else if (!this.getIsBroadcasting())
        {
            return ErrorCode.TTV_EC_STREAM_NOT_STARTED;
        }

        ErrorCode ret = m_Stream.submitVideoFrame(bgraFrame);
        
        // if there is a problem when submitting a frame let the client know
        if (ret != ErrorCode.TTV_EC_SUCCESS)
        {
            String err = ErrorCode.getString(ret);
            if (ErrorCode.succeeded(ret))
            {
                reportWarning(String.format("Warning in SubmitTexturePointer: %s\n", err));
            }
            else
            {
                reportError(String.format("Error in SubmitTexturePointer: %s\n", err));
            
                // errors are not recoverable
                stopBroadcasting();
            }

            if (m_Listener != null)
            {
            	m_Listener.onframeSubmissionIssue(ret);
            }
        }
        
        return ret;
    }
    
    protected boolean checkError(ErrorCode err)
    {
        if (ErrorCode.failed(err))
        {
        	reportError(ErrorCode.getString(err));
        	return false;
        }
        
        return true;
    }

    protected void reportError(String err)
    {
    	System.out.println(err.toString());
    }

    protected void reportWarning(String err)
    {
    	System.out.println(err.toString());
    }
}
