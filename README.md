# Harpoon

[![Build status](https://ci.appveyor.com/api/projects/status/tb4kv08i2g0y0d03/branch/master?svg=true)](https://ci.appveyor.com/project/Poltuu/harpoon/branch/master) [![codecov](https://codecov.io/gh/Poltuu/Harpoon/branch/master/graph/badge.svg)](https://codecov.io/gh/Poltuu/Harpoon)

[![logo](https://github.com/Poltuu/Harpoon/blob/master/icon.png?raw=true)](logo)

DISCLAIMER : this library is still experimental, and should not be considered production-ready until version 1.0.0.

Harpoon provides support for sending your own WebHooks. The general philosophy is to let you design each step your way, while providing strong default solutions. The modularity lets you split the process into n microservices if you deem necessary.

Webhooks processing and sending is located in the nugets `Harpoon.Common` and `Harpoon.MassTransit`.
Webhooks registrations and exposure is located in the nugets `Harpoon.Registrations`, `Harpoon.Registrations.EFStorage` and `Harpoon.Controllers`

## General architecture and classes

Harpoon strongly separates each necessary component to register and retrieve webhooks, to start a notification process, or to actually send the webhooks.

[![schema](https://github.com/Poltuu/Harpoon/blob/master/docs/schema.png?raw=true)](schema)

To start a notification procees, you need to call the `NotifyAsync` method on the `IWebHookService` with the appropriate `IWebHookNotification`.

`IWebHookNotification` exposes a id for the event `string TriggerId` and a payload (`object Payload`).

Depending on your configuration, the `IWebHookNotification` can be passed synchronously (on the current thread), via a `QueuedHostedService` (defers the local treatment of the notification the a background service) or via a messaging service if you use `Harpoon.MassTransit` (lets you potentially treat the notification on another application) to the next handler, `IQueuedProcessor<IWebHookNotification>`.

`IQueuedProcessor<IWebHookNotification>` lets `IWebHookStore` find the matching registrations, and passes the generated `IWebHookWorkItem` along to the registered `IWebHookSender`. To use EF Core to register your webhooks, use `Harpoon.Registrations.EFStorage` (see details below). `IWebHookWorkItem` contains the notification and a matching registration. One `IWebHookNotification` may therefore generate a lot of `IWebHookWorkItem`.

Once again, the treatment of the `IWebHookWorkItem` can be done synchronously (on the current thread), via a `QueuedHostedService` (to defer the local treatment) or via a messaging service (using `Harpoon.MassTransit`) to the next handler `IQueuedProcessor<IWebHookWorkItem>` (to potentially treat the IWebHookWorkItem on another application).

Finally, the `IWebHookWorkItem` are sent via the `IQueuedProcessor<IWebHookWorkItem>`. The general retry policy and failures policy should be configured using `Polly` during the dependency injection registration, as the `IHttpClientBuilder` is exposed; the default in cas of problem is to do nothing. Those behaviors can also be overriden directly.

## General Q/A

### What are the default validation requirements for webhooks

The ``DefaultWebHookValidator`` expects the following things:

- `Id` must be a non default ``Guid``. If not, a new `Guid` is assigned.
- `Secret` must be a 64 characters long string. If empty, a new string is assigned.
- `Filters` must be valid, which means:
  - the `WebHook` must contain at least one filter
  - the ``Trigger`` must match one of the available trigger, obtained by the `IWebHookTriggerProvider`. (see below to allow pattern matching)
- the `callback` url must be a valid http(s) url. If the url contains the `noecho` parameter, the url is not tested.
If not, the validator will send a `GET` request to the callback with an ``echo`` query parameter, and expect to see the given `echo` returned in the body.

### How data protection works on expired keys

The default `ISecretProtector` will use expired keys if the current key is not the one used to protect the database content. If you want to change this behavior, you need to implement your own `ISecretProtector`, or to change your key expiration policy.

### What's the default way the webhook reference the current user in the database

To reference which user created them, the `DefaultPrincipalIdGetter` will try to find a unique string from the `IPrincipal` the following way:

- if the principal is a ``ClaimsPrincipal`` with a claim of type `ClaimTypes.Name`, return it
- if the principal is a ``ClaimsPrincipal`` with a claim of type `ClaimTypes.NameIdentifier`, return it
- if the principal has a named identity, return it
- throws if nothing was found

### How are webhooks registrations matched to an incoming notification

WebHooks registrations are matched to incoming notifications via the matching of `TriggerId`.

By default, `Notification.TriggerId` needs to match `WebHook.Trigger` exactly. It is possible to use pattern matching (see below) to let the user match a wider range of events.

WebHooks need also to not be paused.

The following example shows different `WebHook` and if they match or not the given notification.

```c#
var notification = new WebHookNotification
{
    TriggerId = "something_happened",
    Payload = new MyPayload
    {
        Id = 234,
        Property = "value",
        Sub = new SubPayload { Name = "my name" },
        Sequence = new List<int> { 1, 2, 3 }
    }
};

new WebHookFilter //does not match because of triggerId
{
    TriggerId = "something_else_happened"
};
new WebHookFilter //matches because of triggerId
{
    TriggerId = "something_happened"
};

```

### How are webhooks signed, and how to check the signature

The default `ISignatureService` calculates an `HMACSHA256` over the JSON send, using the shared secret. To verify the secret validity, the consumer may use the following c# snippet

```c#
//code from DefaultSignatureService.cs
public bool VerifySignature(string foundSignature, string sharedSecret, string jsonContent)
{
    var secretBytes = Encoding.UTF8.GetBytes(sharedSecret);
    var data = Encoding.UTF8.GetBytes(jsonContent ?? "");
    using (var hasher = new HMACSHA256(secretBytes))
    {
        return ToHex(hasher.ComputeHash(data)) == foundSignature;
    }
}
private string ToHex(byte[] data)
{
    if (data == null)
    {
        return string.Empty;
    }

    var content = new char[data.Length * 2];
    var output = 0;
    byte d;
    for (var input = 0; input < data.Length; input++)
    {
        d = data[input];
        content[output++] = _hexLookup[d / 0x10];
        content[output++] = _hexLookup[d % 0x10];
    }
    return new string(content);
}
```

### What's the default behavior if a delivery fails

The default behavior is to do nothing. If you wish to change it, you may:

- Create our own `IWebHookSender`, potentially by inheriting from `DefaultWebHookSender` or `EFWebHookSender`. Those classes expose the following methods to help you deal with errors

```c#
protected virtual Task OnSuccessAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken);
protected virtual Task OnNotFoundAsync(HttpResponseMessage response, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken);
protected virtual Task OnFailureAsync(HttpResponseMessage response, Exception exception, IWebHookWorkItem webHookWorkItem, CancellationToken cancellationToken)
```

- Use the `EFWebHookSender`, that automatically pauses webhooks in case of 404 and 410. Please notice that the given `WebHookWorkItem` is NOT attached to the current `DbContext`.
- During services configuration, use the exposed `IHttpClientBuilder` to apply a retry/failures policy. You may use the following extensions method on `IHarpoonBuilder`:

```c#
h.UseDefaultWebHookWorkItemProcessor(Action<IHttpClientBuilder> senderPolicy); //when using the default processor
h.UseDefaultEFWebHookWorkItemProcessor(Action<IHttpClientBuilder> senderPolicy); //when using the default ef processor
h.UseDefaultValidator(Action<IHttpClientBuilder> validatorPolicy); //during the validation process

h.UseAllSynchronousDefaults(Action<IHttpClientBuilder> senderPolicy); //when using synchronous all defaults
h.UseAllLocalDefaults(Action<IHttpClientBuilder> senderPolicy); //when using background service all defaults
h.UseAllMassTransitDefaults(Action<IHttpClientBuilder> senderPolicy); //when using masstransit all defaults
```

## Tutorials

### How to start a notification process

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
        var notification = new WebHookNotification
        {
            TriggerId = "SomeObject.Created",
            //this will be serialized and send to the consumers
            Payload = new MyPayload
            {
                SomeValue = 23,
                SomeOtherValue = "value"
            }
        };

         //what this precisely does depends on you configuration
        await _webHookService.NotifyAsync();
    }
}
```

### How to treat everything locally and synchronously (using a mock storage)

```c#
//Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h.UseAllSynchronousDefaults()); //everything is done locally and synchronously
    services.AddSingleton(new Mock<IWebHookStore>().Object); //mock storage, see below for EF storage
}
```

### How to treat everything locally and in the background using background services (using a mock storage)

```c#
//Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h.UseAllLocalDefaults()); //everything is done locally via background services
    services.AddSingleton(new Mock<IWebHookStore>().Object); //mock storage, see below for EF storage
}
```

### How to notify another application, and let it treat the notifications synchronously, via a messaging service

You need to include the nuget `Harpoon.MassTransit` in App1 and App2 for this to work.

```c#
//App1.Startup.cs
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h => h.SendNotificationsUsingMassTransit());

    services.AddMassTransit(p => /* your configuration here. */);
}
```

```c#
//App2.Startup.cs
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

### How to treat the notification synchronously and locally, but let another application actually do the http calls, via a messaging service

You need to include via nuget `Harpoon.MassTransit` in App1 and App2 for this to work.

```c#
//App1.Startup.cs
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

```c#
//App2.Startup.cs
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

### How to use Ef for webhooks storage

You need to include `Harpoon.Registrations.EFStorage` via nuget.

```c#
//Startup.cs
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddEntityFrameworkSqlServer().AddDbContext<MyContext>(); // register EF and your context as you normally would

    services.AddHarpoon(h =>
    {
        // MyContext needs to implement IRegistrationsContext, MyWebHookTriggerProvider to implement IWebHookTriggerProvider
        h.RegisterWebHooksUsingEfStorage<MyContext, MyWebHookTriggerProvider>();
        h.UseDefaultDataProtection(p => { }, o => { }); // the default data protection uses System.DataProtection
    });
}
```

Don't forget to generate a migration if you are using EF Core Migrations.

```c#
public class TestContext : DbContext, IRegistrationsContext
{
    public DbSet<WebHook> WebHooks { get; set; }
    IQueryable<WebHook> IRegistrationsContext.WebHooks => WebHooks;

    public DbSet<WebHookFilter> WebHookFilters { get; set; } //optional

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddHarpoonDefaultMappings(); //optional. this lets you apply default mappings and constraints.
    }
}
```

### How to use default REST controllers

To use the default mvc controllers to provide REST operations on your webhooks, add the nuget package `Harpoon.Controllers`. The controlelrs should be automatically added to your application.
You also need to register a `IWebHookValidator` in your DI; you may use `.UseDefaultValidator()` or provide your own.

```c#
//Startup.cs
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddEntityFrameworkSqlServer().AddDbContext<MyContext>(); // register EF and your context

    services.AddHarpoon(h =>
    {
        h.RegisterWebHooksUsingEfStorage<MyContext>(); //MyContext needs to implement IRegistrationsContext.
        h.UseDefaultDataProtection(p => { }, o => { }); //the default data protection uses System.DataProtection.
        h.UseDefaultValidator(); //the default validator is necessary for Write operations. This is necessary for WebHookRegistrationStore but not for WebHookStore.
    });
}
```

### How to setup your retry policy on your sender

To prefered way to setup you retry policy is to use [Polly](https://github.com/App-vNext/Polly), by adding `Microsoft.Extensions.Http.Polly`. [The general help is here.](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-2.1)

```c#
//Startup.cs
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

### How to use pattern matching for triggers

If you want to allow users to registers webhooks on triggers (which is not on by default) such as `object.*` that would apply to `object.created`, `object.updated` and so forth, you need to:

- Change default webhooks registration validation `IWebHookValidator.ValidateAsync` to allow for such triggers to be considered valid.
- Modify default ``IWebHookStore`` implementation so that the trigger is understood correctly.

The following example is based on overrides of the default implementations, but other approaches are possible.

```c#
public static class TriggerHerlper
{
    public static IEnumerable<string> GetPotentialTriggers(string trigger)
    {
        if (trigger == null)
        {
            return Enumerable.Empty<string>();
        }
        return GetPotentialTriggers(trigger.Split('.'), 0);
    }

    private static IEnumerable<string> GetPotentialTriggers(string[] parts, int index)
    {
        var options = new[] { "*", parts[index] };
        if (parts.Length == index + 1)
        {
            return options;
        }

        return GetPotentialTriggers(parts, index + 1).SelectMany(e => options.Select(c => c + "." + e));
    }
}

public class MyWebHookStore<TContext> : WebHookStore<TContext>
    where TContext : DbContext, IRegistrationsContext
{
    //...ctr

    protected override IQueryable<WebHook> FilterQuery(IQueryable<WebHook> query, IWebHookNotification notification)
    {
        var validTriggers = TriggerHerlper.GetPotentialTriggers(notification.TriggerId).ToArray();
        return query.Where(w => w.Filters == null || w.Filters.Count == 0 || w.Filters.Any(f => validTriggers.Contains(f.Trigger)));
    }
}

public class MyWebHookValidator : DefaultWebHookValidator
{
    //...ctr

    //This implementation is basically a copy of the default one
    protected override Task VerifyFiltersAsync(IWebHook webHook, CancellationToken cancellationToken)
    {
        if (webHook.Filters == null || webHook.Filters.Count == 0)
        {
            throw new ArgumentException("WebHooks need to target at least one trigger. Wildcard is not allowed.");
        }

        var validTriggers = WebHookTriggerProvider
            .GetAvailableTriggers()
            .SelectMany(kvp => TriggerHerlper.GetPotentialTriggers(kvp.Key).Select(trigger => (trigger, kvp.Value)))
            .GroupBy(t => t.trigger)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Value));

        var errors = new List<string>();
        foreach (var filter in webHook.Filters)
        {
            if (!validTriggers.ContainsKey(filter.Trigger))
            {
                errors.Add($" - Trigger {filter.Trigger} is not valid.");
                continue;
            }
        }

        if (errors.Count != 0)
        {
            throw new ArgumentException("WebHooks filters are incorrect :" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
        return Task.CompletedTask;
    }
}

//Startup.cs
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h =>
    {
        //h.UseDefaultValidator(); can be used without breaking anything, but is completely unecessary

        // this adds other MyWebHookStore dependencies so can still be used fo convenience
        h.RegisterWebHooksUsingEfStorage<MyContext>();

        //customize your services as you usually would otherwise
    });

    services.AddScoped<IWebHookStore, MyWebHookStore<MyContext>>();
    services.AddScoped<IWebHookValidator, MyWebHookValidator>();
    services.AddHttpClient<IWebHookValidator, MyWebHookValidator>();
}
```

### How to describe you available triggers

The class `WebHookTrigger` represents your available events for consumer to subscribe to. It contains the following properties:

- `string Id`: a unique string, typically in the form of `noun.verb`
- `string Description`: a short description for your interface
- `Type PayloadType`: the type of the payload. This is necessary for the documentation auto-generation.
The documentation regarding your webhooks can later on be auto-generated, using the ``[WebHookSubscriptionFilter]`` on your subscription endpoint of your API. This is the default if you use `Harpoon.Controllers`.

The following code exposes the default way to benefit from the auto generated Open Api documentation via swagger.

```c#
//Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    services.AddHarpoon(h =>
    {
         // your configuration here
        //...
         h.AddControllers<MyWebHookTriggerProvider>();
    });

    services.AddSwaggerGen(c =>
    {
        // configuration of your own apis
        //...
        c.AddHarpoonDocumentation();
    });
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseSwaggerUI(c =>
    {
        //your configuration...
        c.AddHarpoonEndpoint();
    });
}
```