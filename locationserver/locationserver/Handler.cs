using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace locationserver
{
    class RequestHandler 
    {
        private Protocol currentProtocol = Protocol.Whois;

        public void AcceptClient(Socket connection)
        {
            NetworkStream socketStream;
            Console.WriteLine("Connection accepted...");
            socketStream = new NetworkStream(connection);
            socketStream.ReadTimeout = locationserver.timeout;
            string request = ReadRequest(socketStream);
            if (locationserver.bDebug)
                Console.WriteLine("Request recevied from " + connection.RemoteEndPoint + "...");
            else
                Console.WriteLine("Request received...");

            byte[] response;
            string action = "";
            if (currentProtocol == Protocol.Whois)
            {
                WhoisManager manager = new WhoisManager();
                response = manager.CreateResponse(request, locationserver.locationDict, ref action);
            }
            else
            {
                HttpManager manager = new HttpManager();
                response = manager.CreateResponse(currentProtocol, request, locationserver.locationDict, ref action);
            }

            socketStream.Write(response, 0, response.Length);
            socketStream.Flush();
            if (locationserver.bDebug)
            {
                Console.WriteLine("Responded: " + action + " to " + connection.RemoteEndPoint);
            }
            Random rnd = new Random();
            bool successfulWrite = false;
            if (locationserver.bLog && locationserver.bHasLogWritePerm)
            {
                do
                {
                    string dataEntry = connection.RemoteEndPoint + " - - [" + DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss +zzz") + "] " + action;
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(locationserver.logDir, true))
                        {
                            sw.WriteLine(dataEntry);
                            successfulWrite = true;
                        }
                    }
                    catch
                    {
                        Thread.Sleep(rnd.Next(50, 500));
                    }
                } while (!successfulWrite);

            }

            socketStream.Close();
            connection.Close();
        }

        private string ReadRequest(NetworkStream ns)
        {
            int bytes;
            StringBuilder requestSB = new StringBuilder();
            byte[] data;
            while (true)
            {
                data = new byte[512];
                try { bytes = ns.Read(data, 0, data.Length); }
                catch (IOException) { break; }
              
                requestSB.Append(Encoding.ASCII.GetString(data));
                if (bytes < 512) { break; }
            }
            requestSB.Replace("\0", "");
            string request = requestSB.ToString();
            DeduceProtocol(request);
            if (currentProtocol == Protocol.HTTP1)
            {
                string[] headerArgs = request.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < headerArgs.Length; i++)
                {
                    if (headerArgs[i].Substring(0,3) == "GET")
                    {
                        return headerArgs[i];
                    }
                }
            }
            return request;
        }

        private void DeduceProtocol(string request)
        {
            bool chosen = false;
            try
            {
                Regex getCheck = new Regex(@"GET \/([!-~])+(\r|\n|\r\n)");
                Regex putCheck = new Regex(@"PUT \/([!-~])+(\r|\n|\r\n){2}([!-~]| )+(\r|\n|\r\n)");
                if (getCheck.Matches(request).Count > 0 || putCheck.Matches(request).Count > 0)
                    currentProtocol = Protocol.HTTP9; chosen = true;
            }
            catch { currentProtocol = Protocol.Whois; return; }
            string[] splitRequest = request.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (splitRequest[0].Contains("HTTP/1.0")) 
            {
                Regex getCheck = new Regex(@"GET \/\?([!-~])+ HTTP\/1.0(\r|\n|\r\n){2}");
                Regex postCheck = new Regex(@"POST \/([!-~])+ HTTP\/1.0(\r|\n|\r\n)Content-Length: ([0-9])+(\r|\n|\r\n){2}([!-~]| )+");
                if (getCheck.Matches(request).Count > 0 || postCheck.Matches(request).Count > 0)
                    currentProtocol = Protocol.HTTP0; return;
            }
            else if (splitRequest[0].Contains("HTTP/1.1"))
            {
                Regex getCheck = new Regex(@"GET \/\?name=([!-~])+ HTTP\/1.1(\r|\n|\r\n)");
                Regex postCheck = new Regex(@"POST \/ HTTP\/1.1(\r|\n|\r\n)Host: ([!-~])+(\r|\n|\r\n)Content-Length: ([0-9])+(\r|\n|\r\n){2}name=([!-~])+&location=([!-~]| )+");
                if (getCheck.Matches(request).Count > 0 || postCheck.Matches(request).Count > 0)
                    currentProtocol = Protocol.HTTP1; return; 
            }
            if (!chosen) { currentProtocol = Protocol.Whois; }
        }
    }
}

