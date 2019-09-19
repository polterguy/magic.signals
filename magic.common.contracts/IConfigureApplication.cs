/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace magic.common.contracts
{
    /// <summary>
    /// Interface allowing you to configure application. If you implement this interface on your
    /// own type, your type will be automatically created and its Configure method will be invoked
    /// during startup/configuration phase of the application. This allows your modules to
    /// configure the application the way they require for themselves.
    /// 
    /// Requires modification of your Startup file, in addition to a constructor on your implementing
    /// type taking no arguments.
    /// </summary>
    public interface IConfigureApplication
    {
        /// <summary>
        /// Invoked when the application needs to be configured for some reasons.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="configuration">The configuration of your application</param>
        void Configure(IApplicationBuilder app, IConfiguration configuration);
    }
}
