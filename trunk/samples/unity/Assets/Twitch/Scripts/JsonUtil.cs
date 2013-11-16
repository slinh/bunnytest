using System;
using System.Collections.Generic;
using LitJson;

namespace Twitch
{
	public class JsonUtil
	{
	    #region Serialization
	
	    public static void WritePropertyPair(JsonWriter writer, string key, string val)
	    {
	        writer.WritePropertyName(key);
	        writer.Write(val);
	    }
	    public static void WritePropertyPair(JsonWriter writer, string key, int val)
	    {
	        writer.WritePropertyName(key);
	        writer.Write(val);
	    }
	    public static void WritePropertyPair(JsonWriter writer, string key, UInt64 val)
	    {
	        writer.WritePropertyName(key);
	        writer.Write(val);
	    }
	    public static void WritePropertyPair(JsonWriter writer, string key, float val)
	    {
	        writer.WritePropertyName(key);
	        writer.Write(val);
	    }
	    public static void WritePropertyPair(JsonWriter writer, string key, bool val)
	    {
	        writer.WritePropertyName(key);
	        writer.Write(val);
	    }
	
	    #endregion
	
	    #region Deserialization
	
	    public delegate void BeginObjectDelegate(object context);
	    public delegate void EndObjectDelegate(object context);
	    public delegate void ParserDelegate(JsonReader reader, object context);
		
		protected static Dictionary<string, string> s_FlatObjectDictionary = null;
		
		public static void ReadFlatObjectIntoDictionary(JsonReader reader, Dictionary<string, string> dict)
		{
			s_FlatObjectDictionary = dict;
			ReadObject(reader, null, ReadValueIntoDictionary);
			s_FlatObjectDictionary = null;
		}
		
		protected static void ReadValueIntoDictionary(JsonReader reader, object context)
		{
	        switch (reader.Token)
	        {
	            case JsonToken.PropertyName:
	            {
					string key = reader.Value.ToString();
					string val = JsonUtil.ReadStringPropertyRHS(reader);
					s_FlatObjectDictionary[key] = val;
	                break;
	            }
	            default:
	            {
					// silently ignore for now
	                //throw new Exception("Not a flat JSON object");
					break;
	            }
	        }
		}
		
	    public static void ReadObject(JsonReader reader, object context, BeginObjectDelegate begin, ParserDelegate parser, EndObjectDelegate end)
	    {
	        System.Diagnostics.Debug.Assert(reader.Token == JsonToken.ObjectStart);
	
	        if (begin != null)
	        {
	            begin(context);
	        }
	
	        // parse the properties
	        reader.Read();
	        while (reader.Token != JsonToken.ObjectEnd)
	        {
	            parser(reader, context);
	            reader.Read();
	        }
	
	        if (end != null)
	        {
	            end(context);
	        }
	    }
	
	    public static void ReadObject(JsonReader reader, object context, ParserDelegate parser)
	    {
	        ReadObject(reader, context, null, parser, null);
	    }
	
	    public static void ReadArray(JsonReader reader, object context, BeginObjectDelegate begin, ParserDelegate parser, EndObjectDelegate end)
	    {
	        System.Diagnostics.Debug.Assert(reader.Token == JsonToken.ArrayStart);
	
	        // parse the elements
	        reader.Read();
	        while (reader.Token != JsonToken.ArrayEnd)
	        {
	            if (reader.Token == JsonToken.ObjectStart)
	            {
	                ReadObject(reader, context, begin, parser, end);
	            }
	            else if (reader.Token == JsonToken.ArrayStart)
	            {
	                ReadArray(reader, context, parser);
	            }
	            else
	            {
	                parser(reader, context);
	            }
	
	            reader.Read();
	        }
	    }
	
	    public static void ReadArray(JsonReader reader, object context, ParserDelegate parser)
	    {
	        ReadArray(reader, context, null, parser, null);
	    }
	
	    public static string ReadStringPropertyRHS(JsonReader reader)
	    {
	        reader.Read();
	        return (string)reader.Value;
	    }
	
	    public static int ReadIntPropertyRHS(JsonReader reader)
	    {
	        reader.Read();
	        return (int)reader.Value;
	    }
	
	    public static uint ReadUIntPropertyRHS(JsonReader reader)
	    {
	        reader.Read();
	        return (uint)(int)reader.Value;
	    }
	
	    public static float ReadFloatPropertyRHS(JsonReader reader)
	    {
	        reader.Read();
	        return (float)reader.Value;
	    }
	
	    public static UInt64 ReadUInt64PropertyRHS(JsonReader reader)
	    {
	        reader.Read();
	        return (UInt64)reader.Value;
	    }
	
	    public static bool ReadBoolPropertyRHS(JsonReader reader)
	    {
	        reader.Read();
	        return (bool)reader.Value;
	    }
	    
	    #endregion
	}
}
