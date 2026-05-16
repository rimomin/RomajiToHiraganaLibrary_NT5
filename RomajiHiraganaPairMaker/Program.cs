using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace RomajiHiraganaPairMaker
{
    enum ReturnCode
    {
        Success,
        ArgumentError,
        InputFileError,
        OutputFileError
    }

    enum SourceGeneratorResult
    {
        Success,
        Same,
        NoResion,
        NoRegionEnd
    }
    class Program
    {
        /// <summary>
        /// プログラムのエントリーポイント
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        /// <returns>正常終了時は0。コマンドライン引数が不正な場合は-1を返す。</returns>
        static int Main(string[] args)
        {
            const string replaceContentFormat = "new KeyValuePair<string, string>(\"{0}\",\"{1}\"),";
            const string regionName = "romajiHiraganaPairs";

            Dictionary<string, string> parsedArgs = ArgParse(args);

            // コマンドライン引数からxmlファイルとソースコードファイルのパスを得ます。
            string xmlFilePath; // RomajiHiraganaPairs.xml
            string sourceFilePath; // RomajiToHiragana.cs
            try
            {
                xmlFilePath = parsedArgs["dict"];
                sourceFilePath = parsedArgs["out"];
            }
            catch (KeyNotFoundException)
            {
                Console.Error.WriteLine("fatal: Required option(s) not found.");
                ShowHelp();
                return (int)ReturnCode.ArgumentError;
            }            

            // xmlファイルからRomajiHiraganaPairのリストを取得します。
            List<RomajiHiraganaPair> romajiHiraganaPairs;
            try
            {
                romajiHiraganaPairs = LoadRomajiHiraganaPair(xmlFilePath);
            }
            catch (Exception e)
            {
                if(e is InvalidOperationException) Console.Error.WriteLine("fatal: Invalid format file: " + xmlFilePath);
#if DEBUG
                else if (!(e is DirectoryNotFoundException || e is FileNotFoundException)) throw;
#endif
                Console.Error.WriteLine("fatal: " + e.Message);

                return (int)ReturnCode.InputFileError;
            }

            List<string> replaceContents = new List<string>(romajiHiraganaPairs.Count);
            foreach (var romajiHiraganaPair in romajiHiraganaPairs)
            {
                string line = romajiHiraganaPair.ToString(replaceContentFormat);
                replaceContents.Add(line);
            }

            // ソースコード領域内で領域外とインデント数を変える指定がある場合取得します。
            int regionIndentAddition;
            {
                string indentOffset;
                parsedArgs.TryGetValue("indentoffset", out indentOffset);
                int.TryParse(indentOffset, out regionIndentAddition);
            }

            // ソースコードファイルへ書き出します。
            SourceGeneratorResult sourceGeneratorResult;
            try
            {
                sourceGeneratorResult = SourceGenerator(sourceFilePath, regionName, replaceContents, regionIndentAddition);
            }
            catch (Exception e)
            {
#if DEBUG
                if (!(e is DirectoryNotFoundException || e is FileNotFoundException)) throw;
#endif
                Console.Error.WriteLine("fatal: " + e.Message);

                return (int)ReturnCode.OutputFileError;
            }
            switch (sourceGeneratorResult)
            {
                case SourceGeneratorResult.NoResion:
                    Console.Error.WriteLine("warning: " + regionName + " region is not found in " + sourceFilePath + " so not changed.");
                    break;
                case SourceGeneratorResult.NoRegionEnd:
                    Console.Error.WriteLine("error: " + regionName + " region is not end in " + sourceFilePath + " so not changed.");
                    break;
                case SourceGeneratorResult.Same:
                    Console.Error.WriteLine("info: " + regionName + " region is same as " + sourceFilePath + " so not changed.");
                    break;
            }

            return (int)ReturnCode.Success;
        }

        /// <summary>
        /// 使い方を表示します。
        /// </summary>
        private static void ShowHelp()
        {
            string appName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Console.WriteLine("Usages: {0} /dict xmlfile /out sourcefile [/indentoffset indentoffset]", appName);
            Console.WriteLine("xmlfile   : xml dictionary path");
            Console.WriteLine("sourcefile: output source code file path");
            Console.WriteLine("indentoffset: put more offset indent in region than just before line this region.");
        }

        /// <summary>
        /// コマンドライン引数を解析します。
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        /// <returns>オプション名と値の辞書</returns>
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

        /// <summary>
        /// ローマ字ひらがな対応表を読み込みます。
        /// </summary>
        /// <param name="filePath">読み込むXMLファイルのパス</param>
        /// <returns>ローマ字とひらがなのペアのリスト</returns>
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

        /// <summary>
        /// ソースコードの指定領域の内容を置き換えます
        /// </summary>
        /// <param name="resionName">領域名</param>
        /// <param name="filePath">ソースコードのパス(UTF-8 BOM付き)</param>
        /// <param name="replaceContents">置き換える内容</param>
        /// <param name="regionIndentAddition">領域直前の字下げに加算する領域内での字下げ文字数</param>
        private static SourceGeneratorResult SourceGenerator(string filePath, string regionName, IEnumerable<string> replaceContents, int regionIndentAddition)
        {
            string regionStartMarker = "#region " + regionName;
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
                    fileContent.AppendLine(line);
                    if (line == regionStartMarker)
                    {
                        innerRegion = true;
                        break;
                    }
                    // 領域開始前の行のインデント+regionIndentAdditionを領域のインデントとする
                    indentCount = regionIndentAddition;
                    foreach (var c in line)
                    {
                        if (c == ' ') indentCount++;
                        else break;
                    }
                }

                // 書き換える領域が見つからなかった場合は、何もせずに終了する。
                if (!innerRegion) return SourceGeneratorResult.NoResion;
                
                // 書き換え後の領域に書き込む予定の文字列を生成
                string indent = "".PadLeft(indentCount, ' ');
                bool hasChange = false;
                foreach (var replaceContent in replaceContents)
                {
                    string newline = indent + replaceContent;

                    // 書き換え前と同一かを確認
                    // 書き換え後のデータがまだある段階で領域が終了した場合、変更ありとみなす。
                    if (!innerRegion)
                    {
                        hasChange = true;
                    }
                    // 領域が閉じていない場合は、何もせずに終了する。
                    else  if (streamReader.EndOfStream)
                    {
                        return SourceGeneratorResult.NoRegionEnd;
                    }
                    else if (!hasChange)
                    {
                        string oldline = streamReader.ReadLine();
                        if (oldline != newline) hasChange = true;
                        if (oldline == regionEndMarker) innerRegion = false;
                    }

                    fileContent.Append(indent);
                    fileContent.AppendLine(replaceContent);
                }

                // 書き換える予定の内容をすべて読み込み終わっても、まだ領域内にいる場合
                if (innerRegion)
                {
                    // 領域が閉じていない場合は、何もせずに終了する。
                    if (streamReader.EndOfStream)
                    {
                        return SourceGeneratorResult.NoRegionEnd;
                    }
                    else
                    {
                        // 次の行で領域を出るか確認し、出ない場合は変更ありとみなす。
                        string line = streamReader.ReadLine();
                        if (line == regionEndMarker) innerRegion = false;
                        else hasChange = true;
                    }
                }

                // 書き換え前後で変更がない場合は何もせずに終了する。
                if (!hasChange) return SourceGeneratorResult.Same;

                // 書き換え後の内容に領域終端を置く
                fileContent.AppendLine(regionEndMarker);

                // 末尾までファイルを読み込む
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    // 領域をでるまでは読み飛ばす。
                    if (innerRegion)
                    {
                        // 領域を出たか判定
                        if (line == regionEndMarker) innerRegion = false;
                    }
                    // 領域後の内容は書き換え後のファイルにも反映するため取得する。
                    else
                    {
                        fileContent.AppendLine(line);
                    }
                }

                // 領域が閉じていない場合は、何もせずに終了する。
                if (innerRegion) return SourceGeneratorResult.NoRegionEnd;

                // UTF-8のBOMは書き換えないため、先頭から3byte分飛ばす。
                fs.Position = 3;
                fs.SetLength(3);
                // 書き換え後の内容でファイルを上書きする。
                var streamWriter = new StreamWriter(fs);
                streamWriter.Write(fileContent);
                streamWriter.Flush();

                return SourceGeneratorResult.Success;
            }
        }

        /// <summary>
        /// XMLに不明な不明なノードがあった場合にその名前と内容を表示します。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Console.Error.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        /// <summary>
        /// XMLに不明な不明な属性があった場合にその属性と値を表示します。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.Error.WriteLine("Unknown attribute " + attr.Name + "='" + attr.Value + "'");
        }


    }
}
