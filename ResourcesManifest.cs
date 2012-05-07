﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.UI.Resources;

namespace ContentSync
{
    public class ResourcesManifest : IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder) {
            var manifest = builder.Add();
            manifest.DefineScript("jquery-ui").SetUrl("jquery-ui-1.8.20.js").SetDependencies("jQuery");
            manifest.DefineStyle("jquery-ui").SetUrl("jquery-ui.1.8.20.custom.css");
        }
    }
}