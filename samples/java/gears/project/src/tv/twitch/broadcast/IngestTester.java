package tv.twitch.broadcast;

import java.util.ArrayList;
import java.util.List;

import tv.twitch.AuthToken;
import tv.twitch.ErrorCode;

/**
 * Performs ingest bandwidth testing to determine the best server a user should connect to for broadcasting.  This will fill in the BitrateKbps 
 * field of the given IngestServers.  Testing may take a while because there are several servers to test and each one may be tested for several
 * seconds.  You may want to display a progress bar each for the current server and overall progress.
 */
public class IngestTester implements IStreamCallbacks, IStatCallbacks
{
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

	public interface Listener
	{
		/**
		 * Event fired when the test changes states.
		 * @param source
		 * @param state
		 */
		void onIngestTestStateChanged(IngestTester source, TestState state);
	}
    
	protected final boolean k_AsyncStartStop = true;
	
    protected Listener m_Listener = null;

    protected Stream m_Stream = null;
    protected IngestList m_IngestList = null;

    protected TestState m_TestState = TestState.Uninitalized;
    protected long m_TestDurationMilliseconds = 8000;
    protected long m_DelayBetweenServerTestsMilliseconds = 1000;
    protected long m_TotalSent = 0;
    protected RTMPState m_RTMPState = RTMPState.Invalid;
    protected VideoParams m_IngestTestVideoParams = null;
    protected AudioParams m_IngestTestAudioParams = null;
    protected long m_StartTimeMilliseconds = 0;
    protected List<FrameBuffer> m_IngestBuffers = null;
    protected boolean m_ServerTestSucceeded = false;
    protected IStreamCallbacks m_PreviousStreamCallbacks = null;
    protected IStatCallbacks m_PreviousStatCallbacks = null;
    protected IngestServer m_CurrentServer = null;
    protected boolean m_CancelTest = false;
    protected boolean m_SkipServer = false;
    protected int m_CurrentServerIndex = -1;
    protected int m_BufferIndex = 0;
    protected long m_LastTotalSent = 0;
    protected float m_TotalProgress = 0;
    protected float m_ServerProgress = 0;
    protected boolean m_WaitingForStartStopCallback = false;

    public void setListener(Listener listener)
    {
    	m_Listener = listener;
    }
    public Listener getListener()
    {
    	return m_Listener;
    }
    
    public TestState getState()
    {
        return m_TestState;
    }

    public IngestServer getCurrentServer()
    {
        return m_CurrentServer;
    }

    public IngestList getIngestList()
    {
        return m_IngestList;
    }

    public boolean getIsDone()
    {
        return m_TestState == TestState.Finished || m_TestState == TestState.Cancelled || m_TestState == TestState.Failed;
    }

    public long getTestDurationMilliseconds()
    {
        return m_TestDurationMilliseconds;
    }
    public void setTestDurationMilliseconds(long value)
    {
        m_TestDurationMilliseconds = value;
    }

    /**
     * The overall progress between [0,1].
     * @return
     */
    public float getTotalProgress()
    {
        return m_TotalProgress;
    }

    /**
     * The progress for the current server [0,1].
     * @return
     */
    public float getServerProgress()
    {
        return m_ServerProgress;
    }

    //#region IStreamCallbacks

    public void requestAuthTokenCallback(ErrorCode result, AuthToken authToken)
    {
    }

    public void loginCallback(ErrorCode result, ChannelInfo channelInfo)
    {
    }

    public void getIngestServersCallback(ErrorCode result, IngestList ingestList)
    {
    }

    public void getUserInfoCallback(ErrorCode result, UserInfo userInfo)
    {
    }

    public void getStreamInfoCallback(ErrorCode result, StreamInfo streamInfo)
    {
    }

    public void getArchivingStateCallback(ErrorCode result, ArchivingState state)
    {
    }

    public void runCommercialCallback(ErrorCode result)
    {
    }

    public void setStreamInfoCallback(ErrorCode result)
    {
    }

    public void getGameNameListCallback(ErrorCode result, GameInfoList list)
    {
    }

    public void bufferUnlockCallback(long address)
    {
    }
    
    public void startCallback(ErrorCode ret)
    {
        m_WaitingForStartStopCallback = false;

        // started
        if (ErrorCode.succeeded(ret))
        {
            setTestState(TestState.ConnectingToServer);

            m_StartTimeMilliseconds = System.currentTimeMillis();
        }
        // failed to start so skip it
        else
        {
            m_ServerTestSucceeded = false;
            setTestState(TestState.DoneTestingServer);
        }
    }

    public void stopCallback(ErrorCode ret)
    {
        m_WaitingForStartStopCallback = false;

        setTestState(TestState.DoneTestingServer);
    }
    
    public void sendActionMetaDataCallback(ErrorCode ret)
    {
    }
    
    public void sendStartSpanMetaDataCallback(ErrorCode ret)
    {
    }
    
    public void sendEndSpanMetaDataCallback(ErrorCode ret)
    {
    }
    
    //#endregion

    //#region IStatCallbacks

    public void statCallback(StatType type, long data)
    {
        switch (type)
        {
            case TTV_ST_RTMPSTATE:
                m_RTMPState = RTMPState.lookupValue((int)data);
                break;

            case TTV_ST_RTMPDATASENT:
                m_TotalSent = data;
                break;
        }
    }

    //#endregion


    public IngestTester(Stream stream, IngestList ingestList)
    {
        m_Stream = stream;
        m_IngestList = ingestList;
    }
    
    protected void finalize() throws Throwable
    {
        if (m_CurrentServer != null)
        {
            cleanupServerTest(m_CurrentServer);
        }
        
        cleanup();
    	
        super.finalize();
    }
    
    /**
     * Begins the ingest testing.
     */
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

        m_PreviousStatCallbacks = m_Stream.getStatCallbacks();
        m_Stream.setStatCallbacks(this);

        m_PreviousStreamCallbacks = m_Stream.getStreamCallbacks();
        m_Stream.setStreamCallbacks(this);

        m_IngestTestVideoParams = new VideoParams();
        m_IngestTestVideoParams.targetFps = tv.twitch.broadcast.Constants.TTV_MAX_FPS;
        m_IngestTestVideoParams.maxKbps = tv.twitch.broadcast.Constants.TTV_MAX_BITRATE;
        m_IngestTestVideoParams.outputWidth = 1280;
        m_IngestTestVideoParams.outputHeight = 720;
        m_IngestTestVideoParams.pixelFormat = PixelFormat.TTV_PF_BGRA;
        m_IngestTestVideoParams.encodingCpuUsage = EncodingCpuUsage.TTV_ECU_HIGH;
        m_IngestTestVideoParams.disableAdaptiveBitrate = true;
        m_IngestTestVideoParams.verticalFlip = false;
        
        m_Stream.getDefaultParams(m_IngestTestVideoParams);

        m_IngestTestAudioParams = new AudioParams();
        m_IngestTestAudioParams.audioEnabled = false;
        m_IngestTestAudioParams.enableMicCapture = false;
        m_IngestTestAudioParams.enablePlaybackCapture = false;
        m_IngestTestAudioParams.enablePassthroughAudio = false;

        m_IngestBuffers = new ArrayList<FrameBuffer>();

        // allocate some buffers
        int numFrames = 3;

        for (int i = 0; i < numFrames; ++i)
        {
        	FrameBuffer buffer = m_Stream.allocateFrameBuffer(m_IngestTestVideoParams.outputWidth * m_IngestTestVideoParams.outputHeight * 4);
            if (!buffer.getIsValid())
            {
            	cleanup();
            	setTestState(TestState.Failed);
                return;
            }

            m_IngestBuffers.add(buffer);

            m_Stream.randomizeFrameBuffer(buffer);
        }

        setTestState(TestState.Starting);
        
        m_StartTimeMilliseconds = System.currentTimeMillis();
    }

    /**
     * Updates the internal state of the tester.  This should be called as frequently as possible.
     */
    public void update()
    {
        if (this.getIsDone() || m_TestState == TestState.Uninitalized)
        {
            return;
        }

        if (m_WaitingForStartStopCallback)
        {
            return;
        }

        if (m_CancelTest)
        {
            setTestState(TestState.Cancelled);
        }

        switch (m_TestState)
        {
            case Starting:
            case DoneTestingServer:
            {
                // cleanup the previous server test
                if (m_CurrentServer != null)
                {
                    if (m_SkipServer || !m_ServerTestSucceeded)
                    {
                        m_CurrentServer.bitrateKbps = 0;
                    }

                    cleanupServerTest(m_CurrentServer);
                    m_CurrentServer = null;
                }
                // wait for the stop to complete before starting the next server
                else if (!m_WaitingForStartStopCallback &&
                		 elapsedMilliseconds() >= m_DelayBetweenServerTestsMilliseconds)
                {
                	m_StartTimeMilliseconds = 0;

                    m_SkipServer = false;
                    m_ServerTestSucceeded = true;

                    if (m_TestState != TestState.Starting)
                    {
                        m_CurrentServerIndex++;
                    }

                    // start the next server test
                    if (m_CurrentServerIndex < m_IngestList.getServers().length)
                    {
                        m_CurrentServer = m_IngestList.getServers()[m_CurrentServerIndex];
                        startServerTest(m_CurrentServer);
                    }
                    // done testing all servers
                    else
                    {
                        setTestState(TestState.Finished);
                    }
                }
            	break;
            }
            case ConnectingToServer:
            case TestingServer:
            {
                updateServerTest(m_CurrentServer);
                break;
            }
            default:
            {
                break;
            }
        }

        updateProgress();

        // test finished
        if (m_TestState == TestState.Cancelled || m_TestState == TestState.Finished)
        {
            if (m_CurrentServer != null)
            {
                if (m_TestState == TestState.Cancelled)
                {
                    m_CurrentServer.bitrateKbps = 0;
                }

                cleanupServerTest(m_CurrentServer);
                m_CurrentServer = null;
            } 
                
            if (m_IngestBuffers != null)
            {
                cleanup();
            }
        }
    }

    /**
     * Skips the server currently being tested.  It will leave a 0 value in the bitrate field of the server.
     */
    public void skipCurrentServer()
    {
        if (this.getIsDone())
        {
            return;
        }
        
        switch (m_TestState)
        {
            case ConnectingToServer:
            case TestingServer:
                m_SkipServer = true;
                break;
            default:
                return;
        }
    }

    /**
     * Cancels the ingest testing.  This may leave invalid values in any servers which did not complete testing.
     */
    public void Cancel()
    {
        if (this.getIsDone())
        {
            return;
        }

      	m_CancelTest = true;
    }

    protected boolean startServerTest(IngestServer server)
    {
        // reset the test
        m_ServerTestSucceeded = true;
        m_TotalSent = 0;
        m_RTMPState = RTMPState.Idle;
        m_CurrentServer = server;

        // start the stream
        setTestState(TestState.ConnectingToServer);
        m_WaitingForStartStopCallback = true;
        ErrorCode ret = m_Stream.start(m_IngestTestVideoParams, m_IngestTestAudioParams, server, StartFlags.TTV_Start_BandwidthTest, k_AsyncStartStop);
        if (ErrorCode.failed(ret))
        {
            m_ServerTestSucceeded = false;
            setTestState(TestState.DoneTestingServer);
            return false;
        }

        // the amount of data sent before the test of this server starts
        m_LastTotalSent = m_TotalSent;

        server.bitrateKbps = 0;
        m_BufferIndex = 0;

        return true;
    }

    protected void cleanupServerTest(IngestServer server)
    {
        m_WaitingForStartStopCallback = true;
        m_Stream.stop(k_AsyncStartStop);

        m_Stream.pollStats();
    }

    protected long elapsedMilliseconds()
    {
    	return System.currentTimeMillis() - m_StartTimeMilliseconds;
    }
    
    protected void updateProgress()
    {
        float elapsed = (float)elapsedMilliseconds();

        switch (m_TestState)
        {
            case Uninitalized:
            case Starting:
            case ConnectingToServer:
            case Finished:
            case Cancelled:
            case Failed:
            {
                m_ServerProgress = 0.0f;
                break;
            }
            case DoneTestingServer:
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
            case Finished:
            case Cancelled:
            case Failed:
            {
                m_TotalProgress = 1.0f;
                break;
            }
            default:
            {
                m_TotalProgress = (float)m_CurrentServerIndex / (float)m_IngestList.getServers().length;
                m_TotalProgress += m_ServerProgress / m_IngestList.getServers().length;
                break;
            }
        }
    }

    protected boolean updateServerTest(IngestServer server)
    {
        if (m_SkipServer || elapsedMilliseconds() >= m_TestDurationMilliseconds)
        {
            setTestState(TestState.DoneTestingServer);
            return true;
        }

        // not started yet
        if (m_WaitingForStartStopCallback)
        {
            return true;
        }

        ErrorCode ret = m_Stream.submitVideoFrame(m_IngestBuffers.get(m_BufferIndex));
        if (ErrorCode.failed(ret))
        {
            m_ServerTestSucceeded = false;
            setTestState(TestState.DoneTestingServer);
            return false;
        }

        m_BufferIndex = (m_BufferIndex+1) % m_IngestBuffers.size();

        m_Stream.pollStats();

        // connected and sending video
        if (m_RTMPState == RTMPState.SendVideo)
        {
            setTestState(TestState.TestingServer);

            long elapsed = elapsedMilliseconds();
            if (elapsed > 0 && m_TotalSent > m_LastTotalSent)
            {
            	server.bitrateKbps = (float)(m_TotalSent * 8) / (float)elapsedMilliseconds();
                m_LastTotalSent = m_TotalSent;
            }
        }

        return true;
    }

    protected void cleanup()
    {
        m_CurrentServer = null;

        // free the buffers
        if (m_IngestBuffers != null)
        {
	        for (int i = 0; i < m_IngestBuffers.size(); ++i)
	        {
	        	m_IngestBuffers.get(i).free();
	        }
	        
	        m_IngestBuffers = null;
        }

        if (m_Stream.getStatCallbacks() == this)
        {
	        m_Stream.setStatCallbacks(m_PreviousStatCallbacks);
	        m_PreviousStatCallbacks = null;
        }
        
        if (m_Stream.getStreamCallbacks() == this)
        {
	        m_Stream.setStreamCallbacks(m_PreviousStreamCallbacks);
	        m_PreviousStreamCallbacks = null;
        }
    }

    protected void setTestState(TestState state)
    {
        if (state == m_TestState)
        {
            return;
        }

        m_TestState = state;

        if (m_Listener != null)
        {
        	m_Listener.onIngestTestStateChanged(this, state);
        }
    }
}
