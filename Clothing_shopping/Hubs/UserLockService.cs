using Clothing_shopping.models;
using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;


/*
 đang fix
 */

namespace Clothing_shopping.Hubs
{
    public class UserLockService
    {
        private readonly IHubContext<AppHub> _hub;
        private SqlTableDependency<User> _tableDependency;
        private readonly string connectionString;

        public UserLockService(IHubContext<AppHub> hub, IConfiguration configuration)
        {
            _hub = hub;
            connectionString = configuration.GetConnectionString("clothingDB");
        }

        public void Start()
        {
            _tableDependency = new SqlTableDependency<User>(connectionString);
            _tableDependency.OnChanged += TableDependency_OnChanged;
            _tableDependency.Start();
        }

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<User> e)
        {
            if (e.ChangeType == ChangeType.Update && e.Entity.IsLocked)
            {
                _hub.Clients.User(e.Entity.UserId.ToString()).SendAsync("ForceLogout");
            }
        }

        public void Stop()
        {
            _tableDependency.Stop();
        }
    }
}
