//////////////////////////////////////////////////////////////////////////////
// This module contains code which generates and animates a simple mesh.
//////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "../wavemesh.h"
#include <d3d9.h>
#include <d3dx9math.h>

#define SAFE_RELEASE(x) if (x) { x->Release(); x = nullptr; } 

extern IDirect3DDevice9* gGraphicsDevice;


struct WaveMeshVertex
{
	D3DVECTOR v; 
	FLOAT tx, ty; 
};

#define WAVE_MESH_VERTEX_FVF (D3DFVF_XYZ | D3DFVF_TEX2)

IDirect3DTexture9* gTwitchTexture = nullptr;
IDirect3DVertexBuffer9* gVertexBuffer = nullptr;
IDirect3DIndexBuffer9* gIndexBuffer = nullptr;

FLOAT gWorldSize = 0;
unsigned int gVertexDim = 0;
unsigned int gNumVertices = 0;
unsigned int gNumIndices = 0;
float gWaveStartTime = 0;


/**
 * Computes the index of the vertex in the vertex buffer for the given vertex coordinates.
 */
inline int GetWaveMeshIndex(unsigned int x, unsigned int y)
{
	return gVertexDim*y + x;
}


/**
 * Creates a wave mesh of the desired world size and number of vertices wide.
 */
void CreateWaveMesh(float scale, unsigned int vertexDim)
{
	DestroyWaveMesh();

	if (scale < 1)
	{
		scale = 1;
	}

	if (vertexDim < 2)
	{
		vertexDim = 2;
	}

	gWorldSize = scale;
	gVertexDim = vertexDim;
	gNumVertices = gVertexDim*gVertexDim;

	// setup the vertex buffer
	HRESULT hr = gGraphicsDevice->CreateVertexBuffer(gNumVertices*sizeof(WaveMeshVertex), 0, WAVE_MESH_VERTEX_FVF, D3DPOOL_MANAGED, &gVertexBuffer, NULL);
	if (FAILED(hr))
	{
		ReportError("Failed to create vertex buffer");
		return;
	}

	WaveMeshVertex* pMeshVertices = nullptr;
	hr = gVertexBuffer->Lock(0, 0, reinterpret_cast<void**>(&pMeshVertices), 0);
	{
		if (FAILED(hr))
		{
			ReportError("Failed to lock vertex buffer");
			return;
		}

		// shift to center on 0,0,0
		FLOAT shift = -(FLOAT)scale / 2;

		int index = 0;
		for (unsigned int y=0; y<vertexDim; ++y)
		{
			for (unsigned int x=0; x<vertexDim; ++x)
			{
				pMeshVertices[index].v.x = shift + scale * (FLOAT)x / (FLOAT)(vertexDim-1);
				pMeshVertices[index].v.y = shift + scale * (FLOAT)y / (FLOAT)(vertexDim-1);
				pMeshVertices[index].v.z = 100;
				pMeshVertices[index].tx = x / FLOAT(vertexDim-1);
				pMeshVertices[index].ty = 1.0f - y / FLOAT(vertexDim-1);

				index++;
			}
		}
	}
	hr = gVertexBuffer->Unlock();

	// setup the index buffer
	gNumIndices = 3*2*(gVertexDim-1)*(vertexDim-1);
	hr = gGraphicsDevice->CreateIndexBuffer(gNumIndices*sizeof(unsigned int), D3DUSAGE_DYNAMIC, D3DFMT_INDEX32, D3DPOOL_DEFAULT, &gIndexBuffer, NULL);
	if (FAILED(hr))
	{
		ReportError("Failed to create index buffer");
		return;
	}

	unsigned int* pMeshIndices = nullptr;
	hr = gIndexBuffer->Lock(0, 0, reinterpret_cast<void**>(&pMeshIndices), 0);
	{
		if (FAILED(hr))
		{
			ReportError("Failed to lock index buffer");
			return;
		}

		int index = 0;
		for (unsigned int y=0; y<vertexDim-1; ++y)
		{
			for (unsigned int x=0; x<vertexDim-1; ++x)
			{
				pMeshIndices[index++] = GetWaveMeshIndex(x, y);
				pMeshIndices[index++] = GetWaveMeshIndex(x, y+1);
				pMeshIndices[index++] = GetWaveMeshIndex(x+1, y);

				pMeshIndices[index++] = GetWaveMeshIndex(x, y+1);
				pMeshIndices[index++] = GetWaveMeshIndex(x+1, y+1);
				pMeshIndices[index++] = GetWaveMeshIndex(x+1, y);
			}
		}
	}
	gIndexBuffer->Unlock();

	// Load the Twitch logo
	hr = D3DXCreateTextureFromFileA(gGraphicsDevice, "twitch.png", &gTwitchTexture);
	if (FAILED(hr))
	{
		hr = D3DXCreateTextureFromFileA(gGraphicsDevice, "..\\..\\twitch.png", &gTwitchTexture);
		if (FAILED(hr))
		{
			ReportError("Failed to load texture");
			return;
		}
	}

	gWaveStartTime = GetSystemTimeMs();
}


/**
 * Frees resources used by the mesh.
 */
void DestroyWaveMesh()
{
	SAFE_RELEASE(gVertexBuffer);
	SAFE_RELEASE(gIndexBuffer);
	SAFE_RELEASE(gTwitchTexture);

	gNumVertices = 0;
	gNumIndices = 0;
	gWorldSize = 0;
	gVertexDim = 0;
}


/**
 * Animate the mesh.
 */
void UpdateWaveMesh()
{
	if (gVertexBuffer == nullptr)
	{
		return;
	}

	// This is a horrible way to animate the mesh and it should be done in a simple vertex shader.  However, it's a simple
	// sample and this keeps things simpler.

	float totalTime = GetSystemTimeMs() - gWaveStartTime;

	const float amp = 3.0f;
	const float freq = 2;
	float shift = freq * 6.28f * totalTime / 1000.0f;

	WaveMeshVertex* pMeshVertices = nullptr;
	HRESULT hr = gVertexBuffer->Lock(0, 0, reinterpret_cast<void**>(&pMeshVertices), 0);
	{
		if (FAILED(hr))
		{
			ReportError("Failed to lock vertex buffer");
			return;
		}

		FLOAT offset = -(FLOAT)gWorldSize / 2;

		int index = 0;
		for (unsigned int y=0; y<gVertexDim; ++y)
		{
			for (unsigned int x=0; x<gVertexDim; ++x)
			{
				float c = cos(shift + 6.28f*(x+y)/(float)(gVertexDim-1));

				pMeshVertices[index].v.z = 100 + amp * c;
				
				index++;
			}
		}
	}
	hr = gVertexBuffer->Unlock();	
}


/**
 * Draws the mesh.
 */
void DrawWaveMesh()
{
	gGraphicsDevice->SetRenderState(D3DRS_ZWRITEENABLE, TRUE);
	gGraphicsDevice->SetRenderState(D3DRS_ZENABLE, TRUE);
	gGraphicsDevice->SetRenderState(D3DRS_ALPHATESTENABLE, TRUE);
	gGraphicsDevice->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
	gGraphicsDevice->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
	gGraphicsDevice->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

	gGraphicsDevice->SetFVF(WAVE_MESH_VERTEX_FVF);
	gGraphicsDevice->SetStreamSource(0, gVertexBuffer, 0, sizeof(WaveMeshVertex));
	gGraphicsDevice->SetIndices(gIndexBuffer);
	gGraphicsDevice->SetTexture(0, gTwitchTexture);
	gGraphicsDevice->DrawIndexedPrimitive(D3DPT_TRIANGLELIST, 0, 0, gNumVertices, 0, 2*(gVertexDim-1)*(gVertexDim-1));

	gGraphicsDevice->SetIndices(NULL);
	gGraphicsDevice->SetStreamSource(0, nullptr, 0, 0);
	gGraphicsDevice->SetTexture(0, nullptr);
}
