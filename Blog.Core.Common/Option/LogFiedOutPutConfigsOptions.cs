using Blog.Core.Common.Option.Core;
using Blog.Core.Serilog.Es.Formatters;

namespace Blog.Core.Common.Option;

public class LogFiedOutPutConfigsOptions : IConfigurableOptions
{
    /// <summary>
    /// tcp日志的host地址
    /// </summary>
    public string tcpAddressHost { set; get; }

    /// <summary>
    /// tcp日志的port地址
    /// </summary>
    public int tcpAddressPort { set; get; }

    public List<Configsinfo> ConfigsInfo { get; set; }
}

public class Configsinfo
{
    public string FiedName { get; set; }
    public string FiedValue { get; set; }
}