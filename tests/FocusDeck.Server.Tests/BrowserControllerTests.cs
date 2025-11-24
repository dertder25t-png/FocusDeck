using System;
using System.Collections.Generic;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Controllers.v1;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Browser;
using Xunit;

namespace FocusDeck.Server.Tests.Controllers
{
    public class BrowserControllerTests
    {
        [Fact]
        public void CanInstantiateBrowserController()
        {
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: "BrowserControllerTest")
                .Options;

            using (var context = new AutomationDbContext(options))
            {
                var service = new BrowserContextService(context);
                var controller = new BrowserController(service, new StubCurrentTenant());
                Assert.NotNull(controller);
            }
        }

        private class StubCurrentTenant : FocusDeck.SharedKernel.Tenancy.ICurrentTenant
        {
            public Guid? TenantId => Guid.NewGuid();
            public bool HasTenant => true;
            public void SetTenant(Guid tenantId) {}
            public IDisposable Change(Guid? tenantId) => new StubDisposable();
        }

        private class StubDisposable : IDisposable { public void Dispose() {} }

        [Fact]
        public void CapturedItem_HasCorrectProperties()
        {
            var item = new CapturedItem
            {
                Id = Guid.NewGuid(),
                Url = "https://example.com",
                Title = "Example",
                Kind = CapturedItemType.Page,
                CapturedAt = DateTime.UtcNow
            };

            Assert.Equal("https://example.com", item.Url);
            Assert.Equal("Example", item.Title);
            Assert.Equal(CapturedItemType.Page, item.Kind);
        }

         [Fact]
        public void Project_HasCorrectProperties()
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = "My Project",
                RepoSlug = "owner/repo",
                CreatedAt = DateTime.UtcNow
            };

            Assert.Equal("My Project", project.Title);
            Assert.Equal("owner/repo", project.RepoSlug);
        }
    }
}
