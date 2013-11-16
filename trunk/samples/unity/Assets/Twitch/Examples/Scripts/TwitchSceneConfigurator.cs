using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hooks up the configured cameras to the RenderTextureResizer instance during Start() and unhooks them during OnDestroy().
/// </summary>
public class TwitchSceneConfigurator : MonoBehaviour
{
	[SerializeField]
	protected Camera[] m_SceneCameras = new Camera[0];

    #region Unity Overrides

    protected void Start()
    {
        RenderTextureResizer rtr = RenderTextureResizer.Instance;
        if (rtr == null)
        {
            Debug.LogError("Unable to find RenderTextureResizer, could not hook up cameras");
            return;
        }

        if (m_SceneCameras != null)
        {
            for (int i = 0; i < m_SceneCameras.Length; ++i)
            {
                if (m_SceneCameras[i] != null)
                {
                    rtr.AddCamera(m_SceneCameras[i]);
                }
            }
        }
    }

    protected void OnDestroy()
    {
        RenderTextureResizer rtr = RenderTextureResizer.Instance;
        if (rtr == null)
        {
            return;
        }

        if (m_SceneCameras != null)
        {
            for (int i = 0; i < m_SceneCameras.Length; ++i)
            {
                if (m_SceneCameras[i] != null)
                {
                    rtr.RemoveCamera(m_SceneCameras[i]);
                }
            }
        }
    }

    #endregion
}
