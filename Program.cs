using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;

namespace EDC_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EDC-Client v0.2");
            Console.WriteLine("");
            Console.WriteLine("Enter the target ip:");
            string stringIP = Console.ReadLine();

            Console.WriteLine("");
            Console.WriteLine("Enter the target port:");
            int port = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine();
            Console.WriteLine("enter the AES-key:");
            string aesKey = Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine("Enter your alias:");
            string alias = Console.ReadLine();

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(stringIP), port);
            crypto cryptClass = new crypto("aselrias38490a32", "8947az34awl34kjq", aesKey, 25);
            Connection connection = new Connection(cryptClass, ip, alias);






            connection._getMessage += new Connection._D_getMessage(getMessage);
            connection.setup();

            Console.Clear();



            string tipped;
            while (true)
            {
                tipped = Console.ReadLine();
                connection.sendMessage(tipped);
                Console.CursorTop -= 1;

                Console.WriteLine("You:\t{0}", tipped);
                Console.WriteLine();
            }
        }


        static void getMessage(string message, string alias)
        {
            Console.WriteLine(alias + ":\t" + message);
            Console.WriteLine();
        }
    }
}
