using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace V13LoadBalance.Web.Indexes
{
    public class TVshowPageIndexPopulator : IndexPopulator
    {
        private readonly IContentService _contentService;
        private readonly TVshowPageIndexValueSetBuilder _tvShowPageIndexValueSetBuilder;

        public TVshowPageIndexPopulator(IContentService contentService, TVshowPageIndexValueSetBuilder tvShowPageIndexValueSetBuilder)
        {
            _contentService = contentService;
            _tvShowPageIndexValueSetBuilder = tvShowPageIndexValueSetBuilder;
            RegisterIndex("TVshowPageIndex");
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            foreach (IIndex index in indexes)
            {
                IContent[] roots = _contentService.GetRootContent().ToArray();
                index.IndexItems(_tvShowPageIndexValueSetBuilder.GetValueSets(roots));

                foreach (IContent root in roots)
                {
                    const int pageSize = 10000;
                    var pageIndex = 0;
                    IContent[] descendants;
                    do
                    {
                        descendants = _contentService.GetPagedDescendants(root.Id, pageIndex, pageSize, out _).ToArray();
                        IEnumerable<ValueSet> valueSets = _tvShowPageIndexValueSetBuilder.GetValueSets(descendants);
                        index.IndexItems(valueSets);

                        pageIndex++;
                    }
                    while (descendants.Length == pageSize);
                }
            }
        }
    }
}
