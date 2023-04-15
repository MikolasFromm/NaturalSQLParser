using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Types.Tranformations;

using OpenAI_API;
using OpenAI_API.Chat;

namespace NaturalSQLParser.Query
{
    public class Processor
    {
        private IEnumerable<ITransformation> possibleTransformations = new List<ITransformation>()
        {
            new EmptyTransformation(),
            new DropColumnTransformation(),
            new SortByTransformation(),
            new GroupByTransformation(),
            new FilterByTransformation()
        };

        public List<EmptyField> Response { get; set; } = new List<EmptyField>();

        private List<ITransformation> Transformations { get; set; } = new List<ITransformation>();

        public IEnumerable<ITransformation> CreateQuery()
        {
            string userInput;
            while (true)
            {
                // Print all possible transformations
                Console.WriteLine("Choose next transformation: ");
                foreach (var transformation in possibleTransformations)
                {
                    Console.Write($"{transformation.GetTransformationName()}; ");
                }
                Console.WriteLine();

                // Read the transformation name given by the user
                userInput = Console.ReadLine();
                if (userInput is null || userInput == "")
                {
                    break;
                }

                string transformationName = userInput;
                var transformationCandidate = TransformationFactory.GetTransformationCandidate(transformationName);
                // Print all possible moves for the transformation
                Console.WriteLine("Next moves are: ");
                foreach (var move in transformationCandidate.GetNextMoves(this.Response))
                {
                    Console.Write($"{move}; ");
                }
                Console.WriteLine();
                // Print all possible arguments for the transformation
                Console.WriteLine("With arguments: ");
                foreach (var item in transformationCandidate.GetArguments())
                {
                    Console.Write($"{item}; ");
                }
                Console.WriteLine();
                // Load user input
                Console.WriteLine("Now write our move with given arguments");
                userInput = Console.ReadLine();

                // Build transformation preprocess
                string[] request = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var generatedTransformation = TransformationFactory.BuildTransformation(transformationName, request);
                
                // Save the transformation
                Transformations.Add(generatedTransformation);

                // Rebuild the possible response
                Response = generatedTransformation.Preprocess(Response);
            }
            return Transformations;
        }

        public List<Field> MakeTransformations(IEnumerable<ITransformation> transformations, List<Field> DataSet)
        {
            foreach(ITransformation transformation in transformations)
            {
                DataSet = transformation.PerformTransformation(DataSet);
            }
            return DataSet;
        }

        public async Task AIRequestTest()
        {
            var api = new OpenAIAPI(Secrets.Credentials.PersonalApiKey);
            var validation = await api.Auth.ValidateAPIKey();

            var chat = api.Chat.CreateConversation();

            /// give instruction as System
            chat.AppendSystemMessage("You are a teacher who helps children understand if things are animals or not.  If the user tells you an animal, you say \"yes\".  If the user tells you something that is not an animal, you say \"no\".  You only ever respond with \"yes\" or \"no\".  You do not say anything else.");

            // give a few examples as user and assistant
            chat.AppendUserInput("Is this an animal? Cat");
            chat.AppendExampleChatbotOutput("Yes");
            chat.AppendUserInput("Is this an animal? House");
            chat.AppendExampleChatbotOutput("No");

            // now let's ask it a question'
            chat.AppendUserInput("Is this an animal? Dog");
            // and get the response
            string response = await chat.GetResponseFromChatbotAsync();
            Console.WriteLine(response); // "Yes"

            // and continue the conversation by asking another
            chat.AppendUserInput("Is this an animal? Chair");
            // and get another response
            response = await chat.GetResponseFromChatbotAsync();
            Console.WriteLine(response); // "No"

            // the entire chat history is available in chat.Messages
            foreach (ChatMessage msg in chat.Messages)
            {
                Console.WriteLine($"{msg.Role}: {msg.Content}");
            }

            var userInput = "";
            while (userInput is not null)
            {
                Console.Write("Input: ");
                userInput = Console.ReadLine();
                Console.WriteLine();
                chat.AppendUserInput(userInput);

                response = await chat.GetResponseFromChatbotAsync();
                Console.Write("Response: ");
                Console.WriteLine(response);
            }
        }
    }
}
