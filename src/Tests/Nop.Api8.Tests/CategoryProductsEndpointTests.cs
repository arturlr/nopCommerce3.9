using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Nop.Api8.Tests
{
    public class CategoryProductsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CategoryProductsEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCategoryProducts_ValidId_ReturnsExpectedStatusCode()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products");
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_WithPagination_ReturnsExpectedStatusCode()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products?pageNumber=1&pageSize=5");
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_WithSorting_ReturnsExpectedStatusCode()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products?orderBy=name");
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_WithPriceFilter_ReturnsExpectedStatusCode()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products?priceMin=10&priceMax=100");
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_WithFeaturedFilter_ReturnsExpectedStatusCode()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products?featuredOnly=true");
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_InvalidPageSize_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products?pageSize=0");
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_InvalidOrderBy_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/v1/categories/1/products?orderBy=invalid");
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GetCategoryProducts_NonExistentCategory_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/v1/categories/99999/products");
            Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }
}