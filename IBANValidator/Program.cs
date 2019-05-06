using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IBANValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            //BE TEST VALID
            var result1 = IBANValidate.IsValid("BE26068900994429");
            Console.WriteLine(result1);

            //NL TEST VALID
            var result2 = IBANValidate.IsValid("NL91ABNA0417164300");
            Console.WriteLine(result2);

            //BE TEST INVALID
            var result3 = IBANValidate.IsValid("BE26068900991429");
            Console.WriteLine(result3);

            //NL TEST INVALID
            var result4 = IBANValidate.IsValid("NL91ABNA0417162300");
            Console.WriteLine(result4);

            Console.ReadKey();
        }
    }

    /// <summary>
    /// Class for IBAN Validate
    /// </summary>
    class IBANValidate
    {
        private static string Code { get; set; }
        private static int Length { get; set; }
        private static string Structure { get; set; }

        /// <summary>
        /// Enums for return results.
        /// </summary>
        public enum ValidationResult
        {
            IsValid,
            IsNotValid,
            CountryCodeNotKnown
        }

        public IBANValidate() { }

        /// <summary>
        /// Main method for IBAN Validation
        /// </summary>
        /// <param name="iban"></param>
        /// <returns></returns>
        public static ValidationResult IsValid(string iban)
        {
            Code = iban.Substring(0, 2).ToUpper();
            Length = iban.Length;

            Specification specification = new Specification();

            if (specification.IBANSpecifications.Find(x => x.CountryCode == Code && x.Length == Length) == null)
                return ValidationResult.CountryCodeNotKnown;

            Structure = specification.IBANSpecifications.Find(x => x.CountryCode == Code && x.Length == Length).Structure;
            Regex regex = ParseStructure(Structure);

            if (regex.IsMatch(iban.Substring(4)) && Iso7064Mod97_10(Iso13616Prepare(iban)) == 1)
                return ValidationResult.IsValid;

            return ValidationResult.IsNotValid;

        }

        /// <summary>
        /// Calculates the MOD 97 10 of the passed IBAN as specified in ISO7064.
        /// </summary>
        /// <param name="iban"></param>
        /// <returns></returns>
        private static int Iso7064Mod97_10(string iban)
        {
            string remainder = iban, block;

            while (remainder.Length > 2)
            {
                try { block = remainder.Substring(0, 9); }
                catch { block = remainder; }

                remainder = Convert.ToInt32(block, 10) % 97 + remainder.Substring(block.Length);
            }

            return Convert.ToInt32(remainder, 10) % 97;
        }

        /// <summary>
        /// Prepare an IBAN for mod 97 computation by moving the first 4 chars to the end and transforming the letters to
        /// numbers(A = 10, B = 11, ..., Z = 35), as specified in ISO13616.
        /// </summary>
        /// <param name="iban"></param>
        /// <returns></returns>
        private static string Iso13616Prepare(string iban)
        {
            string retIban = "";
            iban = iban.ToUpper();
            iban = iban.Substring(4) + iban.Substring(0, 4);

            char[] ibanChars = iban.ToCharArray();

            foreach (var ibanChar in ibanChars)
            {
                var code = ((int)ibanChar).ToString();
                int intCode = Convert.ToInt32(code);

                if (intCode >= 65 && intCode <= 90)
                    retIban += (intCode - 65 + 10).ToString();
                else
                    retIban += ibanChar;
            }

            return retIban;
        }

        /// <summary>
        /// Parse the BBAN structure used to configure each IBAN Specification and returns a matching regular expression.
        /// A structure is composed of blocks of 3 characters (one letter and 2 digits). Each block represents
        /// a logical group in the typical representation of the BBAN.For each group, the letter indicates which characters
        /// are allowed in this group and the following 2-digits number tells the length of the group.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static Regex ParseStructure(string structure)
        {
            string regexPattern = @"(.{3})";
            List<string> list = new List<string>();

            foreach (Match match in Regex.Matches(structure, regexPattern))
            {
                string matchRegex = match.Value;

                string format = "", pattern = matchRegex.Substring(0, 1);
                int repeats = Convert.ToInt32(matchRegex.Substring(1), 10);

                switch (pattern)
                {
                    case "A": format = "0-9A-Za-z"; break;
                    case "B": format = "0-9A-Z"; break;
                    case "C": format = "A-Za-z"; break;
                    case "F": format = "0-9"; break;
                    case "L": format = "a-z"; break;
                    case "U": format = "A-Z"; break;
                    case "W": format = "0-9a-z"; break;
                }

                list.Add("([" + format + "]{" + repeats + "})");
            }

            return new Regex("^" + string.Join("", list.ToArray()) + "$");
        }

    }

    class Specification
    {
        /// <summary>
        /// List of countries that are in IBAN registry
        /// </summary>
        public readonly List<IBANSpecificationModel> IBANSpecifications = new List<IBANSpecificationModel>()
        {
            new IBANSpecificationModel { CountryCode = "BE", Length = 16, Structure = "F03F07F02" },
            new IBANSpecificationModel { CountryCode = "NL", Length = 18, Structure = "U04F10" }
        };

        /// <summary>
        /// Model
        /// </summary>
        public class IBANSpecificationModel
        {
            public string CountryCode { get; set; }
            public int Length { get; set; }
            public string Structure { get; set; }
            public string Example { get; set; }
        }
    }
}
