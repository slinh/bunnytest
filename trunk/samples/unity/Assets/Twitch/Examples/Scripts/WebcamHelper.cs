using UnityEngine;
using System.Collections;

public class WebcamHelper : MonoBehaviour
{
	[SerializeField]
	protected int m_DesiredWidth = 640;
	[SerializeField]
	protected int m_DesiredHeight = 480;
	[SerializeField]
	protected int m_DesiredFrameRate = 30;
	[SerializeField]
	protected Camera m_Camera = null;
	
	protected WebCamTexture m_Texture = null;
	protected WebCamDevice m_Device;
	protected bool m_Initialized = false;
	protected Vector3 m_MeshSize;
	
	
	protected void Start()
	{
		WebCamDevice[] devices = WebCamTexture.devices;
		
		if (devices.Length == 0)
		{
			return;
		}
		
		m_Device = devices[0];
		
		m_Texture = new WebCamTexture(m_Device.name, m_DesiredWidth, m_DesiredHeight, m_DesiredFrameRate);
		this.gameObject.renderer.material.SetTexture("_MainTex", m_Texture);
				
		m_Texture.Play();
	}
	
	protected void Update()
	{
		if (!m_Initialized)
		{
			if (m_Texture.width != 16)
			{
				// normalize the size to fit in the orthographic camera
				m_MeshSize = new Vector3(m_Texture.width * m_Texture.texelSize.x, m_Texture.height * m_Texture.texelSize.y, 0);
				m_MeshSize /= m_MeshSize.y;
				m_MeshSize /= 2.0f;
				m_MeshSize *= 0.6666f;
				
				Mesh mesh = GenerateMesh(m_MeshSize, new Vector3(0, 0, 0), new Vector3(1,1,1));
				this.gameObject.GetComponent<MeshFilter>().mesh = mesh;
				
				m_Initialized = true;
			}
		}
		
		// position the webcam view in the bottom left corner
		Vector3 position = this.gameObject.transform.position;
		float aspectRatio = (float)Screen.width / (float)Screen.height;
		position.x = -aspectRatio/2 + 0.05f;
		this.gameObject.transform.position = position;
	}
	
	protected void OnDestroy()
	{
		if (m_Texture != null)
		{
			m_Texture.Stop();
			m_Texture = null;
		}
	}
	
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
