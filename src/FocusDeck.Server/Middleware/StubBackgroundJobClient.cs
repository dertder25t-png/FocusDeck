using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace FocusDeck.Server.Middleware
{
    /// <summary>
    /// A no-op background job client for SQLite development environments
    /// where Hangfire server is not configured
    /// </summary>
    public class StubBackgroundJobClient : IBackgroundJobClient
    {
        public string Create(Job job, IState state)
        {
            return Guid.NewGuid().ToString();
        }

        public bool ChangeState(string jobId, IState state, string expectedState)
        {
            return true;
        }

        public bool ChangeState(string jobId, IState state, string[] expectedStates)
        {
            return true;
        }
    }
}
