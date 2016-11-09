using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.AspNet.SignalR;
using Smart.Controllers;
using Smart.Data;
using Smart.Repository;
using Smart.Service;

namespace Smart.App_Start
{
    public class AutofacConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterWebApiFilterProvider(config);

            builder.RegisterType<MessageHub>().ExternallyOwned();

            //builder.Register(x => new MessageHub(new ChatsService(new ChatsRepository(), new MappingService())))
            //    .As<Hub>();
            builder.Register(x => new UserService(new UserRepository(), new MappingService())).As<IUserService>();
            builder.Register(x => new ChatsService(new ChatsRepository(new AuthContext()), new UserRepository(), new MappingService())).As<IChatsService>();

            builder.Register(x => new ChatsService(new ChatsRepository(new AuthContext()), new UserRepository(), new MappingService())).As<IChatsService>();
            builder.Register(x => new MessageService(new MessageRepository(new AuthContext()), new MappingService())).As<IMessageService>();

            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

        }
    }
}