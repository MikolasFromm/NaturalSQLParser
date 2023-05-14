using NaturalSQLParser.Types.Enums;
using OpenAI_API;
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
            var chatIntro = "You are an assistant which should translate user input into query request, which will be later executed on the given dataset. " +
                "You will always get a list of options started wtih '--->' from which you can choose the best following option to fulfill the user request. " +
                "Each choosen word must be separated with one space and all transformation parameters must be on one line." +
                "When you are finished with processing the query, only send empty string which is translated to END OF QUERY. " +
                "NOTE THAT YOU MUST RETURN ONLY THE SAME WORDS THAT WERE GIVEN FROM THE SELECTION FOLLOWED BY '--->'";

            if (_verbose)
                Console.WriteLine($"ChatBot intro: {chatIntro}");
            
            _chat.AppendSystemMessage(chatIntro);

            Console.WriteLine("Write your query: ");
            var userQuery = Console.ReadLine();

            if (_verbose)
                Console.WriteLine($"User input: {userQuery}");

            _chat.AppendUserInput(userQuery);

            _chat.AppendSystemMessage("Now it is your turn to choose the right operations.");
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
            {
                Console.WriteLine(message);
            }
            if (_mode == CommunicationAgentMode.AIBot)
            {
                _chat.AppendSystemMessage(message);
            }
        }

        /// <summary>
        /// Adds user input to the system conversation. Relevant only for AIBot mode.
        /// </summary>
        /// <param name="message"></param>
        public void InsertUserMessage(string message)
        {
            if (_verbose)
            {
                Console.WriteLine(message);
            }

            if ( _mode == CommunicationAgentMode.AIBot)
            {
                _chat.AppendUserInput(message);
            }
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
        /// New-line indenting for more readable output. Relevant only for VERBOSE mode.
        /// </summary>
        public void Indent()
        {
            if (_verbose)
            {
                Console.WriteLine();
            }
        }
    }
}
