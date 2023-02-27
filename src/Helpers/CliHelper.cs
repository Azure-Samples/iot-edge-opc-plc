namespace OpcPlc.Helpers;

using Mono.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CliHelper
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
}
