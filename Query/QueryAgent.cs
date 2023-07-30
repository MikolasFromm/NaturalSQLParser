using NaturalSQLParser.Communication;
using NaturalSQLParser.Types;
using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Types.Tranformations;
using OpenAI_API;

namespace NaturalSQLParser.Query
{
    public class QueryAgent
    {
        private CommunicationAgent _communicationAgent;

        private List<EmptyField> _response = new List<EmptyField>();

        private List<ITransformation> _transformations { get; set; } = new List<ITransformation>();

        private IEnumerable<ITransformation> possibleTransformations = new List<ITransformation>()
        {
            new EmptyTransformation(),
            new DropColumnTransformation(),
            new SortByTransformation(),
            new GroupByTransformation(),
            new FilterByTransformation()
        };

        /// <summary>
        /// Default constructor for creating query with OpenAI chatbot
        /// </summary>
        /// <param name="api"></param>
        /// <param name="verbose"></param>
        public QueryAgent(OpenAIAPI api, List<Field> fields, bool verbose = true)
        {
            _response = new List<EmptyField>(fields);
            _communicationAgent = new CommunicationAgent(api, verbose);
        }

        /// <summary>
        /// Default constructor for creating user query, where user creates the response.
        /// </summary>
        /// <param name="verbose"></param>
        public QueryAgent(List<Field> fields, bool verbose = true)
        {
            _response = new List<EmptyField>(fields);
            _communicationAgent = new CommunicationAgent(verbose);
        }

        /// <summary>
        /// Sequentially parsing query, checking correctness, generating transformations and obtaining necessary arguments for chosen transformations.
        /// Query mode independant. Using <see cref="CommunicationAgent"/> for comunication with user/bot.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITransformation> PerformQuery()
        {
            while (true)
            {
                ITransformation generatedTransformation = null;
                try
                {
                    string transformationName = string.Empty;

                    // Print all possible transformations
                    _communicationAgent.InsertUserMessage($"---> Choose next transformation: ");
                    foreach (var transformation in possibleTransformations)
                        _communicationAgent.InsertUserMessage($"> {transformation.GetTransformationName()}");
                    _communicationAgent.Indent();

                    // Gets the next transformation name
                    transformationName = _communicationAgent.GetResponse();

                    if (string.IsNullOrEmpty(transformationName))
                        break;

                    // Create the transformation candidate
                    var transformationCandidate = TransformationFactory.Create(transformationName);

                    // Get the primary instruction for the transformation
                    _communicationAgent.InsertUserMessage($"---> {transformationCandidate.GetNextMovesInstructions()}");
                    var nextPossibleMoves = transformationCandidate.GetNextMoves(_response);
                    foreach (var move in nextPossibleMoves)
                        _communicationAgent.InsertUserMessage($"> {move}");
                    _communicationAgent.Indent();

                    // loop until getting satisfying answer
                    string nextMove = string.Empty;
                    while (true)
                    {
                        var response = _communicationAgent.GetResponse();
                        if (!string.IsNullOrEmpty(response) && nextPossibleMoves.Contains(response))
                        {
                            nextMove = response;
                            break; // go to next stage
                        }
                        else
                        {
                            _communicationAgent.ErrorMessage($"Invalid input, you must choose from the options given above. Please try again.");
                            _communicationAgent.Indent();
                        }
                    }

                    if (transformationCandidate.HasArguments)
                    {
                        // Get all possible transformations for the transformation
                        _communicationAgent.InsertUserMessage($"---> {transformationCandidate.GetArgumentsInstructions()}");
                        foreach (var argument in transformationCandidate.GetArguments())
                            _communicationAgent.InsertUserMessage($"> {argument}");
                        _communicationAgent.Indent();

                        // loop until getting satisfying answer
                        while(true)
                        {
                            var response = _communicationAgent.GetResponse();
                            var arguments = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            try
                            {
                                string[] requestArgments = new string[arguments.Count() + 1];
                                requestArgments[0] = nextMove;
                                arguments.CopyTo(requestArgments, 1);
                                generatedTransformation = TransformationFactory.BuildTransformation(transformationName, requestArgments);
                                _transformations.Add(generatedTransformation);
                                break;
                            }
                            catch(ArgumentException ex)
                            {
                                _communicationAgent.ErrorMessage($"{ex.Message}. Please try again.");
                                _communicationAgent.Indent();
                            }
                        }
                    }
                    else
                    {
                        generatedTransformation = TransformationFactory.BuildTransformation(transformationName, new string[] { nextMove });
                    }

                    // rebuild the possible response
                    _response = generatedTransformation.Preprocess(_response);
                }
                catch (ArgumentException ex)
                {
                    _communicationAgent.ErrorMessage($"{ex.Message}. Please try again.");
                    _communicationAgent.Indent();
                }
                _communicationAgent.Indent();

                Console.WriteLine("(Enter any key to continue...)");
                var endIt = Console.ReadLine();
                if (String.IsNullOrEmpty(endIt))
                {
                    _communicationAgent.ShowConversationHistory();
                    break;
                }
            }

            return _transformations;
        }

        /// <summary>
        /// Sequentially parsing query, checking correctness, generating transformations and obtaining necessary arguments for chosen transformations.
        /// Query mode independant. Using <see cref="CommunicationAgent"/> for comunication with user/bot.
        /// Instead of asking for choosing the next transformation by name, it uses the index of the transformation in the list.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITransformation> PerformQueryWithIndices()
        {
            while (true)
            {
                ITransformation generatedTransformation = null;
                try
                {
                    string nextMove = string.Empty;
                    string transformationName = string.Empty;
                    string firstArgument = string.Empty;
                    string secondArgument = string.Empty;

                    // Print all possible transformations
                    _communicationAgent.InsertUserMessage($"---> Choose next transformation: ");
                    int i = 0;
                    foreach (var transformation in possibleTransformations)
                        _communicationAgent.InsertUserMessage($"> [{i++}] {transformation.GetTransformationName()}");
                        
                    _communicationAgent.Indent();

                    // Gets the next transformation name
                    transformationName = _communicationAgent.GetResponse();

                    if (string.IsNullOrEmpty(transformationName))
                        break;

                    // Try to parse the string into number
                    if (!Int32.TryParse(transformationName, out int transformationIndex))
                    {
                        _communicationAgent.ErrorMessage($"Invalid input, you must enter only an integer. Please try again.");
                        _communicationAgent.Indent();
                        continue;
                    }

                    // Check if the number is in range
                    if (transformationIndex >= possibleTransformations.Count())
                    {
                        _communicationAgent.ErrorMessage($"Invalid input out of range. You must choose from the selection above. Please try again.");
                        _communicationAgent.Indent();
                        continue;
                    }

                    // Create the transformation candidate
                    var transformationCandidate = TransformationFactory.CreateByIndex(transformationIndex);

                    // Get the primary instruction for the transformation
                    _communicationAgent.InsertUserMessage($"---> {transformationCandidate.GetNextMovesInstructions()}");
                    var nextPossibleMoves = transformationCandidate.GetNextMoves(_response);
                    i = 0;
                    foreach (var move in nextPossibleMoves)
                        _communicationAgent.InsertUserMessage($"> [{i++}] {move}");
                    _communicationAgent.Indent();

                    // loop until getting satisfying answer
                    while (true)
                    {
                        // obtain the message
                        var response = _communicationAgent.GetResponse();

                        // check the message
                        bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);
                        bool isInt = Int32.TryParse(response, out int choice);
                        bool isInRange = choice < nextPossibleMoves.Count();

                        // act
                        if (isNotNullOrEmpty && isInt && isInRange)
                        {
                            nextMove = transformationCandidate.GetNextMoves(_response).ElementAt(choice);
                            break; // go to next stage
                        }
                        else
                        {
                            if (!isNotNullOrEmpty)
                                _communicationAgent.ErrorMessage($"Invalid input: Empty message received!");

                            else if (!isInt)
                                _communicationAgent.ErrorMessage($"Invalid input: Non-integer message received!");

                            else if (!isInRange)
                                _communicationAgent.ErrorMessage($"Invalid input: Message out of range received!");
                            else
                                _communicationAgent.ErrorMessage($"Invalid input, you must choose from the options given above. Please try again.");
                            
                            _communicationAgent.Indent();
                        }
                    }

                    if (transformationCandidate.HasArguments)
                    {
                        // Get all possible transformations for the transformation
                        _communicationAgent.InsertUserMessage($"---> {transformationCandidate.GetArgumentsInstructions()}");

                        i = 0;
                        var nextPossibleArguments = transformationCandidate.GetArguments();
                        foreach (var argument in nextPossibleArguments)
                            _communicationAgent.InsertUserMessage($"> [{i++}] {argument}");
                        _communicationAgent.Indent();

                        // loop until getting satisfying answer
                        while (true)
                        {
                            // obtain the message
                            var response = _communicationAgent.GetResponse();

                            // check the message
                            bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);
                            bool isInt = Int32.TryParse(response, out int choice);
                            bool isInRange = choice < nextPossibleArguments.Count();

                            // act
                            if (isNotNullOrEmpty && isInt && isInRange)
                            {
                                firstArgument = transformationCandidate.GetArguments().ElementAt(choice);
                                break; // go to next stage
                            }
                            else
                            {
                                if (!isNotNullOrEmpty)
                                    _communicationAgent.ErrorMessage($"Invalid input: Empty message received!");

                                else if (!isInt)
                                    _communicationAgent.ErrorMessage($"Invalid input: Non-integer message received!");

                                else if (!isInRange)
                                    _communicationAgent.ErrorMessage($"Invalid input: Message out of range received!");
                                else
                                    _communicationAgent.ErrorMessage($"Invalid input, you must choose from the options given above. Please try again.");

                                _communicationAgent.Indent();
                            }
                        }

                        if (transformationCandidate.HasFollowingHumanArguments)
                        {
                            i = 0;
                            var nextPossibleHumanArguments = transformationCandidate.GetFollowingHumanArgumentsInstructions();
                            _communicationAgent.InsertUserMessage(nextPossibleHumanArguments);
                            _communicationAgent.Indent();

                            // loop until getting satisfying answer
                            while (true)
                            {
                                // obtain the message
                                var response = _communicationAgent.GetResponse();

                                // check the message
                                bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);

                                // act
                                if (isNotNullOrEmpty)
                                {
                                    secondArgument = transformationCandidate.GetFollowingHumanArgumentsInstructions();
                                    break; // go to next stage
                                }
                                else
                                {
                                    _communicationAgent.ErrorMessage($"Invalid input: Empty message received!");
                                    _communicationAgent.Indent();
                                }
                            }
                        }

                    }

                    // build the transformation
                    generatedTransformation = TransformationFactory.BuildTransformation(transformationName, new string[] { nextMove, firstArgument, secondArgument });

                    // rebuild the possible response
                    _response = generatedTransformation.Preprocess(_response);
                }
                catch (ArgumentException ex)
                {
                    _communicationAgent.ErrorMessage($"{ex.Message}. Please try again.");
                    _communicationAgent.Indent();
                }
                _communicationAgent.Indent();

                Console.WriteLine("(Enter any key to continue...)");
                var endIt = Console.ReadLine();
                if (String.IsNullOrEmpty(endIt))
                {
                    _communicationAgent.ShowConversationHistory();
                    break;
                }
            }

            return _transformations;
        }
    }
}
