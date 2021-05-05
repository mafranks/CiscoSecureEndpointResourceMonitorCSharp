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
            // This youtube video is amazing for BackgroundWorker explanation!
            // https://www.youtube.com/watch?v=snkDYT1Qz6g Thanks Zaheer Sani!
            backgroundWorker1 = new BackgroundWorker();
            //Add DoWork Event Handler
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            //Add ProgressChanged Event Hanlder *** THIS ALLOWS YOU TO UPDATE THE GUI THREAD ***
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            //Add RunWorkerCompleted Event Handler ** NOT USING THIS SINCE MY LOOP IS ENDLESS**
            //backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
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
                var sfcperfCounter = new PerformanceCounter("Process", "% Processor Time", "sfc");
                var cscmperfCounter = new PerformanceCounter("Process", "% Processor Time", "cscm");
                var orbitalperfCounter = new PerformanceCounter("Process", "% Processor Time", "orbital");
                sfcperfCounter.NextValue();
                cscmperfCounter.NextValue();
                orbitalperfCounter.NextValue();

                // Give some time to accumulate data
                Thread.Sleep(1000);

                // Divide the cpu response by the number of processors
                float current_sfc_cpu = sfcperfCounter.NextValue() / Environment.ProcessorCount;
                float current_cscm_cpu = cscmperfCounter.NextValue() / Environment.ProcessorCount;
                float current_orbital_cpu = orbitalperfCounter.NextValue() / Environment.ProcessorCount;
                Trace.WriteLine($"sfc CPU: {current_sfc_cpu}");
                Trace.WriteLine($"cscm CPU: {current_cscm_cpu}");
                Trace.WriteLine($"orbital CPU: {current_orbital_cpu}");
                backgroundWorker1.ReportProgress(0, new Tuple<float, float, float>(current_sfc_cpu, current_cscm_cpu, current_orbital_cpu));
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var args = (Tuple<float, float, float>)e.UserState;
            this.CPUUsageText.Text = $"{args.Item1.ToString()} %";
            this.cscmCPUUSageText.Text = $"{args.Item2.ToString()} %";
            this.orbitalCPUUsageText.Text = $"{args.Item3.ToString()} %";
        }
        static class Running
        {
            public static bool running;
            //public static float current_cpu;
        }
        public void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
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
            StatusText.Text = "Press Start to Continue";
            backgroundWorker1.CancelAsync();
            Running.running = false;
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
            if (orbital == "1") { OrbitalRect.Fill = new SolidColorBrush(Color.FromRgb(51, 165, 50)); }
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
