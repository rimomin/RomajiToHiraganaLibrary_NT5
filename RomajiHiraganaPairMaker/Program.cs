using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace RomajiHiraganaPairMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            const string xmlFilePath = @"RomajiHiraganaPairs.xml";
            const string replaceContentFormat = "new KeyValuePair<string, string>(\"{0}\",\"{1}\"),";

            List<RomajiHiraganaPair> romajiHiraganaPairs = LoadRomajiHiraganaPair(xmlFilePath);
            List<string> replaceContents = new List<string>(romajiHiraganaPairs.Count);

            foreach (var romajiHiraganaPair in romajiHiraganaPairs)
            {
                string line = romajiHiraganaPair.ToString(replaceContentFormat);
                replaceContents.Add(line);
            }
            SourceGenerator("RomajiToHiragana.cs", replaceContents);
        }

        private static List<RomajiHiraganaPair> LoadRomajiHiraganaPair(string filePath)
        {
            var xmlSerializer = new XmlSerializer(typeof(RomajiHiraganaPairs));
            xmlSerializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            xmlSerializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
            RomajiHiraganaPairs romajiHiraganaPairs;

            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                romajiHiraganaPairs = (RomajiHiraganaPairs)xmlSerializer.Deserialize(fs);
            }

            romajiHiraganaPairs.Contents.Sort((x, y) => (y.Key.Length - x.Key.Length));

            return romajiHiraganaPairs.Contents;
        }

        private static int AssumeReplaceStringSize(List<RomajiHiraganaPair> romajiHiraganaPairs, int appendLengthPerLine)
        {
            if (romajiHiraganaPairs.Count == 0) return 0;
            return romajiHiraganaPairs.Count * (romajiHiraganaPairs[0].Key.Length + romajiHiraganaPairs[1].Value.Length + appendLengthPerLine);
        }

        private static void SourceGenerator(string filePath, IEnumerable<string> replaceContents)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                StringBuilder fileContent = new StringBuilder();

                const int bufferSize = 1024;
                const string regionStartMarker = "#region romajiHiraganaPairs";
                const string regionEndMarker = "#endregion";

                byte[][] buffer = new byte[2][];
                var encoding = Encoding.GetEncoding("UTF-8");

                buffer[0] = new byte[bufferSize];
                fs.Read(buffer[0], 0, bufferSize);
                string partialText = encoding.GetString(buffer[0]);
                int startpos = partialText.IndexOf(regionStartMarker);


                int endpos = partialText.IndexOf(regionEndMarker);

                

                var streamReader = new StreamReader(fs);
                var innerRegion = false;
                var indentCount = 0;
                long insertPosition = 0;
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    if (line == "#region romajiHiraganaPairs")
                    {
                        insertPosition = fs.Position;
                        innerRegion = true;
                        break;
                    }
                    indentCount = 0;
                    foreach (var c in line)
                    {
                        if (c == ' ') indentCount++;
                        else break;
                    }
                }
                if (innerRegion)
                {
                    string indent = "".PadLeft(indentCount, ' ');
                    foreach (var replaceContent in replaceContents)
                    {
                        fileContent.Append(indent);
                        fileContent.Append(replaceContent);
                        fileContent.Append('\n');
                    }
                }
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();

                    if (!innerRegion)
                    {
                        fileContent.Append(line);
                        fileContent.Append("\n");
                    }
                    else if (line == "#endregion")
                    {
                        innerRegion = false;
                        fileContent.Append(line);
                        fileContent.Append("\n");
                    }
                }
                
                fs.Position = insertPosition;
                fs.SetLength(insertPosition);
                var streamWriter = new StreamWriter(fs);
                streamWriter.Write(fileContent);
                streamWriter.Flush();
                
            }
        }

        private static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Console.Error.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        private static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.Error.WriteLine("Unknown attribute " + attr.Name + "='" + attr.Value + "'");
        }


    }
}
