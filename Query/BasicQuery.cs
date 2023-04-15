using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Types.Tranformations;

using OpenAI_API;
using OpenAI_API.Chat;

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

        public async Task AIRequestTest()
        {
            var api = new OpenAIAPI(Secrets.Credentials.PersonalApiKey);
            var validation = await api.Auth.ValidateAPIKey();

            Console.WriteLine(validation);

            var chat = api.Chat.CreateConversation();

            //Console.Write("Write SystemMessage: ");
            //var systemMessage = Console.ReadLine();
            //chat.AppendSystemMessage(systemMessage);
            //Console.WriteLine();

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
