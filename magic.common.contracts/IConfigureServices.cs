/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace magic.common.contracts
{
    /// <summary>
    /// Interface allowing you to configure your services provider. If you implement this interface on your
    /// own type, your type will be automatically created and its Configure method will be invoked
    /// during startup/configuration phase of the application. This allows your modules to
    /// configure the services they require for themselves.
    /// 
    /// Requires modification of your Startup file, in addition to a constructor on your implementing
    /// type taking no arguments.
    /// </summary>
    public interface IConfigureServices
    {
        /// <summary>
        /// Invoked when your service collection should be configured
        /// </summary>
        /// <param name="services">The service collection of your application</param>
        /// <param name="configuration">The configuration of your application</param>
        void Configure(IServiceCollection collection, IConfiguration configuration);
    }
}
