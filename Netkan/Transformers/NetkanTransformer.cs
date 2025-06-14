using System.Collections.Generic;
using System.Linq;

using CKAN.NetKAN.Model;
using CKAN.NetKAN.Services;
using CKAN.NetKAN.Validators;
using CKAN.NetKAN.Sources.Curse;
using CKAN.NetKAN.Sources.Github;
using CKAN.NetKAN.Sources.Gitlab;
using CKAN.NetKAN.Sources.Jenkins;
using CKAN.NetKAN.Sources.Spacedock;
using CKAN.Games;
using CKAN.NetKAN.Sources.SourceForge;

namespace CKAN.NetKAN.Transformers
{
    /// <summary>
    /// An <see cref="ITransformer"/> that can perform a complete transform from a NetKAN to CKAN metadata.
    /// </summary>
    internal sealed class NetkanTransformer : ITransformer
    {
        private readonly List<ITransformer> _transformers;
        private readonly IValidator _validator;

        public string Name => "netkan";

        public NetkanTransformer(IHttpService   http,
                                 IFileService   fileService,
                                 IModuleService moduleService,
                                 string?        githubToken,
                                 string?        gitlabToken,
                                 string?        userAgent,
                                 bool?          prerelease,
                                 IGame          game,
                                 IValidator     validator)
        {
            _validator = validator;
            var ghApi = new GithubApi(http, githubToken);
            var glApi = new GitlabApi(http, gitlabToken);
            var sfApi = new SourceForgeApi(http);
            _transformers = InjectVersionedOverrideTransformers(new List<ITransformer>
            {
                new StagingTransformer(game),
                new MetaNetkanTransformer(http, ghApi),
                new SpacedockTransformer(new SpacedockApi(http), ghApi),
                new CurseTransformer(new CurseApi(http, userAgent), userAgent),
                new GithubTransformer(ghApi, prerelease),
                new GitlabTransformer(glApi),
                new SourceForgeTransformer(sfApi),
                new HttpTransformer(http, userAgent),
                new JenkinsTransformer(new JenkinsApi(http)),
                new AvcKrefTransformer(http, ghApi),
                new InternalCkanTransformer(http, moduleService, game),
                new SpaceWarpInfoTransformer(http, ghApi, moduleService, game),
                new AvcTransformer(http, moduleService, ghApi, game),
                new LocalizationsTransformer(http, moduleService, game),
                new VersionEditTransformer(),
                new ForcedVTransformer(),
                new EpochTransformer(),
                // This is the "default" VersionedOverrideTransformer for compatibility with overrides that don't
                // specify a before or after property.
                new VersionedOverrideTransformer(before: new string?[] { null },
                                                 after:  new string?[] { null }),
                new DownloadAttributeTransformer(http, fileService),
                new InstallSizeTransformer(http, moduleService, game),
                new StagingLinksTransformer(),
                new GeneratedByTransformer(),
                new OptimusPrimeTransformer(),
                new StripNetkanMetadataTransformer(),
                new PropertySortTransformer()
            });
        }

        public IEnumerable<Metadata> Transform(Metadata metadata, TransformOptions opts)
        {
            var modules = RunTransformers(metadata, opts);
            foreach (var meta in modules)
            {
                _validator.Validate(meta);
            }
            return modules;
        }

        private Metadata[] RunTransformers(Metadata         metadata,
                                           TransformOptions opts)
            => _transformers.Aggregate(new Metadata[] { metadata },
                                       (modules, tr) => modules.SelectMany(meta => tr.Transform(meta, opts))
                                                               .ToArray());

        private static List<ITransformer> InjectVersionedOverrideTransformers(List<ITransformer> transformers)
        {
            var result = new List<ITransformer>();

            for (var i = 0; i < transformers.Count; i++)
            {
                var before = new List<string>();
                var after = new List<string>();

                before.Add(transformers[i].Name);

                if (i - 1 >= 0)
                {
                    after.Add(transformers[i - 1].Name);
                }

                result.Add(new VersionedOverrideTransformer(before, after));
                result.Add(transformers[i]);
            }

            if (result.Count != 0)
            {
                if (result.First() is VersionedOverrideTransformer firstVersionedOverride)
                {
                    firstVersionedOverride.AddBefore("$all");
                    firstVersionedOverride.AddAfter("$none");
                }

                result.Add(new VersionedOverrideTransformer(
                    new[] { "$none" },
                    new[] { result.Last().Name, "$all" }
                ));
            }

            return result;
        }
    }
}
