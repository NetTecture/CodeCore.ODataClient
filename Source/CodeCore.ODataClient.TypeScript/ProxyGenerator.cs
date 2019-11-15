using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using CodeCore.ODataClient.Abstract;
using Microsoft.OData.Edm;

namespace CodeCore.ODataClient.TypeScript
{
    class ProxyGenerator : ProxyGeneratorBase
    {

        protected override void PrepareTarget(bool initialize)
        {
            string outputPath = TargetPath;
            // Header
            var assembly = Assembly.GetAssembly(typeof(ProxyGenerator));
            var assemblyName = assembly.GetName().Name;

            var prefix = $"{assemblyName}.TemplatesStatic.";
            var streams = assembly.GetManifestResourceNames();
            if (initialize) {
                foreach (var file in Directory.EnumerateFiles(outputPath)) {
                    File.Delete(file);
                }
                foreach (var templateName in streams.Where(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))) {
                    var file = String.Empty;
                    var name = templateName.Substring(prefix.Length);
                    if (name.StartsWith("_")) {
                        // We ignore files that start with _ - we use that for temp files that should not be deployed atm.
                        continue;
                    }
                    using (var resourceStream = assembly.GetManifestResourceStream(templateName))
                    using (var fileStream = new System.IO.FileStream(Path.Combine(outputPath, name), FileMode.Create)) {
                        resourceStream.CopyTo(fileStream);
                    }
                }
            }
        }

        public override void Generate(string tsNamespace, string contextName)
        {
            var outputPath = Path.Combine(TargetPath, $"{tsNamespace}.ts");

            GenerateHeaders();
            this.StringBuilder.AppendLine();
            GenerateImportStatements();
            this.StringBuilder.AppendLine();

            var prefix1 = "";
            GenerateEnumerations(prefix1);
            GenerateComplexTypes(prefix1);
            GenerateEntityTypes(prefix1);
            GenerateActions(prefix1);
            GenerateFunctions(prefix1);
            GenerateEntitySets(prefix1);
            GenerateContainer(prefix1, contextName);

            var s = this.StringBuilder.ToString();
            if (outputPath == default) {
                Console.WriteLine(s);
                return;
            }
            using (var sw = new StreamWriter(outputPath, false)) {
                sw.Write(s);
            }
        }

        void GenerateHeaders()
        {
            this.StringBuilder.AppendLine("// This is generated code. Do not add pr" +
                "operties. If bugs need fixing, inform the author of the");
            this.StringBuilder.AppendLine("// code generator to fix the bugs also in the backend.");
        }

        void GenerateImportStatements()
        {
            this.StringBuilder.AppendLine($"import {{ Injectable }} from '@angular/core';");
            this.StringBuilder.AppendLine($"import {{ map }} from 'rxjs/operators';");
            this.StringBuilder.AppendLine($"import {{");
            this.StringBuilder.AppendLine($"\tODataLiteral, ODataType,");
            this.StringBuilder.AppendLine($"\tODataContext, ODataSettings, ODataEntitySet,");
            this.StringBuilder.AppendLine($"\tODataOperationSet, ODataActionOperation, ODataFunctionOperation, ODataFunctionSetOperation, ODataGetOperation,");
            this.StringBuilder.AppendLine($"\tODataQueryResult");
            this.StringBuilder.AppendLine($"}} from './odataclient';");
        }

        public void GenerateEnumerations(string prefix)
        {
            foreach (var group in EdmModel.SchemaElements.OrderBy(x => x.Name).OfType<IEdmEnumType>().GroupBy(x => x.Namespace))
            {
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}//Namespace: {group.Key}");
                this.StringBuilder.AppendLine($"{prefix}export namespace {group.Key} {{");

                foreach (var item in group)
                {
                    this.StringBuilder.AppendLine();
                    this.StringBuilder.AppendLine($"{prefix}\t//Enum: {item.Name}");
                    this.StringBuilder.AppendLine($"{prefix}\texport enum {item.Name} {{");

                    var first = true;
                    foreach (var member in item.Members)
                    {
                        if (!first)
                        {
                            this.StringBuilder.AppendLine(",");
                        }
                        var name = member.Name;
                        var value = member.Value.Value;
                        this.StringBuilder.Append($"{prefix}\t\t{name} = \"{name}\"");
                        first = false;
                    }
                    this.StringBuilder.AppendLine();
                    this.StringBuilder.AppendLine($"{prefix}\t}}");
                }
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}}}");
            }
        }

        public void GenerateComplexTypes(string prefix)
        {
            foreach (var group in EdmModel.SchemaElements.OrderBy(x => x.Name).OfType<IEdmComplexType>().GroupBy(x => x.Namespace))
            {
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}//Namespace: {group.Key}");
                this.StringBuilder.AppendLine($"{prefix}export namespace {group.Key} {{");
                this.StringBuilder.AppendLine();
                foreach (var item in group)
                {
                    this.StringBuilder.AppendLine($"{prefix}\t//Complex Type: {item.Name}");
                    this.StringBuilder.Append($"{prefix}\texport interface {item.Name} ");
                    var baseType = item.BaseComplexType();
                    if (baseType != null)
                    {
                        var baseName = baseType.ShortQualifiedName();
                        this.StringBuilder.Append($" extends {baseName}");
                    }
                    this.StringBuilder.AppendLine($" {{");
                    foreach (var property in item.DeclaredStructuralProperties())
                    {
                        var name = property.Name;
                        var type = GetPropertyTypeName(property, group.Key);
                        if (type.Name == default)
                        {
                            // No support for this one
                            continue;
                        }
                        if (type.Optional)
                        {
                            name = name + "?";
                        }
                        this.StringBuilder.AppendLine($"{prefix}\t\t{name}: {type.Name};");
                    }
                    foreach (var property in item.DeclaredNavigationProperties())
                    {
                        var name = property.Name;
                        var typeName = GetPropertyTypeName(property, group.Key);
                        this.StringBuilder.AppendLine($"{prefix}\t\t{name}?: {typeName};");
                    }
                    this.StringBuilder.AppendLine($"{prefix}\t}}");
                    this.StringBuilder.AppendLine();
                }
                this.StringBuilder.AppendLine($"{prefix}}}");
            }
            this.StringBuilder.AppendLine();
        }

        public void GenerateEntityTypes (string prefix)
        {
            foreach (var group in EdmModel.SchemaElements.OrderBy(x=>x.Name).OfType<IEdmEntityType>().GroupBy(x => x.Namespace))
            {

                this.StringBuilder.AppendLine($"{prefix}//Namespace: {group.Key}");
                this.StringBuilder.AppendLine($"{prefix}export namespace {group.Key} {{");
                this.StringBuilder.AppendLine();
                foreach (var item in group)
                {
                    this.StringBuilder.AppendLine($"{prefix}\t//Entity Type: {item.Name}");
                    this.StringBuilder.Append($"{prefix}\texport interface {item.Name}");
                    var baseType = item.BaseEntityType();
                    if (baseType != null)
                    {
                        var baseName = baseType.ShortQualifiedName();
                        this.StringBuilder.Append($" extends {baseName}");
                    }
                    this.StringBuilder.AppendLine($" {{");
                    foreach (var property in item.DeclaredStructuralProperties())
                    {
                        var name = property.Name;
                        var type = GetPropertyTypeName(property, group.Key);
                        if (type.Name == default)
                        {
                            continue;
                        }
                        if (type.Optional)
                        {
                            name = name + "?";
                        }
                        this.StringBuilder.AppendLine($"{prefix}\t\t{name}: {type.Name};");
                    }
                    foreach (var property in item.DeclaredNavigationProperties())
                    {
                        var name = property.Name;
                        var typeName = GetPropertyTypeName(property, group.Key);
                        this.StringBuilder.AppendLine($"{prefix}\t\t{name}?: {typeName};");
                    }
                    this.StringBuilder.AppendLine($"{prefix}\t}}");
                    this.StringBuilder.AppendLine();
                }
                this.StringBuilder.AppendLine($"{prefix}}}");
                this.StringBuilder.AppendLine();
            }
        }

        void GenerateActions(string prefix)
        {
            foreach (var actionModel in EdmModel.SchemaElements.OrderBy(x => x.Name).OfType<IEdmAction>().Where(x=>x.IsAction()))
            {
                //if (actionModel.Name.StartsWith("Geo"))
                //{
                //    System.Diagnostics.Debugger.Break();
                //}
                var name = $"{actionModel.Name}In{actionModel.Namespace}";
                var nameInUrl = $"{actionModel.Namespace}.{actionModel.Name}";
                if (actionModel.IsBound)
                {
                    var bindingParameter = actionModel.Parameters.Where(x => x.Name == "bindingParameter").First();
                    switch (bindingParameter.Type.Definition)
                    {
                        case IEdmEntityType bindingType:
                            {
                                name = $"{name}On{bindingType.Name}In{bindingType.Namespace}";
                            }
                            break;
                        case IEdmCollectionType collectionType:
                            {
                                var elementType = collectionType.ElementType;
                                var bindingType = (IEdmEntityType)elementType.Definition;

                                name = $"{name}On{bindingType.Name}In{bindingType.Namespace}OnEntitySet";
                            }
                            break;
                        default:
                            break;

                    }
                }
                this.StringBuilder.AppendLine($"{prefix}export class {name} extends ODataActionOperation {{");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, url: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, url + '/{nameInUrl}');");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\trequest: object = {{}};");
                this.StringBuilder.AppendLine();
                this.StringBuilder.Append($"{prefix}\tpublic Parameters(");
                var first = false;
                foreach (var bindingParameter in actionModel.Parameters.Where(x=>x.Name != "bindingParameter"))
                {
                    if (first)
                    {
                        this.StringBuilder.Append(", ");
                    } else
                    {
                        first = true;
                    }
                    
                    this.StringBuilder.Append($"{bindingParameter.Name}");
                    if (bindingParameter.Type.IsNullable)
                    {
                        this.StringBuilder.Append("?");
                    }
                    this.StringBuilder.Append($": ");
                    this.StringBuilder.Append(GetEdmTypeRefereceString(bindingParameter.Type));
                }
                this.StringBuilder.AppendLine($") {{");
                foreach (var bindingParameter in actionModel.Parameters.Where(x => x.Name != "bindingParameter"))
                {
                    if (bindingParameter.Type.IsNullable)
                    {
                        this.StringBuilder.AppendLine($"{prefix}\t\tif ({bindingParameter.Name}) {{");
                        this.StringBuilder.AppendLine($"{prefix}\t\t\tthis.request['{bindingParameter.Name}'] = {bindingParameter.Name};");
                        this.StringBuilder.AppendLine($"{prefix}\t\t}}");
                    } else {
                        this.StringBuilder.AppendLine($"{prefix}\t\tthis.request['{bindingParameter.Name}'] = {bindingParameter.Name};");
                    }
                }
                this.StringBuilder.AppendLine($"{prefix}\t\treturn this;");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                this.StringBuilder.Append($"{prefix}\tpublic async Execute() ");
                if (actionModel.ReturnType != null)
                {
                    this.StringBuilder.Append($" : Promise<{GetEdmTypeRefereceString(actionModel.ReturnType)}>");
                }
                this.StringBuilder.AppendLine($"{{");
                this.StringBuilder.AppendLine($"{prefix}\t\tlet subscription = this.settings.http.post(this.getBaseUrl(), JSON.stringify(this.request), {{");
                this.StringBuilder.AppendLine($"{prefix}\t\t\twithCredentials: false,");
                this.StringBuilder.AppendLine($"{prefix}\t\t\theaders: this.settings.headers,");
                if (actionModel.ReturnType != null)
                {
                    this.StringBuilder.AppendLine($"{prefix}\t\t}}).pipe(map(a => {{");
                    this.StringBuilder.Append($"{prefix}\t\t\treturn");
                    this.StringBuilder.Append($" a as {GetEdmTypeRefereceString(actionModel.ReturnType)}");
                    this.StringBuilder.AppendLine($";");
                    this.StringBuilder.AppendLine($"{prefix}\t\t}}));");
                    this.StringBuilder.AppendLine($"{prefix}\t\treturn subscription.toPromise();");
                } else
                {
                    this.StringBuilder.AppendLine($"{prefix}\t\t}});");
                    this.StringBuilder.AppendLine($"{prefix}\t\treturn subscription.toPromise();");
                }
                
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();
            }
        }

        void GenerateFunctions(string prefix)
        {
            foreach (var actionModel in EdmModel.SchemaElements.OrderBy(x => x.Name).OfType<IEdmFunction>().Where(x => x.IsFunction()))
            {
                var name = $"{actionModel.Name}In{actionModel.Namespace}";
                var nameInUrl = $"{actionModel.Namespace}.{actionModel.Name}";
                if (actionModel.IsBound)
                {
                    var bindingParameter = actionModel.Parameters.Where(x => x.Name == "bindingParameter").First();
                    switch (bindingParameter.Type.Definition)
                    {
                        case IEdmEntityType bindingType:
                            {
                                name = $"{name}On{bindingType.Name}In{bindingType.Namespace}";
                            }
                            break;
                        case IEdmCollectionType collectionType:
                            {
                                var elementType = collectionType.ElementType;
                                var bindingType = (IEdmEntityType)elementType.Definition;

                                name = $"{name}On{bindingType.Name}In{bindingType.Namespace}OnEntitySet";
                            }
                            break;
                        default:
                            break;
                    }

                }
                switch (actionModel.ReturnType.Definition.TypeKind)
                {
                    case EdmTypeKind.Collection:
                        var am = actionModel.ReturnType.Definition;
                        var rt = am.AsElementType();
                        var collectionReturnType = rt.FullTypeName();
                        this.StringBuilder.AppendLine($"{prefix}export class {name} extends ODataFunctionSetOperation<{collectionReturnType}> {{");
                        break;
                    default:
                        this.StringBuilder.AppendLine($"{prefix}export class {name} extends ODataFunctionOperation {{");
                        break;
                }
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, url: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, url + '/{nameInUrl}');");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                this.StringBuilder.Append($"{prefix}\tpublic Parameters(");
                var first = false;
                foreach (var bindingParameter in actionModel.Parameters.Where(x => x.Name != "bindingParameter"))
                {
                    if (first)
                    {
                        this.StringBuilder.Append(", ");
                    }
                    else
                    {
                        first = true;
                    }

                    this.StringBuilder.Append($"{bindingParameter.Name}");
                    if (bindingParameter.Type.IsNullable)
                    {
                        this.StringBuilder.Append("?");
                    }
                    this.StringBuilder.Append($": ");
                    this.StringBuilder.Append(GetEdmTypeRefereceString(bindingParameter.Type));
                }
                this.StringBuilder.AppendLine($") {{");
                // build the parameters string from the parameters
                first = true;
                foreach (var bindingParameter in actionModel.Parameters.Where(x => x.Name != "bindingParameter"))
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        this.StringBuilder.AppendLine($"{prefix}\t\tthis.parameters += ','");
                    }
                    this.StringBuilder.AppendLine($"{prefix}\t\tthis.parameters += '{bindingParameter.Name}=' + ODataLiteral.For({bindingParameter.Name}).toUrlString();");
                }
                this.StringBuilder.AppendLine($"{prefix}\t\treturn this;");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                this.StringBuilder.Append($"{prefix}\tpublic async Execute()");
                if (actionModel.ReturnType != null)
                {
                    this.StringBuilder.Append(": Promise<");
                    switch (actionModel.ReturnType.Definition.TypeKind)
                    {
                        case EdmTypeKind.Collection:
                            var am = actionModel.ReturnType.Definition;
                            var rt = am.AsElementType();
                            var collectionReturnType = rt.FullTypeName();
                            this.StringBuilder.Append($"ODataQueryResult<{collectionReturnType}>");
                            break;
                        default:
                            this.StringBuilder.Append(GetEdmTypeRefereceString(actionModel.ReturnType));
                            break;
                    }
                    this.StringBuilder.Append(">");
                }
                this.StringBuilder.AppendLine($" {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tvar url = this.getBaseUrl();");
                this.StringBuilder.AppendLine($"{prefix}\t\tlet subscription = this.settings.http.get(url, {{");
                this.StringBuilder.AppendLine($"{prefix}\t\t\twithCredentials: false,");
                this.StringBuilder.AppendLine($"{prefix}\t\t\theaders: this.settings.headers,");
                this.StringBuilder.AppendLine($"{prefix}\t\t}}).pipe(map(a => {{");
                this.StringBuilder.Append($"{prefix}\t\t\treturn a as ");
                switch (actionModel.ReturnType.Definition.TypeKind)
                {
                    case EdmTypeKind.Collection:
                        var am = actionModel.ReturnType.Definition;
                        var rt = am.AsElementType();
                        var collectionReturnType = rt.FullTypeName();
                        this.StringBuilder.Append($"ODataQueryResult<{collectionReturnType}>");
                        break;
                    default:
                        this.StringBuilder.Append(GetEdmTypeRefereceString(actionModel.ReturnType));
                        break;
                }
                this.StringBuilder.AppendLine($";");
                this.StringBuilder.AppendLine($"{prefix}\t\t}}));");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn subscription.toPromise();");
                //if (actionModel.ReturnType == null)
                //{
                //    this.StringBuilder.AppendLine($"{prefix}\t\treturn;");
                //}
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();
            }
        }

        void GenerateEntitySets(string prefix)
        {

            foreach (var set in EdmModel.EntityContainer.Elements.OfType<IEdmEntitySet>())
            {
                var name = set.Name;
                var key = (IEdmStructuralProperty)set.EntityType().Key().First();
                var keyName = key.Name;

                // Entity Actions container class

                this.StringBuilder.AppendLine($"{prefix}export class {name}EntityActions extends ODataOperationSet {{");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, baseUrl: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, baseUrl);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                FillActions(set.EntityType(), prefix + "\t");
                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();

                // Entity Functions container class

                this.StringBuilder.AppendLine($"{prefix}export class {name}EntityFunctions extends ODataOperationSet {{");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, baseUrl: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, baseUrl);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                FillFunctions(set.EntityType(), prefix + "\t");
                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();

                // EntitySet Actions container class

                this.StringBuilder.AppendLine($"{prefix}export class {name}EntitySetActions extends ODataOperationSet {{");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, baseUrl: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, baseUrl);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                FillActions(set, prefix + "\t");
                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();

                // EntitySet Functions container class

                this.StringBuilder.AppendLine($"{prefix}export class {name}EntitySetFunctions extends ODataOperationSet {{");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, baseUrl: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, baseUrl);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                FillFunctions(set, prefix + "\t");
                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();

                // EntityGet implementation

                this.StringBuilder.AppendLine($"{prefix}export class {name}EntityGetOperation extends ODataGetOperation<{key.DeclaringType.FullTypeName()}> {{");
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, url: string) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, url);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                // actions
                this.StringBuilder.AppendLine($"{prefix}\tpublic Actions() {{");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn new {name}EntityActions(this.settings, this.buildQueryUrl());");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                // functions
                this.StringBuilder.AppendLine($"{prefix}\tpublic Functions() {{");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn new {name}EntityFunctions(this.settings, this.buildQueryUrl());");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();

                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();

                // EntitySet

                this.StringBuilder.AppendLine($"{prefix}export class {name}EntitySet extends ODataEntitySet<{key.DeclaringType.FullTypeName()}> {{");
                this.StringBuilder.AppendLine();
                // constructor
                this.StringBuilder.AppendLine($"{prefix}\tconstructor(odatasettings: ODataSettings) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tsuper(odatasettings, \"{name}\");");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                // get
                this.StringBuilder.AppendLine($"{prefix}\tGet(): {name}EntityGetOperation {{");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn new {name}EntityGetOperation(this.settings, this.definedname);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                // actions
                this.StringBuilder.AppendLine($"{prefix}\tpublic Actions() {{");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn new {name}EntitySetActions(this.settings, this.definedname);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
                // functions
                this.StringBuilder.AppendLine($"{prefix}\tpublic Functions() {{");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn new {name}EntitySetFunctions(this.settings, this.definedname);");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();

                this.StringBuilder.AppendLine($"}}");
                this.StringBuilder.AppendLine();
            }

        }

        void FillActions(IEdmEntitySet entitySetType, string prefix)
        {
            var refType = entitySetType.EntityType();
            foreach (var actionModel in EdmModel.SchemaElements.OfType<IEdmAction>()
                .Where(x => x.IsAction() && x.IsBound)
            )
            {
                var bindingParameter = actionModel.Parameters.Where(x => x.Name == "bindingParameter").First();
                var bindingTypeDefinition = bindingParameter.Type.Definition as IEdmCollectionType;
                if (bindingTypeDefinition == null)
                {
                    continue;
                }
                if (bindingTypeDefinition.TypeKind != EdmTypeKind.Collection)
                {
                    continue;
                }
                var entityType = bindingTypeDefinition.ElementType.Definition;
                if (refType != entityType)
                {
                    continue;
                }
                FillAction(actionModel, prefix, $"{refType.Name}In{refType.Namespace}OnEntitySet");
            }
        }

        void FillFunctions(IEdmEntitySet entitySetType, string prefix)
        {
            var refType = entitySetType.EntityType();
            foreach (var functionModel in EdmModel.SchemaElements.OfType<IEdmFunction>()
                .Where(x => x.IsFunction() && x.IsBound)
            )
            {
                var bindingParameter = functionModel.Parameters.Where(x => x.Name == "bindingParameter").First();
                var bindingTypeDefinition = bindingParameter.Type.Definition as IEdmCollectionType;
                if (bindingTypeDefinition == null)
                {
                    continue;
                }
                if (bindingTypeDefinition.TypeKind != EdmTypeKind.Collection)
                {
                    continue;
                }
                var entityType = bindingTypeDefinition.ElementType.Definition;
                if (refType != entityType)
                {
                    continue;
                }
                FillFunction(functionModel, prefix, $"{refType.Name}In{refType.Namespace}OnEntitySet");
            }
        }

        void FillActions(IEdmEntityType entityType, string prefix)
        {
            foreach (var actionModel in EdmModel.SchemaElements.OfType<IEdmAction>()
                .Where(x => x.IsAction() && x.IsBound)
            ) {
                var bindingParameter = actionModel.Parameters.Where(x => x.Name == "bindingParameter").First();
                var bindingTypeDefinition = bindingParameter.Type.Definition as IEdmEntityType;
                if (bindingTypeDefinition != entityType)
                {
                    continue;
                }
                FillAction(actionModel, prefix, $"{entityType.Name}In{entityType.Namespace}");
            }
        }

        void FillFunctions(IEdmEntityType entityType, string prefix)
        {
            foreach (var functionModel in EdmModel.SchemaElements.OfType<IEdmFunction>()
                .Where(x => x.IsFunction() && x.IsBound)
            ) {
                var bindingParameter = functionModel.Parameters.Where(x => x.Name == "bindingParameter").First();
                var bindingTypeDefinition = bindingParameter.Type.Definition as IEdmEntityType;
                if (bindingTypeDefinition != entityType)
                {
                    continue;
                }
                FillFunction(functionModel, prefix, $"{entityType.Name}In{entityType.Namespace}");
            }
        }

        void FillActions(string prefix)
        {
            foreach (var actionModel in EdmModel.SchemaElements.OfType<IEdmAction>()
                //.Select(x=>x.Action)
                .Where(x => x.IsAction() && !x.IsBound)
            )
            {
                FillAction(actionModel, prefix);
            }
        }

        void FillFunctions(string prefix)
        {
            foreach (var functionModel in EdmModel.SchemaElements.OfType<IEdmFunction>()
                //.Select(x => x.Function)
                .Where(x => x.IsFunction() && !x.IsBound)
            )
            {
                FillFunction(functionModel, prefix);
            }
        }

        void FillAction(IEdmAction actionType, string prefix, string nameSuffix = null)
        {
            var name = $"{actionType.Name }In{ actionType.Namespace}";
            if (nameSuffix != null)
            {
                name = $"{name}On{nameSuffix}";
            }

            this.StringBuilder.AppendLine($"{prefix}public {name}() {{");
            this.StringBuilder.AppendLine($"{prefix}\treturn new {name}(this.settings, this.baseUrl)");
            this.StringBuilder.AppendLine($"{prefix}}}");
            this.StringBuilder.AppendLine();
        }

        void FillFunction (IEdmFunction functionType, string prefix, string nameSuffix = null)
        {
            var name = $"{functionType.Name }In{ functionType.Namespace}";
            if (nameSuffix != null)
            {
                name = $"{name}On{nameSuffix}";
            }

            this.StringBuilder.AppendLine($"{prefix}public {name}() {{");
            this.StringBuilder.AppendLine($"{prefix}\treturn new {name}(this.settings, this.baseUrl)");
            this.StringBuilder.AppendLine($"{prefix}}}");
            this.StringBuilder.AppendLine();
        }

        void GenerateContainer(string prefix, string contextName)
        {

            // Types container
            this.StringBuilder.AppendLine($"{prefix}export class {contextName}ODataTypes {{");
            this.StringBuilder.AppendLine();
            foreach (var element in EdmModel.SchemaElements.OfType<IEdmEntityType>().OrderBy(x=>x.Name)) {
                this.StringBuilder.AppendLine($"{prefix}\tpublic {element.Name}() {{");
                this.StringBuilder.AppendLine($"{prefix}\t\treturn ODataType.For('{element.Name}','{element.Namespace}')");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
            }
            this.StringBuilder.AppendLine($"{prefix}}}");
            this.StringBuilder.AppendLine();

            // Entity Actions container class

            this.StringBuilder.AppendLine($"{prefix}export class {contextName}ContainerActions extends ODataOperationSet {{");
            this.StringBuilder.AppendLine();
            this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, baseUrl: string) {{");
            this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, baseUrl);");
            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine();
            FillActions(prefix + "\t");
            this.StringBuilder.AppendLine($"}}");
            this.StringBuilder.AppendLine();

            // Entity Functions container class

            this.StringBuilder.AppendLine($"{prefix}export class {contextName}ContainerFunctions extends ODataOperationSet {{");
            this.StringBuilder.AppendLine();
            this.StringBuilder.AppendLine($"{prefix}\tconstructor(settings: ODataSettings, baseUrl: string) {{");
            this.StringBuilder.AppendLine($"{prefix}\t\tsuper(settings, baseUrl);");
            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine();
            FillFunctions(prefix + "\t");
            this.StringBuilder.AppendLine($"}}");
            this.StringBuilder.AppendLine();

            this.StringBuilder.AppendLine($"@Injectable()");
            this.StringBuilder.AppendLine($"{prefix}export class {contextName} extends ODataContext {{");
            this.StringBuilder.AppendLine();
            this.StringBuilder.AppendLine($"{prefix}\tconstructor() {{");
            this.StringBuilder.AppendLine($"{prefix}\t\tsuper();");
            this.StringBuilder.AppendLine();

            // Container
            foreach (var set in EdmModel.EntityContainer.Elements.OfType<IEdmEntitySet>().OrderBy(x => x.Name))
            {
                var name = set.Name;
                var key = (IEdmStructuralProperty) set.EntityType().Key().First();
                var keyName = key.Name;

                this.StringBuilder.AppendLine($"{prefix}\t\tthis.{name} = new {name}EntitySet(this.ODataSettings);");
            }
            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine();

            // types

            this.StringBuilder.AppendLine($"{prefix}\tpublic ODataTypes() {{");
            this.StringBuilder.AppendLine($"{prefix}\t\treturn new {contextName}ODataTypes();");
            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine();

            // actions

            this.StringBuilder.AppendLine($"{prefix}\tpublic Actions() {{");
            this.StringBuilder.AppendLine($"{prefix}\t\treturn new {contextName}ContainerActions(this.ODataSettings, '');");
            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine();
            // functions

            this.StringBuilder.AppendLine($"{prefix}\tpublic Functions() {{");
            this.StringBuilder.AppendLine($"{prefix}\t\treturn new {contextName}ContainerFunctions(this.ODataSettings, '');");
            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine();

            // Container variables

            this.StringBuilder.AppendLine($"{prefix}\t// Entity Set Variables");
            foreach (var set in EdmModel.EntityContainer.Elements.OfType<IEdmEntitySet>().OrderBy(x => x.Name))
            {
                var name = set.Name;
                //this.StringBuilder.AppendLine($"{prefix}\t{name}: ODataEntitySet<{set.EntityType().FullName()}>;");
                this.StringBuilder.AppendLine($"{prefix}\t{name}: {name}EntitySet;");
            }
            this.StringBuilder.AppendLine();
            this.StringBuilder.AppendLine($"{prefix}}}");
        }

        (string Name, bool Optional) GetPropertyTypeName (IEdmStructuralProperty property, string forNamespace)
        {
            var retval = String.Empty;
            var optional = false;

            switch (property.Type.Definition)
            {
                case IEdmCollectionType ct:
                    {
                        switch (ct.ElementType)
                        {
                            case IEdmPrimitiveType inner:
                                {
                                    retval = GetPropertyEdmPrimtiveTypeString(inner.PrimitiveKind) + "[]";
                                }
                                break;
                            case IEdmPrimitiveTypeReference inner:
                                {
                                    retval = GetPropertyEdmPrimtiveTypeString(inner.PrimitiveKind()) + "[]";
                                }
                                break;
                            case IEdmTypeReference inner:
                                {
                                    retval = inner.AsTypeDefinition().FullName();
                                    if (retval.StartsWith("Edm.")) { System.Diagnostics.Debugger.Break(); }
                                    optional = true;
                                }
                                break;
                            default:
                                {
                                    throw new NotImplementedException("Not prepared for property of this kind.");
                                }
                        }                        
                    }
                    break;
                case IEdmEnumType et:
                    {
                        retval = et.FullTypeName();
                    }
                    break;
                case IEdmComplexType ct:
                    {
                        retval = ct.FullTypeName();
                    }
                    break;
                case IEdmPrimitiveType pt:
                    {
                        retval = GetPropertyEdmPrimtiveTypeString(pt.PrimitiveKind);
                    }
                    break;
                default:
                    {
                        throw new NotImplementedException("Not prepared for property of this kind.");
                    }
            }
            if (retval.StartsWith("Microsoft.OData.Edm.Csdl"))
            {
                System.Diagnostics.Debugger.Break();
            }
            if (retval == default)
            {
                return (null, false);
            }
            if (retval.StartsWith(forNamespace + ".", StringComparison.InvariantCultureIgnoreCase))
            {
                retval = retval.Substring(forNamespace.Length + 1);
            }
            return (retval, optional);
        }

        private string GetPropertyEdmPrimtiveTypeString(EdmPrimitiveTypeKind typeKind)
        {
            switch (typeKind)
            {
                case EdmPrimitiveTypeKind.Binary:
                    return "string";
                case EdmPrimitiveTypeKind.Boolean:
                    return "boolean";
                case EdmPrimitiveTypeKind.Byte:
                    return "number";
                case EdmPrimitiveTypeKind.Date:
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    return "Date";
                case EdmPrimitiveTypeKind.Decimal:
                case EdmPrimitiveTypeKind.Double:
                    return "number";
                case EdmPrimitiveTypeKind.Duration:
                    return "string";
                //case EdmPrimitiveTypeKind.Geography:
                //case EdmPrimitiveTypeKind.GeographyCollection:
                //case EdmPrimitiveTypeKind.GeographyLineString:
                //case EdmPrimitiveTypeKind.GeographyMultiLineString:
                //case EdmPrimitiveTypeKind.GeographyMultiPoint:
                //case EdmPrimitiveTypeKind.GeographyMultiPolygon:
                //case EdmPrimitiveTypeKind.GeographyPoint:
                //case EdmPrimitiveTypeKind.GeographyPolygon:
                //    break;
                case EdmPrimitiveTypeKind.Guid:
                    return "string";
                case EdmPrimitiveTypeKind.Int16:
                case EdmPrimitiveTypeKind.Int32:
                case EdmPrimitiveTypeKind.Int64:
                    return "number";
                case EdmPrimitiveTypeKind.None:
                    return default;
                //case EdmPrimitiveTypeKind.PrimitiveType:
                //    break;
                case EdmPrimitiveTypeKind.SByte:
                case EdmPrimitiveTypeKind.Single:
                    return "number";
                //case EdmPrimitiveTypeKind.Stream:
                //    break;
                case EdmPrimitiveTypeKind.String:
                    return "string";
                case EdmPrimitiveTypeKind.TimeOfDay:
                    return "string";                   
            }
            // Seems our switch statement is not complete.
            throw new NotImplementedException($"Please add handling for EdmKind {typeKind}");
        }

        private string GetEdmTypeRefereceString(IEdmTypeReference typeReference)
        {
            switch (typeReference)
            {
                case IEdmCollectionTypeReference ctr:
                    var elementType = (IEdmTypeReference) ctr.ElementType();
                    return GetEdmTypeRefereceString(elementType) + " []";
                case IEdmPrimitiveTypeReference ptr:
                    return GetPropertyEdmPrimtiveTypeString(ptr.PrimitiveKind());
                case IEdmTypeReference tr:
                    return tr.Definition.FullTypeName();
            }
            throw new NotImplementedException($"Please add handling for EdmTypeReference {typeReference}");
        }

        string GetPropertyTypeName(IEdmNavigationProperty property, string forNamespace)
        {
            var retval = String.Empty;
            switch (property.Type)
            {
                case EdmCollectionTypeReference ctr:
                    retval = ctr.ElementType().ShortQualifiedName() + "[]";
                    break;
                case EdmTypeReference tr:
                    retval = tr.ShortQualifiedName().ToString();
                    break;
            }
            if (retval.StartsWith(forNamespace + ".", StringComparison.InvariantCultureIgnoreCase))
            {
                retval = retval.Substring(forNamespace.Length + 1);
            }
            return retval;
        }

    }
}
