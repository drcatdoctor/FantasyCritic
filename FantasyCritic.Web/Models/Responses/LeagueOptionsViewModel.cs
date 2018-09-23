using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Domain.ScoringSystems;
using FantasyCritic.Lib.Enums;

namespace FantasyCritic.Web.Models.Responses
{
    public class LeagueOptionsViewModel
    {
        public LeagueOptionsViewModel(IEnumerable<int> openYears, IEnumerable<DraftSystem> draftSystems,
            IEnumerable<WaiverSystem> waiverSystems, IEnumerable<ScoringSystem> scoringSystems, IEnumerable<EligibilityLevel> eligibilityLevels)
        {
            OpenYears = openYears.ToList();
            DraftSystems = draftSystems.Select(x => x.Value).ToList();
            WaiverSystems = waiverSystems.Select(x => x.Value).ToList();
            ScoringSystems = scoringSystems.Select(x => x.Name).ToList();
            EligibilityLevels = eligibilityLevels.Select(x => new EligibilityLevelViewModel(x, true)).ToList();
        }

        public IReadOnlyList<int> OpenYears { get; }
        public IReadOnlyList<string> DraftSystems { get; }
        public IReadOnlyList<string> WaiverSystems { get; }
        public IReadOnlyList<string> ScoringSystems { get; }
        public IReadOnlyList<EligibilityLevelViewModel> EligibilityLevels{ get; }
    }
}
