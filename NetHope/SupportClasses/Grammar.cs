using NetHope.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace NetHope.SupportClasses
{
    //Class to Camelcase word
    public class Grammar
    {
        public static string Capitalise(string word)
        {
            string[] words = word.Split(' ');
            string output = "";
            foreach (string x in words)
            {
                if (Char.IsNumber(x.First()))
                {
                    output += x + " ";
                }
                else if (x == StringResources.en_And || x == StringResources.en_Of || x == StringResources.en_For || x == StringResources.en_With)
                {
                    output += x + " ";
                }
                else if (x == StringResources.en_it)
                {
                    output += StringResources.en_ITabbrev;
                }
                else
                {
                    output += x.First().ToString().ToUpper() + x.Substring(1) + " ";
                }
            }
            return output;
        }

        public static int Capitalise(int word)
        {
            return word;
        }
    }
}