using System;
using System.Net.Sockets;

namespace locationWPF
{
    public static class HttpManager
    {
        private static Protocol protocol;
        private static string person;
        private static string location;
        private static bool bPOST = false;

        public static string CreateRequest(Protocol protocol, string person, string location, string address)
        {
            HttpManager.protocol = protocol;
            HttpManager.person = person;
            HttpManager.location = location;
            switch (protocol)
            {
                case Protocol.HTTP9:
                    if (string.IsNullOrEmpty(location)) { return "GET /" + person + "\r\n"; }
                    else { bPOST = true; return "PUT /" + person + "\r\n\r\n" + location + "\r\n"; }
                case Protocol.HTTP0:
                    if (string.IsNullOrEmpty(location)) { return "GET /?" + person + " HTTP/1.0" + "\r\n\r\n"; }
                    else { bPOST = true; return "POST /" + person + " HTTP/1.0" + "\r\nContent-Length: " + location.Length + "\r\n\r\n" + location; }
                case Protocol.HTTP1:
                    if (string.IsNullOrEmpty(location)) { return "GET /?name=" + person + " HTTP/1.1\r\nHost: " + address + "\r\n\r\n"; }
                    else
                    {
                        string message = "name=" + person + "&location=" + location;
                        bPOST = true; return "POST / HTTP/1.1\r\nHost: " + address + "\r\nContent-Length: " + message.Length + "\r\n\r\n" + message;
                    }
            }

            return "";
        }

        public static string HandleResponse(NetworkStream ns)
        {
            switch (protocol)
            {
                case Protocol.HTTP9:
                case Protocol.HTTP0:
                    return HandleHTTP90(ns);
                case Protocol.HTTP1:
                    return HandleHTTP1(ns);
            }
            return "";
        }

        private static string HandleHTTP1(NetworkStream ns)
        {
            int bytes, numberOfBytes = 0;
            string header = "", body = "";
            while ((bytes = ns.ReadByte()) != -1)
            {
                header += (char)bytes;
                if (header.Contains("\r\n\r\n"))
                    break;
            }
            string[] headerArgs = header.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < headerArgs.Length; i++)
            {
                if (headerArgs[i].Contains("Content-Length"))
                {
                    numberOfBytes = int.Parse(headerArgs[i].Split(' ')[1]);
                    break;
                }
            }
            if (headerArgs[0].Contains("404")) { return header; }
            if (numberOfBytes > 0)
            {
                for (int i = 0; i < numberOfBytes; i++)
                {
                    body += (char)ns.ReadByte();
                }
                body += "\r\n";
            }
            else
            {
                if (bPOST) { return person + " location changed to be " + location; }
                while (true)
                {
                    byte[] bodyBytes = new byte[1024];
                    int bytesRead = ns.Read(bodyBytes, 0, bodyBytes.Length);
                    body += System.Text.Encoding.ASCII.GetString(bodyBytes).Replace("\0", "");
                    if (bytesRead < bodyBytes.Length && !ns.DataAvailable) { break; }
                }
            }
            return person + " is " + body;
        }

        private static string HandleHTTP90(NetworkStream ns)
        {
            int bytes;
            string response = "";
            while ((bytes = ns.ReadByte()) != -1)
            {
                response += (char)bytes;
            }
            string[] split = response.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (split[0].Contains("200") && split.Length > 1) { return person + " is " + split[1]; }
            else if (split[0].Contains("200")) { return person + " location changed to be " + location + "\r\n"; }
            else if (split[0].Contains("404")) { return response; }
            return "NULL";
        }
    }
}
