using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using CommandLine;
using Newtonsoft.Json;

namespace WikipediaDataParser
{
    class Program
    {
        static void Main(string[] args)
        {
            ParserResult<CommandLineOptions> parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);

            if (parserResult.Errors.Count() > 0)
            {
                return;
            }

            try
            {
                CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture(parserResult.Value.LanguageCode);
                var fileFormat = (WikipediaArticleParser.FileFormat)Enum.Parse(typeof(WikipediaArticleParser.FileFormat), parserResult.Value.FileFormat, true);

                var parser = new WikipediaArticleParser(cultureInfo);
                WikipediaArticleParser.ParseResult result = parser.Parse(parserResult.Value.SourcePath, fileFormat);

                WriteResult(result, parserResult.Value.OutputPath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception - {e.ToString()}");
            }
        }

        private static void WriteResult(WikipediaArticleParser.ParseResult result, string outPath)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(outPath, FileMode.Create), Encoding.Unicode))
            {
                foreach (char c in JsonConvert.SerializeObject(result))
                {
                    writer.Write(c);
                }
            }
        }
    }
}
