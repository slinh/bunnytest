using System;
using System.Collections.Generic;
using Twitch;

namespace Twitch
{
    public partial class BroadcastController
    {
        #region Member Variables

        protected string m_ClientId = "";
        protected string m_ClientSecret = "";
        protected string m_CaCertFilePath = "";
        protected string m_DllPath = "";
        protected bool m_EnableAudio = true;

        protected Stream m_Stream = null;
        protected List<IntPtr> m_CaptureBuffers = new List<IntPtr>();
        protected List<IntPtr> m_FreeBufferList = new List<IntPtr>();

        #endregion

        #region Properties

        protected string DefaultCaCertFilePath
        {
            get { return "./curl-ca-bundle.crt"; }
        }

        #endregion

        #region IStreamCallbacks

        void IStreamCallbacks.BufferUnlockCallback(IntPtr buffer)
        {
            // Put back on the free list
            m_FreeBufferList.Add(buffer);
        }

        #endregion

        public BroadcastController()
        {
            m_Stream = new Stream(new DesktopStreamAPI());
        }

        protected PixelFormat DeterminePixelFormat()
        {
            return PixelFormat.TTV_PF_BGRA;
        }

        protected bool AllocateBuffers(uint width, uint height)
        {
            // Allocate exactly 3 buffers to use as the capture destination while streaming.
            // These buffers are passed to the SDK.
            for (uint i = 0; i < 3; ++i)
            {
                IntPtr buffer = IntPtr.Zero;
                ErrorCode ret = m_Stream.AllocateFrameBuffer(width * height * 4, out buffer);
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

        protected void CleanupBuffers()
        {
            // Delete the capture buffers
            for (int i = 0; i < m_CaptureBuffers.Count; ++i)
            {
                IntPtr buffer = m_CaptureBuffers[i];
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

        public IntPtr GetNextFreeBuffer()
        {
            if (m_FreeBufferList.Count == 0)
            {
                //ReportError(string.Format("Out of free buffers, this should never happen"));
                return IntPtr.Zero;
            }

            IntPtr buffer = m_FreeBufferList[m_FreeBufferList.Count - 1];
            m_FreeBufferList.RemoveAt(m_FreeBufferList.Count - 1);

            return buffer;
        }

        public ErrorCode SubmitFrame(IntPtr buffer)
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

                try
                {
                    if (FrameSubmissionIssue != null)
                    {
                        FrameSubmissionIssue(ret);
                    }
                }
                catch (Exception x)
                {
                    ReportError(x.ToString());
                }
            }

            return ret;
        }

        protected void PlatformUpdate()
        {
        }

        protected bool CheckError(Twitch.ErrorCode err)
        {
            if (Error.Failed(err))
            {
                System.Windows.Forms.MessageBox.Show(err.ToString());
                return false;
            }

            return true;
        }

        protected void ReportError(string err)
        {
            Console.Error.WriteLine(err);
        }

        protected void ReportWarning(string err)
        {
            Console.Error.WriteLine(err);
        }
    }
}
