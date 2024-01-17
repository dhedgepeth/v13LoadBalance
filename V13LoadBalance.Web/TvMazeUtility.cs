using System.Net;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Strings;
using Umbraco.Deploy.Infrastructure.Extensions;
using System.Net.Http.Formatting;

namespace V13LoadBalance.Web;

public class TvMazeUtility
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly ILogger<TvMazeUtility> _logger;
    private readonly IVariationContextAccessor _variationContextAccessor;

    public TvMazeUtility(IUmbracoContextFactory umbracoContextFactory,
        IContentService contentService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGeneratorCollection,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        ILogger<TvMazeUtility> logger,
        IVariationContextAccessor variationContextAccessor)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _contentService = contentService;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _logger = logger;
        _variationContextAccessor = variationContextAccessor;
    }

    public string MoveTvShowsFromTvMazeToUmbraco()
    {
        int page = 0;

        Uri ShowsAPI(int page) => new($"https://api.tvmaze.com/shows?page={page}");

        HttpClient client = new();
        bool breakNow = false;
        while (true)
        {
            var response = client.GetAsync(ShowsAPI(page++)).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            var shows = response.Content.ReadAsAsync<TVMazeShow[]>(new[] { new JsonMediaTypeFormatter() }).Result;
            try { response.EnsureSuccessStatusCode(); } catch { break; }
            if (shows.Any())
            {
               /* Parallel.ForEach(shows, show => {
                    InsertedOrUpdated(show);
                });*/
               foreach (var show in shows)
                {
                    InsertedOrUpdated(show);
                }
            }
        }
        return $"Sync complete until page {page}";
    }


    private bool InsertedOrUpdated(TVMazeShow show)
    {

        using (var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext())
        {
            var TvshowLibrary = umbracoContextReference.UmbracoContext.Content.GetById(_contentService.GetRootContent().First().Id) as Home;
            var culture = "en-US";
            TVshowPage existingTvShowInUmbraco = null;
            var existingTvShowsInUmbraco = TvshowLibrary.Children<TVshowPage>(_variationContextAccessor, culture).Where(t => t.TvShowID == show.Id.ToString());

            if (existingTvShowsInUmbraco?.Any() ?? false)
            {
                if (existingTvShowsInUmbraco.Count() > 1)
                {
                    existingTvShowInUmbraco = existingTvShowsInUmbraco.OrderBy(t => t.CreateDate).First();
                    foreach (var showToDelete in existingTvShowsInUmbraco.Where(s => s.Id != existingTvShowInUmbraco.Id))
                    {
                        _contentService.Delete(_contentService.GetById(showToDelete.Id));
                    }
                }
                else
                {
                    existingTvShowInUmbraco = existingTvShowsInUmbraco.FirstOrDefault();
                }
            }

            if (existingTvShowInUmbraco == null)
            {
                var media = ImportMediaFromTVMazeToUmbraco(show);
                var newTvShow = _contentService.Create(show.Name, TvshowLibrary.Id, TVshowPage.ModelTypeAlias);
                //newTvShow.SetCultureName(show.Name, culture);
                newTvShow.SetValue(nameof(TVshowPage.TvShowID), show.Id);

                if (media != null)
                {
                    newTvShow.SetValue(nameof(TVshowPage.Thumbnail), media.GetUdi());
                }

                _contentService.SaveAndPublish(newTvShow);
                return true;
            }
            return Updated(show, existingTvShowInUmbraco);
        }
    }

    public IMedia ImportMediaFromTVMazeToUmbraco(TVMazeShow tvMazeShow)
    {

        if (tvMazeShow == null || string.IsNullOrEmpty(tvMazeShow.Name) || string.IsNullOrEmpty(tvMazeShow.Image?.Original))
        {
            return null;
        }

        var webRequest = (HttpWebRequest)WebRequest.Create(tvMazeShow.Image.Original);
        webRequest.AllowWriteStreamBuffering = true;
        webRequest.Timeout = 30000;

        var fileName = $"{tvMazeShow.Id}_{GetFileNameFromUrl(tvMazeShow.Image.Original)}";

        var existingFolder = CreateOrGetMediaFolderFromUmbraco(tvMazeShow.Name);

        var webResponse = webRequest.GetResponse();
        var stream = webResponse.GetResponseStream();

        IMedia media = _mediaService.CreateMedia(fileName, existingFolder.Id, Constants.Conventions.MediaTypes.Image);

        media.SetValue(_mediaFileManager, _mediaUrlGeneratorCollection, _shortStringHelper, _contentTypeBaseServiceProvider, Constants.Conventions.Media.File, fileName, stream);
        try
        {
            _mediaService.Save(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, fileName);
            return null;
        }

        return media;
    }

    private IMedia CreateOrGetMediaFolderFromUmbraco(string tvShowName)
    {
        const string othersFolder = "Others";

        char firstChar = char.ToUpper(tvShowName[0]);

        var parentFolder = _mediaService.GetRootMedia().FirstOrDefault(x => x.Name == "TV Shows");
        if (parentFolder == null)
        {
            parentFolder = _mediaService.CreateMedia("TV Shows", Constants.System.Root, Constants.Conventions.MediaTypes.Folder);
            _mediaService.Save(parentFolder);
        }

        //var existingFolder = _mediaService.GetRootMedia().FirstOrDefault(x => x.Name == firstChar.ToString()); 
        var childFolders = _mediaService.GetPagedChildren(parentFolder.Id, 0, int.MaxValue, out _);
        var existingFolder = childFolders.FirstOrDefault(x => x.Name == firstChar.ToString());

        if (existingFolder == null)
        {
            if (Regex.IsMatch(firstChar.ToString(), @"^[a-zA-Z]+$", RegexOptions.IgnoreCase))
            {
                existingFolder = _mediaService.CreateMedia(firstChar.ToString(), parentFolder,
                    Constants.Conventions.MediaTypes.Folder);
                _mediaService.Save(existingFolder);
            }
            else
            {
                existingFolder = _mediaService.GetRootMedia().FirstOrDefault(x => x.Name == othersFolder);

                if (existingFolder == null)
                {
                    existingFolder = _mediaService.CreateMedia(othersFolder, parentFolder,
                        Constants.Conventions.MediaTypes.Folder);
                    _mediaService.Save(existingFolder);
                }
            }
        }
        return existingFolder;
    }


    private string GetFileNameFromUrl(string url)
    {
        // Get the last part of the URL after the last slash '/'
        int lastSlashIndex = url.LastIndexOf('/');
        string filenameWithExtension = url.Substring(lastSlashIndex + 1);

        return filenameWithExtension;
    }


    private bool Updated(TVMazeShow show, TVshowPage existingTvShowInUmbraco)
    {
        // todo
        return false;
    }
}