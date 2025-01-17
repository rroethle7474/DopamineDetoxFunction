using DopamineDetoxFunction.Models;
using Microsoft.Azure.Functions.Worker;

namespace DopamineDetoxFunction.Services
{
    public interface ISignalRService
    {
        public SignalRMessageAction CreateDataUpdateNotification(SocialMediaTimerDataResponse response);
        public SignalRMessageAction CreateQuoteDataUpdateNotification(bool hasQuoteErrors = false);
    }
}
