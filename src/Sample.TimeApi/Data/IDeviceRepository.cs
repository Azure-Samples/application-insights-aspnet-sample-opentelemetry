using System;
using System.Threading.Tasks;

namespace Sample.TimeApi.Data
{
    public interface IDeviceRepository
    {
        Task<DateTime> GetTimeFromSqlAsync();
    }
}
