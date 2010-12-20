﻿using System;
using System.Collections.Generic;
using System.Web.Hosting;
using System.Web.Routing;
using FubuCore;
using FubuMVC.Core.Content;
using FubuMVC.Core.Registration;

namespace FubuMVC.Core.Packaging
{
    public class FubuMvcPackageFacility : PackageFacility
    {
        private readonly PackagedImageUrlResolver _imageUrlResolver = new PackagedImageUrlResolver(new FileSystem());

       

        public FubuMvcPackageFacility()
        {
            string applicationPath = GetApplicationPath();

            if (applicationPath.IsNotEmpty())
            {
                var fileSystem = new FileSystem();

                // Development mode
                Loader(new PackageManifestReader(applicationPath, fileSystem, folder => folder));
                
                // Production mode with zip files
                var zipFilePackageReader = BuildZipFilePackageReader(applicationPath, fileSystem);
                Loader(zipFilePackageReader);
            }

            // TODO -- need an activator for scripts/*/styles, etc.

            Activator(new VirtualPathProviderActivator());
            Activator(new PackageFolderActivator(_imageUrlResolver));
        }

        public static ZipFilePackageReader BuildZipFilePackageReader(string applicationPath, FileSystem fileSystem)
        {
            var zipFileManifestReader = new PackageManifestReader(applicationPath, fileSystem, dir => FileSystem.Combine(applicationPath, FubuMvcPackages.WebContentFolder));
            var packageExploder = new PackageExploder(new ZipFileService(), new PackageExploderLogger(x => Console.WriteLine(x)), fileSystem);
            return new ZipFilePackageReader(zipFileManifestReader, packageExploder);
        }

        public static string GetApplicationPath()
        {
            return PhysicalRootPath ?? HostingEnvironment.ApplicationPhysicalPath ?? determineApplicationPathFromAppDomain();
        }

        private static string determineApplicationPathFromAppDomain()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (basePath.EndsWith("bin"))
            {
                basePath = basePath.Substring(0, basePath.Length - 3).TrimEnd('/').TrimEnd('\\');
            }

            return basePath;
        }

        public static string PhysicalRootPath { get; set; }

        public void AddRoutes(ICollection<RouteBase> routes)
        {
            var imageHandler = new ImageRouteHandler(_imageUrlResolver);
            imageHandler.RegisterRoute(routes);
        }


        public void RegisterServices(IServiceRegistry services)
        {
            services.AddService<IImageUrlResolver>(_imageUrlResolver);
        }

        public override string ToString()
        {
            return "FubuMVC Packaging Facility";
        }
    }
}