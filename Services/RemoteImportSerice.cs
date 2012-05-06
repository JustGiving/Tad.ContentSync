using System.Collections.Generic;
using System.Linq;
using ContentSync.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Logging;

namespace ContentSync.Services {
    public class RemoteImportSerice : IRemoteImportService {
        private readonly IContentManager _contentManager;
        public ILogger Logger { get; set; }

        public RemoteImportSerice(
            IContentManager contentManager,
            ILoggerFactory loggerFactory) {
            _contentManager = contentManager;
            Logger = loggerFactory.CreateLogger(this.GetType());
        }

        public void Import(IEnumerable<ImportSyncAction> actions) {
            // process replacements
            var importContentSession = new ImportContentSession(_contentManager);
            foreach (var sync in actions.Where(a => a.Action == "Replace"))
            {
                Logger.Debug("{0}, {1}", sync.Action, sync.TargetId);
                Replace(sync, importContentSession);
            }

            // import
            importContentSession = new ImportContentSession(_contentManager);
            foreach (var action in actions)
            {
                ImportItem(action, importContentSession);
            }

        }

        private void ImportItem(ImportSyncAction action, ImportContentSession session) {
            Logger.Debug("Importing {0}", action.Step.Step.Element("Id"));
            _contentManager.Import(action.Step.Step, session);
        }

        private void Replace(ImportSyncAction action, ImportContentSession session)
        {
            // update the identifier on the item in the local instance
            // then let the import continue so the existing item gets paved over
            if (action.Action == "Replace" && !string.IsNullOrWhiteSpace(action.TargetId))
            {
                var item = session.Get(action.TargetId);

                if (item == null)
                {
                    return;
                }

                var newIdentifier = action.Step.Step.Attribute("Id");
                if (newIdentifier == null)
                    return;

                var newIdentity = new ContentIdentity(newIdentifier.Value);
                var existingIdentity = new ContentIdentity(action.TargetId);
                if (!newIdentity.Equals(existingIdentity))
                {
                    Logger.Debug("import - items {0} and {1} have different identifiers", existingIdentity.Get("Identifier"), newIdentity.Get("Identifier"));
                    item.As<IdentityPart>().Identifier = newIdentity.Get("Identifier");
                    session.Store(newIdentity.Get("Identifier"), item);
                }
            }

        }
    }
}