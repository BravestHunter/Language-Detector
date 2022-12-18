using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace WikipediaDataParser
{
    internal static class Utils
    {
        public static readonly ImmutableArray<char> LatinCharacters = ImmutableArray.Create
        ( 
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
        );

        public static readonly ImmutableArray<char> CyrillicCharacters = ImmutableArray.Create
        (
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
        );

        public static readonly ImmutableArray<char> PunctuationCharacters = ImmutableArray.Create
        (
            '.',
            ',',
            '!',
            '?',
            ';',
            ';',
            '\'',
            '\'',
            '"',
            '(',
            ')'
        );

        public static List<char> GetCharacters(CultureInfo cultureInfo)
        {
            switch (cultureInfo.TwoLetterISOLanguageName)
            {
                case "en":
                case "fr":
                case "de":
                    return LatinCharacters.Concat(PunctuationCharacters).ToList();
                case "ru":
                case "uk":
                case "bg":
                    return CyrillicCharacters.Concat(PunctuationCharacters).ToList();
            }

            throw new NotSupportedException("Language is not supported");
        }
    }
}
