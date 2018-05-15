using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace spywin
{
    class Program
    {
        static void Main(string[] args)
        {
            SpyClient client = new SpyClient();
            client.startClient();
            Console.WriteLine("Koniec programu");
            Console.Read();
        }
    }
}
