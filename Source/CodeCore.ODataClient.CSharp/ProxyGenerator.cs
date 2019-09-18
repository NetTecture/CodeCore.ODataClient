using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using CodeCore.OData.ProxyGen.Abstract;
using Microsoft.OData.Edm;

namespace CodeCore.OData.ProxyGen.CSharp
{
    class ProxyGenerator : ProxyGeneratorBase
    {

        protected override void PrepareTarget(bool Initialize)
        {
            string outputPath = TargetPath;
            // Header
            var assembly = Assembly.GetAssembly(typeof(ProxyGenerator));
            var assemblyName = assembly.GetName().Name;

            var prefix = $"{assemblyName}.TemplatesStatic.";
            var streams = assembly.GetManifestResourceNames();
            foreach (var file in Directory.EnumerateFiles(outputPath))
            {
                File.Delete(file);
            }
            foreach (var templateName in streams.Where(x=>x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)))
            {
                var file = String.Empty;
                var name = templateName.Substring(prefix.Length);
                using (var resourceStream = assembly.GetManifestResourceStream(templateName))
                using (var fileStream = new System.IO.FileStream(Path.Combine(outputPath, name), FileMode.Create))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
        }

        public override void Generate(string tsNamespace, string contextName)
        {
            var outputPath = Path.Combine(TargetPath, $"{tsNamespace}.ts");

            GenerateImportStatements();
            this.StringBuilder.AppendLine();

            this.StringBuilder.AppendLine($"namespace {tsNamespace} {{");

            var prefix1 = "\t";
            GenerateEnumerations(prefix1);
            GenerateComplexTypes(prefix1);
            GenerateEntityTypes(prefix1);
            GenerateEntitySets(prefix1);
            GenerateContainer(prefix1);

            this.StringBuilder.AppendLine($"}}");

            var s = this.StringBuilder.ToString();
            if (outputPath == default)
            {
                Console.WriteLine(s);
                return;
            }
            using (var sw = new StreamWriter(Path.Combine(TargetPath, outputPath), false))
            {
                sw.Write(s);
            }
        }

        void GenerateImportStatements()
        {
            this.StringBuilder.AppendLine($"import {{Edm}} from './odataclient-edm'");
        }

        public void GenerateEnumerations(string prefix)
        {
            foreach (var group in EdmModel.SchemaElements.OfType<IEdmEnumType>().GroupBy(x => x.Namespace))
            {
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\t//Namespace: {group.Key}");
                this.StringBuilder.AppendLine($"{prefix}\texport namespace {group.Key} {{");

                foreach (var item in group)
                {
                    this.StringBuilder.AppendLine();
                    this.StringBuilder.AppendLine($"{prefix}\t\t//Enum: {item.Name}");
                    this.StringBuilder.AppendLine($"{prefix}\t\texport enum {item.Name} {{");

                    var first = true;
                    foreach (var member in item.Members)
                    {
                        if (!first)
                        {
                            this.StringBuilder.AppendLine(",");
                        }
                        var name = member.Name;
                        var value = member.Value.Value;
                        this.StringBuilder.Append($"{prefix}\t\t\t{name} = \"{name}\"");
                        first = false;
                    }
                    this.StringBuilder.AppendLine();
                    this.StringBuilder.AppendLine($"{prefix}\t\t}}");
                }
                this.StringBuilder.AppendLine($"{prefix}\t}}");
            }
            this.StringBuilder.AppendLine();
        }

        public void GenerateComplexTypes(string prefix)
        {
            foreach (var group in EdmModel.SchemaElements.OfType<IEdmComplexType>().GroupBy(x => x.Namespace))
            {
                this.StringBuilder.AppendLine();
                this.StringBuilder.AppendLine($"{prefix}\t//Namespace: {group.Key}");
                this.StringBuilder.AppendLine($"{prefix}\texport namespace {group.Key} {{");
                foreach (var item in group)
                {
                    this.StringBuilder.AppendLine($"{prefix}\t\t//Complex Type: {item.Name}");
                    this.StringBuilder.Append($"{prefix}\t\texport interface {item.Name} ");
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
                        if (type.Optional)
                        {
                            name = name + "?";
                        }
                        this.StringBuilder.AppendLine($"{prefix}\t\t\t{name}: {type.Name};");
                    }
                    foreach (var property in item.DeclaredNavigationProperties())
                    {
                        var name = property.Name;
                        var typeName = GetPropertyTypeName(property, group.Key);
                        this.StringBuilder.AppendLine($"{prefix}\t\t\t{name}?: {typeName};");
                    }
                    this.StringBuilder.AppendLine($"{prefix}\t\t}}");
                }
                this.StringBuilder.AppendLine($"{prefix}\t}}");
            }
            this.StringBuilder.AppendLine();
        }

        public void GenerateEntityTypes (string prefix)
        {
            foreach (var group in EdmModel.SchemaElements.OfType<IEdmEntityType>().GroupBy(x => x.Namespace))
            {

                this.StringBuilder.AppendLine($"{prefix}\t//Namespace: {group.Key}");
                this.StringBuilder.AppendLine($"{prefix}\texport namespace {group.Key} {{");
                this.StringBuilder.AppendLine();
                foreach (var item in group)
                {
                    this.StringBuilder.AppendLine($"{prefix}\t\t//Entity Type: {item.Name}");
                    this.StringBuilder.Append($"{prefix}\t\texport interface {item.Name}");
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
                        if (type.Optional)
                        {
                            name = name + "?";
                        }
                        this.StringBuilder.AppendLine($"{prefix}\t\t\t{name}: {type.Name};");
                    }
                    foreach (var property in item.DeclaredNavigationProperties())
                    {
                        var name = property.Name;
                        var typeName = GetPropertyTypeName(property, group.Key);
                        this.StringBuilder.AppendLine($"{prefix}\t\t\t{name}?: {typeName};");
                    }
                    this.StringBuilder.AppendLine($"{prefix}\t\t}}");
                    this.StringBuilder.AppendLine();
                }
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
            }
        }

        void GenerateEntitySets (string prefix)
        {
            foreach (var set in EdmModel.EntityContainer.Elements.OfType<IEdmEntitySet>())
            {
                var name = set.Name;
                var entity = set.EntityType();

                this.StringBuilder.AppendLine($"{prefix}\texport class {name}EntitySet extends odatatools.EntitySet<{entity.Namespace}.{entity.Name}> {{");
                this.StringBuilder.AppendLine($"{prefix}\t\tconstructor (");
                this.StringBuilder.AppendLine($"{prefix}\t\t\tname: string,");
                this.StringBuilder.AppendLine($"{prefix}\t\t\taddress: string,");
                this.StringBuilder.AppendLine($"{prefix}\t\t\tkey: string,");
                this.StringBuilder.AppendLine($"{prefix}\t\t\tadditionalHeaders?: odatajs.Header");
                this.StringBuilder.AppendLine($"{prefix}\t\t) {{");
                this.StringBuilder.AppendLine($"{prefix}\t\t\tsuper(name, address, key, additionalHeaders);");
                this.StringBuilder.AppendLine($"{prefix}\t\t}}");
                this.StringBuilder.AppendLine($"{prefix}\t}}");
                this.StringBuilder.AppendLine();
            }

        }

        void GenerateContainer(string prefix)
        {
            this.StringBuilder.AppendLine($"{prefix}export namespace Container {{");
            this.StringBuilder.AppendLine();

            this.StringBuilder.AppendLine($"{prefix}\texport class Container extends odatatools.ProxyBase {{");
            this.StringBuilder.AppendLine();
            this.StringBuilder.AppendLine($"{prefix}\t\tconstructor(address: string, name? : string, additionalHeaders?: odatajs.Header) {{");
            this.StringBuilder.AppendLine($"{prefix}\t\t\tsuper(address, name, additionalHeaders);");
            this.StringBuilder.AppendLine();

            // Container
            foreach (var set in EdmModel.EntityContainer.Elements.OfType<IEdmEntitySet>())
            {
                var name = set.Name;
                var key = set.EntityType().Key().First();

                this.StringBuilder.AppendLine($"{prefix}\t\t\tthis.{name} = new {name}EntitySet(\"{name}\", address, \"{key}\", additionalHeaders);");
                this.StringBuilder.AppendLine();
            }
            this.StringBuilder.AppendLine($"{prefix}\t\t}}");
            this.StringBuilder.AppendLine();

            // Container variables

            this.StringBuilder.AppendLine($"{prefix}\t\t// Entity Set Variables");
            foreach (var set in EdmModel.EntityContainer.Elements.OfType<IEdmEntitySet>())
            {
                var name = set.Name;
                this.StringBuilder.AppendLine($"{prefix}\t\t{name}: {name}EntitySet;");
            }

            this.StringBuilder.AppendLine($"{prefix}\t}}");
            this.StringBuilder.AppendLine($"{prefix}}}");
        }

        (string Name, bool Optional) GetPropertyTypeName (IEdmStructuralProperty property, string forNamespace)
        {
            var retval = String.Empty;
            if (property.Type.Definition.TypeKind == EdmTypeKind.Collection)
            {
                var et = property.Type.Definition.AsElementType();
                retval = et.FullTypeName() + "[]";
                if (retval.StartsWith(forNamespace + ".", StringComparison.InvariantCultureIgnoreCase))
                {
                    retval = retval.Substring(forNamespace.Length + 1);
                }
                return (retval, true);
            }
            retval = property.Type.Definition.ToString();
            if (retval.StartsWith(forNamespace + ".", StringComparison.InvariantCultureIgnoreCase))
            {
                retval = retval.Substring(forNamespace.Length + 1);
            }
            return (retval, false);
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
