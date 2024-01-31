using Examine.Lucene;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace V13LoadBalance.Web.Indexes
{
    public class TVshowPageIndex : UmbracoExamineIndex
    {
        public TVshowPageIndex(
            ILoggerFactory loggerFactory, 
            string name, 
            IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions, 
            Umbraco.Cms.Core.Hosting.IHostingEnvironment hostingEnvironment, 
            IRuntimeState runtimeState)
            : base(loggerFactory, 
                  name, 
                  indexOptions, 
                  hostingEnvironment, 
                  runtimeState)
        {
        }
    }
}
