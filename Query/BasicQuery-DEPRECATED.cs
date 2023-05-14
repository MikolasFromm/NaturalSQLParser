using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Types.Tranformations;

using OpenAI_API;
using OpenAI_API.Chat;
using System.ComponentModel.Design;

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
                try
                {
                    // Print all possible transformations
                    Console.WriteLine($"---> Choose next transformation: ");
                    foreach (var transformation in possibleTransformations)
                        Console.Write($"{transformation.GetTransformationName()}; ");
                    Console.WriteLine();

                    // Read the transformation name given by the user
                    userInput = Console.ReadLine();
                    if (userInput is null || userInput == "")
                        break;

                    // obtain the transformation
                    string transformationName = userInput;
                    var transformationCandidate = TransformationFactory.GetTransformationCandidate(transformationName);

                    string[] request = new string[1];
                    Console.WriteLine($"---> {transformationCandidate.GetNextMovesInstructions()}");
                    var moves = transformationCandidate.GetNextMoves(this.Response).ToList();
                    var nextMove = string.Empty;

                    while (true)
                    {
                        // Print all possible moves for the transformation
                        foreach (var move in transformationCandidate.GetNextMoves(this.Response))
                        {
                            Console.Write($"{move}; ");
                            moves.Add(move);
                        }
                        Console.WriteLine();

                        nextMove = Console.ReadLine();

                        if (nextMove is not null && moves.Contains(nextMove))
                            break;
                        else
                            Console.WriteLine("---> Invalid input, choose from the following:");
                    }

                    request[0] = nextMove;

                    ITransformation generatedTransformation;
                    if (transformationCandidate.HasArguments)
                    {
                        // Print all possible arguments for the transformation
                        Console.WriteLine($"---> {transformationCandidate.GetArgumentsInstructions()}");
                        var arguments = transformationCandidate.GetArguments().ToList();
                        
                        while (true)
                        {
                            foreach (var item in transformationCandidate.GetArguments())
                            {
                                Console.Write($"{item}; ");
                            }
                            Console.WriteLine();    

                            // Load user input
                            userInput = Console.ReadLine();

                            // Build transformation preprocess
                            string[] arguemnts = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            try
                            {
                                generatedTransformation = TransformationFactory.BuildTransformation(transformationName, request.Concat(arguemnts).ToArray());
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }

                        }
                    }
                    else
                    {
                        generatedTransformation = TransformationFactory.BuildTransformation(transformationName, request);
                    }

                    // Save the transformation
                    Transformations.Add(generatedTransformation);

                    // Rebuild the possible response
                    Response = generatedTransformation.Preprocess(Response);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
            }
            return Transformations;
        }

        public async IAsyncEnumerable<ITransformation> CreateAIQueryAsync()
        {
            var api = new OpenAIAPI(Secrets.Credentials.PersonalApiKey);
            var chat = api.Chat.CreateConversation();

            chat.AppendSystemMessage("You are an assistant which should translate user input into query request, which will be later executed on the given dataset. " +
                "You will always get a list of options started wtih '--->' from which you can choose the best following option to fulfill the user request. " +
                "Each choosen word must be separated with one space and all transformation parameters must be on one line." +
                "When you are finished with processing the query, only send empty string which is translated to END OF QUERY. " +
                "NOTE THAT YOU MUST RETURN ONLY THE SAME WORDS THAT WERE GIVEN FROM THE SELECTION FOLLOWED BY '--->'");

            Console.WriteLine("Write your query request:");
            chat.AppendUserInput($"User input is: {Console.ReadLine()};");
            chat.AppendSystemMessage("Now it is your turn to choose the right operations.");

            string BotResponse;
            while (true)
            {
                ITransformation generatedTransformation = null;
                try
                {
                    // Print all possible transformations
                    Console.WriteLine($"---> Choose next transformation: ");
                    chat.AppendSystemMessage($"---> Choose next transformation: ");
                    foreach (var transformation in possibleTransformations)
                    {
                        Console.Write($"{transformation.GetTransformationName()}; ");
                        chat.AppendSystemMessage($"{transformation.GetTransformationName()}; ");
                    }
                    Console.WriteLine();

                    // Read the transformation name given by the user
                    BotResponse = await chat.GetResponseFromChatbotAsync();
                    if (BotResponse is null || BotResponse == "")
                        break;

                    // obtain the transformation
                    string transformationName = BotResponse;
                    var transformationCandidate = TransformationFactory.GetTransformationCandidate(transformationName);

                    string[] request = new string[1];
                    Console.WriteLine($"---> {transformationCandidate.GetNextMovesInstructions()}");
                    var moves = transformationCandidate.GetNextMoves(this.Response).ToList();
                    var nextMove = string.Empty;

                    while (true)
                    {
                        // Print all possible moves for the transformation
                        foreach (var move in transformationCandidate.GetNextMoves(this.Response))
                        {
                            Console.Write($"{move}; ");
                            moves.Add(move);
                        }
                        Console.WriteLine();

                        nextMove = Console.ReadLine();

                        if (nextMove is not null && moves.Contains(nextMove))
                            break;
                        else
                            Console.WriteLine("---> Invalid input, choose from the following:");
                    }

                    request[0] = nextMove;
                    if (transformationCandidate.HasArguments)
                    {
                        // Print all possible arguments for the transformation
                        Console.WriteLine($"---> {transformationCandidate.GetArgumentsInstructions()}");
                        var arguments = transformationCandidate.GetArguments().ToList();

                        while (true)
                        {
                            foreach (var item in transformationCandidate.GetArguments())
                            {
                                Console.Write($"{item}; ");
                            }
                            Console.WriteLine();

                            // Load user input
                            BotResponse = Console.ReadLine();

                            // Build transformation preprocess
                            string[] arguemnts = BotResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            try
                            {
                                generatedTransformation = TransformationFactory.BuildTransformation(transformationName, request.Concat(arguemnts).ToArray());
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }

                        }
                    }
                    else
                    {
                        generatedTransformation = TransformationFactory.BuildTransformation(transformationName, request);
                    }

                    // Rebuild the possible response
                    Response = generatedTransformation.Preprocess(Response);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (generatedTransformation is not null)
                    yield return generatedTransformation;
            }
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
