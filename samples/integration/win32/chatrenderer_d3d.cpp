////////////////////////////////////////////////////////////////////////////////////
// This file contains the implementation of the chat rendering module for Direct3D
////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"

#include "../chatrenderer.h"

#include <assert.h>
#include <list>
#include <vector>
#include <memory>

#include <d3d9.h>
#include <d3dx9math.h>

#include <ft2build.h>
#include FT_FREETYPE_H

#define SAFE_RELEASE(x) if (x) { x->Release(); x = nullptr; } 
#define FONT_QUAD_VERTEX_FVF (D3DFVF_XYZ | D3DFVF_TEX2 | D3DFVF_DIFFUSE)


struct QuadVertex
{
	D3DVECTOR v;
	D3DCOLOR c;
	FLOAT tx, ty;
};

struct Glyph
{
	// texture coordinates
	float x1;
	float y1;
	float x2;
	float y2;

	int pixelAdvance;
	int height;
	int width;
	int left;
	int top;
};


struct FontData
{
	Glyph glyphs[256];
	IDirect3DTexture9* texture;
	int pixelHeight;
};


struct EmoticonTextureData
{
	IDirect3DTexture9* texture;
	float width;
	float height;
};


struct ChatLine
{
	const TTV_ChatTokenizedMessageList* list;
	int index;
	FontData* font;

	~ChatLine()
	{
		// deleting the last message in this message list so free it
		if (list && index == list->messageCount-1)
		{
			TTV_Chat_FreeTokenizedMessageList(list);
			list = nullptr;
		}
	}
};


struct ChatUser
{
	utf8char username[kMaxChatUserNameLength];
	FontData* font;
	unsigned int nameColorARGB;
	TTV_ChatUserMode modes;
	TTV_ChatUserSubscription subscriptions;
};


bool LoadFontIntoTexture(FT_Library freeTypeLibrary, const std::string& fontPath, int pixelHeight, FontData& data);


extern IDirect3DDevice9* gGraphicsDevice;

static IDirect3DVertexBuffer9* gTextureQuadVertexBuffer = nullptr;	// The vertex buffer used to render the entire font texture.
static std::vector<EmoticonTextureData> gEmoticonTextures;
static D3DXMATRIX gOrthoViewMatrix;								// The screen tranlsation matrix.
static D3DXMATRIX gScreenProjectionMatrix;						// The orthographic screen projection matrix.
static FontData gNormalFont;
static FontData gBoldFont;
static TTV_ChatBadgeData gBadgeData;

static TTV_ChatTokenizedMessageList gInputTokenizedMessageList;
static TTV_ChatTokenizedMessage gInputTokenizedMessage;
static TTV_ChatMessageToken gInputToken;

static std::vector<QuadVertex> gVertices;					// Scratch buffer to save frequent allocations.
static std::list< std::shared_ptr<ChatLine> > gChatLines;
static std::list< std::shared_ptr<ChatUser> > gChatUsers;
static size_t gMaxChatLines = 0;
static unsigned int gWindowWidth = 0;
static unsigned int gWindowHeight = 0;
static std::shared_ptr<ChatLine> gInputText;
static const int kLineSpacing = 2;
static const int kBadgeSpacing = 2;
static const int kFontSize = 24;
static const int kUserListWidth = 150;
static const int kMessagesLeft = 1;

const unsigned int KBlackColor = 0xFF000000;
const unsigned int KWhiteColor = 0xFFFFFFFF;
const char kBackspaceCharacter = 8;


void InitializeChatRenderer(unsigned int windowWidth, unsigned int windowHeight)
{
	FT_Library freeTypeLibrary = nullptr;

	FT_Error error = FT_Init_FreeType( &freeTypeLibrary );
	if ( error )
	{
		ReportError("Error initializing FreeType");
		return;
	}

	gWindowWidth = windowWidth;
	gWindowHeight = windowHeight;

	gNormalFont.texture = nullptr;
	gBoldFont.texture = nullptr;

	LoadFontIntoTexture(freeTypeLibrary, "C:\\windows\\Fonts\\arial.ttf", kFontSize, gNormalFont);
	LoadFontIntoTexture(freeTypeLibrary, "C:\\windows\\Fonts\\arialbd.ttf", kFontSize, gBoldFont);

	gMaxChatLines = windowHeight / (kFontSize + kLineSpacing);
	gMaxChatLines -= 1; // leave space for the input line

	FT_Done_FreeType(freeTypeLibrary);
	freeTypeLibrary = nullptr;

	// Setup the ortho projection for the render to screen
	D3DXMatrixOrthoOffCenterLH(&gScreenProjectionMatrix, 0, (FLOAT)windowWidth, 0, (FLOAT)windowHeight, 1, 100);

	// Setup the screen translation
	D3DXMatrixIdentity(&gOrthoViewMatrix);

	// Setup the font texture
	if ( FAILED(gGraphicsDevice->CreateVertexBuffer(4*sizeof(QuadVertex), 0, FONT_QUAD_VERTEX_FVF, D3DPOOL_MANAGED, &gTextureQuadVertexBuffer, nullptr)) )
	{
		ReportError("Error creating vertex buffer");
		return;
	}

	QuadVertex* vertices = nullptr;
	if ( FAILED(gTextureQuadVertexBuffer->Lock(0, 0, reinterpret_cast<void**>(&vertices), 0)) )
	{
		ReportError("Vertex buffer lock failed");
		return;
	}

	vertices[0].v.x = 0;
	vertices[0].v.y = 0;
	vertices[0].v.z = 1;
	vertices[0].tx = 0;
	vertices[0].ty = 1;

	vertices[1].v.x = 0;
	vertices[1].v.y = (FLOAT)windowHeight;
	vertices[1].v.z = 1;
	vertices[1].tx = 0;
	vertices[1].ty = 0;

	vertices[2].v.x = (FLOAT)windowWidth;
	vertices[2].v.y = 0;
	vertices[2].v.z = 1;
	vertices[2].tx = 1;
	vertices[2].ty = 1;

	vertices[3].v.x = (FLOAT)windowWidth;
	vertices[3].v.y = (FLOAT)windowHeight;
	vertices[3].v.z = 1;
	vertices[3].tx = 1;
	vertices[3].ty = 0;

	gTextureQuadVertexBuffer->Unlock();

	// mark the badges as invalid
	gBadgeData.adminIcon.type = TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE;
	gBadgeData.adminIcon.data.textureImage.sheetIndex = -1;
	gBadgeData.broadcasterIcon.type = TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE;
	gBadgeData.broadcasterIcon.data.textureImage.sheetIndex = -1;
	gBadgeData.channelSubscriberIcon.type = TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE;
	gBadgeData.channelSubscriberIcon.data.textureImage.sheetIndex = -1;
	gBadgeData.moderatorIcon.type = TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE;
	gBadgeData.moderatorIcon.data.textureImage.sheetIndex = -1;
	gBadgeData.staffIcon.type = TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE;
	gBadgeData.staffIcon.data.textureImage.sheetIndex = -1;
	gBadgeData.turboIcon.type = TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE;
	gBadgeData.turboIcon.data.textureImage.sheetIndex = -1;
}


bool LoadFontIntoTexture(FT_Library freeTypeLibrary, const std::string& fontPath, int pixelHeight, FontData& data)
{
	FT_Face face = nullptr;

	// load the font file
	FT_Error error = FT_New_Face( freeTypeLibrary, fontPath.c_str(), 0, &face );
	if (error)
	{
		ReportError("Error loading font file");
		return false;
	}

	error = FT_Set_Pixel_Sizes(face, 0, pixelHeight);
	if (error)
	{
		ReportError("Error setting font pixel size");
		return false;
	}

	const int kTextureDim = 1024;
	SAFE_RELEASE(data.texture);

	// Allocate another texture of the correct size
	if ( FAILED(gGraphicsDevice->CreateTexture(kTextureDim, kTextureDim, 1, 0, D3DFMT_A8, D3DPOOL_MANAGED, &data.texture, nullptr)) )
	{
		ReportError("Error creating texture");
		return false;
	}

	// lock the texture for writing
	D3DLOCKED_RECT rect;
	
	if ( FAILED(data.texture->LockRect(0, &rect, nullptr, D3DLOCK_DISCARD)) )
	{
		ReportError("Error locking texture");
		return false;
	}
	
	// clear the texture to alpha 0
	memset(rect.pBits, 0, static_cast<size_t>(kTextureDim*kTextureDim));

	// load each character
	int pixelWidth = pixelHeight;
	int x = 1;
	int y = 1;
	int maxRowHeight = 0;
	int destRowSize = static_cast<int>(kTextureDim);

	for (int ch=0; ch<256; ++ch)
	{
		error = FT_Load_Char( face, ch, FT_LOAD_RENDER );

		if (error)
		{
			// fill the character with junk
			continue;
		}

		// check for new row
		if (x+face->glyph->bitmap.width+1 >= kTextureDim)
		{
			x = 1;
			y += maxRowHeight + 2;

			maxRowHeight = 0;
		}
		
		maxRowHeight = std::max(maxRowHeight, face->glyph->bitmap.rows);

		for (int i=0; i<face->glyph->bitmap.rows; ++i)
		{
			unsigned char* dest = reinterpret_cast<unsigned char*>(rect.pBits);
			dest += (y+i)*destRowSize;
			dest += x;
			
			unsigned char* src = reinterpret_cast<unsigned char*>(face->glyph->bitmap.buffer);
			src += face->glyph->bitmap.width * i;

			memcpy(dest, src, face->glyph->bitmap.width);
		}

		data.glyphs[ch].x1 = (static_cast<float>(x) - 0.5f) / static_cast<float>(kTextureDim);
		data.glyphs[ch].y1 = (static_cast<float>(y) - 0.5f) / static_cast<float>(kTextureDim);
		data.glyphs[ch].x2 = (static_cast<float>(x+face->glyph->bitmap.width) + 0.5f) / static_cast<float>(kTextureDim);
		data.glyphs[ch].y2 = (static_cast<float>(y+face->glyph->bitmap.rows) + 0.5f) / static_cast<float>(kTextureDim);

		data.glyphs[ch].pixelAdvance = face->glyph->advance.x >> 6;
		data.glyphs[ch].width = face->glyph->bitmap.width;
		data.glyphs[ch].height = face->glyph->bitmap.rows+1;
		data.glyphs[ch].left = face->glyph->bitmap_left;
		data.glyphs[ch].top = face->glyph->bitmap_top;

		x += face->glyph->bitmap.width + 2;
	}

	data.texture->UnlockRect(0);
	data.pixelHeight = pixelHeight;

	// clean up the face
	FT_Done_Face(face);
	face = nullptr;

	return true;
}


void ProcessEmoticonData()
{
	TTV_ChatEmoticonData* data = nullptr;

	TTV_ErrorCode ret = TTV_Chat_GetEmoticonData(&data);
	assert( TTV_SUCCEEDED(ret) );

	for (size_t i=0; i<data->textures.count; ++i)
	{
		const TTV_ChatTextureSheet& sheet = data->textures.list[i];

		EmoticonTextureData data;

		data.texture = nullptr;
		data.width = 0;
		data.height = 0;

		// Allocate another texture of the correct size
		if ( FAILED(gGraphicsDevice->CreateTexture(sheet.width, sheet.height, 1, 0, D3DFMT_A8R8G8B8, D3DPOOL_MANAGED, &data.texture, nullptr)) )
		{
			ReportError("Error creating texture");
			gEmoticonTextures.push_back(data);
			continue;
		}

		// lock the texture for writing
		D3DLOCKED_RECT rect;
	
		if ( FAILED(data.texture->LockRect(0, &rect, nullptr, D3DLOCK_DISCARD)) )
		{
			ReportError("Error locking texture");
			gEmoticonTextures.push_back(data);
			continue;
		}

		// copy the rgba data
		memcpy(rect.pBits, sheet.buffer, sheet.width*sheet.height*4);

		// unlock the texture
		data.texture->UnlockRect(0);

		data.width = static_cast<float>(sheet.width);
		data.height = static_cast<float>(sheet.height);

		gEmoticonTextures.push_back(data);
	}

	// get the badge data
	memcpy(&gBadgeData, &data->badges, sizeof(gBadgeData));

	// free the data
	ret = TTV_Chat_FreeEmoticonData(data);
}


int RenderString(int left, int bottom, unsigned int rgba, FontData& font, const utf8char* text)
{
	if (text == nullptr || text[0] == '\0')
	{
		return left;
	}

	int len = strlen(text);
	gVertices.resize(len * 6);

	float x = static_cast<float>(left);
	float y = static_cast<float>(bottom);
	int v = 0;

	for (int i=0; i<len; ++i)
	{
		Glyph& glyph = font.glyphs[ static_cast<unsigned char>(text[i]) ];

		float glyphBottom = y + glyph.top - glyph.height;

		// bottom-left
		gVertices[v].tx = glyph.x1;
		gVertices[v].ty = glyph.y2;
		gVertices[v].v.x = x + glyph.left + 0.5f;
		gVertices[v].v.y = glyphBottom;
		gVertices[v].v.z = 1;
		gVertices[v].c = rgba;
		v++;

		// top-left
		gVertices[v].tx = glyph.x1;
		gVertices[v].ty = glyph.y1;
		gVertices[v].v.x = x + glyph.left + 0.5f;
		gVertices[v].v.y = glyphBottom + glyph.height;
		gVertices[v].v.z = 1;
		gVertices[v].c = rgba;
		v++;

		// top-right
		gVertices[v].tx = glyph.x2;
		gVertices[v].ty = glyph.y1;
		gVertices[v].v.x = x + glyph.width + glyph.left + 0.5f;
		gVertices[v].v.y = glyphBottom + glyph.height;
		gVertices[v].v.z = 1;
		gVertices[v].c = rgba;
		v++;

		gVertices[v] = gVertices[v-3];
		v++;

		gVertices[v] = gVertices[v-2];
		v++;

		// bottom-right
		gVertices[v].tx = glyph.x2;
		gVertices[v].ty = glyph.y2;
		gVertices[v].v.x = x + glyph.width + glyph.left + 0.5f;
		gVertices[v].v.y = glyphBottom;
		gVertices[v].v.z = 1;
		gVertices[v].c = rgba;
		v++;

		x += glyph.pixelAdvance;
	}

	// cache the previous texture operation state
	DWORD colorop_0;
	DWORD colorarg_0;
	DWORD alphaop_0;
	DWORD alphaarg_0;
	DWORD colorop_1;
	DWORD colorarg1_1;
	DWORD colorarg2_1;

	gGraphicsDevice->GetTextureStageState(0, D3DTSS_COLOROP, &colorop_0);
	gGraphicsDevice->GetTextureStageState(0, D3DTSS_COLORARG1, &colorarg_0);
	gGraphicsDevice->GetTextureStageState(0, D3DTSS_ALPHAOP, &alphaop_0);
	gGraphicsDevice->GetTextureStageState(0, D3DTSS_ALPHAARG1, &alphaarg_0);

	gGraphicsDevice->GetTextureStageState(1, D3DTSS_COLOROP, &colorop_1);
	gGraphicsDevice->GetTextureStageState(1, D3DTSS_COLORARG1, &colorarg1_1);
	gGraphicsDevice->GetTextureStageState(1, D3DTSS_COLORARG2, &colorarg2_1);

	// set the texture sampling operations to take the rgb color from the vertex color and alpha from the texture
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);

	gGraphicsDevice->SetTextureStageState(1, D3DTSS_COLOROP, D3DTOP_MODULATE);
	gGraphicsDevice->SetTextureStageState(1, D3DTSS_COLORARG1, D3DTA_CURRENT);
	gGraphicsDevice->SetTextureStageState(1, D3DTSS_COLORARG2, D3DTA_TEXTURE);

	// render the string
	gGraphicsDevice->SetFVF(FONT_QUAD_VERTEX_FVF);
	gGraphicsDevice->SetTexture(0, font.texture);
	gGraphicsDevice->DrawPrimitiveUP(D3DPT_TRIANGLELIST, len*2, gVertices.data(), sizeof(QuadVertex));
	gGraphicsDevice->SetTexture(0, nullptr);

	// restore the previous texture operation state
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_COLOROP, colorop_0);
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_COLORARG1, colorarg_0);
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_ALPHAOP, alphaop_0);
	gGraphicsDevice->SetTextureStageState(0, D3DTSS_ALPHAARG1, alphaarg_0);

	gGraphicsDevice->SetTextureStageState(1, D3DTSS_COLOROP, colorop_1);
	gGraphicsDevice->SetTextureStageState(1, D3DTSS_COLORARG1, colorarg1_1);
	gGraphicsDevice->SetTextureStageState(1, D3DTSS_COLORARG2, colorarg2_1);

	// cleanup
	gVertices.clear();

	return static_cast<int>(x);
}


int RenderEmoticon(int left, int bottom, FontData& font, const TTV_ChatMessageToken* token)
{
	if (token->type == TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE)
	{
		const TTV_ChatTextureImageMessageToken& image = token->data.textureImage;
		if (image.sheetIndex < 0)
		{
			return left;
		}

		// get the texture
		EmoticonTextureData& data = gEmoticonTextures[image.sheetIndex];
		if (data.texture == nullptr)
		{
			return left;
		}

		float width = static_cast<float>(image.x2 - image.x1);
		float height = static_cast<float>(image.y2 - image.y1);

		float lineHeight =  static_cast<float>(font.pixelHeight) * 0.75f; // the rough actual height of text

		float x = static_cast<float>(left);
		float y = static_cast<float>(bottom + (lineHeight-height)/2);
		int v = 0;

		gVertices.resize(6);

		// bottom-left
		gVertices[v].tx = static_cast<float>(image.x1) / data.width;
		gVertices[v].ty = static_cast<float>(image.y2) / data.height;
		gVertices[v].v.x = x;
		gVertices[v].v.y = y;
		gVertices[v].v.z = 1;
		gVertices[v].c = 0xFFFFFFFF;
		v++;

		// top-left
		gVertices[v].tx = static_cast<float>(image.x1) / data.width;
		gVertices[v].ty = static_cast<float>(image.y1) / data.height;
		gVertices[v].v.x = x;
		gVertices[v].v.y = y + height;
		gVertices[v].v.z = 1;
		gVertices[v].c = 0xFFFFFFFF;
		v++;

		// top-right
		gVertices[v].tx = static_cast<float>(image.x2) / data.width;
		gVertices[v].ty = static_cast<float>(image.y1) / data.height;
		gVertices[v].v.x = x + width;
		gVertices[v].v.y = y + height;
		gVertices[v].v.z = 1;
		gVertices[v].c = 0xFFFFFFFF;
		v++;

		gVertices[v] = gVertices[v-3];
		v++;

		gVertices[v] = gVertices[v-2];
		v++;

		// bottom-right
		gVertices[v].tx = static_cast<float>(image.x2) / data.width;
		gVertices[v].ty = static_cast<float>(image.y2) / data.height;
		gVertices[v].v.x = x + width;
		gVertices[v].v.y = y;
		gVertices[v].v.z = 1;
		gVertices[v].c = 0xFFFFFFFF;
		v++;

		// render the image
		gGraphicsDevice->SetFVF(FONT_QUAD_VERTEX_FVF);
		gGraphicsDevice->SetTexture(0, data.texture);
		gGraphicsDevice->DrawPrimitiveUP(D3DPT_TRIANGLELIST, 2, gVertices.data(), sizeof(QuadVertex));
		gGraphicsDevice->SetTexture(0, nullptr);

		x += width + kBadgeSpacing;

		return static_cast<int>(x);
	}
	else if (token->type == TTV_CHAT_MSGTOKEN_URL_IMAGE)
	{
		left = RenderString(left, bottom, KWhiteColor, font, "[");
		left = RenderString(left, bottom, KWhiteColor, font, token->data.urlImage.url);
		left = RenderString(left, bottom, KWhiteColor, font, "]");
		return left;
	}
	else
	{
		assert(false);
		return left;
	}
}


void RenderFontTexture()
{
	// Disable depth buffering
	gGraphicsDevice->SetRenderState(D3DRS_ZENABLE, FALSE);

	// View transformation
	gGraphicsDevice->SetTransform(D3DTS_VIEW, &gOrthoViewMatrix);
	
	// Perspective transformation
	gGraphicsDevice->SetTransform(D3DTS_PROJECTION, &gScreenProjectionMatrix);

	// Render the texture
	gGraphicsDevice->BeginScene();
	{
		gGraphicsDevice->SetFVF(FONT_QUAD_VERTEX_FVF);
		gGraphicsDevice->SetTexture(0, gNormalFont.texture);

		gGraphicsDevice->SetStreamSource(0, gTextureQuadVertexBuffer, 0, sizeof(QuadVertex));
		gGraphicsDevice->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 2);

		gGraphicsDevice->SetStreamSource(0, nullptr, 0, 0);
		gGraphicsDevice->SetTexture(0, nullptr);
	}
	gGraphicsDevice->EndScene();
}


void AddChatUser(const TTV_ChatUserInfo* user)
{
	std::shared_ptr<ChatUser> entry( new ChatUser() );

	strncpy(entry->username, user->displayName, sizeof(entry->username));
	entry->username[sizeof(entry->username)-1] = '\0';
	entry->font = &gNormalFont;
	entry->nameColorARGB = user->nameColorARGB;
	entry->modes = user->modes;
	entry->subscriptions = user->subscriptions;

	gChatUsers.push_back(entry);
}


void RemoveChatUser(const TTV_ChatUserInfo* user)
{
	for (auto iter = gChatUsers.begin(); iter != gChatUsers.end(); ++iter)
	{
		std::shared_ptr<ChatUser> cur = *iter;
		if (strcmp(user->displayName, cur->username) == 0)
		{
			gChatUsers.erase(iter);
			return;
		}
	}
}


void UpdateChatUser(const TTV_ChatUserInfo* user)
{
	RemoveChatUser(user);
	AddChatUser(user);
}


void AddChatMessages(const TTV_ChatTokenizedMessageList* list)
{
	for (uint32_t i = 0; i< list->messageCount; ++i)
	{
		std::shared_ptr<ChatLine> line( new ChatLine() );

		line->font = list->messageList[i].action ? &gBoldFont : &gNormalFont;
		line->list = list;
		line->index = i;

		gChatLines.push_back(line);

		if (gChatLines.size() > gMaxChatLines)
		{
			gChatLines.erase( gChatLines.begin() );
		}
	}
}


void BeginChatInput()
{
	if (gInputText != nullptr)
	{
		return;
	}

	gInputText.reset( new ChatLine() );

	gInputToken.type = TTV_CHAT_MSGTOKEN_TEXT;
	gInputToken.data.text.buffer[0] = '\0';

	gInputText->font = &gNormalFont;

	gInputText->list = &gInputTokenizedMessageList;
	gInputText->index = 0;
	gInputTokenizedMessageList.messageCount = 1;
	gInputTokenizedMessageList.messageList = &gInputTokenizedMessage;
	gInputTokenizedMessage.tokenList = &gInputToken;
	gInputTokenizedMessage.tokenCount = 1;
	gInputTokenizedMessage.nameColorARGB = 0xFFFFFFFF;
	gInputTokenizedMessage.action = false;
	sprintf_s(gInputTokenizedMessage.displayName, sizeof(gInputTokenizedMessage.displayName), ">");
}


void AppendChatInput(char ch)
{
	if (gInputText == nullptr)
	{
		return;
	}

	int len = strlen(gInputToken.data.text.buffer);

	if (ch == kBackspaceCharacter)
	{
		if (len == 0)
		{
			return;
		}

		gInputToken.data.text.buffer[len-1] = '\0';
	}
	else
	{
		if (len == kMaxChatMessageLength-2)
		{
			return;
		}

		gInputToken.data.text.buffer[len] = ch;
		gInputToken.data.text.buffer[len+1] = '\0';
	}
}


void EndChatInput(bool submit)
{
	if (gInputText == nullptr)
	{
		return;
	}

	if (submit)
	{
		TTV_Chat_SendMessage( gInputToken.data.text.buffer );
	}

	// don't try and free the static message used for holding the input
	gInputText->list = nullptr;
	gInputText->index = -1;

	gInputText.reset();
}


bool AcceptingChatInput()
{
	return gInputText != nullptr;
}


void ClearChatMessages()
{
	gChatLines.clear();
}


void ClearChatUsers()
{
	gChatUsers.clear();
}


int RenderChatLine(int left, int bottom, std::shared_ptr<ChatLine> line, unsigned int color)
{
	const TTV_ChatTokenizedMessage& msg = line->list->messageList[line->index];

	// render the badges
	if (msg.modes & TTV_CHAT_USERMODE_BROADCASTER) left = RenderEmoticon(left, bottom, *line->font, &gBadgeData.broadcasterIcon);
	else if (msg.modes & TTV_CHAT_USERMODE_MODERATOR) left = RenderEmoticon(left, bottom, *line->font, &gBadgeData.moderatorIcon);

	if (msg.modes & TTV_CHAT_USERMODE_ADMINSTRATOR) left = RenderEmoticon(left, bottom, *line->font, &gBadgeData.adminIcon);
	if (msg.modes & TTV_CHAT_USERMODE_STAFF) left = RenderEmoticon(left, bottom, *line->font, &gBadgeData.staffIcon);
	if (msg.subscriptions & TTV_CHAT_USERSUB_SUBSCRIBER) left = RenderEmoticon(left, bottom, *line->font, &gBadgeData.channelSubscriberIcon);
	if (msg.subscriptions & TTV_CHAT_USERSUB_TURBO) left = RenderEmoticon(left, bottom, *line->font, &gBadgeData.turboIcon);

	// render the username
	utf8char username[kMaxChatUserNameLength + 8];
	sprintf_s(username, sizeof(username), line == gInputText ? "%s " : "%s: ", msg.displayName);
	left = RenderString(left, bottom, msg.nameColorARGB, *line->font, username);

	for (size_t t=0; t<msg.tokenCount; ++t)
	{
		const TTV_ChatMessageToken* token = &msg.tokenList[t];
		switch (token->type)
		{
			case TTV_CHAT_MSGTOKEN_TEXT:
			{
				left = RenderString(left, bottom, color, *line->font, token->data.text.buffer);
				break;
			}
			case TTV_CHAT_MSGTOKEN_TEXTURE_IMAGE:
			{
				left = RenderEmoticon(left, bottom, *line->font, token);
				break;
			}
			case TTV_CHAT_MSGTOKEN_URL_IMAGE:
			{
				left = RenderString(left, bottom, color, *line->font, "[");
				left = RenderString(left, bottom, color, *line->font, token->data.urlImage.url);
				left = RenderString(left, bottom, color, *line->font, "]");
				break;
			}
		}
	}

	return left;
}


void RenderChatText()
{
	// View transformation
	gGraphicsDevice->SetTransform(D3DTS_VIEW, &gOrthoViewMatrix);
	
	// Perspective transformation
	gGraphicsDevice->SetTransform(D3DTS_PROJECTION, &gScreenProjectionMatrix);

	// Render the texture
	gGraphicsDevice->BeginScene();
	{
		// render the user list
		size_t index = 0;
		int y = kFontSize / 4;
		for (auto iter=gChatUsers.rbegin(); iter != gChatUsers.rend() && index < gMaxChatLines; ++iter)
		{
			std::shared_ptr<ChatUser> user = *iter;
			RenderString(gWindowWidth-kUserListWidth, y, user->nameColorARGB, *user->font, user->username);

			y += user->font->pixelHeight + kLineSpacing;
			index++;
		}

		y = kFontSize / 4;

		// render the input text if showing
		if (gInputText != nullptr)
		{
			int x = RenderChatLine(kMessagesLeft, y, gInputText, KWhiteColor) + 2;
			RenderString(x, y, KWhiteColor, gNormalFont, "_");
			y += gInputText->font->pixelHeight + kLineSpacing;
		}

		// render the history
		for (auto iter=gChatLines.rbegin(); iter != gChatLines.rend(); ++iter)
		{
			std::shared_ptr<ChatLine> line = *iter;
			RenderChatLine(kMessagesLeft, y, line, KBlackColor);

			y += line->font->pixelHeight + kLineSpacing;
		}

		gGraphicsDevice->SetTexture(0, nullptr);
	}
	gGraphicsDevice->EndScene();
}


void DeinitChatRenderer()
{
	for (size_t i=0; i<gEmoticonTextures.size(); ++i)
	{
		SAFE_RELEASE(gEmoticonTextures[i].texture);
	}
	gEmoticonTextures.clear();

	ClearChatMessages();
	ClearChatUsers();

	SAFE_RELEASE(gTextureQuadVertexBuffer);
	SAFE_RELEASE(gNormalFont.texture);
	SAFE_RELEASE(gBoldFont.texture);
}
