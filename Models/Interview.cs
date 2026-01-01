using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Interview
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public int RecruiterId { get; set; }

        // Interview Details
        [Required]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [Range(15, 480)] // 15 minutes to 8 hours
        public int DurationMinutes { get; set; }

        [Required]
        public InterviewType Type { get; set; }

        [Required]
        public InterviewRound Round { get; set; }

        [Required]
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

        // Location/Link
        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        [MaxLength(2000)]
        public string? SpecialInstructions { get; set; }

        // Feedback
        [MaxLength(5000)]
        public string? Feedback { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(1000)]
        public string? CancellationReason { get; set; }

        // Navigation Properties
        public Application? Application { get; set; }
        public Recruiter? Recruiter { get; set; }
    }

    public enum InterviewType
    {
        Phone,
        Video,
        InPerson
    }

    public enum InterviewRound
    {
        Screening,
        Technical,
        HR,
        Final,
        Other
    }

    public enum InterviewStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        Rescheduled,
        NoShow
    }
}
