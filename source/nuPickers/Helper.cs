﻿namespace nuPickers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Web;
    using System.Web.Hosting;
    using umbraco;
    using Umbraco.Web;

    internal static class Helper
    {
        internal static IEnumerable<string> GetAssemblyNames()
        {
            List<string> assemblyNames = new List<string>();

            // try to add App_Code directory
            string appCodePath = HostingEnvironment.MapPath("~/App_Code");
            if (appCodePath != null)
            {
                DirectoryInfo appCode = new DirectoryInfo(appCodePath);
                if (appCode.Exists && appCode.GetFiles().Length > 0 && Helper.GetAssembly(appCode.Name) != null)
                {
                    assemblyNames.Add(appCode.Name);
                }
            }

            // add any .dll assemblies from the /bin directory
            string binPath = HostingEnvironment.MapPath("~/bin");
            if (binPath != null)
            {
                assemblyNames.AddRange(Directory.GetFiles(binPath, "*.dll").Select(x => x.Substring(x.LastIndexOf('\\') + 1)));
            }

            return assemblyNames;
        }

        /// <summary>
        /// attempts to get an assembly by it's name
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns>an Assembly or null</returns>
        internal static Assembly GetAssembly(string assemblyName)
        {
            if (string.Equals(assemblyName, "App_Code", StringComparison.InvariantCultureIgnoreCase))
            {
				try
				{
					return Assembly.Load(assemblyName);
				}
				catch (FileNotFoundException)
				{
					return null;
				}
            }

            string assemblyFilePath = HostingEnvironment.MapPath(string.Concat("~/bin/", assemblyName));
            if (!string.IsNullOrEmpty(assemblyFilePath))
            {
                try
                {
                    // HACK: http://stackoverflow.com/questions/1031431/system-reflection-assembly-loadfile-locks-file
                    return Assembly.Load(File.ReadAllBytes(assemblyFilePath));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// extension method on Assembly to handle reflection loading exceptions
        /// </summary>
        /// <param name="assembly">the assembly to get types from</param>
        /// <returns>a collection of types found</returns>
        internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(x => x != null);
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// uses supplied url to check the file system (if prefixed with ~/) else makes an http query
        /// </summary>
        /// <param name="url">Url to download the resource from</param>
        /// <returns>An empty string, or the string result of either a file or an http response</returns>
        internal static string GetDataFromUrl(string url)
        {
            string data = string.Empty;

            if (!string.IsNullOrEmpty(url))
            {
                using (WebClient client = new WebClient())
                {
                    if (url.StartsWith("~/"))
                    {
                        string filePath = HttpContext.Current.Server.MapPath(url);

                        if (File.Exists(filePath))
                        {
                            url = filePath;
                        }
                        else
                        {
                            url = url.Replace("~/", (HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/"));
                        }
                    }

                    data = client.DownloadString(url);
                }
            }

            return data;
        }

        /// <summary>
        /// temp replacement for built-in uQuery method (as that hits the database)
        /// </summary>
        /// <param name="id">the id of a content, media or member item</param>
        /// <returns>an enum instance of the UmbracoObjectType</returns>
        internal static uQuery.UmbracoObjectType GetUmbracoObjectType(int id)
        {
            // return variable
            uQuery.UmbracoObjectType umbracoObjectType = uQuery.UmbracoObjectType.Unknown;

            UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            // attempt to get content
            if (umbracoHelper.TypedContent(id) != null)
            {
                umbracoObjectType = uQuery.UmbracoObjectType.Document;
            }
            else
            {
                // attempt to get media
                if(umbracoHelper.TypedMedia(id) != null)
                {
                    umbracoObjectType = uQuery.UmbracoObjectType.Media;
                }
                else
                {
                    // attempt to get member
                    try
                    {
                        if (umbracoHelper.TypedMember(id) != null)
                        {
                            umbracoObjectType = uQuery.UmbracoObjectType.Member;
                        }
                    }
                    catch
                    {
                        // HACK: suppress Umbraco error
                    }
                }
            }

            return umbracoObjectType;
        }
    }
}
