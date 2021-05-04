using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace CiscoSecureEndpointResourceMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var path = "C:/Program Files/Cisco/AMP";
            // Pull all subdirectories from the AMP directory and parse for version numbers
            var directories = Directory.GetDirectories(path);
            var current_version = showMatch(directories, @"\d{1,2}\.\d{1,2}\.\d{1,2}");

            //Create lists for XML traversal
            List<string> build_list = new List<string> { "agent", "revision" };
            List<string> policy_uuid_list = new List<string>{ "policy", "uuid"};
            List<string> policy_name_list = new List<string>{ "policy", "name"};
            List<string> policy_serial_list = new List<string> { "policy", "serial_number" };
            List<string> tetra_version_list = new List<string> { "tetra", "defversions" };

            // Create and read from the XML file(s)
            XmlDocument policy_xml = new XmlDocument();
            policy_xml.Load($"{path}/policy.xml");
            XmlDocument global_xml = new XmlDocument();
            global_xml.Load($"{path}/{current_version}/global.xml");
            XmlDocument local_xml = new XmlDocument();
            local_xml.Load($"{path}/local.xml");
            string build = parseXML(global_xml, build_list);
            string policy_uuid = parseXML(policy_xml, policy_uuid_list);
            string policy_name = parseXML(policy_xml, policy_name_list);
            string policy_serial = parseXML(policy_xml, policy_serial_list);
            // TETRA has two entries.  Need to figure a way around this.
//            string tetra_version = parseXML(local_xml, tetra_version_list);
//            Trace.WriteLine(tetra_version);

            // Populate the GUI
            VersionText.Text = $"{current_version}.{build}";
            PolicyNameText.Text = policy_name;
            PolicyUUIDText.Text = policy_uuid;
            PolicySerialText.Text = policy_serial;
//            TETRAVersionText.Text = tetra_version;
        }
        private string parseXML(XmlDocument xml, List<string> list)
        {
            XmlNodeList XMLNodeList = xml.GetElementsByTagName(list[0]);
            string item1 = XMLNodeList[0][list[1]].InnerText;
            return item1;

            /*** DO NOT DELETE UNTIL YOU HAVE A VALID REPLACEMENT!!
            // Create XML from file and parse based on required path
            XmlDocument xml = new XmlDocument();
            xml.Load(path);
            XmlNodeList XMLNodeList = xml.GetElementsByTagName("business");
            string bus_uuid = XMLNodeList[0]["uuid"].InnerText;
            XmlNodeList XMLNodeList2 = xml.GetElementsByTagName("policy");
            string policy_uuid = XMLNodeList2[0]["uuid"].InnerText;
            string policy_serial = XMLNodeList2[0]["serial_number"].InnerText;
            string policy_name = XMLNodeList2[0]["name"].InnerText;
            ***/
        }


            private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Enabled/Disable button if the other is pressed
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
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
