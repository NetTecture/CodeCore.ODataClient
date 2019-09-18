import { map } from "rxjs/operators";
import {
  ODataDeleteOperation, ODataPostOperation, ODataPutOperation, ODataPatchOperation,
  ODataLinkOperation, ODataQuery
} from './odataclient-operation';
import { ODataQueryResult } from './odataclient-response';
import { ODataSettings } from './odataclient-context';
import { ODataLiteral } from "./odataclient";


export class ODataEntitySet<T> {

  constructor(
    protected settings: ODataSettings,
    protected definedname: string
  ) {
  }

  protected getEntityUriSegment(entityKey: any): string {
    var literal = ODataLiteral.For(entityKey);
    return `(${literal.toUrlString()})`;
  }

  Query(): ODataQuery<T> {
    let entitySetUrl = this.settings.Url + this.definedname;
    return new ODataQuery(this.settings, entitySetUrl);
  }

  Delete(): ODataDeleteOperation<T> {
    let entitySetUrl = this.settings.Url + this.definedname;
    return new ODataDeleteOperation<T>(this.settings, entitySetUrl);
  }

  Post(): ODataPostOperation<T> {
    let entitySetUrl = this.settings.Url + this.definedname;
    return new ODataPostOperation<T>(this.settings, entitySetUrl);
  }

  Put(): ODataPutOperation<T> {
    let entitySetUrl = this.settings.Url + this.definedname;
    return new ODataPutOperation<T>(this.settings, entitySetUrl);
  }

  Patch(): ODataPatchOperation<T> {
    let entitySetUrl = this.settings.Url + this.definedname;
    return new ODataPatchOperation<T>(this.settings, entitySetUrl);
  }

  Link(): ODataLinkOperation<T> {
    let entitySetUrl = this.settings.Url + this.definedname;
    return new ODataLinkOperation<T>(this.settings, entitySetUrl);
  }

}
