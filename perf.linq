<Query Kind="Program">
  <NuGetReference>NBomber</NuGetReference>
  <NuGetReference>NBomber.Http</NuGetReference>
  <Namespace>NBomber.Plugins.Http.CSharp</Namespace>
  <Namespace>NBomber.CSharp</Namespace>
  <Namespace>FsToolkit.ErrorHandling</Namespace>
  <Namespace>NBomber.Contracts</Namespace>
  <Namespace>System.Net.Http.Json</Namespace>
</Query>

void Main()
{
	var factory = HttpClientFactory.Create();

	var almost = Step.Create("almost", factory, async context =>
	{
		var response = await context.Client.GetAsync("http://localhost:5275/");

		return response.IsSuccessStatusCode
			? Response.Ok(statusCode: (int) response.StatusCode)
			: Response.Fail(statusCode: (int) response.StatusCode);
	});

	var better = Step.Create("better", factory, async context =>
	{
		var response = await context.Client.GetAsync("http://localhost:5134/");

		return response.IsSuccessStatusCode
			? Response.Ok(statusCode: (int)response.StatusCode)
			: Response.Fail(statusCode: (int)response.StatusCode);
	});

	var s1 = ScenarioBuilder
		.CreateScenario("almost", almost)
		.WithWarmUpDuration(TimeSpan.FromSeconds(10))
		.WithLoadSimulations(Simulation.KeepConstant(24, TimeSpan.FromSeconds(60)));

	var s2 = ScenarioBuilder
		.CreateScenario("better", better)
		.WithWarmUpDuration(TimeSpan.FromSeconds(10))
		.WithLoadSimulations(Simulation.KeepConstant(24, TimeSpan.FromSeconds(60)));
		
	NBomberRunner
	.RegisterScenarios(s2, s1)
	.Run();
}

