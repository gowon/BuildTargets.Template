namespace build.Commands;

using System.CommandLine;
using System.Net.NetworkInformation;

public class PingCommand : Command
{
    public readonly Option<Uri> EndpointOption = new(new[] { "--endpoint", "-e" }, "The target IP or hostname")
        { IsRequired = true };

    public readonly Option<int> TimeoutOption = new(new[] { "--timeout", "-t" }, () => 1000,
        "The maximum number in milliseconds of time to wait for a response");

    public PingCommand() : base("ping", "Execute ping on a given endpoint")
    {
        AddOption(EndpointOption);
        AddOption(TimeoutOption);

        this.SetHandler(context =>
        {
            var uri = context.ParseResult.GetValueForOption(EndpointOption);
            var timeout = context.ParseResult.GetValueForOption(TimeoutOption);

            using var ping = new Ping();
            try
            {
                context.Console.WriteLine($"Pinging '{uri}'...");
                var pingReply = ping.Send(uri!.ToString(), timeout);
                if (pingReply.Status == IPStatus.Success)
                {
                    context.Console.WriteLine($"Address: {pingReply.Address}");
                    context.Console.WriteLine($"RoundTrip time: {pingReply.RoundtripTime}");
                    context.Console.WriteLine($"Time to live: {pingReply.Options!.Ttl}");
                    context.Console.WriteLine($"Buffer size: {pingReply.Buffer.Length}");
                }
                else
                {
                    context.Console.WriteLine($"'{uri}' is not reachable for this reason: {pingReply.Status}");
                }
            }
            catch (Exception ex)
            {
                context.Console.WriteLine($"'{uri}' is not reachable for this reason: {ex.Message}");
            }
        });
    }
}