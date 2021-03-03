using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace locationserver
{
    public enum Protocol
    {
        Whois,
        HTTP9,
        HTTP0,
        HTTP1
    }

    class Utility
    {

        public static Dictionary<string, string> locationDict;
        private static TcpListener server;
        public static int port = 43;
        public static bool bDebug = false;
        public static bool bHasLogWritePerm = false;
        public static int timeout = 1000;
        public static string databaseDir = "";
        public static string logDir = "";
        public static bool bDatabase = false;
        public static bool bDatabaseWriteAccess = false, bDatabaseReadAccess = false;
        public static bool bLog = false;
        public static readonly string[] flags = { "-p", "-t", "-d", "-l", "-f" };

        public static void OnLoad()
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

        public static void OnExit()
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

        public static void RunServer(TextBox handleToUIText)
        {
            RequestHandler requestHandler;
            IPAddress localIP = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localIP, port);
            CheckIfPortOpen(handleToUIText);
            server.Start();
            UpdateUIText(handleToUIText, "Server started...\r\n");
            while (true)
            {
                Socket connection;
                try
                {
                    connection = server.AcceptSocket();
                }
                catch { return; }
                requestHandler = new RequestHandler();
                Thread t = new Thread(() => requestHandler.AcceptClient(connection, handleToUIText));
                t.Start();
            }
        }

        public static void RunServer()
        {
            RequestHandler requestHandler;
            IPAddress localIP = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localIP, port);
            server.Start();
            while (true)
            {
                Socket connection = server.AcceptSocket();
                requestHandler = new RequestHandler();
                Thread t = new Thread(() => requestHandler.AcceptClient(connection));
                t.Start();
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

        public static void CheckIfPortOpen(TextBox HandleToUI = null)
        {
            bool available = true;
            int attemptCount = 0;
            do
            {
                if (attemptCount > 10)
                {
                    Console.WriteLine("Connection failed after 10 tries, closing server...");
                    if (HandleToUI != null) { UpdateUIText(HandleToUI, "Connection failed after 10 tries, closing server...\r\n"); }
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
                        if (HandleToUI != null) { UpdateUIText(HandleToUI, "Port in use by other server, waiting 500ms...\r\n"); }
                        available = false;
                        attemptCount++;
                        Thread.Sleep(500);
                        break;
                    }
                    available = true;
                }
            } while (!available);


        }

        public static void ProcessArguments(string[] args)
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
                case "-f":
                    try
                    {
                        databaseDir = args[index + 1];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        databaseDir = Directory.GetCurrentDirectory() + "/database.db";
                    }
                    bDatabase = true;
                    index++;
                    break;
                case "-l":
                    try
                    {
                        logDir = args[index + 1];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        logDir = Directory.GetCurrentDirectory() + "server.log";
                    }
                    bLog = true;
                    index++;
                    break;
            }
        }

        public static void UpdateUIText(TextBox UIToUpdate, string s)
        {
            UIToUpdate.Dispatcher.Invoke(() =>
            {
                UIToUpdate.AppendText(s);
                UIToUpdate.ScrollToEnd();
            });
        }

        public static void CloseServer()
        {
            server.Server.Close();
        }

        public static void Exit(int exitCode = 0) { Environment.Exit(exitCode); }
    }
}