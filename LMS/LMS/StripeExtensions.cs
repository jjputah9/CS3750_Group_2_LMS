
using Stripe;
public static class StripeExtensions
{
    public static IServiceCollection AddStripeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration.GetValue<string>("Stripe:SecretKey");

        return services
            .AddScoped<CustomerService>()
            .AddScoped<ChargeService>()
            .AddScoped<TokenService>()
            .AddScoped<PaymentIntentService>();
    }
}