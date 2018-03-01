# vedirect
Simple VE.Direct reader for C#, adapted from https://github.com/karioja/vedirect

Usage:

~~~cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mpptReader
{
    class Program
    {
        static void Main(string[] args)
        {
           var ve = new VEDirect("COM3", 10000);
           ve.read_data_callback(ve.print_data_callback);
        }
    }
}
~~~
