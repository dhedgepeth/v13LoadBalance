using Umbraco.Cms.Core.Sync;

namespace V13LoadBalance.Web.ServerRoleAccessors
{
    public class SchedulingPublisherServerRoleAccessor : IServerRoleAccessor
    {
        public ServerRole CurrentServerRole => ServerRole.SchedulingPublisher;
    }
}