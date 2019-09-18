# CodeCore.ODataClient
Odata Client Generator and support libraries. Currently only support for Typescript. C# is planned. This is a planned dotnet global tool, though currently not available as one.

## Unique Features
The CodeCore OdataClient supports a unique set of features. No client on the marke supports all features so choose wisely.
* Running automatic from a command line, thus integrateable with automatic build scripts. This seems to be a major issue with many other generators which go as far as working as integrated too linto Visual Studio.
* Support for multiple backend API that are all generated into separate files and into different namespaces. This is a funny issue with a lot of other Generators which can not handle multiple different client endpoints.
* Support for a different feature set than most other clients. We do not support inheritance (which we do not use) so far, or metadata (again not used here so far). On the other hand we have strong support for complex filter conditions (lacking in many clients) and generate proper containers and endpoints for Functions and Actions (which somehow many other clients totally lack).

## Roadmap
The following features are on our short term Roadmap for 2019 based on our own needs:
* Support for media streams and media stream URL generation.
* Exposed support for paging. Right now we suppress all metadata, we likely have to find a way to expose this wile working within the confines of a strongly types system (like typescript).

Missing features are added on an as needed basis. Pull requests are welcome. We also do not handle authentication in any way, leaving this to an interceptor that is part of the application.

## Usage
* Build the proxy generator.
* In the root of the API project put a json file named "odataconfig.json".
* The json file has the following format:

    {
        "output": "src/app/core/odata",
        "services": [
            {
                "namespace": "odata.coreapi",
                "metadata": "{url-to-$metadata}",
                "contextName": "ODataCoreContext"
            }
        ]
    }

* It is possible to set up multiple services.
* Start the command line with the command line. Make sure your current directory / work directory is the folder in which the odataconfig.json resides.
* The folder with the output path (possibly relative to the work folder) will be initialized (wiped). The client librraries are copied in and one .ts file per service is generated (with {namespace}.ts as name)
* For Angular it is recommended to create anew service in a separate folder. We use "odata" and "odata-service". "odata.coreapi" gets a service odata-coreapi-service that extends the base context. This creates a hierarchy point where additional code can be added.
* Initialize the core api with the relevant URL in your code. We do not hardcode it because if you run multiple environments, this should be loaded from a configuration file. The 2 fields which must be initialized are in thecontext:
 * OdataSettings.Url with the url of the api, base point (i.e. the folder where $metadata resides)
 * OdataSettings.http with the httpclient to use.
 
All naming in the library is following odata conventions. Please read them. All. Front to End. Then refer to the generated client code as documentation.
