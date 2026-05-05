namespace GP.Application.DTOs.Profile;

public class ActiveChallengeDto
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Type { get; set; }
    public int CurrentProgress { get; set; }
    public int GoalValue { get; set; }
    public int RewardPoints { get; set; }
}
