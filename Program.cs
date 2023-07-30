#define INDEXING

using NaturalSQLParser.Query;
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
            // load mock files
            var fields = CsvParser.ParseCsvFile("C:\\Users\\mikol\\Documents\\SQLMock.csv");

            QueryAgent queryAgent;

            // prompt for OpenAI chatbot usage
            Console.WriteLine("Use OpenAI ChatBot? (Y/N)?");
            var yesNo = Console.ReadLine();

            if (yesNo == "Y")
                queryAgent = new QueryAgent(new OpenAIAPI(Credentials.PersonalApiKey), fields);
            else
                queryAgent = new QueryAgent(fields);

            // perform query

#if WORD_MATCHING
            var transformations = queryAgent.PerformQuery();
#endif

#if INDEXING
            var transformations = queryAgent.PerformQueryWithIndices();
#endif

            var result = Transformator.TransformFields(fields, transformations);

            // save result to file
            CsvParser.ParseFieldsIntoCsv(result, "C:\\Users\\mikol\\Documents\\SQLMock-output.csv");
        }
    }
}