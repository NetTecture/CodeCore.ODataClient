export class ODataQueryResult<T>{

    public '@odata.context': string;
    public '@odata.count': number;
    Value: any;

    /**
        * The '@odata.context' variable returned by the OData service
        */
    public get Context(): string {
        return this['@odata.context'];
    }

    /**
        * The '@odata.count' variable returned by the OData service
        */
    public get Count(): number {
        return this['@odata.count'];
    }

    /**
        * The query result in an array
        */
    public value: T[] = [];

}

export class ODataError implements Error {

    public Response: any;
    public name: string;
    public message: string;

    constructor(response: Response) {
        this.name = "OData Request Error";
        this.message = response.statusText;
    }

}
