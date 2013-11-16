/********************************************************************************************
* Twitch Broadcasting SDK
*
* This software is supplied under the terms of a license agreement with Justin.tv Inc. and
* may not be copied or used except in accordance with the terms of that agreement
* Copyright (c) 2012-2013 Justin.tv Inc.
*********************************************************************************************/

#pragma once

#include <stdint.h>
#include <cstddef>

#if TTV_PLATFORM_MAC
#	define TTVSDK_API __attribute__((visibility("default")))
#else
#	define TTVSDK_API
#endif

/**
 * Specifies that the string is encoded as UTF-8.
 */
typedef char utf8char;
typedef unsigned int uint;

//lint -emacro(920, UNUSED) Cast from type to void
#define UNUSED(x) (void)x;


