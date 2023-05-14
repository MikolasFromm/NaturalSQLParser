﻿using NaturalSQLParser.Communication;
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
                    var transformationCandidate = TransformationFactory.GetTransformationCandidate(transformationName);

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
                            _communicationAgent.ErrorMessage($"Invalid input, you must choose from the options given above. Pleas try again.");
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
    }
}
