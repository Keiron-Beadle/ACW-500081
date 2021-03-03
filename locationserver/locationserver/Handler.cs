using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace locationserver
{
    class RequestHandler
    {
        private Protocol currentProtocol = Protocol.Whois;
        private TextBox handleToUIOutput;

        public void AcceptClient(Socket connection, TextBox handleToOutput)
        {
            handleToUIOutput = handleToOutput;
            //System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            //s.Start();
            NetworkStream socketStream;
            Utility.UpdateUIText(handleToOutput,"Connection accepted...\r\n");
            socketStream = new NetworkStream(connection)
            {
                ReadTimeout = Utility.timeout
            };
            string request = ReadRequest(socketStream);
            if (Utility.bDebug)
                Utility.UpdateUIText(handleToOutput,"Request recevied from " + connection.RemoteEndPoint + "...\r\n");
            else
                Utility.UpdateUIText(handleToOutput,"Request received...\r\n");
            byte[] response;
            string action = "";
            if (currentProtocol == Protocol.Whois)
            {
                WhoisManager manager = new WhoisManager();
                response = manager.CreateResponse(request, Utility.locationDict, ref action);
            }
            else
            {
                HttpManager manager = new HttpManager();
                response = manager.CreateResponse(currentProtocol, request, Utility.locationDict, ref action);
            }
            socketStream.Write(response, 0, response.Length);
            //s.Stop();
            //Console.WriteLine("pre flush: " + s.ElapsedMilliseconds + "ms / " + s.ElapsedTicks + " ticks");
            socketStream.Flush();
            if (Utility.bDebug)
            {
                string responseStr = Encoding.ASCII.GetString(response);
                responseStr = responseStr.Substring(0, response.Length - 4);
                Utility.UpdateUIText(handleToOutput,"Responded: " + responseStr + " to " + connection.RemoteEndPoint + "\r\n");
            }
            Random rnd = new Random();
            bool successfulWrite = false;
            if (Utility.bLog && Utility.bHasLogWritePerm)
            {
                do
                {
                    string dataEntry = connection.RemoteEndPoint + " - - [" + DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss +zzz") + "] " + action;
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(Utility.logDir, true))
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

        public void AcceptClient(Socket connection)
        {
            //System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            //s.Start();
            NetworkStream socketStream;
            Console.WriteLine("Connection accepted...");
            socketStream = new NetworkStream(connection)
            {
                ReadTimeout = Utility.timeout
            };
            string request = ReadRequest(socketStream);
            if (Utility.bDebug)
                Console.WriteLine("Request recevied from " + connection.RemoteEndPoint + "...");
            else
                Console.WriteLine("Request received...");
            //Console.WriteLine(request.Replace('\r', '0').Replace('\n', '0'));
            byte[] response;
            string action = "";
            if (currentProtocol == Protocol.Whois)
            {
                WhoisManager manager = new WhoisManager();
                response = manager.CreateResponse(request, Utility.locationDict, ref action);
            }
            else
            {
                HttpManager manager = new HttpManager();
                response = manager.CreateResponse(currentProtocol, request, Utility.locationDict, ref action);
            }
            socketStream.Write(response, 0, response.Length);
            //s.Stop();
            //Console.WriteLine("pre flush: " + s.ElapsedMilliseconds + "ms / " + s.ElapsedTicks + " ticks");
            socketStream.Flush();
            if (Utility.bDebug)
            {
                Console.WriteLine("Responded: " + action + " to " + connection.RemoteEndPoint);
            }
            Random rnd = new Random();
            bool successfulWrite = false;
            if (Utility.bLog && Utility.bHasLogWritePerm)
            {
                do
                {
                    string dataEntry = connection.RemoteEndPoint + " - - [" + DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss +zzz") + "] " + action;
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(Utility.logDir, true))
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
                    if (headerArgs[i].Substring(0, 3) == "GET")
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
