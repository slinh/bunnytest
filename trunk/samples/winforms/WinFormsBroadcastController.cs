using System;
using System.Collections.Generic;
using Twitch;

namespace Twitch.Broadcast
{
    public class WinFormsBroadcastController : BroadcastController
    {
        #region Member Variables

        protected string m_ClientId = "";
        protected string m_ClientSecret = "";
        protected string m_CaCertFilePath = "";
        protected bool m_EnableAudio = true;

        protected List<UIntPtr> m_CaptureBuffers = new List<UIntPtr>();
        protected List<UIntPtr> m_FreeBufferList = new List<UIntPtr>();

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

        public override bool EnableAudio
        {
            get { return m_EnableAudio; }
            set { m_EnableAudio = value; }
        }

        public override bool IsAudioSupported
        {
            get
            {
                // http://stackoverflow.com/questions/2819934/detect-windows-7-in-net

                OperatingSystem os = System.Environment.OSVersion;

                if (os.Platform != PlatformID.Win32Windows)
                {
                    Version v = os.Version;

                    if (v.Major < 6) // Windows Vista
                    {
                        return false;
                    }

                    return true;
                }

                // not a valid Windows
                return false;
            }
        }

        public override bool IsPlatformSupported
        {
            get
            {
                // http://stackoverflow.com/questions/2819934/detect-windows-7-in-net

                OperatingSystem os = System.Environment.OSVersion;

                if (os.Platform != PlatformID.Win32Windows)
                {
                    Version v = os.Version;

                    if (v.Major < 6) // Windows Vista
                    {
                        return false;
                    }

                    return true;
                }

                // not a valid Windows
                return false;
            }
        }

        #endregion

        public WinFormsBroadcastController()
        {
            m_Core = new Twitch.Core(new Twitch.StandardCoreAPI());
            m_Stream = new Twitch.Broadcast.Stream(new Twitch.Broadcast.DesktopStreamAPI());
        }

        protected override void HandleBufferUnlock(UIntPtr buffer)
        {
            // Put back on the free list
            m_FreeBufferList.Add(buffer);
        }
        
        protected override bool AllocateBuffers()
        {
            // Allocate exactly 3 buffers to use as the capture destination while streaming.
            // These buffers are passed to the SDK.
            for (uint i = 0; i < 3; ++i)
            {
                UIntPtr buffer = UIntPtr.Zero;
                ErrorCode ret = m_Stream.AllocateFrameBuffer(m_VideoParams.OutputWidth * m_VideoParams.OutputHeight * 4, out buffer);
                if (Error.Failed(ret))
                {
                    string err = Error.GetString(ret);
                    ReportError(string.Format("Error while allocating frame buffer: {0}", err));
                    return false;
                }

                m_CaptureBuffers.Add(buffer);
                m_FreeBufferList.Add(buffer);
            }

            return true;
        }

        protected override void CleanupBuffers()
        {
            // Delete the capture buffers
            for (int i = 0; i < m_CaptureBuffers.Count; ++i)
            {
                UIntPtr buffer = m_CaptureBuffers[i];
                ErrorCode ret = m_Stream.FreeFrameBuffer(buffer);
                if (Error.Failed(ret))
                {
                    string err = Error.GetString(ret);
                    ReportError(string.Format("Error while freeing frame buffer: {0}", err));
                }
            }

            m_FreeBufferList.Clear();
            m_CaptureBuffers.Clear();
        }

        public UIntPtr GetNextFreeBuffer()
        {
            if (m_FreeBufferList.Count == 0)
            {
                //ReportError(string.Format("Out of free buffers, this should never happen"));
                return UIntPtr.Zero;
            }

            UIntPtr buffer = m_FreeBufferList[m_FreeBufferList.Count - 1];
            m_FreeBufferList.RemoveAt(m_FreeBufferList.Count - 1);

            return buffer;
        }

        public ErrorCode SubmitFrame(UIntPtr buffer)
        {
            if (this.IsPaused)
            {
                ResumeBroadcasting();
            }
            else if (!this.IsBroadcasting)
            {
                return ErrorCode.TTV_EC_STREAM_NOT_STARTED;
            }

            ErrorCode ret = m_Stream.SubmitVideoFrame(buffer);

            // if there is a problem when submitting a frame let the client know
            if (ret != ErrorCode.TTV_EC_SUCCESS)
            {
                string err = Error.GetString(ret);
                if (Error.Succeeded(ret))
                {
                    ReportWarning(string.Format("Warning in SubmitTexturePointer: {0}\n", err));
                }
                else
                {
                    ReportError(string.Format("Error in SubmitTexturePointer: {0}\n", err));

                    // free the buffer
                    m_FreeBufferList.Add(buffer);

                    StopBroadcasting();
                }

                FireFrameSubmissionIssue(ret);
            }

            return ret;
        }

        #region Error Checking
        
        protected override bool CheckError(Twitch.ErrorCode err)
        {
            if (Error.Failed(err))
            {
                System.Windows.Forms.MessageBox.Show(err.ToString());
                return false;
            }

            return true;
        }

        protected override void ReportError(string err)
        {
            System.Windows.Forms.MessageBox.Show(err, "Error");
        }

        protected override void ReportWarning(string err)
        {
            System.Windows.Forms.MessageBox.Show(err, "Warning");
        }

        #endregion
    }
}
