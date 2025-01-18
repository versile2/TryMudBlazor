﻿namespace Try.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Runtime;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components.Routing;
    using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Razor;
    using Microsoft.JSInterop;

    public class CompilationService
    {
        public const string DefaultRootNamespace = $"{nameof(Try)}.{nameof(UserComponents)}";

        private const string WorkingDirectory = "/TryMudBlazor/";
        private static readonly string[] DefaultImports =
        [
            "@using System.ComponentModel.DataAnnotations",
            "@using System.Linq",
            "@using System.Net.Http",
            "@using System.Net.Http.Json",
            "@using Microsoft.AspNetCore.Components.Forms",
            "@using Microsoft.AspNetCore.Components.Routing",
            "@using Microsoft.AspNetCore.Components.Web",
            "@using Microsoft.JSInterop",
            "@using MudBlazor"
        ];

        private const string MudBlazorServices = @"
<MudDialogProvider FullWidth=""true"" MaxWidth=""MaxWidth.ExtraSmall"" />
<MudSnackbarProvider/>

";

        // Creating the initial compilation + reading references is on the order of 250ms without caching
        // so making sure it doesn't happen for each run.
        private static CSharpCompilation _baseCompilation;
        private static CSharpParseOptions _cSharpParseOptions;

        private readonly RazorProjectFileSystem fileSystem = new VirtualRazorProjectFileSystem();
        private readonly RazorConfiguration configuration = new(
            RazorLanguageVersion.Latest,
            ConfigurationName: "Blazor",
            Extensions: ImmutableArray<RazorExtension>.Empty);

        public static async Task InitAsync(Func<IReadOnlyCollection<string>, ValueTask<IReadOnlyList<byte[]>>> getReferencedDllsBytesFunc)
        {
            var basicReferenceAssemblyRoots = new[]
            {
                typeof(Console).Assembly, // System.Console
                typeof(Uri).Assembly, // System.Private.Uri
                typeof(AssemblyTargetedPatchBandAttribute).Assembly, // System.Private.CoreLib
                typeof(NavLink).Assembly, // Microsoft.AspNetCore.Components.Web
                typeof(IQueryable).Assembly, // System.Linq.Expressions
                typeof(HttpClientJsonExtensions).Assembly, // System.Net.Http.Json
                typeof(HttpClient).Assembly, // System.Net.Http
                typeof(IJSRuntime).Assembly, // Microsoft.JSInterop
                typeof(RequiredAttribute).Assembly, // System.ComponentModel.Annotations
                typeof(MudBlazor.MudButton).Assembly, // MudBlazor
                typeof(WebAssemblyHostBuilder).Assembly, // Microsoft.AspNetCore.Components.WebAssembly
                typeof(FluentValidation.AbstractValidator<>).Assembly,
            };
            var assemblyNames = await getReferencedDllsBytesFunc(basicReferenceAssemblyRoots
                .SelectMany(assembly => assembly.GetReferencedAssemblies().Concat(
                [
                    assembly.GetName()
                ]))
                .Select(assemblyName => assemblyName.Name)
                .ToHashSet());

            var basicReferenceAssemblies = assemblyNames
                .Select(peImage => MetadataReference.CreateFromImage(peImage, MetadataReferenceProperties.Assembly))
                .ToList();

            _baseCompilation = CSharpCompilation.Create(
                DefaultRootNamespace,
                Array.Empty<SyntaxTree>(),
                basicReferenceAssemblies,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    concurrentBuild: false,
                    //// Warnings CS1701 and CS1702 are disabled when compiling in VS too
                    specificDiagnosticOptions: new[]
                    {
                        new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
                        new KeyValuePair<string, ReportDiagnostic>("CS1702", ReportDiagnostic.Suppress),
                    }));

            _cSharpParseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        }

        public async Task<CompileToAssemblyResult> CompileToAssemblyAsync(
            ICollection<CodeFile> codeFiles,
            Func<string, Task> updateStatusFunc) // TODO: try convert to event
        {
            ArgumentNullException.ThrowIfNull(codeFiles);

            var cSharpResults = await this.CompileToCSharpAsync(codeFiles, updateStatusFunc);

            await (updateStatusFunc?.Invoke("Compiling Assembly") ?? Task.CompletedTask);
            var result = CompileToAssembly(cSharpResults);

            return result;
        }

        private static CompileToAssemblyResult CompileToAssembly(IReadOnlyList<CompileToCSharpResult> cSharpResults)
        {
            if (cSharpResults.Any(r => r.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)))
            {
                return new CompileToAssemblyResult { Diagnostics = cSharpResults.SelectMany(r => r.Diagnostics).ToList() };
            }

            var syntaxTrees = new SyntaxTree[cSharpResults.Count];
            for (var i = 0; i < cSharpResults.Count; i++)
            {
                var cSharpResult = cSharpResults[i];
                syntaxTrees[i] = CSharpSyntaxTree.ParseText(cSharpResult.Code, _cSharpParseOptions, cSharpResult.FilePath);
            }

            var finalCompilation = _baseCompilation.AddSyntaxTrees(syntaxTrees);

            var compilationDiagnostics = finalCompilation.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info);

            var result = new CompileToAssemblyResult
            {
                Compilation = finalCompilation,
                Diagnostics = compilationDiagnostics
                    .Select(CompilationDiagnostic.FromCSharpDiagnostic)
                    .Concat(cSharpResults.SelectMany(r => r.Diagnostics))
                    .ToList(),
            };

            if (result.Diagnostics.All(x => x.Severity != DiagnosticSeverity.Error))
            {
                using var peStream = new MemoryStream();
                finalCompilation.Emit(peStream);

                result.AssemblyBytes = peStream.ToArray();
            }

            return result;
        }

        private static VirtualProjectItem CreateRazorProjectItem(string fileName, string fileContent)
        {
            var fullPath = WorkingDirectory + fileName;

            // File paths in Razor are always of the form '/a/b/c.razor'
            var filePath = fileName;
            if (!filePath.StartsWith('/'))
            {
                filePath = '/' + filePath;
            }

            fileContent = fileContent.Replace("\r", string.Empty);

            return new VirtualProjectItem(
                WorkingDirectory,
                filePath,
                fullPath,
                fileName,
                FileKinds.Component,
                Encoding.UTF8.GetBytes(fileContent.TrimStart()));
        }

        private async Task<IReadOnlyList<CompileToCSharpResult>> CompileToCSharpAsync(
            ICollection<CodeFile> codeFiles,
            Func<string, Task> updateStatusFunc)
        {
            // The first phase won't include any metadata references for component discovery. This mirrors what the build does.
            var projectEngine = this.CreateRazorProjectEngine(Array.Empty<MetadataReference>());

            // Result of generating declarations
            var declarations = new CompileToCSharpResult[codeFiles.Count];
            var index = 0;
            foreach (var codeFile in codeFiles)
            {
                if (codeFile.Type == CodeFileType.Razor)
                {
                    var fileContent = index == 0 ? MudBlazorServices : string.Empty;
                    fileContent += codeFile.Content;
                    var projectItem = CreateRazorProjectItem(codeFile.Path, fileContent);

                    var codeDocument = projectEngine.ProcessDeclarationOnly(projectItem);
                    var cSharpDocument = codeDocument.GetCSharpDocument();

                    declarations[index] = new CompileToCSharpResult
                    {
                        FilePath = codeFile.Path,
                        ProjectItem = projectItem,
                        Code = cSharpDocument.GeneratedCode,
                        Diagnostics = cSharpDocument.Diagnostics.Select(CompilationDiagnostic.FromRazorDiagnostic).ToList(),
                    };
                }
                else if (codeFile.Type == CodeFileType.CSharp)
                {
                    declarations[index] = new CompileToCSharpResult
                    {
                        FilePath = codeFile.Path,
                        Code = codeFile.Content,
                        Diagnostics = Enumerable.Empty<CompilationDiagnostic>(), // Will actually be evaluated later
                    };
                }

                index++;
            }

            // Result of doing 'temp' compilation
            var tempAssembly = CompileToAssembly(declarations);
            if (tempAssembly.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return [new CompileToCSharpResult { Diagnostics = tempAssembly.Diagnostics }];
            }

            // Add the 'temp' compilation as a metadata reference
            var references = new List<MetadataReference>(_baseCompilation.References) { tempAssembly.Compilation.ToMetadataReference() };
            projectEngine = CreateRazorProjectEngine(references);

            await (updateStatusFunc?.Invoke("Preparing Project") ?? Task.CompletedTask);

            var results = new CompileToCSharpResult[declarations.Length];
            for (index = 0; index < declarations.Length; index++)
            {
                var declaration = declarations[index];
                var isRazorDeclaration = declaration.ProjectItem != null;

                if (isRazorDeclaration)
                {
                    var codeDocument = projectEngine.Process(declaration.ProjectItem);
                    var cSharpDocument = codeDocument.GetCSharpDocument();

                    results[index] = new CompileToCSharpResult
                    {
                        FilePath = declaration.FilePath,
                        ProjectItem = declaration.ProjectItem,
                        Code = cSharpDocument.GeneratedCode,
                        Diagnostics = cSharpDocument.Diagnostics.Select(CompilationDiagnostic.FromRazorDiagnostic).ToList(),
                    };
                }
                else
                {
                    results[index] = declaration;
                }
            }

            return results;
        }

        private RazorProjectEngine CreateRazorProjectEngine(IReadOnlyList<MetadataReference> references) =>
            RazorProjectEngine.Create(configuration, fileSystem, builder =>
            {
                builder.SetRootNamespace(DefaultRootNamespace);
                builder.AddDefaultImports(DefaultImports);

                // Features that use Roslyn are mandatory for components
                CompilerFeatures.Register(builder);

                builder.Features.Add(new CompilationTagHelperFeature());
                builder.Features.Add(new DefaultMetadataReferenceFeature { References = references });
            });
    }
}
