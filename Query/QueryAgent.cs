using NaturalSQLParser.Communication;
using NaturalSQLParser.Model;
using NaturalSQLParser.Types;
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

        #region Factory methods
        public static QueryAgent CreateUserQueryAgent(List<Field> fields, bool verbose = true)
        {
            return new QueryAgent(fields, verbose);
        }

        public static QueryAgent CreateOpenAIQueryAgent(OpenAIAPI api, List<Field> fields, bool verbose = true)
        {
            return new QueryAgent(api, fields, verbose);
        }

        public static QueryAgent CreateOpenAIServerQueryAgent(OpenAIAPI api, bool verbose = true)
        {
            return new QueryAgent(api, verbose);
        }
        #endregion

        #region Constructors
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
        /// Default constructor for Server API communication
        /// </summary>
        public QueryAgent(OpenAIAPI api, bool verbose = true)
        {
            _communicationAgent = new CommunicationAgent(api, verbose);
        }

        #endregion

        /// <summary>
        /// Mirror of a <see cref="CommunicationAgent"/> method to make it public for <see cref="QueryAgent"/> class. A legal way to create a new user query.
        /// </summary>
        /// <param name="userQuery"></param>
        public void AddUserQuery(string userQuery = null)
        {
            if (_communicationAgent is not null)
                _communicationAgent.AddUserQuery(userQuery);
        }

        #region Query mode methods

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
                    _communicationAgent.InsertNextPossibleArguments(from transformation in possibleTransformations select transformation.GetTransformationName());
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
                    _communicationAgent.InsertNextPossibleArguments(nextPossibleMoves);
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
                        _communicationAgent.InsertNextPossibleArguments(transformationCandidate.GetArguments());
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
                        _transformations.Add(generatedTransformation);
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
                    _communicationAgent.InsertNextPossibleArgumentsWithIndices(from transformation in this.possibleTransformations select $"{transformation.GetTransformationName()}");
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

                    // check is over
                    if (transformationIndex == 0)
                        break;

                    // Create the transformation candidate
                    var transformationCandidate = TransformationFactory.CreateByIndex(transformationIndex);

                    // Get the primary instruction for the transformation
                    _communicationAgent.InsertUserMessage($"---> {transformationCandidate.GetNextMovesInstructions()}");
                    var nextPossibleMoves = transformationCandidate.GetNextMoves(_response);
                    _communicationAgent.InsertNextPossibleArgumentsWithIndices(nextPossibleMoves);
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
                        var nextPossibleArguments = transformationCandidate.GetArguments();
                        _communicationAgent.InsertNextPossibleArgumentsWithIndices(nextPossibleArguments);
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
                                firstArgument = transformationCandidate.GetArgumentAt(choice);
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
                                    secondArgument = response;
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
                    _transformations.Add(generatedTransformation);

                    // rebuild the possible response
                    _response = generatedTransformation.Preprocess(_response);
                }
                catch (ArgumentException ex)
                {
                    _communicationAgent.ErrorMessage($"{ex.Message}. Please try again.");
                    _communicationAgent.Indent();
                }
                _communicationAgent.Indent();
            }

            return _transformations;
        }

        /// <summary>
        /// Sequentially builds the transformations based on the query built so far. After any input, the "nextMoves" is saved in order to return the current next moves when whole query performed.
        /// </summary>
        /// <returns></returns>
        public QueryViewModel ServerLikePerformQueryWithIndices(IList<string> queryItems, IList<Field> fields)
        {
            // refresh with the initial table
            _response = new List<EmptyField>(fields);

            var responseQueryModel = new QueryViewModel();

            bool firstQueryItem = true;

            ITransformation transformationCandidate = null;
            int totalStepsMade = 0;

            var querySoFar = string.Join('.', queryItems);

            if (_communicationAgent.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"---> Query: {querySoFar}");
            }

            while (queryItems.Any() || firstQueryItem)
            {
                totalStepsMade = 0;
                ITransformation generatedTransformation = null;
                try
                {
                    string nextMove = string.Empty;
                    string transformationName = string.Empty;
                    string firstArgument = string.Empty;
                    string secondArgument = string.Empty;

                    // Print all possible transformations
                    responseQueryModel.NextMoves = _communicationAgent.CreateNextQuestion($"---> Choose next transformation: ", from transformation in this.possibleTransformations select $"{transformation.GetTransformationName()}");

                    // dont skip the previous queryItem when at the beginning
                    if (!firstQueryItem)
                        queryItems.RemoveAt(0);
                    else
                        firstQueryItem = false;

                    var nextQueryItem = queryItems.FirstOrDefault();
                    var index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                    // Gets the next transformation name    
                    transformationName = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index);
                    responseQueryModel.AddBotSuggestion(transformationName);


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

                    // check is over
                    if (transformationIndex == 0)
                        break;

                    // Create the transformation candidate
                    transformationCandidate = TransformationFactory.CreateByIndex(transformationIndex);

                    IEnumerable<string> nextMoves = null;

                    // loop until getting satisfying answer
                    while (queryItems.Any())
                    {
                        // Get the primary instruction for the transformation
                        var nextPossibleMoves = transformationCandidate.GetNextMoves(_response);
                        nextMoves = _communicationAgent.CreateNextQuestion($"---> {transformationCandidate.GetNextMovesInstructions()}", nextPossibleMoves);

                        // obtain the message
                        queryItems.RemoveAt(0);

                        responseQueryModel.NextMoves = nextMoves;

                        nextQueryItem = queryItems.FirstOrDefault();
                        index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                        var response = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index);
                        totalStepsMade++;
                        responseQueryModel.AddBotSuggestion(response);

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

                    if (transformationCandidate.HasArguments && queryItems.Any())
                    {
                        // Get all possible transformations for the transformation
                        var nextPossibleArguments = transformationCandidate.GetArguments();
                        nextMoves = _communicationAgent.CreateNextQuestion($"---> {transformationCandidate.GetArgumentsInstructions()}", nextPossibleArguments);

                        // loop until getting satisfying answer
                        while (queryItems.Any())
                        {
                            // obtain the message
                            queryItems.RemoveAt(0);

                            responseQueryModel.NextMoves = nextMoves;

                            nextQueryItem = queryItems.FirstOrDefault();
                            index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                            var response = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index);
                            totalStepsMade++;
                            responseQueryModel.AddBotSuggestion(response);

                            // check the message
                            bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);
                            bool isInt = Int32.TryParse(response, out int choice);
                            bool isInRange = choice < nextPossibleArguments.Count();

                            // act
                            if (isNotNullOrEmpty && isInt && isInRange)
                            {
                                firstArgument = transformationCandidate.GetArgumentAt(choice);
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

                        if (transformationCandidate.HasFollowingHumanArguments && queryItems.Any())
                        {
                            _communicationAgent.CreateNextQuestion(transformationCandidate.GetFollowingHumanArgumentsInstructions());

                            // loop until getting satisfying answer
                            while (queryItems.Any())
                            {
                                // obtain the message
                                queryItems.RemoveAt(0);

                                nextQueryItem = queryItems.FirstOrDefault();
                                index = responseQueryModel.NextMoves.ToList().IndexOf(nextQueryItem);

                                var response = _communicationAgent.GetResponse(querySoFar, nextQueryItem, index, true);
                                totalStepsMade++;
                                responseQueryModel.AddBotSuggestion(response, true);

                                // check the message
                                bool isNotNullOrEmpty = !string.IsNullOrEmpty(response);

                                // act
                                if (isNotNullOrEmpty)
                                {
                                    secondArgument = response;
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

                    if (transformationCandidate is not null && totalStepsMade == transformationCandidate.TotalStepsNeeded)
                    {
                        // build the transformation
                        generatedTransformation = TransformationFactory.BuildTransformation(transformationName, new string[] { nextMove, firstArgument, secondArgument });

                        // blocks building the transformation when the last argument is given by chatBot
                        if (queryItems.Any())
                            responseQueryModel.AddTransformation(generatedTransformation);

                        // rebuild the possible response
                        _response = generatedTransformation.Preprocess(_response);
                    }
                }
                catch (ArgumentException ex)
                {
                    _communicationAgent.ErrorMessage($"{ex.Message}. Please try again.");
                    _communicationAgent.Indent();
                }
                _communicationAgent.Indent();
            }

            if (_communicationAgent.Verbose)
                _communicationAgent.ShowConversationHistory();

            return responseQueryModel;
        }

        #endregion
    }
}
