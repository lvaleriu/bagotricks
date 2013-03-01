using System.Collections.Generic;
using System.IO;

namespace SilverlightCompLib.Mathematics
{
    public static partial class Extensions
    {
    
    }

    public static class File
    {
        /// <summary>
        /// (Supplemented) Reads all lines from a stream.
        /// </summary>
        /// <param name="s">The stream to read.</param>
        /// <returns>A string array containing all lines of the file.</returns>
        public static string[] ReadAllLines(Stream s)
        {
            System.IO.StreamReader reader = new StreamReader(s);
            List<string> allLines = new List<string>();

            while(!reader.EndOfStream)
            {
                allLines.Add(reader.ReadLine());
            }
            return allLines.ToArray();
        }
    }
}