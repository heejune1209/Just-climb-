using Microsoft.EntityFrameworkCore;
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
            builder.Services.AddSingleton<ConflictResolver>();


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

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
