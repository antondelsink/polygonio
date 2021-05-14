using System;
using System.Runtime.InteropServices;

namespace PolygonIo.Demos
{
    /// <summary>
    /// Fixed Length String allocated on the stack as a struct consisting of a total of 16 bytes representing up to 12 characters as bytes. Simple cast to Char from Byte and Byte to Char (no Encoding).
    /// </summary>
    /// <remarks>
    /// sizeof(StringStructMax12) == 16 == 1 byte for Length + 12 bytes of characters + 3 bytes of padding
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct StringStructMax12
    {
        private readonly byte _Length;
        private readonly byte B00;
        private readonly byte B01;
        private readonly byte B02;
        private readonly byte B03;
        private readonly byte B04;
        private readonly byte B05;
        private readonly byte B06;
        private readonly byte B07;
        private readonly byte B08;
        private readonly byte B09;
        private readonly byte B10;
        private readonly byte B11;
        private readonly byte Reserved00;
        private readonly byte Reserved01;
        private readonly byte Reserved02;

        public StringStructMax12(string s)
        {
            if (s is null)
                throw new ArgumentNullException();

            if (s.Length > 12)
                throw new ArgumentOutOfRangeException(nameof(s), $"Specified string is too long. Maximum length is 12. Provided string is length {s.Length}.");

            _Length = (byte)s.Length;
            B00 = (s.Length > 00) ? (byte)s[00] : (byte)0;
            B01 = (s.Length > 01) ? (byte)s[01] : (byte)0;
            B02 = (s.Length > 02) ? (byte)s[02] : (byte)0;
            B03 = (s.Length > 03) ? (byte)s[03] : (byte)0;
            B04 = (s.Length > 04) ? (byte)s[04] : (byte)0;
            B05 = (s.Length > 05) ? (byte)s[05] : (byte)0;
            B06 = (s.Length > 06) ? (byte)s[06] : (byte)0;
            B07 = (s.Length > 07) ? (byte)s[07] : (byte)0;
            B08 = (s.Length > 08) ? (byte)s[08] : (byte)0;
            B09 = (s.Length > 09) ? (byte)s[09] : (byte)0;
            B10 = (s.Length > 10) ? (byte)s[10] : (byte)0;
            B11 = (s.Length > 11) ? (byte)s[11] : (byte)0;

            Reserved00 = Reserved01 = Reserved02 = 0;
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 00: return (_Length > 00) ? B00 : throw new IndexOutOfRangeException();
                    case 01: return (_Length > 01) ? B01 : throw new IndexOutOfRangeException();
                    case 02: return (_Length > 02) ? B02 : throw new IndexOutOfRangeException();
                    case 03: return (_Length > 03) ? B03 : throw new IndexOutOfRangeException();
                    case 04: return (_Length > 04) ? B04 : throw new IndexOutOfRangeException();
                    case 05: return (_Length > 05) ? B05 : throw new IndexOutOfRangeException();
                    case 06: return (_Length > 06) ? B06 : throw new IndexOutOfRangeException();
                    case 07: return (_Length > 07) ? B07 : throw new IndexOutOfRangeException();
                    case 08: return (_Length > 08) ? B08 : throw new IndexOutOfRangeException();
                    case 09: return (_Length > 09) ? B09 : throw new IndexOutOfRangeException();
                    case 10: return (_Length > 10) ? B10 : throw new IndexOutOfRangeException();
                    case 11: return (_Length > 11) ? B11 : throw new IndexOutOfRangeException();
                    default:
                        throw new IndexOutOfRangeException($"Valid values are 0 to 12. Specified value was {index}.");
                }
            }
        }

        public override string ToString()
        {
            var chars = new char[] { (char)B00, (char)B01, (char)B02, (char)B03, (char)B04, (char)B05, (char)B06, (char)B07, (char)B08, (char)B09, (char)B10, (char)B11 };
            return new string(chars.AsSpan().Slice(0, _Length));
        }

        public static implicit operator string(StringStructMax12 s12) => s12.ToString();

        public static explicit operator StringStructMax12(string s)
        {
            return new StringStructMax12(s);
        }

        public int Length => _Length;
    }
}