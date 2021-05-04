using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows.Media;
using System.Threading;
using System.Management;

namespace CiscoSecureEndpointResourceMonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var path = "C:/Program Files/Cisco/AMP";
            // Pull all subdirectories from the AMP directory and parse for version numbers
            var directories = Directory.GetDirectories(path);
            var current_version = showMatch(directories, @"\d{1,2}\.\d{1,2}\.\d{1,2}");
            pullXMLFileInfo(path, current_version);
            Running.running = true;
        }
        static class Running
        {
            public static bool running;
        }
        public void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
            StatusText.Text = "Running";
            Running.running = true;
            Thread thread = new Thread(new ThreadStart(parseProcesses));
            thread.Start();
        }
        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            Running.running = false;
            StatusText.Text = "Press Start to Continue";
        }
        public void parseProcesses()
        {
                var perfCounter = new PerformanceCounter("Process", "% Processor Time", "sfc");

                // Initialize to start capturing
                perfCounter.NextValue();

                while (Running.running == true)
                {
                    // give some time to accumulate data
                    Thread.Sleep(1000);

                    float current_cpu = perfCounter.NextValue() / Environment.ProcessorCount;
                Trace.WriteLine(current_cpu);
                }
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
