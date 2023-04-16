using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace NaturalSQLParser.Parser
{
    public static class CsvParser
    {
        /// <summary>
        /// Parsing CSV file to List of <see cref="Field"/>s.
        /// </summary>
        /// <param name="filePath">Path to a CSV file.</param>
        /// <returns>List of <see cref="Field"/>s from input.</returns>
        public static List<Field> ParseCsvFile(string filePath)
        {
            var result = new List<Field>();

            var lines = File.ReadAllLines(filePath);
            var headers = lines[0].Split(';');
            var data = lines.Skip(1).ToArray();

            Dictionary<int, Field> fieldDict = new Dictionary<int, Field>();
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

            // iteration over all data lines
            for (int j = 0; j < data.Length; j++) // iterating over raws
            {
                var dataLine = data[j].Split(';');
                for (int i = 0; i < dataLine.Length; i++) // iterating over columns in a row
                {
                    if (fieldDict.ContainsKey(i))
                        fieldDict[i].Data.Add(new Cell() { Content = dataLine[i], Index = i });
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
            using(var writer  = new StreamWriter(outputFilePath))
            using(var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
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
    }
}
