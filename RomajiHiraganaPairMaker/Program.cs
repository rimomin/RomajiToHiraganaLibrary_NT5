using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace RomajiHiraganaPairMaker
{
    class Program
    {
        static int Main(string[] args)
        {
            const string replaceContentFormat = "new KeyValuePair<string, string>(\"{0}\",\"{1}\"),";

            Dictionary<string, string> parsedArgs = ArgParse(args);

            string xmlFilePath; // RomajiHiraganaPairs.xml
            string sourceFilePath; // RomajiToHiragana.cs
            try
            {
                xmlFilePath = parsedArgs["dict"];
                sourceFilePath = parsedArgs["out"];
            }
            catch (KeyNotFoundException)
            {
                Console.Error.WriteLine("Fatal: Required option(s) not found.");
                ShowHelp();
                return -1;
            }

            List<RomajiHiraganaPair> romajiHiraganaPairs = LoadRomajiHiraganaPair(xmlFilePath);
            List<string> replaceContents = new List<string>(romajiHiraganaPairs.Count);

            foreach (var romajiHiraganaPair in romajiHiraganaPairs)
            {
                string line = romajiHiraganaPair.ToString(replaceContentFormat);
                replaceContents.Add(line);
            }
            SourceGenerator(sourceFilePath, replaceContents);

            return 0;
        }

        private static void ShowHelp()
        {
            string appName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Console.Error.WriteLine("Usages: {0} /dict xmlfile /out sourcefile", appName);
            Console.Error.WriteLine("xmlfile   : xml dictionary path");
            Console.Error.WriteLine("sourcefile: output source code file path");
        }

        private static Dictionary<string, string> ArgParse(string[] args)
        {
            string key = "";
            var parsedArgs = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                if (arg[0] == '/')
                {
                    key = arg.Substring(1);
                }
                else if (key != "")
                {
                    parsedArgs.Add(key, arg);
                }
            }

            return parsedArgs;
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

            // ローマ字の比較は小文字で行うため、すべて小文字に統一する。
            foreach(var content in romajiHiraganaPairs.Contents){
                content.Key = content.Key.ToLowerInvariant();
            }

            // 文字数の多い順に置換する必要がある。
            romajiHiraganaPairs.Contents.Sort((x, y) => {
                // .OrderByDescending(x => x.Key.Length
                var diff = y.Key.Length - x.Key.Length;
                if (diff != 0) return diff;
                // .ThenBy(x => x.Romaji)
                for (var i = 0; i < x.Key.Length; i++)
                {
                    diff = x.Key[i] - y.Key[i];
                    if (diff != 0) return diff;
                }
                return 0;
            });

            return romajiHiraganaPairs.Contents;
        }

        private static int AssumeReplaceStringSize(List<RomajiHiraganaPair> romajiHiraganaPairs, int appendLengthPerLine)
        {
            if (romajiHiraganaPairs.Count == 0) return 0;
            return romajiHiraganaPairs.Count * (romajiHiraganaPairs[0].Key.Length + romajiHiraganaPairs[1].Value.Length + appendLengthPerLine);
        }

        private static void SourceGenerator(string filePath, IEnumerable<string> replaceContents)
        {
            const string regionStartMarker = "#region romajiHiraganaPairs";
            const string regionEndMarker = "#endregion";  

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                StringBuilder fileContent = new StringBuilder();         

                var streamReader = new StreamReader(fs);

                // 書き換える領域の開始まで読み取る。
                var innerRegion = false;
                var indentCount = 0;
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    fileContent.Append(line);
                    fileContent.Append("\r\n");
                    if (line == regionStartMarker)
                    {
                        innerRegion = true;
                        break;
                    }
                    indentCount = 4;
                    foreach (var c in line)
                    {
                        if (c == ' ') indentCount++;
                        else break;
                    }
                }

                // 書き換える領域が見つからなかった場合は何もせずに終了する。
                if (!innerRegion) return;
                
                // 書き換え後の領域に書き込む予定の文字列を生成
                string indent = "".PadLeft(indentCount, ' ');
                bool hasChange = false;
                foreach (var replaceContent in replaceContents)
                {
                    string newline = indent + replaceContent;

                    // 書き換え前と同一かを確認
                    // 書き換え後のデータがまだある段階でファイル終端に達するか、領域が終了した場合、変更ありとみなす。
                    if (streamReader.EndOfStream || !innerRegion)
                    {
                        hasChange = true;
                    }
                    else if (!hasChange)
                    {
                        string oldline = streamReader.ReadLine();
                        if (oldline != newline) hasChange = true;
                        if (oldline == regionEndMarker) innerRegion = false;
                    }

                    fileContent.Append(indent);
                    fileContent.Append(replaceContent);
                    fileContent.Append("\r\n");
                }

                // 書き換え前後で変更がない場合は何もせずに終了する。
                if (!hasChange) return;

                // 末尾までファイルを読み込み、書き換え用に結合する。
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    if (line == regionEndMarker)
                    {
                        innerRegion = false;
                    }
                    if (!innerRegion)
                    {
                        fileContent.Append(line);
                        fileContent.Append("\r\n");
                    }
                }

                // 書き換え後の内容でファイルを上書きする。
                // BOMのため3byte飛ばす。
                fs.Position = 3;
                fs.SetLength(3);
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
