using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Twitch.Broadcast
{
    /// <summary>
    /// Performs ingest bandwidth testing to determine the best server a user should connect to for broadcasting.  This will fill in the BitrateKbps 
    /// field of the given IngestServers.  Testing may take a while because there are several servers to test and each one may be tested for several
    /// seconds.  This class cannot be used independently from the BroadcastController (BC) and is single-use only.  Begin a new test via BC.StartIngestTest().
    /// 
    /// The IngestTester (IT) will fire events to indicate state changes such as the starting and stopping of a server test and completion of all tests.
    /// 
    /// Progress of the overall test and of the current server can be gotten from TotalProgress and ServerProgress, respectively.  You may want to display a progress bar for each.
    /// 
    /// The whole test can be cancelled via Cancel().  The current server test can be skipped via SkipCurrentServer() which will trigger the testing of the next server.
    /// </summary>
    public class IngestTester : IStreamCallbacks, IStatCallbacks
    {
        public delegate void TestStateChangedDelegate(IngestTester source, TestState state);

        public enum TestState
        {
            Uninitalized,
            Starting,
            ConnectingToServer,
            TestingServer,
            DoneTestingServer,
            Finished,
            Cancelled,
            Failed
        }

        /// <summary>
        /// Event fired when the test changes states.
        /// </summary>
        public event TestStateChangedDelegate OnTestStateChanged;

        protected const bool k_AsyncStartStop = true;

        protected Stream m_Stream = null;
        protected IngestServer[] m_IngestList = null;

        protected TestState m_TestState = TestState.Uninitalized;
        protected long m_TestDurationMilliseconds = 8000;
        protected long m_DelayBetweenServerTestsMilliseconds = 1000;
        protected UInt64 m_TotalSent = 0;
        protected RTMPState m_RTMPState = RTMPState.Invalid;
        protected VideoParams m_IngestTestVideoParams = null;
        protected AudioParams m_IngestTestAudioParams = null;
        protected System.Diagnostics.Stopwatch m_Timer = new System.Diagnostics.Stopwatch();
        protected List<UIntPtr> m_IngestBuffers = null;
        protected bool m_ServerTestSucceeded = false;
        protected IStreamCallbacks m_PreviousStreamCallbacks = null;
        protected IStatCallbacks m_PreviousStatCallbacks = null;
        protected IngestServer m_CurrentServer = null;
        protected bool m_CancelTest = false;
        protected bool m_SkipServer = false;
        protected int m_CurrentServerIndex = -1;
        protected int m_BufferIndex = 0;
        protected UInt64 m_LastTotalSent = 0;
        protected float m_TotalProgress = 0;
        protected float m_ServerProgress = 0;
        protected bool m_WaitingForStartStopCallback = false;


        public TestState State
        {
            get { return m_TestState; }
        }

        /// <summary>
        /// The server currently being tested.  Will be null at the beginning and end of testing.
        /// </summary>
        public IngestServer CurrentServer
        {
            get { return m_CurrentServer; }
        }

        /// <summary>
        /// The list being tested.
        /// </summary>
        public IngestServer[] IngestList
        {
            get { return m_IngestList; }
        }

        /// <summary>
        /// Whether or not all testing is complete.
        /// </summary>
        public bool IsDone
        {
            get { return m_TestState == TestState.Finished || m_TestState == TestState.Cancelled || m_TestState == TestState.Failed; }
        }

        /// <summary>
        /// The number of milliseconds to test each server.  The longer the time, the more accurate the testing.
        /// </summary>
        public long TestDurationMilliseconds
        {
            get { return m_TestDurationMilliseconds; }
            set { m_TestDurationMilliseconds = value; }
        }

        /// <summary>
        /// The overall progress between [0,1].
        /// </summary>
        public float TotalProgress
        {
            get { return m_TotalProgress; }
        }

        /// <summary>
        /// The progress for the current server [0,1].
        /// </summary>
        public float ServerProgress
        {
            get { return m_ServerProgress; }
        }

        #region IStreamCallbacks

        void IStreamCallbacks.RequestAuthTokenCallback(ErrorCode result, AuthToken authToken)
        {
        }

        void IStreamCallbacks.LoginCallback(ErrorCode result, ChannelInfo channelInfo)
        {
        }

        void IStreamCallbacks.GetIngestServersCallback(ErrorCode result, IngestList ingestList)
        {
        }

        void IStreamCallbacks.GetUserInfoCallback(ErrorCode result, UserInfo userInfo)
        {
        }

        void IStreamCallbacks.GetStreamInfoCallback(ErrorCode result, StreamInfo streamInfo)
        {
        }

        void IStreamCallbacks.GetArchivingStateCallback(ErrorCode result, ArchivingState state)
        {
        }

        void IStreamCallbacks.RunCommercialCallback(ErrorCode result)
        {
        }

        void IStreamCallbacks.SetStreamInfoCallback(ErrorCode result)
        {
        }

        void IStreamCallbacks.GetGameNameListCallback(ErrorCode result, GameInfoList list)
        {
        }

        void IStreamCallbacks.BufferUnlockCallback(UIntPtr buffer)
	    {
	    }

        void IStreamCallbacks.StartCallback(ErrorCode ret)
        {
            m_WaitingForStartStopCallback = false;

            // started
            if (Error.Succeeded(ret))
            {
                SetTestState(TestState.ConnectingToServer);

                m_Timer.Reset();
                m_Timer.Start();
            }
            // failed to start so skip it
            else
            {
                m_ServerTestSucceeded = false;
                SetTestState(TestState.DoneTestingServer);
            }
        }

        void IStreamCallbacks.StopCallback(ErrorCode ret)
        {
            m_WaitingForStartStopCallback = false;

            SetTestState(TestState.DoneTestingServer);
        }

        void IStreamCallbacks.SendActionMetaDataCallback(ErrorCode ret)
        {
        }

        void IStreamCallbacks.SendStartSpanMetaDataCallback(ErrorCode ret)
        {
        }

        void IStreamCallbacks.SendEndSpanMetaDataCallback(ErrorCode ret)
        {
        }
    
        #endregion

        #region IStatCallbacks

        void IStatCallbacks.StatCallback(StatType type, ulong data)
        {
            switch (type)
            {
                case StatType.TTV_ST_RTMPSTATE:
                    m_RTMPState = (RTMPState)data;
                    break;

                case StatType.TTV_ST_RTMPDATASENT:
                    m_TotalSent = data;
                    break;
            }
        }

        #endregion


        internal IngestTester(Stream stream, IngestList ingestList)
        {
            m_Stream = stream;
            m_IngestList = ingestList.Servers;
        }

        internal IngestTester(Stream stream, IngestServer[] ingestList)
        {
            m_Stream = stream;
            m_IngestList = ingestList;
        }

        ~IngestTester()
        {
            if (m_CurrentServer != null)
            {
                CleanupServerTest(m_CurrentServer);
            }

            Cleanup();
        }

        /// <summary>
        /// Begins the ingest testing.
        /// </summary>
        public void Start()
        {
            // can only run it once per instance
            if (m_TestState != TestState.Uninitalized)
            {
                return;
            }

            m_CurrentServerIndex = 0;
            m_CancelTest = false;
            m_SkipServer = false;

            m_PreviousStatCallbacks = m_Stream.StatCallbacks;
            m_Stream.StatCallbacks = this;

            m_PreviousStreamCallbacks = m_Stream.StreamCallbacks;
            m_Stream.StreamCallbacks = this;

            m_IngestTestVideoParams = new VideoParams();
            m_IngestTestVideoParams.TargetFps = Twitch.Broadcast.Constants.TTV_MAX_FPS;
            m_IngestTestVideoParams.MaxKbps = Twitch.Broadcast.Constants.TTV_MAX_BITRATE;
            m_IngestTestVideoParams.OutputWidth = 1280;
            m_IngestTestVideoParams.OutputHeight = 720;
            m_IngestTestVideoParams.PixelFormat = PixelFormat.TTV_PF_BGRA;
            m_IngestTestVideoParams.DisableAdaptiveBitrate = true;
            m_IngestTestVideoParams.VerticalFlip = false;

            m_Stream.GetDefaultParams(m_IngestTestVideoParams);

            m_IngestTestAudioParams = new AudioParams();
            m_IngestTestAudioParams.AudioEnabled = false;
            m_IngestTestAudioParams.EnableMicCapture = false;
            m_IngestTestAudioParams.EnablePlaybackCapture = false;
            m_IngestTestAudioParams.EnablePassthroughAudio = false;

            m_IngestBuffers = new List<UIntPtr>();

            // allocate some buffers
            int numFrames = 3;

            for (int i = 0; i < numFrames; ++i)
            {
                UIntPtr buffer = UIntPtr.Zero;
                uint size = m_IngestTestVideoParams.OutputWidth * m_IngestTestVideoParams.OutputHeight * 4;

                m_Stream.AllocateFrameBuffer(size, out buffer);

                if (buffer == UIntPtr.Zero)
                {
                    Cleanup();
                    SetTestState(TestState.Failed);
                    return;
                }

                m_Stream.RandomizeFrameBuffer(buffer, size);
                
                m_IngestBuffers.Add(buffer);
            }

            SetTestState(TestState.Starting);

            m_Timer.Reset();
            m_Timer.Start();
        }

        /// <summary>
        /// Updates the internal state of the tester.  This should be called as frequently as possible.
        /// </summary>
        public void Update()
        {
            if (this.IsDone || m_TestState == TestState.Uninitalized)
            {
                return;
            }

            if (m_WaitingForStartStopCallback)
            {
                return;
            }

            if (m_CancelTest)
            {
                SetTestState(TestState.Cancelled);
            }

            switch (m_TestState)
            {
                case TestState.Starting:
                case TestState.DoneTestingServer:
                {
                    // cleanup the previous server test
                    if (m_CurrentServer != null)
                    {
                        if (m_SkipServer || !m_ServerTestSucceeded)
                        {
                            m_CurrentServer.BitrateKbps = 0;
                        }

                        CleanupServerTest(m_CurrentServer);
                        m_CurrentServer = null;
                    }
                    // wait for the stop to complete before starting the next server
                    else if (!m_WaitingForStartStopCallback &&
                             m_Timer.ElapsedMilliseconds >= m_DelayBetweenServerTestsMilliseconds)
                    {
                        m_Timer.Stop();

                        m_SkipServer = false;
                        m_ServerTestSucceeded = true;

                        if (m_TestState != TestState.Starting)
                        {
                            m_CurrentServerIndex++;
                        }

                        // start the next server test
                        if (m_CurrentServerIndex < m_IngestList.Length)
                        {
                            m_CurrentServer = m_IngestList[m_CurrentServerIndex];
                            StartServerTest(m_CurrentServer);
                        }
                        // done testing all servers
                        else
                        {
                            SetTestState(TestState.Finished);
                        }
                    }

                    break;
                }
                case TestState.ConnectingToServer:
                case TestState.TestingServer:
                {
                    UpdateServerTest(m_CurrentServer);
                    break;
                }
                default:
                {
                    break;
                }
            }

            UpdateProgress();

            // test finished
            if (m_TestState == TestState.Cancelled || m_TestState == TestState.Finished)
            {
                if (m_CurrentServer != null)
                {
                    if (m_TestState == TestState.Cancelled)
                    {
                        m_CurrentServer.BitrateKbps = 0;
                    }

                    CleanupServerTest(m_CurrentServer);
                    m_CurrentServer = null;
                } 
                    
                if (m_IngestBuffers != null)
                {
                    Cleanup();
                }
            }
        }

        /// <summary>
        /// Skips the server currently being tested.  It will leave a 0 value in the bitrate field of the server.
        /// </summary>
        public void SkipCurrentServer()
        {
            if (this.IsDone)
            {
                return;
            }

            switch (m_TestState)
            {
                case TestState.ConnectingToServer:
                case TestState.TestingServer:
                    m_SkipServer = true;
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Cancels the ingest testing.  This may leave invalid values in any servers which did not complete testing.
        /// </summary>
        public void Cancel()
        {
            if (this.IsDone)
            {
                return;
            }
    
            m_CancelTest = true;
        }

        protected bool StartServerTest(IngestServer server)
        {
            // reset the test
            m_ServerTestSucceeded = true;
            m_TotalSent = 0;
            m_RTMPState = RTMPState.Idle;
            m_CurrentServer = server;

            // start the stream asynchronously
            SetTestState(TestState.ConnectingToServer);
            m_WaitingForStartStopCallback = true;
            ErrorCode ret = m_Stream.Start(m_IngestTestVideoParams, m_IngestTestAudioParams, server, StartFlags.TTV_Start_BandwidthTest, k_AsyncStartStop);
            if (Error.Failed(ret))
            {
                m_ServerTestSucceeded = false;
                SetTestState(TestState.DoneTestingServer);
                return false;
            }
                
            // the amount of data sent before the test of this server starts
            m_LastTotalSent = m_TotalSent;

            server.BitrateKbps = 0;
            m_BufferIndex = 0;

            return true;
        }

        protected void CleanupServerTest(IngestServer server)
        {
            m_WaitingForStartStopCallback = true;
            m_Stream.Stop(k_AsyncStartStop);

            m_Stream.PollStats();
        }

        protected void UpdateProgress()
        {
            float elapsed = (float)m_Timer.ElapsedMilliseconds;

            switch (m_TestState)
            {
                case TestState.Uninitalized:
                case TestState.Starting:
                case TestState.ConnectingToServer:
                case TestState.Finished:
                case TestState.Cancelled:
                case TestState.Failed:
                {
                    m_ServerProgress = 0.0f;
                    break;
                }
                case TestState.DoneTestingServer:
                {
                    m_ServerProgress = 1.0f;
                    break;
                }
                default:
                {
                    m_ServerProgress = elapsed / (float)m_TestDurationMilliseconds;
                    break;
                }
            }

            switch (m_TestState)
            {
                case TestState.Finished:
                case TestState.Cancelled:
                case TestState.Failed:
                {
                    m_TotalProgress = 1.0f;
                    break;
                }
                default:
                {
                    m_TotalProgress = (float)m_CurrentServerIndex / (float)m_IngestList.Length;
                    m_TotalProgress += m_ServerProgress / m_IngestList.Length;
                    break;
                }
            }
        }

        protected bool UpdateServerTest(IngestServer server)
        {
            if (m_SkipServer || m_Timer.ElapsedMilliseconds >= m_TestDurationMilliseconds)
            {
                SetTestState(TestState.DoneTestingServer);
                return true;
            }

            // not started yet
            if (m_WaitingForStartStopCallback)
            {
                return true;
            }

            ErrorCode ret = m_Stream.SubmitVideoFrame(m_IngestBuffers[m_BufferIndex]);
            if (Error.Failed(ret))
            {
                m_ServerTestSucceeded = false;
                SetTestState(TestState.DoneTestingServer);
                return false;
            }

            m_BufferIndex = (m_BufferIndex + 1) % m_IngestBuffers.Count;

            m_Stream.PollStats();

            // connected and sending video
            if (m_RTMPState == RTMPState.SendVideo)
            {
                SetTestState(TestState.TestingServer);

                long elapsed = m_Timer.ElapsedMilliseconds;
                if (elapsed > 0 && m_TotalSent > m_LastTotalSent)
                {
                    server.BitrateKbps = (float)(m_TotalSent * 8) / (float)(m_Timer.ElapsedMilliseconds);
                    m_LastTotalSent = m_TotalSent;
                }
            }

            return true;
        }

        protected void Cleanup()
        {
            m_CurrentServer = null;

            // free the buffers
            if (m_IngestBuffers != null)
            {
                for (int i = 0; i < m_IngestBuffers.Count; ++i)
                {
                    m_Stream.FreeFrameBuffer(m_IngestBuffers[i]);
                }

                m_IngestBuffers = null;
            }

            if (m_Stream.StatCallbacks == this)
            {
                m_Stream.StatCallbacks = m_PreviousStatCallbacks;
                m_PreviousStatCallbacks = null;
            }

            if (m_Stream.StreamCallbacks == this)
            {
                m_Stream.StreamCallbacks = m_PreviousStreamCallbacks;
                m_PreviousStreamCallbacks = null;
            }
        }

        protected void SetTestState(TestState state)
        {
            if (state == m_TestState)
            {
                return;
            }

            m_TestState = state;

            if (OnTestStateChanged != null)
            {
                OnTestStateChanged(this, state);
            }
        }
    }
}
