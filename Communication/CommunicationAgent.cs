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

        /// <summary>
        /// Constructor for OpenAIAPIBot feature. Use when communication with bot required. <see cref="OpenAIAPI"/> should be already initialized with API-token and all related settings.
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
    }
}
