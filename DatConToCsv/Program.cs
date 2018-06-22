using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatConToCsv
{
    class Program
    {
        public class FieldSpec
        {
            public string name;
            public int offset;
            public string type;
        }
        class RecordSpec
        {
            public string definedIn;
            public string name, id, len;
            public List<FieldSpec> fields= new List<FieldSpec>();
        }

        static void Main(string[] args)
        {
            var srcPath = "C:/Users/v-chph/Downloads/DatCon-master/DatCon/src/DatConRecs/";
            var fileNames = Directory.GetFiles(srcPath, "*.java", SearchOption.AllDirectories);

            var typeLookup = new Dictionary<string, string>
            {
                { ".getByte(", "byte" },
                { ".getUnsignedByte(", "byte" },
                { ".getUnsignedInt(", "UInt32" },
                { ".getUnsignedShort(", "UInt16" },
                { ".getFloat(", "float" },
                { ".getShort(", "short" },
                { ".getInt(", "int" },
                { ".getDouble(", "double" },
                { ".getString(", "string" },
                { ".getCleanString(", "string" }
            };
            var specString = "new RecClassSpec(";

            var recSpecs = new Dictionary<string, RecordSpec>();

            foreach (var fn in fileNames)
            {
                var lines = File.ReadAllLines(fn);
                foreach (var l in lines)
                {
                    if (l.Contains(specString))
                    {
                        if (l.Trim().StartsWith("//"))
                            continue;
                        var ia = l.IndexOf(specString) + specString.Length;
                        var ib = l.IndexOf(".class");
                        if (ib < 0)
                            continue;

                        var name = l.Substring(ia, ib - ia);
                        var parts = l.Substring(ib).Split(new char[] { ',', ')' });
                        if (parts.Length < 3)
                            continue;
                        var id = parts[1].Trim();
                        var len = parts[2].Trim();
                        Console.WriteLine("{0},{1},{2}", name, id, len);
                        var cleanName = name.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_' });
                        var dirName = Path.GetFileName(Directory.GetParent(fn).ToString());
                        recSpecs[name] = new RecordSpec { name = cleanName, id = id, len = len,
                            definedIn = dirName
                            };
                    }
                }
            }

            var allLines = new List<string>();
            var validLines = new List<string>();
            var invalidLines = new List<string>();

            validLines.Add("definedIn,groupName,groupId,groupLen,name,offset,type,validParse,sourceLine");
            foreach (var fn in fileNames)
            {
                var lines = File.ReadAllLines(fn);
                foreach (var l in lines)
                {
                    foreach (var key in typeLookup.Keys)
                    {
                        if (l.Trim().StartsWith("//"))
                            continue;
                        if (l.Contains(key))
                        {
                            var name = l.Split('=')[0].Trim();
                            var ia = l.IndexOf(key) + key.Length;
                            var ib = l.IndexOf(')', ia);
                            var offStr = l.Substring(ia, ib - ia);

                            var valid = true;
                            int off;
                            if(typeLookup[key]=="string")
                            {
                                off = 0;
                            }
                            else if (!int.TryParse(offStr, out off))
                            {
                                valid = false;
                            }
                            var groupName = Path.GetFileNameWithoutExtension(fn);
                            var recSpec = new RecordSpec { name = "unk", id = "0", len = "0" };

                            if (recSpecs.Keys.Contains(groupName))
                                recSpec = recSpecs[groupName];
                            else
                                valid = false;

                            if (name.StartsWith("double "))//special case for a few lines that are defined like this.
                                name = name.Substring("double ".Length);

                            //groupName = groupName.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_' });

                            var outLine = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", Path.GetFileName(Directory.GetParent(fn).ToString()), recSpec.name, recSpec.id, recSpec.len, name, off, typeLookup[key], valid, l.Trim());

                            if (recSpec.id == "65533")
                            {

                            }

                            if (valid)
                            {
                                var field = new FieldSpec() { name = name, offset = off,type= typeLookup[key] };
                                recSpec.fields.Add(field);

                                validLines.Add(outLine);
                            }
                            else
                                invalidLines.Add(outLine);
                        }
                    }

                }
            }
            var destPath = "../../../TelloLib/";
            File.WriteAllLines(destPath + "parsedRecSpecs.csv", validLines.Concat(invalidLines).ToArray());

            string json = JsonConvert.SerializeObject(recSpecs.Values.ToArray(),Formatting.Indented);
            File.WriteAllText(destPath + "parsedRecSpecs.json", json);

            //var xx = JsonConvert.DeserializeObject(json);

        }
    }
}
