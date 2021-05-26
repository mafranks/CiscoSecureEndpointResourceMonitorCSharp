using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Microsoft.Win32;

namespace CiscoSecureEndpointResourceMonitor
{
    public partial class MainWindow : Window
    {
        BackgroundWorker backgroundWorker1;
        public MainWindow()
        {
            InitializeComponent();
            findPath();
            pullXMLFileInfo();
            Running.running = true;
            ResetButton.IsEnabled = false;
            // This youtube video is amazing for BackgroundWorker explanation!
            // https://www.youtube.com/watch?v=snkDYT1Qz6g Thanks Zaheer Sani!
            backgroundWorker1 = new BackgroundWorker();
            //Add DoWork Event Handler
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            //Add ProgressChanged Event Hanlder *** THIS ALLOWS YOU TO UPDATE THE GUI THREAD ***
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            //Add RunWorkerCompleted Event Handler ** RUN CODE WHEN BACKGROUND WORKER COMPLETES **
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            //Allow worker to report progress
            backgroundWorker1.WorkerReportsProgress = true;
            //Allow worker to be cancelled
            backgroundWorker1.WorkerSupportsCancellation = true;
            
        }
        public void findPath()
        {
            // Allows for non-default directory installations
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Immunet Protect"))
            {
                string message = "Unable to find AMP install directory.";
                string title = "Path Error";
                if (key != null)
                {
                    Object temp_path = key.GetValue("UninstallString");
                    if (temp_path != null)
                    {
                        List<string> path_list = new List<string>(temp_path.ToString().Trim('"').Split('\\'));
                        path_list.RemoveAt(path_list.Count - 1); // Removes uninstall.exe
                        string version_test = path_list[path_list.Count - 1];
                        path_list.RemoveAt(path_list.Count - 1); // Removes the version
                        string path = string.Join("\\", path_list);
                        path_list.RemoveAt(path_list.Count - 1); // Removes AMP
                        path_list.Add("Orbital"); // Adds Orbital
                        string orbital_path = string.Join("\\", path_list);
                        Running.current_version = version_test;
                        Running.path = path;
                        Running.orbital_path = orbital_path;
                    }
                    else 
                    {
                        MessageBox.Show(message, title);
                    }
                }
                else 
                {
                    MessageBox.Show(message, title);
                }
            }

        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (Running.running == true)
            {
                if (backgroundWorker1.CancellationPending) { break; }
                // Initialize to start capturing
                PerformanceCounter sfccpuCounter = new PerformanceCounter("Process", "% Processor Time", "sfc");
                PerformanceCounter sfcramCounter = new PerformanceCounter("Process", "Working Set", "sfc");
                PerformanceCounter cscmcpuCounter = new PerformanceCounter("Process", "% Processor Time", "cscm");
                PerformanceCounter cscmramCounter = new PerformanceCounter("Process", "Working Set", "cscm");
                PerformanceCounter orbitalcpuCounter = new PerformanceCounter("Process", "% Processor Time", "orbital");
                PerformanceCounter orbitalramCounter = new PerformanceCounter("Process", "Working Set", "orbital");
                PerformanceCounter iptraycpuCounter = new PerformanceCounter("Process", "% Processor Time", "iptray");
                PerformanceCounter iptrayramCounter = new PerformanceCounter("Process", "Working Set", "iptray");
                PerformanceCounter connectivitytoolcpuCounter = new PerformanceCounter("Process", "% Processor Time", "ConnectivityTool");
                PerformanceCounter connectivitytoolramCounter = new PerformanceCounter("Process", "Working Set", "ConnectivityTool");
                PerformanceCounter ipsupporttoolcpuCounter = new PerformanceCounter("Process", "% Processor Time", "ipsupporttool");
                PerformanceCounter ipsupporttoolramCounter = new PerformanceCounter("Process", "Working Set", "ipsupporttool");
                PerformanceCounter updatercpuCounter = new PerformanceCounter("Process", "% Processor Time", "updater");
                PerformanceCounter updaterramCounter = new PerformanceCounter("Process", "Working Set", "updater");
                PerformanceCounter casetup64cpuCounter = new PerformanceCounter("Process", "% Processor Time", "casetup64");
                PerformanceCounter casetup64ramCounter = new PerformanceCounter("Process", "Working Set", "casetup64");
                PerformanceCounter freshclamcpuCounter = new PerformanceCounter("Process", "% Processor Time", "freshclam");
                PerformanceCounter freshclamramCounter = new PerformanceCounter("Process", "Working Set", "freshclam");
                PerformanceCounter freshclamwrapcpuCounter = new PerformanceCounter("Process", "% Processor Time", "freshclamwrap");
                PerformanceCounter freshclamwrapramCounter = new PerformanceCounter("Process", "Working Set", "freshclamwrap");

                sfccpuCounter.NextValue();
                cscmcpuCounter.NextValue();
                try { iptraycpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                try { connectivitytoolcpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                try { ipsupporttoolcpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                try { updatercpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                try { casetup64cpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                try { freshclamcpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                try { freshclamwrapcpuCounter.NextValue(); } catch (System.InvalidOperationException) { }
                if (Running.orbital == 1)
                {
                    orbitalcpuCounter.NextValue();
                }
                // Give some time to accumulate data
                Thread.Sleep(1000);

                // Divide the cpu response by the number of processors
                float current_sfc_cpu = sfccpuCounter.NextValue() / Environment.ProcessorCount;
                float current_cscm_cpu = cscmcpuCounter.NextValue() / Environment.ProcessorCount;
                try { Running.current_iptray_cpu = iptraycpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_iptray_cpu = 0; }
                try { Running.current_connectivitytool_cpu = connectivitytoolcpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_connectivitytool_cpu = 0; }
                try { Running.current_ipsupporttool_cpu = ipsupporttoolcpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_ipsupporttool_cpu = 0; }
                try { Running.current_updater_cpu = updatercpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_updater_cpu = 0; }
                try { Running.current_casetup64_cpu = casetup64cpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_casetup64_cpu = 0; }
                try { Running.current_freshclam_cpu = freshclamcpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_freshclam_cpu = 0; }
                try { Running.current_freshclamwrap_cpu = freshclamwrapcpuCounter.NextValue() / Environment.ProcessorCount; }
                catch (System.InvalidOperationException) { Running.current_freshclamwrap_cpu = 0; }
                if (Running.orbital == 1)
                {
                    Running.current_orbital_cpu = orbitalcpuCounter.NextValue() / Environment.ProcessorCount;
                    Running.current_orbital_ram = orbitalramCounter.NextValue() / 1024 / 1024;
                }
                // Calculate RAM in MB
                float current_sfc_ram = sfcramCounter.NextValue() / 1024 / 1024;
                float current_cscm_ram = cscmramCounter.NextValue() / 1024 / 1024;
                try { Running.current_iptray_ram = iptrayramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_iptray_ram = 0; }
                try { Running.current_connectivitytool_ram = connectivitytoolramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_connectivitytool_ram = 0; }
                try { Running.current_ipsupporttool_ram = ipsupporttoolramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_ipsupporttool_ram = 0; }
                try { Running.current_updater_ram = updaterramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_updater_ram = 0; }
                try { Running.current_casetup64_ram = casetup64ramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_casetup64_ram = 0; }
                try { Running.current_freshclam_ram = freshclamramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_freshclam_ram = 0; }
                try { Running.current_freshclamwrap_ram = freshclamwrapramCounter.NextValue() / 1024 / 1024; }
                catch (System.InvalidOperationException) { Running.current_freshclamwrap_ram = 0; }
                
                // Get totals
                float total_cpu = current_sfc_cpu + current_cscm_cpu + Running.current_orbital_cpu + Running.current_iptray_cpu +
                    Running.current_connectivitytool_cpu + Running.current_creport_cpu + Running.current_ipsupporttool_cpu +
                    Running.current_updater_cpu + Running.current_casetup64_cpu + Running.current_freshclam_cpu +
                    Running.current_freshclamwrap_cpu;
                float total_ram = current_sfc_ram + current_cscm_ram + Running.current_orbital_ram + Running.current_iptray_ram +
                    Running.current_connectivitytool_ram + Running.current_creport_ram + Running.current_ipsupporttool_ram +
                    Running.current_updater_ram + Running.current_casetup64_ram + Running.current_freshclam_ram +
                    Running.current_freshclamwrap_ram;

                // Compare current with max and change if necessary
                if (current_sfc_cpu > Running.max_sfc_cpu) { Running.max_sfc_cpu = current_sfc_cpu; }
                if (current_sfc_ram > Running.max_sfc_ram) { Running.max_sfc_ram = current_sfc_ram; }
                if (current_cscm_cpu > Running.max_cscm_cpu) { Running.max_cscm_cpu = current_cscm_cpu; }
                if (current_cscm_ram > Running.max_cscm_ram) { Running.max_cscm_ram = current_cscm_ram; }
                if (Running.current_orbital_cpu > Running.max_orbital_cpu) { Running.max_orbital_cpu = Running.current_orbital_cpu; }
                if (Running.current_orbital_ram > Running.max_orbital_ram) { Running.max_orbital_ram = Running.current_orbital_ram; }
                if (Running.current_iptray_cpu > Running.max_iptray_cpu) { Running.max_iptray_cpu = Running.current_iptray_cpu; }
                if (Running.current_iptray_ram > Running.max_iptray_ram) { Running.max_iptray_ram = Running.current_iptray_ram; }
                if (Running.current_connectivitytool_cpu > Running.max_connectivitytool_cpu) { Running.max_connectivitytool_cpu = Running.current_connectivitytool_cpu; }
                if (Running.current_connectivitytool_ram > Running.max_connectivitytool_ram) { Running.max_connectivitytool_ram = Running.current_connectivitytool_ram; }
                if (Running.current_creport_cpu > Running.max_creport_cpu) { Running.max_creport_cpu = Running.current_creport_cpu; }
                if (Running.current_creport_ram > Running.max_creport_ram) { Running.max_creport_ram = Running.current_creport_ram; }
                if (Running.current_ipsupporttool_cpu > Running.max_ipsupporttool_cpu) { Running.max_ipsupporttool_cpu = Running.current_ipsupporttool_cpu; }
                if (Running.current_ipsupporttool_ram > Running.max_ipsupporttool_cpu) { Running.max_ipsupporttool_ram = Running.current_ipsupporttool_ram; }
                if (Running.current_updater_cpu > Running.max_updater_cpu) { Running.max_updater_cpu = Running.current_updater_cpu; }
                if (Running.current_updater_ram > Running.max_updater_ram) { Running.max_updater_ram = Running.current_updater_ram; }
                if (Running.current_casetup64_cpu > Running.max_casetup64_cpu) { Running.max_casetup64_cpu = Running.current_casetup64_cpu; }
                if (Running.current_casetup64_ram > Running.max_casetup64_ram) { Running.max_casetup64_ram = Running.current_casetup64_ram; }
                if (Running.current_freshclam_cpu > Running.max_freshclam_cpu) { Running.max_freshclam_cpu = Running.current_freshclam_cpu; }
                if (Running.current_freshclam_ram > Running.max_freshclam_ram) { Running.max_freshclam_ram = Running.current_freshclam_ram; }
                if (Running.current_freshclamwrap_cpu > Running.max_freshclamwrap_cpu) { Running.max_freshclamwrap_cpu = Running.current_freshclamwrap_cpu; }
                if (Running.current_freshclamwrap_ram > Running.max_freshclamwrap_ram) { Running.max_freshclamwrap_ram = Running.current_freshclamwrap_ram; }

                if (total_cpu > Running.max_cpu) { Running.max_cpu = total_cpu; }
                if (total_ram > Running.max_ram) { Running.max_ram = total_ram; }

                // Create a set of 3 dots for a dynamic message
                if (Running.dots.Length == 1) { Running.dots = ".."; }
                else if (Running.dots.Length == 2) { Running.dots = "..."; Running.diskSize = GetDirectorySize(); }
                else { Running.dots = "."; }

                // Tuple has a limit of 7 items so you have to use nested Tuples to report progress to the main thread
                backgroundWorker1.ReportProgress(0, Tuple.Create(current_sfc_cpu, current_cscm_cpu, total_cpu,
                        current_sfc_ram, current_cscm_ram, total_ram));
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var args = (Tuple<float, float, float, float, float, float>)e.UserState;
            CPUUsageText.Text = $"{args.Item1} %";
            cscmCPUUSageText.Text = $"{args.Item2} %";
            orbitalCPUUsageText.Text = $"{Running.current_orbital_cpu} %";
            TotalCPUText.Text = $"{args.Item3} %";
            sfcMaxCPUText.Text = $"{Running.max_sfc_cpu} %";
            cscmMaxCPUText.Text = $"{Running.max_cscm_cpu} %";
            orbitalMaxCPUText.Text = $"{Running.max_orbital_cpu} %";
            MaxCPUText.Text = $"{Running.max_cpu} %";
            sfcRAMText.Text = $"{args.Item4} MB";
            cscmRAMText.Text = $"{args.Item5} MB";
            orbitalRAMText.Text = $"{Running.current_orbital_ram} MB";
            TotalMemoryText.Text = $"{args.Item6} MB";
            sfcMaxRAMText.Text = $"{Running.max_sfc_ram} MB";
            cscmMaxRAMText.Text = $"{Running.max_cscm_ram} MB";
            orbitalMaxRAMText.Text = $"{Running.max_orbital_ram} MB";
            MaxRAMText.Text = $"{Running.max_ram} MB";
            TotalDiskText.Text = $"{Running.diskSize} MB";
            StatusText.Text = $"Running {Running.dots}";
            if (Running.export_checkbox == true)
            {
                var timeStamp = new DateTimeOffset(DateTime.UtcNow); //.ToUnixTimeSeconds();
                File.AppendAllText(Running.writePath, $"{timeStamp} UTC, {Running.diskSize}MB, {TotalCPUText.Text}, " +
                    $"{Running.max_cpu}%, {TotalMemoryText.Text}, {Running.max_ram}MB, {CPUUsageText.Text}, " +
                    $"{cscmCPUUSageText.Text}, {Running.current_orbital_cpu}%, {Running.current_iptray_cpu}%, " +
                    $"{Running.current_connectivitytool_cpu}%, {Running.current_creport_cpu}%, {Running.current_ipsupporttool_cpu}%, " +
                    $"{Running.current_updater_cpu}%, {Running.current_casetup64_cpu}%, " +
                    $"{Running.current_freshclam_cpu}%, {Running.current_freshclamwrap_cpu}%, " +
                    $"{Running.max_sfc_cpu}%, {Running.max_cscm_cpu}%, {Running.max_orbital_cpu}%, {Running.max_iptray_cpu}%, " +
                    $"{Running.max_connectivitytool_cpu}%, {Running.max_creport_cpu}%, {Running.max_ipsupporttool_cpu}%, " +
                    $"{Running.max_updater_cpu}%, {Running.max_casetup64_cpu}%, {Running.max_freshclam_cpu}%, " +
                    $"{Running.max_freshclamwrap_cpu}%, {sfcRAMText.Text}, {cscmRAMText.Text}, " +
                    $"{Running.current_orbital_ram}MB, {Running.current_iptray_ram}MB, {Running.current_connectivitytool_ram}MB, " +
                    $"{Running.current_creport_ram}MB, {Running.current_ipsupporttool_ram}MB, {Running.current_updater_ram}MB, " +
                    $"{Running.current_casetup64_ram}MB, {Running.current_freshclam_ram}MB, {Running.current_freshclamwrap_ram}MB, " +
                    $"{Running.max_sfc_ram}MB, {Running.max_cscm_ram}MB, {Running.max_orbital_ram}MB, " +
                    $"{Running.max_iptray_ram}MB, {Running.max_connectivitytool_ram}MB, {Running.max_creport_ram}MB, " +
                    $"{Running.max_ipsupporttool_ram}MB, {Running.max_updater_ram}MB, {Running.max_casetup64_ram}MB, " +
                    $"{Running.max_freshclam_ram}MB, {Running.max_freshclamwrap_ram}MB\n");

            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Running.export_checkbox = true;
            exportCheckbox.IsEnabled = false;
            using (StreamWriter writer = new StreamWriter(Running.writePath))
            {
                writer.WriteLine("Timestamp, Disk_Usage, Total_CPU, Total_MAX_CPU, Total_RAM, " +
                    "Total_MAX_RAM, SFC_CPU, CSCM_CPU, Orbital_CPU, IPTray_CPU, " +
                    "ConnectivityTool_CPU, Creport_CPU, IPSupportTool_CPU, Updater_CPU, Casetup64_CPU, " +
                    "Freshclam_CPU, Freshclamwrap_CPU, SFC_MAX_CPU, CSCM_MAX_CPU, " +
                    "Orbital_MAX_CPU, IPTray_MAX_CPU, ConnectivityTool_MAX_CPU, Creport_MAX_CPU, " +
                    "IPSupportTool_MAX_CPU, Updater_MAX_CPU, Casetup64_MAX_CPU, Freshclam_MAX_CPU, " +
                    "Freshclamwrap_MAX_CPU, SFC_RAM, CSCM_RAM, Orbital_RAM, IPTray_RAM, " +
                    "ConnectivityTool_RAM,  Creport_RAM,  IPSupportTool_RAM, Updater_RAM, Casetup64_RAM, " +
                    "Freshclam_RAM, Freshclamwrap_RAM, SFC_MAX_RAM, CSCM_MAX_RAM, " +
                    "Orbital_MAX_RAM, IPTray_MAX_RAM, ConnecitivityTool_MAX_RAM, Creport_MAX_RAM, " +
                    "IPSupportTool_MAX_RAM, Updater_MAX_RAM, Casetup64_MAX_RAM, Freshclam_MAX_RAM, " +
                    "Freshclamwrap_MAX_RAM");
            }
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StatusText.Text = "Press Start to Continue";
        }
        static class Running
        {
            // variables to use in the running loop
            public static bool running;
            public static float max_cpu = 0;
            public static float max_ram = 0;
            public static float max_sfc_cpu = 0;
            public static float max_sfc_ram = 0;
            public static float max_cscm_cpu = 0;
            public static float max_cscm_ram = 0;
            public static float current_iptray_ram = 0;
            public static float max_iptray_ram = 0;
            public static float current_iptray_cpu = 0;
            public static float max_iptray_cpu = 0;
            public static float current_connectivitytool_cpu = 0;
            public static float max_connectivitytool_cpu = 0;
            public static float current_connectivitytool_ram = 0;
            public static float max_connectivitytool_ram = 0;
            public static float current_creport_cpu = 0;
            public static float max_creport_cpu = 0;
            public static float current_creport_ram = 0;
            public static float max_creport_ram = 0;
            public static float current_ipsupporttool_cpu = 0;
            public static float max_ipsupporttool_cpu = 0;
            public static float current_ipsupporttool_ram = 0;
            public static float max_ipsupporttool_ram = 0;
            public static float current_updater_cpu = 0;
            public static float max_updater_cpu = 0;
            public static float current_updater_ram = 0;
            public static float max_updater_ram = 0;
            public static float current_casetup64_cpu = 0;
            public static float max_casetup64_cpu = 0;
            public static float current_casetup64_ram = 0;
            public static float max_casetup64_ram = 0;
            public static float current_freshclam_cpu = 0;
            public static float max_freshclam_cpu = 0;
            public static float current_freshclam_ram = 0;
            public static float max_freshclam_ram = 0;
            public static float current_freshclamwrap_cpu = 0;
            public static float max_freshclamwrap_cpu = 0;
            public static float current_freshclamwrap_ram = 0;
            public static float max_freshclamwrap_ram = 0;
            public static long diskSize = 0;
            public static string dots = ".";
            public static int orbital = 0;
            public static float current_orbital_cpu = 0;
            public static float current_orbital_ram = 0;
            public static float max_orbital_cpu = 0;
            public static float max_orbital_ram = 0;
            public static string current_version = "0.0.0";
            public static string path = @"C:\Program Files\Cisco\AMP";
            public static string orbital_path = @"C:\Program Files\Cisco\Orbital";
            public static string tetra_def_version = "0";
            public static bool export_checkbox = false;
            public static string writePath = "ResourceMonitor.csv";
        }
        static long GetDirectorySize()
        {
            // Get array of all file names.
            string[] AMPfiles = Directory.GetFiles(Running.path, "*.*", SearchOption.AllDirectories);
            
            // Calculate total bytes of all files in a loop.
            long fileBytes = 0;
            fileBytes += parseDir(AMPfiles);
            // Repeat for Orbital if it is enabled

            if (Running.orbital == 1)
            {
                string[] Orbitalfiles = Directory.GetFiles(Running.orbital_path, "*.*", SearchOption.AllDirectories);
                fileBytes += parseDir(Orbitalfiles);
                // Return total size
            }
            return fileBytes;

        }
        static long parseDir(string[] files)
        {
            long fileBytes = 0;
            foreach (string name in files)
            {
                // Use FileInfo to get length of each file.
                FileInfo info = new FileInfo(name);
                // Check to ensure the file exists before checking length - allows for temp files that might be gone
                if (info.Exists)
                {
                    fileBytes += info.Length;
                }
            }
            return fileBytes / 1024 / 1024;
        }
        public void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
            ResetButton.IsEnabled = false;
            StatusText.Text = "Running";
            Running.running = true;
            //parseProcesses();
            backgroundWorker1.RunWorkerAsync();
        }
        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            ResetButton.IsEnabled = true;
            backgroundWorker1.CancelAsync();
            Running.running = false;
            
        }
        private void ResetButton_Click_1(object sender, RoutedEventArgs e)
        {
            ResetButton.IsEnabled = false;
            Running.max_cpu = 0;
            Running.max_ram = 0;
            Running.max_sfc_cpu = 0;
            Running.max_sfc_ram = 0;
            Running.max_cscm_cpu = 0;
            Running.max_cscm_ram = 0;
            Running.diskSize = 0;
            Running.max_orbital_cpu = 0;
            Running.max_orbital_ram = 0;
            Running.max_casetup64_cpu = 0;
            Running.max_casetup64_ram = 0;
            Running.max_connectivitytool_cpu = 0;
            Running.max_connectivitytool_ram = 0;
            Running.max_creport_cpu = 0;
            Running.max_creport_ram = 0;
            Running.max_freshclamwrap_cpu = 0;
            Running.max_freshclamwrap_ram = 0;
            Running.max_freshclam_cpu = 0;
            Running.max_freshclam_ram = 0;
            Running.max_ipsupporttool_cpu = 0;
            Running.max_ipsupporttool_ram = 0;
            Running.max_iptray_cpu = 0;
            Running.max_iptray_ram = 0;
            Running.max_updater_cpu = 0;
            Running.max_updater_ram = 0;
            CPUUsageText.Text = "0 %";
            cscmCPUUSageText.Text = "0 %";
            orbitalCPUUsageText.Text = "0 %";
            TotalCPUText.Text = "0 %";
            sfcMaxCPUText.Text = "0 %";
            cscmMaxCPUText.Text = "0 %";
            orbitalMaxCPUText.Text = "0 %";
            MaxCPUText.Text = "0 %";
            sfcRAMText.Text = "0 MB";
            cscmRAMText.Text = "0 MB";
            orbitalRAMText.Text = "0 MB";
            TotalMemoryText.Text = "0 MB";
            sfcMaxRAMText.Text = "0 MB";
            cscmMaxRAMText.Text = "0 MB";
            orbitalMaxRAMText.Text = "0 MB";
            MaxRAMText.Text = "0 MB";
            TotalDiskText.Text = "0 MB";
        }
        public void pullXMLFileInfo()
        {
            //Create lists for XML traversal
            List<string> build_list = new List<string> { "agent", "revision" };
            List<string> policy_uuid_list = new List<string>{ "policy", "uuid"};
            List<string> policy_name_list = new List<string>{ "policy", "name"};
            List<string> policy_serial_list = new List<string> { "policy", "serial_number" };
            List<string> tetra_version_list = new List<string> { "tetra", "defversions" };
            List<string> network_list = new List<string> { "nfm", "enable" };
            List<string> MAP_list = new List<string> { "heuristic", "enable" };
            List<string> script_protection_list = new List<string> { "amsi", "enable" };
            List<string> SPP_list = new List<string> { "selfprotect", "spp" };
            List<string> exprev_list = new List<string> { "exprev", "enable" };
            List<string> exprev4_options_list = new List<string> { "v4", "options" };
            List<string> behavioral_protection_list = new List<string> { "apde", "enable" };
            List<string> tetra_list = new List<string> { "tetra", "enable" };
            List<string> orbital_list = new List<string> { "orbital", "enablemsi" };

            // Create and read from the XML file(s)
            XmlDocument policy_xml = new XmlDocument();
            policy_xml.Load($"{Running.path}/policy.xml");
            XmlDocument global_xml = new XmlDocument();
            global_xml.Load($"{Running.path}/{Running.current_version}/global.xml");
            XmlDocument local_xml = new XmlDocument();
            local_xml.Load($"{Running.path}/local.xml");
            string build = parseXML(global_xml, build_list, 0);
            string policy_uuid = parseXML(policy_xml, policy_uuid_list, 0);
            string policy_name = parseXML(policy_xml, policy_name_list, 0);
            string policy_serial = parseXML(policy_xml, policy_serial_list, 0); 
            try { Running.tetra_def_version = parseXML(local_xml, tetra_version_list, 1); }
            catch { Running.tetra_def_version = "0"; }
            string network = parseXML(policy_xml, network_list, 1);
            string MAP = parseXML(policy_xml, MAP_list, 0);
            string script_protection = parseXML(policy_xml, script_protection_list, 0);
            string SPP = parseXML(policy_xml, SPP_list, 1);
            string exprev = parseXML(policy_xml, exprev_list, 0);
            string exprev4_options = parseXML(policy_xml, exprev4_options_list, 0);
            string behavioral_protection = parseXML(policy_xml, behavioral_protection_list, 0);
            string tetra = parseXML(policy_xml, tetra_list, 0);
            string orbital = parseXML(policy_xml, orbital_list, 0);

            // Populate the GUI
            VersionText.Text = $"{Running.current_version}.{build}";
            PolicyNameText.Text = policy_name;
            PolicyUUIDText.Text = policy_uuid;
            PolicySerialText.Text = policy_serial;
            try 
            {
                TETRAVersionText.Text = $"{Running.tetra_def_version.Split(':')[1]}"; 
            }
            catch
            {
                TETRAVersionText.Text = "Not yet available";
            }

            // Populate engines
            FileScanRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); 
            if (network == "1") { NetworkScanRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (MAP == "1") { MAPRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (script_protection == "1") { ScriptProtectionRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (SPP == "1") { SPPRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (exprev == "1") { ExprevRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            List<string> opts_list = new List<string>() { "0x0000012B", "0x0000033B" };
            if (opts_list.Contains(exprev4_options)){ ScriptControlRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (behavioral_protection == "1") { BehavioralProtectionRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (tetra == "1" & Directory.Exists($"{Running.path}\\tetra") & Running.tetra_def_version != "0") 
                {    
                TETRARect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); 
            }
            if (orbital == "1" & File.Exists($"{Running.orbital_path}\\orbital.exe")) 
            { 
                OrbitalRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); 
                Running.orbital = 1; 
            }
        }
        public string parseXML(XmlDocument xml, List<string> list, int depth)
        {
            XmlNodeList XMLNodeList = xml.GetElementsByTagName(list[0]);
            try
            {
                string item1 = XMLNodeList[depth][list[1]].InnerText;
                return item1;
            }
            catch (NullReferenceException ex) { return "0"; }
        }

    }
}
