using System.Collections.Generic;
using Autofac;
using CKAN.GameVersionProviders;
using CKAN.Versioning;
using CKAN.NetKAN.Model;

namespace CKAN.NetKAN.Validators
{
    internal sealed class MatchesKnownGameVersionsValidator : IValidator
    {
        public MatchesKnownGameVersionsValidator()
        {
            buildMap = ServiceLocator.Container.Resolve<IKspBuildMap>();
            buildMap.Refresh(BuildMapSource.Embedded);
        }

        public void Validate(Metadata metadata)
        {
            var mod = CkanModule.FromJson(metadata.Json().ToString());
            if (!mod.IsCompatibleKSP(new KspVersionCriteria(null, buildMap.KnownVersions)))
            {
                KspVersion minKsp = null, maxKsp = null;
                Registry.GetMinMaxVersions(new List<CkanModule>() {mod}, out _, out _, out minKsp, out maxKsp);
                throw new Kraken($"{metadata.Identifier} doesn't match any valid game version: {KspVersionRange.VersionSpan(minKsp, maxKsp)}");
            }
        }

        private IKspBuildMap buildMap;
    }
}
