using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DA_Assets.FCU
{

    public static class DownloadResultLogger
    {
        public static void LogFailedDownloads(ConcurrentBag<FObject> failedObjects, int splitLimit)
        {
            if (failedObjects.IsEmpty())
            {
                return;
            }

            List<List<string>> components = failedObjects.Select(x => x.Data.NameHierarchy).Split(splitLimit);

            foreach (List<string> component in components)
            {
                string hierarchies = string.Join("\n", component);
                UnityEngine.Debug.LogError(FcuLocKey.log_malformed_url.Localize(component.Count, hierarchies));
            }
        }
    }
}
