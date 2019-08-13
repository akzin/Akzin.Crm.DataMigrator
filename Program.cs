using CommandLine;
using Akzin.Crm.DataMigrator.Migration;
using Akzin.Crm.DataMigrator.Services;
using Akzin.Crm.DataMigrator.Strategy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Akzin.Crm.DataMigrator.Helpers;

namespace Akzin.Crm.DataMigrator
{
    class Program
    {
        private static void Init()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        [STAThread]
        static void Main(string[] args)
        {
            Init();
            Run(args);
        }

        private static void Run(string[] args)
        {
            var parseResult = Parser.Default.ParseArguments<ExportOptions, ImportOptions>(args)
                .WithParsed<ExportOptions>(Export)
                .WithParsed<ImportOptions>(Import);

            if (parseResult.Tag == ParserResultType.NotParsed)
            {
                //Exit with error code when input arguments are invalid
                Environment.Exit(-1);
            }
        }

        private static void Export(ExportOptions options)
        {
            var crmService = new CrmService(options.CrmConnectionString);
            var fileService = new FileService(options.Directory);
            var entityStrategyFactory = new EntityStrategyFactory(crmService);
            var exporter = new EntityExporter(crmService, fileService, entityStrategyFactory);

            foreach (var entity in options.EntitiesList)
            {
                exporter.Export(entity);
            }
        }

        private static void Import(ImportOptions options)
        {
            var crmService = new CrmService(options.CrmConnectionString);
            var fileService = new FileService(options.Directory);
            var entityStrategyFactory = new EntityStrategyFactory(crmService);
            var importer = new EntityImporter(crmService, fileService, entityStrategyFactory);

            var commandsList = new List<CrmCommandsQueue>();
            foreach (var entityLogicalName in options.EntitiesList)
            {
                var commands = new CrmCommandsQueue(entityLogicalName);
                importer.Import(commands, entityLogicalName);

                if(!commands.IsEmpty)
                    commandsList.Add(commands);
            }

            using (new DisabledWorkflowsScope(crmService, commandsList.Select(x => x.EntityLogicalName)))
            {
                foreach (var commands in commandsList)
                {
                    commands.ExecuteUpdateSteps(crmService);
                }

                foreach (var commands in commandsList.AsEnumerable().Reverse())
                {
                    commands.ExecuteDeleteSteps(crmService);
                }
            }
        }

        [Verb("export", HelpText = "Exports CRM data")]
        public class ExportOptions
        {
            [Option(shortName: 'c', longName: "crm", Required = true, HelpText = "CRM Connectionstring: AuthType=Office365;Url=http://contoso:8080/Test;UserName=jsmith@contoso.onmicrosoft.com;Password=passcode")]
            public string CrmConnectionString { get; set; }

            [Option(shortName: 'e', longName: "entities", Required = true)]
            public string Entities { get; set; }

            public string[] EntitiesList => Entities.Split(',', ';');

            [Option(shortName: 'd', longName: "directory", Required = true)]
            public string Directory { get; set; }
        }

        [Verb("import", HelpText = "Imports CRM data")]
        public class ImportOptions
        {
            [Option(shortName: 'c', longName: "crm", Required = true, HelpText = "CRM Connectionstring: AuthType=Office365;Url=http://contoso:8080/Test;UserName=jsmith@contoso.onmicrosoft.com;Password=passcode")]
            public string CrmConnectionString { get; set; }

            [Option(shortName: 'e', longName: "entities", Required = true)]
            public string Entities { get; set; }

            public string[] EntitiesList => Entities.Split(',', ';');

            [Option(shortName: 'd', longName: "directory", Required = true)]
            public string Directory { get; set; }
        }
    }
}
