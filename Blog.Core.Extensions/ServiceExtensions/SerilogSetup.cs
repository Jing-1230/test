using Blog.Core.Common;
using Blog.Core.Serilog.Configuration;
using Blog.Core.Serilog.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Blog.Core.Common.Option;
using Serilog.Sinks.Elasticsearch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Formatting;
namespace Blog.Core.Extensions.ServiceExtensions;

public static class SerilogSetup
{
    public static IHostBuilder AddSerilogSetup(this IHostBuilder host)
    {
        if (host == null) throw new ArgumentNullException(nameof(host));

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(AppSettings.Configuration)
            .Enrich.FromLogContext()
            //输出到控制台
            .WriteToConsole()
            //将日志保存到文件中
            .WriteToFile()
            //配置日志库
            .WriteToLogBatching();

        var option = App.GetOptions<SeqOptions>();
        //配置Seq日志中心
        if (option.Enabled)
        {
            var address = option.Address;
            var apiKey = option.ApiKey;
            if (!address.IsNullOrEmpty())
            {
                loggerConfiguration =
                    loggerConfiguration.WriteTo.Seq(address, restrictedToMinimumLevel: LogEventLevel.Verbose,
                        apiKey: apiKey, eventBodyLimitBytes: 10485760);
            }
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        //Serilog 内部日志
        /*var file = File.CreateText(LogContextStatic.Combine($"SerilogDebug{DateTime.Now:yyyyMMdd}.txt"));
        SelfLog.Enable(TextWriter.Synchronized(file));*/

        host.UseSerilog();
        return host;
    }

    public static IHostBuilder AddSerilogEsSetup(this IHostBuilder host)
    {
        if (host == null) throw new ArgumentNullException(nameof(host));

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(AppSettings.Configuration)
            .Enrich.FromLogContext()
            //输出到控制台
            .WriteToConsole();

        var option = App.GetOptions<LogFiedOutPutConfigsOptions>();
        //配置Elasticsearch日志中心
        if (!string.IsNullOrEmpty(option.tcpAddressHost))
        {
            loggerConfiguration =
                     loggerConfiguration
                     /*.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://" + option.tcpAddressHost + ":" + option.tcpAddressPort))
                     {
                         FailureCallback = (e,ex)=>{

                             Console.WriteLine("===============================================================");
                             Console.WriteLine("An error occurred while sending logs to Elasticsearch:");
                             Console.WriteLine(e.RenderMessage());
                             Console.WriteLine(ex.Message);
                             Console.WriteLine("===============================================================");
                         },
                         EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                       EmitEventFailureHandling.WriteToFailureSink |
                                       EmitEventFailureHandling.RaiseCallback,

                         AutoRegisterTemplate = true,
                         AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                         //IndexFormat = "log-jing-{0:yyyy.MM}",
                         IndexFormat = "log-{LogType}-{0:yyyy.MM.dd}",
                         OverwriteTemplate = true,
                         ModifyConnectionSettings = (settings) => settings.BasicAuthentication("blog", "1234qwer")
                     })*/
                     .WriteTo.Elasticsearch(GetElasticsearchSinkOptions());
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        host.UseSerilog();

        return host;
    }

    private static ElasticsearchSinkOptions GetElasticsearchSinkOptions()
    {
        var option = App.GetOptions<LogFiedOutPutConfigsOptions>();

        return new ElasticsearchSinkOptions(new Uri("http://" + option.tcpAddressHost + ":" + option.tcpAddressPort))
        {
            FailureCallback = (e, ex) =>
            {

                Console.WriteLine("===============================================================");
                Console.WriteLine("An error occurred while sending logs to Elasticsearch:");
                Console.WriteLine(e.RenderMessage());
                Console.WriteLine(ex.Message);
                Console.WriteLine("===============================================================");
            },
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                           EmitEventFailureHandling.WriteToFailureSink |
                                           EmitEventFailureHandling.RaiseCallback,

            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            //IndexFormat = "log-jing-{0:yyyy.MM}",
            //IndexFormat = "log-" + logName + "-{0:yyyy.MM.dd}",
            IndexFormat = "log-blog-core",
            OverwriteTemplate = true,
            ModifyConnectionSettings = (settings) => settings.BasicAuthentication("blog", "1234qwer"),
            BufferCleanPayload = (failingEvent, statuscode, exception) =>
            {
                dynamic e = JObject.Parse(failingEvent);
                return JsonConvert.SerializeObject(new Dictionary<string, object>()
                        {
                            { "@timestamp",e["@timestamp"]},
                            { "level","Error"},
                            { "message","Error: "+e.message},
                            { "messageTemplate",e.messageTemplate},
                            { "failingStatusCode", statuscode},
                            { "failingException", exception}
                        });
            },
        };
    }
}