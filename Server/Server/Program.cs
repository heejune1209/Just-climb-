using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Config;
using Server.Database;
using Server.Services;
using Server.Utils;

namespace Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // AWS 환경 설정 파일 로드
            if (builder.Environment.IsEnvironment("AWS"))
            {
                builder.Configuration.AddJsonFile("appsettings.AWS.json", optional: false, reloadOnChange: true);
                
                // 환경 변수도 추가
                builder.Configuration.AddEnvironmentVariables();
            }

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            
            // 로깅 설정 강화
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            // HTTPS 리다이렉션 설정 (개발 환경에서는 비활성화)
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                    options.HttpsPort = 443;
                });
            }

            // HSTS (HTTP Strict Transport Security) 설정 - 프로덕션용
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(365);
                    options.ExcludedHosts.Clear();
                });
            }

            // CORS 설정 추가 (Unity 클라이언트 접근 허용)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowUnity", policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        // 개발 환경: 모든 Origin 허용
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                    }
                    else
                    {
                        // 프로덕션 환경: 특정 도메인만 허용
                        policy.WithOrigins("https://justclimb.com", "https://*.justclimb.com")
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    }
                });
            });

            // 데이터베이스 컨텍스트 설정 (PostgreSQL/SQL Server 지원)
            builder.Services.AddDbContext<JustClimbDbContext>(opts =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (builder.Environment.IsEnvironment("AWS") || 
                    connectionString.Contains("Host=") || 
                    connectionString.Contains("Server=") && connectionString.Contains("Port="))
                {
                    // PostgreSQL 사용
                    opts.UseNpgsql(connectionString);
                }
                else
                {
                    // SQL Server 사용 (기본값)
                    opts.UseSqlServer(connectionString);
                }
            });
            builder.Services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString"));

            builder.Services.AddScoped<IUserStateService, UserStateService>();
            builder.Services.AddScoped<IRankingService, RankingService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAchievementService, AchievementService>();
            builder.Services.AddSingleton<ConflictResolver>();
            
            // HttpClient 등록 (Steam Web API 호출용)
            builder.Services.AddHttpClient();
            
            // JWT 인증 설정
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });
            
            builder.Services.AddAuthorization();


            // JSON 설정 파일 추가
            builder.Configuration
                   .SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("Config/RedisSyncConfig.json", optional: false, reloadOnChange: true);

            // 바인딩
            builder.Services.Configure<RedisSyncConfig>(
                builder.Configuration.GetSection("RedisSyncConfig"));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            else
            {
                // 프로덕션에서만 HSTS 활성화
                app.UseHsts();
                // HTTPS 리다이렉션
                app.UseHttpsRedirection();
            }

            // CORS 미들웨어 추가
            app.UseCors("AllowUnity");

            // 인증 및 인가 미들웨어 추가
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Health Check 엔드포인트
            app.MapGet("/api/v1/health", () => new { 
                status = "Healthy", 
                timestamp = DateTime.UtcNow,
                environment = app.Environment.EnvironmentName
            });

            // 데이터베이스 마이그레이션 및 업적 시드
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<JustClimbDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    await context.Database.MigrateAsync();
                    logger.LogInformation("데이터베이스 마이그레이션 완료");
                    
                    await AchievementSeeder.SeedAsync(context);
                    logger.LogInformation("업적 시드 완료");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "데이터베이스 초기화 중 오류 발생");
                    throw;
                }
            }

            await app.RunAsync();
        }
    }
}
