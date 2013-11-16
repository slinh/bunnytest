//////////////////////////////////////////////////////////////////////////////
// This header contains the interface for a faster capture method which relies
// on asynchronous capturing and events from DirectX to indicate that the capture
// is complete.  This is a preferred method to use in production to minimize
// delays introduced by broadcasting.
// 
// PROS: Extremely fast
// 
// CONS: Uses a significant amount of extra memory, captures are asynchronous
//////////////////////////////////////////////////////////////////////////////

#ifndef CAPTUREFAST_D3D_H
#define CAPTUREFAST_D3D_H

/**
 * Initializes the render method.  This should be called when the screen or broadcast resolution changes.
 */
void InitRendering_Fast(unsigned int windowWidth, unsigned int windowHeight, unsigned int broadcastWidth, unsigned int broadcastHeight);
void RenderScene_Fast();
bool CaptureFrame_Fast(int captureWidth, int captureHeight, unsigned char*& outBgraFrame, int& outWidth, int& outHeight);
void DeinitRendering_Fast();

#endif
