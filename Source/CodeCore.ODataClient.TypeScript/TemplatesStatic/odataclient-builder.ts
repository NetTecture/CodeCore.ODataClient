import { ODataPath, ODataLiteral } from './odataclient'
import { ODataQuery } from './odataclient-operation'
import { ODataOrder } from './odataclient-primitives';

type FilterSegment<T> = ODataFilterExpression<T> | ODataFilterConnection<T>;

export class ODataFilterExpression<T>{

  private value: string = "";


  constructor(public filterBuilderRef: ODataFilterBuilder<T>) { }

  private getFilterValueSegment(value: any): string {
    let castedValue = value.toString();
    if (typeof value == "string" && !/^[0-9]*$/.test(castedValue)) {
      castedValue = encodeURI(castedValue);
      return `('${castedValue}')`;
    }
    castedValue = encodeURI(castedValue);
    return `(${castedValue})`;
  }

  private Finialize() {
    this.filterBuilderRef.filterSegments.push(this);
    return new ODataFilterConnection<T>(this.filterBuilderRef);
  }

  public Evaluate(left: ODataPath, operator: string, right: ODataLiteral) {
    this.value = `${left.toString()} eq ${right.toString()}`;
    return this.Finialize();
  }

  /**
    * Creates an instance of an Equals (~eq) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public Equals<T>(path: ODataPath, value: any) {
    return this.EqualsImpl(path.toString(), value)
  }
  public NotEqualsPath<T>(path: ODataPath, value: any) {
    return this.NotEqualsImpl(path.toString(), value)
  }
  public EqualsField<K extends keyof T>(field: K, value: any) {
    return this.EqualsImpl(field.toString(), value);
  }
  private EqualsImpl(path: string, value: any) {
    this.value = `${path} eq ${this.getFilterValueSegment(value)}`;
    return this.Finialize();
  }
  private NotEqualsImpl(path: string, value: any) {
    this.value = `${path} ne ${this.getFilterValueSegment(value)}`;
    return this.Finialize();
  }

  /**
    * Creates an instance of an Not Equals (~ne) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public NotEquals<K extends keyof T>(field: K, value: any) {
      this.value = `${field} ne ${this.getFilterValueSegment(value)}`;
      return this.Finialize();
  }

  /**
    * Creates an instance of a Greater Than (~gt) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public GreaterThan<K extends keyof T>(field: K, value: any) {
      this.value = `${field} gt ${this.getFilterValueSegment(value)}`;
      return this.Finialize();
  }

  /**
    * Creates an instance of a Greater Than or Equals (~ge) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public GreaterThanOrEquals<K extends keyof T>(field: K, value: any) {
      this.value = `${field} ge ${this.getFilterValueSegment(value)}`;
      return this.Finialize();
  }

  /**
    * Creates an instance of a Lesser Than (~lt) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public LessThan<K extends keyof T>(field: K, value: any) {
      this.value = `${field} lt ${this.getFilterValueSegment(value)}`;
      return this.Finialize();
  }


  /**
    * Creates an instance of a Lesser Than or equals (~le) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public LessThanOrEquals<K extends keyof T>(field: K, value: any) {
      this.value = `${field} le ${this.getFilterValueSegment(value)}`;
      return this.Finialize();
  }

  /**
    * Creates an instance of a HAS (~has) filter segment
    * @param field The name of the field to check (String literal)
    * @param value The value to check
    * @returns The next ODataFilterConnection (Fluent)
    */
  public Has<K extends keyof T>(field: K, value: any) {
      this.value = `${field} has ${this.getFilterValueSegment(value)}`;
      return this.Finialize();
  }

  /**
    * Creates an instance of a nested negated (~not) FilterBuilder object
    * @param build The fluent chain for the filter expression
    * @returns The next ODataFilterConnection (Fluent)
    */
  public Not<K extends keyof T>(build: (b: ODataFilterExpression<T>) => void) {
      let builder = ODataFilterBuilder.Create<T>();
      build(ODataFilterBuilder.Create<T>());
      this.value = `not (${builder.toString()})`;
      return this.Finialize();
  }

  /**
    * Seaches for any element. Takes a sub-query 
    * @param build
    * @returns The next ODataFilterConnection (Fluent)
    */
public Any<K extends keyof T>(field: K, value: any) {
    let builder = ODataFilterBuilder.Create<T>();
  this.value = `${field}/any (${value})`;
    return this.Finialize();
  }

  /**
  * Seaches for any element. Takes a sub-query 
  * @param build
  * @returns The next ODataFilterConnection (Fluent)
  */
  public Contains<K extends keyof T>(path: ODataPath | K, value: any) {
    var literal = ODataLiteral.For(value);
    this.value = `contains(${path.toString()}, ${literal.toString()})`;
    return this.Finialize();
  }

  /**
    * Seaches for all element. Takes a sub-query 
    * @param build
    * @returns The next ODataFilterConnection (Fluent)
    */
  public All<K extends keyof T>(field: K, build: (b: ODataFilterExpression<T>) => void) {
    let builder = ODataFilterBuilder.Create<T>();
    build(ODataFilterBuilder.Create<T>());
    this.value = `all (${builder.toString()})`;
    return this.Finialize();
  }


  /**
    * Creates an instance of a nested FilterBuilder object
    * @param build The fluent chain for the filter expression
    * @returns The next ODataFilterConnection (Fluent)
    */
  public BuildFilter(build: (b: ODataFilterExpression<T>) => void) {
      let builder = ODataFilterBuilder.Create<T>();
      build(ODataFilterBuilder.Create<T>());
      this.value = `(${builder.toString()})`;
      return this.Finialize();
  }

  /**
    * Gets the evaluated OData filter segment
    * @returns The OData filter segment
    */
  public toString(): string {
      return this.value;
  }

}

export class ODataFilterConnection<T>{

  private type: 'and' | 'or' = "and";

  constructor(public filterBuilderRef: ODataFilterBuilder<T>) { }

  /**
    * Sets the connection between OData Filter expression segments to 'AND' type
    * @returns The next ODataFilterExpression (Fluent)
    */
  public get And() {
      this.type = "and";
      this.filterBuilderRef.filterSegments.push(this);
      return new ODataFilterExpression<T>(this.filterBuilderRef);
  }

  /**
  * Sets the connection between OData Filter expression segments to 'OR' type
  * @returns The next ODataFilterExpression (Fluent)
  */
  public get Or() {
      this.type = "or";
      this.filterBuilderRef.filterSegments.push(this);
      return new ODataFilterExpression<T>(this.filterBuilderRef);
  }

  public toString() {
      return this.type;
  }

}

export class ODataFilterBuilder<T>{

  public filterSegments: FilterSegment<T>[] = [];

  /**
    * Factory method for creating ODataFilterBuilders
    * @returns The first ODataFilterExpression value for the ODataFilterBuilder
    */
  public static Create<T>(): ODataFilterExpression<T> {
      let builder = new ODataFilterBuilder();
      let firstSegment = new ODataFilterExpression(builder);
      return firstSegment;
  }

  /**
    * Evaluates the ODataFilterBuilder<T>'s segments into a parsed OData Filter expression
    * @returns The Filter query expression
    */
  public toString(): string {
      return this.filterSegments.map(s => s.toString()).join(' ');
  }

}

/**
 * This class represents an Expand and inner select clause.
 */
export class ODataExpandBuilder<T> {

  //protected _expand!: string;
  protected _select: string = '';
  protected _expand: string = '';
  protected _filter: string = '';
  protected _orderBy: string = '';

  /**
   * Sets the select
   * @param ...expand The field name(s) to be expanded
   */
  public Select<K extends keyof T>(select: K) {
    if (this._select != '') {
      this._select += ",";
    }
    this._select += select;
    //this._select.push(select);
    //this._expand = this.parseStringOrStringArray(expand);
    return this;
  }

  public OrderBy<K extends keyof T>(property: K, order?: ODataOrder) {
    if (this._orderBy != '') {
      this._orderBy += ',';
    }
    this._orderBy += property;
    if (order) {
      this._orderBy += ' ' + order;
    }
    return this;
  }
  /**
   * Sets the expand
   * @param ...expand The field name(s) to be expanded
   */
  public Expand<K extends keyof T>(property: K, expand?: (expansion: ODataExpandBuilder<T[K]>) => void) {
    var expando = new ODataExpandBuilder<T>();
    if (expand != null) {
      expand(expando);
    }
    if (this._expand != '') {
      this._expand += ',';
    }
    this._expand += property;
    let nextExpand = expando.buildUrl();
    if (nextExpand != '') {
      this._expand += '(' + nextExpand + ')';
    }
    return this;
  }

  /**
   * Sets the expand
   * @param ...expand The field name(s) to be expanded
   */
  public ExpandArray<K extends keyof any, T>(property: K, expand?: (expansion: ODataExpandBuilder<T[any]>) => void) {
    var expando = new ODataExpandBuilder<T>();
    if (expand != null) {
      expand(expando);
    }
    if (this._expand != '') {
      this._expand += ',';
    }
    this._expand += property;
    let nextExpand = expando.buildUrl();
    if (nextExpand != '') {
      this._expand += '(' + nextExpand + ')';
    }
    //this._expand = this.parseStringOrStringArray(...expand);
    return this;
  }

  /**
  * Sets the '$filter=' variable in the OData Query URL.
  * @param filter The plain text value for the odata $filter. Overrides the FilterBuilder
  * @returns the ODataQuery instance (Fluent)
  *
  * @deprecated Do not use, instead use Filter and the FilterExpression in it.
  */
  public FilterString(filter: string) {
    this._filter = filter;
    return this;
  };

  /**
    * Builds a query expression for the OData Query
    * @param build The builder expression
    * @returns The ODataQuery instance (Fluent)
    */
  public Filter(build: (b: ODataFilterExpression<T>) => void) {
    let builder = ODataFilterBuilder.Create<T>();
    build(builder);
    this._filter = builder.filterBuilderRef.toString();
    return this;
  }


  //public parseStringOrStringArray(...input: Array<string | number | symbol>): string {
  //  if (input instanceof Array) {
  //    return input.join(",");
  //  }
  //  return input as string;
  //}

  public buildUrl(prefix: boolean = false): string {
    let retval = '';
    if (this._expand != '') {
      retval += "$expand=" + this._expand;
    }
    if (this._select != '') {
      if (retval != '') {
        retval += ';';
      }
      retval += '$select=' + this._select;
    }
    if (this._orderBy != '') {
      if (retval != '') {
        retval += ';';
      }
      retval += '$orderby=' + this._orderBy;
    }
    if (this._filter != '') {
      if (retval != '') {
        retval += ';';
      }
      retval += '$filter=' + this._filter;
    }
    return retval;
  }

}
