//////////////////////////////////////////////////////////////////////////////
// This header contains the interface for a slow capture method which relies
// on a synchronous capture.  It is not recommeded that this method is
// used because it may introduce stalls in rendering.
// 
// PROS: Uses little extra memory
// 
// CONS: Horribly slow
//////////////////////////////////////////////////////////////////////////////

#ifndef CAPTURESLOW_D3D_H
#define CAPTURESLOW_D3D_H

/**
 * Initializes the rendering method with the given 
 */
void InitRendering_Slow(unsigned int windowWidth, unsigned int windowHeight);
void RenderScene_Slow();
bool CaptureFrame_Slow(int captureWidth, int captureHeight, unsigned char*& outBgraFrame);
void DeinitRendering_Slow();

#endif
