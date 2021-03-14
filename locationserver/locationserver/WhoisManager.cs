using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace locationserver
{
    public class WhoisManager
    {
        public byte[] CreateResponse(string request, ConcurrentDictionary<string, string> locationDict, ref string action)
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
                locationDict.AddOrUpdate(split[0], location, (key,oldValue)=>location);
                action = "\"POST " + split[0] + '"' + " OK";
                return Encoding.ASCII.GetBytes("OK\r\n");
            }
            else
            {
                string locationElement = "";
                try
                {
                    //StringBuilder sb = new StringBuilder(30);
                    string name = request.Substring(0, request.Length - 2);
                    bool wasPresent = locationDict.TryGetValue(name, out locationElement);
                    if (!wasPresent)
                    {
                        action = "\"GET " + request + '"' + " Sent: ERROR: no entries found";
                        return Encoding.ASCII.GetBytes("ERROR: no entries found\r\n");
                    }

                    char[] response = new char[locationElement.Length + 2];
                    for (int i = 0; i < response.Length - 2; i++) 
                    {
                        response[i] = locationElement[i];               
                    }
                    response[response.Length - 2] = '\r';
                    response[response.Length - 1] = '\n';
                    action = "\"GET " + name + '"' + " Sent: " + locationElement;
                    return Encoding.ASCII.GetBytes(response);
                }
                catch
                {
                    action = "\"GET " + request + '"' + " Sent: ERROR: no entries found";
                    return Encoding.ASCII.GetBytes("ERROR: no entries found\r\n");
                }
            }
        }
    }
}
