import { map } from "rxjs/operators";
import { Guid } from "guid-typescript";

import {
  ODataType,
  ODataBinding,

  ODataPath
} from './odataclient';
import { ODataOrder, ODataLiteral } from './odataclient-primitives';
import { ODataSettings } from './odataclient-context';
import { ODataEntitySet } from './odataclient-entityset'
import { ODataExpandBuilder } from './odataclient-builder';
import { ODataQueryResult } from './odataclient-response';
import {
  ODataFilterBuilder, ODataFilterExpression
} from './odataclient-builder'

export abstract class ODataEntitiesOperation<T> {

  protected _expand!: string;
  protected _select!: string;

  /**
    * Sets the OData $expand= property
    * @param ...expand The field name(s) to be expanded
    */
  public ExpandProperties<K extends keyof T>(...expand: K[]) {
    this._expand = this.parseStringOrStringArray(...expand);
    return this;
  }

  /**
    * Sets the OData expand property based on more complex Oboject hierarchy structures
    * @param select
    */
  public Expand(expand: (expansion: ODataExpandBuilder<T>) => void) {
    var expando = new ODataExpandBuilder<T>();
    expand(expando);
    this._expand = expando.buildUrl();
    return this;
  }    

  /**
    * Sets the OData $select= property
    * @param ...select The field name(s) to be included in the OData Select
    */
  public Select<K extends keyof T>(...select: K[]) {
    this._select = this.parseStringOrStringArray(...select);
    return this;
  }

  /**
    * Executes the operation, should return an awaitable Promise
    */
  //public abstract Exec(): Promise<any>;

  protected parseStringOrStringArray(...input: Array<string | number | symbol>): string {
    if (input instanceof Array) {
      return input.join(",");
    }

    return input as string;
  }

}

/**
 * Base class for operations that work against a single entity, such as delete, get.
 * */
abstract class ODataEntityOperation<T> extends ODataEntitiesOperation<T> {

  constructor(
    protected settings: ODataSettings,
    protected url: string,
  ) {
    super();
  }

  protected key: string = '';

  /**
   * Directly sets the key string for the operation. This will not be encoded or changed in any way and should be used
   * as "last resort" of a key format not supported is used.
   * @param key
   */
  public KeyString(key: string) {
    this.key = `(${key})`;
    return this;
  }

  public Key(key: string | number | Guid) {
    var literal = ODataLiteral.For(key);
    this.KeyString(literal.toUrlString())
    return this;
  }

  protected buildQueryUrlParmeters(): string {
    let url = "?";
    if (this._expand) { url += `$expand=${this._expand}&`; }
    if (this._select) { url += `$select=${this._select}&`; }
    if (url === "?") { url = ""; }
    return url;
  }

  protected buildQueryUrl(): string {
    let url = this.url;
    if (this.key) {
      url = url + this.key;
    }
    return url;
  }

}

/**
 * Base class for get operations. This requires a key to be provided
 * */
export class ODataGetOperation<T> extends ODataEntityOperation<T> {

  constructor(
    settings: ODataSettings,
    url: string
  ) {
    super(settings, url);
  }

  suffix: string = '';

  /**
   * Set to raw value. Untested.
   * */
  public Raw() {
    this.suffix = "/$value";
    return this;
  }

  /**
   * Execute the get operation
   * */
  public async Exec(): Promise<ODataQueryResult<T>> {
    let queryString = this.buildQueryUrlParmeters();
    let url = this.url + this.key + this.suffix + queryString;
    let subscription = this.settings.http.get(url, {
      withCredentials: false,
      headers: this.settings.headers,
    }).pipe(map(a => {
      return a as ODataQueryResult<T>;
    }));
    return subscription.toPromise();
  }

  /**
   *  Get the binding string for an element. This does not trigger a get - so the object may not exist.  
   * @param relative Relative or absolute? Relative is shorter - and the default.
   */
  public Bind(relative: boolean = true): ODataBinding {
    var url = '';
    if (relative == false) {
      url = this.settings.Url;
    }
    url += this.url;
    url += this.key;
    let binding = ODataBinding.For(url);
    return binding;
  }

  /**
   * Navigate to a property (that is a a navigation property to one entity)
   * @param property
   */
  public NavigateTo<R>(property: keyof T): ODataGetOperation<R> {
    var retval = new ODataGetOperation<R>(this.settings, this.ToBinding());
    return retval;
  }

  /**
 * Navigate to a property (that is a a navigation property to an entity collection)
 * @param property
 */
  public NavigateToArray<R>(property: keyof T): ODataEntitySet<R> {
    var retval = new ODataEntitySet<R>(this.settings, this.ToBinding());
    return retval;
  }

  /**
   * Returns the binding form of the URL of the item. This is important because there are other functions that
   * actually do take this binding (to create or drop relationships, i.e.).
   * */
  public ToBinding(): string {
    let queryString = this.buildQueryUrlParmeters();
    let url = this.url + this.key + queryString;
    return url;
  }

}

/**
 * Base class for delete operations. Requires a key to be set.
 * */
export class ODataDeleteOperation<T> extends ODataEntityOperation<T> {

  constructor(
    settings: ODataSettings,
    url: string
  ) {
    super(settings, url);
  }

  suffix: string = '';

  /**
   * Set to raw value. Untested.
   * */
  public Raw() {
    this.suffix = "/$value";
    return this;
  }

  public async Exec(): Promise<any> {
    let url = this.buildQueryUrl();
    let httpRequest = this.settings.http.delete(url, {
      withCredentials: false,
      headers: this.settings.headers,
    });
    return httpRequest.toPromise();
  }

}

/**
 * Base class for post operations. Requires a key to be set.
 * */
export class ODataPostOperation<T> extends ODataEntityOperation<T> {

  constructor(
    settings: ODataSettings,
    url: string
  ) {
    super(settings, url);
  }

  entity = <any>{};

  /**
   * Set the type information. MUST be executed. Please use a type from context.ODataTypes
   * @param type
   */
  public ValueType(type: ODataType) {
    this.entity['@odata.type'] = type.toString();
    return this;
  }

  /**
   * Set the value to be put
   * @param value
   */
  public Value(value: T) {
    for (let propertyKey in Object.keys(value)) {
      var propertyBinding = propertyKey + '@odata.bind';
      delete this.entity[propertyKey]
      this.entity[propertyKey] = value[propertyKey];
    }
    this.entity = value;
    return this;
  }

  public ValueProperty(property: keyof T, value?: any) {
    var key = '' + property;
    delete this.entity[key];
    if (value != null) {
      this.entity[key] = value;
    }
    return this;
  }

  /**
   * Set a property binding.
   * @param property Name of the property
   * @param binding The binding value, or null to delete the binding.
   */
  public ValuePropertyBinding(property: keyof T, binding?: ODataBinding) {
    var propertyKey = '' + property;
    // Delete the property if it is there...
    delete this.entity[propertyKey];
    if (binding != null) {
      // set the binding...
      var propertyBinding = property + '@odata.bind';
      if (binding.toString() === "") {
        delete this.entity[propertyBinding];
      } else {
        this.entity[propertyBinding] = binding.toString();
      }
      return this;
    }
  }

  suffix: string = '';

  public Raw() {
    this.suffix = "/$value";
    return this;
  }

  public async Exec(): Promise<any> {
    let url = this.url + this.key + this.suffix;
    let httpRequest = this.settings.http.post(url, this.entity, {
      withCredentials: false,
      headers: this.settings.headers,
    });
    return httpRequest.toPromise();
  }

}

/**
 * Base class for put operations. Requires a key to be set.
 * */
export class ODataPutOperation<T> extends ODataEntityOperation<T> {

  constructor(
    settings: ODataSettings,
    url: string
  ) {
    super(settings, url);
  }

  entity: {};

  /**
   * Set the type information. MUST be executed. Please use a type from context.ODataTypes
   * @param type
   */
  public ValueType(type: ODataType) {
    this.entity['@odata.type'] = type.toString();
    return this;
  }

  /**
   * Set the value to be put
   * @param value
   */
  public Value(value: T) {
    for (let propertyKey in Object.keys(value)) {
      var propertyBinding = propertyKey + '@odata.bind';
      delete this.entity[propertyKey]
      this.entity[propertyKey] = value[propertyKey];
    }
    this.entity = value;
    return this;
  }

  public ValueProperty(property: keyof T, value?: any) {
    var key = '' + property;
    delete this.entity[key];
    if (value != null) {
      this.entity[key] = value;
    }
    return this;
  }

  /**
   * Set a property binding.
   * @param property Name of the property
   * @param binding The binding value, or null to delete the binding.
   */
  public ValuePropertyBinding(property: keyof T, binding?: ODataBinding) {
    var propertyKey = '' + property;
    // Delete the property if it is there...
    delete this.entity[propertyKey];
    if (binding != null) {
      // set the binding...
      var propertyBinding = property + '@odata.bind'
      this.entity[propertyBinding] = binding.toString;
      return this;
    }
  }

  suffix: string = '';

  public Raw() {
    this.suffix = "/$value";
    return this;
  }

  public async Exec(): Promise<any> {
    let url = this.url + this.key + this.suffix;
    let httpRequest = this.settings.http.put(url, this.entity, {
      withCredentials: false,
      headers: this.settings.headers,
    });
    return httpRequest.toPromise();
  }

}

/**
 * Base class for patch operations. Requires a key to be set.
 * */
export class ODataPatchOperation<T> extends ODataEntityOperation<T> {

  constructor(
    settings: ODataSettings,
    url: string
  ) {
    super(settings, url);
  }

  entity = <any>{};

  public Value(entity: T) {
    this.entity = entity;
    return this;
  }

  /**
 * Set the type information. MUST be executed. Please use a type from context.ODataTypes
 * @param type
 */
  public ValueType(type: ODataType) {
    this.entity['@odata.type'] = type.toString();
    return this;
  }

  public ValueProperty(property: keyof T, value?: any) {
    var key = '' + property;
    delete this.entity[key];
    if (value != null) {
      this.entity[key] = value;
    }
    return this;
  }

  /**
   * Set a property binding.
   * @param property Name of the property
   * @param binding The binding value, or null to delete the binding.
   */
  public ValuePropertyBinding(property: keyof T, binding?: ODataBinding) {
    var propertyKey = '' + property;
    // Delete the property if it is there...
    delete this.entity[propertyKey];
    if (binding != null) {
      // set the binding...
      var propertyBinding = property + '@odata.bind'
      this.entity[propertyBinding] = binding.toString();
      return this;
    }
  }

  public async Exec(): Promise<any> {
    let url = this.url + this.key;
    let httpRequest = this.settings.http.patch(url, this.entity, {
      withCredentials: false,
      headers: this.settings.headers,
    });
    return httpRequest.toPromise();
  }

}

/**
 * Base class for link/unlink operations. Requires a key to be set, as well as a binding string to the other side,
 * and the name of the property.
 * */
export class ODataLinkOperation<T> extends ODataEntityOperation<T> {

  constructor(
    settings: ODataSettings,
    url: string
  ) {
    super(settings, url);
  }

  property: String;
  binding: ODataBinding;

  /**
   * Value of the binding
   * @param property
   * @param binding The binding - can be null, for delete operations.
   */
  public Value(property: keyof T, binding: ODataBinding = null) {
    this.property = property.toString();
    this.binding = binding;
    return this;
  }

  public async Post(): Promise<any> {
    let url = this.url + this.key + "/" + this.property + "/$ref";
    let httpRequest = this.settings.http.post(url, {
      "@odata.id" : this.binding.toString()
    }, {
      withCredentials: false,
      headers: this.settings.headers,
    });
    return httpRequest.toPromise();
  }

  public async Delete(): Promise<any> {
    let url = this.url + this.key + "/" + this.property + "/$ref?$id=" + this.binding.toUrlString();
    let httpRequest = this.settings.http.delete(url, {
      withCredentials: false,
      headers: this.settings.headers,
    });
    return httpRequest.toPromise();
  }

}

export class ODataOperationSet {

  constructor(protected settings: ODataSettings, protected baseUrl: string) {
  }

}

/**
 * Base class for action operations.
 * */
export class ODataActionOperation {

  constructor(protected settings: ODataSettings, protected baseUrl: string) {
  }

  protected content: string

}

/**
 * Base class for function operations.
 * */
export class ODataFunctionOperation {

  constructor(protected settings: ODataSettings, protected baseUrl: string) {
  }

  protected parameters: string = '';

  protected getBaseUrl(): string {
    let url = this.baseUrl;
    if (this.parameters) {
      url = url + '(' + this.parameters + ')';
    }
    return url;
  }

}

export class ODataQuery<T> extends ODataEntitiesOperation<T> {

  private _filter!: string;
  private _top!: number;
  private _skip!: number;
  private _orderBy: string = '';
  private _parameters: string;

  ///The operation to perform on the queried element.
  private _operation!: string;

  private buildQueryUrl(): string {
    let url = '?';
    if (this._filter) { url += `$filter=${this._filter}&`; }
    if (this._top) { url += `$top=${this._top}&`; }
    if (this._skip) { url += `$skip=${this._skip}&`; }
    if (this._orderBy != '') { url += `$orderby=${this._orderBy}&`; }
    if (this._expand) { url += `${this._expand}&`; }
    if (this._select) { url += `$select=${this._select}&`; }
    if (this._parameters) { url += `${this._parameters}&`; }
    if (url === '?') url = '';
    if (this._operation) {
      url = "/" + this._operation + url;
    }
    if (url.endsWith("$count")) {
      return url;
    }
    return url.substring(0, url.length - 1)
  }

  constructor(
    protected settings: ODataSettings,
    protected url: string,
  ) {
    super();
  }

  /**
   * Sets a parameters string. This string will be directly set into the URL (including parameters etc.) and serves as a
   * "last resort of expandability". It allows to inject any custom set of parameters.
   * @param parameters
   */
  public ParametersString(parameters: string): ODataQuery<T> {
    this._parameters = parameters;
    return this;
  };

  /**
    * Sets the '$filter=' variable in the OData Query URL.
    * @param filter The plain text value for the odata $filter. Overrides the FilterBuilder
    * @returns the ODataQuery instance (Fluent)
    *
    * @deprecated Do not use, instead use Filter and the FilterExpression in it.
    */
  public FilterString(filter: string): ODataQuery<T> {
    this._filter = filter;
    return this;
  };

  /**
    * Builds a query expression for the OData Query
    * @param build The builder expression
    * @returns The ODataQuery instance (Fluent)
    */
  public Filter(build: (b: ODataFilterExpression<T>) => void): ODataQuery<T> {
    let builder = ODataFilterBuilder.Create<T>();
    build(builder);
    this._filter = builder.filterBuilderRef.toString();
    return this;
  }

  /**
    * Sets the OData $top= query attribute
    * @param top The value to be returned by the query
    * @returns The ODataQuery instance (Fluent)
    */
  public Top(top: number): ODataQuery<T> {
    this._top = top;
    return this;
  };

  /**
    * Sets the OData $skip= query attribute
    * @param skip The value to be skipped by the query
    * @returns The ODataQuery instance (Fluent)
    */
  public Skip(skip: number): ODataQuery<T> {
    this._skip = skip;
    return this;
  }

  /**
 * Orders elements by the given property. Works only on Get()
 *
 * @param {string} property Property on dataset to order by
 * @param {Order} [order=asc] Order "asc" for ascending and "desc" for descending.
 * @returns {ODataQueryFilterOptions<T>}
 *
 * @memberof ODataQueryFilterOptions
 */
  OrderBy(property: ODataPath | keyof T, order?: ODataOrder): ODataQuery<T> {
    if (this._orderBy != '') {
      this._orderBy += ',';
    }
    this._orderBy += property.toString();
    if (order) {
      this._orderBy += ' ' + order;
    }
    return this;
  }

  ///**
  //  * Sets the OData $orderby= query attribute
  //  * @param orderBy The field name(s) in string
  //  * @returns The ODataQuery instance (Fluent)
  //  */
  //public OrderByProperties<K extends keyof T>(...orderBy: K[]): ODataQuery<T> {
  //  this._orderBy = this.parseStringOrStringArray(...orderBy);
  //  return this;
  //}

  /**
    * Executes the query. 
    * @returns An awaitable promise with the query result.
    */
  public async Exec(): Promise<ODataQueryResult<T>> {
    let url = this.url + this.buildQueryUrl();
    let http = this.settings.http
    let subscription = http.get(url, {
      withCredentials: false,
      headers: this.settings.headers,
    }).pipe(map(a => {
      return a as ODataQueryResult<T>;
    }));
    return subscription.toPromise();
  }

  /**
  * Executes the query. 
  * @returns An awaitable promise with the query result.
  */
  public async ExecToNumber(): Promise<number> {
    let url = this.url + this.buildQueryUrl();
    let http = this.settings.http
    let subscription = http.get(url, {
      withCredentials: false,
      headers: this.settings.headers,
    }).pipe(map(a => {
      return a as number;
    }));
    return subscription.toPromise<number>();
  }

  /**
   * Executes the query as a count (i.e. getting the numbers of elements).
   * @returns An awaitable promise with the number of elements.
   *  
   */
  public Count(): Promise<number> {
    this._operation = "$count";
    return this.ExecToNumber();
  }
}


/**
 * Base class for function operations that returns an entity set, and thus will possibly allow Query() to be used
 * to further define the search result.
 * */
export class ODataFunctionSetOperation<T> extends ODataFunctionOperation {

  constructor(protected settings: ODataSettings, protected baseUrl: string) {
    super (settings, baseUrl)
  }

  Query(): ODataQuery<T> {
    return new ODataQuery(this.settings, this.getBaseUrl());
  }

}


