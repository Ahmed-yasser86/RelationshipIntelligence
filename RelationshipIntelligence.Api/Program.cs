using ContactsManger.Core.Domain.IdentityEntities;
using ContactsManger.Core.ServiceContracts;
using ContactsManger.Core.Services;
using Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using RelationshipIntelligence.AI;
using Repositories;
using RepositryContracts;
using ServiceContracts;
using Servicess;
using System;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new ProducesAttribute("application/json"));
    options.Filters.Add(new ConsumesAttribute("application/json"));

    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddTransient<IjwtAuthentication, JwtServices>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ServiceContracts.ICurrentUserService, Servicess.CurrentUserService>();
//builder.Services.AddScoped<IPersonQuickAdderService, PersonQuickAdderService  >();
builder.Services.AddScoped<IPersonSearcherService, PersonSearcherService>();
builder.Services.AddScoped<IPersonQuickAdderService, PersonQuickAdderService>();
builder.Services.AddScoped<CircleRepositryContract, CircleRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<PersonRepositryContract, PersonRepository>();
builder.Services.AddScoped<ICountryAdderService, CountryAdderService>();
builder.Services.AddScoped<ICountryGetterService, CountryGetterService>();
builder.Services.AddScoped<IPersonAdderService, PersonAdderService>();
builder.Services.AddScoped<CountryRepositryContract, CountryRepository>();
builder.Services.AddScoped<PersonRepositryContract, PersonRepository>();
builder.Services.AddScoped<CircleRepositryContract, CircleRepository>();
builder.Services.AddScoped<CountryRepositryContract, CountryRepository>();

builder.Services.AddScoped<ContactItemRoleRepositryContract, ContactItemRoleRepository>();
builder.Services.AddScoped<ConnectionChannelRepositryContract, ConnectionChannelRepository>();
builder.Services.AddScoped<SocialMediaAccountRepositryContract, SocialMediaAccountRepository>();
builder.Services.AddScoped<SystemStatusTagRepositryContract, SystemStatusTagRepository>();
builder.Services.AddScoped<UserDefinedTagsRepositryContract, UserDefinedTagsRepository>(); builder.Services.AddScoped<IPersonGetterService, PersonGetterService>();
builder.Services.AddScoped<SystemStatusTagRepositryContract, SystemStatusTagRepository>();
builder.Services.AddScoped<ISystemTagsGetter, SystemTagsGetterService>();
builder.Services.AddScoped<IPersonAdderService, PersonAdderService>();
builder.Services.AddScoped<IPersonUpdaterService, PersonUpdaterService>();
builder.Services.AddScoped<IPersonGetterService, PersonGetterService>();
builder.Services.AddScoped<IPersonSearcherService, PersonSearcherService>();
builder.Services.AddScoped<IPersonSorterService, PersonSorterService>();
builder.Services.AddScoped<IPersonQuickAdderService, PersonQuickAdderService>();
builder.Services.AddScoped<IPersonDeleterService, PersonDeleterService>();
builder.Services.AddScoped<IRelationshipScoringService, RelationshipScoringService>();
builder.Services.AddScoped<IDemoWorkspaceService, DemoWorkspaceService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IRelationshipMemoryService, RelationshipMemoryService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<RelationshipMemoryRepositoryContract, RelationshipMemoryRepository>();
builder.Services.AddScoped<RelationshipEventRepositoryContract, RelationshipEventRepository>();
builder.Services.AddScoped<IAiProviderSettingsService, AiProviderSettingsService>();
builder.Services.AddScoped<AiProviderSettingsRepositoryContract, AiProviderSettingsRepository>();
builder.Services.AddDataProtection();
builder.Services.AddScoped<KernelFactory>();
builder.Services.AddScoped<RelationshipPlugin>();
var copilotMode = builder.Configuration["Copilot:Mode"];
if (string.Equals(copilotMode, "Stub", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<ICopilotService, StubCopilotService>();
else
    builder.Services.AddScoped<ICopilotService, CopilotService>();
builder.Services.AddScoped<INetworkAnalysisService, NetworkAnalysisService>();
builder.Services.AddScoped<RelationshipStateRepositoryContract, RelationshipStateRepository>();
builder.Services.AddScoped<DigestRepositoryContract, DigestRepository>();
builder.Services.AddScoped<IEmailSender>(sp => new FileEmailSender(
    sp.GetRequiredService<IConfiguration>()["Digest:OutputDirectory"] ?? string.Empty,
    sp.GetRequiredService<ILogger<FileEmailSender>>()));
builder.Services.AddScoped<IDigestService>(sp => new DigestService(
    sp.GetRequiredService<IRelationshipScoringService>(),
    sp.GetRequiredService<PersonRepositryContract>(),
    sp.GetRequiredService<InteractionRepositoryContract>(),
    sp.GetRequiredService<DigestRepositoryContract>(),
    sp.GetRequiredService<RelationshipStateRepositoryContract>(),
    sp.GetRequiredService<ICurrentUserService>(),
    sp.GetRequiredService<IEmailSender>(),
    sp.GetRequiredService<IUnitOfWork>(),
    sp.GetRequiredService<IConfiguration>()["Digest:Secret"] ?? "dev-secret-change-in-production",
    sp.GetRequiredService<ILogger<DigestService>>()));
builder.Services.AddHostedService<RelationshipIntelligence.Api.Workers.RelationshipMaintenanceJob>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<InteractionRepositoryContract, InteractionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICountryAdderService, CountryAdderService>();
builder.Services.AddScoped<ICountryGetterService, CountryGetterService>();

builder.Services.AddScoped<ISystemTagsGetter, SystemTagsGetterService>();

builder.Services.AddScoped<IjwtAuthentication, JwtServices>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<PersonOwnershipFilter>();

builder.Services.AddDbContext<AppDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ContactDb"));
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<AppDBContext>()
    .AddDefaultTokenProviders()
    .AddUserStore<UserStore<ApplicationUser, ApplicationRole, AppDBContext, Guid>>()
    .AddRoleStore<RoleStore<ApplicationRole, AppDBContext, Guid>>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:issuer"],
        ValidAudience = builder.Configuration["Jwt:audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT. Example: \"Bearer eyJhbGci...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Contacts Manager API",
        Version = "v1"
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        var allowedOrigins = builder.Configuration.GetSection("allowOrigins").Get<List<string>>();

        if (allowedOrigins != null && allowedOrigins.Count > 0)
        {
            policyBuilder.WithOrigins(allowedOrigins.ToArray())
                         .AllowAnyHeader()
                         .AllowAnyMethod();
        }
        else
        {
            policyBuilder.AllowAnyOrigin()
                         .AllowAnyHeader()
                         .AllowAnyMethod();
        }
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Contacts Manager API v1");
    });
}

var useHttpsRedirection = builder.Configuration.GetValue<bool>("UseHttpsRedirection");
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();




app.Run();