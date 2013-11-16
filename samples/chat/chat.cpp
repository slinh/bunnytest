// chat.cpp : Defines the entry point for the application.
//

#include "stdafx.h"
#include "chat.h"
#include "twitchsdk.h"
#include "twitchchat.h"

// Forward declarations of functions included in this code module:
INT_PTR CALLBACK	DialogProc(HWND, UINT, WPARAM, LPARAM lParam);

HWND hChannelMembers = NULL;
HWND hMessages = NULL;
HWND hInputBox = NULL;

const char gClientId[] = "<client id>";
const char gClientSecret[] = "<client secret>";

// Stuff for init file
const int kMaxUserName = 128;
const int kMaxPassword = 128;
char gUserName[kMaxUserName];
char gPassword[kMaxPassword];
TTV_AuthToken gAuthToken;
bool gReceivedAuthToken = false;
bool gWaitingForAuthToken = false;

void* AllocCallback (size_t size, size_t alignment)
{
	return _aligned_malloc(size, alignment);
}

void FreeCallback (void* ptr)
{
	return _aligned_free(ptr);
}

//////////////////////////////////////////////////////////////////////////
// Callbacks for chat module
//////////////////////////////////////////////////////////////////////////
void ChatQueryChannelUsersCallback (const TTV_ChatUserList* userList, void* /*userdata*/)
{
	OutputDebugString (_T("ChatQueryChannelUsersCallback \n"));

	for (uint i = 0; i < userList->userCount; ++i)
	{
		SendMessageA(hChannelMembers, LB_ADDSTRING, 0, (LPARAM)userList->userList[i].displayName);
	}
	
	//////////////////////////////////////////////////////////////////////////
	// Important to free user lists when we are done with them
	//////////////////////////////////////////////////////////////////////////
	TTV_Chat_FreeUserList(userList);
}

void ChatStatusCallback(TTV_ErrorCode result, void* /*userdata*/)
{
	if (TTV_SUCCEEDED(result))
	{
		OutputDebugString (_T("ChatStatusCallback SUCCESS\n"));
	}
	else if (result == TTV_EC_CHAT_LOST_CONNECTION)
	{
		OutputDebugString (_T("ChatStatusCallback LOST_CONNECTION\n"));
	}
	else
	{
		OutputDebugString (_T("ChatStatusCallback FAILED\n"));
		ASSERT_ON_ERROR(result);
	}
}

void ChatMembershipCallback(TTV_ChatEvent evt, const TTV_ChatChannelInfo* channelInfo, void* /*userdata*/)
{
	switch (evt)
	{
	case TTV_CHAT_JOINED_CHANNEL:
		OutputDebugString (_T("ChatMembershipCallback --- Local user Joined\n"));
		break;
	case TTV_CHAT_LEFT_CHANNEL:
		OutputDebugString (_T("ChatMembershipCallback --- Local user Left\n"));
		break;
	default:
		OutputDebugString (_T("ChatMembershipCallback --- Unknown value\n"));
		break;
	}
}

void ChatUserCallback (const TTV_ChatUserList* joinList, const TTV_ChatUserList* leaveList, const TTV_ChatUserList* userInfoList, void* /*userdata*/)
{
	OutputDebugString (_T("ChatUserCallback \n"));

	for (uint i = 0; i < leaveList->userCount; ++i)
	{
		auto index = SendMessageA(hChannelMembers, LB_FINDSTRING, -1, (LPARAM)leaveList->userList[i].displayName);
		SendMessageA(hChannelMembers, LB_DELETESTRING, index, 0);
	}

	for (uint i = 0; i < joinList->userCount; ++i)
	{
		SendMessageA(hChannelMembers, LB_ADDSTRING, 0, (LPARAM)joinList->userList[i].displayName);
	}

	//////////////////////////////////////////////////////////////////////////
	// Important to free user lists when we are done with them
	//////////////////////////////////////////////////////////////////////////
	TTV_Chat_FreeUserList(joinList);
	TTV_Chat_FreeUserList(leaveList);
	TTV_Chat_FreeUserList(userInfoList);
}

void ChatMessageCallback (const TTV_ChatMessageList* messageList, void* /*userdata*/)
{
	assert (messageList);
	assert (hMessages);

	const int maxMessageSize = kMaxChatUserNameLength + kMaxChatMessageLength + sizeof ("<> ") + 1;

	utf8char buffer[maxMessageSize];

	for (uint i = 0; i < messageList->messageCount; ++i)
	{
		sprintf_s (buffer, "<%s> %s", messageList->messageList[i].userName, messageList->messageList[i].message);
		SendMessageA(hMessages, LB_ADDSTRING, NULL, (LPARAM)buffer);
	}
}

void ChatClearCallback(const utf8char* channelName, void* /*userdata*/)
{
	assert (hMessages);

	OutputDebugString (_T("ChatClearCallback \n"));

	SendMessageA(hMessages, LB_RESETCONTENT, NULL, NULL);
}


void AuthDoneCallback(TTV_ErrorCode result, void* userData)
{
	gWaitingForAuthToken = false;

	if ( result == TTV_EC_API_REQUEST_FAILED || 
		 TTV_FAILED(result) )
	{
		gReceivedAuthToken = false;

		const char* err = TTV_ErrorToString(result);
		OutputDebugStringA("AuthDoneCallback: ");
		OutputDebugStringA(err);
		OutputDebugStringA("\n");
	}
	else
	{
		gReceivedAuthToken = true;
	}
}

void EmoticonDataDownloadCallback(TTV_ErrorCode error, void* /*userdata*/)
{
	assert( TTV_SUCCEEDED(error) );

	// grab the texture data
	if (TTV_SUCCEEDED(error))
	{
		TTV_ChatEmoticonData* data = nullptr;

		TTV_ErrorCode ret = TTV_Chat_GetEmoticonData(&data);
		assert( TTV_SUCCEEDED(ret) );

		// TODO: make use of the emoticon and badge data

		ret = TTV_Chat_FreeEmoticonData(data);
		assert( TTV_SUCCEEDED(ret) );
	}
}

INT_PTR CALLBACK DialogProc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case IDM_EXIT:
			DestroyWindow(hwndDlg);
			break;
		case IDOK:
			{
				TCHAR currentText[kMaxChatMessageLength+1];
				utf8char utf8Buffer[kMaxChatMessageLength+1];
				int length = (int)SendMessage(hInputBox, EM_GETLINE, 0, (LPARAM)currentText);
				SetWindowText(hInputBox, _T(""));

				if (length > 0)
				{
					length = WideCharToMultiByte(CP_UTF8, 0, currentText, length, utf8Buffer, kMaxChatMessageLength+1, NULL, NULL);
					assert (length > 0);
					utf8Buffer[length] = 0;

					TTV_Chat_SendMessage(utf8Buffer);
				}

				break;
			}
		}
		break;
	case WM_CLOSE:
		DestroyWindow(hwndDlg);
		break;
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	}
	return FALSE;
}

INT_PTR CALLBACK PasswordDialogFunc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case IDOK:
			GetWindowTextA(GetDlgItem(hwndDlg, IDC_USERNAME), gUserName, kMaxUserName);
			GetWindowTextA(GetDlgItem(hwndDlg, IDC_PASSWORD), gPassword, kAuthTokenBufferSize);

			EndDialog(hwndDlg, TRUE);
			break;
		case IDCANCEL:
			EndDialog(hwndDlg, FALSE);
			break;
		}
		break;
	case WM_CLOSE:
		EndDialog(hwndDlg, FALSE);
		break;
	}
	return FALSE;
}

int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR /*lpCmdLine*/, int nCmdShow)
{
	//////////////////////////////////////////////////////////////////////////
	// Initialize the SDK
	//////////////////////////////////////////////////////////////////////////
	TTV_MemCallbacks memCallbacks;
	memCallbacks.size = sizeof (TTV_MemCallbacks);
	memCallbacks.allocCallback = AllocCallback;
	memCallbacks.freeCallback = FreeCallback;

	TTV_ErrorCode ret = TTV_Init(
		&memCallbacks, 
		gClientId,		
		_T(""));
	ASSERT_ON_ERROR(ret);

	TTV_ChatCallbacks chatCallbacks;
	memset(&chatCallbacks, 0, sizeof(chatCallbacks));
	chatCallbacks.statusCallback = ChatStatusCallback;
	chatCallbacks.membershipCallback = ChatMembershipCallback;
	chatCallbacks.userCallback = ChatUserCallback;
	chatCallbacks.messageCallback = ChatMessageCallback;
	chatCallbacks.clearCallback = ChatClearCallback;
	chatCallbacks.unsolicitedUserData = nullptr;

	HWND hWnd = CreateDialog(hInstance, MAKEINTRESOURCE(IDD_CHAT_DIALOG), 0, DialogProc);

	hChannelMembers = GetDlgItem(hWnd, IDC_MEMBERS);
	hMessages = GetDlgItem(hWnd, IDC_MESSAGES);
	hInputBox = GetDlgItem(hWnd, IDC_MESSAGE);

	MSG msg;
	memset(&msg, 0, sizeof(msg));

	if (hWnd)
	{
		ShowWindow(hWnd, nCmdShow);
		UpdateWindow(hWnd);

		HACCEL hAccelTable;
		hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_CHAT));

		gReceivedAuthToken = false;
		while (!gReceivedAuthToken)
		{
			auto passwordRet = DialogBox(hInstance, MAKEINTRESOURCE(IDD_PASSWORD), hWnd, PasswordDialogFunc);
			if (passwordRet == FALSE)
			{
				gReceivedAuthToken = false;
				break;
			}

			// Now that we have the password we must obtain an auth token from it
			TTV_AuthParams authParams;
			authParams.size = sizeof(TTV_AuthParams);
			authParams.userName = gUserName;
			authParams.password = gPassword;
			authParams.clientSecret = gClientSecret;

			gWaitingForAuthToken = true;
			TTV_ErrorCode ret = TTV_RequestAuthToken(&authParams, AuthDoneCallback, nullptr, &gAuthToken);
			if ( TTV_FAILED(ret) )
			{
				const char* err = TTV_ErrorToString(ret);
				printf("TTV_RequestAuthToken failed: %s\n", err);
				gReceivedAuthToken = false;
				break;
			}

			// Wait for the auth token to come in
			while (gWaitingForAuthToken)
			{
				Sleep(10);
				TTV_PollTasks();
			}
		}

		if (gReceivedAuthToken)
		{
			//////////////////////////////////////////////////////////////////////////
			// Initialize the Chat module
			//////////////////////////////////////////////////////////////////////////
			ret = TTV_Chat_Init(
				gUserName,				
				&chatCallbacks);
			ASSERT_ON_ERROR(ret);

			ret = TTV_Chat_DownloadEmoticonData(false, 
				EmoticonDataDownloadCallback, 
				nullptr);
			ASSERT_ON_ERROR(ret);

			ret = TTV_Chat_Connect(
				gUserName,
				gAuthToken.data
				);
			ASSERT_ON_ERROR(ret);

			while (true)
			{
				while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE) == FALSE) 
				{
					//////////////////////////////////////////////////////////////////////////
					// Call flushevents to make sure that the sdk can call your callbacks.
					//////////////////////////////////////////////////////////////////////////
					TTV_Chat_FlushEvents();
				}

				if (msg.message == WM_QUIT)
				{
					break;
				}

				if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
				{
					if (!IsDialogMessage(hWnd, &msg))
					{
						TranslateMessage(&msg);
						DispatchMessage(&msg);
					}
				}
			}

			//////////////////////////////////////////////////////////////////////////
			// Cleanup
			//////////////////////////////////////////////////////////////////////////
			TTV_Chat_Shutdown();
		}
	}

	TTV_Shutdown();

	return (int) msg.wParam;
}
