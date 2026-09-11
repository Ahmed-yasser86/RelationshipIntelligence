using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using Serilog;
using ServiceContracts;
using Servicess;
using ContactsManager.ContactComponent.Filters;
using Repositories;
using ContactsManger.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
namespace ContactsManager.Startup
{
    public static class ConfigureServicesExstintion 
    {

        public static IServiceCollection ConfigureServices(this IServiceCollection Services, IConfiguration Configuration)
        {

         

           Services.AddControllersWithViews();

            // ========== REPOSITORY LAYER ==========
           Services.AddScoped<CountryRepositryContract, CountryRepository>();
           Services.AddScoped<PersonRepositryContract, PersonRepository>();

            // ========== SERVICE LAYER ==========
            // Register country services separately
          Services.AddScoped<ICountryGetterService, CountryGetterService>();
          Services.AddScoped<ICountryAdderService, CountryAdderService>();
            // Register each service separately
            Services.AddScoped<IPersonGetterService, PersonGetterService>();
           Services.AddScoped<IPersonAdderService, PersonAdderService>();
           Services.AddScoped<IPersonUpdaterService, PersonUpdaterService>();
           Services.AddScoped<IPersonDeleterService, PersonDeleterService>();
           Services.AddScoped<IPersonSearcherService, PersonSearcherService>();
           Services.AddScoped<IPersonSorterService, PersonSorterService>();
          
            Services.AddIdentity<ApplicationUser,ApplicationRole>().
                AddEntityFrameworkStores<AppDBContext>().
                AddDefaultTokenProviders().
                AddUserStore<UserStore<ApplicationUser,ApplicationRole, AppDBContext,Guid>>().
                 AddRoleStore<RoleStore<ApplicationRole,AppDBContext,Guid>>();

           Services.AddScoped<PerformanceLoggingFilter>();
            // For Stocks DbContext


            // ========== DATABASE CONTEXTS ==========
           Services.AddDbContext<AppDBContext>(
                Options => Options.UseSqlServer(Configuration.GetConnectionString("ContactDb"))
            );



            // ========== CONFIGURATION ==========
         

            // ========== HTTP LOGGING ==========
           Services.AddHttpLogging(options =>
            {
                options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPropertiesAndHeaders | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponsePropertiesAndHeaders;
            });

            return Services;
        }

    }
}
