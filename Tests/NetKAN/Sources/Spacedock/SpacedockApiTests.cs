using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

using CKAN;
using CKAN.NetKAN.Services;
using CKAN.NetKAN.Sources.Spacedock;

using Tests.Data;

namespace Tests.NetKAN.Sources.Spacedock
{
    [TestFixture]
    [Category("FlakyNetwork"),
     Category("Online")]
    public sealed class SpacedockApiTests
    {
        private string?       _cachePath;
        private NetFileCache? _cache;

        [SetUp]
        public void TestFixtureSetup()
        {
            _cachePath = TestData.NewTempDir();
            var path = Path.Combine(_cachePath, Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(path);
            _cache = new NetFileCache(path);
        }

        [TearDown]
        public void TestFixtureTearDown()
        {
            _cache?.Dispose();
            _cache = null;
            if (_cachePath != null)
            {
                Directory.Delete(_cachePath, recursive: true);
            }
        }

        [Test]
        public void GetsModCorrectly()
        {
            // Arrange
            var sut = new SpacedockApi(new CachingHttpService(_cache!));

            // Act
            var result = sut.GetMod(20); // PlaneMode

            // Assert
            var latestVersion = result?.All().FirstOrDefault();

            Assert.That(result?.id, Is.EqualTo(20));
            Assert.That(result?.author, Is.Not.Null);
            Assert.That(result?.background, Is.Not.Null);
            Assert.That(result?.license, Is.Not.Null);
            Assert.That(result?.name, Is.Not.Null);
            Assert.That(result?.short_description, Is.Not.Null);
            Assert.That(result?.source_code, Is.Not.Null);
            Assert.That(result?.website, Is.Not.Null);
            Assert.That(result?.versions?.Length, Is.GreaterThan(0));
            Assert.That(latestVersion?.changelog, Is.Not.Null);
            Assert.That(latestVersion?.download_path, Is.Not.Null);
            Assert.That(latestVersion?.friendly_version, Is.Not.Null);
            Assert.That(latestVersion?.KSP_version, Is.Not.Null);
        }

        [Test]
        public void ThrowsWhenModMissing()
        {
            // Arrange
            var sut = new SpacedockApi(new CachingHttpService(_cache!));

            // Act
            TestDelegate act = () => sut.GetMod(-1);

            // Assert
            Assert.That(act, Throws.Exception.InstanceOf<Kraken>());
        }
    }
}
