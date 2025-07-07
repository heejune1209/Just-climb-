using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Server.Config;
using Server.Database;
using Server.Services;
using Server.Utils;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // CORS 설정 추가 (Unity 클라이언트 접근 허용)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowUnity", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddDbContext<JustClimbDbContext>(opts =>
                opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddStackExchangeRedisCache(opts =>
                opts.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString"));

            builder.Services.AddScoped<IUserStateService, UserStateService>();
            builder.Services.AddScoped<IRankingService, RankingService>();
            builder.Services.AddScoped<IUserService, UserService>();
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

            app.UseHttpsRedirection();

            // CORS 미들웨어 추가
            app.UseCors("AllowUnity");

            // 인증 및 인가 미들웨어 추가
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
