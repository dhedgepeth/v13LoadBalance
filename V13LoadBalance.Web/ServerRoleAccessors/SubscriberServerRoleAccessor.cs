using Umbraco.Cms.Core.Sync;

namespace V13LoadBalance.Web.ServerRoleAccessors
{
    public class SubscriberServerRoleAccessor : IServerRoleAccessor
    {
        public ServerRole CurrentServerRole => ServerRole.Subscriber;
    }
}