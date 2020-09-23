using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrimS.Telnet;

namespace _7DaysServer_InfoMonitor
{
    class ServerCurrentInfo
    {
        public static string SERVER = "localhost";
        public static int PORT = 8081;

        public ServerCurrentInfo()
        {

        }


        /// <summary>
        /// Sends a telnet command to the server which causes the server to save the gamestate to the current log file
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SendServerCommand(string identifier)
        {
            using (Client client = new Client(SERVER, PORT, new System.Threading.CancellationToken()))
            {
                if (client.IsConnected == false)
                    return false;
                    //System.Windows.MessageBox.Show($"Connected to: {SERVER} {PORT}");
                await client.WriteLine($"loggamestate \"{identifier}\"");
                await client.WriteLine("exit");
                //string telnetOutput = "first";
                //while (!String.IsNullOrEmpty(telnetOutput))
                //{
                //    telnetOutput = await client.ReadAsync();
                //    System.Windows.Forms.MessageBox.Show(telnetOutput);
                //}
                //telnetOutput = await client.ReadAsync();
                //System.Windows.Forms.MessageBox.Show(telnetOutput);
                //telnetOutput = await client.ReadAsync();
                //System.Windows.Forms.MessageBox.Show(telnetOutput);
                //string telnetOutput = await client.TerminatedReadAsync(">"); //Only contains server status, no day/time or players info
                //await Task.Delay(2000); //wait 2000ms to close the connection otherwise the 7d server throws a fit
                //System.Windows.MessageBox.Show($"{test1}");
                //client.Dispose(); //Shouldn't be needed, but just in case... don't want to use any unneeded ram
                //if (String.IsNullOrEmpty(telnetOutput))
                //    return false;
            }
            return true;
        }

        public string GenerateIdentifierString()
        {
            Random random = new Random();
            int num = random.Next();
            string hexString = num.ToString("X");
            return hexString;
        }


    }
}
