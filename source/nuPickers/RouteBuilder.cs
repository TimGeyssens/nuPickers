﻿using System.Web.Mvc;
using System.Web.Routing;

namespace nuPickers
{
    public static class RouteBuilder
    {
        public static void BuildRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                name: "nuPickersShared",
                url: "App_Plugins/nuPickers/Shared/{folder}/{file}.nu",
                defaults: new
                {
                    controller = "EmbeddedResource",
                    action = "GetSharedResource"
                },
                namespaces: new[] { "nuPickers" }
                );
        }
    }
}