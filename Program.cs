using UserManagementAPI.Interfaces;
using UserManagementAPI.Services;
using UserManagementAPI.Middleware;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register services
    builder.Services.AddSingleton<IUserService, UserService>();

    var app = builder.Build();

    // Register middleware in correct order (outermost to innermost):
    // 1. Error Handling - catches all exceptions
    // 2. Authentication - validates tokens
    // 3. Logging - logs all requests/responses
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseMiddleware<TokenValidationMiddleware>();
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application startup failed: {ex.Message}");
    throw;
}
