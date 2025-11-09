using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    [DbContext(typeof(FocusDeck.Persistence.AutomationDbContext))]
    public partial class AutomationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "9.0.10");
        }
    }
}
