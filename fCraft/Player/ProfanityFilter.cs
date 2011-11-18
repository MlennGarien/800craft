
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace fCraft
{
    static class ProfanityFilter
    {
        private static Dictionary<string, string> RegexReduce;
        private static IEnumerable<string> SwearWords;
        public static void Init()
        {
            // Initializes the reduction dictionary and word list
            RegexReduce = new Dictionary<string, string>();
            RegexReduce.Add("a", "[@]");
            RegexReduce.Add("b", "i3|l3");
            RegexReduce.Add("c", "[(]");
            RegexReduce.Add("e", "[3]");
            RegexReduce.Add("f", "ph");
            RegexReduce.Add("g", "[6]");
            RegexReduce.Add("h", "#");
            // Because Is and Ls are similar, the swear list will contain a lowercase I instead of Ls.
            // For example, the word "asshole" would be saved as "asshoie".
            RegexReduce.Add("i", "[l!1]");
            RegexReduce.Add("o", "[0]");
            RegexReduce.Add("q", "[9]");
            RegexReduce.Add("s", "[$5]");
            RegexReduce.Add("w", "vv");
            RegexReduce.Add("z", "[2]");

            // Load/create the SwearWords.txt file and import them into the SwearWords list
            if (!File.Exists("SwearWords.txt"))
            {
                // No file exists yet, so let's create one
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# This file contains a list of bad words to remove via the profanity filter");
                sb.AppendLine("# Each bad word should be on a new line all by itself");
                File.WriteAllText("SwearWords.txt", sb.ToString());
            }

            // OK the file should exist now
            var tempSwearWords = File.ReadAllLines("SwearWords.txt").Where(s => s.StartsWith("#") == false || s.Trim().Equals(String.Empty));

            // Run the SwearWords through the reducer to ensure things like Ls become Is and everything is lowercase
            // Also remove lines starting with a "#" since they are comments
            SwearWords = from s in tempSwearWords where !s.StartsWith("#") select Reduce(s.ToLower());
        }

        public static string Parse(string text)
        {
            //return ParseMatchWholeWords(text);
            return ParseMatchPartialWords(text);
        }

        // Replace bad words only if the whole word matches
        private static string ParseMatchWholeWords(string text)
        {
            var result = new List<string>();
            var originalWords = text.Split(' ');
            var reducedWords = Reduce(text).Split(' ');
            for (var i = 0; i < originalWords.Length; i++)
            {
                if (SwearWords.Contains(reducedWords[i].ToLower()))
                {
                    // A reduced word matched a bad word from our file!
                    result.Add(new String('*', originalWords[i].Length));
                }
                else
                {
                    result.Add(originalWords[i]);
                }
            }

            return String.Join(" ", result.ToArray());
        }

        // Replace any whole word containing a bad word inside it (including partial word matches)
        private static string ParseMatchPartialWords(string text)
        {
            var result = new List<string>();
            var originalWords = text.Split(' ');
            var reducedWords = Reduce(text).Split(' ');

            // Loop through each reduced word, looking for a badword
            for (int i = 0; i < reducedWords.Length; i++)
            {
                bool badwordfound = false;
                foreach (string badword in SwearWords)
                {
                    if (reducedWords[i].Contains(badword))
                    {
                        badwordfound = true;
                        break;
                    }
                }

                if (badwordfound)
                {
                    // If a badword is found anywhere in the string, replace the whole word
                    result.Add(new String('*', originalWords[i].Length));
                }
                else
                {
                    // Nothing found, so use the original word
                    result.Add(originalWords[i]);
                }
            }

            return String.Join(" ", result.ToArray());
        }


        private static string Reduce(string text)
        {
            text = text.ToLower();
            foreach (var pattern in RegexReduce)
            {
                text = Regex.Replace(text, pattern.Value, pattern.Key/*, RegexOptions.IgnoreCase*/);
            }
            return text;
        }
    }
}
