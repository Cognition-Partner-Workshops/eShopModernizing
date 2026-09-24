using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace eShopLegacyMVC
{
    public static class SessionInfo
    {
        public const string MachineNameKey = "MachineName";
        public const string SessionStartTimeKey = "SessionStartTime";

        public static void EnsureInitialized(ISession session)
        {
            if (session.GetString(MachineNameKey) == null)
            {
                session.SetString(MachineNameKey, Environment.MachineName);
                session.SetString(SessionStartTimeKey, DateTime.Now.ToString());
            }
        }
    }

    public class ActivityIdHelper
    {
        public override string ToString()
        {
            if (Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }

            return Trace.CorrelationManager.ActivityId.ToString();
        }
    }

    public class WebRequestInfo
    {
        private readonly string _info;

        public WebRequestInfo(HttpContext context)
        {
            _info = context.Request.Path + context.Request.QueryString + ", " + context.Request.Headers.UserAgent;
        }

        public override string ToString() => _info;
    }
}
