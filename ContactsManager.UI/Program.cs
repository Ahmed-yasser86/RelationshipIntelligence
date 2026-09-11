using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using Serilog;
using ServiceContracts;
using Servicess;
using ContactsManager.Middleware;
using ContactsManager.Startup;
var builder = WebApplication.CreateBuilder(args);



builder.Host.UseSerilog((HostBuilderContext context, IServiceProvider services, LoggerConfiguration loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration)
       .ReadFrom.Services(services);

});



builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();



if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseExecptionHandlingMiddleware();
}
app.UseHttpLogging();
app.UseRouting();
app.MapControllers();

app.MapControllerRoute(
    name: "MyAreas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Trade}/{action=Index}/{id?}");

app.UseStaticFiles();

app.Run();

public partial class Program { }