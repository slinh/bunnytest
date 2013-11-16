using UnityEngine;
using System.Collections;

/// <summary>
/// A simple script which configures the GameObject to be persistent and not be unloaded between scenes.
/// </summary>
public class KeepAlive : MonoBehaviour
{
	protected void Awake() 
	{
        GameObject.DontDestroyOnLoad(this.gameObject);
	}
}
