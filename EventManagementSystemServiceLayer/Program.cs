using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementServiceDAL.Repositories.AttendeeRepo;
using EventManagementServiceDAL.Repositories.CommonRepo;
using EventManagementServiceDAL.Repositories.EventManagerRepo;
using EventManagementSystemServiceLayer.DTOs.Administrator;
using EventManagementSystemServiceLayer.Middleware;
using EventManagementSystemServiceLayer.Services;
using EventManagementSystemServiceLayer.Services.Administrator;
using EventManagementSystemServiceLayer.Services.Auth;
using EventManagementSystemServiceLayer.Services.EventManager;
using EventManagementSystemServiceLayer.Services.Notifications;
using EventManagementSystemServiceLayer.Validators.Administrator;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EventManagementDb")
    ?? "Data Source=localhost;Initial Catalog=EventManagementDB;Integrated Security=true;TrustServerCertificate=true";

builder.Services.AddDbContext<EventManagementDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IReportingRepository, ReportingRepository>();
builder.Services.AddScoped<IPublicEventRepository, PublicEventRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IRegistrationRequestRepository, RegistrationRequestRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Brownfield Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IEventApprovalRepository, EventApprovalRepository>();
builder.Services.AddScoped<IEventSeriesRepository, EventSeriesRepository>();
builder.Services.AddScoped<ICapacityAlertRepository, CapacityAlertRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();

builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();
builder.Services.AddScoped<IAuditMonitoringService, AuditMonitoringService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<IPublicEventService, PublicEventService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<IAttendeeHistoryService, AttendeeHistoryService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventManagementService, EventManagementService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IRegistrationRequestService, RegistrationRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.BusinessManagement.IBusinessManagementService, EventManagementSystemServiceLayer.Services.BusinessManagement.BusinessManagementService>();

// Brownfield Services
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.ICategoryService, EventManagementSystemServiceLayer.Services.Brownfield.CategoryService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.IVenueService, EventManagementSystemServiceLayer.Services.Brownfield.VenueService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.IEventApprovalWorkflowService, EventManagementSystemServiceLayer.Services.Brownfield.EventApprovalWorkflowService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.IRecurringEventService, EventManagementSystemServiceLayer.Services.Brownfield.RecurringEventService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.ICapacityAlertService, EventManagementSystemServiceLayer.Services.Brownfield.CapacityAlertService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.ICalendarExportService, EventManagementSystemServiceLayer.Services.Brownfield.CalendarExportService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.IFeedbackService, EventManagementSystemServiceLayer.Services.Brownfield.FeedbackService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.IRecommendationService, EventManagementSystemServiceLayer.Services.Brownfield.RecommendationService>();
builder.Services.AddScoped<EventManagementSystemServiceLayer.Services.Brownfield.IAdvancedAnalyticsService, EventManagementSystemServiceLayer.Services.Brownfield.AdvancedAnalyticsService>();

builder.Services.AddScoped<IValidator<UserCreateDto>, UserCreateDtoValidator>();
builder.Services.AddScoped<IValidator<RoleUpdateDto>, RoleUpdateDtoValidator>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecretKeyHere1234567890123456";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EventManagementSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EventManagementSystemUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("EventManager", policy => policy.RequireRole("EventManager", "Administrator"));
    options.AddPolicy("BusinessManagement", policy => policy.RequireRole("BusinessManagement", "Administrator"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Management System API",
        Version = "v1",
        Description = "RESTful API for Event Management System",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@eventmanagementsystem.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Event Management System API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseCors("DevelopmentCorsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
