using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopamineDetoxFunction.Models
{
    public class SignalRMessageResponse
    {
        [SignalROutput(HubName = "socialmedia")]
        public SignalRMessageAction SignalRMessage { get; set; }
        public IActionResult HttpResponse { get; set; }
    }
}
