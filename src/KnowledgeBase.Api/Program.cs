using KnowledgeBase.Api.Services;
using KnowledgeBase.Core.Interfaces;
using KnowledgeBase.Core.Services;
using KnowledgeBase.Infrastructure.Data;
using KnowledgeBase.Infrastructure.Repositories;
using KnowledgeBase.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var useLocalJwt =
    builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Authentication:UseLocalJwt");


builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<KnowledgeBaseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IArticleSearchService, EfArticleSearchService>();
builder.Services.AddScoped<IArticleService, ArticleService>();

if (useLocalJwt)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
    builder.Services.AddRequiredScopeAuthorization();
}
else
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(
        builder.Configuration);
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
