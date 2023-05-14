using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Query;
using NaturalSQLParser.Types;
using NaturalSQLParser.Parser;
using NaturalSQLParser.Types.Tranformations;
using NaturalSQLParser.Query.Secrets;
using OpenAI_API;

namespace NaturalSQLParser
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // MOCK DATA
            List<Field> dataSet = new List<Field>
            {
                new Field() { Header = new Header("FirstName", FieldDataType.String, 0), Data = new List<Cell>() { new Cell() { Content = "John", Index = 0 }, new Cell() { Content = "Jane", Index = 1 }, new Cell() { Content = "Tomas", Index = 2 } } },
                new Field() { Header = new Header("LastName", FieldDataType.String, 1), Data = new List<Cell>() { new Cell() { Content = "Lennon", Index = 0 }, new Cell() { Content = "Petrová", Index = 1 }, new Cell() { Content = "Fromm", Index = 2 } } },
                new Field() { Header = new Header("Age", FieldDataType.Number, 2), Data = new List<Cell>() { new Cell() { Content = "35", Index = 0 }, new Cell() { Content = "25", Index = 1 }, new Cell() { Content = "45", Index = 2 } } },
                new Field() { Header = new Header("Department", FieldDataType.String,3), Data = new List<Cell>() { new Cell() { Content = "IT", Index = 0 }, new Cell() { Content = "HR", Index = 1 }, new Cell() { Content = "CEO", Index = 2 } } }
            };

            var fields = CsvParser.ParseCsvFile("C:\\Users\\mikol\\Documents\\SQLMock.csv");

            var api = new OpenAIAPI(Credentials.PersonalApiKey);
            var query = new QueryAgent(fields);
            var transformations = query.PerformQuery();
            var result = Transformator.TransformFields(fields, transformations);

            CsvParser.ParseFieldsIntoCsv(result, "C:\\Users\\mikol\\Documents\\SQLMock-output.csv");

            //await prc.AIRequestTest();


            return;
        }
    }
}