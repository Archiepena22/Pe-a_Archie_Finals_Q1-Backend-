var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

const string corsPolicyName = "AllowViteDevServer";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.MapControllers();

app.Run();
