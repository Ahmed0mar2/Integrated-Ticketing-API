using System;

namespace GP.Domain.Entities
{
    public class UserChallenge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int ChallengeId { get; set; }
        public Challenge Challenge { get; set; } = null!;
        public int CurrentProgress { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
    }
}
