using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.AspNetCore.Identity;
using Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using AdminApi.Interfaces;
using AdminApi.Services;
namespace AdminApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            //Serilog
            Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Logger(lc => lc
                .WriteTo.File(
                    path: $"logs/system_log-{DateTime.Now:dd-MM-yy}.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
                )
                .Filter.ByExcluding(e => e.Properties.ContainsKey("AuditLog")))
            .WriteTo.Logger(lc => lc
                .WriteTo.File(
                    path: $"logs/audit_log-{DateTime.Now:dd-MM-yy}.log",
                    rollingInterval: RollingInterval.Day
                )
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("AuditLog")))
            .CreateLogger();
            builder.Host.UseSerilog();


            builder.Services.AddControllers();
            object value = builder.Services.AddControllers();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                // options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                options.JsonSerializerOptions.ReferenceHandler = null;
            });
            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opt =>
            {
                opt.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Bearer Token",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                opt.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                            Name = "Bearer",
                            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                                Id = "Bearer",
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });
            });

            //DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddAutoMapper(typeof(MappingProfile));

            builder.Services.AddScoped<IJwtServices, JwtServices>();
            builder.Services.AddScoped<IEmailServices, EmailServices>();
            builder.Services.AddScoped<UserServices>();
            builder.Services.AddScoped<IImageServices, ImageServices>();
            builder.Services.AddScoped<IAuditLogServices, AuditLogService>();
            builder.Services.AddScoped<IBrandServices, BrandServices>();
            builder.Services.AddScoped<IColorServices, ColorServices>();
            builder.Services.AddScoped<INameTagServices, NameTagServices>();
            builder.Services.AddScoped<ISizeServices, SizeServices>();

            builder.Services.AddIdentityCore<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
            }).AddRoles<IdentityRole>()
            .AddRoleManager<RoleManager<IdentityRole>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager<SignInManager<User>>()
            .AddUserManager<UserManager<User>>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),
                    ValidIssuer = builder.Configuration["JWT:Issuer"],
                    ValidateIssuer = true,
                    ValidateAudience = false
                };
            });



            builder.Services.AddCors();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("OnlyAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
