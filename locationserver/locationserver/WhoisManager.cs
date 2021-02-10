using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace locationserver
{
    public class WhoisManager
    {
        public byte[] CreateResponse(string request, Dictionary<string,string> locationDict, ref string action)
        {
            if (request.Contains(' '))
            {
                string[] split = request.Split(' ');
                string location = "";
                for (int i = 1; i < split.Length; i++)
                {
                    location += split[i];
                    if (i != split.Length - 1) { location += ' '; }
                }
                if (locationDict.ContainsKey(split[0]))
                {
                    locationDict[split[0]] = location;
                }
                else
                {
                    locationDict.Add(split[0], location);
                }
                //action = "\"POST " + split[0] + '"' + " OK";
                return Encoding.ASCII.GetBytes("OK\r\n");
            }
            else
            {
                try
                {
                    //StringBuilder sb = new StringBuilder(30);
                    string name = request.Substring(0, request.Length - 2);
                    char[] response = new char[locationDict[name].Length + 2];
                    for (int i = 0; i < response.Length-2; i++) { response[i] = locationDict[name][i]; }
                    response[response.Length - 2] = '\r';
                    response[response.Length - 1] = '\n';
                    //action = "\"GET " + name + '"' + " Sent: " + locationDict[name];
                    return Encoding.ASCII.GetBytes(response);
                }
                catch
                {
                    //action = "\"GET " + request.Substring(0, request.Length - 2) + '"' + " Sent: ERROR: no entries found";
                    return Encoding.ASCII.GetBytes("ERROR: no entries found\r\n");
                }
            }
        }
    }
}
