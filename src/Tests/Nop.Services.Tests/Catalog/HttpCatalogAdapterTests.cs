using System;
using NUnit.Framework;
using Rhino.Mocks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Logging;

namespace Nop.Services.Tests.Catalog
{
    [TestFixture]
    public class HttpCatalogAdapterTests
    {
        private ICategoryService _mockCategoryService;
        private IProductService _mockProductService;
        private ILogger _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockCategoryService = MockRepository.GenerateMock<ICategoryService>();
            _mockProductService = MockRepository.GenerateMock<IProductService>();
            _mockLogger = MockRepository.GenerateMock<ILogger>();
        }

        [Test]
        public void GetCategoryById_FeatureFlagDisabled_UsesFallback()
        {
            // Arrange
            Environment.SetEnvironmentVariable("USE_DOTNET8_API", "false");
            var expectedCategory = new Category { Id = 1, Name = "Test Category" };
            _mockCategoryService.Stub(x => x.GetCategoryById(1)).Return(expectedCategory);

            using var adapter = new HttpCatalogAdapter(_mockCategoryService, _mockProductService, _mockLogger);

            // Act
            var result = adapter.GetCategoryById(1);

            // Assert
            Assert.AreEqual(expectedCategory, result);
            _mockCategoryService.AssertWasCalled(x => x.GetCategoryById(1));
        }

        [Test]
        public void GetProductById_FeatureFlagDisabled_UsesFallback()
        {
            // Arrange
            Environment.SetEnvironmentVariable("USE_DOTNET8_API", "false");
            var expectedProduct = new Product { Id = 1, Name = "Test Product" };
            _mockProductService.Stub(x => x.GetProductById(1)).Return(expectedProduct);

            using var adapter = new HttpCatalogAdapter(_mockCategoryService, _mockProductService, _mockLogger);

            // Act
            var result = adapter.GetProductById(1);

            // Assert
            Assert.AreEqual(expectedProduct, result);
            _mockProductService.AssertWasCalled(x => x.GetProductById(1));
        }

        [Test]
        public void GetCategoryById_ApiCallFails_UsesFallback()
        {
            // Arrange
            Environment.SetEnvironmentVariable("USE_DOTNET8_API", "true");
            var expectedCategory = new Category { Id = 1, Name = "Fallback Category" };
            _mockCategoryService.Stub(x => x.GetCategoryById(1)).Return(expectedCategory);

            using var adapter = new HttpCatalogAdapter(_mockCategoryService, _mockProductService, _mockLogger);

            // Act - API will fail since no server is running
            var result = adapter.GetCategoryById(1);

            // Assert
            Assert.AreEqual(expectedCategory, result);
            _mockCategoryService.AssertWasCalled(x => x.GetCategoryById(1));
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("USE_DOTNET8_API", null);
        }
    }
}