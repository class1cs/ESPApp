using System.Threading;
using System.Threading.Tasks;
using ESPApp.Infrastructure.Models;

namespace ESPApp.Infrastructure.Interfaces
{
    public interface IEspToolConfigurator
    {
        EsptoolSource? Current { get; }

        void Use(EsptoolSource source);

        Task<EsptoolProbe> ProbeAsync(EsptoolSource source, CancellationToken cancellationToken = default);
    }

    public interface ISettingsService
    {
        AppSettings Load();

        bool Save(AppSettings settings);
    }
}
