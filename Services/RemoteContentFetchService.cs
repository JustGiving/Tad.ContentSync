using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Recipes.Services;

namespace Tad.ContentSync.Services {
    public class RemoteContentFetchService : IRemoteContentFetchService {
        private readonly IRecipeParser _recipeParser;
        private readonly IOrchardServices _orchardServices;
        private readonly Lazy<IEnumerable<IContentHandler>> _handlers;

        public RemoteContentFetchService(
            IRecipeParser recipeParser,
            IOrchardServices orchardServices,
            Lazy<IEnumerable<IContentHandler>> handlers)
        {
            _recipeParser = recipeParser;
            _orchardServices = orchardServices;
            _handlers = handlers;
        }

        public IEnumerable<ContentItem> Fetch(Uri remoteInstanceRoot) {
            var remoteExportEndpoint = new Uri(remoteInstanceRoot + "/Admin/ContentImportExport/Export");
            string remoteXml = FetchRemoteExportXml(remoteExportEndpoint);
            List<ContentItem> contentItems = new List<ContentItem>();

            var recipe = _recipeParser.ParseRecipe(remoteXml);
            var importContentSession = new ImportContentSession(_orchardServices.ContentManager);
            foreach (var step in recipe.RecipeSteps)
            {
                foreach (var element in step.Step.Elements())
                {
                    if (!ContentSync.SyncableContentTypes.Contains(element.Name.LocalName))
                        continue;

                    var elementId = element.Attribute("Id");
                    if (elementId == null)
                        continue;

                    var identity = elementId.Value;
                    var status = element.Attribute("Status");

                    var item = _orchardServices.ContentManager.New(element.Name.LocalName);

                    var context = new ImportContentContext(item, element, importContentSession);

                    foreach (var contentHandler in _handlers.Value)
                    {
                        contentHandler.Importing(context);
                    }

                    foreach (var contentHandler in _handlers.Value)
                    {
                        contentHandler.Imported(context);
                    }


                    contentItems.Add(item);
                }
            }
            _orchardServices.ContentManager.Clear();

            return contentItems;
        }

        private string FetchRemoteExportXml(Uri remoteExportUrl) {

            string etag = "";

            string xml = "";
            HttpWebRequest request = HttpWebRequest.Create(remoteExportUrl) as HttpWebRequest;
            using (var response = request.GetResponse() as HttpWebResponse) {
                if(response.Headers["ETag"] != null) {
                    etag = response.Headers["ETag"];
                }

                using (var stream = response.GetResponseStream()) 
                using (var reader = new StreamReader(stream)) {
                    xml = reader.ReadToEnd();
                }
            }
            
            return xml;
        }

    }
}