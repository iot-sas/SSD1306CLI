using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using SSD1306;
using SSD1306.Fonts;
using SSD1306.I2CPI;

namespace SSD1306CLI
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            IFont font = null;

            bool flip = false;
            bool ipwait = false;
            bool proportional = false;
            var lines = new List<string>();

            if (args.Length == 0)
            {
                DisplayHelp();
                return;
            }
    
            for (int i = 0; i < args.Length; i++)
            {
                var argLow = args[i].ToLower();
                switch (argLow)
                {
                    case "--flip":
                        flip = true;
                        break;
                    case "--ip":
                    case "-i":
                        lines.Add(getIPAddress());
                        break;
                    case "--proportional":
                    case "-p":
                        proportional = true;
                        break;
                    case "-f":
                    case "--font":
                        i++;
                        foreach (var fontType in GetFonts())
                        {
                            if (fontType.ToString().ToLower().Contains(args[i]))
                            {
                                font = (IFont)Activator.CreateInstance(fontType);
                                continue;
                            }
                        }
                        break;
                    case "--ipwait":
                        ipwait = true;
                        break;
                    case "--help":
                    case "-h":
                        DisplayHelp();
                        return;
                    default:
                        lines.Add(args[i]);
                        break;
                }
            }

            while (ipwait)
            {
                var ip = getIPAddress();
                if (!ip.StartsWith("0.") && !ip.StartsWith("127"))
                {
                    lines.Add(ip);
                    ipwait = false;
                    break;
                }
                Thread.Sleep(500);
            }

            if (lines.Count == 0)
            {
                lines.Add(getIPAddress());
            }

            if (font == null && lines.Count == 1)
            {
                Match match = Regex.Match(lines[0], @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                if (match.Success)
                {
                    font = new DinerRegular24();
                    proportional = true;
                }
            }

            if (font == null) font = new Tahmona10();


            using (var i2cBus = new I2CBusPI("/dev/i2c-1"))
            {
                var i2cDevice = new I2CDevicePI(i2cBus, Display.DefaultI2CAddress);

                var display = new SSD1306.Display(i2cDevice, 128, 32, flip);
                display.Init();

                if (proportional)
                {
                    display.WriteLineBuffProportional(font, lines[0]);
                }
                else
                {
                    display.WriteLineBuff(font, lines.ToArray());
                }
                display.DisplayUpdate();
            }
        }
        
        static string getIPAddress()
        {
            string ipAddress = "0.0.0.0";
            
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var ip in localIPs.Where(x=>x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                ipAddress = ip.ToString();
                if (!ipAddress.StartsWith("127") && !ipAddress.StartsWith("0.") )
                {
                    return ipAddress;
                }               
            }
            return ipAddress;
        }
        
        static IEnumerable<Type> GetFonts()
        {
            var type = typeof(IFont);
            return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p)&& !p.IsInterface);
        }

        static void DisplayHelp()
        {
            Console.WriteLine(
@"SSD1306CLI <options> <line1> [line2] [line3] [line4]
--flip               Flip the orientation of the display.
--ip -i              Get IP address.
--proportional -p    Use full width of the display (one line only).
--font -f <match>    Select font (first sub-string match).
--ipwait             Wait for valid ipv4 address.
--help -h
");
        }
    }
}