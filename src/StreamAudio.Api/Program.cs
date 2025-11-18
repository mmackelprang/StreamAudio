var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "StreamAudio API",
        Version = "v1",
        Description = "REST API for StreamAudio - Audio streaming and management system",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "StreamAudio",
            Url = new Uri("https://github.com/mmackelprang/StreamAudio")
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Initialize configuration
var config = StreamAudio.Core.Configuration.ConfigurationManager.Instance;
config.Logger.Information("StreamAudio API starting...");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StreamAudio API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.MapControllers();

config.Logger.Information("StreamAudio API started. Swagger UI available at http://localhost:5000");
app.Run();

