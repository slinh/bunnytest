using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This component is responsible for creating and maintaining the scene RenderTexture that the game will render the scene to. It handles resize events from 
/// the Unity Screen instance and automatically regenerates the screen RenderTexture. It also updates the configured scene cameras and notifies them to update 
/// their projections. When the RenderTexture is automatically updated it is connected to the configured BroadcastController.
/// </summary>
public class RenderTextureResizer : MonoBehaviour
{
	[SerializeField]
	protected Camera[] m_SceneCameras = new Camera[0];
	[SerializeField]
	protected MeshRenderer m_ScreenSurface = null;
	[SerializeField]
    protected Twitch.Broadcast.UnityBroadcastController m_BroadcastController = null;
	
	protected RenderTexture m_RenderTexture = null;
	protected Mesh m_ScreenMesh = null;
    protected List<Camera> m_CurrentSceneCameras = new List<Camera>();


    #region Singleton

    protected static RenderTextureResizer s_Instance = null;

    public static RenderTextureResizer Instance
    {
        get { return s_Instance; }
    }

    #endregion


    #region Unity Overrides

    protected void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
        }

        if (m_SceneCameras != null)
        {
            for (int i = 0; i < m_SceneCameras.Length; ++i)
            {
                if (m_SceneCameras[i] != null)
                {
                    m_CurrentSceneCameras.Add(m_SceneCameras[i]);
                }
            }
        }
    }

    protected void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }

    protected void OnLevelWasLoaded(int levelId)
    {
        // whenever a level was loaded, we try to clean up all the cameras that are now null due to the load.
        m_CurrentSceneCameras.RemoveAll(cam => cam == null);
    }
    
    protected void Update()
	{
		//DebugOverlay.Instance.AddViewportText(string.Format("{0}x{1}", Screen.width, Screen.height), 0);
		
		// see if the screen texture size needs to be changed
		if (m_RenderTexture != null &&
			m_RenderTexture.width == Screen.width &&
			m_RenderTexture.height == Screen.height)
		{
			return;
		}
		
		// create a new screen render texture
		RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        rt.useMipMap = false;

		// configure the rendertexture cameras to render to it
        for (int i = 0; i < m_CurrentSceneCameras.Count; ++i)
		{
            if (m_CurrentSceneCameras[i] != null)
			{
                m_CurrentSceneCameras[i].targetTexture = rt;
                m_CurrentSceneCameras[i].ResetAspect();
                m_CurrentSceneCameras[i].ResetProjectionMatrix();
			}
		}
		
		if (m_BroadcastController != null)
		{
			m_BroadcastController.SceneRenderTexture = rt;
		}
		
		// destroy the old one
		if (m_RenderTexture != null)
		{
			GameObject.Destroy(m_RenderTexture);
		}
		
		m_RenderTexture = rt;
		
		// update the screen mesh to use the new render texture
		if (m_ScreenSurface != null)
		{
			Vector2 size = new Vector2(Screen.width, Screen.height);
			size /= size.y;
			
			Vector3 shift = new Vector3(-size.x, -size.y, 0) * 0.5f;
			
			Mesh mesh = GenerateMesh(size, shift, new Vector3(1,1,1));
			m_ScreenSurface.GetComponent<MeshFilter>().mesh = mesh;
			m_ScreenSurface.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", m_RenderTexture);
			
			if (m_ScreenMesh != null)
			{
				GameObject.Destroy(m_ScreenMesh);
			}
			
			m_ScreenMesh = mesh;
		}
	}

    #endregion

    #region Camera Management

    protected void UpdateCamera(Camera camera, RenderTexture rt)
    {
        camera.targetTexture = rt;
        camera.ResetAspect();
        camera.ResetProjectionMatrix();
    }

    public void ClearCameras()
    {
        while (m_CurrentSceneCameras.Count > 0)
        {
            RemoveCamera(m_CurrentSceneCameras[0]);
        }
    }

    public void AddCamera(Camera camera)
    {
        if (camera != null)
        {
            UpdateCamera(camera, m_RenderTexture);
            m_CurrentSceneCameras.Add(camera);
        }
    }

    public void RemoveCamera(Camera camera)
    {
        if (camera != null)
        {
            int index = m_CurrentSceneCameras.IndexOf(camera);

            if (index >= 0)
            {
                UpdateCamera(camera, null);

                m_CurrentSceneCameras.RemoveAt(index);
            }
        }
    }

    #endregion

    protected Mesh GenerateMesh(Vector2 size, Vector3 shift, Vector3 flip)
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
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),
        };

        mesh.triangles = new int[]
        {
            0,1,2,
            0,2,3,
        };
		
		return mesh;
    }		
}
