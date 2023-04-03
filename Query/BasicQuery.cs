using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Types.Tranformations;

namespace NaturalSQLParser.Query
{
    public class Processor
    {
        private List<ITransformation> possibleTransformations = new List<ITransformation>()
        {
            new EmptyTransformation(),
            new DropColumnTransformation(),
            new SortByTransformation(),
            new GroupByTransformation(),
            new FilterByTransformation()
        };

        public List<EmptyField> Response { get; set; } = new List<EmptyField>();

        public List<EmptyField> CreateQuery()
        {
            string userInput = "";
            while (userInput is not null)
            {
                Console.WriteLine("Choose next operator: ");
                foreach (var transformation in possibleTransformations)
                {
                    Console.Write(transformation.GetOperator());
                    Console.Write("; ");
                }
                Console.WriteLine();
                string[] request = Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var generatedTransformation = TransformationFactory.GetTransformation(request[0], request.Skip(1).ToArray());
                Response = generatedTransformation.Preprocess(Response);
            }
            return Response;
        }
    }
}
