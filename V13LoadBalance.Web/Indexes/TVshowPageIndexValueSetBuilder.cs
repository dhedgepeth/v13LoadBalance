using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;

namespace V13LoadBalance.Web.Indexes
{
    public class TVshowPageIndexValueSetBuilder : IValueSetBuilder<IContent>
    {
        public IEnumerable<ValueSet> GetValueSets(params IContent[] contents)
        {
            foreach (IContent content in contents.Where(CanAddToIndex))
            {
                var indexValues = new Dictionary<string, object>
                {
                    // this is a special field used to display the content name in the Examine dashboard
                    [UmbracoExamineFieldNames.NodeNameFieldName] = content.Name!,
                    ["name"] = content.Name!,
                    // add the fields you want in the index
                    ["nodeName"] = content.Name!,
                    ["id"] = content.Id,
                };

                yield return new ValueSet(content.Id.ToString(), IndexTypes.Content, content.ContentType.Alias, indexValues);
            }
        }
        // filter out all content types except "product"
        private bool CanAddToIndex(IContent content) => content.ContentType.Alias == "product";
    }
}