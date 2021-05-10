using System;
using System.Text.Json;

namespace PolygonIo.Demos
{
    public static class Utf8JsonReaderUtils
    {
        public static bool TryGetUInt64(ref Utf8JsonReader reader, ref ulong number)
        {
            if (!reader.Read())
                return false;

            if (reader.TokenType != JsonTokenType.Number)
                return false;
            
            return reader.TryGetUInt64(out number);
        }

        public static bool TryGetFloat(ref Utf8JsonReader reader, ref float number)
        {
            if (!reader.Read())
                return false;

            if (reader.TokenType != JsonTokenType.Number)
                return false;

            return reader.TryGetSingle(out number);
        }
        
        public static bool TryGetUInt32(ref Utf8JsonReader reader, ref uint number)
        {
            if (!reader.Read())
                return false;

            if (reader.TokenType != JsonTokenType.Number)
                return false;

            return reader.TryGetUInt32(out number);
        }

        public static bool TryGetString(ref Utf8JsonReader reader, out string text)
        {
            text = null;

            if (!reader.Read())
                return false;

            if (reader.TokenType != JsonTokenType.String)
                return false;

            text = reader.GetString();
            return true;
        }

        public static bool AllRequiredFieldsAssigned(ReadOnlySpan<bool> flags)
        {
            for (int ix = 0; ix < flags.Length; ix++)
            {
                if (flags[ix] == false)
                    return false;
            }
            return true;
        }

    }
}
