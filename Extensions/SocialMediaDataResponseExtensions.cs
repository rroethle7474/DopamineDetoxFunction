using DopamineDetoxFunction.Models;

namespace DopamineDetoxFunction.Extensions
{
    public static class SocialMediaDataResponseExtensions
    {

        public static SocialMediaDataResponse CheckTotalSuccessCounts(this SocialMediaDataResponse response, int successCount, int totalCount)
        {
            if (successCount > 0)
            {
                response.ReportMessage = $"Successfully saved {successCount} search results";
            }
            else if (totalCount > 0)
            {
                response.ReportMessage = "No new records found";
            }
            else
            {
                response.ReportMessage = "No search results saved";
            }

            return response;
        }
    }
}
