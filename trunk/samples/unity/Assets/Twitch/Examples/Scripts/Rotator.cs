using UnityEngine;
using System.Collections;

public class Rotator : MonoBehaviour
{
	void Update() 
	{
		Vector3 euler = this.gameObject.transform.localEulerAngles;
		euler.y += Time.deltaTime * 45.0f;
		this.gameObject.transform.localEulerAngles = euler;
	}
}
