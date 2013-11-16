using UnityEngine;
using System.Collections;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField]
    protected AudioSource m_AudioSource = null;


    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (m_AudioSource != null)
            {
                m_AudioSource.Play();
            }
        }
    }
}
