using RESUMATE_FINAL_WORKING_MODEL.Models;

namespace RESUMATE_FINAL_WORKING_MODEL.Services
{
    public interface IIcsCalendarService
    {
        string GenerateIcsFile(Interview interview);
        byte[] GenerateIcsFileBytes(Interview interview);
    }
}
