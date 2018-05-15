﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Taken from:
// https://stackoverflow.com/questions/8241060/how-to-get-little-endian-data-from-big-endian-in-c-sharp-using-bitconverter-toin

namespace spywin
{
    public static class BigEndian
    {
        public static short ToBigEndian(this short value)
        {
            return System.Net.IPAddress.HostToNetworkOrder(value);
        }
        public static int ToBigEndian(this int value)
        {
            return System.Net.IPAddress.HostToNetworkOrder(value);
        }
        public static long ToBigEndian(this long value)
        {
            return System.Net.IPAddress.HostToNetworkOrder(value);
        }
        public static short FromBigEndian(this short value)
        {
            return System.Net.IPAddress.NetworkToHostOrder(value);
        }
        public static int FromBigEndian(this int value)
        {
            return System.Net.IPAddress.NetworkToHostOrder(value);
        }
        public static long FromBigEndian(this long value)
        {
            return System.Net.IPAddress.NetworkToHostOrder(value);
        }
    }
}
