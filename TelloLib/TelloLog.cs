using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelloLib
{
    public class TelloLog
    {
        public class FieldSpec
        {
            public string name { get; set; }
            public int offset { get; set; }
            public string type { get; set; }
            public Object value { get; set; }
        }
        public class RecordSpec
        {
            public string definedIn { get; set; }
            public string name { get; set; }
            public int id { get; set; }
            public int len { get; set; }
            public List<FieldSpec> fields { get; set; }

            public override string ToString()
            {
                var str ="";
                foreach(var field in fields)
                {
                    str += string.Format("{0}.{1} = {2}\n", this.name, field.name, field.value);
                }
                return str;
            }
        }
        static RecordSpec[] recordSpecs;
        static Dictionary<string, RecordSpec> recordSpecLookup= new Dictionary<string, RecordSpec>();
        static Dictionary<string, FieldSpec> fieldSpecLookup = new Dictionary<string, FieldSpec>();
        static TelloLog()
        {
            var srcPath = "f:/temp/DatCon-master/DatCon/src/DatConRecs/";
            var json = System.IO.File.ReadAllText(srcPath + "parsedRecSpecs.json");
            recordSpecs = (RecordSpec[])Newtonsoft.Json.JsonConvert.DeserializeObject(json,typeof(RecordSpec[]));
            foreach(var r in recordSpecs)
            {
                recordSpecLookup[/*r.len + "_" +*/ r.id.ToString()]=r;
                foreach(var f in r.fields)
                    fieldSpecLookup[r.name + "." + f.name] = f;

            }
        }

        static public RecordSpec[] Parse(byte[] data)
        {
            var records = new List<RecordSpec>();

            int pos = 0;
            //A packet can contain more than one record.
            while (pos < data.Length - 200)//-2 for CRC bytes at end of packet.
            {
                if (data[pos] != 'U')//Check magic byte
                {
                    pos += 1;
                    //Console.WriteLine("PARSE ERROR!!!");
                    continue;
                }
                var len = data[pos + 1];
                if (data[pos + 2] != 0)//Should always be zero (so far)
                {
                    pos += 1;
                    //Console.WriteLine("SIZE OVERFLOW!!!");
                    break;
                }
                var crc = data[pos + 3];
                //todo Check crc.

                var id = BitConverter.ToUInt16(data, pos + 4);
                var xorBuf = new byte[256];
                byte xorValue = data[pos + 6];

                var recSpecId = /*len + "_" +*/ id.ToString();
                Console.WriteLine(recSpecId);
                if (recordSpecLookup.Keys.Contains(recSpecId))
                {
                    for (var i = 0; i < len; i++)//Decrypt payload.
                        xorBuf[i] = (byte)(data[pos + i] ^ xorValue);
                    int baseOffset = 10;
                    var record = recordSpecLookup[recSpecId];
                    var fields = record.fields;
                    foreach (var field in fields)
                    {
                        switch (field.type)
                        {
                            case "byte":
                                field.value = xorBuf[baseOffset + field.offset];
                                break;
                            case "short":
                                field.value = BitConverter.ToInt16(xorBuf, baseOffset + field.offset);
                                break;
                            case "UInt16":
                                field.value = BitConverter.ToUInt16(xorBuf, baseOffset + field.offset);
                                break;
                            case "int":
                                field.value = BitConverter.ToInt32(xorBuf, baseOffset + field.offset);
                                break;
                            case "UInt32":
                                field.value = BitConverter.ToUInt32(xorBuf, baseOffset + field.offset);
                                break;
                            case "float":
                                field.value = BitConverter.ToSingle(xorBuf, baseOffset + field.offset);
                                break;
                            case "double":
                                field.value = BitConverter.ToDouble(xorBuf, baseOffset + field.offset);
                                break;
                        }
                    }
                    Console.WriteLine(record.ToString());

                    records.Add(record);
                }
                else
                {
                    Console.WriteLine("Not found:"+recSpecId+" len:"+len);

                }
                pos += len;
            }
            return records.ToArray();
        }


    }
}
