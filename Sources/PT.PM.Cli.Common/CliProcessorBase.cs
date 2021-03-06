﻿using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using PT.PM.Common;
using PT.PM.Common.SourceRepository;
using PT.PM.Common.Utils;
using PT.PM.Matching.PatternsRepository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace PT.PM.Cli.Common
{
    public abstract class CliProcessorBase<TStage, TWorkflowResult, TPattern, TParameters, TRenderStage>
        where TStage : Enum
        where TWorkflowResult : WorkflowResultBase<TStage, TPattern, TRenderStage>
        where TParameters : CliParameters, new()
        where TRenderStage : Enum
    {
        public ILogger Logger { get; protected set; } = new NLogLogger();

        public TParameters Parameters { get; protected set; }

        public virtual bool ContinueWithInvalidArgs => false;

        public virtual bool StopIfDebuggerAttached => true;

        public virtual int DefaultMaxStackSize => Utils.DefaultMaxStackSize;

        public abstract string CoreName { get; }

        public TWorkflowResult Process(string args) => Process(args.SplitArguments());

        public TWorkflowResult Process(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            TWorkflowResult result = null;

            if (Parameters != null)
            {
                result = ProcessJsonConfig(args);
            }
            else
            {
                var paramsNormalizer = new CliParametersNormalizer<TParameters>();
                bool success = paramsNormalizer.Normalize(args, out string[] outArgs);

                var parser = new Parser(config =>
                {
                    config.IgnoreUnknownArguments = ContinueWithInvalidArgs;
                    config.CaseInsensitiveEnumValues = true;
                });

                ParserResult<TParameters> parserResult = parser.ParseArguments<TParameters>(outArgs);

                if (success || ContinueWithInvalidArgs)
                {
                    parserResult.WithParsed(
                        parameters =>
                        {
                            Parameters = parameters;
                            FillLoggerSettings(parameters);
                            Logger.LogErrors(paramsNormalizer.Errors);
                            result = ProcessJsonConfig(outArgs);
                        })
                        .WithNotParsed(errors =>
                        {
                            Logger.LogErrors(paramsNormalizer.Errors);
                            if (ContinueWithInvalidArgs)
                            {
                                result = ProcessJsonConfig(outArgs, errors);
                            }
                            else
                            {
                                LogInfoAndErrors(outArgs, errors);
                            }
                        });
                }
            }

#if DEBUG
            if (StopIfDebuggerAttached && Debugger.IsAttached)
            {
                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
#endif

            return result;
        }

        protected virtual TWorkflowResult ProcessParameters(TParameters parameters)
        {
            TWorkflowResult result = null;

            int maxStackSize = parameters.MaxStackSize?.ConvertToInt32(ContinueWithInvalidArgs, DefaultMaxStackSize, Logger)
                               ?? DefaultMaxStackSize;

            if (maxStackSize == 0)
            {
                result = RunWorkflow(parameters);
            }
            else
            {
                Thread thread = new Thread(() => result = RunWorkflow(parameters), maxStackSize);
                thread.Start();
                thread.Join();
            }

            return result;
        }

        protected virtual WorkflowBase<TStage, TWorkflowResult, TPattern, TRenderStage>
            InitWorkflow(TParameters parameters)
        {
            var workflow = CreateWorkflow(parameters);

            workflow.SourceRepository = CreateSourceRepository(parameters);

            if (parameters.Languages?.Count() > 0)
            {
                workflow.SourceRepository.Languages = parameters.Languages.ParseLanguages();
            }

            workflow.PatternsRepository = CreatePatternsRepository(parameters);
            workflow.Logger = Logger;
            NLogLogger nLogLogger = Logger as NLogLogger;

            if (parameters.Stage != null)
            {
                workflow.Stage = parameters.Stage.ParseEnum(ContinueWithInvalidArgs, workflow.Stage, Logger);
            }

            if (parameters.ThreadCount.HasValue)
            {
                workflow.ThreadCount = parameters.ThreadCount.Value;
            }
            if (parameters.NotFoldConstants.HasValue)
            {
                workflow.IsFoldConstants = !parameters.NotFoldConstants.Value;
            }
            if (parameters.MaxStackSize.HasValue)
            {
                workflow.MaxStackSize = parameters.MaxStackSize.Value.ConvertToInt32(ContinueWithInvalidArgs, workflow.MaxStackSize, Logger);
            }
            if (parameters.Memory.HasValue)
            {
                workflow.MemoryConsumptionMb = parameters.Memory.Value.ConvertToInt32(ContinueWithInvalidArgs, workflow.MemoryConsumptionMb, Logger);
            }
            if (parameters.FileTimeout.HasValue)
            {
                workflow.FileTimeout = TimeSpan.FromSeconds(parameters.FileTimeout.Value);
            }
            if (parameters.LogsDir != null)
            {
                workflow.LogsDir = NormalizeLogsDir(parameters.LogsDir);
                workflow.DumpDir = NormalizeLogsDir(parameters.LogsDir);
            }
            if (parameters.Silent != null && nLogLogger != null)
            {
                if (parameters.Silent.Value)
                {
                    Logger.LogInfo("Silent mode.");
                }
                nLogLogger.IsLogToConsole = !parameters.Silent.Value;
            }
            if (parameters.NoLog != null && nLogLogger != null)
            {
                if (parameters.NoLog.Value)
                {
                    Logger.LogInfo("File log disabled.");
                }
                nLogLogger.IsLogToFile = false;
            }
            if (parameters.TempDir != null)
            {
                workflow.TempDir = NormalizeLogsDir(parameters.TempDir);
            }
            if (parameters.IndentedDump.HasValue)
            {
                workflow.IndentedDump = parameters.IndentedDump.Value;
            }
            if (parameters.NotIncludeTextSpansInDump.HasValue)
            {
                workflow.DumpWithTextSpans = !parameters.NotIncludeTextSpansInDump.Value;
            }
            if (parameters.LineColumnTextSpans.HasValue)
            {
                workflow.LineColumnTextSpans = parameters.LineColumnTextSpans.Value;
            }
            if (parameters.IncludeCodeInDump.HasValue)
            {
                workflow.IncludeCodeInDump = parameters.IncludeCodeInDump.Value;
            }
            if (parameters.StrictJson.HasValue)
            {
                workflow.StrictJson = parameters.StrictJson.Value;
            }
            if (parameters.IsDumpJsonOutput.HasValue)
            {
                workflow.IsDumpJsonOutput = parameters.IsDumpJsonOutput.Value;
            }
            if (parameters.DumpStages?.Count() > 0)
            {
                workflow.DumpStages = new HashSet<TStage>(parameters.DumpStages.ParseEnums<TStage>(ContinueWithInvalidArgs, Logger));
            }
            if (parameters.DumpPatterns.HasValue)
            {
                workflow.IsDumpPatterns = parameters.DumpPatterns.Value;
            }
            if (parameters.RenderStages?.Count() > 0)
            {
                workflow.RenderStages = new HashSet<TRenderStage>(parameters.RenderStages.ParseEnums<TRenderStage>(ContinueWithInvalidArgs, Logger));
            }
            if (parameters.RenderFormat != null)
            {
                workflow.RenderFormat = parameters.RenderFormat.ParseEnum(ContinueWithInvalidArgs, workflow.RenderFormat, Logger);
            }
            if (parameters.RenderDirection != null)
            {
                workflow.RenderDirection = parameters.RenderDirection.ParseEnum(ContinueWithInvalidArgs, workflow.RenderDirection, Logger);
            }
            if (parameters.SerializationFormat != null)
            {
                workflow.SerializationFormat =
                    parameters.SerializationFormat.ParseEnum(ContinueWithInvalidArgs, workflow.SerializationFormat, Logger);
            }
            if (parameters.CompressedSerialization.HasValue)
            {
                workflow.CompressedSerialization = parameters.CompressedSerialization.Value;
            }

            return workflow;
        }

        protected virtual SourceRepository CreateSourceRepository(TParameters parameters)
        {
            return RepositoryFactory
                .CreateSourceRepository(parameters.InputFileNameOrDirectory, parameters.TempDir, parameters);
        }

        protected virtual IPatternsRepository CreatePatternsRepository(TParameters parameters)
        {
            return RepositoryFactory.CreatePatternsRepository(parameters.Patterns, parameters.PatternIds, Logger);
        }

        protected abstract WorkflowBase<TStage, TWorkflowResult, TPattern, TRenderStage> CreateWorkflow(TParameters parameters);

        protected abstract void LogStatistics(TWorkflowResult workflowResult);

        private TWorkflowResult ProcessJsonConfig(string[] args, IEnumerable<Error> errors = null)
        {
            try
            {
                var parameters = Parameters ?? new TParameters();

                bool error = false;
                string configFile = FileExt.Exists("config.json") ? "config.json" : parameters.ConfigFile;
                if (!string.IsNullOrEmpty(configFile))
                {
                    string content = null;
                    try
                    {
                        content = FileExt.ReadAllText(configFile);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                        error = true;
                    }

                    if (content != null)
                    {
                        try
                        {
                            var settings = new JsonSerializerSettings
                            {
                                MissingMemberHandling = ContinueWithInvalidArgs
                                    ? MissingMemberHandling.Ignore
                                    : MissingMemberHandling.Error
                            };
                            JsonConvert.PopulateObject(content, parameters, settings);
                            FillLoggerSettings(parameters);
                            Logger.LogInfo($"Load settings from {configFile}...");
                            SplitOnLinesAndLog(content);
                        }
                        catch (JsonException ex)
                        {
                            FillLoggerSettings(parameters);
                            Logger.LogError(ex);
                            Logger.LogInfo("Ignored some parameters from json");
                            error = true;
                        }
                    }
                }

                LogInfoAndErrors(args, errors);
                if (errors != null)
                {
                    Logger.LogInfo("Ignored some cli parameters");
                }

                if (!error || ContinueWithInvalidArgs)
                {
                    return ProcessParameters(parameters);
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);

                return null;
            }
        }

        private void FillLoggerSettings(TParameters parameters)
        {
            if (parameters.LogsDir != null)
            {
                Logger.LogsDir = NormalizeLogsDir(parameters.LogsDir);
            }
            Logger.IsLogDebugs = parameters.IsLogDebugs ?? false;
        }

        protected virtual TWorkflowResult RunWorkflow(TParameters parameters)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var workflow = InitWorkflow(parameters);
                TWorkflowResult workflowResult = workflow.Process();
                stopwatch.Stop();

                LogOutput(stopwatch.Elapsed, workflow, workflowResult);

                return workflowResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);

                return null;
            }
        }

        protected void LogOutput(TimeSpan totalElapsed, WorkflowBase<TStage, TWorkflowResult, TPattern, TRenderStage> workflow, TWorkflowResult workflowResult)
        {
            Logger.LogInfo("");

            LogExtraVersion();
            LoggerUtils.LogSystemInfo(Logger, CoreName);

            Logger.LogInfo("");

            if (!string.IsNullOrEmpty(workflowResult.RootPath))
            {
                Logger.LogInfo($"{"Scan path or file:",LoggerUtils.Align} {workflowResult.RootPath}");
            }
            string threadCountString = workflowResult.ThreadCount <= 0 ?
                "default" : workflowResult.ThreadCount.ToString();
            Logger.LogInfo($"{"Thread count:",LoggerUtils.Align} {threadCountString}");
            Logger.LogInfo($"{"Finish date:",LoggerUtils.Align} {DateTime.Now}");

            if (!workflow.Stage.Is(Stage.Match))
            {
                Logger.LogInfo($"{"Stage: ",LoggerUtils.Align} {workflow.Stage}");
            }

            LogMatchesCount(workflowResult);

            if (workflowResult.ErrorCount > 0)
            {
                Logger.LogInfo($"{"Errors count: ",LoggerUtils.Align} {workflowResult.ErrorCount}");
            }

            LogStatistics(workflowResult);

            Logger.LogInfo("");
            Logger.LogInfo($"{"Time elapsed:",LoggerUtils.Align} {totalElapsed.Format()}");
        }

        protected virtual void LogExtraVersion()
        {
        }

        protected virtual void LogMatchesCount(TWorkflowResult workflowResult)
        {
            Logger.LogInfo($"{"Matches count: ",LoggerUtils.Align} {workflowResult.TotalMatchesCount} ({workflowResult.TotalSuppressedCount} suppressed)");
        }

        private void LogInfoAndErrors(string[] args, IEnumerable<Error> errors)
        {
            Logger.LogInfo($"{CoreName} started at {DateTime.Now}");

            if (errors == null || errors.FirstOrDefault() is VersionRequestedError)
            {
                Logger.LogInfo($"{CoreName} version: {Utils.GetVersionString()}");
            }

            string commandLineArguments = "Command line arguments: " +
                    (args.Length > 0 ? string.Join(" ", args) : "not defined.");
            Logger.LogInfo(commandLineArguments);

            if (errors != null)
            {
                LogParseErrors(errors);
            }
        }

        private void LogParseErrors(IEnumerable<Error> errors)
        {
            foreach (Error error in errors)
            {
                if (error is HelpRequestedError || error is VersionRequestedError)
                {
                    continue;
                }

                string parameter = "";
                if (error is NamedError namedError)
                {
                    parameter = $"({namedError.NameInfo.NameText})";
                }
                else if (error is TokenError tokenError)
                {
                    parameter = $"({tokenError.Token})";
                }
                Logger.LogError(new Exception($"Launch Parameter {parameter} Error: {error.Tag}"));
            }

            if (!(errors.First() is VersionRequestedError))
            {
                var paramsParseResult = new Parser().ParseArguments<TParameters>(new[] { "--help" });
                string paramsInfo = HelpText.AutoBuild(paramsParseResult, 100);
                SplitOnLinesAndLog(paramsInfo);
            }
        }

        private void SplitOnLinesAndLog(string str)
        {
            string[] lines = str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                Logger.LogInfo(line);
            }
        }

        private string NormalizeLogsDir(string logsDir)
        {
            string shortCoreName = CoreName.Split('.').Last().ToLowerInvariant();

            if (!Path.GetFileName(logsDir).ToLowerInvariant().Contains(shortCoreName))
            {
                return Path.Combine(logsDir, CoreName);
            }

            return logsDir;
        }
    }
}
