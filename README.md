# Harpoon

[![Build status](https://ci.appveyor.com/api/projects/status/tb4kv08i2g0y0d03/branch/master?svg=true)](https://ci.appveyor.com/project/Poltuu/harpoon/branch/master) [![codecov](https://codecov.io/gh/Poltuu/Harpoon/branch/master/graph/badge.svg)](https://codecov.io/gh/Poltuu/Harpoon)

[![logo](https://github.com/Poltuu/Harpoon/blob/master/icon.png?raw=true)](logo)

Harpoon provides support for sending your own WebHooks. The general philosophy is to let you design each step your way, while providing strong default solutions. The modularity lets you split the process into n microservices if you deem necessary.

## General architecture and classes

Harpoon strongly separates each necessary component to register and retrieve webhooks, to start a notification process, or to actually send the webhooks.

[![schema](https://github.com/Poltuu/Harpoon/blob/master/docs/schema.png?raw=true)](schema)

To start a notification procees, you need to call the `NotifyAsync` method on the `IWebHookService` with the appropriate `IWebHookNotification`.

`IWebHookNotification` simply exposes a `TriggerId` and a payload (`IReadOnlyDictionary<string, object>`).

Depending on your configuration, the `IWebHookNotification` can be passed synchronously (on the current thread), via a `QueuedHostedService` (defers the local treatment of the notification the a background service) or via a messaging service (lets you potentially treat the notification on another application) to the next handler, `IQueuedProcessor<IWebHookNotification>`.

`IQueuedProcessor<IWebHookNotification>` asks the `IWebHookStore` to find the matching registrations, and passes the generated `IWebHookWorkItem` along to the registered `IWebHookSender`. To use EF Core to register your webhooks, use the package `Harpoon.Registrations.EFStorage` (see details below). `IWebHookWorkItem` contains the notification and a matching registration. One `IWebHookNotification` may therefore generate a lot of `IWebHookWorkItem`.

Once again, the treatment of the `IWebHookWorkItem` can be done synchronously (on the current thread), via a `QueuedHostedService` (to defer the local treatment) or via a messaging service to the next handler `IQueuedProcessor<IWebHookWorkItem>` (to potentially treat the IWebHookWorkItem on another application).

Finally, the `IWebHookWorkItem` are sent via the `IQueuedProcessor<IWebHookWorkItem>`. The general retry policy and failures policy should be configured using `Polly` during the dependency injection registration, as the `IHttpClientBuilder` is exposed; there is no default for this.

### How to start a notification process

MyClass.cs

```c#
using Harpoon;
using System.Threading;
using System.Threading.Tasks;

public class MyClass
{
    private readonly IWebHookService _webHookService;

    //...

    public async Task MyMethodAsync(/* */)
    {
         //this will be serialized and send to the consumers
        var notification = new WebHookNotification
        {
            TriggerId = "SomObject.Created",
            Payload = new Dictionary<string, object>
            {
                ["someValue"] = 23,
                ["someOtherValue"] = "value"
            }
        };

         //what this precisely does depends on you configuration
        await _webHookService.NotifyAsync();
    }
}
```

### How to treat everything locally and synchronously (using a mock storage)

Startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h.UseAllSynchronousDefaults()); //everything is done locally and synchronously
    services.AddSingleton(new Mock<IWebHookStore>().Object); //mock storage, see below for EF storage
}
```

### How to treat everything locally and in the background using background services (using a mock storage)

Startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h.UseAllLocalDefaults()); //everything is done locally via background services
    services.AddSingleton(new Mock<IWebHookStore>().Object); //mock storage, see below for EF storage
}
```

### How to notify another application App2, and let this one treat the notification synchronously, using a messaging service

You need to include via nuget `Harpoon.MassTransit` in App1 and App2 for this to work.

App1.Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h.SendNotificationsUsingMassTransit());

    services.AddMassTransit(p => /* your configuration here. */);
}
```

App2.Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(c => c
        .UseDefaultNotificationProcessor()
        .ProcessWebHookWorkItemSynchronously()
        .UseDefaultWebHookWorkItemProcessor()
    );

    //register webhooks storage here
    services.AddSingleton(new Mock<IWebHookStore>().Object);

    //configuration example uses RabbitMq, but other bus factories are usable
    services.AddMassTransit(p => Bus.Factory.CreateUsingRabbitMq(cfg =>
    {
        var host = cfg.Host(new Uri("rabbitmq://localhost:5672"), hostConfigurator =>
        {
            hostConfigurator.Username("guest");
            hostConfigurator.Password("guest");
        });

        cfg.ConfigureNotificationsConsumer(p, "My_queue_name");
    }), x => x.ReceiveNotificationsUsingMassTransit());
}
```

### How to treat the notification synchrnously and locally, but let another application App2 actually do the http calls, using a messaging service

You need to include via nuget `Harpoon.MassTransit` in App1 and App2 for this to work.

App1.Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h
        .ProcessNotificationsSynchronously()
        .UseDefaultNotificationProcessor()
        .SendWebHookWorkItemsUsingMassTransit()
    );

    //register webhooks storage here
    services.AddSingleton(new Mock<IWebHookStore>().Object);

    services.AddMassTransit(p => /* your configuration here. */);
}
```

App2.Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(c => c.UseDefaultWebHookWorkItemProcessor());

    //configuration example uses RabbitMq, but other bus factories are usable
    services.AddMassTransit(p => Bus.Factory.CreateUsingRabbitMq(cfg =>
    {
        var host = cfg.Host(new Uri("rabbitmq://localhost:5672"), hostConfigurator =>
        {
            hostConfigurator.Username("guest");
            hostConfigurator.Password("guest");
        });

        cfg.ConfigureWebHookWorkItemsConsumer(p, "My_queue_name");
    }), x => x.ReceiveWebHookWorkItemsUsingMassTransit());
}
```

### Ho to use Ef for webhooks storage

You need to include `Harpoon.Registrations.EFStorage` via nuget.

Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddEntityFrameworkSqlServer().AddDbContext<MyContext>(); // register EF and your context as you normally would

    services.AddHarpoon(h =>
    {
        h.RegisterWebHooksUsingEfStorage<MyContext>(); // MyContext needs to implement IRegistrationsContext
        h.UseDefaultDataProtection(p => { }, o => { }); // the default data protection uses System.DataProtection
    });
}
```

Don't forget to generate a migration if you are using EF Core Migrations.

MyContext.cs

```c#
public class TestContext : DbContext, IRegistrationsContext
{
    public DbSet<WebHook> WebHooks { get; set; }
    IQueryable<WebHook> IRegistrationsContext.WebHooks => WebHooks;

    public DbSet<WebHookFilter> WebHookFilters { get; set; } //optional

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddHarpoonDefaultMappings(); //optional. this lets you apply default mappings.
    }
}
```

### How to use default REST controllers

To use default mvc controllers to provide default REST operations on your webhooks, simply add the nuget package `Harpoon.Controllers`.
You also need to register a `IWebHookValidator` in your DI; you may use `.UseDefaultValidator()` or provide your own.

Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddEntityFrameworkSqlServer().AddDbContext<MyContext>(); // register EF and your context

    services.AddHarpoon(h =>
    {
        h.RegisterWebHooksUsingEfStorage<MyContext>(); //MyContext needs to implement IRegistrationsContext
        h.UseDefaultDataProtection(p => { }, o => { }); //the default data protection uses System.DataProtection
        h.UseDefaultValidator(); //the default validator is necessary for Write operations. This is necessary for WebHookRegistrationStore but not for WebHookStore
    });
}
```

### How to setup your retry policy on your sender

To prefered way to setup you retry policy is to use [Polly](https://github.com/App-vNext/Polly), by adding `Microsoft.Extensions.Http.Polly`. [The general help is here.](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-2.1).

Startup.cs

```c#
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddEntityFrameworkSqlServer().AddDbContext<MyContext>(); // register EF and your context

    services.AddHarpoon(h =>
    {
        //most methods let you configure via a Action<IHttpClientBuilder>
        h.UseAllSynchronousDefaults(b => 
        {
            b.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(6, index => TimeSpan.FromMinutes(index * index * index * 10)))
                .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
        });
    });
}
```