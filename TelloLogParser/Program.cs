using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelloLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = File.ReadAllBytes("F:/Projects/Tello/droneLog/1526178686416.dat");
            var allLines = new List<string>();
            //skip header and parse rest. 
            var records = TelloLib.TelloLog.Parse(data.Skip(0x100).ToArray());

            foreach(var record in records)
            {
                //if (record.id == 0X1D)//new_mvo 
                {
                    foreach (var field in record.fields)
                    {
                        var str = string.Format("{0}.{1} = {2}", record.name, field.name, field.value);
                        Console.WriteLine(str);
                    }
                }
            }

        }
    }
}
