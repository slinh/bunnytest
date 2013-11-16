using UnityEngine;
using System.Collections;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField]
    protected string m_Scene = "";

	protected void Awake() 
	{
        // first load the persistent Twitch stuff
        Application.LoadLevelAdditive("Twitch");

        // now load the initial scene
        if (string.IsNullOrEmpty(m_Scene))
        {
            Application.LoadLevel(1);
        }
        else
        {
            Application.LoadLevel(m_Scene);
        }
	}
}
