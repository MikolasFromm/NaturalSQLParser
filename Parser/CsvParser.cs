using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using CsvHelper;
using System.Globalization;
using System.Text;
using System.Reflection.PortableExecutable;

namespace NaturalSQLParser.Parser
{
    public static class CsvParser
    {
        public static char delimiter = ';';
        /// <summary>
        /// Parsing CSV file to List of <see cref="Field"/>s from given path.
        /// </summary>
        /// <param name="filePath">Path to a CSV file.</param>
        /// <returns>List of <see cref="Field"/>s from input.</returns>
        public static List<Field> ParseCsvFile(string filePath)
        {
            using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
            {
                return ParseCsvStream(reader);
            }
        }

        /// <summary>
        /// Parsing CSV file to List of <see cref="Field"/>s. from given stream.
        /// </summary>
        /// <param name="filePath">Input stream</param>
        /// <returns>List of <see cref="Field"/>s from input.</returns>
        public static List<Field> ParseCsvFile(Stream fileStream)
        {
            using (var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8))
            {
                return ParseCsvStream(reader);
            }
        }

        private static List<Field> ParseCsvStream(StreamReader reader)
        {
            var result = new List<Field>();

            string line = null;
            string headersLine = null;
            string[] headers = null;
            Dictionary<int, Field> fieldDict = new Dictionary<int, Field>();

            headersLine = reader.ReadLine(); // first line is headers

            if (headersLine is null)
                return null;

            headers = headersLine.Split(delimiter);

            var dataTypes = new FieldDataType[headers.Length];



            // parse data
            line = reader.ReadLine();
            int lineIndex = 0;

            while (line is not null)
            {
                var dataLine = line.Split(delimiter);

                // try obtaining the dataTypes and headers from the zero and first line
                if (lineIndex == 0)
                {
                    for (int i = 0; i < dataLine.Length; i++)
                    {
                        if (Double.TryParse(dataLine[i], CultureInfo.CurrentCulture, out double number))
                            dataTypes[i] = FieldDataType.Number;
                        else if (DateTime.TryParse(dataLine[i], CultureInfo.CurrentCulture, out DateTime datetime))
                            dataTypes[i] = FieldDataType.Date;
                        else if (bool.TryParse(dataLine[i], out bool bolean))
                            dataTypes[i] = FieldDataType.Bool;
                        else
                            dataTypes[i] = FieldDataType.String;
                    }

                    // parse headers
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var header = headers[i];
                        var field = new Field()
                        {
                            Header = new Header(header, dataTypes[i], i),
                            Data = new List<Cell>()
                        };
                        fieldDict.Add(i, field);
                    }
                }
                
                // regularly parse data, row by row
                for (int i = 0; i < dataLine.Length; i++)
                {
                    if (fieldDict.ContainsKey(i))
                        fieldDict[i].Data.Add(new Cell() { Content = dataLine[i], Index = lineIndex });
                }

                // go to next row
                lineIndex++;
                line = reader.ReadLine();
            }

            foreach (var field in fieldDict.Values)
                result.Add(field);

            return result;
        }

        /// <summary>
        /// Parsing the inner List of <see cref="Field"/> representation back to CSV file.
        /// </summary>
        /// <param name="fields"><see cref="List{Field}"/> input fields to parse.</param>
        /// <param name="outputFilePath">Output filePath.</param>
        public static void ParseFieldsIntoCsv(IEnumerable<Field> fields, string outputFilePath)
        {
            using(var writer  = new StreamWriter(outputFilePath, false, System.Text.Encoding.UTF8))
            using(var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var fieldList = new List<Field>();
                foreach (var field in fields)
                {
                    csv.WriteField(field.Header.Name);
                    fieldList.Add(field);
                }
                csv.NextRecord();

                // until not all lines empty
                int currentRow = 0;
                bool rowEmpty = false;
                while(!rowEmpty)
                {
                    foreach (var field in fieldList)
                    {
                        if (currentRow < field.Data.Count)
                        {
                            csv.WriteField(field.Data[currentRow].Content);
                            rowEmpty = false;
                        }
                        else
                        {
                            rowEmpty = true;
                        }
                    }
                    csv.NextRecord();
                    currentRow++;
                }
            }
        }

        /// <summary>
        /// Parsing the inner List of <see cref="Field"/> representation back to CSV file, returning in string.
        /// </summary>
        /// <param name="fields"><see cref="List{Field}"/> input fields to parse.</param>
        /// <param name="outputFilePath">Output filePath.</param>
        public static string ParseFieldsIntoCsv(IEnumerable<Field> fields)
        {
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var fieldList = new List<Field>();
                foreach (var field in fields)
                {
                    csv.WriteField(field.Header.Name);
                    fieldList.Add(field);
                }
                csv.NextRecord();

                // until not all lines empty
                int currentRow = 0;
                bool rowEmpty = false;
                while (!rowEmpty)
                {
                    foreach (var field in fieldList)
                    {
                        if (currentRow < field.Data.Count)
                        {
                            csv.WriteField(field.Data[currentRow].Content);
                            rowEmpty = false;
                        }
                        else
                        {
                            rowEmpty = true;
                        }
                    }
                    csv.NextRecord();
                    currentRow++;
                }

                return writer.ToString();
            }
        }
    }
}
