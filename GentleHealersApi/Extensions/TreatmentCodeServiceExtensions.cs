using GentleHealersApi.Services;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GentleHealersApi.Extensions;

public static class TreatmentCodeServiceExtensions
{
    public static IServiceCollection AddTreatmentCodeImport(this IServiceCollection services)
    {
        services.TryAddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ITreatmentCodeReader, TreatmentCodeFileReader>();
        services.AddScoped<ITreatmentCodeRepository, TreatmentCodeRepository>();
        services.AddScoped<ITreatmentCodeImportService, TreatmentCodeImportService>();
        return services;
    }
}
