namespace OpcPlc.Helpers;

using Mono.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public static class CliHelper
{
    /// <summary>
    /// Helper to build a list of filenames out of a comma separated list of filenames (optional in double quotes).
    /// </summary>
    public static List<string> ParseListOfFileNames(string fileCsvList, string option)
    {
        var fileNames = new List<string>();
        if (fileCsvList[0] == '"' && (fileCsvList.Count(c => c.Equals('"')) % 2 == 0))
        {
            while (fileCsvList.Contains('"'))
            {
                int first = 0;
                int next = 0;
                first = fileCsvList.IndexOf('"', next);
                next = fileCsvList.IndexOf('"', ++first);
                string fileName = fileCsvList[first..next];

                if (File.Exists(fileName))
                {
                    fileNames.Add(fileName);
                }
                else
                {
                    throw new OptionException($"The file '{fileName}' does not exist.", option);
                }

                fileCsvList = fileCsvList.Substring(++next);
            }
        }
        else if (fileCsvList.Contains(','))
        {
            var parsedFileNames = fileCsvList
                .Split(',')
                .Select(st => st.Trim());

            foreach (var fileName in parsedFileNames)
            {
                if (File.Exists(fileName))
                {
                    fileNames.Add(fileName);
                }
                else
                {
                    throw new OptionException($"The file '{fileName}' does not exist.", option);
                }
            }
        }
        else
        {
            if (File.Exists(fileCsvList))
            {
                fileNames.Add(fileCsvList);
            }
            else
            {
                throw new OptionException($"The file '{fileCsvList}' does not exist.", option);
            }
        }
        return fileNames;
    }

    /// <summary>
    /// Parse float value from string, verify that it is within the specified range and
    /// round it to the specified number of decimal digits.
    /// </summary>
    public static float ParseFloat(string input, float min, float max, string optionName, int digits)
    {
        if (float.TryParse(input, CultureInfo.InvariantCulture, out float value))
        {
            if (value >= min && value <= max)
            {
                return (float)Math.Round(value, digits);
            }
            else
            {
                throw new OptionException($"The {optionName} {input} is not within the range {min} to {max}.", optionName);
            }
        }

        throw new OptionException($"The {optionName} {input} is not a valid double.", optionName);
    }

    /// <summary>
    /// Parse double value from string, verify that it is within the specified range and
    /// round it to the specified number of decimal digits.
    /// </summary>
    public static double ParseDouble(string input, double min, double max, string optionName, int digits)
    {
        if (double.TryParse(input, CultureInfo.InvariantCulture, out double value))
        {
            if (value >= min && value <= max)
            {
                return Math.Round(value, digits);
            }
            else
            {
                throw new OptionException($"The {optionName} {input} is not within the range {min} to {max}.", optionName);
            }
        }

        throw new OptionException($"The {optionName} {input} is not a valid double.", optionName);
    }

    /// <summary>
    /// Parse int value from string and verify that it is within the specified range.
    /// </summary>
    public static double ParseInt(string input, int min, int max, string optionName)
    {
        if (int.TryParse(input, out int value))
        {
            if (value >= min && value <= max)
            {
                return value;
            }
            else
            {
                throw new OptionException($"The {optionName} {input} is not within the range {min} to {max}.", optionName);
            }
        }

        throw new OptionException($"The {optionName} {input} is not a valid int.", optionName);
    }
}
