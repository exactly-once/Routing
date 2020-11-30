using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    class StoppableRawEndpoint : IStoppableRawEndpoint
    {
        TransportInfrastructure transportInfrastructure;
        SettingsHolder settings;

        public StoppableRawEndpoint(TransportInfrastructure transportInfrastructure, SettingsHolder settings)
        {
            this.transportInfrastructure = transportInfrastructure;
            this.settings = settings;
        }

        public async Task Stop()
        {
            Log.Info("Initiating shutdown.");

            try
            {
                await transportInfrastructure.Stop().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                //Ignore when shutting down
            }
            finally
            {
                settings.Clear();
                Log.Info("Shutdown complete.");
            }
        }

        static ILog Log = LogManager.GetLogger<StoppableRawEndpoint>();
    }
}