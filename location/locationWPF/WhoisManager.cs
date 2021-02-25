using System;
using System.Net.Sockets;


namespace locationWPF
{
    public static class WhoisManager
    {
        private static string person;
        private static string location = null;
        private static bool bPOST = false;
        public static string CreateRequest(string person, string location)
        {
            WhoisManager.person = person;
            WhoisManager.location = location;
            if (location == null) { return person + "\r\n"; }
            else { bPOST = true; return person + ' ' + location + "\r\n"; }
        }

        public static string HandleResponse(NetworkStream ns)
        {      
            int bytes;
            string response = "";
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            while ((bytes = ns.ReadByte()) != -1)
            { s.Append((char)bytes); }
            response = s.ToString();
            if (bPOST && response.Equals("OK\r\n")) { return person + " location changed to be " + location + "\r\n"; }
            else if (response.Equals("ERROR: no entries found\r\n")) { return response; }
            else { return person + " is " + response; }
        }
    }
}
