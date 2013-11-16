By downloading these files you agree to the terms of service below https://docs.google.com/a/justin.tv/document/d/1Gt5R4sk6dU1YWDUL7cu05Q7GNNSg4GIrd9793oUSv9s

#How To Integrate the Twitch Broadcasting SDK

###PC

- Run build_jsoncpp.bat to build the jsoncpp libs

- You will need 2012 or later to build the code. Open the twitchsdk.sln in Visual Studio and select the appropriate config to build. If you want to statically link the SDK code into your game select one of the non-dll configs; otherwise select the dll configs and build.

- Add the following SDK include directories to your game's include path and include twitchsdk.h:  
	- /twitchsdk/include  
	- /twitchsdk/twitchcore/include

- Add the SDK lib directory to your game's linker include path and add the appropriate twitchsdk lib to your linker input

- Copy all the DLL's for your platform (win32 or x64) from twitchsdk/intel/bin, twitchsdk/ffmpeg/bin/, and twitchsdk/libmp3lame/bin to your executable's directory.  

- If you get compiler errors about inttypes.h create one (in twitchsdk/include is fine) and just #include \<stdint.h\> in it  

- In order to stream to your channel, you'll need to supply the SDK with your Twitch user name and password, as well as your application's Client ID and Client Secret. To get your Client ID and Secret:
	- Go to twitch.tv and log into your account
	- From the top right, click on your channel name and select Settings
	- Select Applications
	- Under Developer Applications, select Register your application
	- Enter your application name and website and click Register
	- On the next page you'll see your Client ID and Client Secret
	- If you've already registered, you can just click Edit to see your Client ID and Client Secret
 	- **IMPORTANT** 
  		- You'll need to provide us with this Client ID so that we can  whitelist it prior to streaming (no need to do this for the Client Secret)
        - If your endpoint is api.justin.tv you have googled and stumbled across the old api we are currently depracating

###iOS  
- Copy the libtwitchsdk.a library from the ioslibs.tar file under the twitchbins repo to a location that you'll include in your project's library path  
- Either add the SDK include directory to your project's include path or copy the header files there to a folder that's in your include path
- Include twitchsdk.h
- Minimum supported iOS deployment target is iOS 5.0
- Please note that you cannot build for the simulator (only building for the device is supported)
- Make sure you have the following frameworks included: AVFoundation, Security, CoreVideo, CoreMdia
- If you get STL linker errors you'll need to set your project setting for "C++ Standard Library" to "libc++ (LLVM C++ standard library with C++11 support)." This won't work for iOS < 5.0
- Follow the steps for getting your client ID and Secret and whitelisting it as described in the PC section above
- For audio capture to work, you must initialize your audio session with the kAudioSessionCategory_PlayAndRecord category

###FAQ
####Why am I getting TTV_EC_API_REQUEST_FAILED when calling TTV_RequestAuthToken or TTV_Login?

You will receive TTV_EC_API_REQUEST_FAILED as an error if you attempt to use a client id you create without first sending it to Twitch for white-listing. Email it to your Twitch contact (or brooke@twitch.tv) to have it approved. Otherwise, sometimes this error can occur randomly because of a communication problem with the Twitch servers. Just try again in these cases.
