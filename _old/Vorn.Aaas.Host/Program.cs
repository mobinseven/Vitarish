AaasWebApplicationRunner aaasWebApplication = new(await AaasWebApplicationBuilder.BuildWebApplication(args));
await aaasWebApplication.Run();
