// telegraph.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <conio.h>
#include <assert.h>
#include <boost/asio.hpp>
#include <boost/array.hpp>
#include <boost/algorithm/string.hpp>
#include <boost/algorithm/string/split.hpp>

#define SUBSCRIBED_MSG		"SUBSCRIBED"
#define VIEWER_COUNT_MSG	"viewer_count"
#define UP_MSG				"up"
#define DOWN_MSG			"down"
#define COMMERCIAL_MSG		"commercial"

#define TYPE_PROP			"type"
#define SERVER_TIME_PROP	"server_time"
#define PLAY_DELAY_PROP		"play_delay"
#define VIEWERS_PROP		"viewers"
#define LENGTH_PROP			"length"


struct ChannelInfo
{
	std::string channelName;
	std::string type;
	unsigned int numViewers;
	unsigned int play_delay;
	unsigned int commercial_length;
	float server_time;
	bool currentlyUp;
};


ChannelInfo gChannelInfo;
std::string gBufferedData;
char gReadBuffer[256];
bool gSubscribed = false;


// Async callback from the socket when data arrives - only flushed during call to socket.get_io_service().run();
void ReadHandler(const boost::system::error_code& err, std::size_t bytes_transferred)
{
	if (err)
	{
		return;
	}

	gBufferedData.append(gReadBuffer, bytes_transferred);
}

static std::vector<std::string> gMessage;
static int gMessageBodyLength = -1;


// Attempt to receive a full message and return true if received message, false otherwise.
bool ReceiveMessage(boost::asio::ip::tcp::socket& socket, std::string& type, std::string& body)
{
	// Message format:
	//   1- Message header in form /<user>.<property>\n
	//   2- Hexadecimal number encoded in ASCII followed by \n which denotes how many bytes are in the message body.
	//   3- Body of the message, followed by \n\0.

	// NOTE: The very first message we receive is simply 
	//     SUBSCRIBED\0

	// grab some data from the socket - we can't assume a full message comes in during a single read nor will there only be one message in a read
	socket.async_read_some(boost::asio::buffer(gReadBuffer, sizeof(gReadBuffer)), ReadHandler);

	// flushes the socket data callback
	socket.get_io_service().run();

	// determine the message type and body size if not known yet
	if (gMessageBodyLength < 0)
	{
		size_t index = 0;
		for (; index<gBufferedData.length(); ++index)
		{
			// found the end of a line
			if (gBufferedData[index] == '\0' || gBufferedData[index] == '\n')
			{
				std::string token = gBufferedData.substr(0, index);
				boost::trim(token);

				if (token != "")
				{
					gMessage.push_back(token);
				}

				gBufferedData.erase(0, index+1);
				index = 0;

				switch (gMessage.size())
				{
					case 0:
					{
						break;
					}
					case 1:
					{
						// special initial message
						if (gMessage.back() == SUBSCRIBED_MSG)
						{
							type = gMessage.back();
							body = "";

							gMessage.clear();
							gMessageBodyLength = -1;
							return true;
						}
						break;
					}
					case 2:
					{
						// determine the message body length
						sscanf(gMessage[1].c_str(), "%x", &gMessageBodyLength);
						assert(gMessageBodyLength >= 0);

						// exit the loop
						index = gBufferedData.length();
						break;
					}
					default:
					{
						assert(false);
						break;
					}
				}
			}
		}
	}

	// finding the end of the body
	if (gMessageBodyLength >= 0)
	{
		assert(gMessage.size() == 2);

		// enough data read for entire body
		if (gBufferedData.length() >= static_cast<size_t>(gMessageBodyLength))
		{
			type = gMessage[0];
			body = gBufferedData.substr(0, gMessageBodyLength);

			gBufferedData.erase(0, gMessageBodyLength+2); // extra 2 to get rid of the \n\0 following the body if it's there

			gMessage.clear();
			gMessageBodyLength = -1;
			return true;
		}
	}

	// no full message received yet
	type = "";
	body = "";

	return false;
}


// Helper to get the key and value from a property.
void ParseProperty(const std::string& prop, std::string& key, std::string& val)
{
	std::vector<std::string> kv;
	boost::split(kv, prop, boost::is_any_of("="), boost::token_compress_on);
	kv.erase(std::remove_if(kv.begin(), kv.end(), [](const std::string& s){ return s.length() == 0; }), kv.end()); 

	assert(kv.size() == 2);

	if (kv.size() == 2)
	{
		key = kv[0];
		val = kv[1];
	}
	else
	{
		key = "";
		val = "";
	}
}


// Interprets a message from the server.
void ParseMessage(const std::string& type, const std::string& body)
{
	if (type == SUBSCRIBED_MSG)
	{
		gSubscribed = true;
		return;
	}

	// Process a standard message of the form
	//    /<channel_name>.<property>\n
	std::string prefix("/");
	prefix += gChannelInfo.channelName + ".";

	// make sure it's for the channel we expect
	if (type.substr(0, prefix.length()) != prefix)
	{
		// unknown message
		assert(false);
		return;
	}

	// isolate the message type
	std::string shortType = type.substr(prefix.length());

	// find all the properties
	std::vector<std::string> props;
	boost::split(props, body, boost::is_any_of("&"), boost::token_compress_on);
	props.erase(std::remove_if(props.begin(), props.end(), [](const std::string& s){ return s.length() == 0; }), props.end()); 

	for (size_t p=0; p<props.size(); ++p)
	{
		std::string key;
		std::string val;
		char buffer[128];

		ParseProperty(props[p], key, val);
				
		if (key == TYPE_PROP)
		{
			sscanf(val.c_str(), "%s", buffer);
			gChannelInfo.type = buffer;
		}
		else if (key == SERVER_TIME_PROP)
		{
			sscanf(val.c_str(), "%f", &gChannelInfo.server_time);
		}
		else if (key == PLAY_DELAY_PROP)
		{
			sscanf(val.c_str(), "%u", &gChannelInfo.play_delay);
		}
		else if (key == VIEWERS_PROP)
		{
			sscanf(val.c_str(), "%u", &gChannelInfo.numViewers);
		}
		else if (key == LENGTH_PROP)
		{
			sscanf(val.c_str(), "%u", &gChannelInfo.commercial_length);
		}
		else
		{
			// unhandled key
			//assert(false);
		}
	}

	// NOTE: you may want to fire an event based on the some change here

	// a change in viewer count for the stream
	if (shortType == VIEWER_COUNT_MSG)
	{
		// the stream is implicitly marked as up since we're receiving updates for it
		gChannelInfo.currentlyUp = true;

		// expected properties: server_time, viewers

		// Example message:

		// /channel_name.viewer_count
		// 23
		// server_time=1364339424.45&viewers=4756
	}
	// stream just came up
	else if (shortType == UP_MSG)
	{
		gChannelInfo.currentlyUp = true;

		// expected properties: play_delay, type, server_time

		// Example message:

		// /channel_name.up
		// 30
		// play_delay=0&type=live&server_time=1364339425.12
	}
	// stream just went down
	else if (shortType == DOWN_MSG)
	{
		gChannelInfo.currentlyUp = false;

		// expected properties: type, server_time

		// Example message:

		// /channel_name.down
		// 23
		// type=live&server_time=1364339449.34
	}
	// play a commercial
	else if (shortType == COMMERCIAL_MSG)
	{
		// expected properties: length, server_time

		// Example message:

		// /thftester2.commercial
		// 23
		// length=30&server_time=1364345336.24
	}
	else
	{
		// ignore all other messages
	}
}


int _tmain(int argc, _TCHAR* argv[])
{
	gChannelInfo.channelName = "<channelName>"; // NOTE: put the channel name here
	gChannelInfo.numViewers = 0;
	gChannelInfo.server_time = 0;
	gChannelInfo.currentlyUp = false;
	gChannelInfo.play_delay = 0;
	gChannelInfo.type = "";

	memset(gReadBuffer, 0, sizeof(gReadBuffer));

	// be sure to set the name of the channel you're interested in
	assert(gChannelInfo.channelName != "");

	boost::asio::io_service io_service;

	// resolve the twitch telegraph host
	boost::asio::ip::tcp::resolver resolver(io_service);
	boost::asio::ip::tcp::resolver::query query("<host here>", "443");
	boost::asio::ip::tcp::resolver::iterator endpoint_iterator = resolver.resolve(query);
	boost::asio::ip::tcp::resolver::iterator end;

	// connect to the telegraph host
	boost::asio::ip::tcp::socket consumer_socket(io_service);

	boost::system::error_code err = boost::asio::error::host_not_found;
	while (err && endpoint_iterator != end)
	{
		consumer_socket.close();
		consumer_socket.connect(*endpoint_iterator++, err);
	}

	// subscribe to the channel - send a message of the format
	//    SUBSCRIBE /<channel_name>. TELEGRAPH/0.1\0\n
	char buffer[128];
	sprintf(buffer, "SUBSCRIBE /%s. TELEGRAPH/0.1\0\n", gChannelInfo.channelName.c_str());
	consumer_socket.write_some(boost::asio::buffer(buffer), err);

	// process info from the channel until space bar pressed
	for (;;)
	{
		std::string type, body;
		if ( ReceiveMessage(consumer_socket, type, body) )
		{
			printf("%s %s\n", type.c_str(), body.c_str());

			ParseMessage(type, body);
		}

		if (_kbhit() && _getch() == ' ')
		{
			break;
		}
	}

	return 0;
}
