using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Services
{
    public interface IEmailService
    {
        Task SendInterviewInvitationAsync(Interview interview);
        Task SendApplicationReceivedAsync(Application application);
        Task SendApplicationStatusChangeAsync(Application application, string newStatus);
        Task SendInterviewReminderAsync(Interview interview);
        Task SendInterviewCancellationAsync(Interview interview);
        Task SendInterviewRescheduleAsync(Interview interview);
    }
}
