#include "stdafx.h"
#include "captureslow_d3d.h"
#include "../wavemesh.h"
#include "../streaming.h"

#include <d3d9.h>
#include <d3dx9math.h>

#define SAFE_RELEASE(x) if (x) { x->Release(); x = nullptr; } 

extern IDirect3DDevice9* gGraphicsDevice;
extern D3DXMATRIX gViewMatrix;
extern D3DXMATRIX gProjectionMatrix;

static IDirect3DSurface9* gCaptureSurface;				// A destination surface to write the backbuffer to.
static IDirect3DTexture9* gResizeTexture = nullptr;
static IDirect3DSurface9* gResizeSurface = nullptr;

static unsigned int gWindowWidth = 0;					
static unsigned int gWindowHeight = 0;
static unsigned int gBroadcastWidth = 0;
static unsigned int gBroadcastHeight = 0;


/**
 * Initializes the render method.
 */
void InitRendering_Slow(unsigned int windowWidth, unsigned int windowHeight)
{
	DeinitRendering_Slow();

	gWindowWidth = windowWidth;
	gWindowHeight = windowHeight;
}


/**
 * Cleans up the render method.
 */
void DeinitRendering_Slow()
{
	SAFE_RELEASE(gCaptureSurface);
	SAFE_RELEASE(gResizeSurface);
	SAFE_RELEASE(gResizeTexture);
}


/**
 * Renders the scene using the render method.
 */
void RenderScene_Slow()
{
	gGraphicsDevice->SetRenderState(D3DRS_LIGHTING, FALSE);

	// View transformation
	gGraphicsDevice->SetTransform(D3DTS_VIEW, &gViewMatrix);

	// Perspective transformation
	gGraphicsDevice->SetTransform(D3DTS_PROJECTION, &gProjectionMatrix);

	gGraphicsDevice->Clear(0, nullptr, D3DCLEAR_TARGET, D3DCOLOR_XRGB(0, 0, 255), 1.0f, 0);

	gGraphicsDevice->BeginScene();
	{
		// Render the wave mesh
		DrawWaveMesh();
	}
	gGraphicsDevice->EndScene();

	gGraphicsDevice->Present(nullptr, nullptr, nullptr, nullptr);
}


/**
 * Captures the frame in BGRA format from the backbuffer.  This method works but is really slow because it locks the backbuffer
 * during the copy and prevents any furthur rendering until the copy is complete.  It also locks the backbuffer surface 
 * immediately after requesting the render target data which is bad because it doesn't give the GPU time to prepare the data
 * for locking asynchronously.
 */
bool CaptureFrame_Slow(int captureWidth, int captureHeight, unsigned char*& outBgraFrame)
{
	bool grabbed = false;

	// Grab the back buffer
	IDirect3DSurface9* pBackBuffer = nullptr;
	gGraphicsDevice->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &pBackBuffer);

	// Backbuffer not available
	if (pBackBuffer == nullptr)
	{
		return false;
	}

	// Get backbuffer info
	D3DSURFACE_DESC srcDesc;
	pBackBuffer->GetDesc(&srcDesc);

	// Create/recreate the target textures and surfaces if needed
	if (gResizeTexture == nullptr || captureHeight != gBroadcastHeight || captureWidth != gBroadcastWidth)
	{
		SAFE_RELEASE(gCaptureSurface);
		SAFE_RELEASE(gResizeSurface);
		SAFE_RELEASE(gResizeTexture);

		gBroadcastHeight = captureHeight;
		gBroadcastWidth = captureWidth;

		// Allocate a texture for the stretch and copy
		if ( FAILED(gGraphicsDevice->CreateTexture(captureWidth, captureHeight, 1, D3DUSAGE_RENDERTARGET, srcDesc.Format, D3DPOOL_DEFAULT, &gResizeTexture, nullptr)) )
		{
			ReportError("Error allocating resize texture");
			return false;
		}

		if ( FAILED(gResizeTexture->GetSurfaceLevel(0, &gResizeSurface)) )
		{
			ReportError("Error retrieving surface");
			return false;
		}

		// Allocate a surface for capturing the final buffer
		if ( FAILED(gGraphicsDevice->CreateOffscreenPlainSurface(captureWidth, captureHeight, srcDesc.Format, D3DPOOL_SYSTEMMEM, &gCaptureSurface, nullptr)) )
		{
			ReportError("Error allocating capture surface");
			return false;
		}

		// Clear the surface to black
		if ( FAILED(gGraphicsDevice->ColorFill(gResizeSurface, nullptr, D3DCOLOR_ARGB(0, 0, 0, 0))) )
		{
			ReportError("Error filling with black");
			return false;
		}
	}
	
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

	// Do the stretch and copy
	if ( FAILED(gGraphicsDevice->StretchRect(pBackBuffer, nullptr, gResizeSurface, &destRect, D3DTEXF_LINEAR)) )
	{
		ReportError("Error in StretchRect");
		return false;
	}

	// Capture the results of the stretch and copy
	gGraphicsDevice->GetRenderTargetData(gResizeSurface, gCaptureSurface);

	// This is expensive since LockRect will block until GetRenderTargetData finishes
	D3DLOCKED_RECT rect;
	if ( FAILED(gCaptureSurface->LockRect(&rect, 0, D3DLOCK_READONLY)) )
	{
		ReportError("Error locking rectangle");
		gCaptureSurface->UnlockRect();
		return false;
	}

	const int kPixelSize = 4;
	const int bgraWidthBytes = captureWidth * kPixelSize;
	const int bgraFrameBytes = captureWidth * captureHeight * kPixelSize;

	// Grab the free buffer from the streaming pool
	outBgraFrame = GetNextFreeBuffer();

	// Copy the buffer
	if (outBgraFrame != nullptr)
	{
		for (int y = 0; y < captureHeight; ++y)
		{				
			memcpy( outBgraFrame + y * bgraWidthBytes, (char*)rect.pBits +  y * bgraWidthBytes, bgraWidthBytes );
		}
		grabbed = true;
	}

	gCaptureSurface->UnlockRect();
	pBackBuffer->Release();

	return grabbed;
}
