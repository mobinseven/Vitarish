using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Main.Infrastructure.Services
{
    public class EntityDataProvider(IRequestContext requestContext) : IEntityUserService, IEntityTimeService
    {
        private DateTime? now;

        public string? UserId { get => field ?? requestContext.UserId; private set; }
        public DateTime Now => now ?? DateTime.UtcNow;
        public void Set(string? userId) => UserId = userId;
        public void Set(DateTime? now) => this.now = now;
    }
}
