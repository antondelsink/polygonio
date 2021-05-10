namespace PolygonIo.Demos
{
    public class PolygonResponseMessage
    {
        public string Original { get; set; }

        public bool IsStatusMessage => Original.Contains(@"""ev"":""status""");

        public bool IsSubscribedSuccessfullyStatusMessage => Original.Contains(@"""ev"":""status"",""status"":""success"",""message"":""subscribed to:");

        public bool IsAuthenticatedSuccessfullyStatusMessage => Original.Contains(@"""ev"":""status"",""status"":""auth_success"",""message"":""authenticated""");

        public bool IsConnectedSuccessfullyStatusMessage => Original.Contains(@"""ev"":""status"",""status"":""connected"",""message"":""Connected Successfully""");

        public string EV
        {
            get
            {
                var token = @"""ev"":""";
                IndexOfValue(token, out int ixValueStart, out int lenValue);
                return Original.Substring(ixValueStart, lenValue);
            }
        }
        public string Status
        {
            get
            {
                var token = @"""status"":""";
                IndexOfValue(token, out int ixValueStart, out int lenValue);
                return Original.Substring(ixValueStart, lenValue);
            }
        }
        public string Message
        {
            get
            {
                var token = @"""message"":""";
                IndexOfValue(token, out int ixValueStart, out int lenValue);
                return Original.Substring(ixValueStart, lenValue);
            }
        }
        public PolygonResponseMessage(string message)
        {
            Original = message;
        }

        private void IndexOfValue(string token, out int ixValueStart, out int lenValue)
        {
            var ixToken = Original.IndexOf(token);
            ixValueStart = ixToken + token.Length;
            var ixValueEnd = Original.IndexOf(@"""", ixValueStart);
            lenValue = ixValueEnd - ixValueStart;
        }

        public override string ToString()
        {
            return this.Original;
        }
    }
}
