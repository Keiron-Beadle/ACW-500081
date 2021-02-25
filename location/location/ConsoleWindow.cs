using System;
using System.IO;
using System.Net.Sockets;

namespace location
{
    public enum Protocol
    {
        Whois,
        HTTP9,
        HTTP0,
        HTTP1
    }

    class ConsoleWindow
    {
        static Protocol currentProtocol = Protocol.Whois;
        static string address = "whois.net.dcs.hull.ac.uk";
        static int port = 43;
        static int timeout = 1000;
        static bool bDebug = false;
        static string person = "";
        static string personLocation = null;

        public static void Main(string[] args)
        {
            ProcessArguments(args);

            TcpClient client = new TcpClient(address, port);
            if (timeout > 0) { client.ReceiveTimeout = timeout; }

            NetworkStream ns = client.GetStream();

            string request = SendRequest(ns);
            if (bDebug)
            {
                Console.WriteLine("Connected to: " + client.Client.RemoteEndPoint);
                Console.WriteLine("Timeout set to: " + client.ReceiveTimeout + "ms");
                Console.WriteLine("Sent request: " + request);
            }
            try
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                string response = HandleResponse(ns);
                sw.Stop();
                if (bDebug) { Console.WriteLine("Time for response: " + sw.ElapsedMilliseconds + "ms"); }
                Console.Write(response);
            }
            catch (IOException) { Console.WriteLine("Request timed out. Disconnecting..."); client.Close(); }
        }

        private static string SendRequest(NetworkStream ns)
        {
            string request = "";
            switch (currentProtocol)
            {
                case Protocol.Whois:
                    request = WhoisManager.CreateRequest(person, personLocation);
                    break;
                case Protocol.HTTP9:
                case Protocol.HTTP0:
                case Protocol.HTTP1:
                    request = HttpManager.CreateRequest(currentProtocol, person, personLocation, address);
                    break;
            }
            ns.Write(System.Text.Encoding.ASCII.GetBytes(request), 0, request.Length);
            ns.Flush();
            return request;
        }

        private static string HandleResponse(NetworkStream ns)
        {
            string response = "";
            switch (currentProtocol)
            {
                case Protocol.Whois:
                    response = WhoisManager.HandleResponse(ns);
                    break;
                case Protocol.HTTP9:
                case Protocol.HTTP0:
                case Protocol.HTTP1:
                    response = HttpManager.HandleResponse(ns);
                    break;
            }
            return response;
        }

        private static void ProcessArguments(string[] args)
        {
            bool nameFound = false;
            for (int argIndex = 0; argIndex < args.Length; argIndex++)
            {
                try
                {
                    if (args[argIndex][0] == '-')
                    {
                        ProcessArgument(args, args[argIndex], ref argIndex);
                    }
                    else
                    {
                        if (nameFound) { personLocation = args[argIndex]; }
                        else { person = args[argIndex]; nameFound = true; }
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    if (nameFound) { personLocation = args[argIndex]; }
                    else { person = args[argIndex]; nameFound = true; }
                }
            }
        }

        private static void ProcessArgument(string[] args, string command, ref int index)
        {
            switch (command)
            {
                case "-h1":
                    currentProtocol = Protocol.HTTP1;
                    break;
                case "-h0":
                    currentProtocol = Protocol.HTTP0;
                    break;
                case "-h9":
                    currentProtocol = Protocol.HTTP9;
                    break;
                case "-h":
                    try { address = args[index + 1]; }
                    catch (IndexOutOfRangeException) { Console.WriteLine("You must enter an IP Address after the -h argument."); Exit(); }
                    index++;
                    break;
                case "-p":
                    try { port = int.Parse(args[index + 1]); }
                    catch (IndexOutOfRangeException) { Console.WriteLine("You must enter a port number after the -p argument. -p is optional for port 43."); Exit(); }
                    catch (FormatException) { Console.WriteLine("Please provide a valid integer port argument."); Exit(); }
                    index++;
                    break;
                case "-t":
                    try { timeout = int.Parse(args[index + 1]); }
                    catch (IndexOutOfRangeException) { Console.WriteLine("You must enter a timeout number after the -t argument."); Exit(); }
                    catch (FormatException) { Console.WriteLine("Please provide a valid integer timeout argument."); Exit(); }
                    index++;
                    break;
                case "-d":
                    bDebug = true;
                    break;
                default:
                    Console.WriteLine("Enter a valid argument after '-'.");
                    Exit();
                    break;
            }
        }

        private static void Exit() { Environment.Exit(0); }
    }
}