using Umbraco.Cms.Core.Sync;

namespace V13LoadBalance.Web.ServerRoleAccessors
{
    public class SingleServerRoleAccessor : IServerRoleAccessor
    {
        public ServerRole CurrentServerRole => ServerRole.Single;
    }
}