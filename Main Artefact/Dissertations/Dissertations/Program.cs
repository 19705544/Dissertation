using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Dissertations
{
    class Program
    {
        private static string[] filePaths, trustedDevices, trustedFiles, trustedTasks;
        private static List<string> savedDev, savedDevType, savedNet, savedPid;
        private static ConcurrentBag<String> untrustedFilesOfTheLastWeek;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter h for help");
            while (true)
            {
                Console.Write("Next command\n-");
                string input = Console.ReadLine();
                if (input == "t devices")
                {
                    trustedDevices = Program.ReadTrusted("trustedDevices.txt");
                    Program.CreatePhysicalFiles();
                }
                else if (input == "c devices")
                {
                    trustedDevices = Program.ReadTrusted("trustedDevices.txt");
                    if (trustedDevices != null)
                    {
                        if (trustedDevices.Length > 0)
                        {
                            Program.CheckPhysical();
                            Program.ComparePhysical();
                        }
                        else
                        {
                            Console.WriteLine("The trusted file has nothing in it, \nplease input 't devices' to check devices and then add to the trusted devices.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The trusted file does not exist, \nplease input 't devices' to check devices and then add to the trusted devices.");
                    }
                }
                else if (input == "t tasks")
                {
                    trustedTasks = Program.ReadTrusted("trustedTasks.txt");
                    Program.CreateNetTasksFiles();
                }
                else if (input == "c devices")
                {
                    trustedTasks = Program.ReadTrusted("trustedTasks.txt");
                    if (trustedTasks != null)
                    {
                        if (trustedTasks.Length > 0)
                        {
                            Program.CheckNetTasks();
                            Program.CompareNetTasks();
                        }
                        else
                        {
                            Console.WriteLine("The trusted file has nothing in it, \nplease input 't tasks' to check devices and then add to the trusted devices.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The trusted file does not exist, \nplease input 't tasks' to check devices and then add to the trusted devices.");
                    }
                }
                else if (input == "c files")
                {
                    //Gets all files
                    filePaths = PathsTxt();
                    //Prior trusted files saved to compare later
                    trustedFiles = Program.ReadTrusted("trustedFiles.txt");
                    if (trustedFiles != null)
                    {
                        //Checks if last week, filters
                        Program.CheckFiles(filePaths);
                    }
                    else
                    {
                        //parallel 
                        untrustedFilesOfTheLastWeek = new ConcurrentBag<String>();
                        foreach (string trustedFile in filePaths)
                        {
                            untrustedFilesOfTheLastWeek.Add(trustedFile);
                        }
                    }
                    //Shows files that may not be trustworthy in red
                    Console.ForegroundColor = ConsoleColor.Red;
                    foreach (string file in untrustedFilesOfTheLastWeek)
                    {
                        Console.WriteLine(file);
                    }
                    Console.ResetColor();
                    //If files trusted it takes input, concatinate to last files trusted
                    Console.WriteLine("Are these files trusted?\n\nInput 'y' for yes,\nif the first time running, select 'y',\nelse input any letter/number");
                    string inputInCFiles = Console.ReadLine().ToLower().Replace(" ", "");
                    if (inputInCFiles == "y")
                    {
                        Program.FilesLastWeek(untrustedFilesOfTheLastWeek);
                    }
                }
                else if (input == "h")
                {
                    Console.WriteLine("'c files' checks files and if all the files are trusted which they should be at the start of a fresh device, type 'y' to add them to the trusted files.\n");
                    Console.WriteLine("'c tasks' checks all programs running and if trusted press 't tasks'.\n");
                    Console.WriteLine("'c devices' checks all physical devices including past connections to ports and if trusted press 't devices'.\n");
                }
                else if (input == "e")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Enter h for help");
                }
            }
        }
        //Watches files
        static public void FilesLastWeek(ConcurrentBag<string> fileArray)
        {
            try
            {
                //Creates files and writes to, then closes
                TextWriter files = new StreamWriter("trustedFiles.txt", true);
                foreach (string file in fileArray) { files.WriteLine(file); }
                files.Close();
            }
            catch (Exception e) { Console.WriteLine(e); }
        }

        //A couple of functions to make some functions easier to read, also less copying and pasting. The first one gets net information and the second gets hardware information
        static public string[] NetStatInfo()
        {
            //Runs network tasks currently using network through cmd and formats to array
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Verb = "runas",
                Arguments = "/C netstat -ano",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            //Create array of each line from cmd
            return Process.Start(startInfo).StandardOutput.ReadToEnd().Split('\n');
        }

        static public string[] GetPnpDeviceInfo()
        {
            //Runs and formats to array of strings the devices in or recently connected to the device.
            var startInfo = new ProcessStartInfo
            {
                FileName = "Powershell.exe",
                Arguments = "Get-PnpDevice",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            //Create array of each line from Powershell
            return Process.Start(startInfo).StandardOutput.ReadToEnd().Split('\n');
        }

        static public string[] PathsTxt()
        {
            Console.WriteLine("Started: " + DateTime.Now.ToString());
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Verb = "runas",
                //Made formatting easier to separate lines, gets all files from every directory
                Arguments = "/C dir /s /b /o:gn /A:-D C:\\",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            //Create array of each line from Powershell
            return Process.Start(startInfo).StandardOutput.ReadToEnd().Split('\n');
        }

        static public void CheckFiles(string[] pathDir)
        {
            List<string> week = new List<string>();
            //Last week
            for (int day = 0; day > -7; day--)
            {
                week.Add(DateTime.Now.AddDays(day).Date.ToString().Substring(0, 10));
            }
            //Parallel programming to check each file is in last week and not in trusted
            untrustedFilesOfTheLastWeek = new ConcurrentBag<String>();
            Parallel.ForEach(pathDir.Except(trustedFiles), path =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Verb = "runas",
                    //Checks when it was last edited, if so it prioritises these since the older programes should be in trusted
                    Arguments = "/C dir /T:W \"" + path + "\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using (Process start = Process.Start(startInfo))
                {
                    String[] outP = start.StandardOutput.ReadToEnd().ToString().Split('\n');
                    try
                    {
                        //String manipulation to get date
                        if (week.Contains(outP[5].Substring(0, 10)))
                        {
                            //Later used to output files, parallel programming does not allow this easily in the loop
                            untrustedFilesOfTheLastWeek.Add(path);
                        }
                    }
                    catch { }
                }
            });
        }

        //Trusted programes and devices kept in file, both crreated by the function below
        static public string[] ReadTrusted(string trusted)
        {
            if (File.Exists(trusted))
            {
                string[] trustedOut = File.ReadAllLines(trusted);
                return trustedOut;
            }
            else
            {
                return null;
            }
        }

        //Create trusted tasks and net information to use
        static public void CreateNetTasksFiles()
        {
            //Creates files
            TextWriter textTasks = new StreamWriter("trustedTasks.txt", true);
            TextWriter textNet = new StreamWriter("netInfo.txt", true);
            
            //Run cmd with netstat information and save output as string
            string[] cmdStr = NetStatInfo();
            if (trustedTasks == null)
            {
                try
                {
                    for (int line = 4; line < cmdStr.Count() - 1; line++)
                    {
                        //Breaks down information through string manipulation
                        string[] array = cmdStr[line].Split(' ');
                        List<string> info = new List<string>();
                        for (int section = 0; section < array.Length; section++)
                        {
                            if (array[section] != "" && array[section] != " " && array[section] != "\r")
                            {
                                info.Add(array[section]);
                            }
                        }
                        //Writes to files
                        String processName = Process.GetProcessById(int.Parse(info.Last())).ProcessName;
                        textTasks.WriteLine(processName);
                        textNet.WriteLine(processName + " " + info[2]);
                    }

                    textTasks.Close();
                    textNet.Close();
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
            else
            {
                try
                {
                    for (int line = 4; line < cmdStr.Count() - 1; line++)
                    {
                        //Breaks down information through string manipulation
                        string[] array = cmdStr[line].Split(' ');
                        List<string> info = new List<string>();
                        for (int section = 0; section < array.Length; section++)
                        {
                            if (array[section] != "" && array[section] != " " && array[section] != "\r")
                            {
                                info.Add(array[section]);
                            }
                        }
                        //Writes to files
                        String processName = Process.GetProcessById(int.Parse(info.Last())).ProcessName;
                        if (!(trustedTasks.Contains<String>(processName)))
                        {
                            textTasks.WriteLine(processName);
                            textNet.WriteLine(processName + " " + info[2]);
                        }
                    }
                    textTasks.Close();
                    textNet.Close();
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        //Compare tasks that use internet
        static public void CompareNetTasks()
        {
            for (int current = 0; current < savedPid.Count(); current++)
            {
                String processName = Process.GetProcessById(int.Parse(savedPid[current])).ProcessName;
                if (!trustedTasks.Contains(processName)) Console.WriteLine(processName + " " + savedNet[current]);
            }
        }

        //Looks for programs that uses the network
        static public void CheckNetTasks()
        {
            //Create array of each line from cmd
            string[] cmdStr = NetStatInfo();
            savedNet = new List<string>();
            savedPid = new List<string>();

            //Breaks down information through string manipulation
            for (int line = 4; line < cmdStr.Count() - 1; line++)
            {
                string[] array = cmdStr[line].Split(' ');
                List<string> info = new List<string>();
                for (int section = 0; section < array.Length; section++)
                {
                    if (array[section] != "" && array[section] != " " && array[section] != "\r") info.Add(array[section]);
                }

                //Saves name in first array and ip in next
                savedNet.Add(info[2]);
                savedPid.Add(info.Last());
            }
        }

        //Compare physical devices, checks if any not seen before
        static public void ComparePhysical()
        {
            for (int current = 0; current < savedDev.Count(); current++)
            {
                String deviceName = savedDev[current];
                if (!trustedDevices.Contains(deviceName) || deviceName == "odd") Console.WriteLine(savedDevType[current]);
            }
        }

        //Checks physical devices
        static public void CheckPhysical()
        {
            string[] psStr = GetPnpDeviceInfo();
            savedDev = new List<string>();
            savedDevType = new List<string>();

            for (int line = 3; line < psStr.Count() - 3; line++)
            {
                //Breaks down information through string manipulation
                string[] array = psStr[line].Split(' ');
                List<string> info = new List<string>();
                for (int section = 0; section < array.Length; section++) if (array[section] != "" && array[section] != " " && array[section] != "\r") info.Add(array[section]);

                //Write name and info to separate files
                if (info.Count < 4) savedDev.Add("odd");
                else savedDev.Add(info[2]);
                savedDevType.Add(psStr[line]);
            }
        }

        static public void CreatePhysicalFiles()
        {
            string[] psStr = GetPnpDeviceInfo();
            savedDev = new List<string>();
            savedDevType = new List<string>();

            TextWriter device = new StreamWriter("trustedDevices.txt", true);
            TextWriter deviceInfo = new StreamWriter("trustedDevicesInfo.txt", true);

            if (trustedDevices == null)
            {
                try
                {
                    for (int line = 3; line < psStr.Count() - 3; line++)
                    {
                        //Breaks down information through string manipulation
                        string[] array = psStr[line].Split(' ');
                        List<string> info = new List<string>();
                        for (int section = 0; section < array.Length; section++) if (array[section] != "" && array[section] != " " && array[section] != "\r") info.Add(array[section]);

                        //Name of device
                        if (info.Count < 4) savedDev.Add("odd");
                        else savedDev.Add(info[2]);

                        savedDevType.Add(psStr[line - 3]);

                        //Write name and info to separate files
                        String deviceName = savedDev[line - 3];
                        device.WriteLine(deviceName);
                        deviceInfo.WriteLine(deviceName + " " + savedDev.Last());
                    }

                    device.Close();
                    deviceInfo.Close();
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
            else
            {
                try
                {
                    for (int line = 3; line < psStr.Count() - 3; line++)
                    {
                        //Breaks down information through string manipulation
                        string[] array = psStr[line].Split(' ');
                        List<string> info = new List<string>();
                        for (int section = 0; section < array.Length; section++) if (array[section] != "" && array[section] != " " && array[section] != "\r") info.Add(array[section]);

                        //Name of device
                        if (info.Count < 4) savedDev.Add("odd");
                        else savedDev.Add(info[2]);

                        savedDevType.Add(psStr[line - 3]);

                        //Write name and info to separate files
                        String deviceName = savedDev[line - 3];
                        if (!(trustedDevices.Contains<String>(deviceName)))
                        {
                            device.WriteLine(deviceName);
                            deviceInfo.WriteLine(deviceName + " " + savedDev.Last());
                        }
                    }

                    device.Close();
                    deviceInfo.Close();
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }
    }
}