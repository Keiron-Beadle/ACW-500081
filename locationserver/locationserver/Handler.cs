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
    public class SocketStateObject
    {
        public Socket socket = null;
        public const int BufferSize = 512;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public string request;
    }
    class RequestHandler
    {
        private Protocol currentProtocol = Protocol.Whois;
        private TextBox handleToUIOutput;
        ManualResetEvent readReset = new ManualResetEvent(false);
        ManualResetEvent writeReset = new ManualResetEvent(false);

        public async void HandleClient(Socket connection, TextBox handleToOutput)
        {
            //handleToUIOutput = handleToOutput;
            //NetworkStream socketStream;
            //Utility.UpdateUIText(handleToOutput,"Connection accepted...\r\n");
            //socketStream = new NetworkStream(connection)
            //{
            //    ReadTimeout = Utility.timeout
            //};
            //string request = ReadRequest();
            //if (Utility.bDebug)
            //    Utility.UpdateUIText(handleToOutput,"Request recevied from " + connection.RemoteEndPoint + "...\r\n");
            //else
            //    Utility.UpdateUIText(handleToOutput,"Request received...\r\n");
            //byte[] response;
            //string action = "";
            //if (currentProtocol == Protocol.Whois)
            //{
            //    WhoisManager manager = new WhoisManager();
            //    response = manager.CreateResponse(request, Utility.locationDict, ref action);
            //}
            //else
            //{
            //    HttpManager manager = new HttpManager();
            //    response = manager.CreateResponse(currentProtocol, request, Utility.locationDict, ref action);
            //}
            //await socketStream.WriteAsync(response, 0, response.Length);
            //await socketStream.FlushAsync();
            //if (Utility.bDebug)
            //{
            //    string responseStr = Encoding.ASCII.GetString(response);
            //    responseStr = responseStr.Substring(0, response.Length - 4);
            //    Utility.UpdateUIText(handleToOutput,"Responded: " + responseStr + " to " + connection.RemoteEndPoint + "\r\n");
            //}
            //Random rnd = new Random();
            //bool successfulWrite = false;
            //if (Utility.bLog && Utility.bHasLogWritePerm)
            //{
            //    do
            //    {
            //        string dataEntry = connection.RemoteEndPoint + " - - [" + DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss +zzz") + "] " + action;
            //        try
            //        {
            //            using (StreamWriter sw = new StreamWriter(Utility.logDir, true))
            //            {
            //                sw.WriteLine(dataEntry);
            //                successfulWrite = true;
            //            }
            //        }
            //        catch
            //        {
            //            Thread.Sleep(rnd.Next(50, 500));
            //        }
            //    } while (!successfulWrite);

            //}

            //socketStream.Close();
            //connection.Close();
        }

        public void HandleClient(Socket connection)
        {
            SocketStateObject SSO = new SocketStateObject();
            SSO.socket = connection;
            //Console.WriteLine("Connection accepted...");
            connection.BeginReceive(SSO.buffer, 0, SocketStateObject.BufferSize, 0,
                 new AsyncCallback(ReadCallback), SSO);
            //if (Utility.bDebug)
            //    Console.WriteLine("Request recevied from " + connection.RemoteEndPoint + "...");
            //else
            //    Console.WriteLine("Request received...");

            //connection.Send(response);
            //if (Utility.bDebug)
            //{
            //    Console.WriteLine("Responded: " + action + " to " + connection.RemoteEndPoint);
            //}
            //Random rnd = new Random();
            //bool successfulWrite = false;
            //if (Utility.bLog && Utility.bHasLogWritePerm)
            //{
            //    do
            //    {
            //        string dataEntry = connection.RemoteEndPoint + " - - [" + DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss +zzz") + "] " + action;
            //        try
            //        {
            //            using (StreamWriter sw = new StreamWriter(Utility.logDir, true))
            //            {
            //                sw.WriteLine(dataEntry);
            //                successfulWrite = true;
            //            }
            //        }
            //        catch
            //        {
            //            Thread.Sleep(rnd.Next(50, 500));
            //        }
            //    } while (!successfulWrite);

            //}

            //connection.Close();
        }

        private void ReadRequest(Socket connection, SocketStateObject pSSO)
        {
            //int bytes = 0;
            //byte[] data;
            //while (true)
            //{
            //    data = new byte[256];
            //    try { bytes = connection.Receive(data); }
            //    catch (IOException) { }
            //    pSSO.sb.Append(Encoding.ASCII.GetString(data));
            //    if (bytes < 512) { break; }
            // }
            connection.BeginReceive(pSSO.buffer, 0, SocketStateObject.BufferSize, 0,
                 new AsyncCallback(ReadCallback), pSSO);
            //readReset.WaitOne();
            //pSSO.sb.Replace("\0", "");
            //string request = pSSO.sb.ToString();
            //DeduceProtocol(request);
            //if (currentProtocol == Protocol.HTTP1)
            //{
            //    string[] headerArgs = request.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //    for (int i = 0; i < headerArgs.Length; i++)
            //    {
            //        if (headerArgs[i].Substring(0, 3) == "GET")
            //        {
            //            return headerArgs[i];
            //        }
            //    }
            //}
            //return request;
        }

        private void ReadCallback(IAsyncResult ar)
        {
            SocketStateObject sso = (SocketStateObject)ar.AsyncState;
            Socket handler = sso.socket;
            //readReset.Set();
            int bytes = handler.EndReceive(ar);
            if (bytes > 0)
            {
                sso.sb.Append(Encoding.ASCII.GetString(sso.buffer, 0, bytes));
                string msg = sso.sb.ToString();
                if (msg.Length == SocketStateObject.BufferSize)
                {
                    handler.BeginReceive(sso.buffer, 0, SocketStateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), sso);
                }
            }

            sso.sb.Replace("\0", "");
            string request = sso.sb.ToString();
            DeduceProtocol(request);
            if (currentProtocol == Protocol.HTTP1)
            {
                string[] headerArgs = request.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < headerArgs.Length; i++)
                {
                    if (headerArgs[i].Substring(0, 3) == "GET")
                    {
                        sso.request = headerArgs[i];
                    }
                }
            }
            sso.request = request;

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

            sso.socket.BeginSend(response, 0, response.Length, 0, new AsyncCallback(WriteCallback), sso);
        }

        private void WriteCallback(IAsyncResult ar)
        {
            SocketStateObject sso = (SocketStateObject)ar.AsyncState;
            Socket handler = sso.socket;
            try
            {
                handler.EndSend(ar);
            }
            catch
            {
                Console.WriteLine("Error sending bytes");
            }
            finally
            {
                handler.Close();
            }
        }

        private void DeduceProtocol(string request)
        {
            bool chosen = false;
            try
            {
                Regex getCheck = new Regex(@"GET \/([!-~])+(\r|\n|\r\n)");
                Regex putCheck = new Regex(@"PUT \/([!-~])+(\r|\n|\r\n){2}([!-~]| )+(\r|\n|\r\n)");
                if (getCheck.Matches(request).Count > 0 || putCheck.Matches(request).Count > 0)
                {
                    currentProtocol = Protocol.HTTP9;
                    chosen = true;
                }
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
