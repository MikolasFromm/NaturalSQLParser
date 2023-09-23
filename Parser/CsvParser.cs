using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using CsvHelper;
using System.Globalization;
using System.Text;

namespace NaturalSQLParser.Parser
{
    public static class CsvParser
    {
        public static char delimiter = ';';
        /// <summary>
        /// Parsing CSV file to List of <see cref="Field"/>s.
        /// </summary>
        /// <param name="filePath">Path to a CSV file.</param>
        /// <returns>List of <see cref="Field"/>s from input.</returns>
        public static List<Field> ParseCsvFile(string filePath)
        {
            var result = new List<Field>();

            string line = null;
            string headersLine = null;
            string[] headers = null;
            Dictionary<int, Field> fieldDict = new Dictionary<int, Field>();

            using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8)) 
            {
                headersLine = reader.ReadLine();

                if (headersLine is null)
                    return null;
                
                headers = headersLine.Split(delimiter);
                for (int i = 0; i < headers.Length; i++)
                {
                    var header = headers[i];
                    var field = new Field()
                    {
                        Header = new Header(header, FieldDataType.String, i),
                        Data = new List<Cell>()
                    };
                    fieldDict.Add(i, field);
                }

                line = reader.ReadLine();
                int lineIndex = 0;
                while (line is not null) 
                {
                    var dataLine = line.Split(delimiter);
                    for (int i = 0; i < dataLine.Length; i++)
                    {
                        if (fieldDict.ContainsKey(i))
                            fieldDict[i].Data.Add(new Cell() { Content = dataLine[i], Index = lineIndex });
                    }   
                    lineIndex++;
                    line = reader.ReadLine();
                }
            }

            foreach(var field in fieldDict.Values)
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
