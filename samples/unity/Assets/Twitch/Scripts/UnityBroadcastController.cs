using System;
using System.Collections.Generic;
using UnityEngine;
using Twitch;
using ErrorCode = Twitch.ErrorCode;

namespace Twitch.Broadcast
{
    /// <summary>
    /// Once broadcasting begins, frames are submitted automatically for broadcasting in the Update() call.  The real time will be used to determine the rate of frame 
    /// submission and is independent of Time.time.  At most one frame will be submitted per Update() call so you need to ensure that it will be called frequently enough to 
    /// satisfy the FPS you have configured for broadcasting.  The maximum allowed time step will be a limiter in determing the maximum FPS in which the frames 
    /// can be submitted for broadcasting.  For 30 FPS (which is the recommended maximum FPS allowed) make sure the project has a value of 0.03333 seconds configured 
    /// for the "Maximum Allowed Timestep" property available via Edit->Project Settings->Time.
    /// 
    /// The general setup for broadcasting is as follows:
    /// - Render your scene to a RenderTexture at the same aspect ratio as the Screen.  Call this RenderTexture the scene RT.  
    /// - Render the scene RT to the screen using a quad.
    /// - After hooking up the scene RT to the BroadcastController (BC), the BC will automatically setup the needed Camera and generate a RenderTexture which will be appropriate
    ///   for generating the image for submitting for broadcasting.
    ///   
    /// Concerning aspect ratios.  There are 3 aspect ratios (ARs) to consider when setting up your game: the screen, the broadcast and the video player on the site.  If any
    /// of these don't match then there will be black borders added to compensate, potentially at each different stage.  As of the time of this writing, the video player has an AR 
    /// of 1.7777777 which is that of 1080p.  If the Screen AR doesn't match the broadcast AR (configured by the width and height parameters in VideoParams) there will be 
    /// bordering introduced to compensate.  Then, if the broadcast AR doesn't match the video player AR then even more black bordering could be introduced.  Thus, ideally you'll 
    /// setup VideoParams to have a 1.777777 AR (to match the video player) and configure the Screen resolution to as closely match the broadcast as possible.
    /// Another possibility could be to render the scene twice: once to the screen for the screen AR and another for the broadast at an AR of 1.77777.  This would produce the best results but
    /// would most likely be too performance intensive so isn't recommended but does illustrate the point.
    /// </summary>
	public class UnityBroadcastController : BroadcastController
	{
		#region Member Variables
	
	    [SerializeField]
		protected string m_ClientId = "";
		[SerializeField]
		protected string m_ClientSecret = "";
		[SerializeField]
		protected bool m_EnableAudio = true;
		[SerializeField]
		protected Camera m_BroadcastCamera = null;
		[SerializeField]
		protected GameObject m_BroadcastSurface = null;

        protected RenderTexture m_SceneRenderTexture = null;
        protected RenderTexture m_BroadcastRenderTexture = null; 	//!< The dynamically created RenderTexture matching the broadcast dimensions.
        protected float m_LastCaptureTime = 0;						//!< The timestamp of the last frame capture.

		#endregion
		
		#region Properties
		
		protected UnityStream UnityStream
		{
			get { return m_Stream as Twitch.Broadcast.UnityStream; }
		}
		
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
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    // http://stackoverflow.com/questions/2819934/detect-windows-7-in-net

                    OperatingSystem os = System.Environment.OSVersion;

                    if (os != null && os.Platform != PlatformID.Win32Windows)
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
                else if (Application.platform == RuntimePlatform.OSXEditor ||
                         Application.platform == RuntimePlatform.OSXPlayer)
                {
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        public override bool IsPlatformSupported
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    // http://stackoverflow.com/questions/2819934/detect-windows-7-in-net

                    OperatingSystem os = System.Environment.OSVersion;

                    if (os != null && os.Platform != PlatformID.Win32Windows)
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
                else if (Application.platform == RuntimePlatform.OSXEditor ||
                         Application.platform == RuntimePlatform.OSXPlayer)
                {
                    // only support 32-bit Mac
                    return IntPtr.Size == 4;
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    // TODO: when Unity fixes stuff
                    return false;
                }
                else if (Application.platform == RuntimePlatform.Android)
                {
                    // TODO: not supported yet
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }
		
        /// <summary>
        /// The RenderTexture which contains the scene to broadcast.
        /// </summary>
	    public RenderTexture SceneRenderTexture
		{
			get { return m_SceneRenderTexture; }
			set 
			{
                // no change
                if (value == m_SceneRenderTexture)
                {
                    return;
                }

				m_SceneRenderTexture = value;
								
				if (m_BroadcastSurface != null)
				{
					m_BroadcastSurface.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", value);
				}
				

				if (this.IsBroadcasting)
				{
					HandleSceneRenderTextureChange();
				}
			}
		}
	
		#endregion
		
		#region Callbacks
		
#if UNITY_IPHONE && false
	
	    protected void UnityIosRequestAuthTokenCallback(string message)
	    {
			LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
			Dictionary<string, string> dict = new Dictionary<string, string>();
	        reader.Read();
	        JsonReadFlatObjectIntoDictionary(reader, dict);
	
	        ErrorCode status = (ErrorCode)Enum.Parse(typeof(ErrorCode), dict["Status"]);
	
	        AuthToken token = null;
	        if (Error.Succeeded(status))
	        {
	            token = new AuthToken();
	            token.Data = dict["Token"];
	        }
			
	        RequestAuthTokenDoneCallback(status, token);
	    }
		
	    protected void UnityIosLoginCallback(string message)
	    {
			LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
			Dictionary<string, string> dict = new Dictionary<string, string>();
	        reader.Read();
	        JsonReadFlatObjectIntoDictionary(reader, dict);
			
	        ErrorCode status = (ErrorCode)Enum.Parse(typeof(ErrorCode), dict["Status"]);
	
	        ChannelInfo info = null;
	        if (Error.Succeeded(status))
	        {
	            info = new ChannelInfo();
	            info.Name = dict["Name"];
	            info.DisplayName = dict["DisplayName"];
	            info.ChannelUrl = dict["ChannelUrl"];
	        }
	
	        LoginCallback(status, info);
	    }
		
		#region IngestListCallback
		
		protected class IngestListCallbackData
		{
			public ErrorCode status = ErrorCode.TTV_EC_SUCCESS;
			public List<IngestServer> servers = new List<IngestServer>();
		}
		
	    protected void UnityIosIngestListCallback(string message)
	    {
			LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
	        IngestListCallbackData data = new IngestListCallbackData();
			JsonReadObject(reader, data, this.ReadIngestServerObject);
	
	        IngestList ingestList = new IngestList();
	        ingestList.List = data.servers.ToArray();
	
	        IngestListCallback(data.status, ingestList);
	    }
		
		protected void ReadIngestListObject(LitJson.JsonReader reader, object context)
		{
			IngestListCallbackData data = context as IngestListCallbackData;
			
	        switch (reader.Token)
	        {
	            case LitJson.JsonToken.PropertyName:
	            {
					string key = reader.Value.ToString();
					switch (key)
					{
						case "Status":
						{
							string val = JsonReadStringPropertyRHS(reader);
							data.status = (ErrorCode)Enum.Parse(typeof(ErrorCode), val);
							break;
						}
						case "List":
						{
	                        reader.Read();
	                        JsonReadArray(reader, context, this.BeginIngestServerObject, this.ReadIngestServerObject, null);
							break;
						}
					}
	                break;
	            }
	            default:
	            {
					break;
	            }
	        }
		}	
		
		protected void BeginIngestServerObject(object context)
	    {
			IngestListCallbackData data = context as IngestListCallbackData;
			data.servers.Add( new IngestServer() );
	    }
		
		protected void ReadIngestServerObject(LitJson.JsonReader reader, object context)
		{
			IngestListCallbackData data = context as IngestListCallbackData;
			IngestServer server = data.servers.Last();
			
	        switch (reader.Token)
	        {
	            case LitJson.JsonToken.PropertyName:
	            {
					string key = reader.Value.ToString();
					string val = JsonReadStringPropertyRHS(reader);
					switch (key)
					{
						case "Name":
						{
							server.ServerName = val;
							break;
						}
						case "Url":
						{
							server.ServerUrl = val;
							break;
						}
						case "Default":
						{
							server.DefaultServer = val != "0" && val.ToLower() != "false";
							break;
						}
					}
	                break;
	            }
	            default:
	            {
					break;
	            }
	        }
		}
	
		#endregion
		
	    protected void UnityIosUserInfoCallback(string message)
	    {
			LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
			Dictionary<string, string> dict = new Dictionary<string, string>();
	        reader.Read();
	        JsonReadFlatObjectIntoDictionary(reader, dict);
			
	        ErrorCode status = (ErrorCode)Enum.Parse(typeof(ErrorCode), dict["Status"]);
			
	        UserInfo info = new UserInfo();
			info.Name = dict["Name"];
			info.DisplayName = dict["DisplayName"];
	
	        UserInfoDoneCallback(status, info);
	    }
	
	    protected void UnityIosStreamInfoCallback(string message)
	    {
	 		LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
			Dictionary<string, string> dict = new Dictionary<string, string>();
	        reader.Read();
	        JsonReadFlatObjectIntoDictionary(reader, dict);
			
	        ErrorCode status = (ErrorCode)Enum.Parse(typeof(ErrorCode), dict["Status"]);
			
	        StreamInfo info = new StreamInfo();
	        info.Viewers = int.Parse(dict["Viewers"]);
			info.StreamId = UInt64.Parse(dict["Id"]);
	
	        StreamInfoDoneCallback(status, info);
	    }
	
	    protected void UnityIosArchivingStateCallback(string message)
	    {
	 		LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
			Dictionary<string, string> dict = new Dictionary<string, string>();
	        reader.Read();
	        JsonReadFlatObjectIntoDictionary(reader, dict);
			
	        ErrorCode status = (ErrorCode)Enum.Parse(typeof(ErrorCode), dict["Status"]);
			
	        ArchivingState state = new ArchivingState();
			state.CureUrl = dict["CureUrl"];
			state.RecordingEnabled = dict["RecordingEnabled"] != "0";
	
	        ArchivingStateDoneCallback(status, state);
	    }
		
		#region GameInfoListCallback
		
		protected class GameInfoListCallbackData
		{
			public ErrorCode status = ErrorCode.TTV_EC_SUCCESS;
			public List<GameInfo> games = new List<GameInfo>();
		}
		
	    protected void UnityIosGameInfoListCallback(string message)
	    {
			LitJson.JsonReader reader = new LitJson.JsonReader(message);
			
	        GameInfoListCallbackData data = new GameInfoListCallbackData();
			JsonReadObject(reader, data, this.ReadGameInfoListObject);
	
	        GameInfoList gameList = new GameInfoList();
	        gameList.List = data.games.ToArray();
	
	        GameNameListCallback(data.status, gameList);
	    }
		
		
		protected void ReadGameInfoListObject(LitJson.JsonReader reader, object context)
		{
			GameInfoListCallbackData data = context as GameInfoListCallbackData;
			
	        switch (reader.Token)
	        {
	            case LitJson.JsonToken.PropertyName:
	            {
					string key = reader.Value.ToString();
					switch (key)
					{
						case "Status":
						{
							string val = JsonReadStringPropertyRHS(reader);
							data.status = (ErrorCode)Enum.Parse(typeof(ErrorCode), val);
							break;
						}
						case "List":
						{
	                        reader.Read();
	                        JsonReadArray(reader, context, this.BeginGameInfoObject, this.ReadGameInfoObject, null);
							break;
						}
					}
	                break;
	            }
	            default:
	            {
					break;
	            }
	        }
		}	
		
		protected void BeginGameInfoObject(object context)
	    {
			GameInfoListCallbackData data = context as GameInfoListCallbackData;
			data.games.Add( new GameInfo() );
	    }
		
		protected void ReadGameInfoObject(LitJson.JsonReader reader, object context)
		{
			GameInfoListCallbackData data = context as GameInfoListCallbackData;
			GameInfo info = data.games.Last();
			
	        switch (reader.Token)
	        {
	            case LitJson.JsonToken.PropertyName:
	            {
					string key = reader.Value.ToString();
					string val = JsonReadStringPropertyRHS(reader);
					switch (key)
					{
						case "Name":
						{
							info.Name = val;
							break;
						}
						case "Id":
						{
							info.Id = int.Parse(val);
							break;
						}
						case "Popularity":
						{
							info.Popularity = int.Parse(val);
							break;
						}
					}
	                break;
	            }
	            default:
	            {
					break;
	            }
	        }
		}
	
		#endregion
		
#endif	
		
		#endregion

        #region Library Loading

        [System.Runtime.InteropServices.DllImport("kernel32", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, EntryPoint = "LoadLibraryW")]
        internal static extern UIntPtr LoadWindowsLibrary([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string path);

        internal static bool LoadTwitchLibrary(string path)
        {
            if (LoadWindowsLibrary(path) == UIntPtr.Zero)
            {
                Debug.LogError("Failed to load Twitch native library: " + path);
                return false;
            }
            else
            {
                Debug.Log("Loaded Twitch native library: " + path);
                return true;
            }
        }

        internal static void LoadTwitchLibrariesFromBasePath(string path)
        {
            // now manually load all the dependent libraries in reverse order
            switch (IntPtr.Size)
            {
                // 32-bit libraries
                case 4:
                {
                    LoadTwitchLibrary(path + "\\avutil-ttv-51.dll");
                    LoadTwitchLibrary(path + "\\swresample-ttv-0.dll");
                    LoadTwitchLibrary(path + "\\libmp3lame-ttv.dll");
                    LoadTwitchLibrary(path + "\\libmfxsw32.dll");
                    LoadTwitchLibrary(path + "\\twitchsdk.dll");
                    break;
                }
                // 64-bit libraries
                case 8:
                {
                    LoadTwitchLibrary(path + "\\avutil-ttv-51.dll");
                    LoadTwitchLibrary(path + "\\swresample-ttv-0.dll");
                    LoadTwitchLibrary(path + "\\libmp3lame-ttv.dll");
                    LoadTwitchLibrary(path + "\\libmfxsw64.dll");
                    LoadTwitchLibrary(path + "\\twitchsdk.dll");
                    break;
                }
                default:
                {
                    Debug.LogError("Unable to load Twitch libraries, unknown Windows platform with word size: " + IntPtr.Size);
                    break;
                }
            }        
        }

        internal static void LoadTwitchLibraries()
        {
#if !UNITY_WEBPLAYER
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string path = null;

                // look in the same directory as twitchsdkwrapper.dll
                path = new System.IO.FileInfo(System.Reflection.Assembly.GetAssembly(typeof(Twitch.Broadcast.Stream)).Location).Directory.FullName;
                if (System.IO.File.Exists(path + System.IO.Path.DirectorySeparatorChar + "twitchsdk.dll"))
                {
                    LoadTwitchLibrariesFromBasePath(path);
                    return;
                }

                // look in the ../Managed directory since this might be a built game
                path = new System.IO.FileInfo(System.Reflection.Assembly.GetAssembly(typeof(Twitch.Broadcast.Stream)).Location).Directory.Parent.FullName;
                path = path + System.IO.Path.DirectorySeparatorChar + "Plugins";
                if (System.IO.File.Exists(path + System.IO.Path.DirectorySeparatorChar + "twitchsdk.dll"))
                {
                    LoadTwitchLibrariesFromBasePath(path);
                    return;
                }

                // look in the platform-specific directory
                path = new System.IO.FileInfo(System.Reflection.Assembly.GetAssembly(typeof(Twitch.Broadcast.Stream)).Location).Directory.FullName;
                path = path + System.IO.Path.DirectorySeparatorChar;
                switch (IntPtr.Size)
                {
                    // 32-bit libraries
                    case 4:
                    {
                        path = path + "x86";
                        break;
                    }
                    // 64-bit libraries
                    case 8:
                    {
                        path = path + "x86_64";
                        break;
                    }
                }

                if (System.IO.File.Exists(path + System.IO.Path.DirectorySeparatorChar + "twitchsdk.dll"))
                {
                    LoadTwitchLibrariesFromBasePath(path);
                    return;
                }

                Debug.LogError("Unable to find the directory containing Twitch native libraries");
            }
            else
            {
                // not sure if anything needs to be done on other platforms
            }
#endif
        }

        #endregion

		#region Unity Overrides
		
        protected void Awake()
        {
            LoadTwitchLibraries();

            m_Core = new Core(new StandardCoreAPI());

            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                m_Stream = new UnityStream(new DesktopUnityStreamAPI());
            }
            else if (Application.platform == RuntimePlatform.OSXEditor ||
                     Application.platform == RuntimePlatform.OSXPlayer)
            {
                m_Stream = new UnityStream(new DesktopUnityStreamAPI());
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // TODO: when Unity fixes stuff - need to reinclude InternalUnityStreamAPI in twitchsdkwrapper.dll
                //m_Stream = new UnityStream( new InternalUnityStreamAPI() );
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                // TODO: not supported yet
            }
            else
            {
                // TODO: not supported yet
            }
        }

        protected void OnDestroy()
        {
            Shutdown();

            // force a low-level shutdown
            if (m_Stream != null)
            {
                //m_Stream.Shutdown();
            }
        }
		
        public override void Update()
		{
			base.Update();
			
            if (m_BroadcastState != BroadcastState.Broadcasting)
			{
	            return;
	        }
	
			float curTime = Time.realtimeSinceStartup;
			
			// If you send frames too quickly to the SDK (based on the broadcast FPS you configured) it will not be able 
			// to make use of them all.  In that case, it will simply release buffers without using them which means the
			// game wasted time doing the capture.  To mitigate this, the app should pace the captures to the broadcast FPS.
			float captureDelta = curTime - m_LastCaptureTime;
			bool isTimeForNextCapture = captureDelta >= (1.0f / (float)m_VideoParams.TargetFps);
			
			if (!isTimeForNextCapture)
			{
	            return;
	        }
	
			IntPtr p = m_BroadcastRenderTexture.GetNativeTexturePtr();
			if (p != IntPtr.Zero)
			{
                ErrorCode ret = this.UnityStream.SubmitTexturePointer(p);

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
                        
                        // errors are not recoverable
                        StopBroadcasting();
                    }
					
					FireFrameSubmissionIssue(ret);
                }
	        }
	
	        m_LastCaptureTime = curTime;
	    }

		#endregion
	
		public override PixelFormat DeterminePixelFormat()
		{
			PixelFormat format = base.DeterminePixelFormat();
	
	        ErrorCode ret = this.UnityStream.GetCapturePixelFormat(ref format);
	        if (Error.Failed(ret))
	        {
	            string err = Error.GetString(ret);
	            ReportError(string.Format("Error in GetCapturePixelFormat: {0}", err));
	        }
	
	        return format;
		}
		
	    protected override bool AllocateBuffers()
	    {
			// Attempt to create the broadcast render texture required
            m_BroadcastRenderTexture = new RenderTexture((int)m_VideoParams.OutputWidth, (int)m_VideoParams.OutputHeight, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            m_BroadcastRenderTexture.useMipMap = false;

			m_BroadcastCamera.targetTexture = m_BroadcastRenderTexture;
			m_BroadcastCamera.gameObject.SetActive(true);
			
			HandleSceneRenderTextureChange();
			
			return true;
	    }
		
		protected void HandleSceneRenderTextureChange()
		{
			uint width = (uint)m_BroadcastRenderTexture.width;
			uint height = (uint)m_BroadcastRenderTexture.height;

            float screenAspectRatio = (float)m_SceneRenderTexture.width / (float)m_SceneRenderTexture.height;
            float broadcastApectRatio = (float)width / (float)height;
	
			m_LastCaptureTime = Time.realtimeSinceStartup;
			
            // scale the screen texture horizontally to fit
			if (broadcastApectRatio >= screenAspectRatio)
			{
                m_BroadcastSurface.transform.localScale = new Vector3(1, 1, 1);
            }
            // scale the screen texture vertically to fit
			else
			{
				float relative = broadcastApectRatio / screenAspectRatio;
				m_BroadcastSurface.transform.localScale = new Vector3(relative, relative, 1);
			}
			
			// Create the mesh to render the screen texture
	        Vector3 shift = new Vector3(-screenAspectRatio * 0.5f, -0.5f, 0);
	        Vector3 flip = new Vector3(1, 1, 1);
			Mesh mesh = GenerateSurfaceMesh(new Vector2(screenAspectRatio, 1.0f), shift, flip);
	    	m_BroadcastSurface.GetComponent<MeshFilter>().mesh = mesh;
		}
		
		protected override void CleanupBuffers()
		{
			m_BroadcastCamera.gameObject.SetActive(false);
			m_BroadcastCamera.targetTexture = null;
			
            // destroy the broadcast texture
			if (m_BroadcastRenderTexture != null)
			{
				GameObject.Destroy(m_BroadcastRenderTexture);
				m_BroadcastRenderTexture = null;
			}
		}
		
	    protected Mesh GenerateSurfaceMesh(Vector2 size, Vector3 shift, Vector3 flip)
	    {
	        Vector3[] vertices = new Vector3[]
	        {
	            new Vector3(0,0,0),
	            new Vector3(0,size.y,0),
	            new Vector3(size.x,size.y,0),
	            new Vector3(size.x,0,0),
	        };
	
	        for (int i = 0; i < vertices.Length; ++i)
	        {
	            vertices[i].x = flip.x * (vertices[i].x + shift.x);
	            vertices[i].y = flip.y * (vertices[i].y + shift.y);
	        }
	
	        Mesh mesh = new Mesh();
	        mesh.vertices = vertices;
	
	        mesh.uv = new Vector2[]
	        {
	            new Vector2(0,1),
	            new Vector2(0,0),
	            new Vector2(1,0),
	            new Vector2(1,1),
	        };
	
	        mesh.triangles = new int[]
	        {
	            0,1,2,
	            0,2,3,
	        };
			
			return mesh;
	    }
		
		#region Error Handling
		
        protected override bool CheckError(Twitch.ErrorCode err)
	    {
	        if (Error.Failed(err))
	        {
	            Debug.LogError(err.ToString());
                return false;
	        }

            return true;
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
