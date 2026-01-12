using Vitarish.Entities.Server.Services;

namespace Vitarish.Main.Infrastructure.Services
{
    public class EntityDataProvider(IRequestContext requestContext) : IEntityUserService, IEntityTimeService
    {
        private DateTimeOffset? now;
        private string? userId;
        public string? UserId => userId ?? requestContext.UserId;

        public DateTimeOffset Now => now ?? DateTimeOffset.Now;

        public void Set(string? userId) => this.userId = userId;
        public void Set(DateTimeOffset? now) => this.now = now;
    }
}
