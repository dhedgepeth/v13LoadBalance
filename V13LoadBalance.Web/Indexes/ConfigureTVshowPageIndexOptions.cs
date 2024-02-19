using Examine;
using Examine.Lucene;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;

namespace V13LoadBalance.Web.Indexes
{
    public class ConfigureTVshowPageIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
    {
        private readonly IOptions<IndexCreatorSettings> _settings;

        public ConfigureTVshowPageIndexOptions(IOptions<IndexCreatorSettings> settings) 
            => _settings = settings;

        public void Configure(string? name, LuceneDirectoryIndexOptions options)
        {
            if (name?.Equals("ProductIndex") is false)
            {
                return;
            }

            options.Analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

            options.FieldDefinitions = new(
                new("id", FieldDefinitionTypes.Integer),
                new("name", FieldDefinitionTypes.FullText)
            );

            options.UnlockIndex = true;

            if (_settings.Value.LuceneDirectoryFactory == LuceneDirectoryFactory.SyncedTempFileSystemDirectoryFactory)
            {
                // if this directory factory is enabled then a snapshot deletion policy is required
                options.IndexDeletionPolicy = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
            }
        }

        public void Configure(LuceneDirectoryIndexOptions options)
        {
            throw new NotImplementedException();
        }
    }
}