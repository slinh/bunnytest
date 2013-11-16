#include "stdafx.h"
#include "capturefast_d3d.h"
#include "../wavemesh.h"
#include "../streaming.h"

#include <d3d9.h>
#include <d3dx9math.h>

#define SAFE_RELEASE(x) if (x) { x->Release(); x = nullptr; } 
#define NUM_CAPTURE_SURFACES 4

#define SCREEN_QUAD_VERTEX_FVF (D3DFVF_XYZ | D3DFVF_TEX2)

struct ScreenQuadVertex
{
	D3DVECTOR v; 
	FLOAT tx, ty; 
};


extern IDirect3DDevice9* gGraphicsDevice;
extern D3DXMATRIX gViewMatrix;
extern D3DXMATRIX gProjectionMatrix;
extern unsigned int gBroadcastWidth;
extern unsigned int gBroadcastHeight;


IDirect3DVertexBuffer9* gScreenQuadVertexBuffer = nullptr;	// The vertex buffer containing the screen quad.
IDirect3DTexture9* gMainRenderTexture = nullptr;			// The texture that the app renders to.
IDirect3DSurface9* gMainRenderTargetSurface = nullptr;		// The surface that the app renders to.
D3DXMATRIX gOrthoViewMatrix;								// The screen tranlsation matrix.
D3DXMATRIX gScreenProjectionMatrix;							// The orthographic screen projection matrix.

static IDirect3DTexture9* gCaptureTexture = nullptr;												// The texture which serves as the destination of the resized output.
static IDirect3DSurface9* gCaptureSurface = nullptr;
IDirect3DQuery9* gCaptureQuery[NUM_CAPTURE_SURFACES] = { nullptr, nullptr, nullptr, nullptr };		// The pool of query requests.
IDirect3DSurface9* gResizeSurface[NUM_CAPTURE_SURFACES] = { nullptr, nullptr, nullptr, nullptr };	// The pool of surfaces to capture to.

int gCaptureWidth = 0;			// The desired width of the output buffer.
int gCaptureHeight = 0;			// The desired height of the output buffer.
unsigned int gCapturePut = 0;	// The current request to render the resized texture to the destination render target.
unsigned int gCaptureGet = 0;	// The current request for the pixel data.


/**
 * Initializes the render method.
 */
void InitRendering_Fast(unsigned int windowWidth, unsigned int windowHeight, unsigned int broadcastWidth, unsigned int broadcastHeight)
{
	DeinitRendering_Fast();

	// Create the offscreen texture for rendering to
	if ( FAILED(gGraphicsDevice->CreateTexture(windowWidth, windowHeight, 1, D3DUSAGE_RENDERTARGET, D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &gMainRenderTexture, nullptr)) )
	{
		ReportError("Error creating render texture");
		return;
	}

	// Create the surface to render to
	if ( FAILED(gMainRenderTexture->GetSurfaceLevel(0, &gMainRenderTargetSurface)) )
	{
		ReportError("Error creating render surface");
		return;
	}

	// Setup the screen quad
	if ( FAILED(gGraphicsDevice->CreateVertexBuffer(4*sizeof(ScreenQuadVertex), 0, SCREEN_QUAD_VERTEX_FVF, D3DPOOL_MANAGED, &gScreenQuadVertexBuffer, nullptr)) )
	{
		ReportError("Error creating vertex buffer");
		return;
	}

	ScreenQuadVertex* pScreenQuad = nullptr;
	if ( FAILED(gScreenQuadVertexBuffer->Lock(0, 0, reinterpret_cast<void**>(&pScreenQuad), 0)) )
	{
		ReportError("Vertex buffer lock failed");
		return;
	}

	pScreenQuad[0].v.x = 0;
	pScreenQuad[0].v.y = 0;
	pScreenQuad[0].v.z = 1;
	pScreenQuad[0].tx = 0;
	pScreenQuad[0].ty = 1;

	pScreenQuad[1].v.x = 0;
	pScreenQuad[1].v.y = (FLOAT)windowHeight;
	pScreenQuad[1].v.z = 1;
	pScreenQuad[1].tx = 0;
	pScreenQuad[1].ty = 0;

	pScreenQuad[2].v.x = (FLOAT)windowWidth;
	pScreenQuad[2].v.y = 0;
	pScreenQuad[2].v.z = 1;
	pScreenQuad[2].tx = 1;
	pScreenQuad[2].ty = 1;

	pScreenQuad[3].v.x = (FLOAT)windowWidth;
	pScreenQuad[3].v.y = (FLOAT)windowHeight;
	pScreenQuad[3].v.z = 1;
	pScreenQuad[3].tx = 1;
	pScreenQuad[3].ty = 0;

	gScreenQuadVertexBuffer->Unlock();

	// Setup the ortho projection for the render to screen
	D3DXMatrixOrthoOffCenterLH(&gScreenProjectionMatrix, 0, (FLOAT)windowWidth, 0, (FLOAT)windowHeight, 1, 100);

	// Setup the screen translation
	D3DXMatrixIdentity(&gOrthoViewMatrix);
}


/**
 * Cleans up the render method.
 */
void DeinitRendering_Fast()
{
	SAFE_RELEASE(gScreenQuadVertexBuffer);
	SAFE_RELEASE(gMainRenderTargetSurface);
	SAFE_RELEASE(gMainRenderTexture);

	SAFE_RELEASE(gCaptureTexture);
	SAFE_RELEASE(gCaptureSurface);

	for (int i = 0; i < NUM_CAPTURE_SURFACES; ++i)
	{
		SAFE_RELEASE(gCaptureQuery[i]);
		SAFE_RELEASE(gResizeSurface[i]);
	}
}


/**
 * Renders the scene to a texture.
 */
void RenderOffscreen()
{
	// Cache previous render target
	IDirect3DSurface9* pPreviousSurface = nullptr;
	gGraphicsDevice->GetRenderTarget(0, &pPreviousSurface);

	// Set the offscreen render target
	gGraphicsDevice->SetRenderTarget(0, gMainRenderTargetSurface);
	{
		// Enable depth buffering
		gGraphicsDevice->SetRenderState(D3DRS_ZENABLE, TRUE);

		// Clear the buffers
		gGraphicsDevice->Clear(0, nullptr, D3DCLEAR_TARGET, D3DCOLOR_ARGB(255, 0, 0, 255), 1.0f, 0);

		// View transformation
		gGraphicsDevice->SetTransform(D3DTS_VIEW, &gViewMatrix);

		// Orthographic projection
		gGraphicsDevice->SetTransform(D3DTS_PROJECTION, &gProjectionMatrix);

		// Render the scene
		gGraphicsDevice->BeginScene();
		{
			// Render the wave mesh
			DrawWaveMesh();
		}
		gGraphicsDevice->EndScene();
	}

	// Restore the previous render target
	gGraphicsDevice->SetRenderTarget(0, pPreviousSurface);
}


/**
 * Renders the scene texture to the screen.
 */
void RenderToScreen()
{
	// Disable depth buffering
	gGraphicsDevice->SetRenderState(D3DRS_ZENABLE, FALSE);

	// View transformation
	gGraphicsDevice->SetTransform(D3DTS_VIEW, &gOrthoViewMatrix);
	
	// Perspective transformation
	gGraphicsDevice->SetTransform(D3DTS_PROJECTION, &gScreenProjectionMatrix);

	// Render the texture to the screen
	gGraphicsDevice->BeginScene();
	{
		gGraphicsDevice->SetFVF(SCREEN_QUAD_VERTEX_FVF);
		gGraphicsDevice->SetTexture(0, gMainRenderTexture);
		gGraphicsDevice->SetStreamSource(0, gScreenQuadVertexBuffer, 0, sizeof(ScreenQuadVertex));
		gGraphicsDevice->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 2);

		gGraphicsDevice->SetStreamSource(0, nullptr, 0, 0);
		gGraphicsDevice->SetTexture(0, nullptr);
	}
	gGraphicsDevice->EndScene();
}


/**
 * Renders the scene using the render method.
 */
void RenderScene_Fast()
{
	// Disable lighting
	gGraphicsDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	RenderOffscreen();
	RenderToScreen();

	gGraphicsDevice->Present(nullptr, nullptr, nullptr, nullptr);
}


/**
 * Captures the frame asynchronously in BGRA format from the current scene render target.  Since it is asynchronous, the buffer will be returned when a 
 * request from a previous call is ready.  Because it is asynchronous it blocks as little as possible to ensure there is a 
 * minimal hit to the rendering pipeline.
 *
 * This implementation black-boxes the captured buffer in case the broadcast aspect ratio and game aspect ratio do not match.
 */
bool CaptureFrame_Fast(int captureWidth, int captureHeight, unsigned char*& outBgraFrame, int& outWidth, int& outHeight)
{
	// Clear the outputs until we have confirmed a capture
	outBgraFrame = nullptr;
	outWidth = 0;
	outHeight = 0;

	// Check for valid parameters
	if (gMainRenderTargetSurface == nullptr || 
		captureWidth <= 0 || 
		captureHeight <= 0 ||
		captureWidth % 16 != 0 ||
		captureHeight % 16 != 0)
	{
		return false;
	}

	// Get information about the source surface
	D3DSURFACE_DESC srcDesc;
	gMainRenderTargetSurface->GetDesc( &srcDesc );

	// Cancel all outstanding captures and re-allocate the capture textures
	if (gCaptureTexture == nullptr ||
		captureWidth != gCaptureWidth || 
		captureHeight != gCaptureHeight)
	{
		// Cleanup the previous texture
		SAFE_RELEASE(gCaptureTexture);
		SAFE_RELEASE(gCaptureSurface);

		// Release previous queries and surfaces
		for (int i = 0; i < NUM_CAPTURE_SURFACES; ++i)
		{
			SAFE_RELEASE(gCaptureQuery[i]);
			SAFE_RELEASE(gResizeSurface[i]);
		}

		// Allocate another texture of the correct size
		if ( FAILED(gGraphicsDevice->CreateTexture(captureWidth, captureHeight, 1, D3DUSAGE_RENDERTARGET, srcDesc.Format, D3DPOOL_DEFAULT, &gCaptureTexture, nullptr)) )
		{
			return false;
		}

		// Get the main surface from this texture
		if ( FAILED(gCaptureTexture->GetSurfaceLevel(0, &gCaptureSurface)) )
		{
			return false;
		}

		// Reallocate surfaces
		for (int i = 0; i < NUM_CAPTURE_SURFACES; ++i)
		{
			// Create offscreen render targets for the results of the captures
			if ( FAILED(gGraphicsDevice->CreateOffscreenPlainSurface(captureWidth, captureHeight, srcDesc.Format, D3DPOOL_SYSTEMMEM, &gResizeSurface[i], nullptr)) )
			{
				return false;
			}
		}

		gCaptureGet = 0;
		gCapturePut = 0;

		gCaptureWidth = captureWidth;
		gCaptureHeight = captureHeight;
	}

	// We haven't queued too many requests
	if (gCapturePut - gCaptureGet < NUM_CAPTURE_SURFACES)
	{
		// Stretch and copy the image to the correct area of the destination (black-bordering if necessary)
		float captureAspect = (float)captureHeight / (float)captureWidth;
		float srcAspect = (float)srcDesc.Height / (float)srcDesc.Width;
		RECT destRect;

		// Determine the destination rectangle
		if (captureAspect >= srcAspect)
		{
			float scale = (float)captureWidth / (float)srcDesc.Width;

			destRect.left = 0;
			destRect.right = captureWidth-1;
			destRect.top = (LONG)( ((float)captureHeight - (float)srcDesc.Height*scale) / 2 );
			destRect.bottom = (LONG)( ((float)captureHeight + (float)srcDesc.Height*scale) / 2 );
		}
		else
		{
			float scale = (float)captureHeight / (float)srcDesc.Height;

			destRect.top = 0;
			destRect.bottom = captureHeight-1;
			destRect.left = (LONG)( ((float)captureWidth - (float)srcDesc.Width*scale) / 2 );
			destRect.right = (LONG)( ((float)captureWidth + (float)srcDesc.Width*scale) / 2 );
		}

		int idx = gCapturePut % NUM_CAPTURE_SURFACES;

		// Resample the main render target to our capture render target
		if ( FAILED(gGraphicsDevice->StretchRect(gMainRenderTargetSurface, nullptr, gCaptureSurface, &destRect, D3DTEXF_LINEAR)) )
		{
			return false;
		}

		// Copy data from the rendertarget to the memory surface
		if ( FAILED(gGraphicsDevice->GetRenderTargetData(gCaptureSurface, gResizeSurface[idx])) )
		{
			return false;
		}

		// Create a query that will indicate when the GetRenderTargetData call has finished
		if (gCaptureQuery[idx] == nullptr)
		{
			gGraphicsDevice->CreateQuery(D3DQUERYTYPE_EVENT, &gCaptureQuery[idx]);
		}

		// Schedule the query
		gCaptureQuery[idx]->Issue( D3DISSUE_END );

		++gCapturePut;
	}

	// Get the latest capture
	{
		int idx = gCaptureGet % NUM_CAPTURE_SURFACES;
		if (gCaptureGet != gCapturePut && 
			gCaptureQuery[idx] && 
			gCaptureQuery[idx]->GetData(nullptr, 0, 0) == S_OK)
		{
			D3DLOCKED_RECT locked;
			memset( &locked, 0, sizeof(locked) );

			// Attempt to lock the surface to obtain the pixel data
			HRESULT ret = gResizeSurface[idx]->LockRect(&locked, nullptr, D3DLOCK_READONLY);
			if ( SUCCEEDED(ret) )
			{
				// Create the output buffer
				int bufferSize = captureWidth*captureHeight*4;

				// grab the free buffer from the streaming pool
				outBgraFrame = GetNextFreeBuffer();
				if (!outBgraFrame)
				{
					gResizeSurface[idx]->UnlockRect();
					return false;
				}
				
				memcpy(outBgraFrame, locked.pBits, bufferSize);

				// Unlock the surface
				gResizeSurface[idx]->UnlockRect();
				++gCaptureGet;

				outWidth = captureWidth;
				outHeight = captureHeight;

				return true;
			}
		}
	}

	return false;
}
