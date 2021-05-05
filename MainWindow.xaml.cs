using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace CiscoSecureEndpointResourceMonitor
{
    public partial class MainWindow : Window
    {
        BackgroundWorker backgroundWorker1;
        public MainWindow()
        {
            InitializeComponent();
            var path = "C:/Program Files/Cisco/AMP";
            // Pull all subdirectories from the AMP directory and parse for version numbers
            var directories = Directory.GetDirectories(path);
            var current_version = showMatch(directories, @"\d{1,2}\.\d{1,2}\.\d{1,2}");
            pullXMLFileInfo(path, current_version);
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
                sfccpuCounter.NextValue();
                cscmcpuCounter.NextValue();
                if (Running.orbital == 1)
                {
                    orbitalcpuCounter.NextValue();
                }
                // Give some time to accumulate data
                Thread.Sleep(1000);

                // Divide the cpu response by the number of processors
                float current_sfc_cpu = sfccpuCounter.NextValue() / Environment.ProcessorCount;
                float current_cscm_cpu = cscmcpuCounter.NextValue() / Environment.ProcessorCount;
                if (Running.orbital == 1)
                {
                    Running.current_orbital_cpu = orbitalcpuCounter.NextValue() / Environment.ProcessorCount;
                    Running.current_orbital_ram = orbitalramCounter.NextValue() / 1024 / 1024;
                }
                // Calculate RAM in MB
                float current_sfc_ram = sfcramCounter.NextValue() / 1024 / 1024;
                float current_cscm_ram = cscmramCounter.NextValue() / 1024 / 1024;
                
                // Get totals
                float total_cpu = current_sfc_cpu + current_cscm_cpu + Running.current_orbital_cpu;
                float total_ram = current_sfc_ram + current_cscm_ram + Running.current_orbital_ram;

                // Compare current with max and change if necessary
                if (current_sfc_cpu > Running.max_sfc_cpu) { Running.max_sfc_cpu = current_sfc_cpu; }
                if (current_sfc_ram > Running.max_sfc_ram) { Running.max_sfc_ram = current_sfc_ram; }
                if (current_cscm_cpu > Running.max_cscm_cpu) { Running.max_cscm_cpu = current_cscm_cpu; }
                if (current_cscm_ram > Running.max_cscm_ram) { Running.max_cscm_ram = current_cscm_ram; }
                if (Running.current_orbital_cpu > Running.max_orbital_cpu) { Running.max_orbital_cpu = Running.current_orbital_cpu; }
                if (Running.current_orbital_ram > Running.max_orbital_ram) { Running.max_orbital_ram = Running.current_orbital_ram; }
                if (total_cpu > Running.max_cpu) { Running.max_cpu = total_cpu; }
                if (total_ram > Running.max_ram) { Running.max_ram = total_ram; }

                // Create a set of 3 dots for a dynamic message
                if (Running.dots.Length == 1) { Running.dots = ".."; }
                else if (Running.dots.Length == 2) { Running.dots = "..."; Running.diskSize = GetDirectorySize(); }
                else { Running.dots = "."; }

                // Tuple has a limit of 7 items so you have to use a nested Tuple
                backgroundWorker1.ReportProgress(0, Tuple.Create(
                    Tuple.Create(current_sfc_cpu, current_cscm_cpu, Running.current_orbital_cpu, total_cpu),
                    Tuple.Create(Running.max_sfc_cpu, Running.max_cscm_cpu, Running.max_orbital_cpu, Running.max_cpu),
                    Tuple.Create(current_sfc_ram, current_cscm_ram, Running.current_orbital_ram, total_ram),
                    Tuple.Create(Running.max_sfc_ram, Running.max_cscm_ram, Running.max_orbital_ram, Running.max_ram),
                    Running.diskSize, Running.dots));
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var args = (Tuple<Tuple<float, float, float, float>, Tuple<float, float, float, float>,
                Tuple<float, float, float, float>, Tuple<float, float, float, float>, long, string>)e.UserState;
            this.CPUUsageText.Text = $"{args.Item1.Item1} %";
            this.cscmCPUUSageText.Text = $"{args.Item1.Item2} %";
            this.orbitalCPUUsageText.Text = $"{args.Item1.Item3} %";
            this.TotalCPUText.Text = $"{args.Item1.Item4} %";
            this.sfcMaxCPUText.Text = $"{args.Item2.Item1} %";
            this.cscmMaxCPUText.Text = $"{args.Item2.Item2} %";
            this.orbitalMaxCPUText.Text = $"{args.Item2.Item3} %";
            this.MaxCPUText.Text = $"{args.Item2.Item4} %";
            this.sfcRAMText.Text = $"{args.Item3.Item1} MB";
            this.cscmRAMText.Text = $"{args.Item3.Item2} MB";
            this.orbitalRAMText.Text = $"{args.Item3.Item3} MB";
            this.TotalMemoryText.Text = $"{args.Item3.Item4} MB";
            this.sfcMaxRAMText.Text = $"{args.Item4.Item1} MB";
            this.cscmMaxRAMText.Text = $"{args.Item4.Item2} MB";
            this.orbitalMaxRAMText.Text = $"{args.Item4.Item3} MB";
            this.MaxRAMText.Text = $"{args.Item4.Item4} MB";
            this.TotalDiskText.Text = $"{args.Item5} MB";
            this.StatusText.Text = $"Running {args.Item6}";
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
            public static long diskSize = 0;
            public static string dots = ".";
            public static int orbital = 0;
            public static float current_orbital_cpu = 0;
            public static float current_orbital_ram = 0;
            public static float max_orbital_cpu = 0;
            public static float max_orbital_ram = 0;
        }
        static long GetDirectorySize()
        {
            // Get array of all file names.
            string[] AMPfiles = Directory.GetFiles(@"C:\Program Files\Cisco\AMP", "*.*");
            
            // Calculate total bytes of all files in a loop.
            long fileBytes = 0;
            fileBytes += parseDir(AMPfiles);
            // Repeat for Orbital if it is enabled

            if (Running.orbital == 1)
            {
                string[] Orbitalfiles = Directory.GetFiles(@"C:\Program Files\Cisco\Orbital", "*.*");
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
                fileBytes += info.Length;
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
        public void pullXMLFileInfo(string path, string current_version)
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
            policy_xml.Load($"{path}/policy.xml");
            XmlDocument global_xml = new XmlDocument();
            global_xml.Load($"{path}/{current_version}/global.xml");
            XmlDocument local_xml = new XmlDocument();
            local_xml.Load($"{path}/local.xml");
            string build = parseXML(global_xml, build_list, 0);
            string policy_uuid = parseXML(policy_xml, policy_uuid_list, 0);
            string policy_name = parseXML(policy_xml, policy_name_list, 0);
            string policy_serial = parseXML(policy_xml, policy_serial_list, 0);
            string tetra_version = parseXML(local_xml, tetra_version_list, 1);
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
            VersionText.Text = $"{current_version}.{build}";
            PolicyNameText.Text = policy_name;
            PolicyUUIDText.Text = policy_uuid;
            PolicySerialText.Text = policy_serial;
            TETRAVersionText.Text = $"{tetra_version.Split(':')[1]}";

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
            if (tetra == "1") { TETRARect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
            if (orbital == "1" & Directory.Exists(@"C:\Program Files\Cisco\Orbital")) 
            { 
                OrbitalRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); 
                Running.orbital = 1; 
            }
        }
        public string parseXML(XmlDocument xml, List<string> list, int depth)
        {
            XmlNodeList XMLNodeList = xml.GetElementsByTagName(list[0]);
            string item1 = XMLNodeList[depth][list[1]].InnerText;
            return item1;
        }
        public string showMatch(string[] directories, string expr)
        {
            // Walk through each regex response for version and replace if it is higher
            var current_version = "0.0.0";
            var matches = new List<string>();
            foreach (string dir in directories)
            {
                var mc = Regex.Match(dir, expr);
                // Check for null to remove blank regex responses
                if (mc.ToString() == "") { }
                else
                {
                    var this_match = mc.ToString();
                    matches.Add(this_match);
                }
            }
            foreach (var match in matches)
            {
                string[] current_sections = current_version.Split('.');
                string[] possible_sections = match.Split('.');
                int x = Int32.Parse(current_sections[0]);
                int y = Int32.Parse(possible_sections[0]);
                if (y > x)
                {
                    current_version = match;
                }
                else if (int.Parse(possible_sections[0]) == int.Parse(current_sections[0]))
                {
                    if (int.Parse(possible_sections[1]) > int.Parse(current_sections[1]))
                    {
                        current_version = match;
                    }
                    else if (int.Parse(possible_sections[1]) == int.Parse(current_sections[1]))
                    {
                        if (int.Parse(possible_sections[2]) > int.Parse(current_sections[2]))
                        {
                            current_version = match;
                        }
                    }
                }
            } return current_version;
        }


    }
}
