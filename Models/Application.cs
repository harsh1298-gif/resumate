using System;
using System.ComponentModel.DataAnnotations;

namespace RESUMATE_FINAL_WORKING_MODEL.Models
{
    public class Application
    {
        public int Id { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; } = "Pending";

        public int ApplicantId { get; set; }
        public Applicant? Applicant { get; set; } // Change

        public int JobId { get; set; }
        public Job? Job { get; set; } // Change
    }
}