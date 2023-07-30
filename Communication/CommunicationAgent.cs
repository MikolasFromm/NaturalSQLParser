#define INDEXING

using NaturalSQLParser.Types.Enums;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System.Diagnostics.Tracing;

namespace NaturalSQLParser.Communication
{
    public class CommunicationAgent
    {
        private CommunicationAgentMode _mode;

        private OpenAIAPI? _api;

        private bool _verbose;

        private OpenAI_API.Chat.Conversation? _chat;

        private void BotIntroduction()
        {

            _chat.AppendSystemMessage("You are an assistant who should translate given user input into a query request.");
            _chat.AppendSystemMessage("You always get instructions and options from which you can choose.");

#if WORD_MATCHING
            _chat.AppendSystemMessage("You must use at most 2 words only from the selection given."); // default PerformQuery
            _chat.AppendSystemMessage("But usually use just one word."); // default PerformQuery
            _chat.AppendSystemMessage("You can not use any other words than the ones given from the user input."); // default PerformQuery
#endif

#if INDEXING
            _chat.AppendSystemMessage("You write your choice as a number from the [] brackets. Dont write anything else!"); // extension PerformQueryWithIndices
            _chat.AppendSystemMessage("Only when you are asked to write a word, you can write any word you want."); // extension PerformQueryWithIndices
#endif

            _chat.AppendSystemMessage("Dont ask any questions or dont give any following options. Just answer.");

            Console.WriteLine("Write your query: ");
            var userQuery = Console.ReadLine();

            if (_verbose)
                Console.WriteLine($"User input: {userQuery}");

            _chat.AppendSystemMessage($"Query: {userQuery}");
        }

        /// <summary>
        /// Constructor for OpenAIAPIBot feature. Use when communication with bot required. <see cref="OpenAIAPI"/> should be already initialized with API-token and all related settings.
        /// Also initializes the chatBot with introduction and gives him the user query input obtained via <see cref="Console"/>.
        /// Creates new local <see cref="OpenAI_API.Chat.Conversation"/> instance.
        /// </summary>
        /// <param name="api">Initialized API for OpenAI bot.</param>
        /// <param name="verbose"></param>
        public CommunicationAgent(OpenAIAPI api, bool verbose)
        {
            _api = api;
            _mode = CommunicationAgentMode.AIBot;
            _verbose = verbose;

            _chat = _api.Chat.CreateConversation();
            _chat.RequestParameters.Temperature = 0;
            _chat.RequestParameters.TopP = 0;
            _chat.RequestParameters.Model = Model.ChatGPTTurbo0301;

            this.BotIntroduction();
        }

        /// <summary>
        /// Constructor for user inputs. Will not provide any responses.
        /// </summary>
        /// <param name="verbose"></param>
        public CommunicationAgent(bool verbose)
        {
            _mode = CommunicationAgentMode.User;
            _verbose = verbose;
        }

        /// <summary>
        /// Represents current class setting status
        /// </summary>
        public CommunicationAgentMode Mode { get { return _mode; } }

        /// <summary>
        /// Add (system) message to the conversation.
        /// </summary>
        /// <param name="message"></param>
        public void InsertSystemMessage(string message)
        {
            if (_verbose)
                Console.WriteLine(message);

            if (_mode == CommunicationAgentMode.AIBot)
                _chat.AppendSystemMessage(message);
        }

        /// <summary>
        /// Adds user input to the system conversation. Relevant only for AIBot mode.
        /// </summary>
        /// <param name="message"></param>
        public void InsertUserMessage(string message)
        {
            Console.WriteLine(message);

            if ( _mode == CommunicationAgentMode.AIBot)
                _chat.AppendUserInput(message);
        }

        /// <summary>
        /// Default Error message "stream". Verbose independent
        /// </summary>
        /// <param name="message"></param>
        public void ErrorMessage(string message)
        {
            if (_mode == CommunicationAgentMode.User)
                Console.WriteLine($"ERROR: {message}");

            if (_mode == CommunicationAgentMode.AIBot)
                this.InsertUserMessage($"ERROR: {message}");
        }

        /// <summary>
        /// Get response to the given query. If userMode set, content from <see cref="Console"/> will be given.
        /// </summary>
        /// <returns></returns>
        public string GetResponse()
        {
            if (_mode == CommunicationAgentMode.User)
            {
                string response = Console.ReadLine();
                
                if (_verbose)
                {
                    Console.WriteLine($"User response: {response}");
                }

                if (response is not null)
                    return response;
            }
           
            if (_mode == CommunicationAgentMode.AIBot)
            {
                string response = _chat.GetResponseFromChatbotAsync().Result;
               
                if (_verbose)
                {
                    Console.WriteLine($"Bot response: {response}");
                }
                
                return response;
            }

            return string.Empty;
        }

        /// <summary>
        /// Show the whole conversation history with chatbot.
        /// </summary>
        public void ShowConversationHistory()
        {
            if (_mode == CommunicationAgentMode.AIBot)
            {
                foreach (var message in _chat.Messages)
                {
                    Console.WriteLine($"{message.Role}: {message.Content}");
                }
            }
        }

        /// <summary>
        /// New-line indenting for more readable output. Relevant only for VERBOSE mode.
        /// </summary>
        public void Indent()
        {
            Console.WriteLine();
        }
    }
}
