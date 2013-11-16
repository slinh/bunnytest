using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twitch;
using ErrorCode = Twitch.ErrorCode;

namespace Twitch.Broadcast
{
    /// <summary>
    /// The state machine which manages the broadcasting state.  It provides a high level interface to the SDK libraries.  The BroadcastController (BC) performs many operations
    /// asynchronously and hides the details.  
    /// 
    /// Events will be fired during the call to BC.Update().
    /// 
    /// The typical order of operations a client of BC will take is:
    /// 
    /// - Call BC.InitializeTwitch()
    /// - Call BC.RequestAuthToken() / Call BC.SetAuthToken()
    /// - Determine the server to use for broadcasting
    /// - Call BC.StartBroadcasting()
    /// - Submit frames (this is done differently depending on platform), see platform-specific documentation for details
    /// - Call BC.StopBroadcasting()
    /// - Call BC.ShutdownTwitch()
    /// 
    /// When setting up the VideoParams for the broadcast you should use the bitrate version of GetRecommendedVideoParams.  This will setup the resolution and other parameters
    /// based on your connection to the server.  The other version is more explicit and may be useful for advanced users and testing but is generally more confusing and produces
    /// poorer results for the typical user who doesn't understand the settings.  We've found that many users just crank up the settings and don't understand why their 
    /// broadcast doesn't look that great or the stream drops (network backup).  Simply giving the users a slider for maximum bitrate generally produces better visual quality.
    /// In a more advanced integration the bitrate can actually be determined by ingest testing (see below).
    /// 
    /// The ingest server to use for broadcasting can be configured via the IngestServer property.  The list of servers can be retrieved from the IngestServers property.
    /// Normally, the default server will be adequate and sufficiently close for decent broadcasting.  However, to be sure, you can perform ingest testing which will determine 
    /// the connection speeds to all the Twitch servers.  This will help both in determining the best server to use (the server with the highest connection) and the actual bitrate 
    /// to use when calculating the optimal VideoParams for that server. 
    /// 
    /// Ingest testing.  This can be done by using the IngestTester class.  After logging into the BC, ingest testing can be performed by calling BC.StartIngestTest() which is only 
    /// available in the ReadyForBroadcasting state.  This returns an instance of IngestTester which is a single-use instance.  This class will perform a test which will measure 
    /// the connection speed to each of the Twitch ingest servers.  See the documentation of the class for details.  While the test is underway the BC is unavailable for any operations.
    /// </summary>
    public abstract partial class BroadcastController : IStreamCallbacks
    {
        #region Types

        /// <summary>
        /// The possible states the BroadcastController can be in.
        /// </summary>
        public enum BroadcastState
        {
            Uninitialized,          //!< InitializeTwitch not called.
            Initialized,            //!< InitializeTwitch has been called.
            Authenticating,         //!< Requesting an AuthToken.
            Authenticated,          //!< Have an AuthToken (not necessarily a valid one).
            LoggingIn,              //!< Waiting to see if the AuthToken is valid).
            LoggedIn,               //!< AuthToken is valid.
            FindingIngestServer,    //!< Determining which server we can braodcast to.
            ReceivedIngestServers,  //!< Received the list of ingest servers.
            ReadyToBroadcast,       //!< Idle and ready to broadcast.
            Starting,               //!< Processing a request to start broadcasting.
            Broadcasting,           //!< Currently broadcasting.
            Stopping,               //!< Processing a request to stop broadcasting.
            Paused,                 //!< Streaming but paused.
            IngestTesting           //!< Running the ingest tester.
        }
    
        /// <summary>
        /// The callback signature for the event fired when a request for an auth token is complete.
        /// </summary>
        /// <param name="result">Whether or not the request was successful.</param>
        /// <param name="authToken">The auth token, if successful.</param>
        public delegate void AuthTokenRequestCompleteDelegate(ErrorCode result, AuthToken authToken);

        /// <summary>
        /// The callback signature for the event which is fired when an attempt to login is complete.
        /// </summary>
        /// <param name="result">Whether or not the attempt was successful.</param>
        public delegate void LoginAttemptCompleteDelegate(ErrorCode result);

        /// <summary>
        /// The callback signature for the event which is fired when a game name list request is complete.
        /// </summary>
        /// <param name="result">Whether or not the request was successful.</param>
        /// <param name="list">The resulting list, if successful.</param>
        public delegate void GameNameListReceivedDelegate(ErrorCode result, GameInfo[] list);

        /// <summary>
        /// The callback signature for the event which is fired when the BroadcastController changes state.
        /// </summary>
        /// <param name="state">The new state.</param>
        public delegate void BroadcastStateChangedDelegate(BroadcastState state);

        /// <summary>
        /// The callback signature for the event which is fired when the user is logged out.
        /// </summary>
        public delegate void LoggedOutDelegate();

        /// <summary>
        /// The callback signature for the event which is fired when the stream info is updated.
        /// </summary>
        /// <param name="info">The new stream inforamtion.</param>
        public delegate void StreamInfoUpdatedDelegate(StreamInfo info);

        /// <summary>
        /// The callback signature for the event which is fired when the ingest list is updated.
        /// </summary>
        /// <param name="info">The new stream inforamtion.</param>
        public delegate void IngestListReceivedDelegate(IngestList list);

        /// <summary>
        /// The callback signature for the event which is fired when there was an issue submitting frames to the SDK for encoding.
        /// </summary>
        /// <param name="result">The issue.</param>
        public delegate void FrameSubmissionIssueDelegate(ErrorCode result);

        /// <summary>
        /// The callback signature for the event which is fired when broadcasting begins.
        /// </summary>
        public delegate void BroadcastStartedDelegate();

        /// <summary>
        /// The callback signature for the event which is fired when broadcasting ends.
        /// </summary>
        public delegate void BroadcastStoppedDelegate();

        #endregion

        #region Constants

        protected static int s_StreamInfoUpdateInterval = 30; //!< Update the stream info every 30 seconds.

        #endregion

        #region Member Variables
    
        public event AuthTokenRequestCompleteDelegate AuthTokenRequestComplete;
        public event LoginAttemptCompleteDelegate LoginAttemptComplete;
        public event GameNameListReceivedDelegate GameNameListReceived;
        public event BroadcastStateChangedDelegate BroadcastStateChanged;
        public event LoggedOutDelegate LoggedOut;
        public event StreamInfoUpdatedDelegate StreamInfoUpdated;
        public event IngestListReceivedDelegate IngestListReceived;
        public event FrameSubmissionIssueDelegate FrameSubmissionIssue;
        public event BroadcastStartedDelegate BroadcastStarted;
        public event BroadcastStoppedDelegate BroadcastStopped;

        protected Twitch.Core m_Core = null;
        protected Twitch.Broadcast.Stream m_Stream = null;

        protected bool m_SdkInitialized = false;    //!< Has Stream.Initialize() been called?
        protected bool m_LoggedIn = false;          //!< The AuthToken as been validated and can be used for calls to the server.
        protected bool m_ShuttingDown = false;      //!< The controller is currently shutting down.

        protected BroadcastState m_BroadcastState = BroadcastState.Uninitialized;

        protected string m_UserName = null;
        protected VideoParams m_VideoParams = null;         //!< The VideoParams currently in use.
        protected AudioParams m_AudioParams = null;         //!< The AudioParams currently in use.

        protected IngestList m_IngestList = new IngestList();
        protected IngestServer m_IngestServer = null;
        protected AuthToken m_AuthToken = new AuthToken();
        protected ChannelInfo m_ChannelInfo = new ChannelInfo();
        protected UserInfo m_UserInfo = new UserInfo();
        protected StreamInfo m_StreamInfo = new StreamInfo();
        protected ArchivingState m_ArchivingState = new ArchivingState();

        protected System.DateTime m_LastStreamInfoUpdateTime = System.DateTime.MinValue;
        protected IngestTester m_IngestTester = null;

        #endregion


        #region IStreamCallbacks

        void IStreamCallbacks.RequestAuthTokenCallback(ErrorCode result, AuthToken authToken)
        {
            if (Error.Succeeded(result))
            {
                // Now that the user is authorized the information can be requested about which server to stream to
                m_AuthToken = authToken;
                SetBroadcastState(BroadcastState.Authenticated);
            }
            else
            {
                m_AuthToken.Data = "";
                SetBroadcastState(BroadcastState.Initialized);

                string err = Error.GetString(result);
                ReportError(string.Format("RequestAuthTokenDoneCallback got failure: {0}", err));
            }
        
            try
            {
                if (AuthTokenRequestComplete != null)
                {
                    this.AuthTokenRequestComplete(result, authToken);
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        void IStreamCallbacks.LoginCallback(ErrorCode result, ChannelInfo channelInfo)
        {
            if (Error.Succeeded(result))
            {
                m_ChannelInfo = channelInfo;
                SetBroadcastState(BroadcastState.LoggedIn);
                m_LoggedIn = true;
            }
            else
            {
                SetBroadcastState(BroadcastState.Initialized);
                m_LoggedIn = false;

                string err = Error.GetString(result);
                ReportError(string.Format("LoginCallback got failure: {0}", err));
            }
        
            try
            {
                if (LoginAttemptComplete != null)
                {
                    this.LoginAttemptComplete(result);
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        void IStreamCallbacks.GetIngestServersCallback(ErrorCode result, IngestList ingestList)
        {
            if (Error.Succeeded(result))
            {
                m_IngestList = ingestList;

                // assume we're going to use the default ingest server unless overidden by the client
                m_IngestServer = m_IngestList.DefaultServer;

                SetBroadcastState(BroadcastState.ReceivedIngestServers);

                try
                {
                    if (IngestListReceived != null)
                    {
                        IngestListReceived(ingestList);
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                }
            }
            else
            {
                string err = Error.GetString(result);
                ReportError(string.Format("IngestListCallback got failure: {0}", err));

                // try again
                SetBroadcastState(BroadcastState.LoggedIn);
            }
        }

        void IStreamCallbacks.GetUserInfoCallback(ErrorCode result, UserInfo userInfo)
        {
            m_UserInfo = userInfo;

            if (Error.Failed(result))
            {
                string err = Error.GetString(result);
                ReportError(string.Format("UserInfoDoneCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.GetStreamInfoCallback(ErrorCode result, StreamInfo streamInfo)
        {
            if (Error.Succeeded(result))
            {
                m_StreamInfo = streamInfo;

                try
                {
                    if (StreamInfoUpdated != null)
                    {
                        StreamInfoUpdated(streamInfo);
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                } 
            }
            else
            {
                //string err = Error.GetString(result);
                //ReportWarning(string.Format("StreamInfoDoneCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.GetArchivingStateCallback(ErrorCode result, ArchivingState state)
        {
            m_ArchivingState = state;

            if (Error.Failed(result))
            {
                //string err = Error.GetString(result);
                //ReportWarning(string.Format("ArchivingStateDoneCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.RunCommercialCallback(ErrorCode result)
        {
            if (Error.Failed(result))
            {
                string err = Error.GetString(result);
                ReportWarning(string.Format("RunCommercialCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.SetStreamInfoCallback(ErrorCode result)
        {
            if (Error.Failed(result))
            {
                string err = Error.GetString(result);
                ReportWarning(string.Format("SetStreamInfoCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.GetGameNameListCallback(ErrorCode result, GameInfoList list)
        {
            if (Error.Failed(result))
            {
                string err = Error.GetString(result);
                ReportError(string.Format("GameNameListCallback got failure: {0}", err));
            } 
        
            try
            {
                if (GameNameListReceived != null)
                {
                    this.GameNameListReceived(result, list == null ? new GameInfo[0] : list.List);
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        void IStreamCallbacks.StartCallback(ErrorCode ret)
        {
            if (Error.Succeeded(ret))
            {
                try
                {
                    if (BroadcastStarted != null)
                    {
                        this.BroadcastStarted();
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                } 
                
                SetBroadcastState(BroadcastState.Broadcasting);
            }
            else
            {
                m_VideoParams = null;
                m_AudioParams = null;

                SetBroadcastState(BroadcastState.ReadyToBroadcast);
                
                string err = Error.GetString(ret);
                ReportError(string.Format("StartCallback got failure: {0}", err));            
            }
        }

        void IStreamCallbacks.StopCallback(ErrorCode ret)
        {
            if (Error.Succeeded(ret))
            {
                m_VideoParams = null;
                m_AudioParams = null;

                CleanupBuffers();

                try
                {
                    if (BroadcastStopped != null)
                    {
                        this.BroadcastStopped();
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                }

                if (m_LoggedIn)
                {
                    SetBroadcastState(BroadcastState.ReadyToBroadcast);
                }
                else
                {
                    SetBroadcastState(BroadcastState.Initialized);
                }
            }
            else
            {
                // there's not really a good state to go into here
                SetBroadcastState(BroadcastState.ReadyToBroadcast);
                
                string err = Error.GetString(ret);
                ReportError(string.Format("StopCallback got failure: {0}", err));            
            }
        }

        void IStreamCallbacks.BufferUnlockCallback(UIntPtr buffer)
        {
            HandleBufferUnlock(buffer);
        }

        void IStreamCallbacks.SendActionMetaDataCallback(ErrorCode ret)
        {
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("SendActionMetaDataCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.SendStartSpanMetaDataCallback(ErrorCode ret)
        {
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("SendStartSpanMetaDataCallback got failure: {0}", err));
            }
        }

        void IStreamCallbacks.SendEndSpanMetaDataCallback(ErrorCode ret)
        {
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("SendEndSpanMetaDataCallback got failure: {0}", err));
            }
        }

        #endregion


        #region Properties

        /// <summary>
        /// Whether or not InitializeTwitch has been called.
        /// </summary>
        public bool IsInitialized
        {
            get { return m_SdkInitialized; }
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
        /// Whether or not to enable capturing of system and microphone audio.
        /// </summary>
        public abstract bool EnableAudio
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
        }

        public BroadcastState CurrentState
        {
            get { return m_BroadcastState; }
        }

        public ArchivingState ArchivingState
        {
            get { return m_ArchivingState; }
        }

        public AuthToken AuthToken
        {
            get { return m_AuthToken; }
        }

        public StreamInfo StreamInfo
        {
            get { return m_StreamInfo; }
        }

        public UserInfo UserInfo
        {
            get { return m_UserInfo; }
        }

        public ChannelInfo ChannelInfo
        {
            get { return m_ChannelInfo; }
        }

        public bool IsBroadcasting
        {
            get { return m_BroadcastState == BroadcastState.Broadcasting || m_BroadcastState == BroadcastState.Paused; }
        }

        public bool IsReadyToBroadcast
        {
            get { return m_BroadcastState == BroadcastState.ReadyToBroadcast; }
        }

        public bool IsIngestTesting
        {
            get { return m_BroadcastState == BroadcastState.IngestTesting; }
        }

        public bool IsPaused
        {
            get { return m_BroadcastState == BroadcastState.Paused; }
        }

        public bool IsLoggedIn
        {
            get { return m_LoggedIn; }
        }

        public bool HaveAuthToken
        {
            get { return m_AuthToken != null && m_AuthToken.IsValid; }
        }

        /// <summary>
        /// The currently configured ingest server to broadcast to.  This can only be set before beginning a broadcast
        /// and must be from the IngestList.
        /// </summary>
        public IngestServer IngestServer
        {
            get { return m_IngestServer; }
            set { m_IngestServer = value; }
        }

        /// <summary>
        /// The list of ingest servers available for broadcasting to.
        /// </summary>
        public IngestList IngestList
        {
            get { return m_IngestList; }
        }

        /// <summary>
        /// The microphone volume while recorking.  This can currently can only be read / set while broadcasting.
        /// </summary>
        public float MicrophoneVolume
        {
            get
            { 
                float volume = 0;
                m_Stream.GetVolume(TTV_AudioDeviceType.TTV_RECORDER_DEVICE, ref volume);
                return volume;
            }
            set
            {
                m_Stream.SetVolume(TTV_AudioDeviceType.TTV_RECORDER_DEVICE, value);
            }
        }

        /// <summary>
        /// The global system volume while recording.  This can currently can only be read / set while broadcasting.
        /// </summary>
        public float SystemVolume
        {
            get
            {
                float volume = 0;
                m_Stream.GetVolume(TTV_AudioDeviceType.TTV_PLAYBACK_DEVICE, ref volume);
                return volume;
            }
            set
            {
                m_Stream.SetVolume(TTV_AudioDeviceType.TTV_PLAYBACK_DEVICE, value);
            }
        }

        /// <summary>
        /// The IngestTester instance currently being used to run the ingest test. This will only be non-null while the state is IngestTesting.
        /// </summary>
        public IngestTester IngestTester
        {
            get { return m_IngestTester; }
        }

        /// <summary>
        /// Retrieves the current broadcast time in milliseconds since the start of the broadcast.  Pausing the stream does not stop this timer.
        /// </summary>
        public UInt64 CurrentBroadcastTime
        {
            get
            {
                UInt64 time = 0;
                m_Stream.GetStreamTime(out time);
                return time;
            }
        }

        /// <summary>
        /// Determines if audio capture is available.
        /// </summary>
        public virtual bool IsAudioSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Determines if the broadcasting libraries support the operating system.
        /// </summary>
        public virtual bool IsPlatformSupported
        {
            get
            {
                return true;
            }
        }
        
        #endregion

        protected virtual bool AllocateBuffers()
        {
            return true;
        }

        protected virtual void CleanupBuffers()
        {
        }

        protected virtual void HandleBufferUnlock(UIntPtr buffer)
        {
        }

        public virtual Twitch.Broadcast.PixelFormat DeterminePixelFormat()
        {
            return PixelFormat.TTV_PF_BGRA;
        }

        /// <summary>
        /// Initializes the controller.
        /// </summary>
        /// <returns>True if successful</returns>
        public virtual bool Initialize()
        {
            if (m_SdkInitialized)
            {
                return false;
            }

            m_Stream.StreamCallbacks = this;

            ErrorCode err = m_Core.Initialize(this.ClientId, VideoEncoder.TTV_VID_ENC_DEFAULT, null);
            if (!CheckError(err))
            {
                m_Stream.StreamCallbacks = null;
                return false;
            }

            m_Core.SetTraceLevel(MessageLevel.TTV_ML_ERROR);
            if (!CheckError(err))
            {
                m_Stream.StreamCallbacks = null;
                return false;
            }

            if (Error.Succeeded(err))
            {
                m_SdkInitialized = true;
                SetBroadcastState(BroadcastState.Initialized);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Cleans up and shuts down the controller.  This will force broadcasting to terminate and the user to be logged out.
        /// </summary>
        /// <returns>True if successful</returns>
        public virtual bool Shutdown()
        {
            if (!m_SdkInitialized)
            {
                return true;
            }
            else if (this.IsIngestTesting)
            {
                return false;
            }

            m_ShuttingDown = true;

            Logout();

            m_Stream.StreamCallbacks = null;
            m_Stream.StatCallbacks = null;

            ErrorCode err = m_Core.Shutdown();
            CheckError(err);

            m_SdkInitialized = false;
            m_ShuttingDown = false;
            SetBroadcastState(BroadcastState.Uninitialized);

            return true;
        }

        /// <summary>
        /// Asynchronously request an authentication key based on the provided username and password.  When the request completes 
        /// AuthTokenRequestComplete will be fired.  This does not need to be called every time the user wishes to stream.  A valid 
        /// auth token can be saved locally between sessions and restored by calling SetAuthToken.  If a request for a new auth token is made
        /// it will invalidate the previous valid auth token.  If successful, this will proceed to log the user in and will fire LoginAttemptComplete
        /// with the result.
        /// </summary>
        /// <param name="username">The account username</param>
        /// <param name="password">The account password</param>
        /// <returns>Whether or not the request was made</returns>
        public virtual bool RequestAuthToken(string username, string password)
        {
            if (this.IsIngestTesting || !m_SdkInitialized)
            {
                return false;
            }

            Logout();

            m_UserName = username;

            AuthParams authParams = new AuthParams();
            authParams.UserName = username;
            authParams.Password = password;
            authParams.ClientSecret = this.ClientSecret;

            ErrorCode err = m_Stream.RequestAuthToken(authParams);
            CheckError(err);

            if (Error.Succeeded(err))
            {
                SetBroadcastState(BroadcastState.Authenticating);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the auth token to use if it has been saved from a previous session.  If successful, this will proceed to log the user in 
        /// and will fire LoginAttemptComplete with the result.
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="token">The AuthToken</param>
        /// <returns>Whether or not the auth token was set</returns>
        public virtual bool SetAuthToken(string username, AuthToken token)
        {
            if (this.IsIngestTesting)
            {
                return false;
            }

            Logout();

            if (string.IsNullOrEmpty(username))
            {
                ReportError("Username must be valid");
                return false;
            }
            else if (token == null || token.Data[0] == '\0')
            {
                ReportError("Auth token must be valid");
                return false;
            }

            m_UserName = username;
            m_AuthToken = token;
        
            if (this.IsInitialized)
            {
                SetBroadcastState(BroadcastState.Authenticated);
            }

            return true;
        }
    
        /// <summary>
        /// Logs the current user out and clear the username and auth token.  This will terminate the broadcast if necessary.
        /// </summary>
        /// <returns>Whether or not successfully logged out</returns>
        public virtual bool Logout()
        {
            if (this.IsIngestTesting)
            {
                return false;
            }

            // stop synchronously
            if (this.IsBroadcasting)
            {
                m_Stream.Stop(false);
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
                    if (LoggedOut != null)
                    {
                        this.LoggedOut();
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                }
            }

            SetBroadcastState(BroadcastState.Initialized);

            return true;
        }

        /// <summary>
        /// Sets the visible data about the channel of the currently logged in user.
        /// </summary>
        /// <param name="channel">The name of the channel.  Normally your username.</param>
        /// <param name="game">The name of the game.  If the string is null or empty then this parameter is ignored.</param>
        /// <param name="title">The title of the channel.  If the string is null or empty then this parameter is ignored.</param>
        /// <returns>Whether or not the request was made</returns>
        public virtual bool SetStreamInfo(string channel, string game, string title)
        {
            if (!m_LoggedIn)
            {
                return false;
            }

            if (String.IsNullOrEmpty(channel))
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
            info.StreamTitle = title;
            info.GameName = game;

            ErrorCode err = m_Stream.SetStreamInfo(m_AuthToken, channel, info);
            CheckError(err);

            return Error.Succeeded(err);
        }

        /// <summary>
        /// Runs a commercial on the channel.  Must be broadcasting.
        /// </summary>
        /// <returns>Whether or not successful</returns>
        public virtual bool RunCommercial()
        {
            if (!this.IsBroadcasting)
            {
                return false;
            }

            ErrorCode err = m_Stream.RunCommercial(m_AuthToken);
            CheckError(err);

            return Error.Succeeded(err);
        }

        /// <summary>
        /// Using the provided maximum bitrate, motion factor, and aspect ratio, calculate the maximum resolution at which the video quality would 
        /// be acceptable.
        /// 
        /// This is the recommended way to populate VideoParams but requires knowing the users' maximum bitrate.  This can be obtained by 
        /// performing ingest testing.
        /// </summary>
        /// <param name="maxKbps">Maximum bitrate supported (this should be determined by running the ingest tester for a given ingest server)</param>
        /// <param name="frameRate">The desired frame rate. For a given bitrate and motion factor, a higher framerate will mean a lower resolution.</param>
        /// <param name="bitsPerPixel">The bits per pixel used in the final encoded video. A fast motion game (e.g. first person shooter) required more 
        /// bits per pixel of encoded video avoid compression artifacting. Use 0.1 for an average motion game. For games without too many fast changes 
        /// in the scene, you could use a value below 0.1 but not much. For fast moving games with lots of scene changes a value as high as  0.2 would 
        /// be appropriate.</param>
        /// <param name="aspectRatio">The aspect ratio of the video which we'll use for calculating width and height</param>
        /// <returns>The resulting VideoParams</returns>
        public virtual VideoParams GetRecommendedVideoParams(uint maxKbps, uint frameRate, float bitsPerPixel, float aspectRatio)
        {
            uint width = 0;
            uint height = 0;
            m_Stream.GetMaxResolution(maxKbps, frameRate, bitsPerPixel, aspectRatio, ref width, ref height);

            VideoParams videoParams = new VideoParams();
            videoParams.MaxKbps = maxKbps;
            videoParams.EncodingCpuUsage = EncodingCpuUsage.TTV_ECU_HIGH;
            videoParams.PixelFormat = DeterminePixelFormat();
            videoParams.TargetFps = frameRate;
            videoParams.OutputWidth = width;
            videoParams.OutputHeight = height;
            videoParams.DisableAdaptiveBitrate = false;
            videoParams.VerticalFlip = false;

            return videoParams;
        }

        /// <summary>
        /// Returns a fully populated VideoParams struct based on the given width, height and frame rate.  
        /// 
        /// This function is not the advised way to setup VideoParams because it has no information about the user's maximum bitrate. 
        /// Use the other version of GetRecommendedVideoParams instead if possible.
        /// </summary>
        /// <param name="width">The broadcast width</param>
        /// <param name="height">The broadcast height</param>
        /// <param name="frameRate">The broadcast frames per second</param>
        /// <returns>The VideoParams</returns>
        public virtual VideoParams GetRecommendedVideoParams(uint width, uint height, uint frameRate)
        {
            VideoParams videoParams = new VideoParams();

            videoParams.OutputWidth = width;
            videoParams.OutputHeight = height;
            videoParams.TargetFps = frameRate;
            videoParams.PixelFormat = DeterminePixelFormat();
            videoParams.EncodingCpuUsage = EncodingCpuUsage.TTV_ECU_HIGH;
            videoParams.DisableAdaptiveBitrate = false;
            videoParams.VerticalFlip = false;

            // Compute the rest of the fields based on the given parameters
            ErrorCode ret = m_Stream.GetDefaultParams(videoParams);
            if (Error.Failed(ret))
            {
                String err = Error.GetString(ret);
                ReportError(String.Format("Error in GetDefaultParams: {0}", err));
                return null;
            }

            return videoParams;
        }

        /// <summary>
        /// Begins broadcast using the given VideoParams.
        /// </summary>
        /// <param name="videoParams">The video params</param>
        /// <returns>Whether or not successfully broadcasting</returns>
        public virtual bool StartBroadcasting(VideoParams videoParams)
        {
            if (videoParams == null || !this.IsReadyToBroadcast)
            {
                return false;
            }

            m_VideoParams = videoParams.Clone() as VideoParams;
            
            // Setup the audio parameters
            m_AudioParams = new AudioParams();
            m_AudioParams.AudioEnabled = this.EnableAudio && this.IsAudioSupported; // only enable audio if possible
            m_AudioParams.EnableMicCapture = m_AudioParams.AudioEnabled;
            m_AudioParams.EnablePlaybackCapture = m_AudioParams.AudioEnabled;
            m_AudioParams.EnablePassthroughAudio = false;

            if (!AllocateBuffers())
            {
                m_VideoParams = null;
                m_AudioParams = null;
                return false;
            }

            ErrorCode ret = m_Stream.Start(videoParams, m_AudioParams, m_IngestServer, StartFlags.None, true);
            if (Error.Failed(ret))
            {
                CleanupBuffers();

                string err = Error.GetString(ret);
                ReportError(string.Format("Error while starting to broadcast: {0}", err));
                
                m_VideoParams = null;
                m_AudioParams = null;

                return false;
            }

            SetBroadcastState(BroadcastState.Starting);

            return true;
        }

        /// <summary>
        /// Terminates the broadcast.
        /// </summary>
        /// <returns>Whether or not successfully stopped</returns>
        public virtual bool StopBroadcasting()
        {
            if (!this.IsBroadcasting)
            {
                return false;
            }

            ErrorCode ret = m_Stream.Stop(true);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error while stopping the broadcast: {0}", err));
                return false;
            }

            SetBroadcastState(BroadcastState.Stopping);

            return true;
        }
    
        /// <summary>
        /// Pauses the current broadcast and displays the default pause screen.
        /// </summary>
        /// <returns>Whether or not successfully paused</returns>
        public virtual bool PauseBroadcasting()
        {
            if (!this.IsBroadcasting)
            {
                return false;
            }

            ErrorCode ret = m_Stream.PauseVideo();
            if (Error.Failed(ret))
            {
                // not streaming anymore
                StopBroadcasting();

                string err = Error.GetString(ret);
                ReportError(string.Format("Error pausing broadcast: {0}\n", err));
            }
            else
            {
                SetBroadcastState(BroadcastState.Paused);
            }

            return Error.Succeeded(ret);
        }
    
        /// <summary>
        /// Resumes broadcasting after being paused.
        /// </summary>
        /// <returns>Whether or not successfully resumed</returns>
        public virtual bool ResumeBroadcasting()
        {
            if (!this.IsPaused)
            {
                return false;
            }
        
            SetBroadcastState(BroadcastState.Broadcasting);

            return true;
        }

        /// <summary>
        /// Send a singular action metadata point to Twitch's metadata service.
        /// </summary>
        /// <param name="name">A specific name for an event meant to be queryable</param>
        /// <param name="streamTime">Number of milliseconds into the broadcast for when event occurs</param>
        /// <param name="humanDescription">Long form string to describe the meaning of an event. Maximum length is 1000 characters</param>
        /// <param name="data">A valid JSON object that is the payload of an event. Values in this JSON object have to be strings. Maximum of 50 keys are allowed. Maximum length for values are 255 characters.</param>
        /// <returns>True if submitted and no error, false otherwise.</returns>
        public virtual bool SendActionMetaData(string name, UInt64 streamTime, string humanDescription, string data)
        {
            ErrorCode ret = m_Stream.SendActionMetaData(m_AuthToken, name, streamTime, humanDescription, data);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error while sending meta data: {0}\n", err));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Send the beginning datapoint of an event that has a beginning and end.
        /// </summary>
        /// <param name="name">A specific name for an event meant to be queryable</param>
        /// <param name="streamTime">Number of milliseconds into the broadcast for when event occurs</param>
        /// <param name="sequenceId">A unique sequenceId returned that associates a start and end event together</param>
        /// <param name="humanDescription">Long form string to describe the meaning of an event. Maximum length is 1000 characters</param>
        /// <param name="data">A valid JSON object that is the payload of an event. Values in this JSON object have to be strings. Maximum of 50 keys are allowed. Maximum length for values are 255 characters.</param>
        /// <returns>True if submitted and no error, false otherwise.</returns>
        public virtual bool StartSpanMetaData(string name, UInt64 streamTime, out ulong sequenceId, string humanDescription, string data)
        {
            sequenceId = 0xFFFFFFFFFFFFFFFF;

            ErrorCode ret = m_Stream.SendStartSpanMetaData(m_AuthToken, name, streamTime, out sequenceId, humanDescription, data);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error in SendStartSpanMetaData: {0}\n", err));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Send the ending datapoint of an event that has a beginning and end.
        /// </summary>
        /// <param name="name">A specific name for an event meant to be queryable</param>
        /// <param name="streamTime">Number of milliseconds into the broadcast for when event occurs</param>
        /// <param name="sequenceId">Associates a start and end event together. Use the corresponding sequenceId returned in TTV_SendStartSpanMetaData</param>
        /// <param name="humanDescription">Long form string to describe the meaning of an event. Maximum length is 1000 characters</param>
        /// <param name="data">A valid JSON object that is the payload of an event. Values in this JSON object have to be strings. Maximum of 50 keys are allowed. Maximum length for values are 255 characters.</param>
        /// <returns>True if submitted and no error, false otherwise.</returns>
        public virtual bool EndSpanMetaData(string name, UInt64 streamTime, ulong sequenceId, string humanDescription, string data)
        {
            if (sequenceId == 0xFFFFFFFFFFFFFFFF)
            {
                ReportError(string.Format("Invalid sequenceId: {0}\n", sequenceId));
                return false;
            }

            ErrorCode ret = m_Stream.SendEndSpanMetaData(m_AuthToken, name, streamTime, sequenceId, humanDescription, data);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error in SendStopSpanMetaData: {0}\n", err));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Requests a list of all games matching the given search string.  The result will be returned asynchronously via OnGameNameListReceived.
        /// </summary>
        /// <param name="str">The string to match</param>
        public virtual void RequestGameNameList(string str)
        {
            ErrorCode ret = m_Stream.GetGameNameList(str);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error in GetGameNameList: {0}\n", err));
            }
        }

        protected virtual void SetBroadcastState(BroadcastState state)
        {
            if (state == m_BroadcastState)
            {
                return;
            }

            m_BroadcastState = state;

            try
            {
                if (BroadcastStateChanged != null)
                {
                    this.BroadcastStateChanged(state);
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        /// <summary>
        /// Updates the internals of the controller.  This should be called at least as frequently as the desired broadcast framerate.
        /// Asynchronous results will be fired from inside this function.
        /// </summary>
        public virtual void Update()
        {
            // for stress testing to make sure memory is being passed around properly
            //GC.Collect();

            if (m_Stream == null || !m_SdkInitialized)
            {
                return;
            }

            ErrorCode ret = m_Stream.PollTasks();
            CheckError(ret);

            // update the ingest tester
            if (this.IsIngestTesting)
            {
                m_IngestTester.Update();

                // all done testing
                if (m_IngestTester.IsDone)
                {
                    m_IngestTester = null;
                    SetBroadcastState(BroadcastState.ReadyToBroadcast);
                }
            }

            switch (m_BroadcastState)
            {
                // Kick off an authentication request
                case BroadcastState.Authenticated:
                {
                    SetBroadcastState(BroadcastState.LoggingIn);

                    ret = m_Stream.Login(m_AuthToken);
                    if (Error.Failed(ret))
                    {
                        string err = Error.GetString(ret);
                        ReportError(string.Format("Error in TTV_Login: {0}\n", err));
                    }
                    break;
                }
                // Login
                case BroadcastState.LoggedIn:
                {
                    SetBroadcastState(BroadcastState.FindingIngestServer);

                    ret = m_Stream.GetIngestServers(m_AuthToken);
                    if (Error.Failed(ret))
                    {
                        SetBroadcastState(BroadcastState.LoggedIn);

                        string err = Error.GetString(ret);
                        ReportError(string.Format("Error in TTV_GetIngestServers: {0}\n", err));
                    }
                    break;
                }
                // Ready to stream
                case BroadcastState.ReceivedIngestServers:
                {
                    SetBroadcastState(BroadcastState.ReadyToBroadcast);

                    // Kick off requests for the user and stream information that aren't 100% essential to be ready before streaming starts
                    ret = m_Stream.GetUserInfo(m_AuthToken);
                    if (Error.Failed(ret))
                    {
                        string err = Error.GetString(ret);
                        ReportError(string.Format("Error in TTV_GetUserInfo: {0}\n", err));
                    }

                    UpdateStreamInfo();

                    ret = m_Stream.GetArchivingState(m_AuthToken);
                    if (Error.Failed(ret))
                    {
                        string err = Error.GetString(ret);
                        ReportError(string.Format("Error in TTV_GetArchivingState: {0}\n", err));
                    }

                    break;
                }
                // Waiting for the start/stop callback
                case BroadcastState.Starting:
                case BroadcastState.Stopping:
                {
                    break;
                }
                // No action required
                case BroadcastState.FindingIngestServer:
                case BroadcastState.Authenticating:
                case BroadcastState.Initialized:
                case BroadcastState.Uninitialized:
                case BroadcastState.IngestTesting:
                {
                    break;
                }
                case BroadcastState.Broadcasting:
                case BroadcastState.Paused:
                {
                    UpdateStreamInfo();
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        protected virtual void UpdateStreamInfo()
        {
            System.DateTime now = System.DateTime.Now;
            System.TimeSpan delta = now - m_LastStreamInfoUpdateTime;

            // only check every so often
            if (delta.TotalSeconds < s_StreamInfoUpdateInterval)
            {
                return;
            }

            m_LastStreamInfoUpdateTime = now;

            ErrorCode ret = m_Stream.GetStreamInfo(m_AuthToken, m_UserName);
            if (Error.Failed(ret))
            {
                string err = Error.GetString(ret);
                ReportError(string.Format("Error in TTV_GetStreamInfo: {0}\n", err));
            }
        }

        #region Ingest Testing

        /// <summary>
        /// If the user is logged in and ready to broadcast this will kick off an asynchronous test of bandwidth to all ingest servers.
        /// This will in turn fill in the bitrate fields in the ingest list.
        /// </summary>
        /// <returns>The IngestTester instance that is valid during the test.</returns>
        public virtual IngestTester StartIngestTest()
        {
            if (!this.IsReadyToBroadcast || m_IngestList == null)
            {
                return null;
            }

            if (this.IsIngestTesting)
            {
                return null;
            }

            m_IngestTester = new IngestTester(m_Stream, m_IngestList);
            m_IngestTester.Start();

            SetBroadcastState(BroadcastState.IngestTesting);

            return m_IngestTester;
        }

        /// <summary>
        /// Asynchronously cancels a currently underway ingest test.  
        /// </summary>
        public virtual void CancelIngestTest()
        {
            if (this.IsIngestTesting)
            {
                m_IngestTester.Cancel();
            }
        }

        #endregion

        #region Error Checking

        protected void FireFrameSubmissionIssue(ErrorCode ret)
        {
            try
            {
                if (this.FrameSubmissionIssue != null)
                {
                    this.FrameSubmissionIssue(ret);
                }
            }
            catch (Exception x)
            {
                ReportError(x.ToString());
            }
        }

        protected virtual bool CheckError(Twitch.ErrorCode err)
        {
            if (Error.Failed(err))
            {
                return false;
            }

            return true;
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
