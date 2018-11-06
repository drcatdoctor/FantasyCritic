using CSharpFunctionalExtensions;

namespace FantasyCritic.Lib.Domain.Requests
{
    public class ClaimGameDomainRequest
    {
        public ClaimGameDomainRequest(Publisher publisher, string gameName, bool counterPick, bool managerOverride, Maybe<MasterGame> masterGame)
        {
            Publisher = publisher;
            GameName = gameName;
            CounterPick = counterPick;
            ManagerOverride = managerOverride;
            MasterGame = masterGame;
        }

        public Publisher Publisher { get; }
        public string GameName { get; }
        public bool CounterPick { get; }
        public bool ManagerOverride { get; }
        public Maybe<MasterGame> MasterGame { get; }
    }
}
