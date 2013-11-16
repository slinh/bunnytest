using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Twitch;

namespace Twitch.Broadcast
{
    public class XnaBroadcastController : BroadcastController
    {
        #region Member Variables

        protected string m_ClientId = "";
        protected string m_ClientSecret = "";
        protected string m_CaCertFilePath = "";
        protected string m_DllPath = "";
        protected bool m_EnableAudio = true;

        protected GraphicsDevice m_GraphicsDevice = null;

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

        protected Twitch.Broadcast.XnaStream XnaStream
        {
            get { return m_Stream as Twitch.Broadcast.XnaStream; }
        }

        #endregion

        public XnaBroadcastController()
        {
            m_Core = new Twitch.Core(new Twitch.StandardCoreAPI());
            m_Stream = new Twitch.Broadcast.XnaStream(new Twitch.Broadcast.DesktopXnaStreamAPI());
        }

        public ErrorCode SubmitFrame(RenderTarget2D renderTarget)
        {
            if (this.IsPaused)
            {
                ResumeBroadcasting();
            }
            else if (!this.IsBroadcasting)
            {
                return ErrorCode.TTV_EC_STREAM_NOT_STARTED;
            }
            else if (renderTarget == null)
            {
                return ErrorCode.TTV_EC_INVALID_ARG;
            }

            IntPtr p = GetNativeRenderTexturePointer(renderTarget);

            ErrorCode ret = this.XnaStream.SubmitRenderTarget(p);

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

                    StopBroadcasting();
                }

                FireFrameSubmissionIssue(ret);
            }

            return ret;
        }

        protected void PlatformUpdate()
        {
        }

        public void SetGraphicsDevice(GraphicsDevice device)
        {
            // no change
            if (device == m_GraphicsDevice)
            {
                return;
            }

            if (m_GraphicsDevice != null)
            {
                m_GraphicsDevice.DeviceResetting -= m_GraphicsDevice_DeviceResetting;
                m_GraphicsDevice.DeviceLost -= m_GraphicsDevice_DeviceLost;
                m_GraphicsDevice.DeviceReset -= m_GraphicsDevice_DeviceReset;
            }

            m_GraphicsDevice = device;

            if (m_GraphicsDevice != null)
            {
                m_GraphicsDevice.DeviceResetting += m_GraphicsDevice_DeviceResetting;
                m_GraphicsDevice.DeviceLost += m_GraphicsDevice_DeviceLost;
                m_GraphicsDevice.DeviceReset += m_GraphicsDevice_DeviceReset;
            }

            // stop broadcasting if there is no device
            if (m_GraphicsDevice == null && this.IsBroadcasting)
            {
                m_Stream.Stop(false);
                SetBroadcastState(BroadcastState.ReadyToBroadcast);
            }
            
            m_GraphicsDevice_DeviceReset(null, null);
        }

        protected void m_GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            this.XnaStream.SetGraphicsDevice(IntPtr.Zero);
        }

        protected void m_GraphicsDevice_DeviceLost(object sender, EventArgs e)
        {
            this.XnaStream.SetGraphicsDevice(IntPtr.Zero);
        }

        protected void m_GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            // set the device natively
            IntPtr d = IntPtr.Zero;

            if (m_GraphicsDevice != null)
            {
                d = GetNativeDevicePointer(m_GraphicsDevice);
            }

            this.XnaStream.SetGraphicsDevice(d);
        }

        protected unsafe IntPtr GetNativeDevicePointer(GraphicsDevice device)
        {
            if (device == null)
            {
                return IntPtr.Zero;
            }

            // http://social.msdn.microsoft.com/Forums/en-US/e1100102-2124-4671-a471-f8362f4dfa42/get-the-direct3d-device

            FieldInfo field = typeof(GraphicsDevice).GetField("pComPtr", BindingFlags.NonPublic | BindingFlags.Instance);
            Pointer reflectionPointer = (Pointer)field.GetValue(device);
            return (IntPtr)Pointer.Unbox(reflectionPointer);
        }

        protected unsafe IntPtr GetNativeRenderTexturePointer(RenderTarget2D rt)
        {
            if (rt == null)
            {
                return IntPtr.Zero;
            }

            // http://social.msdn.microsoft.com/Forums/en-US/e1100102-2124-4671-a471-f8362f4dfa42/get-the-direct3d-device

            FieldInfo field = typeof(RenderTarget2D).GetField("pComPtr", BindingFlags.NonPublic | BindingFlags.Instance);
            Pointer reflectionPointer = (Pointer)field.GetValue(rt);
            return (IntPtr)Pointer.Unbox(reflectionPointer);
        }

        #region Error Handling

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
