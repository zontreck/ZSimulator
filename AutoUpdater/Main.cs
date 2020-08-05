using Ronz.Compression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace AutoUpdater
{
    public class MainProgram
    {
        private static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static bool runReplacer=false;
        private static bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        private static ManualResetEvent downloadEvent = new ManualResetEvent(false);
        public static void Main(string[] args)
        {

            Process[] processes = Process.GetProcessesByName("Simulator");
            Process[] processes2 = Process.GetProcessesByName("simulator");
            if (!(processes.Length == 0 && processes2.Length == 0))
            {
                Console.WriteLine("Simulator process is running. Exit");
                Environment.Exit(0);
            }
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            Console.WriteLine("Simulator Automatic Updater");
            Console.WriteLine("Simulator Executable name: " +Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)));
            string filename = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            if (File.Exists("replacer.bat")) File.Delete("replacer.bat");
            if (File.Exists("replacer.sh")) File.Delete("replacer.sh");
            


            if (filename.StartsWith("AutoUpdater"))
            {
                if (args.Length == 0)
                {

                    if (Directory.Exists("update")) Directory.Delete("update", true);
                    string FileVersion = "0";
                    try
                    {
                        if (!File.Exists("Simulator.dll")) throw new Exception("No assembly exists by that name");
                        Assembly asm = Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "Simulator.dll"));


                        AssemblyFileVersionAttribute ver = asm.GetCustomAttribute<AssemblyFileVersionAttribute>();
                        FileVersion = ver.Version;

                        asm = null;
                    }
                    catch (Exception e)
                    {

                    }
                    GC.Collect(2);
                    GC.WaitForPendingFinalizers();

                    Console.WriteLine("Detected current version: " + FileVersion);
                    Console.WriteLine("Checking for updates...");

                    HttpWebRequest hwr = HttpWebRequest.CreateHttp("https://raw.githubusercontent.com/zontreck/ZSimulator/master/Version.txt");
                    hwr.Method = "GET";
                    HttpWebResponse hwre = (HttpWebResponse)hwr.GetResponse();
                    StreamReader reader = new StreamReader(hwre.GetResponseStream());
                    string RemoteVer = reader.ReadToEnd();

                    Console.WriteLine("Remote version: " + RemoteVer);
                    if (FileVersion == RemoteVer)
                    {
                        Console.WriteLine("Versions are identical, no update required");



                        ProcessStartInfo psi = new ProcessStartInfo();
                        if (isWindows) psi.FileName = "Simulator.exe";
                        else
                        {
                            psi.FileName = "screen";
                            psi.Arguments = "-dmS Simulator "+Path.Combine(Directory.GetCurrentDirectory(), "Simulator");
                        }

                        psi.UseShellExecute = true;
                        psi.WorkingDirectory = Directory.GetCurrentDirectory();
                        Process.Start(psi);
                    }
                    else
                    {
                        Console.WriteLine("An update is available");
                        Console.WriteLine("Spawning a new process");
                        if (File.Exists("__" + filename)) File.Delete("__" + filename);
                        File.Copy(filename, "__" + filename);

                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = "__" + filename;
                        psi.UseShellExecute = true;
                        psi.WindowStyle = ProcessWindowStyle.Maximized;
                        psi.WorkingDirectory = Directory.GetCurrentDirectory();
                        Process.Start(psi);

                        Environment.Exit(0);
                    }
                } else
                {
                    Console.WriteLine("Updating.......");

                    Console.WriteLine("Reading list of deprecated files.....");

                    List<string> deprecate = new List<string>();

                    // Remove deprecated files...
                    foreach(string dep in deprecate)
                    {
                        Console.WriteLine("REMOVE DEPRECATED: " + dep);
                        if(File.Exists(Path.Combine(args[0], dep)))
                            File.Delete(Path.Combine(args[0], dep));
                    }

                    if (isWindows)
                    {
                        File.AppendAllText("replacer.bat", "\n\nxcopy /E /Y " + Directory.GetCurrentDirectory() + "\\* " + args[0]+"\\");
                    }
                    else
                    {
                        File.AppendAllText("replacer.sh", "\n\ncp -rv " + Directory.GetCurrentDirectory() + "/* " + args[0] + "/");
                        File.AppendAllText("replacer.sh", "\n\nchmod +x " + Path.Combine(args[0], "AutoUpdater"));
                        File.AppendAllText("replacer.sh", "\n\nchmod +x " + Path.Combine(args[0], "Simulator"));
                        File.AppendAllText("replacer.sh", "\n\nchmod +x " + Path.Combine(args[0], "wait"));
                    }

                    runReplacer = true;
                    //RecursiveCopy(new DirectoryInfo(Directory.GetCurrentDirectory()), new DirectoryInfo(args[0]));

                    if (isWindows)
                    {
                        File.AppendAllText("replacer.bat", "\n\ndel /q /s " + Directory.GetCurrentDirectory());
                    } else
                    {
                        File.AppendAllText("replacer.sh", "\nrm -rfv " + Directory.GetCurrentDirectory());
                    }


                    ProcessStartInfo psi = new ProcessStartInfo();
                    if (!runReplacer)
                        psi.FileName = filename;
                    else
                    {
                        if (isWindows)
                        {
                            psi.FileName = Path.Combine(args[0], "replacer.bat");

                            File.AppendAllText("replacer.bat", "\n"+Path.Combine(args[0], "AutoUpdater.exe"));
                            if (File.Exists(Path.Combine(args[0], "replacer.bat"))) File.Delete(Path.Combine(args[0], "replacer.bat"));
                            File.Move("replacer.bat", psi.FileName);
                        }
                        else
                        {
                            psi.FileName = "/bin/sh";
                            psi.Arguments = Path.Combine(args[0],"replacer.sh");

                            File.AppendAllText("replacer.sh", "\n"+Path.Combine(args[0], "AutoUpdater"));
                            Process.Start("chmod", "+x replacer.sh");
                            if (File.Exists(Path.Combine(args[0], "replacer.sh"))) File.Delete(Path.Combine(args[0], "replacer.sh"));
                            File.Move("replacer.sh", Path.Combine(args[0], "replacer.sh"));
                        }
                    }
                    psi.UseShellExecute = true;
                    psi.WindowStyle = ProcessWindowStyle.Maximized;
                    Directory.SetCurrentDirectory(args[0]);
                    psi.WorkingDirectory = args[0];
                    Process.Start(psi);
                    File.WriteAllText("XUP", "1");
                }
            }else
            {
                Console.WriteLine("Starting update..");
                // Download the update manifest
                if (!isWindows) File.WriteAllText("replacer.sh", "#! /bin/bash\n\n");
                if (isWindows) File.AppendAllText("replacer.bat", "\ncd " + Directory.GetCurrentDirectory());
                else File.AppendAllText("replacer.sh", "\ncd " + Directory.GetCurrentDirectory());
                try
                {
                    WebClient wc = new WebClient();
                    if (File.Exists("update.tar")) File.Delete("update.tar");
                    //wc.DownloadFile("https://ci.zontreck.dev:8080/job/Bot/lastSuccessfulBuild/artifact/*zip*/archive.zip", "update.zip");
                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    wc.DownloadFileCompleted += Wc_DownloadFileCompleted;

                    HttpWebRequest hwr_srv = HttpWebRequest.CreateHttp("https://raw.githubusercontent.com/zontreck/ZSimulator/master/CIServer.txt");
                    hwr_srv.Method = "GET";
                    HttpWebResponse hwre = (HttpWebResponse)hwr_srv.GetResponse();
                    StreamReader reader = new StreamReader(hwre.GetResponseStream());
                    string CIServer = reader.ReadToEnd();

                    Console.WriteLine("Found CI SERVER: " + CIServer);

                    string ARCHIVE_URL = $"{CIServer.TrimEnd('\n')}job/ZSimulator/lastSuccessfulBuild/artifact/";
                    if (isWindows) ARCHIVE_URL += "windows.tar";
                    if (isLinux) ARCHIVE_URL += "linux.tar";
                    if (isMac) ARCHIVE_URL += "osx.tar";
                    wc.DownloadFileAsync(new Uri(ARCHIVE_URL), "update.tar");
                    if (!downloadEvent.WaitOne(TimeSpan.FromMinutes(1)))
                    {
                        Console.WriteLine("ERROR: Download failure or timeout has occured");
                        Console.WriteLine("Automatic Updater is terminating");
                        Environment.Exit(0);
                    }
                    Console.WriteLine("Final URL : " + ARCHIVE_URL);

                }catch(Exception e)
                {
                    Console.WriteLine("Could not save artifact! Error: " + e.Message+"\n\nSTACK: "+e.StackTrace);
                }
                Console.WriteLine("Update bundle saved");
                Console.WriteLine("Unpacking update..............");

                if (Directory.Exists(".tmp")) Directory.Delete(".tmp", true);
                ///ZipFile.ExtractToDirectory("update.zip", ".tmp");
                Archive.ExtractTar("update.tar", ".tmp");
                
                
                string OS="";
                string OS1="";
                if (isWindows) {
                    OS = "win-x64";
                    OS1 = "windows";
                }
                if (isLinux) {
                    OS = "linux-x64";
                    OS1 = "linux";
                }
                if (isMac) {
                    OS = "osx-x64";
                    OS1 = "osx";
                }
                if (Directory.Exists("update")) Directory.Delete("update", true);
                Directory.Move(".tmp/Simulator/bin/debpub/" + OS, "update");
                Directory.Delete(".tmp", true);
                if(!isWindows)
                    Process.Start("chmod", "+x " + Path.Combine(Directory.GetCurrentDirectory(), "update/AutoUpdater"));


                filename = filename.Replace("_", "");
                File.WriteAllText("XUP", "1");
                string UPDATE_PATH = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "update"), filename);
                Console.WriteLine("Path to updater: "+UPDATE_PATH);
                string DESTINATION = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "update"));
                Process.Start(UPDATE_PATH, DESTINATION);
                //RecursiveCopy(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "update")), new DirectoryInfo(Directory.GetCurrentDirectory()));
                
                /*
                filename = filename.Replace("_", "");

                ProcessStartInfo psi = new ProcessStartInfo();
                if (!runReplacer)
                    psi.FileName = filename;
                else
                {
                    if (isWindows)
                    {
                        psi.FileName = "replacer.bat";

                        File.AppendAllText("replacer.bat", Path.Combine(Directory.GetCurrentDirectory(),"AutoUpdater.exe"));
                    }
                    else
                    {
                        psi.FileName = "/bin/sh";
                        psi.Arguments = "replacer.sh";

                        File.AppendAllText("replacer.sh", Path.Combine(Directory.GetCurrentDirectory(),"AutoUpdater"));
                    }
                }
                psi.UseShellExecute = true;
                psi.WindowStyle = ProcessWindowStyle.Maximized;
                psi.WorkingDirectory = Directory.GetCurrentDirectory();
                Process.Start(psi);
                File.WriteAllText("XUP", "1");*/
            }
        }

        private static void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.Write("\r[OK] Download of update.tgz has completed!                                 \n");
            downloadEvent.Set();
        }

        private static void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            
            Console.Write($"[{e.ProgressPercentage}%] Downloading Update.tgz [{e.BytesReceived}/{e.TotalBytesToReceive}]\r");
        }

        public static void RecursiveCopy(DirectoryInfo source, DirectoryInfo target)
        {
            if (source.FullName.ToLower() == target.FullName.ToLower())
            {
                return;
            }

            if(Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            foreach(FileInfo fi in source.GetFiles())
            {
                try
                {
                    if (!IsFileLocked(fi))
                    {

                        Console.WriteLine($"Updating.. {target.FullName}/{fi.Name}");
                        fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
                    }
                    else throw new Exception("File in use");
                }catch(Exception e)
                {
                    runReplacer = true;
                    Console.WriteLine($"File in use: {target.FullName}/{fi.Name} - Will update this file after AutoUpdate finishes");
                    if (isWindows)
                    {
                        File.AppendAllText("replacer.bat", $"\ncopy /Y /V {fi.FullName} {target.FullName}\\{fi.Name}\n");
                    }else
                    {
                        File.AppendAllText("replacer.sh", $"\ncp {fi.FullName} {target.FullName}/{fi.Name}\n");
                    }
                }
            }

            foreach(DirectoryInfo subfolder in source.GetDirectories())
            {
                DirectoryInfo next;
                if (Directory.Exists(Path.Combine(target.FullName, subfolder.Name))) next = new DirectoryInfo(Path.Combine(target.FullName, subfolder.Name));
                else next = target.CreateSubdirectory(subfolder.Name);
                RecursiveCopy(subfolder, next);
            }
        }

        private static bool IsFileLocked(FileInfo fi)
        {
            try
            {
                using(FileStream stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
                return false;
            }catch(Exception e)
            {
                return true;
            }
        }
    }
}
