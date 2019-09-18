import { Guid } from "guid-typescript";

export type ODataOrder = "asc" | "desc";

export class ODataPath {

  constructor(private elements: string[]) {
  }

  static For(...elements: string[]): ODataPath {
    return new ODataPath(elements);
  }

  public toString() : string {
    return this.elements.join('/');
  }

}

export class ODataLiteral {

  constructor(private element: string) {
  }

  static For(value: string | number | Guid | Date): ODataLiteral {
    var valueString: string;
    switch (typeof value) {
      case 'string':
        valueString = `'${value}'`;
        break;
      default:
        valueString = `${value}`;
        break;
    }
    return new ODataLiteral(valueString);
  }

  public toString() : string {
    return this.element
  }

  public toUrlString(): string {
    return encodeURI(this.element);
  }

}

export class ODataBinding {

  constructor(private url: string) {
  }

  public static For(value: string): ODataBinding {
    return new ODataBinding(value);
  }

  public toString(): string {
    return this.url;
  }

  public toUrlString(): string {
    return encodeURI(this.url);
  }

}

export class ODataType {

  constructor(private type: string) {
  }

  public static For(name: string, namespace: string) {
    var type = '';
    if (namespace) {
      type += namespace;
    }
    type += "." + name;
    return new ODataType(type);
  }

  public toString(): string {
    return this.type;
  }

}
