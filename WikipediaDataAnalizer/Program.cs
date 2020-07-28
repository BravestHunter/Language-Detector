using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace WikipediaDataAnalizer
{
    class Program
    {
        static List<char> LatinCharacters = new List<char> {
            'a',
            'ä',
            'b',
            'c',
            'd',
            'e',
            'f',
            'g',
            'h',
            'i',
            'j',
            'k',
            'l',
            'm',
            'n',
            'o',
            'ö',
            'p',
            'q',
            'r',
            's',
            'ß',
            't',
            'u',
            'ü',
            'v',
            'w',
            'x',
            'y',
            'z'
        };
        static List<char> CyrillicCharacters = new List<char> {
            'а',
            'б',
            'в',
            'г',
            'д',
            'е',
            'ё',
            'ж',
            'з',
            'и',
            'к',
            'л',
            'м',
            'н',
            'о',
            'п',
            'р',
            'с',
            'т',
            'у',
            'ф',
            'х',
            'ц',
            'ч',
            'ш',
            'щ',
            'ъ',
            'ы',
            'ь',
            'э',
            'ю',
            'я'
        };
        static List<char> StandartPunctuationsCharacters = new List<char>
        {
            '.',
            ',',
            '!',
            '?',
            ';',
            ';',
            '\'',
            '"',
            '(',
            ')'
        };

        static void Main(string[] args)
        {
            args = new string[] {"bg", "PlainText", @"E:\AnalysInfo\Bulgarian\articles", @"E:\AnalysInfo\BulgarianResultPlain.txt" };
            /* 
             * Arg0 - short IETF language code
             * Arg1 - InputMode - Xml or plain text parsing
             * Arg2 - data source path
             * Arg3 - path for result
             * Arg4 - (optional)SpeedMode - Normal or Fast
            */

            try
            {
                if (args.Length < 4)
                    throw new Exception("Wrong arguments");

                string language_code = args[0];
                InputMode input_mode = (InputMode)Enum.Parse(typeof(InputMode), args[1]);
                string from_path = args[2];
                string to_path = args[3];
                SpeedMode speed_mode = SpeedMode.Normal;
                if (args.Length > 4)
                    speed_mode = (SpeedMode)Enum.Parse(typeof(SpeedMode), args[4]);

                List<char> chars = new List<char>();
                switch(language_code)
                {
                    case "en":
                    case "fr":
                    case "de":
                        chars = LatinCharacters.Concat(StandartPunctuationsCharacters).ToList();
                        break;
                    case "ru":
                    case "bg":
                        chars = CyrillicCharacters.Concat(StandartPunctuationsCharacters).ToList();
                        break;
                }


                LanguageAnalizer analizer = new LanguageAnalizer(chars, from_path, to_path, InputMode.PlainText, SpeedMode.Normal);
                analizer.Analize();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception - {}", ex.Message);
            }
        }
    }
}
