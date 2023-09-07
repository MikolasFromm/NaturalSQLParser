using NaturalSQLParser.Types.Tranformations;

namespace NaturalSQLParser.Model
{
    public class QueryViewModel
    {
        private List<ITransformation> _transformations = new List<ITransformation>();

        private List<string> _nextMoves = new List<string>();

        private string _botSuggestion = string.Empty;

        private int _botSuggestionIndex = -1;

        public int BotSuggestionIndex { get { return _botSuggestionIndex; } }

        public string BotSuggestion { get { return _botSuggestion; } }

        public IEnumerable<ITransformation> Transformations { get { return _transformations; } }

        public IEnumerable<string> NextMoves { get { return _nextMoves; } set { _nextMoves = value.ToList(); } }

        public void AddTransformation(ITransformation transformation) { _transformations.Add(transformation); }

        public string AddBotSuggestion(string suggestion) 
        {
            _botSuggestion = suggestion;

            if (!Int32.TryParse(suggestion, out _botSuggestionIndex))
            {
                _botSuggestionIndex = -1;
            }

            if (_botSuggestionIndex >= 0 && _botSuggestionIndex < _nextMoves.Count)
            {
                _botSuggestion = _nextMoves[_botSuggestionIndex];

                return BotSuggestion;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
