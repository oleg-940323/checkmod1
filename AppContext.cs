using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

/// <summary>
/// Класс запуска приложения (НЕ ТРОГАТЬ!)
/// </summary>
namespace checkmod
{
    class AppContext : DbContext
    {
        public DbSet<ParamConf> Modules { get; set; }

        public AppContext() : base("DefaultConnection")
        { }

    }
}
