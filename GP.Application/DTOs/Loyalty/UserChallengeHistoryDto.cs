namespace GP.Application.DTOs.Loyalty
{
    public class UserChallengeHistoryDto
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int CurrentProgress { get; set; }
        public int GoalValue { get; set; }
        public int RewardPoints { get; set; }
        public bool IsCompleted { get; set; }
    }
}
