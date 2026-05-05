using GP.Domain.Enums;

namespace GP.Domain.Entities
{
    public class Challenge
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ChallengeType Type { get; set; }
        public int GoalValue { get; set; }
        public int RewardPoints { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<UserChallenge> UserChallenges { get; set; } = [];
    }
}
