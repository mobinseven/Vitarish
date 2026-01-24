using Vitarish.Entities.Server.Services;

namespace Vitarish.Main.Infrastructure.Services
{
    public class EntityDataProvider(IRequestContext requestContext) : IEntityUserService, IEntityTimeService
    {
        private DateTimeOffset? now;

        public string? UserId { get => field ?? requestContext.UserId; private set; }
        public DateTimeOffset Now => now ?? DateTimeOffset.Now;
        public void Set(string? userId) => UserId = userId;
        public void Set(DateTimeOffset? now) => this.now = now;
    }
}
