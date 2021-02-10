using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace locationserver
{
    public enum Protocol
    {
        Whois,
        HTTP9,
        HTTP0,
        HTTP1
    }

    class locationserver
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleMsgEventHandler handler, bool add);

        private delegate bool SetConsoleMsgEventHandler(CtrlType msg);

        private enum CtrlType
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT,
            CTRL_SHUTDOWN_EVENT
        }

        public static Dictionary<string, string> locationDict;
        static int port = 43;
        static readonly string[] flags = { "-p", "-t", "-d", "-l", "-f" };
        public static bool bDebug = false;
        public static bool bHasLogWritePerm = false;
        public static int timeout = 1000;
        public static string databaseDir = "";
        public static string logDir = "";
        public static bool bDatabase = false;
        static bool bDatabaseWriteAccess = false, bDatabaseReadAccess = false;
        public static bool bLog = false;

        static void Main(string[] args)
        {
            ProcessArguments(args);
            CheckIfPortOpen();
            OnLoad();
            RunServer();
        }

        private static void CheckIfPortOpen()
        {
            bool available = true;
            int attemptCount = 0;
            do
            {
                if (attemptCount > 10)
                {
                    Console.WriteLine("Connection failed after 10 tries, closing server...");
                    Thread.Sleep(800);
                    Environment.Exit(0);
                }
                IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] tcpInfo = globalProperties.GetActiveTcpListeners();

                foreach (IPEndPoint e in tcpInfo)
                {
                    if (e.Port == port)
                    {
                        Console.WriteLine("Port in use by other server, waiting 500ms...");
                        available = false;
                        attemptCount++;
                        Thread.Sleep(500);
                        break;
                    }
                    available = true;
                }
            } while (!available);


        }

        private static void OnLoad()
        {
            locationDict = new Dictionary<string, string>();
            if (bLog)
                SetLogWritePermission();
            if (bDatabase)
            {
                bDatabaseReadAccess = HasReadPermissions(databaseDir);
                ReadDatabase();
                bDatabaseWriteAccess = HasWritePermissions(databaseDir);
            }
        }

        private static void SetLogWritePermission()
        {
            if (bLog)
            {
                string path;
                try { path = Path.GetFullPath(logDir); } catch { logDir = ""; return; }
                bHasLogWritePerm = HasWritePermissions(path);
            }
        }

        private static void ReadDatabase()
        {
            string path = "";
            try
            {
                path = Path.GetFullPath(databaseDir);
            }
            catch { Console.WriteLine("Error reading database directory."); }
            StreamReader sr = null;
            try
            {
                if (bDatabase && File.Exists(path) && bDatabaseReadAccess)
                {
                    sr = new StreamReader(path);
                    string line = "";
                    while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                    {
                        string[] entry = line.Split(',');
                        try { locationDict.Add(entry[0], entry[1]); }
                        catch { continue; }
                    }
                }
            }
            catch
            {
                Console.Write("Error reading database.");
                databaseDir = "";
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }
        }

        private static void OnExit()
        {
            if (!bDatabase || !bDatabaseWriteAccess) { Environment.Exit(0); }
            StringBuilder sb = new StringBuilder(128);
            try
            {
                foreach (KeyValuePair<string, string> entry in locationDict)
                {
                    sb.Append(entry.Key + "," + entry.Value + "\r\n");
                }
                File.WriteAllText(databaseDir, sb.ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine("IOException thrown: " + e.Message);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Insufficient permissions to write to the database path.");
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static void RunServer()
        {
            SetConsoleCtrlHandler(ConsoleExitHandler, true);
            RequestHandler requestHandler;
            IPAddress localIP = IPAddress.Parse("127.0.0.1");
            TcpListener server = new TcpListener(localIP, port);
            server.Start();
            Console.WriteLine("Server started...");
            while (true)
            {
                Socket connection = server.AcceptSocket();
                requestHandler = new RequestHandler();
                //Change visual studio development settings to get more performance in threads. 
                Thread t = new Thread(() => requestHandler.AcceptClient(connection));
                t.Start();
            }
        }

        private static void ProcessArguments(string[] args)
        {
            for (int argIndex = 0; argIndex < args.Length; argIndex++)
            {
                bool cFlag = false;
                foreach (string flag in flags)
                {
                    if (flag == args[argIndex])
                    {
                        ProcessArgument(args, flag, ref argIndex);
                        cFlag = true;
                        break;
                    }
                }
                if (cFlag) { continue; }
            }
        }

        private static void ProcessArgument(string[] args, string command, ref int index)
        {
            switch (command)
            {
                case "-p":
                    port = int.Parse(args[index + 1]);
                    index++;
                    break;
                case "-t":
                    timeout = int.Parse(args[index + 1]);
                    index++;
                    break;
                case "-d":
                    bDebug = true;
                    break;
                case "-f":
                    databaseDir = args[index + 1];
                    bDatabase = true;
                    index++;
                    break;
                case "-l":
                    logDir = args[index + 1];
                    bLog = true;
                    index++;
                    break;
            }
        }

        public static bool HasReadPermissions(string directory)
        {
            if (!bDatabase) return false;

            if (File.Exists(directory))
            {
                var ps = new PermissionSet(PermissionState.None);
                var writePerm = new FileIOPermission(FileIOPermissionAccess.Read, directory);
                ps.AddPermission(writePerm);

                if (ps.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
                    return true;
                else
                    return false;
            }
            return false;
        }

        public static bool HasWritePermissions(string directory)
        {
            if (!bDatabase && !bLog) return false;
            try
            {
                if (File.Exists(directory)) { File.Delete(directory); }
                PermissionSet permissionSet = new PermissionSet(PermissionState.None);

                FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, directory);

                permissionSet.AddPermission(writePermission);

                if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

        }

        private static bool ConsoleExitHandler(CtrlType msg)
        {
            switch (msg)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    if (bDatabase)
                    {
                        Console.WriteLine("Saving database...");
                        OnExit();
                    }
                    return false;
                default:
                    return false;
            }
        }
    }

}