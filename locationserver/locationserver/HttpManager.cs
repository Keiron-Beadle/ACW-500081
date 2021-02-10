﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace locationserver
{
    public class HttpManager
    {
        public byte[] CreateResponse(Protocol protocol, string request, Dictionary<string, string> locationDict, ref string action)
        {
            StringBuilder sb = new StringBuilder(32);
            switch (protocol)
            {
                case Protocol.HTTP9:
                    return HandleHTTP9(request, locationDict, ref action, sb);
                case Protocol.HTTP0:
                    return HandleHTTP0(request, locationDict, ref action, sb);
                case Protocol.HTTP1:
                    return HandleHTTP1(request, locationDict, ref action, sb);
            }
            return new byte[2];
        }

        private byte[] HandleHTTP1(string request, Dictionary<string, string> locationDict, ref string action, StringBuilder sb)
        {
            if (request.Substring(0, 3) == "GET")
            {
                string name = request.Split('=')[1].Split(' ')[0];
                if (locationDict.ContainsKey(name))
                {
                    sb.Append("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                    sb.Append(locationDict[name]);
                    sb.Append("\r\n");
                    //action = "\"GET " + name + '"' + " Sent: " + locationDict[name];
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }
                else
                {
                    //sb.Append("HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                    //action = "\"GET " + name + '"' + " Sent: ERROR";
                    return Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                }
            }
            else if (request.Substring(0, 4) == "POST")
            {
                string[] split = request.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string header = split[0];
                string body = split[1];
                string name = body.Split('=')[1].Split('&')[0];
                string location = body.Split('=')[2];
                if (locationDict.ContainsKey(name))
                {
                    locationDict[name] = location;
                }
                else
                {
                    locationDict.Add(name, location);
                }
                //action = "\"POST " + name + ' ' + location + '"' + " Sent: OK";
                //sb.Append("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                return Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n");
            }

            return new byte[2];
        }

        private byte[] HandleHTTP0(string request, Dictionary<string, string> locationDict, ref string action, StringBuilder sb)
        {
            if (request.Substring(0, 3) == "GET")
            {
                string name = request.Split(' ')[1];
                name = name.Substring(2, name.Length - 2);
                if (locationDict.ContainsKey(name))
                {
                    sb.Append("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                    sb.Append(locationDict[name]);
                    sb.Append("\r\n");
                    //action = "\"GET " + name + '"' + " Sent: " + locationDict[name];
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }
                else
                {
                    //string response = "HTTP/1.0 404 Not Found\r\nContent-Type: text/plain\r\n\r\n";
                    //action = "\"GET " + name + '"' + " Sent: ERROR";
                    return Encoding.ASCII.GetBytes("HTTP/1.0 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                }
            }
            else if (request.Substring(0, 4) == "POST")
            {
                string[] split = request.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string location = split[1];
                string name = split[0].Split('/')[1].Split(' ')[0];
                if (locationDict.ContainsKey(name))
                {
                    locationDict[name] = location;
                }
                else
                {
                    locationDict.Add(name, location);
                }
                //string response = "HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n";
                //action = "\"POST " + name + ' ' + location + '"' + " Sent: OK";
                return Encoding.ASCII.GetBytes("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n");
            }

            return new byte[2];
        }

        private byte[] HandleHTTP9(string request, Dictionary<string, string> locationDict, ref string action, StringBuilder sb)
        {
            if (request.Substring(0, 3) == "GET")
            {
                int index = request.IndexOf('/');
                string name = request.Remove(0, index+1);
                //string name = request.('/')[1];
                name = name.Substring(0, name.Length - 2);
                if (locationDict.ContainsKey(name))
                {
                    sb.Append("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                    sb.Append(locationDict[name]);
                    sb.Append("\r\n");
                    //action = "\"GET " + name + '"' + " Sent: " + locationDict[name];
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }
                else
                {
                    //string response = "HTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n\r\n";
                    //action = "\"GET " + name + '"' + " Sent: ERROR";
                    return Encoding.ASCII.GetBytes("HTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                }
                
            }
            else if (request.Substring(0, 3) == "PUT")
            {
                string[] split = request.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string name = split[0].Split('/')[1];
                string location = split[1];
                if (locationDict.ContainsKey(name))
                {
                    locationDict[name] = location;
                }
                else
                {
                    locationDict.Add(name, location);
                }
                //string response = "HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n";
                //action = "\"POST " + name + ' ' + location + '"' + " Sent: OK";
                return Encoding.ASCII.GetBytes("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n");
            }

            return new byte[2];
        }
    }
}